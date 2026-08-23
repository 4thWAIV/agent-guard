// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// A Provider's opt-in recommended default set of Rules, applied as a template over a ruleset when the
/// Provider is enabled — the way a lint "recommended" set works.
/// </summary>
/// <param name="Name">A stable, human-readable identifier for the baseline.</param>
/// <param name="Rules">The default Rules the Provider recommends.</param>
public sealed record Baseline(string Name, IReadOnlyList<Rule> Rules);
