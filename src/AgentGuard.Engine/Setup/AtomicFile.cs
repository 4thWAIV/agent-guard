// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Setup;

/// <summary>
/// Writes a regular file atomically by writing a temporary sibling and renaming it over the destination, so a
/// reader never sees a half-written record. The temporary file is created in the destination directory so the
/// rename stays on one filesystem. It writes through the owned <see cref="IFileWriter"/> and
/// <see cref="IDirectoryWriter"/> and names the temporary sibling with the owned <see cref="IRandomGenerator"/>, so no
/// raw filesystem or randomness call lives here; it is built from the container at the composition point.
/// </summary>
internal sealed class AtomicFile
{
    private readonly IFileWriter _fileWriter;
    private readonly IDirectoryWriter _directoryWriter;
    private readonly IRandomGenerator _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomicFile"/> class over the owned write services.
    /// </summary>
    /// <param name="fileWriter">The owned file-write side of the filesystem.</param>
    /// <param name="directoryWriter">The owned directory-write side of the filesystem.</param>
    /// <param name="random">The owned random generator used to name the temporary sibling.</param>
    internal AtomicFile(IFileWriter fileWriter, IDirectoryWriter directoryWriter, IRandomGenerator random)
    {
        _fileWriter = fileWriter;
        _directoryWriter = directoryWriter;
        _random = random;
    }

    /// <summary>
    /// Builds an atomic-file writer from a setup context's owned services.
    /// </summary>
    /// <param name="context">The setup context carrying the owned write services.</param>
    /// <returns>The atomic-file writer.</returns>
    internal static AtomicFile For(SetupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new AtomicFile(context.FileWriter, context.DirectoryWriter, context.Random);
    }

    /// <summary>
    /// Atomically writes text to a file, creating parent directories.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <param name="content">The text content.</param>
    internal void WriteAllText(string path, string content) =>
        WriteAtomically(path, temporaryPath => _fileWriter.WriteAllText(temporaryPath, content));

    /// <summary>
    /// Atomically writes bytes to a file, creating parent directories, without blocking on the write.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <param name="bytes">The byte content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the file has been written and renamed into place.</returns>
    internal async Task WriteAllBytesAsync(
        string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        string temporaryPath = BeginWrite(path);
        await _fileWriter.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken).ConfigureAwait(false);
        CommitWrite(temporaryPath, path);
    }

    /// <summary>
    /// Atomically copies a source file over a destination, creating parent directories. The source is never
    /// moved and the destination is replaced by a rename.
    /// </summary>
    /// <param name="sourcePath">The source file.</param>
    /// <param name="destinationPath">The destination file.</param>
    internal void CopyOver(string sourcePath, string destinationPath) =>
        WriteAtomically(destinationPath, temporaryPath => _fileWriter.Copy(sourcePath, temporaryPath, overwrite: true));

    private void WriteAtomically(string path, Action<string> writeTemporary)
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
    private string BeginWrite(string path)
    {
        _directoryWriter.CreateDirectory(Path.GetDirectoryName(path)!);
        return _random.TemporarySiblingPath(path);
    }

    /// <summary>
    /// Finalizes an atomic write by renaming the temporary sibling over the destination.
    /// </summary>
    /// <param name="temporaryPath">The written temporary sibling.</param>
    /// <param name="path">The destination path.</param>
    private void CommitWrite(string temporaryPath, string path) =>
        _fileWriter.Move(temporaryPath, path, overwrite: true);
}
