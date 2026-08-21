// Copyright (c) 4thWAIV. All rights reserved.

using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// One accessor on a <see cref="ContainerNode"/>: the member NAME the accessor is exposed under (which the AG0019
/// navigator name <c>On&lt;MemberName&gt;()</c> is derived from), the target type it yields, its
/// <see cref="ServiceAccessorKind"/>, and — for a nested container — the sub-node reached through it. A plain sealed
/// class (not a record) so it needs no <c>IsExternalInit</c> polyfill on this netstandard2.0 analyzer project.
/// </summary>
internal sealed class ServiceAccessor
{
    private ServiceAccessor(string memberName, INamedTypeSymbol target, ServiceAccessorKind kind, ContainerNode? child)
    {
        MemberName = memberName;
        Target = target;
        Kind = kind;
        Child = child;
    }

    /// <summary>Gets the member name the accessor is exposed under (for example <c>FileSystem</c>, <c>Environment</c>,
    /// <c>Clock</c>).</summary>
    internal string MemberName { get; }

    /// <summary>Gets the target type the accessor yields — a <c>Contracts</c> interface for a leaf or container, or
    /// <c>System.TimeProvider</c> for the clock.</summary>
    internal INamedTypeSymbol Target { get; }

    /// <summary>Gets the kind of accessor.</summary>
    internal ServiceAccessorKind Kind { get; }

    /// <summary>Gets the recursively-resolved sub-node for a <see cref="ServiceAccessorKind.Container"/> accessor, or
    /// <see langword="null"/> for a leaf, the clock, or a container whose target was already visited.</summary>
    internal ContainerNode? Child { get; }

    /// <summary>Builds a leaf-service accessor.</summary>
    /// <param name="memberName">The member name the accessor is exposed under.</param>
    /// <param name="target">The owned service interface the accessor yields.</param>
    /// <returns>The leaf-service accessor.</returns>
    internal static ServiceAccessor Leaf(string memberName, INamedTypeSymbol target) =>
        new(memberName, target, ServiceAccessorKind.LeafService, child: null);

    /// <summary>Builds a nested-container accessor.</summary>
    /// <param name="memberName">The member name the accessor is exposed under.</param>
    /// <param name="target">The sub-container interface the accessor yields.</param>
    /// <param name="child">The recursively-resolved sub-node, or <see langword="null"/> on a revisited target.</param>
    /// <returns>The nested-container accessor.</returns>
    internal static ServiceAccessor Nested(string memberName, INamedTypeSymbol target, ContainerNode? child) =>
        new(memberName, target, ServiceAccessorKind.Container, child);

    /// <summary>Builds the clock accessor.</summary>
    /// <param name="memberName">The member name the clock is exposed under.</param>
    /// <param name="target">The <c>System.TimeProvider</c> type.</param>
    /// <returns>The clock accessor.</returns>
    internal static ServiceAccessor Clock(string memberName, INamedTypeSymbol target) =>
        new(memberName, target, ServiceAccessorKind.Clock, child: null);
}
