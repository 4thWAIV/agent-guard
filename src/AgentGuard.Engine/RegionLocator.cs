// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine;

/// <summary>
/// The opaque address of a region within a file. Only the per-format adapter interprets it; the region-aware
/// verifier, diff, and restore treat it as a token they never read. A <see cref="Kind"/> selects the adapter's
/// addressing scheme (a JSON key path, the guard-hook groups, the config document shape) and <see cref="Argument"/>
/// carries whatever that scheme needs (the key path, or the absolute launcher path a settings region is pinned to).
/// </summary>
/// <param name="Kind">The adapter-specific addressing scheme.</param>
/// <param name="Argument">The scheme's parameter, or the empty string when it needs none.</param>
internal sealed record RegionLocator(string Kind, string Argument);
