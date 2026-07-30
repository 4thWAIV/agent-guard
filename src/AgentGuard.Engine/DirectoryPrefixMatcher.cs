// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Matches any canonical path at or beneath a canonical directory. Used for the directory-anchored Sealed and
/// System rules (the store dirs and the guard's shipped-code trees).
/// </summary>
internal sealed class DirectoryPrefixMatcher : IPathMatcher
{
    private readonly string _canonicalDirectory;
    private readonly string _canonicalPrefix;

    private DirectoryPrefixMatcher(string canonicalDirectory)
    {
        _canonicalDirectory = canonicalDirectory;
        _canonicalPrefix = canonicalDirectory + Path.DirectorySeparatorChar;
    }

    /// <inheritdoc />
    public bool Matches(CanonicalPath canonicalPath) =>
        string.Equals(canonicalPath.Value, _canonicalDirectory, StringComparison.Ordinal)
        || canonicalPath.Value.StartsWith(_canonicalPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Creates a matcher for one raw directory, canonicalizing it through the given canonicalizer.
    /// </summary>
    /// <param name="canonicalizer">The canonicalizer used to resolve the directory.</param>
    /// <param name="rawDirectory">The raw directory whose subtree is matched.</param>
    /// <returns>The matcher, as its interface.</returns>
    internal static IPathMatcher Create(IPathCanonicalizer canonicalizer, string rawDirectory)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        return new DirectoryPrefixMatcher(canonicalizer.Canonicalize(rawDirectory).Value);
    }
}
