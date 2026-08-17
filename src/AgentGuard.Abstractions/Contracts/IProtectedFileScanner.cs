// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// Lists the configurable protected files currently present on disk, which is what Capture snapshots and what
/// Post re-reads to detect drift. It walks the working tree under the environment's project root, does not
/// descend into the Sealed build-output locations, and matches each remaining candidate against
/// <see cref="IProtectedSet"/>. It is the whole reason Post can revert drift caused by a shell command whose
/// target was never parsed: it captures the entire protected set, not just a known edit target.
/// </summary>
public interface IProtectedFileScanner
{
    /// <summary>
    /// Returns every configurable protected file currently present on disk under the project root. The scan
    /// must be complete or fail: an inaccessible directory is surfaced as an error, never skipped, so an
    /// incomplete walk denies the call rather than hiding a file. Because a completed scan is the authoritative
    /// record of what exists, a path absent from it did not exist, which is what lets Post tell a created file
    /// from an untouched one.
    /// </summary>
    /// <param name="environment">The call's environment, whose project root anchors the walk.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the canonical paths of the protected files present on disk.</returns>
    Task<IReadOnlyList<CanonicalPath>> ScanAsync(CallEnvironment environment, CancellationToken cancellationToken);
}
