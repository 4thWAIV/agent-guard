// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The region-aware Verifier: for each declared region of a changed config file it judges only that region and
/// leaves everything else alone. A mode-1 region must equal the guard's canonical value; a mode-2 region must not
/// differ from the pre-call backup unless a covering grant authorizes it; a mode-3 region is never checked. A
/// deleted file, or a file that no longer parses, denies so the restore can fall back to the whole-file snapshot.
/// It is format-blind, reaching the file only through its region map's adapter.
/// </summary>
internal sealed class RegionVerifier : IVerifier
{
    private readonly RegionMap _map;
    private readonly IRegionAdapter _adapter;

    private RegionVerifier(RegionMap map, IRegionAdapter adapter)
    {
        _map = map;
        _adapter = adapter;
    }

    /// <inheritdoc />
    public string Name => "region-aware";

    /// <inheritdoc />
    public Task<Verdict> VerifyAsync(FileChange change, Grant? grant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);
        Verdict verdict = change switch
        {
            FileRemoved removed => Verdict.Deny($"A protected config file was removed: {removed.Path}"),
            FileCreated created => VerifyLanded(created.After, default, grant, isCreate: true),
            FileModified modified => VerifyLanded(modified.After, modified.Before, grant, isCreate: false),
            _ => Verdict.Deny($"Unhandled change kind: {change.GetType().Name}"),
        };
        return Task.FromResult(verdict);
    }

    /// <summary>
    /// Creates the region-aware verifier for one file's region map and its format's adapter.
    /// </summary>
    /// <param name="map">The changed file's region map.</param>
    /// <param name="adapter">The adapter for the map's format.</param>
    /// <returns>The verifier, as its interface.</returns>
    internal static IVerifier Create(RegionMap map, IRegionAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(adapter);
        return new RegionVerifier(map, adapter);
    }

    private Verdict VerifyLanded(
        ReadOnlyMemory<byte> current,
        ReadOnlyMemory<byte> backup,
        Grant? grant,
        bool isCreate)
    {
        if (!RegionDiffer.IsReadable(_map, _adapter, current))
        {
            return Verdict.Deny("A protected config file no longer parses; falling back to the whole-file snapshot.");
        }

        foreach (Region region in _map.Regions)
        {
            switch (region.Mode)
            {
                case RegionMode.Canonical:
                    if (region.CanonicalExemplar is { } exemplar
                        && !RegionDiffer.RegionMatches(_adapter, region.Locator, exemplar, current))
                    {
                        return Verdict.Deny($"The canonical region '{region.Id}' is not the guard's computed value.");
                    }

                    break;
                case RegionMode.Backup:
                    if (!isCreate
                        && grant is null
                        && !RegionDiffer.RegionMatches(_adapter, region.Locator, backup, current))
                    {
                        return Verdict.Deny($"The protected region '{region.Id}' changed without a covering grant.");
                    }

                    break;
                case RegionMode.Unprotected:
                default:
                    break;
            }
        }

        return Verdict.Allow();
    }
}
