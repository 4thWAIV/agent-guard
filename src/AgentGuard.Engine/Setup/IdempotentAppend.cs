// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Text;

namespace AgentGuard.Setup;

/// <summary>
/// Appends a block to a text file exactly once. The caller decides, from the file's current content, which block
/// (if any) is missing; this owns the shared mechanism: read-or-empty, a guaranteed blank-line separation before
/// the block, and the atomic write.
/// </summary>
internal static class IdempotentAppend
{
    /// <summary>
    /// Appends the block that <paramref name="buildBlock"/> returns for the file's current content, or does
    /// nothing when it returns <see langword="null"/>.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="buildBlock">Given the current content, returns the block to append, or <see langword="null"/>
    /// when nothing is missing.</param>
    /// <returns><see langword="true"/> when a block was appended; <see langword="false"/> when nothing was.</returns>
    internal static bool Ensure(string path, Func<string, string?> buildBlock)
    {
        string existing = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
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
        AtomicFile.WriteAllText(path, builder.ToString());
        return true;
    }
}
