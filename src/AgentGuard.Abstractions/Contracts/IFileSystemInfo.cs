// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned base view of a filesystem entry — the abstraction that mirrors the .NET <see cref="FileSystemInfo"/>
/// base 1:1, carrying only the members the engine needs today (new members are added later through the contract,
/// never frozen). <see cref="FileInfo"/> and <see cref="DirectoryInfo"/> are stateful objects with behavior, not
/// data, so they are treated as services behind this owned interface: the raw <c>FileSystemInfo</c> lives only
/// inside the wrapper class that implements this interface in <c>AgentGuard.CrossPlatform</c>, and every other site
/// reaches an entry through <see cref="IFileInfo"/>/<see cref="IDirectoryInfo"/> obtained from <see cref="IFileSystem"/>.
/// </summary>
public interface IFileSystemInfo
{
    /// <summary>
    /// Gets the full path of the entry.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Gets the attributes of the entry.
    /// </summary>
    FileAttributes Attributes { get; }

    /// <summary>
    /// Gets the raw (unresolved) target of the entry when it is a link, or <see langword="null"/> when it is not a
    /// link.
    /// </summary>
    string? LinkTarget { get; }
}
