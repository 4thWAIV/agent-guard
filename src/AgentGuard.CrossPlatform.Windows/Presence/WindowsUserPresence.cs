// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The sole implementer of <see cref="IWindowsUserPresence"/> — the Windows presence FLOW port, now a pure DI
/// orchestrator injected with the two native-ops seams below it (deep-native-ops-seam, macos-one-windows-two-native-ops):
/// <see cref="IWindowsHelloNativeOps"/> for Windows Hello and <see cref="ICredentialPromptNativeOps"/> for the
/// secure-desktop credential prompt. It owns all the testable logic: it FIRST runs the non-interactive Hello
/// availability check and fails closed to <see cref="WindowsPresenceResult.NoMethod"/> when Hello is not available — the
/// fast-fail guard that keeps a headless runner from blocking on the interactive prompt — then, only when Hello is
/// available, tries Hello and falls back to the credential prompt (windows-hello-or-password). It wires the Hello
/// <c>TaskCompletionSource</c>/completion, registers the caller's cancellation token to ask a pending WinRT operation to
/// cancel (cooperative cancellation, mirroring the macOS <c>LAContext</c> invalidate), and branches on the
/// prompt/unpack/logon outcomes — mapping the raw results through the pure <see cref="MapHelloResult"/>,
/// <see cref="MapAsyncStatus"/>, and <see cref="MapCredentialStatus"/> functions, which it fakes in tests. It caches no
/// auth context in a field (AG0112) and mints no timeout of its own (AG0107) — cancellation comes only from the passed
/// token and the gate owns the one 60-second bound.
/// </summary>
internal sealed class WindowsUserPresence : IWindowsUserPresence
{
    // UserConsentVerificationResult (Windows.Security.Credentials.UI).
    private const int UserVerified = 0;
    private const int DeviceNotPresent = 1;
    private const int NotConfiguredForUser = 2;
    private const int DisabledByPolicy = 3;
    private const int DeviceBusy = 4;
    private const int RetriesExhausted = 5;
    private const int UserCanceled = 6;

    // AsyncStatus (Windows.Foundation).
    private const int AsyncStatusCompleted = 1;
    private const int AsyncStatusCanceled = 2;

    // UserConsentVerifierAvailability.Available (Windows.Security.Credentials.UI): the only availability value that
    // permits the interactive Hello prompt; every other value (DeviceNotPresent on a headless runner, NotConfiguredForUser,
    // DisabledByPolicy, DeviceBusy) is a fast fail to NoMethod without prompting.
    private const int UserConsentVerifierAvailable = 0;

    // Credential-prompt results (winerror.h).
    private const uint ErrorSuccess = 0;
    private const uint ErrorCancelled = 1223;

    private readonly IWindowsHelloNativeOps _hello;
    private readonly ICredentialPromptNativeOps _credential;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsUserPresence"/> class over its two native-ops seams.
    /// </summary>
    /// <param name="hello">The Windows Hello native-ops seam this orchestrator composes.</param>
    /// <param name="credential">The secure-desktop credential-prompt native-ops seam this orchestrator composes.</param>
    internal WindowsUserPresence(IWindowsHelloNativeOps hello, ICredentialPromptNativeOps credential) =>
        (_hello, _credential) = (hello, credential);

    /// <inheritdoc />
    public async Task<WindowsPresenceResult> VerifyAsync(string reason, CancellationToken ct)
    {
        // The port honors cancellation but mints no timeout of its own (AG0107); the gate owns the 60-second bound.
        ct.ThrowIfCancellationRequested();

        // Fast-fail availability guard (CheckAvailabilityAsync): on a headless runner it answers a non-Available code
        // promptly, so the port returns NoMethod WITHOUT ever issuing the blocking Hello verification request or the
        // secure-desktop credential prompt — the core no-hang guard. The credential prompt (an un-cancellable modal that
        // could hang a headless runner) is therefore unreachable here: this short-circuit runs before it.
        if (!await IsHelloAvailableAsync(ct).ConfigureAwait(false))
        {
            return WindowsPresenceResult.NoMethod;
        }

        // Hello is available: run the interactive verification, honoring the cancellation token. A null outcome (a
        // reported-unavailable code that contradicts the availability probe, or a bind failure) falls back to the
        // secure-desktop credential prompt (windows-hello-or-password) — a path the availability guard already made
        // unreachable on a headless runner.
        WindowsPresenceResult? hello = await TryVerifyWithHelloAsync(reason, ct).ConfigureAwait(false);
        return hello ?? VerifyWithCredentialPrompt(reason);
    }

