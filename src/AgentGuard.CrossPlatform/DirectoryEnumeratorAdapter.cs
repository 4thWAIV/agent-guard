// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IDirectoryEnumerator"/> adapter. It is the single class that implements read-only directory
/// access, so it is the ONE place the raw <c>Directory</c> enumeration members are allowed (AG0011 exempts exactly this
/// owner class in this assembly). Its <see cref="EnumerateChildren"/> is the SINGLE directory walk: one pass over the
/// injected <see cref="IFileInfoFactory"/>'s <see cref="IDirectoryInfo.EnumerateFileSystemInfos"/>, reading each
/// child's kind and reparse flag from the entry that one pass already materialized — never a second walk and never a
/// per-child re-stat. It owns the fail-closed guarantee — the parameterless
/// <see cref="DirectoryInfo.EnumerateFileSystemInfos()"/> throws on an inaccessible directory rather than skipping it,
/// and this enumerator materializes the sequence to a list so that failure lands at the call. No enumerating member
/// guarantees any ordering (enumeration-no-order-guarantee). It is <c>internal</c> with a <c>private</c> constructor
/// (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class DirectoryEnumeratorAdapter : IDirectoryEnumerator
{
    private readonly IFileInfoFactory _factory;

    // Private constructor (AG0003, Wall 1). It receives the wrapper factory directly — one of the two seams that
    // cannot route back through IFileSystem (which itself holds this enumerator), so the factory is injected here
    // (fileinfo-factory-breaks-the-cycle) — and uses it for the single directory walk.
    private DirectoryEnumeratorAdapter(IFileInfoFactory factory) => _factory = factory;

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath)
    {
        // The ONE directory walk: a single pass over the directory's children through the injected factory. Each
        // child's kind and reparse flag are read from the entry that pass already materialized (FullName -> FullPath,
        // Attributes.HasFlag(Directory) -> IsDirectory, Attributes.HasFlag(ReparsePoint) -> IsReparsePoint) — no second
        // walk and no per-child re-stat. Materializing into the list here forces the lazy enumeration, so an
        // inaccessible directory throws at this call (fail-closed) rather than being silently skipped. No ordering is
        // applied (enumeration-no-order-guarantee).
        var children = new List<DirectoryChild>();

        foreach (IFileSystemInfo entry in _factory.GetDirectoryInfo(directoryPath).EnumerateFileSystemInfos())
        {
            FileAttributes attributes = entry.Attributes;
            children.Add(new DirectoryChild(
                entry.FullName,
                attributes.HasFlag(FileAttributes.Directory),
                attributes.HasFlag(FileAttributes.ReparsePoint)));
        }

        return children;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateFiles(string directoryPath, string pattern, EnumerationOptions options) =>
        Directory.EnumerateFiles(directoryPath, pattern, options).ToList();

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateDirectories(string directoryPath, string pattern, EnumerationOptions options) =>
        Directory.EnumerateDirectories(directoryPath, pattern, options).ToList();

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => new(Directory.GetLastWriteTimeUtc(path), TimeSpan.Zero);

    /// <summary>
    /// Creates the directory enumerator. It builds the dependency-free wrapper factory it walks through, so the seam
    /// is never passed in as a method argument.
    /// </summary>
    /// <returns>The directory enumerator, as its interface.</returns>
    internal static IDirectoryEnumerator Create() => new DirectoryEnumeratorAdapter(FileInfoFactory.Create());
}
