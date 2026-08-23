// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Reflection;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Boundaries;

/// <summary>
/// The owned <see cref="IBuildInfo"/> — the single class that reads the running build's version, so it is the ONE
/// place the version-reflection reads are allowed (AG0028 exempts exactly this owner). It reads the three
/// already-stamped version attributes from a specific guard assembly (this owner's own assembly), NEVER the ambient
/// <c>Assembly.GetEntryAssembly()</c> — under <c>dotnet test</c> the entry assembly is the test host, which reports
/// <c>dotnet</c> rather than the guard (build-version-behind-ibuildinfo). The values are computed and stamped one way
/// by the build's compute-version-once/stamp-every-build scheme, so this owner neither falls back nor recomputes: it
/// only reads, and fails loudly if an attribute is somehow absent rather than fabricating a placeholder
/// (implement-scan-decisions). It is <c>internal</c> with a <c>private</c> constructor (Wall 1) and handed out only
/// as its interface.
/// </summary>
internal sealed class BuildInfoReader : IBuildInfo
{
    private readonly string _semVer;
    private readonly string _assemblyVersion;
    private readonly string _fileVersion;

    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it, with the already-read values.
    private BuildInfoReader(string semVer, string assemblyVersion, string fileVersion)
    {
        _semVer = semVer;
        _assemblyVersion = assemblyVersion;
        _fileVersion = fileVersion;
    }

    /// <inheritdoc />
    public string SemVer => _semVer;

    /// <inheritdoc />
    public string AssemblyVersion => _assemblyVersion;

    /// <inheritdoc />
    public string FileVersion => _fileVersion;

    /// <summary>
    /// Creates the build-info reader, reading the three stamped version attributes from this owner's guard assembly
    /// (never the ambient entry assembly).
    /// </summary>
    /// <returns>The build-info reader, as its interface.</returns>
    internal static IBuildInfo Create()
    {
        // A specific guard assembly, resolved off this owner's own type — never Assembly.GetEntryAssembly(), which
        // under a test host resolves to dotnet rather than the guard.
        Assembly assembly = typeof(BuildInfoReader).Assembly;

        // Read only; the stamp scheme guarantees these are present, so a missing attribute is an impossible state
        // that fails loudly rather than falling back to or recomputing a version.
        string semVer = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? throw new InvalidOperationException(
                "The guard assembly has no AssemblyInformationalVersionAttribute; the build did not stamp the version.");
        string assemblyVersion = assembly.GetName().Version?.ToString()
            ?? throw new InvalidOperationException(
                "The guard assembly has no assembly version; the build did not stamp the version.");
        string fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? throw new InvalidOperationException(
                "The guard assembly has no AssemblyFileVersionAttribute; the build did not stamp the version.");

        return new BuildInfoReader(semVer, assemblyVersion, fileVersion);
    }
}
