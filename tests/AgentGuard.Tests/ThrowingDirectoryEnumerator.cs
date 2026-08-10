// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Engine;

namespace AgentGuard.Tests;

/// <summary>
/// A test double for <see cref="IDirectoryEnumerator"/> whose <see cref="EnumerateChildren"/> throws, standing in
/// for a directory the process cannot enumerate. It lets the fail-closed tests prove that an inaccessible directory
/// denies the call and writes no snapshot, OS-agnostically — no <c>chmod</c> and no OS branch — where the real
/// filesystem could only reproduce the condition with a POSIX <c>chmod 000</c>.
/// </summary>
internal sealed class ThrowingDirectoryEnumerator : IDirectoryEnumerator
{
    /// <inheritdoc />
    public bool DirectoryExists(string path) => true;

    /// <inheritdoc />
    public IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath) =>
        throw new UnauthorizedAccessException($"Access to the path '{directoryPath}' is denied.");
}
