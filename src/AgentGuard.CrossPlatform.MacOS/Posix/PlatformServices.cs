// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform.Posix;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The single concrete <see cref="IPlatformServices"/> container for a POSIX OS AND the one composition factory that
/// builds it (container-is-one-class-with-its-own-create): one <c>internal sealed class</c> that implements its
/// contract interface, has a <c>private</c> constructor (Wall 1), and exposes its own parameterless
/// <c>public static IPlatformServices Create()</c> build point — the same self-building shape as
/// <c>SystemServices.Create()</c>. It lives in the per-OS impl assembly, where <see cref="PosixFileSystem"/> is
/// visible, so it builds its own OS-divergent file system with no separate <c>Platform</c> factory and no passed-in
/// service. <c>AgentGuard.Boundaries</c> reaches exactly this one door (AG0029, AG0010 pins the
/// <see cref="IPlatformServices"/> return), so the type is <c>internal</c> with an <c>InternalsVisibleTo</c> grant to
/// Boundaries. This is shared POSIX source: macOS and Linux wire the same <see cref="PosixFileSystem"/>, so this file
/// is linked into the Linux impl unchanged. The Windows impl ships its own <c>PlatformServices.Create()</c>.
/// </summary>
internal sealed class PlatformServices : IPlatformServices
{
    // Private constructor (AG0003, Wall 1): the built OS-divergent file system arrives by constructor injection; only
    // this class's own Create() builds it, so no second container can be assembled to bypass the one door.
    private PlatformServices(IPlatformFileSystem fileSystem) => FileSystem = fileSystem;

    /// <inheritdoc />
    public IPlatformFileSystem FileSystem { get; }

    /// <summary>
    /// Builds the platform-capability container for this POSIX OS. It obtains the OS-uniform owned adapters from the one
    /// <see cref="CrossPlatformAdapters"/> factory, wraps them in the shared OS-uniform helper, injects that helper and
    /// the wrapper factory (the per-OS class reads the OS-uniform symlink target through it) into the per-OS
    /// <see cref="PosixFileSystem"/>, and assembles the container.
    /// </summary>
    /// <returns>The platform services container, as its interface.</returns>
    public static IPlatformServices Create()
    {
        PlatformFileSystemParts parts = PlatformFileSystemComposition.Create();
        return new PlatformServices(PosixFileSystem.Create(parts.Shared, parts.Factory));
    }
}
