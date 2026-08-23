// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Matches any canonical path at or beneath a canonical directory. Used for the directory-anchored Sealed and
/// System rules (the store dirs and the guard's shipped-code trees).
/// </summary>
internal sealed class DirectoryPrefixMatcher : IPathMatcher
{
    private readonly string _canonicalDirectory;
    private readonly string _canonicalPrefix;

    private DirectoryPrefixMatcher(string canonicalDirectory, char directorySeparator)
    {
        _canonicalDirectory = canonicalDirectory;
        _canonicalPrefix = canonicalDirectory + directorySeparator;
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
    /// <param name="directorySeparator">The OS directory separator, owned by <c>IPlatformFileSystem</c> and threaded
    /// in from the composition (the raw <c>Path.DirectorySeparatorChar</c> is not input-deterministic, AG0020).</param>
    /// <returns>The matcher, as its interface.</returns>
    internal static IPathMatcher Create(IPathCanonicalizer canonicalizer, string rawDirectory, char directorySeparator)
    {
        ArgumentNullException.ThrowIfNull(canonicalizer);
        return new DirectoryPrefixMatcher(canonicalizer.Canonicalize(rawDirectory).Value, directorySeparator);
    }
}
