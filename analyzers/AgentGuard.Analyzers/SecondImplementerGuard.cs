// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The one shared "at most N implementers per candidate interface" accumulation, extracted so the single-owner and
/// single-implementer rules do not each spell it out: the owned-leaf-service rule (AG0025,
/// <see cref="OneOwnerPerInterfaceAnalyzer"/>), the container-interface rule (AG0022,
/// <see cref="ContainerInterfaceSingleOwnerAnalyzer"/>), and the leaf/platform test-implementer rule (AG0030,
/// <see cref="LeafPlatformSingleImplementerAnalyzer"/>). It accumulates, per candidate interface, every named type in
/// the compilation that implements it — routed through the same <see cref="OwnerClass.Implements"/>
/// (<see cref="WellKnownType.IsAnyOf"/> over <see cref="ITypeSymbol.AllInterfaces"/>) every boundary rule uses, never a
/// bespoke membership walk — then reports the implementers past the permitted count of each at compilation end, ordered
/// by <see cref="ISymbol.ToDisplayString"/> in <see cref="System.StringComparer.Ordinal"/> so the reported set is stable
/// under concurrent symbol actions. Each caller supplies its own candidate <c>candidateInterfaces</c> set, its own
/// <c>rule</c> descriptor, and how many implementers it permits before reporting: AG0025 and AG0022 permit ONE (the one
/// owner) and report the second-and-later — AG0025 passes the derived leaf-service set
/// (<see cref="DerivedServices.ServiceInterfaces"/>) AFTER its own test-system early-return, and AG0022 passes the
/// container-interface set derived from the tree, with NO test exemption. AG0030 permits ZERO (the sanctioned
/// implementers live in <c>AgentGuard.TestHelpers</c>, referenced not source) and so reports the FIRST-and-later source
/// implementer in a <c>.Tests</c> compilation. All descriptors share the same two-arg message shape — <c>{0}</c> the
/// reported implementer's name, <c>{1}</c> the interface's full name.
/// </summary>
internal static class SecondImplementerGuard
{
    /// <summary>
    /// Registers the per-compilation accumulation and the compilation-end report against
    /// <paramref name="candidateInterfaces"/>. A per-compilation accumulator is created here, so nothing leaks across
    /// compilations; the symbol action runs concurrently, so the accumulator is thread-safe. An empty candidate set is
    /// inert — no type ever matches — so a compilation without the interfaces in scope reports nothing.
    /// </summary>
    /// <param name="context">The compilation-start context to register the symbol and end actions on.</param>
    /// <param name="candidateInterfaces">The (namespace, name) identities whose implementers past
    /// <paramref name="permittedImplementers"/> are a build error.</param>
    /// <param name="rule">The diagnostic descriptor to report — its message takes {0} the reported implementer's name
    /// and {1} the interface's full name.</param>
    /// <param name="permittedImplementers">How many implementers per interface are permitted before reporting: 1 for the
    /// single-owner rules (report the second-and-later), 0 for the leaf/platform test rule (report the first-and-later).
    /// Defaults to 1 so the single-owner callers are unchanged.</param>
    internal static void Register(
        CompilationStartAnalysisContext context,
        ImmutableArray<(string Namespace, string Name)> candidateInterfaces,
        DiagnosticDescriptor rule,
        int permittedImplementers = 1)
    {
        var implementersByInterface =
            new ConcurrentDictionary<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>>();

        context.RegisterSymbolAction(
            symbolContext =>
                Accumulate((INamedTypeSymbol)symbolContext.Symbol, candidateInterfaces, implementersByInterface),
            SymbolKind.NamedType);
        context.RegisterCompilationEndAction(
            endContext => ReportExcessImplementers(endContext, implementersByInterface, rule, permittedImplementers));
    }

    private static void Accumulate(
        INamedTypeSymbol type,
        ImmutableArray<(string Namespace, string Name)> candidateInterfaces,
        ConcurrentDictionary<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>> implementersByInterface)
    {
        // Route the "does this type implement the candidate interface" decision through the one shared
        // OwnerClass.Implements (WellKnownType.IsAnyOf over AllInterfaces), scoped to each single candidate interface —
        // never a bespoke re-derivation of the membership walk. No TypeKind gate: any named-type kind that implements
        // the candidate counts, exactly as the boundary rules accept it as the exempt owner.
        foreach ((string Namespace, string Name) candidate in candidateInterfaces
            .Where(candidate => OwnerClass.Implements(type, ImmutableArray.Create(candidate))))
        {
            implementersByInterface.GetOrAdd(candidate, _ => new ConcurrentBag<INamedTypeSymbol>()).Add(type);
        }
    }

    private static void ReportExcessImplementers(
        CompilationAnalysisContext context,
        ConcurrentDictionary<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>> implementersByInterface,
        DiagnosticDescriptor rule,
        int permittedImplementers)
    {
        foreach (KeyValuePair<(string Namespace, string Name), ConcurrentBag<INamedTypeSymbol>> entry in
            implementersByInterface)
        {
            // The bag's order is nondeterministic under concurrency, so order the implementers to make the reported set
            // stable, then skip the permitted count and flag the rest: the single-owner rules permit one (flag the
            // second and later); the leaf/platform test rule permits zero (flag the first and later).
            foreach (INamedTypeSymbol excess in entry.Value
                .OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal)
                .Skip(permittedImplementers))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    rule, excess.Locations[0], excess.Name, entry.Key.Namespace + "." + entry.Key.Name));
            }
        }
    }
}
