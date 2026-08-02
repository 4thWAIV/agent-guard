// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentGuard.Engine;

namespace AgentGuard.Setup;

/// <summary>
/// Merges, strips, inspects, and reads the guard's hook entries in <c>.claude/settings.json</c>. A guard entry is
/// any matcher group carrying a hook whose command is the guard's own — a call to the guard binary running
/// <c>hook pre</c>/<c>hook post</c> (see <see cref="HookCommand.IsGuardCommand"/>) — so it is identified by that
/// command regardless of its launcher path and distinct from any user hook that merely invokes a <c>guard</c>
/// subcommand. The guard writes a single group per event whose matcher covers every command-running tool. The
/// merge preserves every non-guard key and every non-guard hook group unchanged, adds the guard entries when
/// absent, refreshes a guard entry whose path is stale, and refuses (leaving the file untouched) when the existing
/// shape cannot be merged safely.
/// </summary>
internal static class ClaudeSettingsWiring
{
    private const string PreEventKey = "PreToolUse";
    private const string PostEventKey = "PostToolUse";
    private const string HooksKey = "hooks";
    private const string MatcherKey = "matcher";
    private const string CommandKey = "command";

    private static readonly string GuardMatcher = BuildGuardMatcher();
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    /// <summary>
    /// Gets the tool matcher the guard's Pre/Post hook groups fire on — every command-running tool
    /// (<c>Edit|Write|MultiEdit|NotebookEdit|Bash|Monitor|PowerShell|mcp__.*</c>). Exposed as the single source so
    /// callers and tests assert against the assembled value instead of re-spelling the literal.
    /// </summary>
    internal static string ToolMatcher => GuardMatcher;

    /// <summary>
    /// Builds the exact hook command for an event, embedding the absolute launcher path and the host.
    /// </summary>
    /// <param name="absolutePath">The absolute launcher path (<c>~/.agentguard/bin/guard</c>).</param>
    /// <param name="eventToken">The event token, <c>pre</c> or <c>post</c>.</param>
    /// <returns>The command string.</returns>
    internal static string Command(string absolutePath, string eventToken) =>
        HookCommand.ForEvent(absolutePath, eventToken);

    /// <summary>
    /// Merges the guard's Pre/Post hook entries into the settings, preserving all non-guard content.
    /// </summary>
    /// <param name="existingJson">The current settings JSON, or <see langword="null"/> when the file is absent.</param>
    /// <param name="absolutePath">The absolute launcher path.</param>
    /// <returns>The merge result: merged JSON, or a conflict leaving the file untouched.</returns>
    internal static SettingsMergeResult AddGuardEntries(string? existingJson, string absolutePath)
    {
        JsonObject root;
        if (string.IsNullOrWhiteSpace(existingJson))
        {
            root = new JsonObject();
        }
        else
        {
            string? parseError = ParseObject(existingJson, out JsonObject? parsed);
            if (parseError is not null || parsed is null)
            {
                return SettingsMergeResult.Refused($".claude/settings.json {parseError}");
            }

            root = parsed;
        }

        string? hooksError = GetHooksObject(root, create: true, out JsonObject? hooks);
        if (hooksError is not null || hooks is null)
        {
            return SettingsMergeResult.Refused($".claude/settings.json {hooksError}");
        }

        string? shapeError = ValidateEventShape(hooks, PreEventKey) ?? ValidateEventShape(hooks, PostEventKey);
        if (shapeError is not null)
        {
            return SettingsMergeResult.Refused($".claude/settings.json {shapeError}");
        }

        RewriteGuardGroups(hooks, PreEventKey, Command(absolutePath, HookCommand.PreEvent));
        RewriteGuardGroups(hooks, PostEventKey, Command(absolutePath, HookCommand.PostEvent));
        return SettingsMergeResult.Merged(root.ToJsonString(IndentedOptions));
    }

    /// <summary>
    /// Strips the guard's hook entries from the settings, preserving all non-guard content and leaving valid JSON.
    /// </summary>
    /// <param name="existingJson">The current settings JSON.</param>
    /// <returns>The result: the stripped JSON, or a conflict when the file cannot be parsed and is left as is.</returns>
    internal static SettingsMergeResult RemoveGuardEntries(string existingJson)
    {
        if (string.IsNullOrWhiteSpace(existingJson))
        {
            return SettingsMergeResult.Merged("{}");
        }

        string? parseError = ParseObject(existingJson, out JsonObject? root);
        if (parseError is not null || root is null)
        {
            return SettingsMergeResult.Refused($".claude/settings.json {parseError}");
        }

        string? hooksError = GetHooksObject(root, create: false, out JsonObject? hooks);
        if (hooksError is not null)
        {
            return SettingsMergeResult.Refused($".claude/settings.json {hooksError}");
        }

        if (hooks is null)
        {
            return SettingsMergeResult.Merged(root.ToJsonString(IndentedOptions));
        }

        StripEvent(hooks, PreEventKey);
        StripEvent(hooks, PostEventKey);
        if (hooks.Count == 0)
        {
            root.Remove(HooksKey);
        }

        return SettingsMergeResult.Merged(root.ToJsonString(IndentedOptions));
    }

