// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// A recursive walk over a type's tree — its generic type arguments, array element types, and pointer targets —
/// for the rules that must look for a particular kind of type anywhere inside a signature type. The null- and
/// cycle-guarded recursion lives here once; each caller passes the leaf test it is looking for, so no rule
/// re-spells the walk.
/// <para>
/// <see cref="Describe"/> is the companion reader: the same rules that ask
/// <see cref="Any(ITypeSymbol?, Func{INamedTypeSymbol, bool})"/> about a tree then name
/// that tree in a diagnostic message, and they name it the same way — fully qualified, so a guarded type nested
/// inside a generic argument, an array element, or a pointer target is visible in the message rather than hidden
/// behind the outer type's name, with one spelling for the type that could not be resolved. Both access rules that
/// report a carried or written type — AG0041 and the shared <see cref="OneDoorRule"/> body behind AG0040, AG0023 and
/// AG0029 — call it, so the text is not spelled per rule. <see cref="MemberUseScanner.Describe"/> is the parallel
/// reader for a member USE, which is a different subject and keeps its own owner.
/// </para>
/// </summary>
internal static class TypeTree
{
    /// <summary>
    /// Gets a value indicating whether any named type reachable from <paramref name="root"/> — through generic
    /// type arguments, array element types, and pointer targets — satisfies <paramref name="match"/>.
    /// </summary>
    /// <param name="root">The type whose tree to walk; a <see langword="null"/> root yields <see langword="false"/>.</param>
    /// <param name="match">The leaf test applied to each named type reached in the tree.</param>
    /// <returns><see langword="true"/> when some named type in the tree satisfies <paramref name="match"/>.</returns>
    internal static bool Any(ITypeSymbol? root, Func<INamedTypeSymbol, bool> match)
    {
        return Any(root, match, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
    }

    /// <summary>
    /// Builds the display text of <paramref name="root"/> for a diagnostic message: the fully qualified spelling of
    /// the whole tree, so a guarded type reached through a generic argument, an array element, or a pointer target is
    /// named in the message instead of being hidden behind the outer type's name. A <see langword="null"/> root — a
    /// declaration whose type did not resolve — is spelled <c>?</c> rather than dropping the subject from the message.
    /// </summary>
    /// <param name="root">The type to name; <see langword="null"/> when none resolved.</param>
    /// <returns>The display text for the diagnostic message.</returns>
    internal static string Describe(ITypeSymbol? root)
    {
        return root is null ? "?" : root.ToDisplayString();
    }

    private static bool Any(ITypeSymbol? type, Func<INamedTypeSymbol, bool> match, HashSet<ITypeSymbol> visited)
    {
        if (type is null || !visited.Add(type))
        {
            return false;
        }

        return type switch
        {
            INamedTypeSymbol named => match(named)
                || named.TypeArguments.Any(argument => Any(argument, match, visited)),
            IArrayTypeSymbol array => Any(array.ElementType, match, visited),
            IPointerTypeSymbol pointer => Any(pointer.PointedAtType, match, visited),
            _ => false,
        };
    }
}
