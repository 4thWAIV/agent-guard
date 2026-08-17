// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="SymlinkOps"/> class over the given platform file system, received
    /// by constructor injection.
    /// </summary>
    /// <param name="fileSystem">The platform file-system capability.</param>
    internal SymlinkOps(IPlatformFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
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
