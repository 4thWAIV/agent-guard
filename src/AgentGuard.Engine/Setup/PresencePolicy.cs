// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the presence-gate policy value shared across all three OS: the one 60-second bounded wait the
/// approval gate races the presence check against (prompt-timeout-60s-fail-closed). The gate advances this against the
/// injected clock, so the timeout is testable with no real wait, and denies with
/// <see cref="AgentGuard.Abstractions.ApprovalReason.TimedOut"/> on expiry.
/// </summary>
internal static class PresencePolicy
{
    /// <summary>
    /// The shared prompt timeout — 60 seconds — after which the gate fails closed.
    /// </summary>
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
}
