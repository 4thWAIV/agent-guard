// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// Shared predicates that identify contract interfaces, which are interfaces declared under an
/// <c>.Abstractions.Contracts</c> namespace, and the classes that implement them.
/// </summary>
internal static class ContractPattern
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is an interface declared under an
    /// <c>.Abstractions.Contracts</c> namespace.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> if it is a contract interface.</returns>
    internal static bool IsContractInterface(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Interface && IsUnderAbstractionsContracts(type.ContainingNamespace);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is a class that implements at least one
    /// contract interface.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> if it is a contract implementation class.</returns>
    internal static bool IsContractImplementation(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Class
            && !type.IsRecord
            && type.AllInterfaces.Any(IsContractInterface);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is, or contains anywhere within its type
    /// arguments, array element, or pointed-at type, a contract implementation class.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> if a contract implementation class appears within the type.</returns>
    internal static bool ReferencesContractImplementation(ITypeSymbol? type)
    {
        return TypeTree.Any(type, IsContractImplementation);
    }

    private static bool IsUnderAbstractionsContracts(INamespaceSymbol? containingNamespace)
    {
        for (INamespaceSymbol? ns = containingNamespace; ns is not null && !ns.IsGlobalNamespace; ns = ns.ContainingNamespace)
        {
            if (string.Equals(ns.Name, "Contracts", StringComparison.Ordinal)
                && ns.ContainingNamespace is { IsGlobalNamespace: false } parent
                && string.Equals(parent.Name, "Abstractions", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
