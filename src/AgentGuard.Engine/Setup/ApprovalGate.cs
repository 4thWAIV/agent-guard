// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The single approval gate <c>install</c>, <c>init</c>, and <c>remove</c> call before they mutate anything. In
/// this build the gate allows through — there is no OS presence check yet, so this build ships those three
/// commands ungated. The real Touch ID check is a later build and drops in behind this call with no change to the
/// callers. <c>doctor</c> does not call it (agent self-heal).
/// </summary>
internal static class ApprovalGate
{
    /// <summary>
    /// Requests approval to perform a mutating action. In this build it always approves.
    /// </summary>
    /// <param name="action">The action being requested (for later builds and diagnostics).</param>
    /// <returns>An approved decision.</returns>
    internal static ApprovalDecision RequireApproval(string action) => new(
        IsApproved: true,
        Action: action,
        Detail: "approval placeholder: allowed — this build ships install/init/remove ungated (the OS presence check is a later build)");
}
