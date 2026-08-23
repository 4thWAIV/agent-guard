// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IDirectoryInfo"/> wrapper. It is the single class that implements <see cref="IDirectoryInfo"/>,
/// so it is the ONE place a raw <see cref="DirectoryInfo"/> may be constructed and enumerated (AG0011 exempts exactly
/// this owner class in this assembly). Every other site reaches a directory entry through
/// <see cref="IFileSystem.GetDirectoryInfo(string)"/>. Its <see cref="IFileSystemInfo"/> surface
/// (<c>FullName</c>/<c>Attributes</c>/<c>LinkTarget</c>) is inherited from the shared
/// <see cref="AbstractedFileSystemInfoForwarder"/>, which forwards to the one abstracted core built here, so the
/// forwarding block is not copied. It is <c>internal sealed</c> with a <c>private</c> constructor (Wall 1); the only
/// thing that constructs it is <see cref="FileInfoFactory"/>, through <see cref="Create"/> (AG0033).
/// </summary>
internal sealed class AbstractedDirectoryInfo : AbstractedFileSystemInfoForwarder, IDirectoryInfo
{
    private readonly DirectoryInfo _directory;

    // Private constructor (AG0003, Wall 1). The raw DirectoryInfo is constructed for this wrapper (legal only inside
    // this owner class); it is held for the one directory walk and wrapped once as the abstracted core the shared
    // forwarder exposes the FullName/Attributes/LinkTarget surface through.
    private AbstractedDirectoryInfo(DirectoryInfo directory)
        : base(AbstractedFileSystemInfo.Wrap(directory)) => _directory = directory;

    /// <inheritdoc />
    public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos()
    {
        // One pass over the real DirectoryInfo's children (a raw DirectoryInfo member, legal only in this owner
        // class), yielded lazily one at a time as owned IFileSystemInfo views over the already-materialized entries —
        // no re-stat and no second walk. The consumer materializes for fail-closed behavior.
        foreach (FileSystemInfo entry in _directory.EnumerateFileSystemInfos())
        {
            yield return AbstractedFileSystemInfo.Wrap(entry);
        }
    }

    /// <summary>
    /// Builds a fresh <see cref="IDirectoryInfo"/> for <paramref name="path"/>. Called only by
    /// <see cref="FileInfoFactory"/> (AG0033), so every caller stays on the mockable factory seam.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>A fresh <see cref="IDirectoryInfo"/> for the path, as its interface.</returns>
    internal static IDirectoryInfo Create(string path) => new AbstractedDirectoryInfo(new DirectoryInfo(path));
}
