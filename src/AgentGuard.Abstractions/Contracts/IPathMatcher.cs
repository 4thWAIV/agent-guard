// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// Decides whether a write target is governed by a Rule. Different implementations carry different match
/// strategies — exact canonical path, distinctive segment, or glob.
/// </summary>
public interface IPathMatcher
{
    /// <summary>
    /// Determines whether the given path is a target this matcher governs.
    /// </summary>
    /// <param name="canonicalPath">The canonical path of the write target, produced by the canonicalizer.</param>
    /// <returns><see langword="true"/> when the path matches; otherwise <see langword="false"/>.</returns>
    bool Matches(CanonicalPath canonicalPath);
}
