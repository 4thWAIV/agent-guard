// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The union of all active Rules' matchers that the Engine matches a write against. Assembled from the four
/// Rule origins (Sealed and System as the floor; Provider and Project as the configurable layer).
/// </summary>
public interface IProtectedSet
{
    /// <summary>
    /// Finds the Rules whose matcher governs the given path. The caller inspects the matched Rules' origins to
    /// decide how to treat the write.
    /// </summary>
    /// <param name="canonicalPath">The canonical path of the write target, produced by the canonicalizer.</param>
    /// <returns>The matching Rules, or an empty list when the path is not protected.</returns>
    IReadOnlyList<Rule> Match(CanonicalPath canonicalPath);
}
