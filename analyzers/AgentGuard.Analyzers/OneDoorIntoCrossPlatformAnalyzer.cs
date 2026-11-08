// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a reach from <c>AgentGuard.Boundaries</c> or <c>AgentGuard.Engine</c> into the core
/// <c>AgentGuard.CrossPlatform</c> assembly that is not the one adapter-factory. <c>AgentGuard.CrossPlatform</c>
/// exposes exactly ONE factory that produces its owned adapters (the file-op adapters and <c>GuidFactory</c> that live
/// there per owners-live-at-lowest-consumer), and <c>SystemServices.Create()</c> calls only that one function; the
/// adapters stay <c>internal</c> with private constructors and an <c>InternalsVisibleTo</c> grant lets the consumer
/// reach the factory, so this rule pins both consumers to that single entry point. The factory type name is
/// established by this rule (Rule-Driven Development): the <c>CrossPlatformAdapters</c> type in the
/// <c>AgentGuard.CrossPlatform</c> namespace. The per-OS <c>.MacOS</c>/<c>.Linux</c>/<c>.Windows</c> assemblies are
/// AG0029's door, not this one; <c>IPlatformFileSystem</c>/<c>IPlatformServices</c> live in
/// <c>AgentGuard.Abstractions</c>, a different assembly, so they are never caught here.
/// <para>
/// The two gates are deliberately different. The <c>AgentGuard.Boundaries</c> gate is UNCHANGED: member access only,
/// door only, no caller constraint, so nothing outside Engine is loosened. The <c>AgentGuard.Engine</c> gate adds the
/// caller constraint — the door may be reached only from <c>AgentGuard.Engine.SystemServices.Create()</c> — and adds
/// TYPE-reference coverage, because a class that merely HOLDS or NAMES a CrossPlatform-core type has reached it just
/// as surely as one that calls a member on it. That coverage is the two shared lenses:
/// <see cref="WrittenNameScanner"/> for every name the source writes, and <see cref="DeclaredTypeScanner"/> for the
/// types a declaration carries without writing them.
/// </para>
/// <para>
/// The Engine gate also widens the target filter to the shared <c>AgentGuard.CrossPlatform</c> NAMESPACE, so a
/// same-named decoy squatting it from a foreign assembly is reported rather than silently accepted. That namespace is
/// shared with the three per-OS implementation assemblies, which the widening excludes so the legitimate
/// <c>PlatformServices.Create()</c> call stays AG0029's business. AG0029 admits the same foreign squatters beside its
/// own per-OS types, because neither rule may be silent about an imitation of its own door; both rules reporting one
/// imitation is the intended overlap, not a defect.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneDoorIntoCrossPlatformAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0023";

    private const string Category = "AgentGuard.Architecture";

    // The one allowed door into the core AgentGuard.CrossPlatform assembly: the adapter-factory type. Its name is
    // established by this rule (Rule-Driven Development — the rules are written first and the implementation conforms),
    // mirroring how SystemServices (Engine) and PlatformServices (per-OS) are named. Matched by full type identity.
    private const string AdapterFactoryTypeName = "CrossPlatformAdapters";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "The core CrossPlatform assembly is reached only through the one adapter-factory, and from Engine only inside SystemServices.Create()",
        messageFormat: "Reach from '{0}' into the core AgentGuard.CrossPlatform assembly is permitted only through the one adapter-factory " + AdapterFactoryTypeName + ", and from AgentGuard.Engine only inside " + CompositionPoint.ContainerFactoryDescription + ": {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A reach into the core AgentGuard.CrossPlatform assembly is legal only through the one adapter-factory (the CrossPlatformAdapters type), which produces the file-op adapters and GuidFactory that live in CrossPlatform. The adapters stay internal with private constructors. From AgentGuard.Boundaries the door may be called from anywhere in that assembly. From AgentGuard.Engine the door may be reached only inside AgentGuard.Engine.SystemServices.Create(), and a written or carried TYPE reference to a CrossPlatform-core type counts as a reach just as a member access does.");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules = ImmutableArray.Create(Rule);

    // Cached so no delegate is allocated per analyzed operation, node, or symbol (the shape CrossPlatformBoundary
    // establishes). The BOUNDARIES gate's filter, unchanged: assembly-only — it narrows every use down to
    // CrossPlatform-core types before the door is considered — so it cannot fold into WellKnownType.IsInAssembly,
    // which also pins a type name. The core-assembly comparison itself is CrossPlatformBoundary's, so it is not
    // re-spelled here.
    private static readonly Func<INamedTypeSymbol, bool> IsCrossPlatformCoreType =
        type => CrossPlatformBoundary.IsCoreAssembly(type.ContainingAssembly?.Name);

    // The ENGINE-facing filter: the core assembly, OR a foreign-assembly squatter of the shared
    // AgentGuard.CrossPlatform namespace, as the one owner of that partition decides. The squatter half closes the
    // spoofing hole — a decoy that squats the guarded namespace from a foreign assembly lands in scope and then FAILS
    // the door's namespace-plus-name-plus-assembly conjunction, rather than falling out of scope and being silently
    // accepted, in a call, a written name, or a carried type alike. The per-OS assemblies are excluded there, so the
    // legitimate PlatformServices.Create() call inside SystemServices.Create() stays AG0029's business and is not
    // reported here.
    private static readonly Func<INamedTypeSymbol, bool> IsCrossPlatformCoreTypeFromEngine =
        type => IsCrossPlatformCoreType(type) || CrossPlatformBoundary.IsSharedNamespaceDecoy(type);

    // The door TYPE, matched by identity AND assembly through the shared WellKnownType.IsInAssembly conjunction, so a
    // same-named decoy elsewhere cannot pose as the door. One owner for the member half and the type half.
    private static readonly Func<INamedTypeSymbol, bool> IsDoorType = type => WellKnownType.IsInAssembly(
        type, CrossPlatformBoundary.RootName, AdapterFactoryTypeName, CrossPlatformBoundary.RootName);

    // Any member reached ON the door type is the door; this rule has always keyed on the type, not on a method name.
    private static readonly Func<ISymbol, INamedTypeSymbol, bool> IsDoorMember = (_, type) => IsDoorType(type);

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
            context,
            Rule,
            IsCrossPlatformCoreType,
            IsCrossPlatformCoreTypeFromEngine,
            IsDoorMember,
            IsDoorType);
    }
}
