// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single resolver for an attribute's name from its <see cref="NameSyntax"/> — the rightmost simple identifier and
/// its normalization to the full <c>…Attribute</c> spelling. Extracted here (LESSON 1, DRY) so the two rules that match
/// an attribute syntactically — the native-interop location ban (AG0008, which recognizes <c>[DllImport]</c>/
/// <c>[LibraryImport]</c>) and the native-callback body guard (AG0105, which recognizes <c>[UnmanagedCallersOnly]</c>) —
/// share ONE copy of the <c>"Attribute"</c> suffix, the <see cref="SimpleName"/> extraction, and the
/// <see cref="FullName"/> normalization instead of each spelling them out. The recognition is pure syntax: given an
/// attribute name, what is its simple identifier and its full attribute-suffixed spelling; which attribute a rule cares
/// about is the caller's, applied on top.
/// </summary>
internal static class AttributeSyntaxName
{
    /// <summary>The suffix the C# compiler makes optional on an attribute reference (<c>[Foo]</c> resolves
    /// <c>FooAttribute</c>); normalized onto every simple name so a match compares full spellings.</summary>
    private const string AttributeSuffix = "Attribute";

    /// <summary>
    /// Gets the rightmost simple identifier of an attribute's name — the <c>Foo</c> of <c>[A.B.Foo]</c>,
    /// <c>[global::A.Foo]</c>, or a bare <c>[Foo]</c>. The syntactic name is the reliable signal even where the
    /// <c>[LibraryImport]</c> source generator leaves the attribute symbol unresolved.
    /// </summary>
    /// <param name="name">The attribute name syntax.</param>
    /// <returns>The rightmost identifier text.</returns>
    internal static string SimpleName(NameSyntax name)
    {
        return name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => name.ToString(),
        };
    }

    /// <summary>
    /// Gets the full attribute-suffixed spelling of a simple name — <c>Foo</c> becomes <c>FooAttribute</c>, while a name
    /// already ending in <c>Attribute</c> is returned unchanged — so a match against a known attribute's full name is
    /// insensitive to the optional suffix at the use site.
    /// </summary>
    /// <param name="simpleName">The simple identifier.</param>
    /// <returns>The identifier normalized to its <c>…Attribute</c> spelling.</returns>
    internal static string FullName(string simpleName)
    {
        return simpleName.EndsWith(AttributeSuffix, StringComparison.Ordinal)
            ? simpleName
            : simpleName + AttributeSuffix;
    }
}
