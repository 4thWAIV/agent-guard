// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Boundaries;

/// <summary>
/// The owned <see cref="IEnvironment"/> adapter. It is the single class that implements the OS-environment read
/// seam, so it is the ONE place the raw <c>System.Environment</c> members are allowed (AG0012 exempts exactly this
/// owner class in <c>AgentGuard.Boundaries</c>); every other type reads the environment through
/// <see cref="IEnvironment"/> pulled off <see cref="ISystemServices"/>. Nothing below Boundaries consumes the
/// environment, so the owner stays here (owners-live-at-lowest-consumer). It is <c>internal</c> with a
/// <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class EnvironmentAdapter : IEnvironment
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private EnvironmentAdapter()
    {
    }

    /// <inheritdoc />
    public string GetCurrentDirectory() => Environment.CurrentDirectory;

    /// <inheritdoc />
    public string GetHomeDirectory() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    /// <inheritdoc />
    public string? GetProcessPath() => Environment.ProcessPath;

    /// <inheritdoc />
    [SuppressMessage(
        "Security Hotspot",
        "S5443:Using publicly writable directories is security-sensitive",
        Justification = "This owner returns the temp ROOT as the decided owned read (GetTempDirectory wraps Path.GetTempPath); callers never write into the shared root directly — they create an atomic, uniquely-named subdirectory through IDirectoryWriter.CreateTempSubdirectory.")]
    public string GetTempDirectory() => Path.GetTempPath();

    /// <inheritdoc />
    // The owned AppContext.BaseDirectory read (AG0011 assigns the primitive to the IEnvironment owner in Boundaries).
    // It mirrors the primitive faithfully — the value the runtime reports, trailing directory separator included — so
    // no policy is baked into the wrapper; a caller that needs the deployment path composes onto it.
    public string GetBaseDirectory() => AppContext.BaseDirectory;

    /// <inheritdoc />
    public Architecture GetProcessArchitecture() => RuntimeInformation.ProcessArchitecture;

    /// <inheritdoc />
    public Architecture GetOSArchitecture() => RuntimeInformation.OSArchitecture;

    /// <summary>
    /// Creates the environment adapter.
    /// </summary>
    /// <returns>The environment adapter, as its interface.</returns>
    internal static IEnvironment Create() => new EnvironmentAdapter();
}
