// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Engine.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// The registry of which files are region-watched and how. It is the second source of after-check membership: the
/// scanner unions the whole-file protected set (from rule origin) with the files registered here, and the guard
/// runs the region-aware verifier on the registered set. Populated at composition, never from disk, so there is no
/// further config file to protect.
/// </summary>
internal interface IRegionMapRegistry
{
    /// <summary>
    /// Gets a value indicating whether the given canonical path is region-watched, so the scanner includes it.
    /// </summary>
    /// <param name="path">The canonical path of a candidate file.</param>
    /// <returns><see langword="true"/> when the path is registered with a region map.</returns>
    bool IsRegistered(CanonicalPath path);

    /// <summary>
    /// Finds the region map and adapter for a canonical path.
    /// </summary>
    /// <param name="path">The canonical path of a changed file.</param>
    /// <returns>The registered entry, or <see langword="null"/> when the path is not region-watched.</returns>
    RegionEntry? Find(CanonicalPath path);
}
