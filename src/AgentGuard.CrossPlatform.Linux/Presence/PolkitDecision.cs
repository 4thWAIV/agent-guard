// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The pure, static mapper from a plain <see cref="PolkitResult"/> to the shared <see cref="ApprovalReason"/> — the
/// Linux presence decision, table-tested for every reason. The mapping is: authorized + no temp-auth-id →
/// <see cref="ApprovalReason.Approved"/>; authorized WITH a temp-auth-id / retains marker →
/// <see cref="ApprovalReason.Denied"/> (the non-fresh backstop, fresh-interaction-no-cached-yes); not-authorized +
/// challenge → <see cref="ApprovalReason.Unavailable"/>; <c>polkit.dismissed</c> → <see cref="ApprovalReason.Cancelled"/>;
/// not-authorized + no challenge → <see cref="ApprovalReason.Denied"/>; any fault → <see cref="ApprovalReason.Error"/>.
/// It never returns <see cref="ApprovalReason.TimedOut"/>, and never <see cref="ApprovalReason.Approved"/> from a fault.
/// </summary>
internal static class PolkitDecision
{
    /// <summary>
    /// The reply-detail key polkit sets when a retained (non-fresh) authorization satisfied the check — its presence
    /// means the "yes" was cached, not freshly challenged, so the decision rejects it (fresh-interaction-no-cached-yes).
    /// </summary>
    internal const string TemporaryAuthorizationIdKey = "polkit.temporary_authorization_id";

    /// <summary>The reply-detail key polkit sets when the human dismissed the authentication dialog.</summary>
    internal const string DismissedKey = "polkit.dismissed";

    /// <summary>
    /// Maps a plain polkit reply to the shared approval reason.
    /// </summary>
    /// <param name="result">The plain polkit reply from the port.</param>
    /// <returns>The mapped approval reason.</returns>
    internal static ApprovalReason Map(PolkitResult result)
    {
        // An authorized result is fresh only when polkit did NOT satisfy it from a retained grant: a
        // temporary_authorization_id in the reply marks a cached "yes", which this design rejects as non-fresh.
        if (result.IsAuthorized)
        {
            return result.Details.ContainsKey(TemporaryAuthorizationIdKey)
                ? ApprovalReason.Denied
                : ApprovalReason.Approved;
        }

        // The human closed the dialog without answering.
        if (result.Details.ContainsKey(DismissedKey))
        {
            return ApprovalReason.Cancelled;
        }

        // Not authorized, but polkit offered interaction (a challenge) that no agent could complete — no way to prove
        // presence on this host — versus a flat refusal.
        return result.IsChallenge ? ApprovalReason.Unavailable : ApprovalReason.Denied;
    }
}
