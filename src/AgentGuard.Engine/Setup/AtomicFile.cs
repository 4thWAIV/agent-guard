// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// Writes a regular file atomically by writing a temporary sibling and renaming it over the destination, so a
/// reader never sees a half-written record. The temporary file is created in the destination directory so the
/// rename stays on one filesystem.
/// </summary>
internal static class AtomicFile
{
    /// <summary>
    /// Atomically writes text to a file, creating parent directories.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <param name="content">The text content.</param>
    internal static void WriteAllText(string path, string content) =>
        WriteAtomically(path, temporaryPath => File.WriteAllText(temporaryPath, content));

    /// <summary>
    /// Atomically writes bytes to a file, creating parent directories.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <param name="bytes">The byte content.</param>
    internal static void WriteAllBytes(string path, byte[] bytes) =>
        WriteAtomically(path, temporaryPath => File.WriteAllBytes(temporaryPath, bytes));

    /// <summary>
    /// Atomically copies a source file over a destination, creating parent directories. The source is never
    /// moved and the destination is replaced by a rename.
    /// </summary>
    /// <param name="sourcePath">The source file.</param>
    /// <param name="destinationPath">The destination file.</param>
    internal static void CopyOver(string sourcePath, string destinationPath) =>
        WriteAtomically(destinationPath, temporaryPath => File.Copy(sourcePath, temporaryPath, overwrite: true));

    private static void WriteAtomically(string path, Action<string> writeTemporary)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(
            directory,
            Path.GetFileName(path) + ".tmp-" + Guid.NewGuid().ToString("N"));
        writeTemporary(temporaryPath);
        File.Move(temporaryPath, path, overwrite: true);
    }
}
