// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports the CLI (compiled as <c>guard</c>) or <c>AgentGuard.TestHelpers</c> reaching any internal type or internal
/// member declared in <c>AgentGuard.Engine</c>, in every position the language allows, with one exception: the
/// <c>SystemServices</c> type named as the receiver of an invocation of its static <c>Create</c> method, and that
/// <c>Create</c> member itself in that same invocation. Relocating the container into Engine requires Engine to grant
/// those two assemblies internal access; this rule keeps that grant as narrow as the one factory call it exists for,
/// rather than letting it become a blanket door into every Engine internal.
/// <para>
/// The exception is no wider than the approved call. <c>SystemServices</c> written in any other position — as a
/// declared type, in <c>typeof</c> or <c>nameof</c>, in a cast or a pattern, as a generic argument, in a base list, or
/// through a using alias — is reported, and so is any member of it other than that invoked <c>Create</c>. Every PUBLIC
/// Engine API stays reachable and is never reported, and the <c>AgentGuard.Tests</c> compilation is deliberately NOT
/// gated, so its existing grant is untouched.
/// </para>
/// <para>
/// Coverage is established by two complementary lenses rather than by a list of syntax positions, so a position nobody
/// enumerated is still caught: <see cref="WrittenNameScanner"/> resolves every name the source writes, and
/// <see cref="DeclaredTypeScanner"/> reads the types a declaration or an invocation carries without naming them. Each
/// lens walks the whole type tree through <see cref="TypeTree"/>, so an internal Engine type nested inside a
/// generic argument, an array element, or a pointer target is caught. The two lenses are separate registrations and
/// each reports what it finds; one statement can therefore produce more than one diagnostic.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EngineInternalsOneDoorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0041";

    private const string Category = "AgentGuard.Architecture";

    private const string MessageFormat =
        "'{0}' reaches the AgentGuard.Engine internal '{1}'; the only Engine internal it may reach is "
        + CompositionPoint.ContainerFactoryDescription;

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "The CLI and TestHelpers may reach only SystemServices.Create() among Engine internals",
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AgentGuard.Engine grants internal access to the CLI (guard) and AgentGuard.TestHelpers so they can call the one container factory. That grant reaches exactly one thing: the invocation of AgentGuard.Engine.SystemServices.Create(). Every other internal Engine type and member is out of reach from those two assemblies, in every position the language allows — a declared type, typeof, nameof, a cast, a pattern, a generic argument, a base list, a using alias, and every type a declaration or an invocation carries. Every public Engine API stays reachable, and the AgentGuard.Tests grant is unchanged.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    /// <summary>
    /// The two gated consumers: the CLI's real compiled assembly name and the test-only helpers assembly. The main
    /// <c>AgentGuard.Tests</c> compilation is deliberately absent — its grant predates this rule and is untouched.
    /// </summary>
    private static readonly ImmutableArray<string> GatedAssemblies = ImmutableArray.Create(
        CliAssembly.CompiledName,
        TestAssembly.TestHelpersName);

    // Cached so no delegate is allocated per analyzed node or symbol.
    private static readonly Func<INamedTypeSymbol, bool> IsEngineInternalType =
        type => IsDeclaredInEngine(type) && !IsPubliclyVisible(type);

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

        context.RegisterCompilationStartAction(startContext =>
        {
            foreach (string gatedAssembly in GatedAssemblies)
            {
                WrittenNameScanner.RegisterForAssembly(startContext, gatedAssembly, InspectWrittenName);
                DeclaredTypeScanner.RegisterForAssembly(
                    startContext,
                    gatedAssembly,
                    (declaredType, _, location) => CarriedType(
                        location, startContext.Compilation.AssemblyName, declaredType));
            }
        });
    }

    private static void InspectWrittenName(SyntaxNodeAnalysisContext context, ISymbol symbol)
    {
        if (symbol is ITypeSymbol referenced)
        {
            if (TypeTree.Any(referenced, IsEngineInternalType) && !IsApprovedContainerReceiver(context, referenced))
            {
                Report(context, referenced);
            }

            return;
        }

        if (IsEngineInternalMember(symbol) && !IsApprovedCreateMember(context, symbol))
        {
            Report(context, symbol);
        }
    }

    private static Diagnostic? CarriedType(Location location, string? callingAssembly, ITypeSymbol? declaredType)
    {
        // The carried-type lens has no exception: the approved reach is an INVOCATION, and a type a declaration
        // carries is never that. SystemServices declared as a field, property, parameter, return, or local type is
        // therefore reported here exactly like any other Engine internal.
        return TypeTree.Any(declaredType, IsEngineInternalType)
            ? Diagnostic.Create(Rule, location, callingAssembly, TypeTree.Describe(declaredType))
            : null;
    }

    private static void Report(SyntaxNodeAnalysisContext context, ISymbol reached)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Rule, context.Node.GetLocation(), context.Compilation.AssemblyName, reached.ToDisplayString()));
    }

    // The container type is permitted in exactly one written position: the receiver of an invocation of its own static
    // Create method, however the source spells the path to it (SystemServices.Create() or the fully qualified form).
    private static bool IsApprovedContainerReceiver(SyntaxNodeAnalysisContext context, ITypeSymbol referenced)
    {
        if (!CompositionPoint.IsContainerFactoryType(referenced as INamedTypeSymbol))
        {
            return false;
        }

        ExpressionSyntax reference = OutermostReference(context.Node);
        return reference.Parent is MemberAccessExpressionSyntax access
            && access.Expression == reference
            && access.Parent is InvocationExpressionSyntax invocation
            && invocation.Expression == access
            && IsApprovedCreateCall(context, invocation);
    }

    // The Create member itself is permitted only as the invoked member of that same approved call: a method-group
    // reference or a nameof of it is a reach the grant does not cover.
    private static bool IsApprovedCreateMember(SyntaxNodeAnalysisContext context, ISymbol symbol)
    {
        if (!CompositionPoint.IsContainerFactoryMethod(symbol))
        {
            return false;
        }

        ExpressionSyntax reference = OutermostReference(context.Node);
        return reference.Parent is InvocationExpressionSyntax invocation
            && invocation.Expression == reference
            && IsApprovedCreateCall(context, invocation);
    }

    private static bool IsApprovedCreateCall(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        return CompositionPoint.IsContainerFactoryMethod(
            context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol);
    }

    // Climbs from a written name to the outermost expression that still denotes the SAME symbol: the name is the
    // right-hand side of each member access it climbs through, so `AgentGuard.Engine.SystemServices` is reached from
    // the `SystemServices` node and the fully qualified call is recognised exactly like the short one.
    private static ExpressionSyntax OutermostReference(SyntaxNode node)
    {
        SyntaxNode current = node;
        while (current.Parent is MemberAccessExpressionSyntax parent && parent.Name == current)
        {
            current = parent;
        }

        return (ExpressionSyntax)current;
    }

    private static bool IsEngineInternalMember(ISymbol symbol)
    {
        return symbol is IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol
            && IsDeclaredInEngine(symbol)
            && !IsPubliclyVisible(symbol);
    }

    private static bool IsDeclaredInEngine(ISymbol symbol)
    {
        return string.Equals(symbol.ContainingAssembly?.Name, EngineAssembly.Name, StringComparison.Ordinal);
    }

    // Effective, not declared, accessibility: a public member of an internal type is internal, so the containing-type
    // chain is walked. Anything less than public (internal, protected, private) is out of reach from the two gated
    // consumers, so this is the fail-closed reading of "every public Engine API stays reachable".
    private static bool IsPubliclyVisible(ISymbol symbol)
    {
        for (ISymbol? current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }
}
