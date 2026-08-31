// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform.MacOS;

/// <summary>
/// The macOS <see cref="IPresenceCheck"/>: it calls the internal <see cref="ILocalAuthentication"/> port once per
/// <see cref="Check"/>, gets a plain <see cref="LaResult"/>, and maps it to an <see cref="ApprovalReason"/> through the
/// pure <see cref="Map"/> function — the implementation itself never touches the native library. The mapping is
/// <see cref="LaResult.UserCancel"/> → <see cref="ApprovalReason.Cancelled"/>, <see cref="LaResult.AuthenticationFailed"/>
/// → <see cref="ApprovalReason.Denied"/>, <see cref="LaResult.NotAvailable"/>/<see cref="LaResult.NotEnrolled"/> →
/// <see cref="ApprovalReason.Unavailable"/>, <see cref="LaResult.Success"/> → <see cref="ApprovalReason.Approved"/>, and
/// anything else → <see cref="ApprovalReason.Error"/>. It mints no timeout — the gate owns the one 60-second bound.
/// </summary>
internal sealed class MacOsPresenceCheck : IPresenceCheck
{
    private readonly ILocalAuthentication _localAuthentication;

    // Private constructor (AG0003): only Create() builds it, from the per-OS PlatformServices.CreatePresence().
    private MacOsPresenceCheck(ILocalAuthentication localAuthentication) =>
        _localAuthentication = localAuthentication;

    /// <inheritdoc />
    public async Task<PresenceResult> Check(PresenceRequest request, ISystemServices services, CancellationToken ct)
    {
        System.ArgumentNullException.ThrowIfNull(request);

        // One fresh native evaluation per Check (fresh-interaction-no-cached-yes); the port owns the LAContext lifetime.
        LaResult result = await _localAuthentication.EvaluateAsync(request.PromptText, ct).ConfigureAwait(false);
        ApprovalReason reason = Map(result);
        return new PresenceResult(reason, $"LocalAuthentication evaluatePolicy -> {result} -> {reason}");
    }

    /// <summary>
    /// Creates the macOS presence check over its native port.
    /// </summary>
    /// <param name="localAuthentication">The internal LocalAuthentication port this check calls.</param>
    /// <returns>The presence check, as its interface.</returns>
    internal static IPresenceCheck Create(ILocalAuthentication localAuthentication) =>
        new MacOsPresenceCheck(localAuthentication);

    /// <summary>
    /// Maps a plain macOS native outcome to the shared <see cref="ApprovalReason"/> — the pure function the per-OS
    /// mapper table test exercises for every outcome.
    /// </summary>
    /// <param name="result">The plain native outcome from the port.</param>
    /// <returns>The mapped approval reason.</returns>
    internal static ApprovalReason Map(LaResult result) => result switch
    {
        LaResult.Success => ApprovalReason.Approved,
        LaResult.UserCancel => ApprovalReason.Cancelled,
        LaResult.AuthenticationFailed => ApprovalReason.Denied,
        LaResult.NotAvailable => ApprovalReason.Unavailable,
        LaResult.NotEnrolled => ApprovalReason.Unavailable,
        _ => ApprovalReason.Error,
    };
}
