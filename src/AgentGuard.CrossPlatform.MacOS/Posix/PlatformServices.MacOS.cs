// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform.MacOS;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The macOS-only fragment of the shared POSIX <see cref="PlatformServices"/> container: the per-OS presence check the
/// shared <c>Create()</c> declares as a partial method. It is compiled ONLY into the macOS project — never link-shared
/// into the Linux build, which supplies its own polkit presence check in <c>PlatformServices.Linux.cs</c>
/// (per-os-presence-impls-not-shared). It wires the macOS <see cref="MacOsPresenceCheck"/> over its native
/// <see cref="LocalAuthentication"/> port.
/// </summary>
internal sealed partial class PlatformServices
{
    private static partial IPresenceCheck CreatePresence() =>
        MacOsPresenceCheck.Create(new LocalAuthentication(new ObjCRuntime()));
}
