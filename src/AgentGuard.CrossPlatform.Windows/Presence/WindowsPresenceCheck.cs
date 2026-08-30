// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The Windows <see cref="IPresenceCheck"/>: it calls the internal <see cref="IWindowsUserPresence"/> port once per
/// <see cref="Check"/>, gets a plain <see cref="WindowsPresenceResult"/>, and maps it to an <see cref="ApprovalReason"/>
/// through the pure <see cref="Map"/> function — the implementation itself never touches the native library. The
/// mapping is <see cref="WindowsPresenceResult.Verified"/> → <see cref="ApprovalReason.Approved"/>,
/// <see cref="WindowsPresenceResult.Cancelled"/> → <see cref="ApprovalReason.Cancelled"/>,
/// <see cref="WindowsPresenceResult.NoMethod"/> → <see cref="ApprovalReason.Unavailable"/>,
/// <see cref="WindowsPresenceResult.Failed"/> → <see cref="ApprovalReason.Denied"/>, and anything else →
/// <see cref="ApprovalReason.Error"/>. It mints no timeout — the gate owns the one 60-second bound.
/// </summary>
internal sealed class WindowsPresenceCheck : IPresenceCheck
{
    private readonly IWindowsUserPresence _userPresence;

    // Private constructor (AG0003): only Create() builds it, from the per-OS PlatformServices.Create().
    private WindowsPresenceCheck(IWindowsUserPresence userPresence) => _userPresence = userPresence;

    /// <inheritdoc />
    public async Task<PresenceResult> Check(PresenceRequest request, ISystemServices services, CancellationToken ct)
    {
        System.ArgumentNullException.ThrowIfNull(request);

        // One fresh verification per Check (fresh-interaction-no-cached-yes); the port owns the Hello-request lifetime.
        WindowsPresenceResult result = await _userPresence.VerifyAsync(request.PromptText, ct).ConfigureAwait(false);
        ApprovalReason reason = Map(result);
        return new PresenceResult(reason, $"Windows user presence -> {result} -> {reason}");
    }

    /// <summary>
    /// Creates the Windows presence check over its native port.
    /// </summary>
    /// <param name="userPresence">The internal Windows user-presence port this check calls.</param>
    /// <returns>The presence check, as its interface.</returns>
    internal static IPresenceCheck Create(IWindowsUserPresence userPresence) =>
        new WindowsPresenceCheck(userPresence);

    /// <summary>
    /// Maps a plain Windows native outcome to the shared <see cref="ApprovalReason"/> — the pure function the per-OS
    /// mapper table test exercises for every outcome.
    /// </summary>
    /// <param name="result">The plain native outcome from the port.</param>
    /// <returns>The mapped approval reason.</returns>
    internal static ApprovalReason Map(WindowsPresenceResult result) => result switch
    {
        WindowsPresenceResult.Verified => ApprovalReason.Approved,
        WindowsPresenceResult.Cancelled => ApprovalReason.Cancelled,
        WindowsPresenceResult.NoMethod => ApprovalReason.Unavailable,
        WindowsPresenceResult.Failed => ApprovalReason.Denied,
        _ => ApprovalReason.Error,
    };
}
