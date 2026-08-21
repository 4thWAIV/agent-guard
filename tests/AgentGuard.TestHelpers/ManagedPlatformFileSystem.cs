// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The <see cref="IPlatformFileSystem"/> view over the ONE shared copy-on-write overlay
/// (<see cref="InMemoryFileSystemStore"/>): its link operations land in and read from the same overlay the filesystem
/// leaves read, so a symlink created here via <see cref="MakeLinkTarget"/> resolves for <c>IFileReader.Exists</c> with no
/// real OS symlink created. It makes zero calls into the per-OS <c>PlatformFileSystemShared</c> internals and adds no
/// <c>InternalsVisibleTo</c> from <c>AgentGuard.CrossPlatform</c> — a pure fake over the overlay, not a double that
/// reaches into production code. <see cref="MakeLinkTarget"/> mirrors the per-OS refuse-if-a-real-entry-exists and
/// auto-create-parent behavior, evaluated against the overlay. Its <see cref="DirectorySeparator"/> is the authorized
/// pass-through to <c>Path.DirectorySeparatorChar</c> (directoryseparator-owned-passthrough); it reports
/// <see cref="NeedsExecutableFlag"/> as <see langword="false"/>, so the engine never touches the executable bit in a fake
/// run. It does not own the overlay — the temp-path uniqueness counter lives on the shared overlay, never here. Its
/// constructor is private (AG0003) and it is handed out only as its interface.
/// </summary>
internal sealed class ManagedPlatformFileSystem : IPlatformFileSystem
{
    private readonly InMemoryFileSystemStore _store;

    private ManagedPlatformFileSystem(InMemoryFileSystemStore store) => _store = store;

    /// <inheritdoc />
    public char DirectorySeparator => Path.DirectorySeparatorChar;

    /// <inheritdoc />
    public bool IsLinkTarget(string linkPath) => _store.IsLinkTarget(linkPath);

    /// <inheritdoc />
    public string? ReadLinkTarget(string linkPath) => _store.ReadLinkTarget(linkPath);

    /// <inheritdoc />
    public void MakeLinkTarget(string linkPath, string relativeTarget) =>
        _store.MakeLinkTarget(linkPath, relativeTarget);

    /// <inheritdoc />
    public void RemoveLinkTarget(string linkPath) => _store.RemoveLinkTarget(linkPath);

    /// <inheritdoc />
    public bool NeedsExecutableFlag() => false;

    /// <inheritdoc />
    public bool IsExecutable(string path) =>
        throw new PlatformNotSupportedException("The managed test double does not model the executable bit.");

    /// <inheritdoc />
    public void MakeExecutable(string path) =>
        throw new PlatformNotSupportedException("The managed test double does not model the executable bit.");

    /// <inheritdoc />
    public void MakeNonExecutable(string path) =>
        throw new PlatformNotSupportedException("The managed test double does not model the executable bit.");

    /// <inheritdoc />
    public bool IsCaseSensitive(string path) => _store.CaseSensitive;

    /// <summary>Creates the platform view over the given shared overlay.</summary>
    /// <param name="store">The shared copy-on-write overlay the link operations read and write.</param>
    /// <returns>The platform file system, as its interface.</returns>
    internal static IPlatformFileSystem Create(InMemoryFileSystemStore store) => new ManagedPlatformFileSystem(store);
}
