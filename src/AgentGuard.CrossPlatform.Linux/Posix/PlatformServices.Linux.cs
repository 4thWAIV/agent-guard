// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform.Linux;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The Linux-only fragment of the shared POSIX <see cref="PlatformServices"/> container: the per-OS presence check the
/// shared <c>Create()</c> declares as a partial method. It lives in the Linux project (which link-shares the base
/// <c>PlatformServices.cs</c> but NOT the macOS fragment), so the Linux build supplies its own polkit presence check
/// (per-os-presence-impls-not-shared). It wires the Linux <see cref="LinuxPresenceCheck"/> over its
/// <see cref="TmdsPolkitAuthority"/> port.
/// </summary>
internal sealed partial class PlatformServices
{
    private static partial IPresenceCheck CreatePresence() => LinuxPresenceCheck.Create(new TmdsPolkitAuthority());
}
