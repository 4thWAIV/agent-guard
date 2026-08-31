// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The outcome of a presence check, shared across all three OS. The approval gate derives its decision and the CLI
/// line from this reason; the gate's <c>ApprovalDecision.IsApproved</c> is true only for <see cref="Approved"/>. A
/// per-OS boundary never emits <see cref="TimedOut"/> — the gate synthesizes it when the shared 60-second wait expires.
/// </summary>
public enum ApprovalReason
{
    /// <summary>
    /// A live human confirmed presence; the action may proceed.
    /// </summary>
    Approved,

    /// <summary>
    /// The human was present but the challenge failed (a wrong password, a failed biometric, a not-authorized result).
    /// </summary>
    Denied,

    /// <summary>
    /// No presence method is available to prompt with (no enrolled biometric or credential, no session agent).
    /// </summary>
    Unavailable,

    /// <summary>
    /// The human dismissed or cancelled the prompt.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The shared 60-second wait expired before a result arrived. Gate-synthesized; never emitted by a boundary.
    /// </summary>
    TimedOut,

    /// <summary>
    /// The check faulted (a boundary error the other reasons do not describe).
    /// </summary>
    Error,
}
