// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AgentGuard.Analyzers;

/// <summary>
/// The written-name lens: EVERY name the source writes, resolved through the semantic model to the symbol it binds to,
/// and handed to a rule's inspector. This is how an access rule reaches a guarded type or member in a position that
/// carries no operation and declares no type — <c>typeof</c>, <c>nameof</c>, a cast, an <c>is</c> or <c>case</c>
/// pattern, a generic type argument, a generic constraint, a base or interface list, an attribute argument, a using
/// alias, an array or pointer element, and every declaration position. Coverage is established by the LENS, not by a
/// list of syntax positions: every simple name in the tree is offered, so a position nobody enumerated is still seen.
/// <para>
/// Registering <see cref="SyntaxKind.IdentifierName"/> and <see cref="SyntaxKind.GenericName"/> is what makes that
/// true. A qualified name, an alias-qualified name, and a member access all END in one of those two, so the node that
/// denotes the type or member is always visited however the source spells the path to it; the leading segments bind to
/// namespaces, which a rule's leaf test ignores. Three rules share this one lens — AG0041, AG0023's Engine gate, and
/// AG0029's Engine gate — so it is its own owner rather than living inside any of them, and the shared resolution
/// mechanics are delegated to <see cref="SymbolResolution"/>.
/// </para>
/// <para>
/// The carried-type half — the types a declaration carries WITHOUT writing them out — is the separate
/// <see cref="DeclaredTypeScanner"/>: the two halves resolve different things from different inputs, one walking
/// syntax to resolve a written name and one reading an already-declared type off a symbol.
/// </para>
/// </summary>
internal static class WrittenNameScanner
{
    /// <summary>
    /// The two simple-name kinds every written type or member name ends in. A qualified name
    /// (<c>AgentGuard.Engine.SystemServices</c>), an alias-qualified name (<c>global::…</c>), and a member access
    /// (<c>SystemServices.Create</c>) each terminate in one of these, so visiting them visits every name.
    /// </summary>
    private static readonly ImmutableArray<SyntaxKind> NameKinds = ImmutableArray.Create(
        SyntaxKind.IdentifierName,
        SyntaxKind.GenericName);

    /// <summary>
    /// Registers the written-name scan for <paramref name="inspect"/> only when the compilation being analyzed is the
    /// assembly named <paramref name="assemblyName"/>; a compilation that is any other assembly registers nothing.
    /// This mirrors <see cref="MemberUseScanner.RegisterForAssembly"/> so the "gate to one assembly, then scan" setup
    /// is spelled the same way for both lenses.
    /// </summary>
    /// <param name="context">The compilation-start context to gate and register on.</param>
    /// <param name="assemblyName">The assembly name the compilation must match for the scan to be registered.</param>
    /// <param name="inspect">The rule's inspector, given the syntax-node context and the symbol the name binds to.</param>
    internal static void RegisterForAssembly(
        CompilationStartAnalysisContext context,
        string assemblyName,
        Action<SyntaxNodeAnalysisContext, ISymbol> inspect)
    {
        if (!string.Equals(context.Compilation.AssemblyName, assemblyName, StringComparison.Ordinal))
        {
            return;
        }

        context.RegisterSyntaxNodeAction(nodeContext => Dispatch(nodeContext, inspect), NameKinds);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="node"/> is a DOCUMENTATION reference — a name written inside an
    /// XML documentation <c>cref</c> — rather than application code. A <c>cref</c> is the one place a name may denote a
    /// guarded symbol without reaching it, so this lens does not report one and the existing documentation links stay
    /// unchanged.
    /// <para>
    /// The discriminator is a <see cref="CrefSyntax"/> ANCESTOR, deliberately, because the two obvious alternatives are
    /// wrong. <c>IsPartOfStructuredTrivia</c> is also true for a preprocessor-directive name such as
    /// <c>#if GUARDED</c> or <c>#pragma warning disable CA1822</c>, so it would exempt a construct no rule names. A
    /// parent-KIND test misses a generic cref such as <c>&lt;see cref="Cache{Guarded}"/&gt;</c>, whose name node sits
    /// under a type-argument list, and misses an operator's cref parameter list. Only the ancestor walk covers both.
    /// </para>
    /// <para>
    /// <see cref="MemberUseScanner"/> and <see cref="DeclaredTypeScanner"/> need no such exclusion: a cref produces no
    /// operation and declares no type, so neither lens ever sees one.
    /// </para>
    /// </summary>
    /// <param name="node">The written-name node to test.</param>
    /// <returns><see langword="true"/> when the name sits inside an XML documentation <c>cref</c>.</returns>
    internal static bool IsDocumentationReference(SyntaxNode node)
    {
        return node.FirstAncestorOrSelf<CrefSyntax>() is not null;
    }

    private static void Dispatch(SyntaxNodeAnalysisContext context, Action<SyntaxNodeAnalysisContext, ISymbol> inspect)
    {
        if (IsDocumentationReference(context.Node))
        {
            return;
        }

        // The bound symbol first — a type, a method, a property, a field, an event, or a namespace. When nothing binds
        // (an incomplete or ambiguous name) the type-info tier still recovers a type, so a guarded type is not missed
        // merely because the surrounding expression failed to bind.
        ISymbol? symbol =
            SymbolResolution.Symbol(context.SemanticModel, context.Node, context.CancellationToken)
            ?? SymbolResolution.NamedType(context.SemanticModel, context.Node, context.CancellationToken);

        if (symbol is not null)
        {
            inspect(context, symbol);
        }
    }
}
