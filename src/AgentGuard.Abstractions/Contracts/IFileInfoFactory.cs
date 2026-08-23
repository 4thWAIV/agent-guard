// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The standalone, dependency-free factory that builds the owned <see cref="IFileInfo"/>/<see cref="IDirectoryInfo"/>
/// wrappers — the single seam a fresh wrapper is constructed through. It is an INTERNAL seam: consumers reach the
/// filesystem through <see cref="IFileSystem"/> (which delegates its per-path factory members here); only the two
/// consumers that cannot route back through <see cref="IFileSystem"/> — the directory enumerator that
/// <see cref="IFileSystem"/> itself holds, and the per-OS classes that sit below it — inject this factory directly.
/// It is never a public consumer entry point.
/// </summary>
public interface IFileInfoFactory
{
    /// <summary>
    /// Builds a fresh <see cref="IFileInfo"/> wrapper for <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A fresh <see cref="IFileInfo"/> for the path.</returns>
    IFileInfo GetFileInfo(string path);

    /// <summary>
    /// Builds a fresh <see cref="IDirectoryInfo"/> wrapper for <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>A fresh <see cref="IDirectoryInfo"/> for the path.</returns>
    IDirectoryInfo GetDirectoryInfo(string path);
}
