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

    /// <summary>The exact assembly name of the macOS per-OS implementation library — <c>AgentGuard.CrossPlatform.MacOS</c>.
    /// The single owner of this name so the <c>.MacOS</c> suffix is spelled once (LESSON 1, DRY).</summary>
    internal const string MacOsName = RootName + ".MacOS";

    /// <summary>The exact assembly name of the Linux per-OS implementation library — <c>AgentGuard.CrossPlatform.Linux</c>.
    /// The single owner of this name so the <c>.Linux</c> suffix is spelled once (LESSON 1, DRY).</summary>
    internal const string LinuxName = RootName + ".Linux";

    /// <summary>The exact assembly name of the Windows per-OS implementation library — <c>AgentGuard.CrossPlatform.Windows</c>.
    /// The single owner of this name so the <c>.Windows</c> suffix is spelled once (LESSON 1, DRY).</summary>
    internal const string WindowsName = RootName + ".Windows";

    /// <summary>
    /// The cached <c>Compilation</c>-level gate for the three per-OS implementation libraries — the single adapter
    /// that pulls a compilation's assembly name and calls <see cref="IsPerOsImplementationAssembly"/>. Owned here,
    /// next to the string check it wraps, so the two per-OS-owner rules that use it as an
    /// <see cref="OwnerClass.IsOwner"/> assembly gate — AG0101 (OsDivergentFilesystemOnlyInCrossPlatformAnalyzer, the
    /// OS-divergent filesystem owner) and AG0113 (PresenceNativeInteropOwnerAnalyzer, the presence native-interop
    /// owner) — share this one adapter instead of each re-wrapping the call, mirroring the caching pattern
    /// <see cref="OwnerClass.InAssembly"/> establishes. Cached in a static field so no delegate is allocated per
    /// analyzed operation.
    /// </summary>
    internal static readonly Func<Compilation, bool> IsAnyPerOsImplementationLibrary =
        compilation => IsPerOsImplementationAssembly(compilation.AssemblyName);

    /// <summary>
    /// The exact assembly names of the four platform libraries where native interop and platform-specific code
    /// are permitted: the <c>AgentGuard.CrossPlatform</c> contract assembly and its per-OS <c>.MacOS</c>,
    /// <c>.Linux</c>, and <c>.Windows</c> implementations. The set is matched exactly — a sibling such as
    /// <c>AgentGuard.CrossPlatform.Tests</c> is deliberately NOT a member and stays subject to the rules.
    /// </summary>
    private static readonly ImmutableHashSet<string> PlatformAssemblyNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        RootName,
        MacOsName,
        LinuxName,
        WindowsName);

    /// <summary>
    /// The exact assembly names of the three per-OS implementation libraries — <c>.MacOS</c>, <c>.Linux</c>, and
    /// <c>.Windows</c> — WITHOUT the core <c>AgentGuard.CrossPlatform</c> contract assembly. AG0029 uses this to gate
    /// a call from <c>AgentGuard.Boundaries</c> into a per-OS assembly (allowed only as <c>PlatformServices.Create()</c>),
    /// which is a different door from AG0023's call into the core assembly. Derived from
    /// <see cref="PlatformAssemblyNames"/> by removing the core assembly, so the per-OS suffixes are spelled once.
    /// </summary>
    private static readonly ImmutableHashSet<string> PerOsImplementationAssemblyNames =
        PlatformAssemblyNames.Remove(RootName);

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

    /// <summary>
    /// Gets a value indicating whether <paramref name="assemblyName"/> is one of the three per-OS implementation
    /// libraries (<c>.MacOS</c>/<c>.Linux</c>/<c>.Windows</c>), excluding the core <c>AgentGuard.CrossPlatform</c>
    /// contract assembly. AG0029 uses this to identify a call from Boundaries into a per-OS assembly.
    /// </summary>
    /// <param name="assemblyName">The assembly name to test.</param>
    /// <returns><see langword="true"/> when the assembly is a per-OS implementation library.</returns>
    internal static bool IsPerOsImplementationAssembly(string? assemblyName)
    {
        return assemblyName is not null && PerOsImplementationAssemblyNames.Contains(assemblyName);
    }
}
