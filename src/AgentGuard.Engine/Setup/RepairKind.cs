// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The result of asking a condition to repair itself.
/// </summary>
internal enum RepairKind
{
    /// <summary>
    /// The condition was established or corrected.
    /// </summary>
    Repaired,

    /// <summary>
    /// The condition already held; nothing changed.
    /// </summary>
    NoChangeNeeded,

    /// <summary>
    /// The condition cannot be auto-repaired and must be reported instead of fabricated (for example a binary
    /// hash mismatch, which is never regenerated).
    /// </summary>
    NotRepairable,
}