    /// <summary>
    /// Maps a raw WinRT <c>UserConsentVerificationResult</c> to the plain native outcome — the extracted 7-way pure
    /// function the mapper table test exercises. A <see langword="null"/> result means Hello is unavailable/un-configured
    /// and the orchestrator falls back to the credential prompt.
    /// </summary>
    /// <param name="consentResult">The raw <c>UserConsentVerificationResult</c> code read from the async operation.</param>
    /// <returns>The plain outcome, or <see langword="null"/> to fall back to the credential prompt.</returns>
    internal static WindowsPresenceResult? MapHelloResult(int consentResult) => consentResult switch
    {
        UserVerified => WindowsPresenceResult.Verified,
        UserCanceled => WindowsPresenceResult.Cancelled,
        DeviceBusy or RetriesExhausted => WindowsPresenceResult.Failed,
        DeviceNotPresent or NotConfiguredForUser or DisabledByPolicy => null,
        _ => null,
    };

    /// <summary>
    /// Maps a raw WinRT <c>AsyncStatus</c> to the substitute <c>UserConsentVerificationResult</c> code to use when the
    /// operation did not complete — the extracted pure function the mapper table test exercises. A <see langword="null"/>
    /// result means the operation completed and the orchestrator reads the real result instead.
    /// </summary>
    /// <param name="asyncStatus">The raw <c>AsyncStatus</c> the completed handler reported.</param>
    /// <returns>The substitute result code, or <see langword="null"/> when the operation completed.</returns>
    internal static int? MapAsyncStatus(int asyncStatus) => asyncStatus switch
    {
        AsyncStatusCompleted => null,
        AsyncStatusCanceled => UserCanceled,
        _ => RetriesExhausted,
    };

    /// <summary>
    /// Maps the secure-desktop credential-prompt status to an early plain outcome — the extracted pure function the
    /// mapper table test exercises. A <see langword="null"/> result means the prompt succeeded and the orchestrator
    /// continues to unpack and validate the typed credential.
    /// </summary>
    /// <param name="promptStatus">The prompt's return status.</param>
    /// <param name="hasBuffer">Whether the prompt produced a non-empty authentication buffer.</param>
    /// <returns>The early outcome (cancel or no method), or <see langword="null"/> to continue to validation.</returns>
    internal static WindowsPresenceResult? MapCredentialStatus(uint promptStatus, bool hasBuffer)
    {
        if (promptStatus == ErrorCancelled)
        {
            return WindowsPresenceResult.Cancelled;
        }

        if (promptStatus != ErrorSuccess || !hasBuffer)
        {
            // No interactive secure desktop was available (a headless/service host), or the prompt could not show.
            return WindowsPresenceResult.NoMethod;
        }

        return null;
    }

