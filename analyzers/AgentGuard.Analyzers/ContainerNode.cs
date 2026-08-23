// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// One node of the container tree <see cref="BoundaryServices.ResolveTree"/> builds by walking <c>ISystemServices</c>:
/// a container interface plus the accessors it exposes, recorded in declared-surface order. It is the ONE walk of the
/// container shape, shared so <see cref="BoundaryServices.Resolve"/> (which flattens it into the leaf service set
/// AG0024/AG0025/AG0031 read), the builder-completeness rule (AG0019, which mirrors it against
/// <c>SystemServicesBuilder</c>), and the container-surface guard (AG0034, which guards every node's surface) all
/// reason about the identical structure — no rule re-derives the shape and drifts from the others.
/// </summary>
internal sealed class ContainerNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNode"/> class.
    /// </summary>
    /// <param name="containerInterface">The container interface at this node.</param>
    /// <param name="accessors">The accessors this container exposes, in declared-surface order.</param>
    internal ContainerNode(INamedTypeSymbol containerInterface, ImmutableArray<ServiceAccessor> accessors)
    {
        Interface = containerInterface;
        Accessors = accessors;
    }

    /// <summary>Gets the container interface at this node (the root <c>ISystemServices</c> or a nested container).</summary>
    internal INamedTypeSymbol Interface { get; }

    /// <summary>
    /// Gets the accessors this container exposes, in declared-surface order: one per governed accessor-shaped member
    /// — a <c>Contracts</c>-interface accessor (leaf or nested container) or the <c>TimeProvider</c> clock. An
    /// off-convention member the walk cannot classify is not an accessor and is absent here (AG0034 guards it
    /// separately by re-walking the surface).
    /// </summary>
    internal ImmutableArray<ServiceAccessor> Accessors { get; }
}
