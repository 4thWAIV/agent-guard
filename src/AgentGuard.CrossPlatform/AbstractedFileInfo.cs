// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The owned <see cref="IFileInfo"/> wrapper. It is the single class that implements <see cref="IFileInfo"/>, so it
/// is the ONE place a raw <see cref="FileInfo"/> may be constructed (AG0011 exempts exactly this owner class in this
/// assembly). Every other site reaches a file entry through <see cref="IFileSystem.GetFileInfo(string)"/>. Its
/// <see cref="IFileSystemInfo"/> surface (<c>FullName</c>/<c>Attributes</c>/<c>LinkTarget</c>) is inherited from the
/// shared <see cref="AbstractedFileSystemInfoForwarder"/>, which forwards to the one abstracted core built here, so the
/// forwarding block is not copied. It is <c>internal sealed</c> with a <c>private</c> constructor (Wall 1); the only
/// thing that constructs it is <see cref="FileInfoFactory"/>, through <see cref="Create"/> (AG0033).
/// </summary>
internal sealed class AbstractedFileInfo : AbstractedFileSystemInfoForwarder, IFileInfo
{
    // Private constructor (AG0003, Wall 1). The raw FileInfo is constructed for this wrapper (legal only inside this
    // owner class) and wrapped once as the abstracted core the shared forwarder exposes.
    private AbstractedFileInfo(FileInfo info)
        : base(AbstractedFileSystemInfo.Wrap(info))
    {
    }

    /// <summary>
    /// Builds a fresh <see cref="IFileInfo"/> for <paramref name="path"/>. Called only by <see cref="FileInfoFactory"/>
    /// (AG0033), so every caller stays on the mockable factory seam.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A fresh <see cref="IFileInfo"/> for the path, as its interface.</returns>
    internal static IFileInfo Create(string path) => new AbstractedFileInfo(new FileInfo(path));
}
