// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    /// Atomically writes bytes to a file, creating parent directories, without blocking on the write.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <param name="bytes">The byte content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the file has been written and renamed into place.</returns>
    internal static async Task WriteAllBytesAsync(
        string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        string temporaryPath = BeginWrite(path);
        await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken).ConfigureAwait(false);
        CommitWrite(temporaryPath, path);
    }

    /// <summary>
    /// Builds the temporary-sibling path — a unique <c>.tmp-</c> name beside the destination — for these atomic
    /// write-and-rename operations. It delegates to the single shared recipe,
    /// <see cref="AgentGuard.CrossPlatform.PlatformFileSystemShared.TemporarySiblingPath(string)"/>, which the
    /// per-OS atomic symlink swap also calls, so the recipe is spelled once.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <returns>The temporary sibling path in the destination's directory.</returns>
    internal static string TemporarySiblingPath(string path) =>
        AgentGuard.CrossPlatform.PlatformFileSystemShared.TemporarySiblingPath(path);

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
        string temporaryPath = BeginWrite(path);
        writeTemporary(temporaryPath);
        CommitWrite(temporaryPath, path);
    }

    /// <summary>
    /// Prepares an atomic write: creates the destination directory and returns the temporary sibling to write to.
    /// The create-directory-then-temp step is spelled once here so no atomic writer re-implements the recipe.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <returns>The temporary sibling path to write to.</returns>
    private static string BeginWrite(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return TemporarySiblingPath(path);
    }

    /// <summary>
    /// Finalizes an atomic write by renaming the temporary sibling over the destination.
    /// </summary>
    /// <param name="temporaryPath">The written temporary sibling.</param>
    /// <param name="path">The destination path.</param>
    private static void CommitWrite(string temporaryPath, string path) =>
        File.Move(temporaryPath, path, overwrite: true);
}
