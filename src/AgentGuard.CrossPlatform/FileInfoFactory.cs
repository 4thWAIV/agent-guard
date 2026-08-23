// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The standalone, dependency-free <see cref="IFileInfoFactory"/> — the ONE place the owned
/// <see cref="AbstractedFileInfo"/>/<see cref="AbstractedDirectoryInfo"/> wrappers are constructed (AG0033 pins the
/// wrapper factories here). <see cref="IFileSystem"/> delegates its per-path factory members to this seam, and the two
/// consumers that cannot route back through <see cref="IFileSystem"/> — the directory enumerator and the per-OS
/// classes — inject this factory directly. It holds no state, so it can be built wherever it is needed. It is
/// <c>internal sealed</c> with a <c>private</c> constructor (Wall 1) and handed out only as its interface.
/// </summary>
internal sealed class FileInfoFactory : IFileInfoFactory
{
    // Private constructor (AG0003, Wall 1): only this class's own factory constructs it.
    private FileInfoFactory()
    {
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string path) => AbstractedFileInfo.Create(path);

    /// <inheritdoc />
    public IDirectoryInfo GetDirectoryInfo(string path) => AbstractedDirectoryInfo.Create(path);

    /// <summary>
    /// Creates the wrapper factory.
    /// </summary>
    /// <returns>The wrapper factory, as its interface.</returns>
    internal static IFileInfoFactory Create() => new FileInfoFactory();
}
