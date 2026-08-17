// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// Read-only access to the working tree, used by Capture to snapshot a protected file's current bytes and by
/// Postcheck to read what landed on disk. It is deliberately read-only: the privileged writes that execute a
/// restore or delete are the Engine's alone, kept out of this Guard-facing seam. It owns the read side of the
/// filesystem primitive: every existence, byte, text, attribute, and last-write read routes through here.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Determines whether a file exists at the given path.
    /// </summary>
    /// <param name="path">The absolute path to test.</param>
    /// <returns><see langword="true"/> when a file exists at the path; otherwise <see langword="false"/>.</returns>
    bool Exists(string path);

    /// <summary>
    /// Reads the full contents of a file as bytes. Mirrors <see cref="System.IO.File.ReadAllBytes(string)"/>.
    /// </summary>
    /// <param name="path">The absolute path to read.</param>
    /// <returns>The file's bytes.</returns>
    byte[] ReadAllBytes(string path);

    /// <summary>
    /// Asynchronously reads the full contents of a file as bytes. Mirrors <see cref="System.IO.File.ReadAllBytesAsync(string, CancellationToken)"/>.
    /// </summary>
    /// <param name="path">The absolute path to read.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that completes with the file's bytes.</returns>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct);

    /// <summary>
    /// Reads the full contents of a file as text. Mirrors <see cref="System.IO.File.ReadAllText(string)"/>.
    /// </summary>
    /// <param name="path">The absolute path to read.</param>
    /// <returns>The file's contents decoded as text.</returns>
    string ReadAllText(string path);

    /// <summary>
    /// Asynchronously reads the full contents of a file as text. Mirrors <see cref="System.IO.File.ReadAllTextAsync(string, CancellationToken)"/>.
    /// </summary>
    /// <param name="path">The absolute path to read.</param>
    /// <param name="ct">A token that cancels the operation.</param>
    /// <returns>A task that completes with the file's contents decoded as text.</returns>
    Task<string> ReadAllTextAsync(string path, CancellationToken ct);

    /// <summary>
    /// Reads the attributes of the file at the given path — the link-kind read the Windows symlink delete needs.
    /// </summary>
    /// <param name="path">The absolute path whose attributes are read.</param>
    /// <returns>The file's attributes.</returns>
    FileAttributes GetAttributes(string path);

    /// <summary>
    /// Reads the file's last-write time in UTC, used by the setup condition checks.
    /// </summary>
    /// <param name="path">The absolute path whose last-write time is read.</param>
    /// <returns>The file's last-write time, in UTC.</returns>
    DateTimeOffset GetLastWriteTimeUtc(string path);
}
