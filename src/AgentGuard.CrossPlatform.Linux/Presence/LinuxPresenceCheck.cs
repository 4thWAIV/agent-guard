// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The Linux <see cref="IPresenceCheck"/> and the Linux presence FLOW port: it builds the unix-process subject from
/// <c>/proc/self</c> through the owned <c>IFileReader</c> off <c>services</c> (no native call on Linux), calls
/// <see cref="IPolkitAuthority"/> exactly once per <see cref="Check"/> with <c>AllowUserInteraction</c> set, and maps the
/// plain <see cref="PolkitResult"/> to an <see cref="AgentGuard.Abstractions.ApprovalReason"/> through the pure
/// <see cref="PolkitDecision"/> — forcing a fresh interaction every call and rejecting any cached authorization. It owns
/// the cancellation orchestration (the layer that is fake-testable over the <see cref="IPolkitAuthority"/> seam): it
/// mints a per-call cancellation id from the owned <see cref="AgentGuard.Abstractions.Contracts.IRandomGenerator"/>,
/// wires the token to the port's <see cref="IPolkitAuthority.CancelCheckAuthorization"/> so an in-flight check is
/// actually cancelled, and maps that cancellation to <see cref="AgentGuard.Abstractions.ApprovalReason.Cancelled"/>. It
/// mints no timeout — the gate owns the one 60-second bound.
/// </summary>
internal sealed class LinuxPresenceCheck : IPresenceCheck
{
    // /proc/self/stat fields are 1-based; the first field parsed after the comm field (which ends at the LAST ')') is
    // field 3 (state). So a field's index in the tokens after that ')' is its 1-based number minus 3.
    private const int FirstFieldNumberAfterComm = 3;
    private const int StartTimeFieldNumber = 22;
    private const string ProcStatPath = "/proc/self/stat";
    private const string ProcStatusPath = "/proc/self/status";
    private const string RealUidLinePrefix = "Uid:";

    private readonly IPolkitAuthority _polkitAuthority;

    // Private constructor (AG0003): only Create() builds it, from the per-OS PlatformServices.CreatePresence().
    private LinuxPresenceCheck(IPolkitAuthority polkitAuthority) => _polkitAuthority = polkitAuthority;

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed presence boundary: any caught fault must become a deny (Error), never Approved.")]
    public async Task<PresenceResult> Check(PresenceRequest request, ISystemServices services, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(services);

        try
        {
            IFileReader reader = services.FileSystem.GetFileReader();
            PolkitSubject subject = BuildSubject(reader.ReadAllText(ProcStatPath), reader.ReadAllText(ProcStatusPath));

            // A fresh, per-call unique cancellation id from the owned randomness seam (AG0011): polkit writes it into the
            // check and rejects a reused id. Held only as a local and captured in the token registration below, so no
            // handle is cached in a field.
            string cancellationId = services.Random.NewGuid().ToString("N");

            // Honor the token IN FLIGHT: when it fires, message the port to cancel the pending polkit check (dismissing
            // its out-of-band prompt), mirroring the macOS port's ct.Register(invalidate). Wiring the caller's token is
            // not minting a timeout (AG0107) — the gate still owns the one 60-second bound.
            CancellationTokenRegistration cancellation =
                ct.Register(() => _polkitAuthority.CancelCheckAuthorization(cancellationId));
            try
            {
                PolkitResult result = await _polkitAuthority
                    .CheckAuthorizationAsync(subject, PolkitAction.Id, request.PromptText, cancellationId, ct)
                    .ConfigureAwait(false);

                ApprovalReason reason = PolkitDecision.Map(result);
                return new PresenceResult(
                    reason,
                    $"polkit CheckAuthorization: authorized={result.IsAuthorized}, challenge={result.IsChallenge} -> {reason}");
            }
            finally
            {
                // Dispose the registration first — it waits out any in-flight cancel send — before this method returns.
                await cancellation.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The token cancelled the in-flight check (the port sent CancelCheckAuthorization and polkit ended it): a
            // cancellation is Cancelled — never Approved and never Error.
            return new PresenceResult(ApprovalReason.Cancelled, "polkit CheckAuthorization cancelled -> Cancelled");
        }
        catch (Exception ex)
        {
            // A bus-less runner, a malformed /proc read, or any boundary fault fails closed as Error; a caught fault
            // never yields Approved (fresh-interaction-no-cached-yes / fail-closed).
            return new PresenceResult(ApprovalReason.Error, $"polkit CheckAuthorization faulted: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates the Linux presence check over its polkit port.
    /// </summary>
    /// <param name="polkitAuthority">The internal polkit port this check calls.</param>
    /// <returns>The presence check, as its interface.</returns>
    internal static IPresenceCheck Create(IPolkitAuthority polkitAuthority) =>
        new LinuxPresenceCheck(polkitAuthority);

    // Builds the classic unix-process subject triple from the two proc-self file contents, per presence-subject-triple.
    // The caller reads them through the owned IFileReader, so this helper takes plain text and needs no boundary service.
    private static PolkitSubject BuildSubject(string stat, string status) =>
        new(ParseProcessId(stat), ParseStartTime(stat), ParseRealUserId(status));

    // Field 1 (pid) is the token before the comm field's opening '(' — safe from the comm gotcha, which only affects
    // fields after the comm.
    private static int ParseProcessId(string stat)
    {
        int commStart = stat.IndexOf('(', StringComparison.Ordinal);
        return int.Parse(stat.AsSpan(0, commStart).Trim(), CultureInfo.InvariantCulture);
    }

    // Field 22 (start-time), read from AFTER the LAST ')': the comm field can itself contain spaces and ')' characters
    // (e.g. "(gu a)rd)"), so the numeric fields are located from the final ')' to survive that. Parsed as a 64-bit value
    // so it does not truncate past ~497 days of uptime and silently mismatch the process.
    private static ulong ParseStartTime(string stat)
    {
        int lastParen = stat.LastIndexOf(')');
        string[] fields = stat[(lastParen + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return ulong.Parse(fields[StartTimeFieldNumber - FirstFieldNumberAfterComm], CultureInfo.InvariantCulture);
    }

    // The REAL uid — the first column of the "Uid:" line of /proc/self/status (the line is "Uid: real effective saved
    // fs"), so a setuid caller is named to polkit by the invoking user, not the effective one.
    private static uint ParseRealUserId(string status)
    {
        string uidLine = status
            .Split('\n')
            .FirstOrDefault(line => line.StartsWith(RealUidLinePrefix, StringComparison.Ordinal))
            ?? throw new FormatException($"no '{RealUidLinePrefix}' line found in {ProcStatusPath}");

        string[] columns = uidLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return uint.Parse(columns[1], CultureInfo.InvariantCulture);
    }
}
