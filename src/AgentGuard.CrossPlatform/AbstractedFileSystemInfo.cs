// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IFileSystemInfo"/> view over one directory child that a single walk already materialized. It
/// is the class that implements the <see cref="IFileSystemInfo"/> base, so — like the <c>*Info</c> wrappers — it is an
/// owner of the raw <see cref="FileSystemInfo"/> members it reads (AG0011), and it never re-stats the entry.
/// <see cref="AbstractedDirectoryInfo.EnumerateFileSystemInfos"/> wraps each entry from its one pass in one of these.
/// It is <c>internal sealed</c> with a <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class AbstractedFileSystemInfo : IFileSystemInfo
{
    private readonly FileSystemInfo _entry;

    // Private constructor (AG0003, Wall 1): handed out only through the static factory below.
    private AbstractedFileSystemInfo(FileSystemInfo entry) => _entry = entry;

    /// <inheritdoc />
    public string FullName => _entry.FullName;

    /// <inheritdoc />
    public FileAttributes Attributes => _entry.Attributes;

    /// <inheritdoc />
    public string? LinkTarget => _entry.LinkTarget;

    /// <summary>
    /// Wraps one already-materialized child entry as its owned view. The <paramref name="entry"/> is a
    /// <see cref="FileSystemInfo"/> (a BCL type, not a service), and the return is the <see cref="IFileSystemInfo"/>
    /// base (not an AG0033 wrapper interface), so this is neither a service-parameter site nor a wrapper-construction
    /// site.
    /// </summary>
    /// <param name="entry">The already-materialized child entry from the single directory walk.</param>
    /// <returns>The owned view over the entry, as its interface.</returns>
    internal static IFileSystemInfo Wrap(FileSystemInfo entry) => new AbstractedFileSystemInfo(entry);
}
