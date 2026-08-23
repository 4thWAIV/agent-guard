// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.TestHelpers;

/// <summary>
/// The kind of an entry recorded in the copy-on-write overlay (<see cref="InMemoryFileSystemStore"/>). Each kind is a
/// redefinition layered on top of the real read-only fixture: a later read honors the overlay entry instead of the
/// base.
/// </summary>
internal enum NodeKind
{
    /// <summary>A regular file whose bytes live in the overlay.</summary>
    File,

    /// <summary>A directory created in the overlay.</summary>
    Directory,

    /// <summary>A symlink created in the overlay, pointing at its raw (unresolved) target.</summary>
    Symlink,

    /// <summary>A deletion: the path is treated as absent even when the base fixture still has an entry there.</summary>
    Tombstone,
}
