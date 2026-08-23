// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The one shared home of the OS-uniform symlink-target read (<see cref="IPlatformFileSystem.ReadLinkTarget"/> and the
/// <see cref="IPlatformFileSystem.IsLinkTarget"/> derived from it), so it is written once instead of copied into every
/// per-OS <see cref="IPlatformFileSystem"/> implementation. Reading a link's raw target goes through the abstracted
/// <see cref="IFileSystemInfo.LinkTarget"/> and is identical on every OS; only the link WRITES (the POSIX rename, the
/// Windows in-place reparse re-point) are OS-divergent and stay in the per-OS classes. The per-OS subclass supplies the
/// wrapper <see cref="Factory"/> it injects directly (fileinfo-factory-breaks-the-cycle), so the factory is held only
/// by the per-OS class and never by this base; the base declares the two members once and each subclass's
/// <see cref="IPlatformFileSystem"/> surface is satisfied by these inherited members. It reads only the abstracted
/// <c>*Info</c> seam, never a raw <c>*Info</c>, so it holds no OS primitive.
/// </summary>
internal abstract class PlatformFileSystemBase
{
    /// <summary>
    /// Gets the wrapper factory the OS-uniform symlink-target read obtains <c>*Info</c> views through, supplied by the
    /// per-OS subclass that injects it directly. Held by the subclass, never by this base.
    /// </summary>
    protected abstract IFileInfoFactory Factory { get; }

    /// <inheritdoc cref="IPlatformFileSystem.IsLinkTarget(string)" />
    public bool IsLinkTarget(string linkPath) => ReadLinkTarget(linkPath) is not null;

    /// <inheritdoc cref="IPlatformFileSystem.ReadLinkTarget(string)" />
    public string? ReadLinkTarget(string linkPath)
    {
        try
        {
            // IFileSystemInfo.LinkTarget is the raw, unresolved target and is null for a non-link (a regular file, a
            // directory, or an absent path) — exactly the contract's null case. The *Info view is obtained through the
            // injected factory seam, so this class holds no raw *Info.
            return Factory.GetFileInfo(linkPath).LinkTarget ?? Factory.GetDirectoryInfo(linkPath).LinkTarget;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
