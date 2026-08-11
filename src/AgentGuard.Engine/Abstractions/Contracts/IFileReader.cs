// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions.Contracts;

/// <summary>
/// Read-only access to the working tree, used by Capture to snapshot a protected file's current bytes and by
/// Postcheck to read what landed on disk. It is deliberately read-only: the privileged writes that execute a
/// restore or delete are the Engine's alone, kept out of this Guard-facing seam.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Reads the full contents of a file.
    /// </summary>
    /// <param name="path">The absolute path to read.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the file's bytes.</returns>
    Task<ReadOnlyMemory<byte>> ReadAsync(string path, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether a file exists at the given path.
    /// </summary>
    /// <param name="path">The absolute path to test.</param>
    /// <returns><see langword="true"/> when a file exists at the path; otherwise <see langword="false"/>.</returns>
    bool Exists(string path);
}
