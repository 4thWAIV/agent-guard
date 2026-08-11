// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Engine;

/// <summary>
/// One declared, protected slice of a config file: a stable id, the mode that decides how it is judged, the opaque
/// locator the adapter addresses it by, and — for a <see cref="RegionMode.Canonical"/> region only — a canonical
/// exemplar file the adapter reads the region's correct value from. The exemplar carries the guard's
/// "what it should be" logic (the computed hook groups, the default config shape) precomputed at composition, so
/// the check is per-region and never per-format.
/// </summary>
/// <param name="Id">A stable, human-readable identifier for the region.</param>
/// <param name="Mode">How the region is judged: canonical, backup-protected, or unprotected.</param>
/// <param name="Locator">The opaque address the per-format adapter interprets.</param>
/// <param name="CanonicalExemplar">
/// A canonical exemplar of the whole file, from which the adapter reads the region's correct value; present for a
/// <see cref="RegionMode.Canonical"/> region and <see langword="null"/> otherwise.
/// </param>
internal sealed record Region(
    string Id,
    RegionMode Mode,
    RegionLocator Locator,
    ReadOnlyMemory<byte>? CanonicalExemplar);
