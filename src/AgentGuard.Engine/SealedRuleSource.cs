// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

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
    /// <param name="directorySeparator">The OS directory separator (owned by <c>IPlatformFileSystem</c>), threaded
    /// through to the directory-prefix matchers.</param>
    /// <returns>The rule source, as its interface.</returns>
    internal static IRuleSource Create(IPathCanonicalizer canonicalizer, string projectRoot, char directorySeparator)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        IVerifier verifier = NoChangeVerifier.Create();
        var rules = new List<Rule>
        {
            new(
                "sealed:snapshot-store",
                RuleOrigin.Sealed,
                DirectoryPrefixMatcher.Create(canonicalizer, CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.SnapshotStoreRelative), directorySeparator),
                verifier),
            new(
                "sealed:grant-store",
                RuleOrigin.Sealed,
                DirectoryPrefixMatcher.Create(canonicalizer, CoreSystemPaths.Absolute(projectRoot, CoreSystemPaths.GrantStoreRelative), directorySeparator),
                verifier),
        };
        return new SealedRuleSource(rules);
    }
}
