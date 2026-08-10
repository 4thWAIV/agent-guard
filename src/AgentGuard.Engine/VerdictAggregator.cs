// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Engine.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// Aggregates the Guards' verdicts into one decision: any single deny blocks the call; otherwise any warning is
/// surfaced; otherwise the call is allowed.
/// </summary>
internal static class VerdictAggregator
{
    /// <summary>
    /// Aggregates the given verdicts.
    /// </summary>
    /// <param name="verdicts">The individual Guard verdicts.</param>
    /// <returns>The aggregate verdict.</returns>
    internal static Verdict Aggregate(IReadOnlyList<Verdict> verdicts)
    {
        ArgumentNullException.ThrowIfNull(verdicts);
        var denials = new List<string>();
        var warnings = new List<string>();
        foreach (Verdict verdict in verdicts)
        {
            switch (verdict.Kind)
            {
                case VerdictKind.Deny:
                    denials.Add(verdict.Message ?? "denied");
                    break;
                case VerdictKind.Warn:
                    warnings.Add(verdict.Message ?? "warning");
                    break;
                case VerdictKind.Allow:
                default:
                    break;
            }
        }

        if (denials.Count > 0)
        {
            return Verdict.Deny(string.Join("\n", denials));
        }

        if (warnings.Count > 0)
        {
            return Verdict.Warn(string.Join("\n", warnings));
        }

        return Verdict.Allow();
    }
}
