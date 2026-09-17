// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AgentGuard.Analyzers;

/// <summary>
/// The carried-type lens: the types a symbol or an operation carries WITHOUT the source writing them out — the
/// declared type of a field, a property, a method return, a method parameter, and a local including an inferred
/// <c>var</c>, plus the return type of an invoked member. This is how an access rule reaches a guarded type that code
/// obtains through a public member rather than naming. The four declaration registrations were AG0006's alone; they
/// live here now and AG0006, AG0041, AG0023's Engine gate, and AG0029's Engine gate all run through this one owner, so
/// no rule re-spells them.
/// <para>
/// Each consumer supplies one inspector that receives the carried type, the symbol the declaration belongs to (the
/// site an access rule tests against its one permitted caller), and where to report; it returns the diagnostic to
/// report, or <see langword="null"/> to accept. The walk through generic arguments, array elements, and pointer
/// targets is the separate <see cref="TypeTree"/> owner, which each consumer drives with its own leaf test.
/// </para>
/// <para>
/// The written-name half — every name the source writes out — is the separate <see cref="WrittenNameScanner"/>.
/// </para>
/// </summary>
internal static class DeclaredTypeScanner
{
    /// <summary>
    /// The accessor and event method kinds whose declared types are already covered by the property or event they
    /// belong to; AG0006 has always skipped them and every consumer inherits that.
    /// </summary>
    private static readonly ImmutableArray<MethodKind> SyntheticMethodKinds = ImmutableArray.Create(
        MethodKind.PropertyGet,
        MethodKind.PropertySet,
        MethodKind.EventAdd,
        MethodKind.EventRemove,
        MethodKind.EventRaise);

    /// <summary>
    /// Registers the four DECLARATION positions — field, property, method (return and parameters), and local — for
    /// every compilation. This is the set AG0006 has always scanned, extracted unchanged.
    /// </summary>
    /// <param name="context">The compilation-start context to register on.</param>
    /// <param name="inspect">The rule's inspector, given the carried type, the declaring symbol, and the location;
    /// it returns the diagnostic to report, or <see langword="null"/> to accept.</param>
    internal static void RegisterDeclarations(
        CompilationStartAnalysisContext context,
        Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect)
    {
        context.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, inspect), SymbolKind.Field);
        context.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, inspect), SymbolKind.Property);
        context.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, inspect), SymbolKind.Method);
        context.RegisterOperationAction(
            operationContext => AnalyzeLocal(operationContext, inspect), OperationKind.VariableDeclarator);
    }

    /// <summary>
    /// Registers the FULL carried-type lens — the four declaration positions plus the return type of an invoked member
    /// — but only when the compilation being analyzed is the assembly named <paramref name="assemblyName"/>. The
    /// invoked-return position is added here rather than in <see cref="RegisterDeclarations"/> because AG0006 never
    /// scanned it and this contract forbids changing AG0006's diagnostics; the access rules need it so a guarded type
    /// obtained from a call and never declared is still caught.
    /// </summary>
    /// <param name="context">The compilation-start context to gate and register on.</param>
    /// <param name="assemblyName">The assembly name the compilation must match for the scan to be registered.</param>
    /// <param name="inspect">The rule's inspector, given the carried type, the declaring or containing symbol, and the
    /// location; it returns the diagnostic to report, or <see langword="null"/> to accept.</param>
    internal static void RegisterForAssembly(
        CompilationStartAnalysisContext context,
        string assemblyName,
        Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect)
    {
        if (!string.Equals(context.Compilation.AssemblyName, assemblyName, StringComparison.Ordinal))
        {
            return;
        }

        RegisterDeclarations(context, inspect);
        context.RegisterOperationAction(
            operationContext => AnalyzeInvocationReturn(operationContext, inspect), OperationKind.Invocation);
    }

    private static void AnalyzeField(
        SymbolAnalysisContext context, Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (!field.IsImplicitlyDeclared)
        {
            ReportFirst(context, inspect, field.Type, field, field.Locations);
        }
    }

    private static void AnalyzeProperty(
        SymbolAnalysisContext context, Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (!property.IsImplicitlyDeclared)
        {
            ReportFirst(context, inspect, property.Type, property, property.Locations);
        }
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context, Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared || SyntheticMethodKinds.Contains(method.MethodKind))
        {
            return;
        }

        ReportFirst(context, inspect, method.ReturnType, method, method.Locations);

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            ReportFirst(context, inspect, parameter.Type, method, parameter.Locations);
        }
    }

    private static void AnalyzeLocal(
        OperationAnalysisContext context, Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect)
    {
        var declarator = (IVariableDeclaratorOperation)context.Operation;
        Diagnostic? diagnostic = inspect(
            declarator.Symbol.Type, EnclosingSymbol(context), declarator.Symbol.Locations[0]);
        if (diagnostic is not null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeInvocationReturn(
        OperationAnalysisContext context, Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect)
    {
        var invocation = (IInvocationOperation)context.Operation;
        Diagnostic? diagnostic = inspect(
            invocation.TargetMethod.ReturnType, EnclosingSymbol(context), invocation.Syntax.GetLocation());
        if (diagnostic is not null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    // The innermost symbol the operation sits inside, so a declaration written in a lambda or a local function nested
    // in a permitted method is NOT treated as sitting in that method. Owned by SymbolResolution.
    private static ISymbol EnclosingSymbol(OperationAnalysisContext context)
    {
        return SymbolResolution.EnclosingSymbol(
            context.Operation.SemanticModel,
            context.Operation.Syntax,
            context.ContainingSymbol,
            context.CancellationToken) ?? context.ContainingSymbol;
    }

    private static void ReportFirst(
        SymbolAnalysisContext context,
        Func<ITypeSymbol, ISymbol, Location, Diagnostic?> inspect,
        ITypeSymbol declaredType,
        ISymbol declaringSymbol,
        ImmutableArray<Location> locations)
    {
        if (locations.Length == 0)
        {
            return;
        }

        Diagnostic? diagnostic = inspect(declaredType, declaringSymbol, locations[0]);
        if (diagnostic is not null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }
}
