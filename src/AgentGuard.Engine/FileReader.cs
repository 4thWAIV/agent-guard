// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Read-only access to the working tree, used by Capture to snapshot a protected file's current bytes and by
/// Postcheck to read what landed on disk. It never writes; the privileged restore and delete are the Engine's.
/// </summary>
internal sealed class FileReader : IFileReader
{
    private FileReader()
    {
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return bytes;
    }

    /// <inheritdoc />
    public bool Exists(string path) => File.Exists(path);

    /// <summary>
    /// Creates a file reader.
    /// </summary>
    /// <returns>The file reader, as its interface.</returns>
    internal static IFileReader Create() => new FileReader();
}
