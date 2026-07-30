// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;

namespace AgentGuard.Setup;

/// <summary>
/// The per-project configuration persisted at <c>.agentguard/config.json</c>: the enabled providers and the
/// project protected-paths. It is scaffolding for a later build — nothing consumes it yet — so <c>doctor</c> only
/// checks that it exists and parses, never whether a provider is "enabled".
/// </summary>
/// <param name="EnabledProviders">The enabled provider identifiers; defaults to the C# provider.</param>
/// <param name="ProtectedPaths">The project protected-paths; defaults to empty.</param>
internal sealed record GuardConfig(IReadOnlyList<string> EnabledProviders, IReadOnlyList<string> ProtectedPaths)
{
    private static readonly IReadOnlyList<string> DefaultProviders = new[] { "csharp" };

    /// <summary>
    /// Creates the default configuration: the C# provider enabled and no project protected-paths.
    /// </summary>
    /// <returns>The default configuration.</returns>
    internal static GuardConfig Default() => new(DefaultProviders, Array.Empty<string>());
}
