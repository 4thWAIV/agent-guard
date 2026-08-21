// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single home for the owned-interface identities that MORE THAN ONE rule matches an owner against, so each
/// <c>(namespace, name)</c> pair is spelled exactly once (LESSON 1, DRY). Each is held as a one-element set so a
/// resolver can hand it straight to <see cref="OwnerClass.IsOwner"/> without allocating per operation. Only the
/// identities shared across files live here — an identity a single rule owns stays private to that rule
/// (<c>IFileReader</c> in <see cref="FilesystemMembers"/>, <c>IConsole</c>/<c>IBuildInfo</c> in
/// <see cref="OwnedPrimitives"/>): this class exists to remove duplication, not to become a grab-bag of every
/// interface name.
/// </summary>
internal static class ContractInterfaces
{
    /// <summary>
    /// The environment read owner (<c>IEnvironment</c>) — matched against by the consolidated owner rule (AG0011,
    /// for <c>System.Environment</c> and the deployment-path reads) and the Path-purity rule (AG0020, for the
    /// <c>Path.GetTempPath</c> owner exemption).
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> Environment =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IEnvironment"));

    /// <summary>
    /// The randomness owner (<c>IRandomGenerator</c>, formerly <c>IGuidFactory</c>) — the one concern that owns
    /// <c>Guid.NewGuid</c>/<c>System.Random</c>/<c>RandomNumberGenerator</c> (AG0011) and
    /// <c>Path.GetRandomFileName</c> (AG0020, the owner exemption). The owner is the randomness concern, so a random
    /// file name has a discoverable home beside <c>NewGuid</c> (iguidfactory-renamed-to-irandomgenerator).
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> RandomGenerator =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IRandomGenerator"));

    /// <summary>
    /// The OS-divergent platform-filesystem owner (<c>IPlatformFileSystem</c>) — matched against by the OS-divergent
    /// rule (AG0101, whose implementers are the per-OS <c>PosixFileSystem</c>/<c>WindowsFileSystem</c>) and the
    /// Path-purity rule (AG0020, for the <c>Path.DirectorySeparatorChar</c> owner exemption, whose owners are those
    /// same implementers plus the in-memory fake).
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> PlatformFileSystem =
        ImmutableArray.Create((KnownNamespaces.AgentGuardAbstractionsContracts, "IPlatformFileSystem"));
}
