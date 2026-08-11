// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Contributes an enabled Provider's Baseline Rules to the Protected Set, at <see cref="RuleOrigin.Provider"/>
/// precedence. Decoupling the source from the Provider keeps the Provider a pure bundle and lets the Engine
/// assemble every source uniformly.
/// </summary>
internal sealed class ProviderRuleSource : IRuleSource
{
    private readonly IReadOnlyList<Rule> _rules;

    private ProviderRuleSource(IReadOnlyList<Rule> rules) => _rules = rules;

    /// <inheritdoc />
    public RuleOrigin Origin => RuleOrigin.Provider;

    /// <inheritdoc />
    public IReadOnlyList<Rule> Rules => _rules;

    /// <summary>
    /// Creates a rule source from an enabled Provider's Baseline.
    /// </summary>
    /// <param name="provider">The enabled Provider whose Baseline Rules are contributed.</param>
    /// <returns>The rule source, as its interface.</returns>
    internal static IRuleSource Create(IProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return new ProviderRuleSource(provider.Baseline.Rules);
    }
}
