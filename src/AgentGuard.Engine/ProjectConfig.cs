// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;

namespace AgentGuard.Engine;

/// <summary>
/// The per-project configuration persisted at <c>.agentguard/config.json</c>: the single shared type the engine
/// reads (its <see cref="ProtectedPaths"/> become Project rules) and the setup surface writes. Its schema is only
/// the fields actually consumed; a config that omits <see cref="ProtectedPaths"/> contributes no rules.
/// </summary>
internal sealed record ProjectConfig
{
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
