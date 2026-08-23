// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned write side of the filesystem primitive, file writes only. Directory-mutating operations are split
/// off into <see cref="IDirectoryWriter"/> so this interface keeps a single responsibility.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Writes the given bytes to a file, replacing any existing contents.
    /// </summary>
    /// <param name="path">The absolute path to write.</param>
    /// <param name="bytes">The bytes to write.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that completes when the bytes are written.</returns>
    Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken ct);

    /// <summary>
    /// Writes the given text to a file, replacing any existing contents.
    /// </summary>
    /// <param name="path">The absolute path to write.</param>
    /// <param name="contents">The text to write.</param>
    void WriteAllText(string path, string contents);

    /// <summary>
    /// Copies a file from <paramref name="source"/> to <paramref name="destination"/>.
    /// </summary>
    /// <param name="source">The absolute source path.</param>
    /// <param name="destination">The absolute destination path.</param>
    /// <param name="overwrite"><see langword="false"/> (the default) throws when a file already exists at
    /// <paramref name="destination"/>; <see langword="true"/> replaces it.</param>
    void Copy(string source, string destination, bool overwrite = false);

    /// <summary>
    /// Moves a file from <paramref name="source"/> to <paramref name="destination"/>.
    /// </summary>
    /// <param name="source">The absolute source path.</param>
    /// <param name="destination">The absolute destination path.</param>
    /// <param name="overwrite"><see langword="false"/> (the default) throws when a file already exists at
    /// <paramref name="destination"/>; <see langword="true"/> replaces it.</param>
    void Move(string source, string destination, bool overwrite = false);

    /// <summary>
    /// Deletes the file at the given path.
    /// </summary>
    /// <param name="path">The absolute path to delete.</param>
    void DeleteFile(string path);
}
