// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// Reports a second named type in the same compilation that implements one of the owned boundary interfaces. Each of
/// those interfaces has exactly one owner class; a second implementer — even one that structurally satisfies
/// <c>OwnerClass.Implements</c> — is a build error (ag0025-one-owner-per-interface). This closes the trick of adding
/// <c>: IFileReader</c> with stub members to an inconvenient type to launder a raw call past the single-owner
/// exemption of the boundary rules: the second implementer is flagged here regardless of whether it would have been
/// exempted. Every named-type kind counts — class, struct, record, and record struct — because the exemption predicate
/// AG0025 polices (<c>OwnerClass.Implements</c>) has no <see cref="TypeKind"/> gate, so a struct declaring
/// <c>: IFileReader</c> would otherwise launder a raw call yet be invisible to this scan. The owner-interface set is
/// the one derived from <c>ISystemServices</c> by <see cref="BoundaryServices.Resolve"/>, captured once per
/// compilation. Only source types in the compilation are considered — <see cref="SymbolKind.NamedType"/> visits every
/// declared named type (nested included) and excludes referenced-assembly types, so a referenced assembly's own single
/// implementer never counts against a second one here.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneOwnerPerInterfaceAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The diagnostic identifier reported by this analyzer.
    /// </summary>
    public const string DiagnosticId = "AG0025";

    private const string Category = "AgentGuard.Architecture";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "An owner interface may be implemented by at most one type per compilation",
        messageFormat: "Type '{0}' is a second implementer of owner interface '{1}'; each owned boundary interface has exactly one owner class, so route through that one owner instead of adding a second implementer",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "At most one type per compilation may implement a given owner interface (the owned boundary services derived from ISystemServices). A second implementer of any named-type kind — class, struct, record, or record struct — is a build error even if it structurally satisfies the single-owner exemption, closing the trick of adding ': IFileReader' with stub members to an inconvenient type to launder a raw call. There is exactly one owner; route through it.",
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
        // One-owner-per-interface is a SHIPPING-code invariant. The test system (test-system decision) deliberately
        // ships BOTH a fake (e.g. InMemoryFileSystem : IFileReader) AND a Wrap proxy base (e.g. RecordingFileReader :
        // IFileReader) per service in AgentGuard.TestHelpers — two legitimate implementers of the same owner interface
        // in one compilation — and the test projects reference them, so the test/TestHelpers assemblies are not gated.
        // The "TestHelpers OR .Tests" predicate is the one owned by TestAssembly, shared with NoCoverageOptOutAnalyzer.
        if (TestAssembly.IsTestSystemAssembly(context.Compilation))
        {
            return;
        }

        // Derive the owner-interface set from ISystemServices once per compilation, then capture it for the per-symbol
        // accumulation — no hand-maintained list (derive-service-set-from-isystemservices).
        DerivedServices services = BoundaryServices.Resolve(context.Compilation);

        // Accumulate, per owner interface, every named type in THIS compilation that implements it, then report the
        // second-and-later implementers at compilation end. The symbol action runs concurrently, so the per-compilation
        // accumulator is thread-safe; each compilation-start callback gets its own, so nothing leaks across
        // compilations. SymbolKind.NamedType visits every declared named type — nested included — and excludes
        // referenced-assembly types, so a referenced assembly's legitimate single implementer never combines with a
        // source one to false-positive.
        var implementersByOwner =
            new ConcurrentDictionary<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>>();

        context.RegisterSymbolAction(
            symbolContext => Accumulate((INamedTypeSymbol)symbolContext.Symbol, services, implementersByOwner),
            SymbolKind.NamedType);
        context.RegisterCompilationEndAction(
            endContext => ReportSecondImplementers(endContext, implementersByOwner));
    }

    private static void Accumulate(
        INamedTypeSymbol type,
        DerivedServices services,
        ConcurrentDictionary<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>> implementersByOwner)
    {
        // Route the "does this type implement the owner interface" decision through the one shared
        // OwnerClass.Implements (WellKnownType.IsAnyOf over AllInterfaces) every boundary rule uses, scoped to this
        // single owner interface — never a bespoke re-derivation of the AllInterfaces membership walk. No TypeKind
        // gate: any named-type kind (class, struct, record, record struct) that implements the owner interface is an
        // implementer here, exactly as the boundary rules accept it as the exempt owner.
        foreach ((string Namespace, string Name) owner in services.ServiceInterfaces
            .Where(owner => OwnerClass.Implements(type, ImmutableArray.Create(owner))))
        {
            implementersByOwner.GetOrAdd(owner, _ => new ConcurrentBag<INamedTypeSymbol>()).Add(type);
        }
    }

    private static void ReportSecondImplementers(
        CompilationAnalysisContext context,
        ConcurrentDictionary<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>> implementersByOwner)
    {
        foreach (KeyValuePair<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>> entry in
            implementersByOwner)
        {
            // The bag's order is nondeterministic under concurrency, so order the implementers to make the reported set
            // stable: keep the first as the one owner and flag the second and later.
            foreach (INamedTypeSymbol secondOrLater in entry.Value
                .OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal)
                .Skip(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule, secondOrLater.Locations[0], secondOrLater.Name, entry.Key.Namespace + "." + entry.Key.Name));
            }
        }
    }
}
