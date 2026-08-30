// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
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
    /// Gets a value indicating whether <paramref name="type"/> is the type named <paramref name="typeName"/> in the
    /// namespace <paramref name="containingNamespace"/> AND is compiled into the assembly named
    /// <paramref name="assemblyName"/>. This is the single home for the "type identity AND declaring-assembly"
    /// conjunction the boundary rules use, so a type merely NAMED the same in another assembly cannot self-grant an
    /// exemption. The namespace and the assembly are matched separately because an assembly name may differ from the
    /// root namespace (LESSON 1: the CLI's <c>Program</c> is in namespace <c>AgentGuard.Cli</c> but compiles into the
    /// assembly <c>guard</c>).
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="containingNamespace">The fully qualified namespace the type must be declared in.</param>
    /// <param name="typeName">The simple name the type must have.</param>
    /// <param name="assemblyName">The name of the assembly the type must be compiled into.</param>
    /// <returns><see langword="true"/> when the type matches the namespace, the name, and the assembly.</returns>
    internal static bool IsInAssembly(
        INamedTypeSymbol? type, string containingNamespace, string typeName, string assemblyName)
    {
        return Is(type, containingNamespace, typeName)
            && string.Equals(type?.ContainingAssembly?.Name, assemblyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="symbol"/> is declared in the compilation under analysis — that
    /// is, its containing assembly is the compilation's own assembly, so it has a source location here rather than
    /// being pulled from referenced metadata. This is the single home for the "is this symbol declared in the
    /// compilation under analysis" predicate the boundary rules use to scope a diagnostic to the assembly that DECLARES
    /// the symbol: a referencing assembly sees the same symbol from metadata and has no source location to report at.
    /// </summary>
    /// <param name="symbol">The symbol to test.</param>
    /// <param name="compilation">The compilation under analysis.</param>
    /// <returns><see langword="true"/> when the symbol is declared in the compilation under analysis.</returns>
    internal static bool IsDeclaredInCompilation(ISymbol? symbol, Compilation compilation) =>
        symbol is not null && SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly);

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

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is declared in the namespace <paramref name="root"/>
    /// OR any namespace nested beneath it — the "namespace tree" match, so <c>Tmds.DBus.Protocol</c> matches a
    /// <paramref name="root"/> of <c>Tmds.DBus.Protocol</c> AND of <c>Tmds.DBus</c>. This is the whole-package match
    /// the D-Bus owner rule (AG0110) uses to catch every type a package exposes; it is the tree companion to the
    /// exact-namespace <see cref="IsInNamespace"/>, resolving the type's <see cref="ISymbol.ContainingNamespace"/>
    /// internally so a caller never re-spells the <see cref="ISymbol.ToDisplayString"/> and prefix comparison.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="root">The root namespace the type must be declared in or nested beneath.</param>
    /// <returns><see langword="true"/> when the type is in that namespace or a sub-namespace of it.</returns>
    internal static bool IsInNamespaceTree(INamedTypeSymbol? type, string root)
    {
        string? namespaceName = type?.ContainingNamespace?.ToDisplayString();
        return namespaceName is not null
            && (string.Equals(namespaceName, root, StringComparison.Ordinal)
                || namespaceName.StartsWith(root + ".", StringComparison.Ordinal));
    }

    /// <summary>
    /// Resolves the type named <paramref name="typeName"/> in namespace <paramref name="containingNamespace"/> from
    /// <paramref name="compilation"/> — whether that type is declared in the compilation under analysis OR referenced
    /// from another assembly — by navigating the merged namespace tree, or <see langword="null"/> when no such type
    /// is in scope. This is the "find the anchor type" companion to the <see cref="Is"/> predicate: it is used to
    /// resolve the <c>ISystemServices</c> container the boundary service set is derived from, matched by the same
    /// (namespace + name) identity, never a bare name and never
    /// <see cref="Compilation.GetTypeByMetadataName(string)"/> (which returns <see langword="null"/> for a name
    /// defined or forwarded across more than one assembly).
    /// </summary>
    /// <param name="compilation">The compilation whose merged namespace tree is searched.</param>
    /// <param name="containingNamespace">The fully qualified namespace the type must be declared in.</param>
    /// <param name="typeName">The simple name the type must have.</param>
    /// <returns>The resolved type, or <see langword="null"/> when it is not in scope.</returns>
    internal static INamedTypeSymbol? Resolve(
        Compilation compilation, string containingNamespace, string typeName)
    {
        if (compilation is null)
        {
            return null;
        }

        INamespaceSymbol? namespaceSymbol = ResolveNamespace(compilation.GlobalNamespace, containingNamespace);
        if (namespaceSymbol is null)
        {
            return null;
        }

        return namespaceSymbol.GetTypeMembers(typeName)
            .FirstOrDefault(candidate => Is(candidate, containingNamespace, typeName));
    }

    // Walks the dotted namespace from the merged global namespace one segment at a time. GetNamespaceMembers on the
    // compilation's global namespace spans the compilation AND its references, so a referenced type resolves the same
    // as a source one.
    private static INamespaceSymbol? ResolveNamespace(INamespaceSymbol root, string containingNamespace)
    {
        INamespaceSymbol? current = root;
        foreach (string segment in containingNamespace.Split('.'))
        {
            current = current.GetNamespaceMembers()
                .FirstOrDefault(child => string.Equals(child.Name, segment, StringComparison.Ordinal));
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }
}
