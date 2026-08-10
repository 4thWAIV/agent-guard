// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// Identifies the per-OS platform implementation libraries — the <c>AgentGuard.CrossPlatform</c> assembly and
/// its <c>.MacOS</c>/<c>.Linux</c>/<c>.Windows</c> siblings — which are the only place native interop and
/// platform-specific code are allowed to live. Every other assembly is OS-agnostic and must stay free of both.
/// </summary>
internal static class CrossPlatformBoundary
{
    /// <summary>
    /// The root name shared by the platform libraries and the platform namespace — <c>AgentGuard.CrossPlatform</c>.
    /// The single owner of this value; the factory analyzer references it so a rename touches one place.
    /// </summary>
    internal const string RootName = "AgentGuard.CrossPlatform";

    /// <summary>
    /// The exact assembly names of the four platform libraries where native interop and platform-specific code
    /// are permitted: the <c>AgentGuard.CrossPlatform</c> contract assembly and its per-OS <c>.MacOS</c>,
    /// <c>.Linux</c>, and <c>.Windows</c> implementations. The set is matched exactly — a sibling such as
    /// <c>AgentGuard.CrossPlatform.Tests</c> is deliberately NOT a member and stays subject to the rules.
    /// </summary>
    private static readonly ImmutableHashSet<string> PlatformAssemblyNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        RootName,
        RootName + ".MacOS",
        RootName + ".Linux",
        RootName + ".Windows");

    /// <summary>
    /// Gets a value indicating whether <paramref name="compilation"/> is one of the four
    /// <c>AgentGuard.CrossPlatform</c> platform implementation libraries, where native interop and
    /// platform-specific code are permitted. The match is exact: only the contract assembly and its three
    /// per-OS siblings qualify; any other assembly (including <c>AgentGuard.CrossPlatform.Tests</c>) does not.
    /// </summary>
    /// <param name="compilation">The compilation under analysis.</param>
    /// <returns><see langword="true"/> when the assembly is exactly one of the four
    /// <c>AgentGuard.CrossPlatform</c> platform libraries; otherwise <see langword="false"/>.</returns>
    internal static bool IsCrossPlatformLibrary(Compilation compilation)
    {
        string? assemblyName = compilation.AssemblyName;
        return assemblyName is not null && PlatformAssemblyNames.Contains(assemblyName);
    }
}
