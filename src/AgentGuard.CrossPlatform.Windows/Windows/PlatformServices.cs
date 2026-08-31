// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform.Windows;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The single concrete <see cref="IPlatformServices"/> container for Windows AND the one composition factory that
/// builds it (container-is-one-class-with-its-own-create): one <c>internal sealed class</c> that implements its
/// contract interface, has a <c>private</c> constructor (Wall 1), and exposes its own parameterless
/// <c>public static IPlatformServices Create()</c> build point — the same self-building shape as
/// <c>SystemServices.Create()</c>. It lives in the per-OS impl assembly, where <see cref="WindowsFileSystem"/> is
/// visible, so it builds its own OS-divergent file system with no separate <c>Platform</c> factory and no passed-in
/// service. <c>AgentGuard.Boundaries</c> reaches exactly this one door (AG0029, AG0010 pins the
/// <see cref="IPlatformServices"/> return), so the type is <c>internal</c> with an <c>InternalsVisibleTo</c> grant to
/// Boundaries. Windows wires its own <see cref="WindowsFileSystem"/> (it does not link the POSIX source).
/// </summary>
internal sealed class PlatformServices : IPlatformServices
{
    // Private constructor (AG0003, Wall 1): the built OS-divergent file system and the Windows presence check arrive by
    // constructor injection; only this class's own Create() builds them, so no second container can be assembled to
    // bypass the one door.
    private PlatformServices(IPlatformFileSystem fileSystem, IPresenceCheck presence)
    {
        FileSystem = fileSystem;
        Presence = presence;
    }

    /// <inheritdoc />
    public IPlatformFileSystem FileSystem { get; }

    /// <inheritdoc />
    public IPresenceCheck Presence { get; }

    /// <summary>
    /// Builds the platform-capability container for Windows. It obtains the OS-uniform owned adapters from the one
    /// <see cref="CrossPlatformAdapters"/> factory, wraps them in the shared OS-uniform helper, injects that helper and
    /// the wrapper factory (the per-OS class reads the OS-uniform symlink target through it) into the per-OS
    /// <see cref="WindowsFileSystem"/>, and assembles the container with the Windows presence check.
    /// </summary>
    /// <returns>The platform services container, as its interface.</returns>
    public static IPlatformServices Create()
    {
        PlatformFileSystemParts parts = PlatformFileSystemComposition.Create();
        return new PlatformServices(
            WindowsFileSystem.Create(parts.Shared, parts.Factory),
            WindowsPresenceCheck.Create(
                new WindowsUserPresence(new WindowsHelloNativeOps(), new CredentialPromptNativeOps())));
    }
}
