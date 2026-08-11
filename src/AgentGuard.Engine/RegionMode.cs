// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine;

/// <summary>
/// How a declared region of a protected config file is judged after a call. A region not declared in a file's
/// region map is implicitly <see cref="Unprotected"/> — never checked, never touched.
/// </summary>
internal enum RegionMode
{
    /// <summary>
    /// Mode 1. The guard computes the correct value; the check is "is the region what it should be?", and the
    /// restore rewrites the canonical value. The guard's own edits pass by construction because their output is
    /// the canonical value.
    /// </summary>
    Canonical,

    /// <summary>
    /// Mode 2. Security-critical but user-set, so no canonical value exists; the check is "did the region change
    /// from the pre-call backup without a covering grant?", and the restore reverts the region to the backup.
    /// </summary>
    Backup,

    /// <summary>
    /// Mode 3. Everything not declared: never checked, never touched.
    /// </summary>
    Unprotected,
}
