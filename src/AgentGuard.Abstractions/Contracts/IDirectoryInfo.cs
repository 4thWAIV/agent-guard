// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.IO;

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned view of a single directory entry — the abstraction that mirrors the .NET <see cref="DirectoryInfo"/>
/// 1:1, carrying only the members the engine needs today (its <see cref="IFileSystemInfo"/> base plus the one-pass
/// child enumeration). The raw <see cref="DirectoryInfo"/> lives only inside the wrapper class that implements this
/// interface in <c>AgentGuard.CrossPlatform</c>; every other site obtains one through
/// <see cref="IFileSystem.GetDirectoryInfo(string)"/>.
/// </summary>
public interface IDirectoryInfo : IFileSystemInfo
{
    /// <summary>
    /// Enumerates the immediate children of this directory in one pass, mirroring
    /// <see cref="DirectoryInfo.EnumerateFileSystemInfos()"/> 1:1. Entries are yielded one at a time (lazily); a
    /// consumer that needs fail-closed behavior materializes the sequence so an unreadable directory throws at the
    /// call rather than being silently skipped.
    /// </summary>
    /// <returns>The directory's immediate children, one <see cref="IFileSystemInfo"/> per entry.</returns>
    IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos();
}
