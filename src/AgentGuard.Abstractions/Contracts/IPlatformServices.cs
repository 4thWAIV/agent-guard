// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The container of the platform capabilities the engine depends on, obtained once from the per-OS
/// <c>PlatformServices.Create()</c> factory. It exposes one capability today, <see cref="FileSystem"/>, and is designed to
/// grow — future capabilities (for example a biometric/keychain surface) become additional properties here with no
/// restructuring of the factory or its callers.
/// </summary>
public interface IPlatformServices
{
    /// <summary>
    /// Gets the platform's file-system capability.
    /// </summary>
    IPlatformFileSystem FileSystem { get; }

    /// <summary>
    /// Gets the platform's presence capability — the per-OS <see cref="IPresenceCheck"/> the approval gate consumes to
    /// prove a physically-present human before a mutating command proceeds.
    /// </summary>
    IPresenceCheck Presence { get; }
}
