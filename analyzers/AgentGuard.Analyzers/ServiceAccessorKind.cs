// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers;

/// <summary>
/// The kind of a governed accessor on a <see cref="ContainerNode"/>.
/// </summary>
internal enum ServiceAccessorKind
{
    /// <summary>A leaf service accessor: a property or zero-parameter method whose value type is a <c>Contracts</c>
    /// interface that itself exposes no service accessor (an owned service, not a container).</summary>
    LeafService,

    /// <summary>A nested-container accessor: a property or zero-parameter method whose value type is a <c>Contracts</c>
    /// interface that itself exposes at least one service accessor. Its <see cref="ServiceAccessor.Child"/> is the
    /// recursively-resolved sub-node (or <see langword="null"/> when the target was already visited, so a cyclic
    /// container terminates).</summary>
    Container,

    /// <summary>The clock accessor: a property or zero-parameter method whose value type is <c>System.TimeProvider</c>
    /// — a leaf that is a service TYPE but not a <c>Contracts</c> owner interface, the one deliberately-named
    /// non-interface service.</summary>
    Clock,
}
