// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// Derives the boundary service set the three constructor-injection rules reason about — AG0024's static-holder ban,
/// AG0025's one-owner-per-interface check, and AG0031's no-service-as-a-parameter check — from the shape of
/// <c>ISystemServices</c> at compile time, replacing the hand-maintained list that drifted as services were added
/// (derive-service-set-from-isystemservices). <see cref="Resolve"/> walks the container once per compilation:
/// starting from <c>ISystemServices</c>, it follows every SERVICE ACCESSOR — a property, or a zero-parameter method,
/// whose value type is an interface declared in <c>AgentGuard.Abstractions.Contracts</c> — recursing (visited-guarded,
/// so cycles terminate) into each. A sub-container such as <c>IFileSystem</c> or <c>IPlatformServices</c> is a
/// structural pass-through: its accessors are followed to reach the services it exposes, but the sub-container itself
/// is not a service. A parameterized factory (<c>GetFileInfo(string)</c>) is not an accessor, so its return interface
/// is never collected. The leaf interfaces are the <see cref="DerivedServices.ServiceInterfaces"/> that feed AG0025
/// and AG0031; those plus the container and the one non-interface clock service <c>System.TimeProvider</c> are the
/// <see cref="DerivedServices.ServiceTypes"/> that feed AG0024. Every type is matched by full (namespace + name)
/// identity through <see cref="WellKnownType"/>, never a bare name. The companion rule AG0034
/// (<see cref="SystemServicesMemberMustBeServiceAccessorAnalyzer"/>) keeps the container surface honest so it can
/// never silently outgrow this walk.
/// </summary>
internal static class BoundaryServices
{
    /// <summary>
    /// The simple name of the one container interface, <c>AgentGuard.Abstractions.Contracts.ISystemServices</c>.
    /// </summary>
    internal const string ContainerName = "ISystemServices";

    /// <summary>
    /// The simple name of <c>System.TimeProvider</c> — the clock, the one deliberately-named non-interface service on
    /// the container: a BCL class rather than an owned interface, so it is a service TYPE for AG0024 but not an owner
    /// interface for AG0025 or AG0031. It is the single named exception; the drift-prone interface set is derived,
    /// not named.
    /// </summary>
    internal const string TimeProviderName = "TimeProvider";

    /// <summary>
    /// Walks <c>ISystemServices</c> in <paramref name="compilation"/> and returns the derived boundary service set.
    /// When the container is not in scope (a compilation that does not reference the contracts assembly) the sets are
    /// empty and the three rules that consume them simply do not fire.
    /// </summary>
    /// <param name="compilation">The compilation whose <c>ISystemServices</c> is walked.</param>
    /// <returns>The derived service set, or <see cref="DerivedServices.Empty"/> when the container is absent.</returns>
    internal static DerivedServices Resolve(Compilation compilation)
    {
        INamedTypeSymbol? container = WellKnownType.Resolve(
            compilation, KnownNamespaces.AgentGuardAbstractionsContracts, ContainerName);
        if (container is null)
        {
            return DerivedServices.Empty;
        }

        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var serviceInterfaces = ImmutableArray.CreateBuilder<(string Namespace, string Name)>();
        Collect(container, isRoot: true, visited, serviceInterfaces);

        ImmutableArray<(string Namespace, string Name)> interfaces = serviceInterfaces.ToImmutable();
        ImmutableArray<(string Namespace, string Name)> types = interfaces
            .Add((KnownNamespaces.AgentGuardAbstractionsContracts, ContainerName))
            .Add((KnownNamespaces.System, TimeProviderName));

        return DerivedServices.Of(interfaces, types);
    }

