// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a call from <c>AgentGuard.Engine</c> into the <c>AgentGuard.Boundaries</c> assembly that is not one of the
/// four permitted adapter factories, or that is one of them but is made from somewhere other than
/// <c>AgentGuard.Engine.SystemServices.Create()</c>. Engine holds the container and its composition factory, and
/// Boundaries holds the four adapters the container assembles; that one method is the single door between them.
/// <para>
/// The four permitted identities live in <see cref="BoundaryAdapterFactories"/> and are matched on assembly,
/// namespace, type name, method name, and staticness together, so a same-named type in another namespace or assembly
/// cannot pose as a door. A method's RETURN TYPE grants nothing: a fifth Boundaries factory that hands back a service
/// interface is rejected until the list names it, which is an explicit rule change. The permitted call site is the
/// method-level identity in <see cref="CompositionPoint.IsContainerFactoryMethod"/>, which is deliberately narrower
/// than the container TYPE: another method on <c>SystemServices</c>, another Engine class, and a lambda or local
/// function nested inside <c>Create()</c> are all reported.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EngineToBoundariesOneDoorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0040";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Engine may call Boundaries only through the four permitted adapter factories, and only from SystemServices.Create()",
        messageFormat: "Call from '{0}' into the AgentGuard.Boundaries assembly is permitted only as " + BoundaryAdapterFactories.Description + ", called from " + CompositionPoint.ContainerFactoryDescription + ": {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AgentGuard.Engine reaches AgentGuard.Boundaries through exactly one door: EnvironmentAdapter.Create(), ConsoleAdapter.Create(), Ed25519SignatureService.Create(), or BuildInfoReader.Create(), called from AgentGuard.Engine.SystemServices.Create() itself. Each identity is matched on assembly, namespace, type name, method name, and staticness together, and a return type grants nothing, so a fifth Boundaries factory is rejected until this rule names it. Any other Engine call into Boundaries, and any of the four made from another method, is a build error.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    // Cached so no delegate is allocated per analyzed operation (the shape CrossPlatformBoundary establishes). The
    // scope filter narrows every Engine member-use down to Boundaries before the door and the site are considered, so
    // it cannot fold into WellKnownType.IsInAssembly, which also pins a type name. It matches the Boundaries ASSEMBLY
    // or the Boundaries NAMESPACE — the same string — because a decoy that squats the namespace from another assembly
    // must land in scope and then FAIL the door's full assembly-plus-namespace conjunction, rather than fall out of
    // scope and be silently accepted. Boundaries owns that namespace alone, so nothing legitimate is drawn in, and the
    // namespace half needs no further qualification. The two older one-door rules share ONE namespace between them and
    // do NOT split it: each admits a foreign-assembly squatter of the shared AgentGuard.CrossPlatform namespace
    // (CrossPlatformBoundary.IsSharedNamespaceDecoy) beside its own guarded assemblies, and each then rejects the
    // imitation at its own assembly-pinned door test, so neither rule is silent about an imitation of its own door.
    // The exclusions inside that decoy test are what keep each rule off the other's real territory — the per-OS
    // assemblies for AG0023, the core assembly for AG0029 — so both legitimate calls inside Create() stay accepted.
    // One imitation reported by both rules is the permitted overlap, not a defect.
    private static readonly Func<INamedTypeSymbol, bool> IsBoundariesType =
        type => string.Equals(type.ContainingAssembly?.Name, BoundaryAssembly.Name, StringComparison.Ordinal)
            || WellKnownType.IsInNamespace(type, BoundaryAssembly.Name);

    private static readonly Func<ISymbol, INamedTypeSymbol, bool> IsPermittedFactory = BoundaryAdapterFactories.Is;

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

        // Gated to the AgentGuard.Engine compilation — the rule is about what Engine reaches into. Any other
        // compilation registers nothing, so the same call from elsewhere is not this rule's business.
        OneDoorRule.RegisterEngineCallGate(context, Rule, IsBoundariesType, IsPermittedFactory);
    }
}
