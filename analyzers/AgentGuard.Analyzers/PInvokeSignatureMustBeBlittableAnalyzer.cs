// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a native-interop (P/Invoke) declaration whose signature uses a marshalling-unsafe type — <c>object</c>,
/// <c>dynamic</c>, an unconstrained generic type parameter, or a C-style variadic (<c>__arglist</c>) parameter list.
/// Each of these leaves the marshaller to guess a layout at runtime: <c>object</c>/<c>dynamic</c> box arbitrary values
/// across the boundary, an open type parameter has no fixed native representation, and <c>__arglist</c> has no
/// portable ABI. A native binding must spell an explicit, blittable, marshallable type for every return and parameter
/// (the macOS <c>objc_msgSend</c> and Windows presence bindings). Preventive against the presence P/Invokes; the
/// existing filesystem bindings already use explicit blittable signatures.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PInvokeSignatureMustBeBlittableAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0102";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A P/Invoke signature must not use object, dynamic, an unconstrained generic, or a variadic parameter",
        messageFormat: "Native interop '{0}' uses a marshalling-unsafe type ({1}); a P/Invoke return and every parameter must be an explicit blittable, marshallable type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A method carrying [DllImport] or [LibraryImport] must not use object, dynamic, an unconstrained generic type parameter, or a C-style variadic (__arglist) parameter list in its signature: each leaves the marshaller without a fixed native layout. Every return and parameter must be an explicit blittable, marshallable type. AG0008 confines the P/Invoke declaration to the per-OS libraries; this rule constrains its signature.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedRules;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!PInvoke.IsPInvoke(method))
        {
            return;
        }

        // A C-style variadic parameter list (__arglist) has no portable native ABI.
        if (method.IsVararg)
        {
            Report(context, method, "a variadic (__arglist) parameter list");
            return;
        }

        if (Offending(method.ReturnType) is { } returnReason)
        {
            Report(context, method, "the return type is " + returnReason);
            return;
        }

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (Offending(parameter.Type) is { } parameterReason)
            {
                Report(context, method, $"parameter '{parameter.Name}' is {parameterReason}");
                return;
            }
        }
    }

    private static string? Offending(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Dynamic)
        {
            return "dynamic";
        }

        if (type.SpecialType == SpecialType.System_Object)
        {
            return "object";
        }

        // An unconstrained generic type parameter has no fixed native representation.
        return type is ITypeParameterSymbol ? "an unconstrained generic type parameter" : null;
    }

    private static void Report(SymbolAnalysisContext context, IMethodSymbol method, string reason)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.Name, reason));
    }
}
