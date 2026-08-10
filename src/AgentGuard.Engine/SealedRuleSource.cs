// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The built-in Sealed floor: the grant-token store and the snapshot store. No Grant ever unlocks a Sealed
/// Rule, and Precheck denies a write that touches one unconditionally.
/// </summary>
internal sealed class SealedRuleSource : IRuleSource
{
    private readonly IReadOnlyList<Rule> _rules;

    private SealedRuleSource(IReadOnlyList<Rule> rules) => _rules = rules;

    /// <inheritdoc />
    public RuleOrigin Origin => RuleOrigin.Sealed;

    /// <inheritdoc />
    public IReadOnlyList<Rule> Rules => _rules;

    /// <summary>
    /// Creates the Sealed rule source anchored to the given project root.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to resolve the store directories.</param>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The rule source, as its interface.</returns>
    internal static IRuleSource Create(IPathCanonicalizer canonicalizer, string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        IVerifier verifier = NoChangeVerifier.Create();
        var rules = new List<Rule>
        {
            new(
                "sealed:snapshot-store",
                RuleOrigin.Sealed,
                DirectoryPrefixMatcher.Create(canonicalizer, CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.SnapshotStoreRelative)),
                verifier),
            new(
                "sealed:grant-store",
                RuleOrigin.Sealed,
                DirectoryPrefixMatcher.Create(canonicalizer, CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.GrantStoreRelative)),
                verifier),
        };
        return new SealedRuleSource(rules);
    }
}
