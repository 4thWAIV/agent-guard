// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The result of the approval gate for a mutating command.
/// </summary>
/// <param name="IsApproved">Whether the action is approved to proceed.</param>
/// <param name="Action">The action that was requested.</param>
/// <param name="Detail">A human-readable detail about the decision.</param>
internal sealed record ApprovalDecision(bool IsApproved, string Action, string Detail);
