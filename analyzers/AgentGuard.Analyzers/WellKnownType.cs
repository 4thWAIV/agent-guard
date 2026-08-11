// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// Identifies a type by its containing namespace and simple name. This is used instead of
/// <see cref="Compilation.GetTypeByMetadataName(string)"/>, which returns <see langword="null"/> when a metadata
/// name is defined or forwarded across more than one referenced assembly (as several BCL types are), and would
/// silently disable a rule in a real multi-reference build.
/// </summary>
internal static class WellKnownType
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is the type named <paramref name="typeName"/> in
    /// the namespace <paramref name="containingNamespace"/>.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="containingNamespace">The fully qualified namespace the type must be declared in.</param>
    /// <param name="typeName">The simple name the type must have.</param>
    /// <returns><see langword="true"/> when the type matches both the namespace and the name.</returns>
    internal static bool Is(INamedTypeSymbol? type, string containingNamespace, string typeName)
    {
        return type is not null
            && string.Equals(type.Name, typeName, StringComparison.Ordinal)
            && string.Equals(type.ContainingNamespace?.ToDisplayString(), containingNamespace, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is any of the given (namespace, name) types.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="candidates">The (namespace, name) pairs any of which the type may match.</param>
    /// <returns><see langword="true"/> when the type matches one of the candidates.</returns>
    internal static bool IsAnyOf(
        INamedTypeSymbol? type, ImmutableArray<(string Namespace, string Name)> candidates)
    {
        foreach ((string ns, string name) in candidates)
        {
            if (Is(type, ns, name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is declared directly in the namespace
    /// <paramref name="containingNamespace"/>, regardless of its name. Used to catch a whole namespace such as
    /// <c>System.IO.Enumeration</c>.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="containingNamespace">The fully qualified namespace the type must be declared in.</param>
    /// <returns><see langword="true"/> when the type is declared in that namespace.</returns>
    internal static bool IsInNamespace(INamedTypeSymbol? type, string containingNamespace)
    {
        return type is not null
            && string.Equals(type.ContainingNamespace?.ToDisplayString(), containingNamespace, StringComparison.Ordinal);
    }
}
