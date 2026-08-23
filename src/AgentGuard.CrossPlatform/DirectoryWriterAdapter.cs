// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IDirectoryWriter"/> adapter. It is the single class that implements the directory-write side of
/// the filesystem primitive, so it is the ONE place the raw <c>Directory</c> create/delete/set-time members are allowed
/// (AG0011 exempts exactly this owner class in this assembly). It is <c>internal</c> with a <c>private</c> constructor
/// (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class DirectoryWriterAdapter : IDirectoryWriter
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private DirectoryWriterAdapter()
    {
    }

    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

    /// <inheritdoc />
    public void SetLastWriteTimeUtc(string path, DateTimeOffset time) =>
        Directory.SetLastWriteTimeUtc(path, time.UtcDateTime);

    /// <inheritdoc />
    // Directory.CreateTempSubdirectory is the atomic, uniquely-named directory write owned here (AG0011). Its result is
    // a raw DirectoryInfo whose FullName is owned by IFileSystemInfo, so the path is read back through the owned
    // IFileSystemInfo view (AbstractedFileSystemInfo, the one owner of that read) rather than a raw *Info member access.
    public string CreateTempSubdirectory(string prefix) =>
        AbstractedFileSystemInfo.Wrap(Directory.CreateTempSubdirectory(prefix)).FullName;

    /// <summary>
    /// Creates the directory writer.
    /// </summary>
    /// <returns>The directory writer, as its interface.</returns>
    internal static IDirectoryWriter Create() => new DirectoryWriterAdapter();
}
