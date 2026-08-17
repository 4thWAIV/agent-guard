// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Text;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Setup;

/// <summary>
/// Appends a block to a text file exactly once. The caller decides, from the file's current content, which block
/// (if any) is missing; this owns the shared mechanism: read-or-empty, a guaranteed blank-line separation before
/// the block, and the atomic write. It reads through the owned <see cref="IFileReader"/> and writes through the
/// owned <see cref="AtomicFile"/>, so no raw filesystem call lives here; it is built from the container at the
/// composition point.
/// </summary>
internal sealed class IdempotentAppend
{
    private readonly IFileReader _fileReader;
    private readonly AtomicFile _atomicFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotentAppend"/> class over the owned read and atomic-write
    /// services.
    /// </summary>
    /// <param name="fileReader">The owned read side of the filesystem.</param>
    /// <param name="atomicFile">The owned atomic-file writer.</param>
    internal IdempotentAppend(IFileReader fileReader, AtomicFile atomicFile)
    {
        _fileReader = fileReader;
        _atomicFile = atomicFile;
    }

    /// <summary>
    /// Builds an idempotent appender from a setup context's owned services.
    /// </summary>
    /// <param name="context">The setup context carrying the owned services.</param>
    /// <returns>The idempotent appender.</returns>
    internal static IdempotentAppend For(SetupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new IdempotentAppend(context.FileReader, AtomicFile.For(context));
    }

    /// <summary>
    /// Appends the block that <paramref name="buildBlock"/> returns for the file's current content, or does
    /// nothing when it returns <see langword="null"/>.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="buildBlock">Given the current content, returns the block to append, or <see langword="null"/>
    /// when nothing is missing.</param>
    /// <returns><see langword="true"/> when a block was appended; <see langword="false"/> when nothing was.</returns>
    internal bool Ensure(string path, Func<string, string?> buildBlock)
    {
        string existing = _fileReader.Exists(path) ? _fileReader.ReadAllText(path) : string.Empty;
        string? block = buildBlock(existing);
        if (block is null)
        {
            return false;
        }

        var builder = new StringBuilder(existing);
        if (existing.Length > 0 && !existing.EndsWith('\n'))
        {
            builder.Append('\n');
        }

        builder.Append(block);
        _atomicFile.WriteAllText(path, builder.ToString());
        return true;
    }
}
