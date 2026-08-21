// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a second named type in the same compilation that implements one of the CONTAINER-shaped interfaces — the
/// root <c>ISystemServices</c> and every nested container it exposes (<c>IFileSystem</c>, <c>IPlatformServices</c>, and
/// any future one), DERIVED from the container tree rather than a hand-maintained triple
/// (container-interface-single-owner-rule). Each container has exactly one implementer per compilation; a second is a
/// build error. Unlike the leaf-service one-owner rule (AG0025), this has NO test-system exemption: in a test
/// compilation the only legal container implementers are the fake container types nested inside
/// <c>SystemServicesBuilder</c>, so a test hand-rolling <c>class Fake : ISystemServices</c> (or <c>: IFileSystem</c> /
/// <c>: IPlatformServices</c>) and building a container directly — sidestepping the builder — is caught here. That
/// bypass is uncaught by anything else: AG0025 covers only the leaf services and exempts test assemblies, and AG0017
/// pins only the static factory, not a direct interface implementation. The container-interface set comes from the ONE
/// container walk <see cref="BoundaryServices.ResolveTree"/> performs (via
/// <see cref="BoundaryServices.ContainerInterfaces"/>), the same shared walk AG0019 and AG0034 read, so a future nested
/// container is guarded automatically with no edit here. The accumulate-and-report-second-implementer algorithm is the
/// one shared with AG0025 through <see cref="SecondImplementerGuard"/>. Every named-type kind counts (class, struct,
/// record, record struct), because the container implementer could be any of them; only source types in the compilation
/// are considered (<see cref="SymbolKind.NamedType"/> excludes referenced-assembly types), so a referenced assembly's
/// own single implementer never counts against a second one here. It passes on the current well-formed tree (one
/// implementer of each container per compilation), so it is preventive today.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContainerInterfaceSingleOwnerAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0022";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A container interface may be implemented by at most one type per compilation, with no test exemption",
        messageFormat: "Type '{0}' is a second implementer of container interface '{1}'; each container has exactly one implementer per compilation (in a test, only the fake container nested inside SystemServicesBuilder), so route through the builder instead of implementing the container directly",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "At most one type per compilation may implement a container interface (ISystemServices, IFileSystem, IPlatformServices, and any future nested container, derived from the container tree). A second implementer of any named-type kind is a build error, with NO test-system exemption: in a test compilation the only legal container implementers are the fake container types nested inside SystemServicesBuilder, so a test hand-rolling a container implementation to sidestep the builder is caught here — the one bypass AG0025 (leaf services, test-exempt) and AG0017 (static factory) do not cover.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Derive the container-interface set from the ONE container walk (BoundaryServices.ResolveTree), then delegate
        // to the shared second-implementer accumulation — the identical algorithm AG0025 uses, but with NO test
        // exemption (AG0025 returns early in a test compilation; this rule never does). When the container is not in
        // scope the derived set is empty, so the shared guard is inert.
        ContainerNode? root = BoundaryServices.ResolveTree(context.Compilation);
        ImmutableArray<(string Namespace, string Name)> containers = root is null
            ? ImmutableArray<(string Namespace, string Name)>.Empty
            : BoundaryServices.ContainerInterfaces(root);

        SecondImplementerGuard.Register(context, containers, Rule);
    }
}
