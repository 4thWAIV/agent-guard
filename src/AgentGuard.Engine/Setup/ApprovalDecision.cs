// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;

namespace AgentGuard.Setup;

/// <summary>
/// The result of the approval gate for a mutating command: the action requested, the presence reason it resolved to,
/// and the human-readable detail the CLI prints on its own line. <see cref="IsApproved"/> is derived from the reason —
/// only <see cref="ApprovalReason.Approved"/> lets the command proceed — so approval is never carried as a separate,
/// desyncable flag. It stays in the Engine (moving it to Abstractions would make the presence interface return an
/// Engine type, a backward dependency).
/// </summary>
/// <param name="Action">The action that was requested (the setup verb).</param>
/// <param name="Reason">The presence reason the gate resolved to.</param>
/// <param name="Detail">A human-readable detail about the decision, printed on its own CLI line.</param>
internal sealed record ApprovalDecision(string Action, ApprovalReason Reason, string Detail)
{
    /// <summary>
    /// Gets a value indicating whether the action is approved to proceed — true only when
    /// <see cref="Reason"/> is <see cref="ApprovalReason.Approved"/>.
    /// </summary>
    internal bool IsApproved => Reason == ApprovalReason.Approved;
}
