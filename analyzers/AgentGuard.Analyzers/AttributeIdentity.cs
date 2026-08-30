// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AgentGuard.Analyzers;

/// <summary>
/// Matches an applied attribute against a set of (namespace, name) identities by its RESOLVED type, falling back to
/// the attribute's SYNTACTIC name only when the type does not resolve. The two attribute rules that used to match on
/// the bare syntactic spelling — the native-interop location ban (AG0008, <c>[DllImport]</c>/<c>[LibraryImport]</c>)
/// and the native-callback body guard (AG0105, <c>[UnmanagedCallersOnly]</c>) — share this one resolver so a
/// user-declared attribute of the same simple name in a different namespace is not mistaken for the BCL attribute.
/// <para>
/// Resolve-first is correct for a real same-named user attribute in another namespace: the attribute resolves to the
/// user's type, <see cref="WellKnownType.IsAnyOf"/> reports it is not the BCL type, and nothing fires. Resolution has
/// two tiers because <c>GetSymbolInfo</c> binds the attribute's CONSTRUCTOR: a malformed usage — a required
/// constructor argument omitted, on the BCL attribute or on a same-named user one — fails overload resolution and
/// yields no symbol even though the attribute's TYPE still binds, and <c>GetTypeInfo</c> recovers that type. The
/// syntactic fallback runs ONLY when neither tier binds a type — a genuinely undefined attribute identifier (no such
/// type in scope, a missing reference or using) — where matching the simple name is the fail-closed choice.
/// </para>
/// This reuses the two existing owners rather than re-deriving either: <see cref="WellKnownType.IsAnyOf"/> owns the
/// resolved (namespace, name) identity match, and <see cref="AttributeSyntaxName"/> owns the syntactic simple-name
/// extraction and its <c>…Attribute</c> normalization.
/// </summary>
internal static class AttributeIdentity
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="attribute"/> is any of the given (namespace, name) attribute
    /// identities — matched by resolved type when the type binds, and by syntactic name only when it does not.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the tree the attribute is in.</param>
    /// <param name="attribute">The applied attribute syntax to test.</param>
    /// <param name="candidates">The (namespace, name) identities any of which the attribute may be.</param>
    /// <param name="cancellationToken">A token to observe while resolving the attribute's type.</param>
    /// <returns><see langword="true"/> when the attribute matches one of the candidate identities.</returns>
    internal static bool IsAnyOf(
        SemanticModel semanticModel,
        AttributeSyntax attribute,
        ImmutableArray<(string Namespace, string Name)> candidates,
        CancellationToken cancellationToken)
    {
        INamedTypeSymbol? resolved = ResolveAttributeType(semanticModel, attribute, cancellationToken);
        if (resolved is not null)
        {
            return WellKnownType.IsAnyOf(resolved, candidates);
        }

        return MatchesSyntactically(attribute, candidates);
    }

    // Resolves the attribute's applied type in two tiers: the constructor's containing type (GetSymbolInfo), then the
    // attribute's own type info (GetTypeInfo) when the constructor does not bind — which happens when a required
    // constructor argument is omitted and overload resolution fails, yet the attribute TYPE still binds. Returns null
    // only when neither tier yields a non-error named type (a genuinely undefined identifier — no such type in scope),
    // so the syntactic fallback runs solely for that unresolved case.
    private static INamedTypeSymbol? ResolveAttributeType(
        SemanticModel semanticModel, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is IMethodSymbol constructor
            && constructor.ContainingType is { TypeKind: not TypeKind.Error } fromConstructor)
        {
            return fromConstructor;
        }

        return semanticModel.GetTypeInfo(attribute, cancellationToken).Type is INamedTypeSymbol { TypeKind: not TypeKind.Error } fromTypeInfo
            ? fromTypeInfo
            : null;
    }

    // The fail-closed fallback: with no resolved type, compare the attribute's syntactic simple name — normalized to
    // its …Attribute spelling by the AttributeSyntaxName owner — against each candidate's name. The namespace cannot
    // be checked syntactically, so the simple-name match is the most that can be asserted, and it fires only here.
    private static bool MatchesSyntactically(
        AttributeSyntax attribute, ImmutableArray<(string Namespace, string Name)> candidates)
    {
        string appliedName = AttributeSyntaxName.FullName(AttributeSyntaxName.SimpleName(attribute.Name));
        foreach ((string _, string name) in candidates)
        {
            if (string.Equals(appliedName, AttributeSyntaxName.FullName(name), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
