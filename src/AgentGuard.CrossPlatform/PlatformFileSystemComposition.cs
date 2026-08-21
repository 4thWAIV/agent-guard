// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The one composition helper that builds the OS-uniform parts every per-OS <c>PlatformServices.Create()</c> assembles
/// its OS-divergent file system from — the shared OS-uniform file-operation helper and the wrapper factory — so that
/// block is written once here instead of copied into each per-OS container. Each per-OS <c>Create()</c> then differs
/// only in the one line that builds its own <c>PosixFileSystem</c>/<c>WindowsFileSystem</c> from these parts. It obtains
/// the owned adapters from the single <see cref="CrossPlatformAdapters"/> factory, wraps them in the shared
/// <see cref="PlatformFileSystemShared"/> helper, and builds the wrapper <see cref="IFileInfoFactory"/>; it constructs
/// no OS-divergent primitive of its own.
/// </summary>
internal static class PlatformFileSystemComposition
{
    /// <summary>
    /// Builds the OS-uniform parts a per-OS <see cref="IPlatformFileSystem"/> is assembled from: the shared OS-uniform
    /// file-operation helper wired to the owned adapters, and the wrapper factory the per-OS class reads the OS-uniform
    /// symlink target through.
    /// </summary>
    /// <returns>The shared OS-uniform helper and the wrapper factory the per-OS file system is built from.</returns>
    internal static PlatformFileSystemParts Create()
    {
        CrossPlatformAdapters adapters = CrossPlatformAdapters.Create();
        var shared = new PlatformFileSystemShared(
            adapters.FileReader,
            adapters.Directories,
            adapters.FileWriter,
            adapters.DirectoryWriter,
            adapters.Random);

        return new PlatformFileSystemParts(shared, FileInfoFactory.Create());
    }
}
