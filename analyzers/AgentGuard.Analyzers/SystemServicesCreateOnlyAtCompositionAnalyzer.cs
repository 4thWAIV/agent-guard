// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a call to a static factory that hands back the <c>ISystemServices</c> container, made anywhere but the two
/// allowed composition points: the single <c>Program</c> composition method and the test
/// <c>SystemServicesBuilder</c>. That is the literal <c>SystemServices.Create()</c> AND any sibling static factory
/// whose RETURN TYPE is <c>ISystemServices</c> (a <c>CreateDefault()</c>/<c>Build()</c> returning the container would
/// otherwise bypass the single-construction-point rule by not being named <c>Create</c>). The container is built once
/// at the top and threaded down by constructor injection, so there is exactly one place to mock. This is the second
/// of the two walls that stop the container being reconstructed deep in the chain (the first is the adapters being
/// <c>internal</c> with private constructors): calling such a factory deep in the graph — instead of passing the
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
        // The allowed composition points (Program in AgentGuard.Cli, SystemServicesBuilder in AgentGuard.TestHelpers)
        // live in the shared CompositionPoint owner, which anchors on namespace + name AND assembly so a nominal
        // collision cannot self-grant.
        if (!IsContainerFactory(member, type) || CompositionPoint.Encloses(context.ContainingSymbol))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation()));
    }

    private static bool IsContainerFactory(ISymbol member, INamedTypeSymbol type)
    {
        // Pin the bypass two ways: the literal SystemServices.Create(), and — because a sibling static factory could
        // return the container under a different name (CreateDefault/Build) — ANY static method whose return type is
        // the ISystemServices container (ag0017-edit-factory-by-return-type). Both go through the one type-identity
        // owner WellKnownType.Is (namespace + name), never a bare name.
        return IsSystemServicesCreate(member, type) || IsStaticFactoryReturningContainer(member);
    }

    private static bool IsSystemServicesCreate(ISymbol member, INamedTypeSymbol type)
    {
        // The static Create() on AgentGuard.Boundaries.SystemServices, matched by full type identity, not a bare
        // name-plus-assembly match.
        return member is IMethodSymbol { IsStatic: true }
            && string.Equals(member.Name, FactoryMethodName, StringComparison.Ordinal)
            && WellKnownType.Is(type, BoundaryAssembly.Name, FactoryTypeName);
    }

    private static bool IsStaticFactoryReturningContainer(ISymbol member)
    {
        // Any static method that returns AgentGuard.Abstractions.Contracts.ISystemServices, regardless of its name or declaring
        // type — the return-type pin that stops a differently-named sibling factory from bypassing the wall.
        return member is IMethodSymbol { IsStatic: true } method
            && WellKnownType.Is(
                method.ReturnType as INamedTypeSymbol, KnownNamespaces.AgentGuardAbstractionsContracts, BoundaryServices.ContainerName);
    }
}
