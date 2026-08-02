// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Linq;

namespace AgentGuard.Engine;

/// <summary>
/// The region-scoped diff: it compares a single region across two files through the adapter's normalized read,
/// ignoring every other byte, and reports whether a file can be read at all. It is format-blind — it names no
/// format, only the opaque locator — so the same diff serves the mode-1 canonical check (left = the canonical
/// exemplar) and the mode-2 backup check (left = the pre-call backup).
/// </summary>
internal static class RegionDiffer
{
    /// <summary>
    /// Determines whether every declared region can be read from the file. A region that cannot be read means the
    /// file is unparseable, which forces the whole-file snapshot fallback rather than a partial edit.
    /// </summary>
    /// <param name="map">The file's region map.</param>
    /// <param name="adapter">The adapter for the map's format.</param>
    /// <param name="file">The file bytes to test.</param>
    /// <returns><see langword="true"/> when every region reads a value.</returns>
    internal static bool IsReadable(RegionMap map, IRegionAdapter adapter, ReadOnlyMemory<byte> file)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(adapter);
        return map.Regions.All(region => adapter.Read(file, region.Locator) is not null);
    }

    /// <summary>
    /// Determines whether one region has the same normalized value in two files, ignoring all other content.
    /// </summary>
    /// <param name="adapter">The adapter for the region's format.</param>
    /// <param name="locator">The opaque address of the region.</param>
    /// <param name="left">The reference file (the canonical exemplar, or the pre-call backup).</param>
    /// <param name="right">The file under test (what landed on disk).</param>
    /// <returns><see langword="true"/> when the region's value is equal in both files.</returns>
    internal static bool RegionMatches(
        IRegionAdapter adapter,
        RegionLocator locator,
        ReadOnlyMemory<byte> left,
        ReadOnlyMemory<byte> right)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        return string.Equals(adapter.Read(left, locator), adapter.Read(right, locator), StringComparison.Ordinal);
    }
}
