// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The shared semantic-model mechanics for turning a written syntax node into the symbol it binds to. Two rules need
/// the same resolution and must not drift apart: <see cref="AttributeIdentity"/> resolves an APPLIED ATTRIBUTE to its
/// attribute type, and <see cref="WrittenNameScanner"/> resolves EVERY NAME the source writes to the type or member it
/// denotes. Only the mechanics live here; attribute-specific matching and the syntactic fallback policy stay in
/// <see cref="AttributeIdentity"/>, and each rule's own leaf test stays in that rule.
/// <para>
/// <see cref="NamedType"/> resolves in three tiers because <c>GetSymbolInfo</c> does not always hand back a type. On an
/// attribute node it binds the CONSTRUCTOR, so the constructor's containing type is tier one. On a plain type name it
/// binds the TYPE itself, which is tier two. A malformed usage — a required constructor argument omitted — fails
/// overload resolution and binds no symbol even though the type still binds, and <c>GetTypeInfo</c> recovers it in
/// tier three. A resolution that yields only an error type is treated as no resolution, so a caller's fail-closed
/// fallback runs for a genuinely undefined identifier rather than matching an error placeholder.
/// </para>
/// </summary>
internal static class SymbolResolution
{
    /// <summary>
    /// Resolves the symbol <paramref name="node"/> binds to — a type, a method, a property, a field, an event, a
    /// namespace, or a local — or <see langword="null"/> when nothing binds or only an error type does.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the tree the node is in.</param>
    /// <param name="node">The syntax node to resolve.</param>
    /// <param name="cancellationToken">A token to observe while resolving.</param>
    /// <returns>The bound symbol, or <see langword="null"/>.</returns>
    internal static ISymbol? Symbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        ISymbol? bound = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
        return bound is INamedTypeSymbol { TypeKind: TypeKind.Error } ? null : bound;
    }

    /// <summary>
    /// Resolves the symbol a syntax position sits DIRECTLY inside — the innermost enclosing symbol, so a position in a
    /// lambda body resolves to the lambda and a position in a local function resolves to that local function, not to
    /// the method that declares them. An analysis context's own <c>ContainingSymbol</c> is the enclosing method in all
    /// three cases, which would silently extend a method-level exemption to every lambda and local function nested in
    /// it; the access rules take the narrowest reading, so they ask here instead.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the tree the node is in; a missing model falls back.</param>
    /// <param name="node">The syntax node whose enclosing symbol is wanted.</param>
    /// <param name="fallback">The context's own containing symbol, used when no model is available.</param>
    /// <param name="cancellationToken">A token to observe while resolving.</param>
    /// <returns>The innermost enclosing symbol.</returns>
    internal static ISymbol? EnclosingSymbol(
        SemanticModel? semanticModel, SyntaxNode node, ISymbol? fallback, CancellationToken cancellationToken)
    {
        return semanticModel?.GetEnclosingSymbol(node.SpanStart, cancellationToken) ?? fallback;
    }

    /// <summary>
    /// Resolves the named type <paramref name="node"/> denotes: the containing type when the node binds a member (an
    /// attribute node binds its constructor), the type itself when the node binds a type, and the node's type info
    /// when neither binds. Returns <see langword="null"/> only when no tier yields a non-error named type.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the tree the node is in.</param>
    /// <param name="node">The syntax node to resolve.</param>
    /// <param name="cancellationToken">A token to observe while resolving.</param>
    /// <returns>The resolved named type, or <see langword="null"/>.</returns>
    internal static INamedTypeSymbol? NamedType(
        SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        ISymbol? bound = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;

        if (bound is IMethodSymbol method && method.ContainingType is { TypeKind: not TypeKind.Error } fromMember)
        {
            return fromMember;
        }

        if (bound is INamedTypeSymbol { TypeKind: not TypeKind.Error } fromSymbol)
        {
            return fromSymbol;
        }

        return semanticModel.GetTypeInfo(node, cancellationToken).Type is INamedTypeSymbol { TypeKind: not TypeKind.Error } fromTypeInfo
            ? fromTypeInfo
            : null;
    }
}
