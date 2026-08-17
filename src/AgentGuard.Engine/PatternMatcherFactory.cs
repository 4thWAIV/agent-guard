// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Builds a path matcher from a configured pattern, shared by the Project rule source and the grant store so
/// both speak one pattern vocabulary: a <c>*.ext</c> suffix, a bare file name, or a repo-relative or absolute
/// path resolved through the canonicalizer.
/// </summary>
internal static class PatternMatcherFactory
{
    /// <summary>
    /// Creates a matcher for one pattern.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to resolve path patterns.</param>
    /// <param name="projectRoot">The absolute project root a relative pattern resolves against.</param>
    /// <param name="pattern">The pattern to match.</param>
    /// <returns>The matcher, as its interface.</returns>
    internal static IPathMatcher Create(IPathCanonicalizer canonicalizer, string projectRoot, string pattern)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            return FileExtensionMatcher.Create(pattern[1..]);
        }

        if (!pattern.Contains('/', StringComparison.Ordinal)
            && !pattern.Contains('\\', StringComparison.Ordinal)
            && !pattern.Contains('*', StringComparison.Ordinal))
        {
            return FileNameMatcher.Create(pattern);
        }

        string resolved = System.IO.Path.IsPathRooted(pattern)
            ? pattern
            : CoreSystemPaths.Absolute(projectRoot, pattern);
        return ExactPathMatcher.Create(canonicalizer, resolved);
    }
}
