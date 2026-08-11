// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Engine;
using AgentGuard.Engine.Abstractions;

namespace AgentGuard.Tests;

/// <summary>
/// A test-double region-map registry that reports a chosen file as registered-and-protected (so the scanner
/// captures it and the post-check routes its change through the region pass) yet returns no region/adapter for it.
/// This forces the fail-closed path in <c>FileGuard.ReconcileRegionsAsync</c> where a registered protected file
/// cannot be adjudicated — which must deny and restore, never silently skip.
/// </summary>
internal sealed class UnadjudicableRegionRegistry : IRegionMapRegistry
{
    private readonly string _registeredSuffix;

    internal UnadjudicableRegionRegistry(string registeredSuffix) => _registeredSuffix = registeredSuffix;

    /// <inheritdoc />
    public bool IsRegistered(CanonicalPath path) =>
        path.Value.EndsWith(_registeredSuffix, StringComparison.Ordinal);

    /// <inheritdoc />
    public RegionEntry? Find(CanonicalPath path) => null;
}
