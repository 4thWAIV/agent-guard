// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.TestHelpers;

/// <summary>
/// One entry in the copy-on-write overlay (<see cref="InMemoryFileSystemStore"/>): its <see cref="NodeKind"/>, the
/// payload for that kind (bytes for a file, the raw target for a symlink), its last-write time, and the
/// inaccessible flag that makes a directory throw on enumeration. Mutable so a later write to the same path updates
/// the entry in place.
/// </summary>
internal sealed class OverlayNode
{
    private OverlayNode(NodeKind kind)
    {
        Kind = kind;
    }

    /// <summary>Gets or sets the kind of entry this node represents.</summary>
    internal NodeKind Kind { get; set; }

    /// <summary>Gets or sets the file bytes, when <see cref="Kind"/> is <see cref="NodeKind.File"/>.</summary>
    internal byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the raw (unresolved) symlink target, when <see cref="Kind"/> is
    /// <see cref="NodeKind.Symlink"/>.</summary>
    internal string LinkTarget { get; set; } = string.Empty;

    /// <summary>Gets or sets the entry's last-write time, in UTC.</summary>
    internal DateTimeOffset LastWriteUtc { get; set; }

    /// <summary>Gets or sets a value indicating whether a directory entry denies enumeration — reading its children
    /// throws <see cref="UnauthorizedAccessException"/>, standing in for a directory the process cannot read.</summary>
    internal bool Inaccessible { get; set; }

    /// <summary>Gets or sets a value indicating whether a file entry carries the POSIX executable bit. It is the single
    /// boolean flag the simulator models (copy-on-write-simulator-design), NOT a full Unix-mode model; a base file that
    /// gets its bit set materializes an overlay node (copy-on-write), so the real fixture is never touched.</summary>
    internal bool Executable { get; set; }

    /// <summary>Creates a file node holding the given bytes.</summary>
    /// <param name="content">The file bytes.</param>
    /// <param name="lastWriteUtc">The entry's last-write time, in UTC.</param>
    /// <returns>The file node.</returns>
    internal static OverlayNode ForFile(byte[] content, DateTimeOffset lastWriteUtc) =>
        new(NodeKind.File) { Content = content, LastWriteUtc = lastWriteUtc };

    /// <summary>Creates a directory node.</summary>
    /// <param name="lastWriteUtc">The entry's last-write time, in UTC.</param>
    /// <returns>The directory node.</returns>
    internal static OverlayNode ForDirectory(DateTimeOffset lastWriteUtc) =>
        new(NodeKind.Directory) { LastWriteUtc = lastWriteUtc };

    /// <summary>Creates a symlink node pointing at the given raw target.</summary>
    /// <param name="target">The raw (unresolved) target the link points at.</param>
    /// <param name="lastWriteUtc">The entry's last-write time, in UTC.</param>
    /// <returns>The symlink node.</returns>
    internal static OverlayNode ForSymlink(string target, DateTimeOffset lastWriteUtc) =>
        new(NodeKind.Symlink) { LinkTarget = target, LastWriteUtc = lastWriteUtc };

    /// <summary>Creates a tombstone node marking the path deleted.</summary>
    /// <returns>The tombstone node.</returns>
    internal static OverlayNode ForTombstone() => new(NodeKind.Tombstone);
}
