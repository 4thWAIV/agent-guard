// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IFileWriter"/> adapter. It is the single class that implements the file-write side of the
/// filesystem primitive, so it is the ONE place the raw <c>System.IO.File</c> write/copy/move/delete members are
/// allowed (AG0011 exempts exactly this owner class in this assembly). Directory-mutating operations are
/// <see cref="IDirectoryWriter"/>'s, not this owner's. It is <c>internal</c> with a <c>private</c> constructor (Wall 1)
/// and handed out only as its interface.
/// </summary>
internal sealed class FileWriterAdapter : IFileWriter
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private FileWriterAdapter()
    {
    }

    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken ct) =>
        File.WriteAllBytesAsync(path, bytes, ct);

    /// <inheritdoc />
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

    /// <inheritdoc />
    public void Copy(string source, string destination, bool overwrite = false) =>
        File.Copy(source, destination, overwrite);

    /// <inheritdoc />
    public void Move(string source, string destination, bool overwrite = false) =>
        File.Move(source, destination, overwrite);

    /// <inheritdoc />
    public void DeleteFile(string path) => File.Delete(path);

    /// <summary>
    /// Creates the file writer.
    /// </summary>
    /// <returns>The file writer, as its interface.</returns>
    internal static IFileWriter Create() => new FileWriterAdapter();
}
