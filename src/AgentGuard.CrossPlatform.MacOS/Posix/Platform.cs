// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Posix;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The entry point that hands callers the platform-capability container for the OS this assembly was built for.
/// Every per-OS impl assembly exposes this same <c>AgentGuard.CrossPlatform.Platform.Create()</c> signature, and a
/// consumer references exactly one of them (selected by RuntimeIdentifier in its csproj), so calling code stays
/// identical and OS-agnostic. This factory is shared POSIX source: macOS and Linux wire the same
/// <see cref="PosixFileSystem"/>, so this file is linked into the Linux impl unchanged. The OS-uniform services
/// container itself is owned once in the contract assembly and obtained through
/// <see cref="PlatformFileSystemShared.CreateServices(IPlatformFileSystem)"/>. The Windows impl ships its own
/// <c>Platform.Create()</c> wiring its own file-system implementation. The type name does not match the namespace
/// tail (<c>CrossPlatform</c>), so no type-name analyzer suppression is needed.
/// </summary>
public static class Platform
{
    /// <summary>
    /// Creates the platform-capability container for the current OS.
    /// </summary>
    /// <returns>The platform services container.</returns>
    public static IPlatformServices Create() => PlatformFileSystemShared.CreateServices(new PosixFileSystem());
}
