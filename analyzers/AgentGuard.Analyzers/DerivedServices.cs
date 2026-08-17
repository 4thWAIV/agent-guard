// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The boundary service set derived from <c>ISystemServices</c> for one compilation by
/// <see cref="BoundaryServices.Resolve"/> — the compile-time replacement for the former hand-maintained owner list.
/// Held as (namespace + name) identities so a candidate type is matched through <see cref="WellKnownType"/> exactly
/// as the former list was, never a bare name. Each consuming rule captures one of these once per compilation (in a
/// compilation-start action) and reuses it across its member callbacks.
/// </summary>
internal readonly struct DerivedServices
{
    private DerivedServices(
        ImmutableArray<(string Namespace, string Name)> serviceInterfaces,
        ImmutableArray<(string Namespace, string Name)> serviceTypes)
    {
        ServiceInterfaces = serviceInterfaces;
        ServiceTypes = serviceTypes;
    }

    /// <summary>Gets the empty set — a compilation without the container makes every consuming rule inert.</summary>
    internal static DerivedServices Empty { get; } = new(
        ImmutableArray<(string Namespace, string Name)>.Empty,
        ImmutableArray<(string Namespace, string Name)>.Empty);

    /// <summary>
    /// Gets the leaf owned service interfaces reached through the container's accessors — the owner-interface set
    /// AG0025 (one owner per interface) and AG0031 (no service as a lone parameter) reason about.
    /// </summary>
    internal ImmutableArray<(string Namespace, string Name)> ServiceInterfaces { get; }

    /// <summary>
    /// Gets the service interfaces plus the container <c>ISystemServices</c> itself and the clock
    /// <c>System.TimeProvider</c> — the set AG0024 (no static service holder) forbids a static from holding.
    /// </summary>
    internal ImmutableArray<(string Namespace, string Name)> ServiceTypes { get; }

    /// <summary>Builds a derived set from its interface and type identities.</summary>
    /// <param name="serviceInterfaces">The leaf owned service interfaces.</param>
    /// <param name="serviceTypes">The service interfaces plus the container and the clock.</param>
    /// <returns>The derived set.</returns>
    internal static DerivedServices Of(
        ImmutableArray<(string Namespace, string Name)> serviceInterfaces,
        ImmutableArray<(string Namespace, string Name)> serviceTypes)
    {
        return new DerivedServices(serviceInterfaces, serviceTypes);
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is one of the derived owned service interfaces.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is a derived owner interface.</returns>
    internal bool IsOwnerInterface(INamedTypeSymbol? type) => WellKnownType.IsAnyOf(type, ServiceInterfaces);

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> is the container or one of its service types.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> when the type is the container or one of its service types.</returns>
    internal bool IsServiceType(INamedTypeSymbol? type) => WellKnownType.IsAnyOf(type, ServiceTypes);
}
