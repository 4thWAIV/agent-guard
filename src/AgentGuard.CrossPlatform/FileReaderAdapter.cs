// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IFileReader"/> adapter. It is the single class that implements the read side of the filesystem
/// primitive, so it is the ONE place the raw <c>System.IO.File</c> read members are allowed (AG0011 exempts exactly
/// this owner class in this assembly); every other type reads through <see cref="IFileReader"/> pulled off the
/// container. It is <c>internal</c> with a <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class FileReaderAdapter : IFileReader
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private FileReaderAdapter()
    {
    }

    /// <inheritdoc />
    public bool Exists(string path) => File.Exists(path);

    /// <inheritdoc />
    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    /// <inheritdoc />
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct) => File.ReadAllBytesAsync(path, ct);

    /// <inheritdoc />
    public string ReadAllText(string path) => File.ReadAllText(path);

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken ct) => File.ReadAllTextAsync(path, ct);

    /// <inheritdoc />
    public FileAttributes GetAttributes(string path) => File.GetAttributes(path);

    /// <inheritdoc />
    public DateTimeOffset GetLastWriteTimeUtc(string path) => new(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);

    /// <summary>
    /// Creates the file reader.
    /// </summary>
    /// <returns>The file reader, as its interface.</returns>
    internal static IFileReader Create() => new FileReaderAdapter();
}
