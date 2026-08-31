// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AgentGuard.CrossPlatform.MacOS;

/// <summary>
/// The sole implementer of <see cref="ILocalAuthentication"/> — the macOS presence FLOW port, now a pure DI orchestrator
/// injected with the <see cref="IObjCRuntime"/> native-ops seam below it (deep-native-ops-seam). It owns all the testable
/// logic: it creates a FRESH <c>LAContext</c> per <see cref="EvaluateAsync"/> (fresh-interaction-no-cached-yes), runs the
/// non-interactive capability probe, maps a probe/reply error code through the pure <see cref="MapErrorCode"/> function,
/// wires the interactive evaluation's <c>TaskCompletionSource</c>/<c>GCHandle</c>/cancel-invalidate, and releases every
/// per-call handle — composing the raw runtime calls of <see cref="IObjCRuntime"/>, which it fakes in tests. It holds no
/// native handle in a field (AG0112) and mints no timeout of its own (AG0107) — the gate owns the one 60-second bound.
/// </summary>
internal sealed class LocalAuthentication : ILocalAuthentication
{
    // LAPolicy.DeviceOwnerAuthentication = 2 (Touch ID or the account password) — macos-biometric-or-password.
    private const int DeviceOwnerAuthentication = 2;

    // The LAError codes the mapper translates (LocalAuthentication/LAError.h). Anything else is Unknown -> Error.
    private const long LaErrorAuthenticationFailed = -1;
    private const long LaErrorUserCancel = -2;
    private const long LaErrorPasscodeNotSet = -5;
    private const long LaErrorBiometryNotAvailable = -6;
    private const long LaErrorBiometryNotEnrolled = -7;

    private readonly IObjCRuntime _objc;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalAuthentication"/> class over its native-ops seam.
    /// </summary>
    /// <param name="objc">The macOS Objective-C runtime native-ops seam this orchestrator composes.</param>
    internal LocalAuthentication(IObjCRuntime objc) => _objc = objc;

    /// <inheritdoc />
    public Task<LaResult> EvaluateAsync(string reason, CancellationToken ct)
    {
        // The port honors cancellation but mints no timeout of its own (AG0107); the gate owns the 60-second bound.
        ct.ThrowIfCancellationRequested();

        IntPtr context = _objc.CreateContext();

        // The non-interactive probe: if the device cannot authenticate its owner, map the reason and never prompt.
        if (!_objc.CanEvaluate(context, DeviceOwnerAuthentication, out IntPtr probeError))
        {
            LaResult unavailable = MapErrorCode(_objc.GetErrorCode(probeError));
            ReleaseHandle(context);
            return Task.FromResult(unavailable);
        }

        return EvaluateInteractiveAsync(context, reason, ct);
    }

    /// <summary>
    /// Maps a raw <c>LAError</c> code (from the non-interactive probe or the interactive reply) to the plain native
    /// outcome — the extracted pure function the per-OS mapper table test exercises for every code.
    /// </summary>
    /// <param name="code">The <c>NSError</c> <c>-code</c> read from the native reply.</param>
    /// <returns>The plain native outcome the flow port hands up to the check.</returns>
    internal static LaResult MapErrorCode(long code) => code switch
    {
        LaErrorAuthenticationFailed => LaResult.AuthenticationFailed,
        LaErrorUserCancel => LaResult.UserCancel,
        LaErrorPasscodeNotSet => LaResult.NotEnrolled,
        LaErrorBiometryNotAvailable => LaResult.NotAvailable,
        LaErrorBiometryNotEnrolled => LaResult.NotEnrolled,
        _ => LaResult.Unknown,
    };

    // The interactive gate: shows the OS dialog through evaluatePolicy and completes when the block reply fires. The
    // fresh LAContext, the reason string, the GCHandle, and the block memory are all released once the reply arrives.
    // The port mints no timeout of its own (AG0107); it only HONORS the caller's cancellation token — when the gate's
    // 60-second bound (or any caller) cancels, invalidating the context cancels the pending evaluation and dismisses the
    // dialog, and LA fires the reply block with a cancel error, which maps to a non-approved reason. The reply handler
    // captured in the GCHandle does the mapping and completes the TaskCompletionSource; the native-ops layer's callback
    // only recovers and invokes it.
    private async Task<LaResult> EvaluateInteractiveAsync(IntPtr context, string reason, CancellationToken ct)
    {
        IntPtr nsReason = _objc.CreateReasonString(reason);
        var completion = new TaskCompletionSource<LaResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        // The orchestrator owns the reply handler: on a success reply it completes Success; otherwise it reads the
        // reply's error code through the native-ops seam and maps it through the same MapErrorCode the probe uses.
        void Reply(byte success, IntPtr error) =>
            completion.TrySetResult(success != 0 ? LaResult.Success : MapErrorCode(_objc.GetErrorCode(error)));

        GCHandle replyHandle = GCHandle.Alloc((Action<byte, IntPtr>)Reply);
        IntPtr replyBlock = _objc.BuildReplyBlock(GCHandle.ToIntPtr(replyHandle));
        CancellationTokenRegistration cancellation = ct.Register(() => _objc.Invalidate(context));
        try
        {
            // evaluatePolicy copies the block synchronously (Block_copy) before returning, then invokes the copy when
            // the user answers OR when invalidate cancels it; the reply completes the TaskCompletionSource on whatever
            // thread the OS calls back on.
            _objc.Evaluate(context, DeviceOwnerAuthentication, nsReason, replyBlock);
            return await completion.Task.ConfigureAwait(false);
        }
        finally
        {
            // Dispose the registration first — it waits out any in-flight invalidate — so no callback can message the
            // context after it is released just below.
            await cancellation.DisposeAsync().ConfigureAwait(false);
            _objc.FreeReplyBlock(replyBlock);
            replyHandle.Free();
            ReleaseHandle(nsReason);
            ReleaseHandle(context);
        }
    }

    // The orchestrator guards the non-zero pointer before releasing it through the native-ops seam (the raw Release is
    // an unconditional [instance release]; the guard is the flow port's, not the thin native-ops class's).
    private void ReleaseHandle(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            _objc.Release(handle);
        }
    }
}
