// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.Engine;

/// <summary>
/// The seam between <see cref="ProtectedFileScanner"/> and the OS filesystem walk. Directory enumeration through
/// the BCL is OS-uniform, so this is an engine-level abstraction for test mocking — NOT a cross-platform interop
/// interface, and it lives beside <see cref="IDirectorySkipRule"/> in the engine root, never on
/// <c>IPlatformFileSystem</c>. The production adapter is <see cref="SystemDirectoryEnumerator"/>; a test double can
/// drive the fail-closed path by throwing from <see cref="EnumerateChildren"/>.
/// </summary>
internal interface IDirectoryEnumerator
{
    /// <summary>
    /// Determines whether a directory exists at the given path.
    /// </summary>
    /// <param name="path">The absolute directory path.</param>
    /// <returns><see langword="true"/> when the directory exists.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Returns the immediate children of one directory. An inaccessible directory <b>throws</b>
    /// (<see cref="System.IO.IOException"/> / <see cref="System.UnauthorizedAccessException"/>) and is never
    /// silently skipped, so an incomplete walk denies the call rather than hiding a protected file.
    /// </summary>
    /// <param name="directoryPath">The absolute directory path whose children are listed.</param>
    /// <returns>The directory's immediate children.</returns>
    IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath);
}
