// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The one source of truth for the boundary service interfaces on <c>ISystemServices</c> — the ten owned
/// abstractions every consumer receives by constructor injection. Held here once so the rules that reason about the
/// service surface (AG0024's static-holder ban, AG0025's one-owner-per-interface check, AG0031's no-service-as-a
/// -parameter check) share the exact same list and a rename or a new service is edited in a single place, never
/// re-spelled per rule. Each entry is matched by full name (namespace + simple name) through
/// <see cref="WellKnownType"/> (LESSON 1), never a bare name.
/// </summary>
internal static class BoundaryServices
{
    /// <summary>
    /// The simple name of the one container interface, <c>AgentGuard.Abstractions.Contracts.ISystemServices</c>.
    /// </summary>
    internal const string ContainerName = "ISystemServices";

    /// <summary>
    /// The simple name of <c>System.TimeProvider</c> — the eleventh service (<c>Clock</c>) on the container, a BCL
    /// class rather than an owned interface, so it is a service TYPE for AG0024 but not an owner interface for AG0025
    /// or AG0031.
    /// </summary>
    internal const string TimeProviderName = "TimeProvider";

    /// <summary>
    /// The ten boundary service interfaces defined in <c>AgentGuard.Abstractions.Contracts</c>, each the single owned
    /// abstraction for one primitive. This is the hard-coded owner-interface list AG0025 closes the second-implementer
    /// hole against, and the parameter-type set AG0031 forbids as a lone method argument.
    /// </summary>
    internal static readonly ImmutableArray<(string Namespace, string Name)> OwnerInterfaces = ImmutableArray.Create(
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IFileReader"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IDirectoryEnumerator"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IFileWriter"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IDirectoryWriter"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IEnvironment"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IGuidFactory"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IConsole"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IPlatformFileSystem"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "ISignatureVerifier"),
        (KnownNamespaces.AgentGuardAbstractionsContracts, "IBuildInfo"));

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is one of the ten owned boundary service interfaces.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is one of the owner interfaces.</returns>
    internal static bool IsOwnerInterface(INamedTypeSymbol? type) => WellKnownType.IsAnyOf(type, OwnerInterfaces);

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is the container <c>ISystemServices</c> or any of its
    /// eleven service types — the ten owner interfaces plus <c>System.TimeProvider</c> — the set AG0024 forbids a
    /// static field or property from holding outside the composition point.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is the container or one of its eleven service types.</returns>
    internal static bool IsServiceType(INamedTypeSymbol? type)
    {
        return IsOwnerInterface(type)
            || WellKnownType.Is(type, KnownNamespaces.AgentGuardAbstractionsContracts, ContainerName)
            || WellKnownType.Is(type, KnownNamespaces.System, TimeProviderName);
    }
}
