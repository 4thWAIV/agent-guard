// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The single container of the owned OS/CLR services, built once at composition and threaded down by constructor
/// injection. It is the only legal way to reach a boundary service; the platform capabilities arrive through
/// <see cref="Platform"/>, whose <c>PlatformServices.Create()</c> factory yields the <see cref="IPlatformServices"/>
/// container.
/// </summary>
public interface ISystemServices
{
    /// <summary>
    /// Gets the single filesystem entry point — the owned filesystem surface (per-path <c>*Info</c> factories and the
    /// read/write/enumerate service accessors) reached through one service.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// Gets the OS-environment read service.
    /// </summary>
    IEnvironment Environment { get; }

    /// <summary>
    /// Gets the randomness generator — the one seam through which a GUID or a random file name enters the codebase.
    /// </summary>
    IRandomGenerator Random { get; }

    /// <summary>
    /// Gets the console service.
    /// </summary>
    IConsole Console { get; }

    /// <summary>
    /// Gets the platform capability container; the OS-divergent file system is <c>Platform.FileSystem</c>.
    /// </summary>
    IPlatformServices Platform { get; }

    /// <summary>
    /// Gets the Ed25519 grant-signature service.
    /// </summary>
    ISignatureService Signatures { get; }

    /// <summary>
    /// Gets the running build's version metadata.
    /// </summary>
    IBuildInfo BuildInfo { get; }

    /// <summary>
    /// Gets the injected clock. <c>TimeProvider.System</c> is legal only at the composition point.
    /// </summary>
    TimeProvider Clock { get; }
}
