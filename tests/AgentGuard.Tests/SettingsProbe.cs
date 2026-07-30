// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Linq;
using System.Text.Json.Nodes;
using AgentGuard.Setup;

namespace AgentGuard.Tests;

/// <summary>
/// Helpers for asserting over the guard's hook entries in a parsed <c>.claude/settings.json</c> tree.
/// </summary>
internal static class SettingsProbe
{
    /// <summary>Finds the group in an event array that has the given matcher and carries the sentinel.</summary>
    /// <param name="root">The settings root object.</param>
    /// <param name="eventKey">The event key.</param>
    /// <param name="matcher">The matcher to match.</param>
    /// <returns>The guard group, or <see langword="null"/>.</returns>
    internal static JsonObject? GuardGroup(JsonObject root, string eventKey, string matcher)
    {
        if (root["hooks"] is not JsonObject hooks || hooks[eventKey] is not JsonArray array)
        {
            return null;
        }

        return array.OfType<JsonObject>().FirstOrDefault(group =>
            string.Equals(group["matcher"]?.GetValue<string>(), matcher, StringComparison.Ordinal)
            && CommandOf(group)?.Contains(ClaudeSettingsWiring.Sentinel, StringComparison.Ordinal) == true);
    }

    /// <summary>Returns the first hook command of a group.</summary>
    /// <param name="group">The group.</param>
    /// <returns>The command, or <see langword="null"/>.</returns>
    internal static string? CommandOf(JsonObject group) =>
        group["hooks"] is JsonArray hooks && hooks.FirstOrDefault() is JsonObject first
            ? first["command"]?.GetValue<string>()
            : null;

    /// <summary>Counts the guard-owned groups (those carrying the sentinel) in an event array.</summary>
    /// <param name="root">The settings root object.</param>
    /// <param name="eventKey">The event key.</param>
    /// <returns>The count.</returns>
    internal static int GuardGroupCount(JsonObject root, string eventKey)
    {
        if (root["hooks"] is not JsonObject hooks || hooks[eventKey] is not JsonArray array)
        {
            return 0;
        }

        return array.OfType<JsonObject>().Count(group =>
            CommandOf(group)?.Contains(ClaudeSettingsWiring.Sentinel, StringComparison.Ordinal) == true);
    }

    /// <summary>Gets a value indicating whether any command anywhere carries the sentinel.</summary>
    /// <param name="root">The settings root object.</param>
    /// <returns><see langword="true"/> when a sentinel command is present.</returns>
    internal static bool HasAnySentinel(JsonObject root) =>
        root.ToJsonString().Contains(ClaudeSettingsWiring.Sentinel, StringComparison.Ordinal);
}
