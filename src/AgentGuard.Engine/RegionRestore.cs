// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// Builds the restore effect for a denied change to a region-watched config file. A mode-1 region is rewritten to
/// the guard's canonical value; a mode-2 region is reverted to the pre-call backup; every other byte is preserved.
/// When the file no longer parses, was deleted, or a needed backup cannot be read, it falls back to restoring the
/// whole pre-call snapshot (or deleting a file the call created). Every effect is carried by
/// <see cref="RestoreFileEffect"/> or <see cref="DeleteFileEffect"/>, executed by the privileged writer.
/// </summary>
internal static class RegionRestore
{
    /// <summary>
    /// Builds the effect that undoes an unauthorized change to a region-watched file.
    /// </summary>
    /// <param name="change">The landed change to the file.</param>
    /// <param name="map">The file's region map.</param>
    /// <param name="adapter">The adapter for the map's format.</param>
    /// <param name="grant">The grant that authorizes a mode-2 change, or <see langword="null"/> when none does.</param>
    /// <returns>The restore or delete effect.</returns>
    internal static Effect Build(FileChange change, RegionMap map, IRegionAdapter adapter, Grant? grant)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(adapter);
        return change switch
        {
            FileRemoved removed => new RestoreFileEffect(removed.Path, removed.Before),
            FileCreated created => BuildForCreate(created, map, adapter),
            FileModified modified => BuildForModify(modified, map, adapter, grant),
            _ => throw new InvalidOperationException($"Unhandled change kind: {change.GetType().Name}"),
        };
    }

    private static Effect BuildForCreate(FileCreated created, RegionMap map, IRegionAdapter adapter)
    {
        if (!RegionDiffer.IsReadable(map, adapter, created.After))
        {
            return new DeleteFileEffect(created.Path);
        }

        ReadOnlyMemory<byte> working = created.After;
        foreach (Region region in map.Regions)
        {
            if (region.Mode != RegionMode.Canonical || region.CanonicalExemplar is not { } exemplar)
            {
                continue;
            }

            string? canonical = adapter.Read(exemplar, region.Locator);
            if (canonical is null || RegionDiffer.RegionMatches(adapter, region.Locator, exemplar, working))
            {
                continue;
            }

            ReadOnlyMemory<byte>? rewritten = adapter.Rewrite(working, region.Locator, canonical);
            if (rewritten is null)
            {
                return new DeleteFileEffect(created.Path);
            }

            working = rewritten.Value;
        }

        return new RestoreFileEffect(created.Path, working);
    }

    private static RestoreFileEffect BuildForModify(FileModified modified, RegionMap map, IRegionAdapter adapter, Grant? grant)
    {
        if (!RegionDiffer.IsReadable(map, adapter, modified.After))
        {
            return new RestoreFileEffect(modified.Path, modified.Before);
        }

        ReadOnlyMemory<byte> working = modified.After;
        foreach (Region region in map.Regions)
        {
            ReadOnlyMemory<byte>? rewritten = region.Mode switch
            {
                RegionMode.Canonical => CanonicalRewrite(region, adapter, working),
                RegionMode.Backup => BackupRewrite(region, adapter, modified.Before, working, grant),
                _ => working,
            };

            if (rewritten is null)
            {
                return new RestoreFileEffect(modified.Path, modified.Before);
            }

            working = rewritten.Value;
        }

        return new RestoreFileEffect(modified.Path, working);
    }

    private static ReadOnlyMemory<byte>? CanonicalRewrite(Region region, IRegionAdapter adapter, ReadOnlyMemory<byte> working)
    {
        if (region.CanonicalExemplar is not { } exemplar)
        {
            return working;
        }

        string? canonical = adapter.Read(exemplar, region.Locator);
        if (canonical is null || RegionDiffer.RegionMatches(adapter, region.Locator, exemplar, working))
        {
            return working;
        }

        return adapter.Rewrite(working, region.Locator, canonical);
    }

    private static ReadOnlyMemory<byte>? BackupRewrite(
        Region region,
        IRegionAdapter adapter,
        ReadOnlyMemory<byte> backup,
        ReadOnlyMemory<byte> working,
        Grant? grant)
    {
        if (grant is not null || RegionDiffer.RegionMatches(adapter, region.Locator, backup, working))
        {
            return working;
        }

        string? backupValue = adapter.Read(backup, region.Locator);
        if (backupValue is null)
        {
            return null;
        }

        return adapter.Rewrite(working, region.Locator, backupValue);
    }
}
