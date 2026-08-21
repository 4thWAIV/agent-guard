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

    /// <summary>
    /// Creates a uniquely-named temporary subdirectory under the system temp root and returns its absolute path. It
    /// wraps <c>System.IO.Directory.CreateTempSubdirectory</c> — an atomic, uniquely-named directory write whose
    /// atomicity IS the uniqueness guarantee, so no GUID composition is involved.
    /// </summary>
    /// <param name="prefix">An optional name prefix on the created directory.</param>
    /// <returns>The absolute path of the created temporary subdirectory.</returns>
    string CreateTempSubdirectory(string prefix);
}
