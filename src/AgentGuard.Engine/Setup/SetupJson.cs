// Copyright (c) 4thWAIV. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentGuard.Engine;

namespace AgentGuard.Setup;

/// <summary>
/// Serializes and deserializes the setup records through the source-generated context, so JSON handling stays
/// free of runtime reflection.
/// </summary>
internal static class SetupJson
{
    /// <summary>
    /// Serializes the machine install state.
    /// </summary>
    /// <param name="value">The state.</param>
    /// <returns>The indented JSON.</returns>
    internal static string Serialize(InstallState value) =>
        JsonSerializer.Serialize(value, SetupJsonContext.Default.InstallState);

    /// <summary>
    /// Serializes the per-project state.
    /// </summary>
    /// <param name="value">The state.</param>
    /// <returns>The indented JSON.</returns>
    internal static string Serialize(ProjectState value) =>
        JsonSerializer.Serialize(value, SetupJsonContext.Default.ProjectState);

    /// <summary>
    /// Serializes the per-project configuration.
    /// </summary>
    /// <param name="value">The configuration.</param>
    /// <returns>The indented JSON.</returns>
    internal static string Serialize(ProjectConfig value) =>
        JsonSerializer.Serialize(value, SetupJsonContext.Default.ProjectConfig);

    /// <summary>
    /// Deserializes the machine install state.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>The state, or <see langword="null"/> when the JSON is null content.</returns>
    internal static InstallState? DeserializeInstallState(string json) =>
        JsonSerializer.Deserialize(json, SetupJsonContext.Default.InstallState);

    /// <summary>
    /// Deserializes the per-project state.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>The state, or <see langword="null"/> when the JSON is null content.</returns>
    internal static ProjectState? DeserializeProjectState(string json) =>
        JsonSerializer.Deserialize(json, SetupJsonContext.Default.ProjectState);

    /// <summary>
    /// Deserializes the per-project configuration.
    /// </summary>
    /// <param name="json">The JSON.</param>
    /// <returns>The configuration, or <see langword="null"/> when the JSON is null content.</returns>
    internal static ProjectConfig? DeserializeProjectConfig(string json) =>
        JsonSerializer.Deserialize(json, SetupJsonContext.Default.ProjectConfig);

    /// <summary>
    /// Determines whether text parses as a JSON object — the single parse-as-object check the config condition and
    /// the config-creation helper both use.
    /// </summary>
    /// <param name="content">The JSON text.</param>
    /// <param name="parseError">The parse-error message when the text is not well-formed JSON; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the text is a well-formed JSON object.</returns>
    internal static bool TryParseObject(string content, out string? parseError) =>
        TryParse(content, out _, out parseError);

    /// <summary>
    /// Parses text as a JSON object, handing back the parsed node. This is the single owner of the
    /// parse-string-to-<see cref="JsonObject"/> idiom; a malformed-JSON error is swallowed as a failed parse.
    /// </summary>
    /// <param name="content">The JSON text.</param>
    /// <param name="root">The parsed object when the text is a well-formed JSON object; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the text is a well-formed JSON object.</returns>
    internal static bool TryParseObject(string content, [NotNullWhen(true)] out JsonObject? root) =>
        TryParse(content, out root, out _);

    private static bool TryParse(string content, [NotNullWhen(true)] out JsonObject? root, out string? parseError)
    {
        root = null;
        parseError = null;
        try
        {
            root = JsonNode.Parse(content) as JsonObject;
            return root is not null;
        }
        catch (JsonException exception)
        {
            parseError = exception.Message;
            return false;
        }
    }
}
