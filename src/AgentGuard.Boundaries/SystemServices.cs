// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.CrossPlatform;

namespace AgentGuard.Boundaries;

/// <summary>
/// The single concrete <see cref="ISystemServices"/> container AND the one composition factory that builds it
/// (single-construction-point). It holds the owned OS/CLR services and nothing else, arriving by constructor
/// injection at the one composition point; its constructor is <c>private</c> (Wall 1,
/// containers-are-locked-classes-not-records), so no second container can be built inside the assembly to bypass the
/// one factory. <see cref="Create"/> is pinned by AG0017 to the two legal composition callers (the CLI's
/// <c>Program</c> method and the test <c>SystemServicesBuilder</c>); it reads <c>TimeProvider.System</c> itself
/// (legal only here and in the builder, AG0015) and builds every owned service by hand.
/// </summary>
/// <remarks>
/// This is the one mandated container shape (container-is-one-class-with-its-own-create): ONE locked class that
/// implements its contract interface, has a <c>private</c> constructor, and exposes its own
/// <c>public static ISystemServices Create()</c> build point — never a separate factory type and never a passed-in
/// service. The earlier two-type sketch (a separate <c>SystemServicesContainer</c> impl plus a <c>SystemServices</c>
/// factory) is superseded: a separate private-constructor impl cannot be constructed by a separate factory under the
/// frozen rules (a factory returning <see cref="ISystemServices"/> is pinned to <c>Program</c>/the builder, AG0017; a
/// factory returning the concrete container exposes it, AG0004/AG0006; a service-typed factory parameter is forbidden,
/// AG0031; a private constructor is unreachable from a sibling class), so the one locked class IS the pattern.
/// </remarks>
internal sealed class SystemServices : ISystemServices
{
    // Private constructor (AG0003, Wall 1): every service arrives by constructor injection at the one composition
    // point; nothing else constructs an OS-service container.
    private SystemServices(
        IFileSystem fileSystem,
        IEnvironment environment,
        IRandomGenerator random,
        IConsole console,
        IPlatformServices platform,
        ISignatureService signatures,
        IBuildInfo buildInfo,
        TimeProvider clock)
    {
        FileSystem = fileSystem;
        Environment = environment;
        Random = random;
        Console = console;
        Platform = platform;
        Signatures = signatures;
        BuildInfo = buildInfo;
        Clock = clock;
    }

    /// <inheritdoc />
    public IFileSystem FileSystem { get; }

    /// <inheritdoc />
    public IEnvironment Environment { get; }

    /// <inheritdoc />
    public IRandomGenerator Random { get; }

    /// <inheritdoc />
    public IConsole Console { get; }

    /// <inheritdoc />
    public IPlatformServices Platform { get; }

    /// <inheritdoc />
    public ISignatureService Signatures { get; }

    /// <inheritdoc />
    public IBuildInfo BuildInfo { get; }

    /// <inheritdoc />
    public TimeProvider Clock { get; }

    /// <summary>
    /// Builds the OS/CLR service container once. It reads the clock itself (a direct <c>TimeProvider.System</c>
    /// acquisition is legal only here and in the test builder, AG0015), obtains the OS-uniform filesystem entry point
    /// and GUID factory from the one <see cref="CrossPlatformAdapters"/> factory (the single allowed
    /// Boundaries → CrossPlatform call, AG0023) and the OS-divergent platform from the one per-OS
    /// <c>PlatformServices.Create()</c> (the single allowed Boundaries → per-OS call, AG0029), builds the
    /// Boundaries-owned environment, console, signature, and build-info owners, and assembles the single container by
    /// constructor injection.
    /// </summary>
    /// <returns>The single, fully-assembled service container.</returns>
    public static ISystemServices Create()
    {
        TimeProvider clock = TimeProvider.System;

        CrossPlatformAdapters adapters = CrossPlatformAdapters.Create();
        IPlatformServices platform = global::AgentGuard.CrossPlatform.PlatformServices.Create();

        IEnvironment environment = EnvironmentAdapter.Create();
        IConsole console = ConsoleAdapter.Create();
        ISignatureService signatures = Ed25519SignatureService.Create();
        IBuildInfo buildInfo = BuildInfoReader.Create();

        return new SystemServices(
            adapters.FileSystem,
            environment,
            adapters.Random,
            console,
            platform,
            signatures,
            buildInfo,
            clock);
    }
}