    /// <summary>
    /// The <c>Contracts</c> interface a member exposes as a service accessor — a non-indexer property, or an ordinary
    /// zero-parameter method, whose value type is an interface declared in
    /// <c>AgentGuard.Abstractions.Contracts</c> — or <see langword="null"/> when the member is not such an accessor
    /// (a parameterized factory, a method returning a non-interface, or a member that is not accessor-shaped). Shared
    /// by the derivation walk and AG0034 so both agree on exactly what a service accessor is.
    /// </summary>
    /// <param name="member">The member to classify.</param>
    /// <returns>The exposed <c>Contracts</c> interface, or <see langword="null"/> when the member is not an
    /// accessor.</returns>
    internal static INamedTypeSymbol? ServiceAccessorInterface(ISymbol member)
    {
        return AccessorValueType(member) is INamedTypeSymbol { TypeKind: TypeKind.Interface } candidate
            && WellKnownType.IsInNamespace(candidate, KnownNamespaces.AgentGuardAbstractionsContracts)
                ? candidate
                : null;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> exposes the clock — a non-indexer property, or an
    /// ordinary zero-parameter method, whose value type is <c>System.TimeProvider</c>. The clock is the one
    /// deliberately-named non-interface service AG0034 permits alongside the derived service accessors.
    /// </summary>
    /// <param name="member">The member to test.</param>
    /// <returns><see langword="true"/> when the member exposes the <c>System.TimeProvider</c> clock.</returns>
    internal static bool IsClockAccessor(ISymbol member)
    {
        return WellKnownType.Is(
            AccessorValueType(member) as INamedTypeSymbol, KnownNamespaces.System, TimeProviderName);
    }

    /// <summary>
    /// The declared surface of an interface for the accessor walk: its own members plus those of every interface it
    /// extends (so a member exposed through an inherited accessor is still seen). Internal so AG0034
    /// (<see cref="SystemServicesMemberMustBeServiceAccessorAnalyzer"/>) guards the SAME surface the derivation walks —
    /// the one definition of the <c>ISystemServices</c> surface — instead of a diverging <c>GetMembers()</c> copy.
    /// </summary>
    /// <param name="type">The interface whose declared surface is returned.</param>
    /// <returns>The type's own members concatenated with those of every interface it extends.</returns>
    internal static IEnumerable<ISymbol> SurfaceMembers(INamedTypeSymbol type)
    {
        return type.GetMembers().Concat(type.AllInterfaces.SelectMany(baseInterface => baseInterface.GetMembers()));
    }

    // Depth-first walk from the container. A node with at least one service accessor is a container (the root, or a
    // sub-container): recurse into each accessor's interface, but do not collect the node itself — a container is a
    // structural pass-through, not a service. A node with no service accessor is a leaf service and is collected,
    // unless it is the root itself (the container is never a service interface). The visited set makes cycles
    // terminate.
    private static void Collect(
        INamedTypeSymbol node,
        bool isRoot,
        HashSet<INamedTypeSymbol> visited,
        ImmutableArray<(string Namespace, string Name)>.Builder serviceInterfaces)
    {
        if (!visited.Add(node))
        {
            return;
        }

        ImmutableArray<INamedTypeSymbol> accessors = ServiceAccessorInterfaces(node);
        if (accessors.IsEmpty)
        {
            if (!isRoot)
            {
                serviceInterfaces.Add((node.ContainingNamespace.ToDisplayString(), node.Name));
            }

            return;
        }

        foreach (INamedTypeSymbol accessor in accessors)
        {
            Collect(accessor, isRoot: false, visited, serviceInterfaces);
        }
    }

    // The distinct interfaces reached through a type's service accessors, over the type's own members and those of
    // every interface it extends (so a service exposed through an inherited accessor is still reached).
    private static ImmutableArray<INamedTypeSymbol> ServiceAccessorInterfaces(INamedTypeSymbol type)
    {
        return SurfaceMembers(type)
            .Select(ServiceAccessorInterface)
            .Where(accessor => accessor is not null)
            .Select(accessor => accessor!)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .ToImmutableArray();
    }

    // The value a member exposes when it is accessor-shaped: the property type for a non-indexer property, or the
    // return type for an ordinary zero-parameter method. Anything else — an indexer, a parameterized method, a field,
    // an event, or a property/event accessor method — is not accessor-shaped and yields null.
    private static ITypeSymbol? AccessorValueType(ISymbol member)
    {
        return member switch
        {
            IPropertySymbol { IsIndexer: false } property => property.Type,
            IMethodSymbol { MethodKind: MethodKind.Ordinary, Parameters.IsEmpty: true } method => method.ReturnType,
            _ => null,
        };
    }
}
