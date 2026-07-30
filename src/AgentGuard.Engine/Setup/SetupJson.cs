// Copyright (c) 4thWAIV. All rights reserved.

using System.Text.Json;

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
    internal static string Serialize(GuardConfig value) =>
        JsonSerializer.Serialize(value, SetupJsonContext.Default.GuardConfig);

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
}
