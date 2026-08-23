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
