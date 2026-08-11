// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AgentGuard.Engine;

/// <summary>
/// The per-project configuration persisted at <c>.agentguard/config.json</c>: the single shared type the engine
/// reads (its <see cref="ProtectedPaths"/> become Project rules) and the setup surface writes. Its schema is only
/// the fields actually consumed; a config that omits <see cref="ProtectedPaths"/> contributes no rules.
/// </summary>
internal sealed record ProjectConfig
{
    /// <summary>
    /// The on-the-wire JSON key of <see cref="ProtectedPaths"/>, derived once from the property name through the
    /// same camelCase policy the serializer applies, so a rename of the property cannot leave a hardcoded key
    /// pointing at a name that no longer exists.
    /// </summary>
    internal static readonly string ProtectedPathsWireKey =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(ProtectedPaths));

    /// <summary>
    /// Gets the project protected-paths; defaults to empty when the file omits the field.
    /// </summary>
    public IReadOnlyList<string> ProtectedPaths { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates the default configuration: no project protected-paths.
    /// </summary>
    /// <returns>The default configuration.</returns>
    internal static ProjectConfig Default() => new();
}
