// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a reach from <c>AgentGuard.Boundaries</c> or <c>AgentGuard.Engine</c> into a per-OS implementation assembly
/// (<c>.MacOS</c>/<c>.Linux</c>/<c>.Windows</c>) that is not <c>PlatformServices.Create()</c>. The per-OS
/// <c>InternalsVisibleTo</c> grants otherwise expose every internal member to the consumer; AG0023 pins only the core
/// CrossPlatform assembly, so this rule pins the per-OS door. The one legal call is the static
/// <c>PlatformServices.Create()</c> that <c>SystemServices.Create()</c> uses to obtain the OS-divergent platform
/// container — the self-building factory the <c>container-is-one-class-with-its-own-create</c> mandate makes the one
/// door, whose identity lives once in <see cref="PlatformFactory"/>.
/// <para>
/// The two gates are deliberately different, on the same terms as AG0023. The <c>AgentGuard.Boundaries</c> gate is
/// UNCHANGED: member access only, door only, no caller constraint, so nothing outside Engine is loosened. The
/// <c>AgentGuard.Engine</c> gate adds the caller constraint — the door may be reached only from
/// <c>AgentGuard.Engine.SystemServices.Create()</c> — and adds TYPE-reference coverage through the two shared lenses,
/// <see cref="WrittenNameScanner"/> and <see cref="DeclaredTypeScanner"/>.
/// </para>
/// <para>
/// The Engine gate also widens the target filter to the shared <c>AgentGuard.CrossPlatform</c> NAMESPACE, so a
/// same-named decoy squatting it from a foreign assembly is reported by THIS rule rather than left to a sibling. The
/// widening is paired with the door test below: because <see cref="PlatformFactory"/> matches
/// <c>PlatformServices</c> on namespace plus name alone — the type is declared in all three per-OS assemblies under
/// the same namespace — this rule's door test conjoins that identity with the per-OS assembly set, so an admitted
/// decoy FAILS the door instead of passing it. AG0023 admits the same decoy beside its own core-assembly types; both
/// rules reporting one imitation is the intended overlap, not a defect.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneDoorIntoPerOsAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0029";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A per-OS assembly is reached only through PlatformServices.Create(), and from Engine only inside SystemServices.Create()",
        messageFormat: "Reach from '{0}' into a per-OS implementation assembly is permitted only through the one door PlatformServices.Create(), and from AgentGuard.Engine only inside " + CompositionPoint.ContainerFactoryDescription + ": {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A reach into a per-OS implementation assembly (.MacOS/.Linux/.Windows) is legal only as PlatformServices.Create(), the static factory that hands back the OS-divergent platform container. The per-OS InternalsVisibleTo grants otherwise expose every internal member. From AgentGuard.Boundaries the door may be called from anywhere in that assembly. From AgentGuard.Engine the door may be reached only inside AgentGuard.Engine.SystemServices.Create(), and a written or carried TYPE reference to a per-OS type counts as a reach just as a member access does.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    // Cached so no delegate is allocated per analyzed operation, node, or symbol. The BOUNDARIES gate's filter,
    // unchanged: the per-OS assembly set, owned once by CrossPlatformBoundary; a reach into the core CrossPlatform
    // assembly is AG0023's.
    private static readonly Func<INamedTypeSymbol, bool> IsPerOsType =
        type => CrossPlatformBoundary.IsPerOsImplementationAssembly(type.ContainingAssembly?.Name);

    // The ENGINE-facing filter: a genuine per-OS assembly, OR a foreign-assembly squatter of the shared
    // AgentGuard.CrossPlatform namespace, as the one owner of that partition decides. The squatter half is what makes
    // THIS rule reject a wrong-assembly imitation of its OWN door: the decoy lands in scope and then fails the
    // assembly-pinned door test below, rather than falling out of scope and being silently accepted at the one site
    // that may call the real door. The core assembly is excluded there, so the legitimate CrossPlatformAdapters call
    // inside SystemServices.Create() stays AG0023's business and is not reported here.
    private static readonly Func<INamedTypeSymbol, bool> IsPerOsTypeFromEngine =
        type => IsPerOsType(type) || CrossPlatformBoundary.IsSharedNamespaceDecoy(type);

    // The door TYPE, matched as a conjunction of the two existing owners: the PlatformServices identity that
    // PlatformFactory owns, AND the per-OS assembly set that CrossPlatformBoundary owns. PlatformFactory matches on
    // namespace plus name alone — deliberately, because PlatformServices is declared in each of the three per-OS
    // assemblies under the shared namespace — so the assembly half of the door's identity is supplied here, by the
    // same predicate the Boundaries gate filters on. On the Boundaries gate the conjunct is already guaranteed by that
    // gate's filter, so the door behaves there exactly as it always has; on the Engine gate, where the filter now
    // admits foreign squatters, it is what rejects them. One owner for the member half and the type half.
    private static readonly Func<INamedTypeSymbol, bool> IsDoorType =
        type => IsPerOsType(type) && PlatformFactory.IsFactoryType(type);

    private static readonly Func<ISymbol, INamedTypeSymbol, bool> IsDoorMember =
        (member, type) => IsPerOsType(type) && PlatformFactory.Is(member, type);

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

        OneDoorRule.RegisterBoundariesAndEngineGates(
            context, Rule, IsPerOsType, IsPerOsTypeFromEngine, IsDoorMember, IsDoorType);
    }
}
