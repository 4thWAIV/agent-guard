// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine;

/// <summary>
/// A registered file's region map paired with the adapter that interprets it, as resolved from the region-map
/// registry. Bundles the declarative map with its format's mechanic so the caller never re-resolves the adapter.
/// </summary>
/// <param name="Map">The file's declared regions and format.</param>
/// <param name="Adapter">The adapter serving the map's format.</param>
internal sealed record RegionEntry(RegionMap Map, IRegionAdapter Adapter);
