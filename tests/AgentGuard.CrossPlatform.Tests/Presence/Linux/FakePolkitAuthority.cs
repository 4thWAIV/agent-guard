// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.CrossPlatform.Linux;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A fake of the internal Linux polkit port (<see cref="IPolkitAuthority"/>) for the <c>LinuxPresenceCheck</c> unit
/// tests: it records how many times <see cref="CheckAuthorizationAsync"/> was called and the subject / action id /
/// message / cancellation id it was given, and returns a configured <see cref="PolkitResult"/> — or throws, to model a
/// D-Bus fault (a bus-less runner) so the fault-to-<c>Error</c> path can be exercised. It also stands in for the port's
/// in-flight cancel seam: in <see cref="PendingUntilCancelled"/> mode <see cref="CheckAuthorizationAsync"/> stays
/// pending until <see cref="CancelCheckAuthorization"/> is called, then surfaces the cancellation as an
/// <see cref="OperationCanceledException"/> — exactly as the real <c>TmdsPolkitAuthority</c> does when polkit ends the
/// cancelled check — so the flow port's cancel-registration is proven without a live D-Bus (mirrors
/// <c>FakeObjCRuntime.CancelledOnInvalidate</c>). It touches no D-Bus. It is the single source implementer of the port
/// in this test compilation (AG0114 permits one). The call-count/last-argument capture and the fault behavior are not
/// hand-built here — it composes the shared <see cref="SingleCallRecorder{TArg,TResult}"/>.
/// </summary>
internal sealed class FakePolkitAuthority : IPolkitAuthority
{
    private readonly SingleCallRecorder<PolkitCall, PolkitResult> _recorder;

    // Non-null only in PendingUntilCancelled mode: the completion CheckAuthorizationAsync hands back and that
    // CancelCheckAuthorization ends. Null in the immediate Returning/Faulting modes, where the recorder answers directly.
    private readonly TaskCompletionSource<PolkitResult>? _pending;

    private FakePolkitAuthority(SingleCallRecorder<PolkitCall, PolkitResult> recorder, bool pendingUntilCancelled)
    {
        _recorder = recorder;
        _pending = pendingUntilCancelled
            ? new TaskCompletionSource<PolkitResult>(TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
    }

    internal int CallCount => _recorder.CallCount;

    internal PolkitSubject? LastSubject => _recorder.LastArg?.Subject;

    internal string? LastActionId => _recorder.LastArg?.ActionId;

    internal string? LastMessage => _recorder.LastArg?.Message;

    /// <summary>Gets the cancellation id the port received on the CheckAuthorization call, or null if none.</summary>
    internal string? SentCancellationId => _recorder.LastArg?.CancellationId;

    /// <summary>Gets the id passed to <see cref="CancelCheckAuthorization"/>, or null if it was never called.</summary>
    internal string? CancelledId { get; private set; }

    /// <summary>Gets the number of <see cref="CancelCheckAuthorization"/> calls.</summary>
    internal int CancelCount { get; private set; }

    /// <inheritdoc />
    public Task<PolkitResult> CheckAuthorizationAsync(
        PolkitSubject subject, string actionId, string message, string cancellationId, CancellationToken ct)
    {
        PolkitResult recorded = _recorder.Record(new PolkitCall(subject, actionId, message, cancellationId));
        return _pending is null ? Task.FromResult(recorded) : _pending.Task;
    }

    /// <inheritdoc />
    public void CancelCheckAuthorization(string cancellationId)
    {
        CancelCount++;
        CancelledId = cancellationId;

        // Model polkit ending the pending check: the in-flight CheckAuthorizationAsync surfaces the cancellation as an
        // OperationCanceledException, exactly as the real TmdsPolkitAuthority does when polkit answers the cancelled
        // check with an error. A no-op in the immediate modes, where no call is left pending.
        _pending?.TrySetException(new OperationCanceledException());
    }

    /// <summary>Creates a fake that returns the given polkit reply.</summary>
    /// <param name="result">The reply every call returns.</param>
    /// <returns>The fake authority.</returns>
    internal static FakePolkitAuthority Returning(PolkitResult result) =>
        new(SingleCallRecorder<PolkitCall, PolkitResult>.Returning(result), pendingUntilCancelled: false);

    /// <summary>Creates a fake that throws on every call, modelling a D-Bus fault.</summary>
    /// <returns>The faulting fake authority.</returns>
    internal static FakePolkitAuthority Faulting() =>
        new(
            SingleCallRecorder<PolkitCall, PolkitResult>.Faulting(
                () => new InvalidOperationException("simulated D-Bus fault (no system bus)")),
            pendingUntilCancelled: false);

    /// <summary>
    /// Creates a fake whose <see cref="CheckAuthorizationAsync"/> stays pending until
    /// <see cref="CancelCheckAuthorization"/> is called, then surfaces the cancellation as an
    /// <see cref="OperationCanceledException"/> — modelling a still-pending polkit check the token cancels in flight.
    /// </summary>
    /// <returns>The pending-until-cancelled fake authority.</returns>
    internal static FakePolkitAuthority PendingUntilCancelled() =>
        new(
            SingleCallRecorder<PolkitCall, PolkitResult>.Returning(
                new PolkitResult(IsAuthorized: false, IsChallenge: false, PolkitTestReplies.NoDetails())),
            pendingUntilCancelled: true);
}