    // The non-interactive availability probe (CheckAvailabilityAsync): whether Hello can verify the owner at all. On a
    // headless runner it answers a non-Available code promptly, so the orchestrator fails closed to NoMethod before any
    // blocking prompt. It shares the one WinRT-operation await helper with the interactive verification; an availability
    // check that comes back cancelled/errored (a non-completing async status maps to a non-Available substitute) is
    // likewise treated as not available.
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A Hello availability-probe binding failure of any kind (no WinRT, IInspectable marshalling unsupported on this runtime, activation failure) means Hello cannot be verified — fail closed to not-available; it is never a hard fault. Interim until WebAuthn (#53); suppression signed off by Tim.")]
    private async Task<bool> IsHelloAvailableAsync(CancellationToken ct)
    {
        try
        {
            int availability = await AwaitOperationAsync(
                _hello.BeginAvailabilityCheck, _hello.GetAvailabilityResult, ct).ConfigureAwait(false);
            return availability == UserConsentVerifierAvailable;
        }
        catch (Exception)
        {
            // Hello could not be probed on this host (no WinRT / IInspectable unsupported); fail closed to not-available.
            return false;
        }
    }

    // Tries Windows Hello through the native-ops seam. Returns a concrete outcome when Hello answered, or null when Hello
    // is unavailable / un-configured / could not bind, so the caller falls back to the credential prompt. Any binding
    // failure (missing WinRT, activation failure, a COM error) falls back — never faults the check.
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A Hello binding failure of any kind (version floor, no HWND, single-file activation) falls back to the credential prompt (windows-hello-or-password); it is never a hard fault.")]
    private async Task<WindowsPresenceResult?> TryVerifyWithHelloAsync(string reason, CancellationToken ct)
    {
        try
        {
            int consentResult = await RequestHelloVerificationAsync(reason, ct).ConfigureAwait(false);
            return MapHelloResult(consentResult);
        }
        catch (Exception)
        {
            // Hello could not bind on this host; fall back to the credential prompt.
            return null;
        }
    }

    // Issues one Hello verification through the native-ops seam and awaits it through the shared WinRT-operation helper,
    // closing over the reviewed reason. The helper owns the TaskCompletionSource, honors the cancellation token, and
    // reads the raw verification result once the operation completes.
    private Task<int> RequestHelloVerificationAsync(string reason, CancellationToken ct) =>
        AwaitOperationAsync(
            onCompleted => _hello.BeginVerification(reason, onCompleted), _hello.GetVerificationResult, ct);

    // The one WinRT-operation await both the availability probe and the interactive verification compose. It owns the
    // TaskCompletionSource the native completed handler forwards its raw async status to (AG0115 keeps this wiring in the
    // flow port, never the thin native-ops class), and registers the caller's cancellation token to ASK the pending
    // operation to cancel — WinRT cancellation is cooperative, so a cancel unblocks the await promptly with a cancelled
    // async status, mirroring the macOS LAContext invalidate. When the operation did not complete it substitutes a
    // consent code (MapAsyncStatus); otherwise it reads the operation's real result. The registration is disposed FIRST
    // in finally — so no cancel callback can touch the operation after it is released — then the operation is released.
    // The operation pointer is a per-call local captured in the closures (AG0112), never a field; the port mints no
    // timeout of its own (AG0107) — cancellation comes only from the passed token.
    private async Task<int> AwaitOperationAsync(
        Func<Action<int>, IntPtr> begin, Func<IntPtr, int> getResult, CancellationToken ct)
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        IntPtr operation = begin(status => completion.TrySetResult(status));
        CancellationTokenRegistration cancellation = ct.Register(() => _hello.CancelOperation(operation));
        try
        {
            int asyncStatus = await completion.Task.ConfigureAwait(false);
            int? substitute = MapAsyncStatus(asyncStatus);
            return substitute ?? getResult(operation);
        }
        finally
        {
            // Dispose the registration first — it waits out any in-flight cancel — so no callback can reach the operation
            // after it is released just below.
            await cancellation.DisposeAsync().ConfigureAwait(false);
            _hello.ReleaseOperation(operation);
        }
    }

    // The secure-desktop credential prompt (CREDUIWIN_SECURE_PROMPT): the user types the account password on the same
    // isolated surface as UAC/logon, which this user-mode caller can neither read nor inject; LogonUser validates it
    // against the current account. A cancel is Cancelled; a wrong password is Failed; no interactive surface / no
    // credential is NoMethod. The prompt buffer the native-ops layer produced is freed once branching is done.
    private WindowsPresenceResult VerifyWithCredentialPrompt(string reason)
    {
        CredentialPromptOutcome outcome = _credential.Prompt(reason);
        try
        {
            WindowsPresenceResult? early = MapCredentialStatus(outcome.Status, outcome.Buffer != IntPtr.Zero);
            return early ?? ValidateCredential(outcome);
        }
        finally
        {
            _credential.FreePromptBuffer(outcome.Buffer);
        }
    }

    // Unpacks the typed credential and validates it with LogonUser through the native-ops seam. The field buffers are
    // freed (the password zeroed) after use; a validated logon token is closed.
    private WindowsPresenceResult ValidateCredential(CredentialPromptOutcome outcome)
    {
        CredentialFields fields = _credential.Unpack(outcome.Buffer, outcome.BufferSize);
        try
        {
            if (!fields.Success)
            {
                return WindowsPresenceResult.Error;
            }

            if (_credential.Logon(fields.User, fields.Password, out IntPtr token))
            {
                _credential.CloseToken(token);
                return WindowsPresenceResult.Verified;
            }

            return WindowsPresenceResult.Failed;
        }
        finally
        {
            _credential.FreeFields(fields);
        }
    }
}
