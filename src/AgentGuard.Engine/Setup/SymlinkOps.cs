// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.CrossPlatform;

namespace AgentGuard.Setup;

/// <summary>
/// The engine-side symlink policy over the platform's <see cref="IPlatformFileSystem"/>: it keeps the generic
/// "re-point only when needed" idempotency compare here and delegates the platform primitives (read the raw target,
/// atomically create-or-replace the link) to the interface, so the engine stays OS-agnostic and the native atomic
/// swap lives only in the per-OS platform libraries.
/// </summary>
internal sealed class SymlinkOps
{
    private readonly IPlatformFileSystem _fileSystem;

    private SymlinkOps(IPlatformFileSystem fileSystem) => _fileSystem = fileSystem;

    /// <summary>
    /// Creates the symlink policy over the given platform file system.
    /// </summary>
    /// <param name="fileSystem">The platform file-system capability.</param>
    /// <returns>The symlink policy.</returns>
    internal static SymlinkOps Create(IPlatformFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        return new SymlinkOps(fileSystem);
    }

    /// <summary>
    /// Ensures a symlink at <paramref name="linkPath"/> points at <paramref name="relativeTarget"/>, re-pointing it
    /// atomically only when it is missing or points elsewhere. The compare is OS-agnostic and stays here; the
    /// platform primitive (<see cref="IPlatformFileSystem.MakeLinkTarget"/>) is unconditional and atomic.
    /// </summary>
    /// <param name="linkPath">The symlink path.</param>
    /// <param name="relativeTarget">The relative target the link should resolve to.</param>
    /// <returns><see langword="true"/> when the link was created or changed; <see langword="false"/> when it
    /// already pointed at the target.</returns>
    internal bool EnsurePointsTo(string linkPath, string relativeTarget)
    {
        if (string.Equals(_fileSystem.ReadLinkTarget(linkPath), relativeTarget, StringComparison.Ordinal))
        {
            return false;
        }

        _fileSystem.MakeLinkTarget(linkPath, relativeTarget);
        return true;
    }
}
