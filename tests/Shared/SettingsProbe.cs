// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using AgentGuard.Setup;

namespace AgentGuard.TestSupport;

/// <summary>
/// Helpers for asserting over the guard's hook entries in a parsed <c>.claude/settings.json</c> tree. A guard
/// group is identified by its command — a call to the guard binary running <c>hook pre</c>/<c>hook post</c> — not
/// by any marker.
/// </summary>
internal static class SettingsProbe
{
    /// <summary>Finds the guard's own hook group in an event array, identified by its guard command.</summary>
    /// <param name="root">The settings root object.</param>
    /// <param name="eventKey">The event key.</param>
    /// <returns>The guard group, or <see langword="null"/>.</returns>
    internal static JsonObject? GuardGroup(JsonObject root, string eventKey)
    {
        if (root["hooks"] is not JsonObject hooks || hooks[eventKey] is not JsonArray array)
        {
            return null;
        }

        return array.OfType<JsonObject>().FirstOrDefault(IsGuardGroup);
    }

    /// <summary>Returns the first hook command of a group.</summary>
    /// <param name="group">The group.</param>
    /// <returns>The command, or <see langword="null"/>.</returns>
    internal static string? CommandOf(JsonObject group) =>
        group["hooks"] is JsonArray hooks && hooks.FirstOrDefault() is JsonObject first
            ? first["command"]?.GetValue<string>()
            : null;

    /// <summary>Counts the guard-owned groups (those carrying a guard command) in an event array.</summary>
    /// <param name="root">The settings root object.</param>
    /// <param name="eventKey">The event key.</param>
    /// <returns>The count.</returns>
    internal static int GuardGroupCount(JsonObject root, string eventKey)
    {
        if (root["hooks"] is not JsonObject hooks || hooks[eventKey] is not JsonArray array)
        {
            return 0;
        }

        return array.OfType<JsonObject>().Count(IsGuardGroup);
    }

    /// <summary>Gets a value indicating whether any guard hook command is present anywhere in the settings.</summary>
    /// <param name="root">The settings root object.</param>
    /// <returns><see langword="true"/> when a guard hook command is present.</returns>
    internal static bool HasAnyGuardHook(JsonObject root)
    {
        if (root["hooks"] is not JsonObject hooks)
        {
            return false;
        }

        foreach (KeyValuePair<string, JsonNode?> pair in hooks)
        {
            if (pair.Value is JsonArray array && array.OfType<JsonObject>().Any(IsGuardGroup))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGuardGroup(JsonObject group)
    {
        if (group["hooks"] is not JsonArray hooks)
        {
            return false;
        }

        foreach (JsonObject hook in hooks.OfType<JsonObject>())
        {
            if (hook["command"] is JsonValue value
                && value.TryGetValue(out string? command)
                && HookCommand.IsGuardCommand(command))
            {
                return true;
            }
        }

        return false;
    }
}
