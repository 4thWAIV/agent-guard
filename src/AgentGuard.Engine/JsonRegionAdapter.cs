// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentGuard.Setup;

namespace AgentGuard.Engine;

/// <summary>
/// The JSON region adapter — the only format this build ships. It addresses three region kinds by their opaque
/// locator: the guard-hook groups in <c>.claude/settings.json</c> (delegated wholesale to
/// <see cref="ClaudeSettingsWiring"/>, which reads and rewrites the groups it identifies by their guard command
/// while preserving all other content), the config document's structure, and a top-level JSON key path. It never
/// duplicates the settings wiring or a JSON parser beyond a single <see cref="JsonNode"/> round-trip that keeps
/// every non-region byte intact.
/// </summary>
internal sealed class JsonRegionAdapter : IRegionAdapter
{
    /// <summary>
    /// The locator kind for the <c>.claude/settings.json</c> guard-hook groups; the locator argument is the
    /// absolute launcher path the canonical hooks are pinned to.
    /// </summary>
    internal const string SettingsGuardHooksKind = "settings-guard-hooks";

    /// <summary>
    /// The locator kind for the config document's structure: a valid object carrying a <c>protectedPaths</c> array.
    /// </summary>
    internal const string ConfigStructureKind = "config-structure";

    /// <summary>
    /// The locator kind for a single top-level JSON key, whose name is the locator argument after the <c>$.</c> root.
    /// </summary>
    internal const string JsonPathKind = "json-path";

    private const string FormatId = "json";
    private const string EmptyArrayJson = "[]";
    private const string JsonNullLiteral = "null";

    private static readonly string ProtectedPathsKey = ProjectConfig.ProtectedPathsWireKey;
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    private JsonRegionAdapter()
    {
    }

    /// <inheritdoc />
    public string Format => FormatId;

    /// <inheritdoc />
    public string? Read(ReadOnlyMemory<byte> file, RegionLocator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        string text = Decode(file);
        return locator.Kind switch
        {
            SettingsGuardHooksKind => ReadGuardHooks(text),
            ConfigStructureKind => ReadStructure(text),
            JsonPathKind => ReadJsonPath(text, KeyOf(locator.Argument)),
            _ => throw new InvalidOperationException($"Unknown region locator kind: {locator.Kind}"),
        };
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte>? Rewrite(ReadOnlyMemory<byte> file, RegionLocator locator, string value)
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(value);
        string text = Decode(file);
        return locator.Kind switch
        {
            SettingsGuardHooksKind => RewriteGuardHooks(text, locator.Argument),
            ConfigStructureKind => RewriteStructure(text),
            JsonPathKind => RewriteJsonPath(text, KeyOf(locator.Argument), value),
            _ => throw new InvalidOperationException($"Unknown region locator kind: {locator.Kind}"),
        };
    }

    /// <summary>
    /// Creates the JSON region adapter.
    /// </summary>
    /// <returns>The adapter, as its interface.</returns>
    internal static IRegionAdapter Create() => new JsonRegionAdapter();

    private static string Decode(ReadOnlyMemory<byte> file) => Encoding.UTF8.GetString(file.Span);

    private static ReadOnlyMemory<byte> Encode(string text) => Encoding.UTF8.GetBytes(text);

    private static string KeyOf(string argument) =>
        argument.StartsWith("$.", StringComparison.Ordinal) ? argument[2..] : argument;

    private static string? ReadGuardHooks(string text) => ClaudeSettingsWiring.ReadGuardRegion(text);

    private static ReadOnlyMemory<byte>? RewriteGuardHooks(string text, string launcherPath)
    {
        SettingsMergeResult result = ClaudeSettingsWiring.AddGuardEntries(text, launcherPath);
        return result.Success && result.Json is not null ? Encode(result.Json) : null;
    }

    private static string? ReadStructure(string text)
    {
        if (!SetupJson.TryParseObject(text, out JsonObject? root))
        {
            return null;
        }

        // The structure region's canonical content is the default protectedPaths shape — an empty array. Any array
        // reads as that same canonical "[]" (its contents are the mode-2 region's concern, deliberately normalized
        // away here); a missing, null, or non-array protectedPaths reads as its actual differing content, so the
        // mode-1 compare against the exemplar catches a broken shape and the rewrite re-asserts the array.
        JsonNode? node = root[ProtectedPathsKey];
        if (node is JsonArray)
        {
            return EmptyArrayJson;
        }

        return node is null ? JsonNullLiteral : node.ToJsonString();
    }

    private static ReadOnlyMemory<byte>? RewriteStructure(string text)
    {
        if (!SetupJson.TryParseObject(text, out JsonObject? root))
        {
            return null;
        }

        if (root[ProtectedPathsKey] is not JsonArray)
        {
            root[ProtectedPathsKey] = new JsonArray();
        }

        return Encode(root.ToJsonString(IndentedOptions));
    }

    private static string? ReadJsonPath(string text, string key)
    {
        if (!SetupJson.TryParseObject(text, out JsonObject? root))
        {
            return null;
        }

        JsonNode? node = root[key];
        return node is null ? EmptyArrayJson : node.ToJsonString();
    }

    private static ReadOnlyMemory<byte>? RewriteJsonPath(string text, string key, string value)
    {
        if (!SetupJson.TryParseObject(text, out JsonObject? root))
        {
            return null;
        }

        root[key] = JsonNode.Parse(value);
        return Encode(root.ToJsonString(IndentedOptions));
    }
}
