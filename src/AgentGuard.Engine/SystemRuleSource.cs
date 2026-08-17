// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The built-in System floor: the guard's own shipped code, the grant public key, and the Claude Code runtime
/// wiring. A Grant can unlock a System Rule, but without one Precheck denies a write that touches it.
/// </summary>
internal sealed class SystemRuleSource : IRuleSource
{
    private readonly IReadOnlyList<Rule> _rules;

    private SystemRuleSource(IReadOnlyList<Rule> rules) => _rules = rules;

    /// <inheritdoc />
    public RuleOrigin Origin => RuleOrigin.System;

    /// <inheritdoc />
    public IReadOnlyList<Rule> Rules => _rules;

    /// <summary>
    /// Creates the System rule source anchored to the given project root.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to resolve the System paths.</param>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The rule source, as its interface.</returns>
    internal static IRuleSource Create(IPathCanonicalizer canonicalizer, string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        IVerifier verifier = NoChangeVerifier.Create();
        var rules = new List<Rule>();
        rules.Add(new Rule(
            "system:claude-settings",
            RuleOrigin.System,
            ExactPathMatcher.Create(canonicalizer, CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.ClaudeSettingsRelative)),
            verifier));
        rules.Add(new Rule(
            "system:grant-public-key",
            RuleOrigin.System,
            ExactPathMatcher.Create(canonicalizer, CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.GrantPublicKeyRelative)),
            verifier));
        return new SystemRuleSource(rules);
    }
}
