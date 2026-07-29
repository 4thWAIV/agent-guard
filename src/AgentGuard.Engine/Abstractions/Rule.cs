// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// A per-path policy the File Guard applies: a matcher, whether a Grant can unlock it, and the Verifier that
/// judges a change to it. Rules are data, not mechanisms. In v1 the required grant scope is always
/// <see cref="GrantScope.All"/>; <see cref="GrantScope.Part"/> is a future addition.
/// </summary>
/// <param name="Name">A stable, human-readable identifier for the rule.</param>
/// <param name="Origin">Where the rule came from, which fixes its precedence and whether a Grant may unlock it.</param>
/// <param name="Matcher">Decides whether a canonical write target is governed by this rule.</param>
/// <param name="Verifier">Judges whether a landed change conforms, when a Grant's scope calls for it.</param>
public sealed record Rule(
    string Name,
    RuleOrigin Origin,
    IPathMatcher Matcher,
    IVerifier Verifier)
{
    /// <summary>
    /// Gets a value indicating whether any Grant can authorize a change to a matched path. Derived from
    /// <see cref="Origin"/> — only a Sealed rule is never unlockable — so the invariant cannot be contradicted.
    /// </summary>
    public bool GrantUnlockable => this.Origin != RuleOrigin.Sealed;
}
