// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The owned directory-write side of the filesystem primitive, split off from <see cref="IFileWriter"/> so the
/// production interface completes the set the setup and sweep paths need end to end.
/// </summary>
public interface IDirectoryWriter
{
    /// <summary>
    /// Creates the directory at the given path, including any missing parent directories.
    /// </summary>
    /// <param name="path">The absolute directory path to create.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Deletes the directory at the given path.
    /// </summary>
    /// <param name="path">The absolute directory path to delete.</param>
    /// <param name="recursive"><see langword="true"/> to delete the directory and its contents;
    /// <see langword="false"/> to delete only an empty directory.</param>
    void DeleteDirectory(string path, bool recursive);

    /// <summary>
    /// Sets the directory's last-write time in UTC.
    /// </summary>
    /// <param name="path">The absolute directory path whose last-write time is set.</param>
    /// <param name="time">The last-write time to set, in UTC.</param>
    void SetLastWriteTimeUtc(string path, DateTimeOffset time);
}
