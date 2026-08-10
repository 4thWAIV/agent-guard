// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions.Contracts;

/// <summary>
/// Contributes Rules of one origin to the Protected Set. The Engine collects every source in precedence order
/// to assemble the set: a built-in source for the Sealed and System floor (the anti-cheat rules that are the
/// tool's whole point), a source per enabled Provider, and a source for the repository's own Project config.
/// </summary>
public interface IRuleSource
{
    /// <summary>
    /// Gets the origin of the Rules this source contributes, which fixes their precedence and grantability.
    /// </summary>
    RuleOrigin Origin { get; }

    /// <summary>
    /// Gets the Rules this source contributes to the Protected Set.
    /// </summary>
    IReadOnlyList<Rule> Rules { get; }
}
