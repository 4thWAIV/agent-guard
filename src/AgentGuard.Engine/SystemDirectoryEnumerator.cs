// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.IO;

namespace AgentGuard.Engine;

/// <summary>
/// The production <see cref="IDirectoryEnumerator"/>: it owns the raw <c>DirectoryInfo</c> walk and the
/// <see cref="EnumerationOptions.IgnoreInaccessible"/>-<see langword="false"/> fail-closed behavior, so an
/// inaccessible directory throws rather than being silently skipped. It is the only place in the scanner path that
/// touches <c>System.IO</c> filesystem types; the scanner keeps its policy over <see cref="DirectoryChild"/>.
/// </summary>
internal sealed class SystemDirectoryEnumerator : IDirectoryEnumerator
{
    private static readonly EnumerationOptions WalkOptions = new()
    {
        IgnoreInaccessible = false,
        AttributesToSkip = FileAttributes.None,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public IReadOnlyList<DirectoryChild> EnumerateChildren(string directoryPath)
    {
        // Materialize eagerly so an inaccessible directory throws at this call (fail closed), not lazily mid-walk
        // where the exception could be missed.
        var children = new List<DirectoryChild>();
        foreach (FileSystemInfo entry in new DirectoryInfo(directoryPath).EnumerateFileSystemInfos("*", WalkOptions))
        {
            children.Add(new DirectoryChild(
                entry.FullName,
                entry is DirectoryInfo,
                entry.Attributes.HasFlag(FileAttributes.ReparsePoint)));
        }

        return children;
    }

    /// <summary>
    /// Creates the production directory enumerator.
    /// </summary>
    /// <returns>The enumerator, as its interface.</returns>
    internal static IDirectoryEnumerator Create() => new SystemDirectoryEnumerator();
}
