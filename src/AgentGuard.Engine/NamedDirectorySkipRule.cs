// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Engine;

/// <summary>
/// Skips one specific canonical directory and its subtree — used to prune the Sealed stores (the snapshot store
/// and the grant store) from the walk, so the snapshot store never snapshots itself and a write into a Sealed
/// store never appears as configurable drift.
/// </summary>
internal sealed class NamedDirectorySkipRule : IDirectorySkipRule
{
    private readonly string _canonicalDirectory;

    private NamedDirectorySkipRule(string canonicalDirectory) => _canonicalDirectory = canonicalDirectory;

    /// <inheritdoc />
    public string Identity => $"named:{_canonicalDirectory}";

    /// <inheritdoc />
    public bool ShouldSkip(string directoryFullPath) =>
        string.Equals(directoryFullPath, _canonicalDirectory, StringComparison.Ordinal);

    /// <summary>
    /// Creates a skip rule for one canonical directory.
    /// </summary>
    /// <param name="canonicalDirectory">The canonical directory to skip.</param>
    /// <returns>The skip rule, as its interface.</returns>
    internal static IDirectorySkipRule Create(string canonicalDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(canonicalDirectory);
        return new NamedDirectorySkipRule(canonicalDirectory);
    }
}
