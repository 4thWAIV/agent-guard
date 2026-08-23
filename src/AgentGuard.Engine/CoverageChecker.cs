// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using AgentGuard.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// Answers Coverage: whether an active Grant authorizes a change to a path. It reuses each Grant's covered-path
/// matchers, which share the canonicalizing match logic Rules use.
/// </summary>
internal static class CoverageChecker
{
    /// <summary>
    /// Returns the first active Grant that covers the path, or <see langword="null"/> when none does. Used to
    /// hand a covering Grant to a Verifier for its envelope check.
    /// </summary>
    /// <param name="grants">The active Grants.</param>
    /// <param name="canonicalPath">The canonical path of the changed file.</param>
    /// <returns>The covering Grant, or <see langword="null"/>.</returns>
    internal static Grant? FindCovering(IReadOnlyList<Grant> grants, CanonicalPath canonicalPath)
    {
        ArgumentNullException.ThrowIfNull(grants);
        return grants.FirstOrDefault(grant => grant.CoveredPaths.Any(matcher => matcher.Matches(canonicalPath)));
    }

    /// <summary>
    /// Returns the first active Grant that authorizes the change outright — it covers the path and its scope is
    /// <see cref="GrantScope.All"/>. This is the single authorization predicate both Precheck (to unlock a System
    /// write) and Postcheck (to skip a revert) consult, so the two can never disagree about what a Grant allows.
    /// </summary>
    /// <param name="grants">The active Grants.</param>
    /// <param name="canonicalPath">The canonical path of the changed file.</param>
    /// <returns>The authorizing Grant, or <see langword="null"/>.</returns>
    internal static Grant? FindAuthorizing(IReadOnlyList<Grant> grants, CanonicalPath canonicalPath)
    {
        ArgumentNullException.ThrowIfNull(grants);
        return grants.FirstOrDefault(grant =>
            grant.Scope == GrantScope.All && grant.CoveredPaths.Any(matcher => matcher.Matches(canonicalPath)));
    }
}
