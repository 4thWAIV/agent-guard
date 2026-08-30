// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The plain result a per-OS presence boundary returns: the mapped <see cref="ApprovalReason"/> and a human-readable
/// detail for the CLI line. A boundary never returns <see cref="ApprovalReason.TimedOut"/> — that reason is the gate's.
/// </summary>
/// <param name="Reason">The mapped presence outcome.</param>
/// <param name="Detail">A human-readable detail describing the outcome, surfaced on its own CLI line.</param>
public sealed record PresenceResult(ApprovalReason Reason, string Detail);
