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
/// (derive-service-set-from-isystemservices). <see cref="ResolveTree"/> walks the container ONCE per compilation into a
/// <see cref="ContainerNode"/> tree — the single walk of the container shape — and <see cref="Resolve"/> flattens over
/// it, byte-identical to the former hand-rolled recursion (proven by the regression assertion in the analyzer tests),
/// so AG0024/AG0025/AG0031 read the identical set and the recursive rules AG0019 (builder completeness) and AG0034
/// (container surface) reason about the identical structure. Starting from <c>ISystemServices</c>, the walk follows
/// every SERVICE ACCESSOR — a property, or a zero-parameter method, whose value type is an interface declared in
/// <c>AgentGuard.Abstractions.Contracts</c> — recursing (visited-guarded, so cycles terminate) into each. A
/// sub-container such as <c>IFileSystem</c> or <c>IPlatformServices</c> is a structural pass-through: its accessors are
/// followed to reach the services it exposes, but the sub-container itself is not a leaf service. A parameterized
/// factory (<c>GetFileInfo(string)</c>) is not an accessor, so its return interface is never collected. The leaf
/// interfaces are the <see cref="DerivedServices.ServiceInterfaces"/> that feed AG0025 and AG0031; those plus the
/// container and the one non-interface clock service <c>System.TimeProvider</c> are the
/// <see cref="DerivedServices.ServiceTypes"/> that feed AG0024. Every type is matched by full (namespace + name)
/// identity through <see cref="WellKnownType"/>, never a bare name.
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
    /// Walks <c>ISystemServices</c> in <paramref name="compilation"/> into the container tree — the ONE walk of the
    /// container shape shared by <see cref="Resolve"/>, AG0019, and AG0034. Returns <see langword="null"/> when the
    /// container is not in scope (a compilation that does not reference the contracts assembly), so every consuming
    /// rule is simply inert there.
    /// </summary>
    /// <param name="compilation">The compilation whose <c>ISystemServices</c> is walked.</param>
    /// <returns>The root <see cref="ContainerNode"/>, or <see langword="null"/> when the container is absent.</returns>
    internal static ContainerNode? ResolveTree(Compilation compilation)
    {
        INamedTypeSymbol? container = WellKnownType.Resolve(
            compilation, KnownNamespaces.AgentGuardAbstractionsContracts, ContainerName);
        if (container is null)
        {
            return null;
        }

        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        return BuildNode(container, visited);
    }

    /// <summary>
    /// Walks <c>ISystemServices</c> in <paramref name="compilation"/> and returns the derived boundary service set,
    /// flattening the container tree (<see cref="ResolveTree"/>). When the container is not in scope the sets are
    /// empty and the three rules that consume them simply do not fire.
    /// </summary>
    /// <param name="compilation">The compilation whose <c>ISystemServices</c> is walked.</param>
    /// <returns>The derived service set, or <see cref="DerivedServices.Empty"/> when the container is absent.</returns>
    internal static DerivedServices Resolve(Compilation compilation)
    {
        ContainerNode? root = ResolveTree(compilation);
        if (root is null)
        {
            return DerivedServices.Empty;
        }

        var serviceInterfaces = ImmutableArray.CreateBuilder<(string Namespace, string Name)>();
        var collected = new HashSet<(string Namespace, string Name)>();
        FlattenLeaves(root, serviceInterfaces, collected);

        ImmutableArray<(string Namespace, string Name)> interfaces = serviceInterfaces.ToImmutable();
        ImmutableArray<(string Namespace, string Name)> types = interfaces
            .Add((KnownNamespaces.AgentGuardAbstractionsContracts, ContainerName))
            .Add((KnownNamespaces.System, TimeProviderName));

        return DerivedServices.Of(interfaces, types);
    }

    /// <summary>
    /// The full-name identities of every CONTAINER node in the tree rooted at <paramref name="root"/> — the root
    /// container and every nested container reached through a <see cref="ServiceAccessorKind.Container"/> accessor,
    /// recursively, in pre-order. This is the container-interface set the single-owner container rule (AG0022) guards,
    /// derived from the ONE tree walk <see cref="ResolveTree"/> builds rather than a hand-maintained triple, so a future
    /// nested container is covered automatically. Over the shipped container it yields exactly
    /// <c>ISystemServices</c>, <c>IFileSystem</c>, <c>IPlatformServices</c>. A leaf service and the clock are never
    /// containers, so neither is included.
    /// </summary>
    /// <param name="root">The root container node from <see cref="ResolveTree"/>.</param>
    /// <returns>The (namespace, name) identity of every container node, root first, in pre-order.</returns>
    internal static ImmutableArray<(string Namespace, string Name)> ContainerInterfaces(ContainerNode root)
    {
        var containers = ImmutableArray.CreateBuilder<(string Namespace, string Name)>();
        CollectContainers(root, containers);
        return containers.ToImmutable();
    }

    /// <summary>
    /// The leaf service interfaces reached only THROUGH a nested container — the leaves under every non-root container
    /// node, excluding the leaves the root exposes directly. Over the shipped shape this is exactly the four filesystem
    /// leaves reached through <c>IFileSystem</c> (<c>IFileReader</c>/<c>IDirectoryEnumerator</c>/<c>IFileWriter</c>/
    /// <c>IDirectoryWriter</c>) and <c>IPlatformFileSystem</c> reached through <c>IPlatformServices</c> — the five
    /// filesystem/platform interfaces the shared copy-on-write overlay implements. The root's own direct leaves
    /// (environment, randomness, console, signatures, build-info) are NOT included, because each of those has a single
    /// dedicated fake a test supplies through <c>With(...)</c>, whereas the filesystem/platform leaves must be built
    /// through the overlay. This is the candidate set the leaf/platform single-implementer rule (AG0030) guards in a
    /// <c>.Tests</c> compilation, derived from the ONE tree walk <see cref="ResolveTree"/> builds by reusing the same
    /// <see cref="FlattenLeaves"/> the byte-identical <see cref="Resolve"/> uses, so a future filesystem leaf added under
    /// a nested container is covered automatically with no edit here.
    /// </summary>
    /// <param name="root">The root container node from <see cref="ResolveTree"/>.</param>
    /// <returns>The (namespace, name) identity of every leaf reached through a nested container, in pre-order.</returns>
    internal static ImmutableArray<(string Namespace, string Name)> NestedLeafInterfaces(ContainerNode root)
    {
        var leaves = ImmutableArray.CreateBuilder<(string Namespace, string Name)>();
        var collected = new HashSet<(string Namespace, string Name)>();

        // Skip the root's direct leaf accessors; recurse into each nested container and collect ALL of its leaves
        // (direct or deeper) through the same FlattenLeaves the flatten uses, so the collected identities are spelled
        // identically to Resolve's leaf set.
        foreach (ServiceAccessor accessor in root.Accessors)
        {
            if (accessor.Kind == ServiceAccessorKind.Container && accessor.Child is not null)
            {
                FlattenLeaves(accessor.Child, leaves, collected);
            }
        }

        return leaves.ToImmutable();
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
    /// The value a member produces — the property type for a non-indexer property, or the return type for an ordinary
    /// method REGARDLESS of its parameters, or <see langword="null"/> for anything else. Unlike
    /// <see cref="AccessorValueType"/> this does not require a method to be zero-parameter, so AG0034 can classify a
    /// parameterized per-path <c>*Info</c> factory (<c>GetFileInfo(string)</c>) by its return type on a sub-container.
    /// </summary>
    /// <param name="member">The member whose produced value type is wanted.</param>
    /// <returns>The produced value type, or <see langword="null"/> when the member produces no single value.</returns>
    internal static ITypeSymbol? ProducedType(ISymbol member)
    {
        return member switch
        {
            IPropertySymbol { IsIndexer: false } property => property.Type,
            IMethodSymbol { MethodKind: MethodKind.Ordinary } method => method.ReturnType,
            _ => null,
        };
    }

    /// <summary>
    /// The declared surface of an interface for the accessor walk: its own members plus those of every interface it
    /// extends (so a member exposed through an inherited accessor is still seen). Internal so AG0034
    /// (<see cref="SystemServicesMemberMustBeServiceAccessorAnalyzer"/>) guards the SAME surface the derivation walks —
    /// the one definition of the container surface — instead of a diverging <c>GetMembers()</c> copy.
    /// </summary>
    /// <param name="type">The interface whose declared surface is returned.</param>
    /// <returns>The type's own members concatenated with those of every interface it extends.</returns>
    internal static IEnumerable<ISymbol> SurfaceMembers(INamedTypeSymbol type)
    {
        return type.GetMembers().Concat(type.AllInterfaces.SelectMany(baseInterface => baseInterface.GetMembers()));
    }

    // Depth-first build from a container node. Every governed accessor-shaped member becomes a ServiceAccessor in
    // declared-surface order: a Contracts-interface accessor whose target itself exposes a service accessor is a
    // nested container (recurse into it, visited-guarded so a cycle yields a null child and terminates); one whose
    // target exposes none is a leaf service; a TimeProvider member is the clock. An off-convention member (a
    // parameterized *Info factory, a field, a non-service member) is not a governed accessor and is simply absent —
    // AG0034 re-walks the surface to guard those.
    private static ContainerNode BuildNode(INamedTypeSymbol node, HashSet<INamedTypeSymbol> visited)
    {
        visited.Add(node);

        var accessors = ImmutableArray.CreateBuilder<ServiceAccessor>();
        foreach (ISymbol member in SurfaceMembers(node))
        {
            INamedTypeSymbol? target = ServiceAccessorInterface(member);
            if (target is not null)
            {
                if (HasServiceAccessor(target))
                {
                    ContainerNode? child = visited.Contains(target) ? null : BuildNode(target, visited);
                    accessors.Add(ServiceAccessor.Nested(member.Name, target, child));
                }
                else
                {
                    accessors.Add(ServiceAccessor.Leaf(member.Name, target));
                }
            }
            else if (IsClockAccessor(member) && AccessorValueType(member) is INamedTypeSymbol clock)
            {
                accessors.Add(ServiceAccessor.Clock(member.Name, clock));
            }
        }

        return new ContainerNode(node, accessors.ToImmutable());
    }

    // Pre-order flatten of the container tree into the distinct leaf service interfaces, in first-occurrence order —
    // the exact order and set the former hand-rolled Collect produced (a leaf was collected once via a visited-node
    // set and a per-node Distinct; here a leaf is collected the first time an accessor reaches it, and a revisited
    // sub-container yields a null child, so the flatten is byte-identical on any acyclic container).
    private static void FlattenLeaves(
        ContainerNode node,
        ImmutableArray<(string Namespace, string Name)>.Builder serviceInterfaces,
        HashSet<(string Namespace, string Name)> collected)
    {
        foreach (ServiceAccessor accessor in node.Accessors)
        {
            switch (accessor.Kind)
            {
                case ServiceAccessorKind.LeafService:
                    (string Namespace, string Name) identity =
                        (accessor.Target.ContainingNamespace.ToDisplayString(), accessor.Target.Name);
                    if (collected.Add(identity))
                    {
                        serviceInterfaces.Add(identity);
                    }

                    break;

                case ServiceAccessorKind.Container:
                    if (accessor.Child is not null)
                    {
                        FlattenLeaves(accessor.Child, serviceInterfaces, collected);
                    }

                    break;

                case ServiceAccessorKind.Clock:
                default:
                    // The clock is a service TYPE, not a leaf owner interface, so it is never collected here — it is
                    // added to ServiceTypes once in Resolve.
                    break;
            }
        }
    }

    // Pre-order collect of every container node's full-name identity: the node itself, then recurse into each nested
    // container accessor's resolved child (a revisited container yields a null child and is not recursed, so a cyclic
    // container terminates and each container is collected once). The identity is spelled from the interface's
    // ContainingNamespace + Name, the same shape FlattenLeaves collects a leaf's identity with.
    private static void CollectContainers(
        ContainerNode node, ImmutableArray<(string Namespace, string Name)>.Builder containers)
    {
        containers.Add((node.Interface.ContainingNamespace.ToDisplayString(), node.Interface.Name));
        foreach (ServiceAccessor accessor in node.Accessors)
        {
            if (accessor.Kind == ServiceAccessorKind.Container && accessor.Child is not null)
            {
                CollectContainers(accessor.Child, containers);
            }
        }
    }

    // A node is a container (a structural pass-through) when it exposes at least one service accessor; otherwise it is
    // a leaf service. The same "what is a service accessor" definition the whole walk uses.
    private static bool HasServiceAccessor(INamedTypeSymbol type)
    {
        return SurfaceMembers(type).Any(member => ServiceAccessorInterface(member) is not null);
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