    /// <summary>
    /// Inspects the settings for the guard's hook entries at the given absolute launcher path.
    /// </summary>
    /// <param name="existingJson">The current settings JSON, or <see langword="null"/> when the file is absent.</param>
    /// <param name="absolutePath">The absolute launcher path expected in each guard entry.</param>
    /// <returns>The inspection result.</returns>
    internal static SettingsInspection Inspect(string? existingJson, string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(existingJson))
        {
            return new SettingsInspection(SettingsHealth.Missing, "no .claude/settings.json guard hook entries");
        }

        string? parseError = ParseObject(existingJson, out JsonObject? root);
        if (parseError is not null || root is null)
        {
            return new SettingsInspection(SettingsHealth.Malformed, $".claude/settings.json {parseError}");
        }

        string? hooksError = GetHooksObject(root, create: false, out JsonObject? hooks);
        if (hooksError is not null)
        {
            return new SettingsInspection(SettingsHealth.Malformed, $".claude/settings.json {hooksError}");
        }

        if (hooks is null)
        {
            return new SettingsInspection(SettingsHealth.Missing, "no hook entries present");
        }

        SettingsInspection pre = InspectEvent(hooks, PreEventKey, Command(absolutePath, HookCommand.PreEvent));
        return pre.Health == SettingsHealth.Ok
            ? InspectEvent(hooks, PostEventKey, Command(absolutePath, HookCommand.PostEvent))
            : pre;
    }

    /// <summary>
    /// Reads the guard's own hook region — the guard-owned groups (identified by command) across the Pre and Post
    /// events — as a normalized string, so a mode-1 canonical check compares the settings' actual guard hooks to
    /// the guard's computed value rather than a health label. Key order and whitespace are normalized away; an
    /// injected hook inside a guard group, a drifted launcher path, a renamed matcher, and a missing or extra group
    /// all change the returned value. Returns <see langword="null"/> only when the file or its hooks shape cannot be
    /// parsed, which forces the whole-file snapshot fallback.
    /// </summary>
    /// <param name="existingJson">The current settings JSON, or <see langword="null"/> when the file is absent.</param>
    /// <returns>The normalized guard-region value, or <see langword="null"/> when unparseable.</returns>
    internal static string? ReadGuardRegion(string? existingJson)
    {
        if (string.IsNullOrWhiteSpace(existingJson))
        {
            return EmptyRegion();
        }

        if (ParseObject(existingJson, out JsonObject? root) is not null || root is null)
        {
            return null;
        }

        JsonNode? hooksNode = root[HooksKey];
        if (hooksNode is null)
        {
            return EmptyRegion();
        }

        if (hooksNode is not JsonObject hooks)
        {
            return null;
        }

        if (!TryCollectGuardGroups(hooks, PreEventKey, out JsonArray pre)
            || !TryCollectGuardGroups(hooks, PostEventKey, out JsonArray post))
        {
            return null;
        }

        var region = new JsonObject
        {
            [PreEventKey] = pre,
            [PostEventKey] = post,
        };
        return CanonicalString(region);
    }

    private static string? ParseObject(string json, out JsonObject? root)
    {
        root = null;
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            return $"is not valid JSON: {exception.Message}";
        }

        if (parsed is not JsonObject obj)
        {
            return "is not a JSON object";
        }

        root = obj;
        return null;
    }

    private static string? GetHooksObject(JsonObject root, bool create, out JsonObject? hooks)
    {
        hooks = null;
        JsonNode? node = root[HooksKey];
        if (node is null)
        {
            if (create)
            {
                hooks = new JsonObject();
                root[HooksKey] = hooks;
            }

            return null;
        }

        if (node is not JsonObject obj)
        {
            return "'hooks' is not a JSON object";
        }

        hooks = obj;
        return null;
    }

    private static string? ValidateEventShape(JsonObject hooks, string eventKey)
    {
        JsonNode? node = hooks[eventKey];
        if (node is null)
        {
            return null;
        }

        if (node is not JsonArray array)
        {
            return $"'hooks.{eventKey}' is not an array";
        }

        foreach (JsonNode? element in array)
        {
            if (element is not JsonObject group)
            {
                return $"'hooks.{eventKey}' has a non-object entry";
            }

            if (group["matcher"] is JsonNode matcher && !IsStringValue(matcher))
            {
                return $"'hooks.{eventKey}' has an entry whose matcher is not a string";
            }

            if (group[HooksKey] is not null and not JsonArray)
            {
                return $"'hooks.{eventKey}' has an entry whose 'hooks' is not an array";
            }
        }

        return null;
    }

    private static void RewriteGuardGroups(JsonObject hooks, string eventKey, string command)
    {
        JsonArray array;
        if (hooks[eventKey] is JsonArray existing)
        {
            array = existing;
        }
        else
        {
            array = new JsonArray();
            hooks[eventKey] = array;
        }

        StripGuardGroups(array);
        array.Add(BuildGroup(GuardMatcher, command));
    }

    private static void StripEvent(JsonObject hooks, string eventKey)
    {
        if (hooks[eventKey] is not JsonArray array)
        {
            return;
        }

        StripGuardGroups(array);
        if (array.Count == 0)
        {
            hooks.Remove(eventKey);
        }
    }

    private static void StripGuardGroups(JsonArray array)
    {
        for (int index = array.Count - 1; index >= 0; index--)
        {
            if (array[index] is JsonObject group && IsGuardGroup(group))
            {
                array.RemoveAt(index);
            }
        }
    }

    private static SettingsInspection InspectEvent(JsonObject hooks, string eventKey, string expectedCommand)
    {
        JsonNode? node = hooks[eventKey];
        if (node is null)
        {
            return new SettingsInspection(SettingsHealth.Missing, $"{eventKey} has no guard hook entry");
        }

        if (node is not JsonArray array)
        {
            return new SettingsInspection(SettingsHealth.Malformed, $"'hooks.{eventKey}' is not an array");
        }

        JsonObject? group = FindGuardGroup(array);
        if (group is null)
        {
            return new SettingsInspection(SettingsHealth.Missing, $"{eventKey} guard hook entry is missing");
        }

        if (!string.Equals(GroupCommand(group), expectedCommand, StringComparison.Ordinal))
        {
            return new SettingsInspection(
                SettingsHealth.Stale, $"{eventKey} guard hook entry points at a stale path");
        }

        return new SettingsInspection(SettingsHealth.Ok, "guard hook entries present and current");
    }

    private static JsonObject? FindGuardGroup(JsonArray array)
    {
        foreach (JsonNode? element in array)
        {
            if (element is JsonObject group && IsGuardGroup(group))
            {
                return group;
            }
        }

        return null;
    }

    private static JsonObject BuildGroup(string matcher, string command) => new()
    {
        [MatcherKey] = matcher,
        [HooksKey] = new JsonArray(new JsonObject
        {
            ["type"] = "command",
            [CommandKey] = command,
        }),
    };

    private static bool IsGuardGroup(JsonObject group)
    {
        if (group[HooksKey] is not JsonArray hooks)
        {
            return false;
        }

        foreach (JsonNode? element in hooks)
        {
            if (element is JsonObject hook
                && hook[CommandKey] is JsonValue value
                && value.TryGetValue(out string? command)
                && HookCommand.IsGuardCommand(command))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GroupCommand(JsonObject group)
    {
        if (group[HooksKey] is JsonArray hooks
            && hooks.Count > 0
            && hooks[0] is JsonObject first
            && first[CommandKey] is JsonValue value
            && value.TryGetValue(out string? command))
        {
            return command;
        }

        return null;
    }

    private static bool IsStringValue(JsonNode node) =>
        node is JsonValue value && value.TryGetValue(out string? _);

    private static string BuildGuardMatcher() =>
        string.Join('|', ClaudeCodeHostAdapter.HookMatcherToolTokens);

    private static bool TryCollectGuardGroups(JsonObject hooks, string eventKey, out JsonArray guardGroups)
    {
        guardGroups = new JsonArray();
        JsonNode? node = hooks[eventKey];
        if (node is null)
        {
            return true;
        }

        if (node is not JsonArray array)
        {
            return false;
        }

        foreach (JsonNode? element in array)
        {
            if (element is JsonObject group && IsGuardGroup(group))
            {
                guardGroups.Add(group.DeepClone());
            }
        }

        return true;
    }

    private static string EmptyRegion() => CanonicalString(new JsonObject
    {
        [PreEventKey] = new JsonArray(),
        [PostEventKey] = new JsonArray(),
    });

    private static string CanonicalString(JsonNode node) => Canonicalize(node)!.ToJsonString();

    private static JsonNode? Canonicalize(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var normalizedObject = new JsonObject();
                foreach (KeyValuePair<string, JsonNode?> property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    normalizedObject[property.Key] = Canonicalize(property.Value);
                }

                return normalizedObject;
            case JsonArray array:
                var normalizedArray = new JsonArray();
                foreach (JsonNode? element in array)
                {
                    normalizedArray.Add(Canonicalize(element));
                }

                return normalizedArray;
            case JsonValue value:
                return value.DeepClone();
            default:
                return null;
        }
    }
}
