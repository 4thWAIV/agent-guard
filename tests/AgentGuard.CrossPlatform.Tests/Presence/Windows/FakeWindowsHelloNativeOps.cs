// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.CrossPlatform.Windows;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A stateful fake of the internal Windows HELLO native-ops seam (<see cref="IWindowsHelloNativeOps"/>) for the
/// orchestrator-flow test (<c>WindowsUserPresenceFlowTests</c>, contract-coverage-refactor acceptance #5). It stands in
/// for the live WinRT <c>UserConsentVerifier</c> interop so the REAL <see cref="WindowsUserPresence"/> orchestrator can
/// be driven through the availability guard and the Hello branches — verified, cancelled, busy, retries-exhausted, the
/// unavailable / cannot-bind paths that fall back to the credential prompt, the availability fast-fail, and a pending
/// verification the cancellation token cancels — with no Hello device. Modelling the OS, its
/// <see cref="BeginAvailabilityCheck"/> invokes the orchestrator's completed callback SYNCHRONOUSLY with a completed
/// status and reports the scripted availability code; its <see cref="BeginVerification"/> likewise completes
/// synchronously UNLESS the fake is in pending mode, in which case the verification stays pending until
/// <see cref="CancelOperation"/> completes it with a cancelled status. It is stateful, which is legal in
/// <c>AgentGuard.CrossPlatform.Tests</c> because AG0116/AG0106/AG0115 register only in the Windows and macOS PRODUCTION
/// assemblies; per-method call counts compose the shared <see cref="SingleCallRecorder{TArg,TResult}"/> (reuse-ledger).
/// </summary>
internal sealed class FakeWindowsHelloNativeOps : IWindowsHelloNativeOps
{
    // Non-zero sentinel async-operation handles the orchestrator threads through and releases.
    internal static readonly IntPtr OperationHandle = new(0x510);
    internal static readonly IntPtr AvailabilityOperationHandle = new(0x511);

    private readonly int _availability;
    private readonly int _asyncStatus;
    private readonly bool _pending;

    private readonly SingleCallRecorder<string, IntPtr> _begin;
    private readonly SingleCallRecorder<IntPtr, int> _getResult;
    private readonly SingleCallRecorder<IntPtr, bool> _cancel;
    private readonly SingleCallRecorder<IntPtr, bool> _release;

    // The verification completed callback captured while pending; CancelOperation completes it with a cancelled status.
    private Action<int>? _pendingVerification;
    private int _availabilityChecks;

    private FakeWindowsHelloNativeOps(int availability, int asyncStatus, int verificationResult, bool pending, Func<Exception>? bindFault)
    {
        _availability = availability;
        _asyncStatus = asyncStatus;
        _pending = pending;
        _begin = bindFault is null
            ? SingleCallRecorder<string, IntPtr>.Returning(OperationHandle)
            : SingleCallRecorder<string, IntPtr>.Faulting(bindFault);
        _getResult = SingleCallRecorder<IntPtr, int>.Returning(verificationResult);
        _cancel = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
        _release = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
    }

    /// <summary>Gets the number of availability checks begun.</summary>
    internal int AvailabilityChecks => _availabilityChecks;

    /// <summary>Gets the number of interactive Hello verifications begun.</summary>
    internal int VerificationsBegun => _begin.CallCount;

    /// <summary>Gets the number of times the port asked a pending operation to cancel.</summary>
    internal int CancelsRequested => _cancel.CallCount;

    /// <summary>Gets the number of async-operation releases.</summary>
    internal int OperationsReleased => _release.CallCount;

    /// <summary>Gets the prompt passed to the most recent verification, or <see langword="null"/> if none.</summary>
    internal string? LastReason => _begin.LastArg;

    /// <inheritdoc />
    public IntPtr BeginAvailabilityCheck(Action<int> onCompleted)
    {
        ArgumentNullException.ThrowIfNull(onCompleted);
        _availabilityChecks++;
        onCompleted(AsyncStatusCode.Completed);
        return AvailabilityOperationHandle;
    }

    /// <inheritdoc />
    public int GetAvailabilityResult(IntPtr asyncOperation) => _availability;

    /// <inheritdoc />
    public IntPtr BeginVerification(string reason, Action<int> onCompleted)
    {
        ArgumentNullException.ThrowIfNull(onCompleted);
        IntPtr operation = _begin.Record(reason);
        if (_pending)
        {
            // Stay pending: the verification only completes when CancelOperation fires (modelling a token cancel).
            _pendingVerification = onCompleted;
        }
        else
        {
            onCompleted(_asyncStatus);
        }

        return operation;
    }

    /// <inheritdoc />
    public int GetVerificationResult(IntPtr asyncOperation) => _getResult.Record(asyncOperation);

    /// <inheritdoc />
    public void CancelOperation(IntPtr asyncOperation)
    {
        _cancel.Record(asyncOperation);

        // A cooperative cancel unblocks a pending verification with a cancelled async status, as WinRT would.
        _pendingVerification?.Invoke(AsyncStatusCode.Canceled);
    }

    /// <inheritdoc />
    public void ReleaseOperation(IntPtr asyncOperation) => _release.Record(asyncOperation);

    /// <summary>Hello is available and answers: the request completes with <paramref name="asyncStatus"/> and yields <paramref name="verificationResult"/>.</summary>
    /// <param name="asyncStatus">The raw WinRT <c>AsyncStatus</c> the completed handler reports.</param>
    /// <param name="verificationResult">The raw <c>UserConsentVerificationResult</c> code <c>GetResults</c> returns.</param>
    /// <returns>The configured fake.</returns>
    internal static FakeWindowsHelloNativeOps Answers(int asyncStatus, int verificationResult) =>
        new(AvailabilityCode.Available, asyncStatus, verificationResult, pending: false, bindFault: null);

    /// <summary>Hello cannot bind on this host: <see cref="BeginVerification"/> throws (availability reports Available), so the orchestrator falls back.</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeWindowsHelloNativeOps CannotBind() =>
        new(AvailabilityCode.Available, asyncStatus: 0, verificationResult: 0, pending: false, bindFault: () => new InvalidOperationException("Hello cannot bind"));

    /// <summary>Hello is not available (the availability guard reports DeviceNotPresent, as a headless runner would): the fast-fail path that never issues a prompt.</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeWindowsHelloNativeOps Unavailable() =>
        new(AvailabilityCode.DeviceNotPresent, AsyncStatusCode.Completed, HelloResultCode.Verified, pending: false, bindFault: null);

    /// <summary>Hello is available and the verification stays PENDING until the cancellation token cancels it (CancelOperation completes it with a cancelled status).</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeWindowsHelloNativeOps PendingUntilCancelled() =>
        new(AvailabilityCode.Available, AsyncStatusCode.Completed, HelloResultCode.Verified, pending: true, bindFault: null);
}
