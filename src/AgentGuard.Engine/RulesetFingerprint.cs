// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Computes a stable hash of the assembled protected set — the ordered Rule names and origins, the enabled
/// Provider languages, and the effective skip-list identity. Capture records it and Post compares it, so a call
/// whose membership changed between its Pre and Post is denied rather than diffed across a changed universe.
/// Pre and Post are separate processes that build the same composition, so they compute the same fingerprint.
/// </summary>
internal static class RulesetFingerprint
{
    /// <summary>
    /// Computes the fingerprint from the assembled sources, enabled providers, and effective skip list. The skip
    /// portion is derived from the skip rules' own identities, so adding a skip rule cannot silently
    /// under-represent the fingerprint.
    /// </summary>
    /// <param name="sources">The rule sources, in precedence order.</param>
    /// <param name="providers">The enabled providers.</param>
    /// <param name="skipRules">The effective directory skip rules.</param>
    /// <returns>The fingerprint, as a lowercase hex string.</returns>
    internal static string Compute(
        IReadOnlyList<IRuleSource> sources,
        IReadOnlyList<IProvider> providers,
        IReadOnlyList<IDirectorySkipRule> skipRules)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(skipRules);
        var builder = new StringBuilder();
        foreach (IRuleSource source in sources)
        {
            builder.Append("source:").Append(source.Origin.ToString()).Append('\n');
            foreach (Rule rule in source.Rules)
            {
                builder.Append("rule:")
                    .Append(rule.Origin.ToString())
                    .Append('|')
                    .Append(rule.Name)
                    .Append('\n');
            }
        }

        foreach (IProvider provider in providers)
        {
            builder.Append("provider:").Append(provider.Language).Append('\n');
        }

        foreach (IDirectorySkipRule skipRule in skipRules)
        {
            builder.Append("skip:").Append(skipRule.Identity).Append('\n');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(hash);
    }
}
