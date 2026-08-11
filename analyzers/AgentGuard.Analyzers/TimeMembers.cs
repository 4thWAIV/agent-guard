// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AgentGuard.Analyzers;

/// <summary>
/// The single source of truth for the <c>System.Environment</c> members that are time reads, not environment reads,
/// so the AG0012/AG0015 partition is defined once. <c>Environment.TickCount</c> and <c>TickCount64</c> read the
/// ambient clock, so the time rule (AG0015) claims them and the environment rule (AG0012) excludes them; both rules
/// read this one owner rather than spelling the member set twice.
/// </summary>
internal static class TimeMembers
{
    /// <summary>
    /// The <c>System.Environment</c> member names that read the ambient clock — <c>TickCount</c> and
    /// <c>TickCount64</c> — which AG0015 owns and AG0012 excludes.
    /// </summary>
    private static readonly ImmutableHashSet<string> EnvironmentClockMemberNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "TickCount",
        "TickCount64");

    /// <summary>
    /// Gets a value indicating whether <paramref name="member"/> is a <c>System.Environment</c> clock member
    /// (<c>TickCount</c> or <c>TickCount64</c>) that AG0015 owns and AG0012 excludes.
    /// </summary>
    /// <param name="member">The referenced member.</param>
    /// <returns><see langword="true"/> when the member name is an environment clock member name.</returns>
    internal static bool IsEnvironmentClockMember(ISymbol member) => EnvironmentClockMemberNames.Contains(member.Name);
}
