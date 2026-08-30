// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Setup;

/// <summary>
/// The single asynchronous approval gate <c>install</c>, <c>init</c>, and <c>remove</c> call before they mutate
/// anything (<c>doctor</c> does not — it self-heals). It reaches the presence capability at
/// <c>services.Platform.Presence</c> and the clock at <c>services.Clock</c> through the root, resolves the reviewed
/// dialog text from the owned <see cref="PresenceDialogText"/> resource keyed by <see cref="SetupVerb"/>, and races the
/// presence check against a clock-driven <see cref="PresencePolicy.Timeout"/> — expiry denies with
/// <see cref="AgentGuard.Abstractions.ApprovalReason.TimedOut"/> (fail-closed). It is the ONE production caller of
/// <c>IPresenceCheck.Check</c> (AG0108), so the timeout and the reason-to-CLI-line mapping live in exactly one place.
/// </summary>
internal static class ApprovalGate
{
    /// <summary>
    /// Requests approval to perform a mutating action, proving a physically-present human through the platform's
    /// out-of-band presence channel and denying on any non-approved reason or on the 60-second timeout.
    /// </summary>
    /// <param name="services">The owned service container the gate reaches presence and the clock through.</param>
    /// <param name="action">The setup verb being requested (<see cref="SetupVerb.Install"/>/<see cref="SetupVerb.Init"/>/
    /// <see cref="SetupVerb.Remove"/>), used to key the owned dialog text and stamp the decision.</param>
    /// <returns>The approval decision, with its reason and the CLI detail populated.</returns>
    internal static async Task<ApprovalDecision> RequireApprovalAsync(ISystemServices services, string action)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(action);

        var request = new PresenceRequest(PresenceDialogText.For(action));

        // One CTS drives both sides of the race: whichever wins, the loser is cancelled — a completed check cancels the
        // still-pending timeout timer, and an expired timeout cancels the still-running presence boundary (fail-closed).
        using var cts = new CancellationTokenSource();
        Task<PresenceResult> check = services.Platform.Presence.Check(request, services, cts.Token);
        Task timeout = Task.Delay(PresencePolicy.Timeout, services.Clock, cts.Token);

        Task first = await Task.WhenAny(check, timeout).ConfigureAwait(false);
        await cts.CancelAsync().ConfigureAwait(false);

        if (first == check)
        {
            PresenceResult result = await check.ConfigureAwait(false);
            return new ApprovalDecision(action, result.Reason, result.Detail);
        }

        // The shared 60-second wait expired before the boundary answered: deny, fail-closed. The boundary is the only
        // producer of the other reasons; TimedOut is the gate's alone.
        return new ApprovalDecision(
            action,
            ApprovalReason.TimedOut,
            $"presence was not confirmed within {PresencePolicy.Timeout.TotalSeconds:0} seconds; failing closed");
    }
}
