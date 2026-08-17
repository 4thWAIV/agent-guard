// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Matches a single canonical path exactly. The pattern is canonicalized once at creation, so a symlink to the
/// pattern reduces to the same canonical form and is matched.
/// </summary>
internal sealed class ExactPathMatcher : IPathMatcher
{
    private readonly string _canonical;

    private ExactPathMatcher(string canonical) => _canonical = canonical;

    /// <inheritdoc />
    public bool Matches(CanonicalPath canonicalPath) =>
        string.Equals(canonicalPath.Value, _canonical, StringComparison.Ordinal);

    /// <summary>
    /// Creates a matcher for one raw path, canonicalizing it through the given canonicalizer.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to resolve the pattern.</param>
    /// <param name="rawPath">The raw path to match exactly.</param>
    /// <returns>The matcher, as its interface.</returns>
    internal static IPathMatcher Create(IPathCanonicalizer canonicalizer, string rawPath)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        return new ExactPathMatcher(canonicalizer.Canonicalize(rawPath).Value);
    }
}
