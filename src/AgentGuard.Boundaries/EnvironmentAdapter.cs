// Copyright (c) 4thWAIV. All rights reserved.

using System;
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

    /// <summary>
    /// Creates the environment adapter.
    /// </summary>
    /// <returns>The environment adapter, as its interface.</returns>
    internal static IEnvironment Create() => new EnvironmentAdapter();
}
