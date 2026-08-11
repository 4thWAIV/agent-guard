// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a call to <c>SystemServices.Create()</c> made anywhere but the two allowed composition points: the
/// single <c>Program</c> composition method and the test <c>SystemServicesBuilder</c>. The container is built once
/// at the top and threaded down by constructor injection, so there is exactly one place to mock. This is the second
/// of the two walls that stop the container being reconstructed deep in the chain (the first is the adapters being
/// <c>internal</c> with private constructors): calling <c>Create()</c> deep in the graph — instead of passing the
/// container through the constructors — is a build error, not a matter of discipline.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemServicesCreateOnlyAtCompositionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0017";

    private const string Category = "AgentGuard.Architecture";
    private const string FactoryTypeName = "SystemServices";
    private const string FactoryMethodName = "Create";
    private const string CompositionTypeName = "Program";
    private const string BuilderTypeName = "SystemServicesBuilder";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "SystemServices.Create must be called only at the single composition point",
        messageFormat: "SystemServices.Create() is called outside the single Program composition method and SystemServicesBuilder; receive ISystemServices by constructor injection instead of reconstructing it",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "SystemServices.Create() may be called only from the single Program composition method and the test SystemServicesBuilder. Everywhere else receives ISystemServices by constructor injection, so the container is built once and there is exactly one place to mock; reconstructing it deep in the graph is a build error.");

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
        context.RegisterCompilationStartAction(context => MemberUseScanner.Register(context, Inspect));
    }

    private static void Inspect(OperationAnalysisContext context, ISymbol member, INamedTypeSymbol type)
    {
        if (!IsSystemServicesCreate(member, type) || IsInsideAllowedCompositionPoint(context.ContainingSymbol))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation()));
    }

    private static bool IsSystemServicesCreate(ISymbol member, INamedTypeSymbol type)
    {
        return member is IMethodSymbol { IsStatic: true }
            && string.Equals(member.Name, FactoryMethodName, StringComparison.Ordinal)
            && string.Equals(type.Name, FactoryTypeName, StringComparison.Ordinal)
            && type.ContainingAssembly is not null
            && string.Equals(type.ContainingAssembly.Name, BoundaryAssembly.Name, StringComparison.Ordinal);
    }

    private static bool IsInsideAllowedCompositionPoint(ISymbol containingSymbol)
    {
        for (INamedTypeSymbol? enclosing = containingSymbol as INamedTypeSymbol ?? containingSymbol.ContainingType;
             enclosing is not null;
             enclosing = enclosing.ContainingType)
        {
            if (IsProgramInCli(enclosing) || IsBuilderInTestHelpers(enclosing))
            {
                return true;
            }
        }

        return false;
    }

    // The exemption is bound to assembly identity like every sibling gate: a type merely named 'Program' or
    // 'SystemServicesBuilder' in any other assembly cannot self-grant the right to reconstruct the container.
    private static bool IsProgramInCli(INamedTypeSymbol enclosing)
    {
        return string.Equals(enclosing.Name, CompositionTypeName, StringComparison.Ordinal)
            && string.Equals(enclosing.ContainingAssembly?.Name, CliAssembly.Name, StringComparison.Ordinal);
    }

    private static bool IsBuilderInTestHelpers(INamedTypeSymbol enclosing)
    {
        return string.Equals(enclosing.Name, BuilderTypeName, StringComparison.Ordinal)
            && string.Equals(enclosing.ContainingAssembly?.Name, TestAssembly.TestHelpersName, StringComparison.Ordinal);
    }
}
