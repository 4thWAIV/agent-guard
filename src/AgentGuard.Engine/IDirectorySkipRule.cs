// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine;

/// <summary>
/// Decides whether the scanner must not descend into a directory. The skip list is part of the Sealed/System
/// floor — build-output locations and the Sealed stores — so the AI cannot edit it to hide a file. It is not a
/// contract seam; it is Engine-internal wiring the composition assembles and the scanner consults.
/// </summary>
internal interface IDirectorySkipRule
{
    /// <summary>
    /// Gets a stable identity for this rule, so the ruleset fingerprint can be derived from the assembled skip
    /// rules rather than a hand-maintained literal that could silently drift from them.
    /// </summary>
    string Identity { get; }

    /// <summary>
    /// Determines whether the walk must skip the given directory.
    /// </summary>
    /// <param name="directoryFullPath">The absolute, canonical path of the directory being considered.</param>
    /// <returns><see langword="true"/> when the directory must not be descended into.</returns>
    bool ShouldSkip(string directoryFullPath);
}
