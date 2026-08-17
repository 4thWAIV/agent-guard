// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// Read-only directory access: the owned seam over the OS filesystem walk that the protected-file scanner and the
/// grant-token reader use. Directory enumeration through the BCL is OS-uniform, so this is an OS-uniform boundary
/// abstraction — the caller passes the <see cref="EnumerationOptions"/> it needs, and the owner does not pick
/// behavior for it. None of the enumerating members guarantee any ordering; each returns whatever the native
/// enumeration yields, with no reordering applied.
/// </summary>
public interface IDirectoryEnumerator
{
    /// <summary>
    /// Determines whether a directory exists at the given path.
    /// </summary>
    /// <param name="path">The absolute directory path.</param>
    /// <returns><see langword="true"/> when the directory exists.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Returns the immediate children of one directory. An inaccessible directory throws
    /// (<see cref="System.IO.IOException"/> / <see cref="System.UnauthorizedAccessException"/>) and is never
    /// silently skipped, so an incomplete walk denies the call rather than hiding a protected file. No ordering is
    /// guaranteed.
    /// </summary>
    /// <param name="directoryPath">The absolute directory path whose children are listed.</param>
    /// <returns>The directory's immediate children, in no guaranteed order.</returns>
    IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath);

    /// <summary>
    /// Lists the files in a directory matching a pattern, mirroring the BCL — the caller passes the
    /// <see cref="EnumerationOptions"/> it needs (fail-closed behavior is the caller's obligation). No ordering is
    /// guaranteed.
    /// </summary>
    /// <param name="directoryPath">The absolute directory path to search.</param>
    /// <param name="pattern">The search pattern to match file names against.</param>
    /// <param name="options">The enumeration options the caller requires.</param>
    /// <returns>The matching file paths, in no guaranteed order.</returns>
    IReadOnlyList<string> EnumerateFiles(string directoryPath, string pattern, EnumerationOptions options);

    /// <summary>
    /// Lists the subdirectories in a directory matching a pattern, mirroring the BCL — the caller passes the
    /// <see cref="EnumerationOptions"/> it needs (fail-closed behavior is the caller's obligation). No ordering is
    /// guaranteed.
    /// </summary>
    /// <param name="directoryPath">The absolute directory path to search.</param>
    /// <param name="pattern">The search pattern to match directory names against.</param>
    /// <param name="options">The enumeration options the caller requires.</param>
    /// <returns>The matching subdirectory paths, in no guaranteed order.</returns>
    IReadOnlyList<string> EnumerateDirectories(string directoryPath, string pattern, EnumerationOptions options);

    /// <summary>
    /// Reads the directory's last-write time in UTC.
    /// </summary>
    /// <param name="path">The absolute directory path whose last-write time is read.</param>
    /// <returns>The directory's last-write time, in UTC.</returns>
    DateTimeOffset GetLastWriteTimeUtc(string path);
}
