// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The four-leaf view over the shared copy-on-write overlay (<see cref="InMemoryFileSystemStore"/>): one class that
/// implements all four owned filesystem leaves — <see cref="IFileReader"/>, <see cref="IDirectoryEnumerator"/>,
/// <see cref="IFileWriter"/>, and <see cref="IDirectoryWriter"/> — by delegating every read and write to the one overlay,
/// so a symlink created through the platform fake resolves here and a write here never touches the real base. It holds no
/// state of its own; the overlay is the single source of truth, and the platform fake views the SAME overlay for link
/// operations. Its constructor is private (AG0003); it is handed out only as each leaf interface through the four static
/// factories, each of which returns a fresh view over the SAME shared overlay, so all four leaves observe one another's
/// writes.
/// </summary>
internal sealed class InMemoryFileSystem : IFileReader, IDirectoryEnumerator, IFileWriter, IDirectoryWriter
{
    private readonly InMemoryFileSystemStore _store;

    private InMemoryFileSystem(InMemoryFileSystemStore store) => _store = store;

    /// <inheritdoc />
    public bool Exists(string path) => _store.FileExists(path);

    /// <inheritdoc />
    public byte[] ReadAllBytes(string path) => _store.ReadAllBytes(path);

    /// <inheritdoc />
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_store.ReadAllBytes(path));
    }

    /// <inheritdoc />
    public string ReadAllText(string path) => _store.ReadAllText(path);

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_store.ReadAllText(path));
    }

    /// <inheritdoc />
    public FileAttributes GetAttributes(string path) => _store.GetFileAttributes(path);

    /// <inheritdoc />
    public bool DirectoryExists(string path) => _store.DirectoryExists(path);

    /// <inheritdoc />
    public IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath) =>
        _store.EnumerateChildren(directoryPath);

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateFiles(string directoryPath, string pattern, EnumerationOptions options) =>
        _store.EnumerateFiles(directoryPath, pattern);

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateDirectories(string directoryPath, string pattern, EnumerationOptions options) =>
        _store.EnumerateDirectories(directoryPath, pattern);

    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _store.WriteAllBytes(path, bytes);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void WriteAllText(string path, string contents) => _store.WriteAllText(path, contents);

    /// <inheritdoc />
    public void Copy(string source, string destination, bool overwrite = false) =>
        _store.Copy(source, destination, overwrite);

    /// <inheritdoc />
    public void Move(string source, string destination, bool overwrite = false) =>
        _store.Move(source, destination, overwrite);

    /// <inheritdoc />
    public void DeleteFile(string path) => _store.DeleteFile(path);

    /// <inheritdoc />
    public void CreateDirectory(string path) => _store.CreateDirectory(path);

    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive) => _store.DeleteDirectory(path, recursive);

    /// <inheritdoc />
    public string CreateTempSubdirectory(string prefix) => _store.CreateTempSubdirectory(prefix);

    /// <inheritdoc cref="IFileReader.GetLastWriteTimeUtc" />
    DateTimeOffset IFileReader.GetLastWriteTimeUtc(string path) => _store.GetFileLastWriteTimeUtc(path);

    /// <inheritdoc cref="IDirectoryEnumerator.GetLastWriteTimeUtc" />
    DateTimeOffset IDirectoryEnumerator.GetLastWriteTimeUtc(string path) => _store.GetDirectoryLastWriteTimeUtc(path);

    /// <inheritdoc cref="IDirectoryWriter.SetLastWriteTimeUtc" />
    void IDirectoryWriter.SetLastWriteTimeUtc(string path, DateTimeOffset time) =>
        _store.SetDirectoryLastWriteTimeUtc(path, time);

    /// <summary>Creates the file-reader view over the shared overlay.</summary>
    /// <param name="store">The shared copy-on-write overlay.</param>
    /// <returns>The file reader, as its interface.</returns>
    internal static IFileReader AsFileReader(InMemoryFileSystemStore store) => new InMemoryFileSystem(store);

    /// <summary>Creates the directory-enumerator view over the shared overlay.</summary>
    /// <param name="store">The shared copy-on-write overlay.</param>
    /// <returns>The directory enumerator, as its interface.</returns>
    internal static IDirectoryEnumerator AsDirectoryEnumerator(InMemoryFileSystemStore store) =>
        new InMemoryFileSystem(store);

    /// <summary>Creates the file-writer view over the shared overlay.</summary>
    /// <param name="store">The shared copy-on-write overlay.</param>
    /// <returns>The file writer, as its interface.</returns>
    internal static IFileWriter AsFileWriter(InMemoryFileSystemStore store) => new InMemoryFileSystem(store);

    /// <summary>Creates the directory-writer view over the shared overlay.</summary>
    /// <param name="store">The shared copy-on-write overlay.</param>
    /// <returns>The directory writer, as its interface.</returns>
    internal static IDirectoryWriter AsDirectoryWriter(InMemoryFileSystemStore store) => new InMemoryFileSystem(store);
}
