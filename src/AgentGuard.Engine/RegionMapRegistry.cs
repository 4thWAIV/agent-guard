// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Setup;

namespace AgentGuard.Engine;

/// <summary>
/// The default region-map registry: it declares that <c>.claude/settings.json</c> and
/// <c>.agentguard/config.json</c> are region-watched, with their region maps, and resolves each map's format to an
/// adapter. Settings declares one mode-1 region (the guard-hook groups, canonical =
/// <see cref="ClaudeSettingsWiring"/>'s computed hooks for the absolute launcher path). Config declares a mode-1
/// structure region (canonical = <see cref="ProjectConfig.Default"/>'s shape) and a mode-2 <c>protectedPaths</c>
/// region. It is built at composition from these constants — never read from disk — so it is not itself a config
/// file that would need protecting.
/// </summary>
internal sealed class RegionMapRegistry : IRegionMapRegistry
{
    private static readonly string ProtectedPathsLocator = "$." + ProjectConfig.ProtectedPathsWireKey;

    private readonly IReadOnlyDictionary<string, RegionMap> _maps;
    private readonly IReadOnlyDictionary<string, IRegionAdapter> _adapters;

    private RegionMapRegistry(
        IReadOnlyDictionary<string, RegionMap> maps,
        IReadOnlyDictionary<string, IRegionAdapter> adapters)
    {
        _maps = maps;
        _adapters = adapters;
    }

    /// <inheritdoc />
    public bool IsRegistered(CanonicalPath path) => _maps.ContainsKey(path.Value);

    /// <inheritdoc />
    public RegionEntry? Find(CanonicalPath path)
    {
        return _maps.TryGetValue(path.Value, out RegionMap? map) && _adapters.TryGetValue(map.Format, out IRegionAdapter? adapter)
            ? new RegionEntry(map, adapter)
            : null;
    }

    /// <summary>
    /// Builds the registry for a project: the settings and config region maps anchored at the project root, and the
    /// JSON adapter their maps name.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to key each watched file by its canonical path.</param>
    /// <param name="projectRoot">The absolute project root the watched paths resolve against.</param>
    /// <param name="launcherPath">The absolute launcher path the settings guard hooks are pinned to.</param>
    /// <returns>The registry, as its interface.</returns>
    internal static IRegionMapRegistry CreateDefault(
        IPathCanonicalizer canonicalizer,
        string projectRoot,
        string launcherPath)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        ArgumentException.ThrowIfNullOrEmpty(launcherPath);
        IRegionAdapter json = JsonRegionAdapter.Create();
        string settingsPath = canonicalizer
            .Canonicalize(CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.ClaudeSettingsRelative)).Value;
        string configPath = canonicalizer
            .Canonicalize(CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.ProjectConfigRelative)).Value;
        var maps = new Dictionary<string, RegionMap>(StringComparer.Ordinal)
        {
            [settingsPath] = BuildSettingsMap(json.Format, launcherPath),
            [configPath] = BuildConfigMap(json.Format),
        };
        var adapters = new Dictionary<string, IRegionAdapter>(StringComparer.Ordinal)
        {
            [json.Format] = json,
        };
        return new RegionMapRegistry(maps, adapters);
    }

    private static RegionMap BuildSettingsMap(string format, string launcherPath)
    {
        var guardHooks = new Region(
            "guard-hooks",
            RegionMode.Canonical,
            new RegionLocator(JsonRegionAdapter.SettingsGuardHooksKind, launcherPath),
            SettingsExemplar(launcherPath));
        return new RegionMap(format, new[] { guardHooks });
    }

    private static RegionMap BuildConfigMap(string format)
    {
        var structure = new Region(
            "structure",
            RegionMode.Canonical,
            new RegionLocator(JsonRegionAdapter.ConfigStructureKind, string.Empty),
            ConfigExemplar());
        var protectedPaths = new Region(
            "protected-paths",
            RegionMode.Backup,
            new RegionLocator(JsonRegionAdapter.JsonPathKind, ProtectedPathsLocator),
            null);
        return new RegionMap(format, new[] { structure, protectedPaths });
    }

    private static ReadOnlyMemory<byte> SettingsExemplar(string launcherPath)
    {
        SettingsMergeResult result = ClaudeSettingsWiring.AddGuardEntries("{}", launcherPath);
        return Encoding.UTF8.GetBytes(result.Json ?? "{}");
    }

    private static ReadOnlyMemory<byte> ConfigExemplar() =>
        Encoding.UTF8.GetBytes(SetupJson.Serialize(ProjectConfig.Default()));
}
