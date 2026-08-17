// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The union of all active Rules' matchers, assembled from the four Rule sources in precedence order. A path
/// can match several Rules; the caller inspects the matched Rules' origins to decide how to treat the write.
/// </summary>
internal sealed class ProtectedSet : IProtectedSet
{
    private readonly IReadOnlyList<Rule> _rules;

    private ProtectedSet(IReadOnlyList<Rule> rules) => _rules = rules;

    /// <inheritdoc />
    public IReadOnlyList<Rule> Match(CanonicalPath canonicalPath) =>
        _rules.Where(rule => rule.Matcher.Matches(canonicalPath)).ToList();

    /// <summary>
    /// Creates a Protected Set from the given rule sources, in precedence order.
    /// </summary>
    /// <param name="sources">The rule sources whose Rules are unioned.</param>
    /// <returns>The Protected Set, as its interface.</returns>
    internal static IProtectedSet Create(IReadOnlyList<IRuleSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var rules = new List<Rule>();
        foreach (IRuleSource source in sources)
        {
            rules.AddRange(source.Rules);
        }

        return new ProtectedSet(rules);
    }
}
