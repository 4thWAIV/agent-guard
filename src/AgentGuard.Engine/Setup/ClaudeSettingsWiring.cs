// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentGuard.Engine;

namespace AgentGuard.Setup;

/// <summary>
/// Merges, strips, and inspects the guard's hook entries in <c>.claude/settings.json</c>. A guard entry is any
/// matcher group whose command carries the <see cref="Sentinel"/>, so it is identified regardless of its path and
/// distinct from any user hook that merely invokes a <c>guard</c>. The merge preserves every non-guard key and
/// every non-guard hook group unchanged, adds the guard entries when absent, refreshes a guard entry whose path
/// is stale, and refuses (leaving the file untouched) when the existing shape cannot be merged safely.
/// </summary>
internal static class ClaudeSettingsWiring
{
    /// <summary>
    /// The sentinel argument that marks a hook command as guard-owned.
    /// </summary>
    internal const string Sentinel = "--agentguard-owned";

    private const string HostToken = "--host claude-code";
    private const string PreEventToken = "pre";
    private const string PostEventToken = "post";
    private const string PreEventKey = "PreToolUse";
    private const string PostEventKey = "PostToolUse";
    private const string HooksKey = "hooks";
    private const string BashMatcher = ClaudeCodeHostAdapter.ShellToolName;

    private static readonly string EditMatcher = string.Join('|', ClaudeCodeHostAdapter.EditToolNames);
    private static readonly string[] MatcherOrder = { EditMatcher, BashMatcher };
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    /// <summary>
    /// Builds the exact hook command for an event, embedding the absolute launcher path, the host, and the
    /// sentinel.
    /// </summary>
    /// <param name="absolutePath">The absolute launcher path (<c>~/.agentguard/bin/guard</c>).</param>
    /// <param name="eventToken">The event token, <c>pre</c> or <c>post</c>.</param>
    /// <returns>The command string.</returns>
    internal static string Command(string absolutePath, string eventToken) =>
        $"{absolutePath} hook {eventToken} {HostToken} {Sentinel}";

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

        RewriteGuardGroups(hooks, PreEventKey, Command(absolutePath, PreEventToken));
        RewriteGuardGroups(hooks, PostEventKey, Command(absolutePath, PostEventToken));
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

        SettingsInspection pre = InspectEvent(hooks, PreEventKey, Command(absolutePath, PreEventToken));
        return pre.Health == SettingsHealth.Ok
            ? InspectEvent(hooks, PostEventKey, Command(absolutePath, PostEventToken))
            : pre;
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
        array.Add(BuildGroup(EditMatcher, command));
        array.Add(BuildGroup(BashMatcher, command));
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

        foreach (string matcher in MatcherOrder)
        {
            JsonObject? group = FindGuardGroup(array, matcher);
            if (group is null)
            {
                return new SettingsInspection(
                    SettingsHealth.Missing, $"{eventKey} '{matcher}' guard hook entry is missing");
            }

            if (!string.Equals(GroupCommand(group), expectedCommand, StringComparison.Ordinal))
            {
                return new SettingsInspection(
                    SettingsHealth.Stale, $"{eventKey} '{matcher}' guard hook entry points at a stale path");
            }
        }

        return new SettingsInspection(SettingsHealth.Ok, "guard hook entries present and current");
    }

    private static JsonObject? FindGuardGroup(JsonArray array, string matcher)
    {
        foreach (JsonNode? element in array)
        {
            if (element is JsonObject group
                && IsGuardGroup(group)
                && string.Equals(GroupMatcher(group), matcher, StringComparison.Ordinal))
            {
                return group;
            }
        }

        return null;
    }

    private static JsonObject BuildGroup(string matcher, string command) => new()
    {
        ["matcher"] = matcher,
        [HooksKey] = new JsonArray(new JsonObject
        {
            ["type"] = "command",
            ["command"] = command,
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
                && hook["command"] is JsonValue value
                && value.TryGetValue(out string? command)
                && command is not null
                && command.Contains(Sentinel, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GroupMatcher(JsonObject group) =>
        group["matcher"] is JsonValue value && value.TryGetValue(out string? matcher) ? matcher : null;

    private static string? GroupCommand(JsonObject group)
    {
        if (group[HooksKey] is JsonArray hooks
            && hooks.Count > 0
            && hooks[0] is JsonObject first
            && first["command"] is JsonValue value
            && value.TryGetValue(out string? command))
        {
            return command;
        }

        return null;
    }

    private static bool IsStringValue(JsonNode node) =>
        node is JsonValue value && value.TryGetValue(out string? _);
}
