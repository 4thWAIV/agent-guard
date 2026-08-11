// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Windows;

namespace AgentGuard.CrossPlatform;

/// <summary>
/// The Windows entry point that hands callers the platform-capability container. It exposes the same
/// <c>AgentGuard.CrossPlatform.Platform.Create()</c> signature as the POSIX impl, so calling code is identical and
/// OS-agnostic; a consumer references exactly one per-OS impl (selected by RuntimeIdentifier in its csproj). Windows
/// wires its own <see cref="WindowsFileSystem"/>. The OS-uniform services container itself is owned once in the
/// contract assembly and obtained through
/// <see cref="PlatformFileSystemShared.CreateServices(IPlatformFileSystem)"/>. The type name does not match the
/// namespace tail (<c>CrossPlatform</c>), so no type-name analyzer suppression is needed.
/// </summary>
public static class Platform
{
    /// <summary>
    /// Creates the platform-capability container for Windows.
    /// </summary>
    /// <returns>The platform services container.</returns>
    public static IPlatformServices Create() => PlatformFileSystemShared.CreateServices(new WindowsFileSystem());
}
