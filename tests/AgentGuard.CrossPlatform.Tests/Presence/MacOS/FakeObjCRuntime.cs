// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AgentGuard.CrossPlatform.MacOS;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A stateful fake of the internal macOS NATIVE-OPS seam (<see cref="IObjCRuntime"/>) for the orchestrator-flow test
/// (<c>LocalAuthenticationFlowTests</c>, contract-coverage-refactor acceptance #5, "the orchestrators' flow ... is
/// unit-tested by a fake native-ops across all branches"). It stands in for the live Objective-C runtime so the REAL
/// <see cref="LocalAuthentication"/> orchestrator — the flow port under test — can be driven through every branch
/// (probe-fail per error code with NO interactive call, probe-pass then interactive success/fail, and cancellation)
/// with no dialog and no framework binding. It is stateful (per-call handle bookkeeping, the captured completion,
/// per-method call counts) — legal in <c>AgentGuard.CrossPlatform.Tests</c> because AG0116/AG0106/AG0115 register only
/// in the macOS and Windows PRODUCTION assemblies. Per-method call counts compose the shared
/// <see cref="SingleCallRecorder{TArg,TResult}"/> (reuse-ledger: per-OS native-ops fake with per-method call counts);
/// the release SET is tracked separately because a fresh interaction releases BOTH the context and the reason string,
/// so a last-argument recorder cannot answer "was each handle released."
///
/// The interactive reply. The frozen seam hands the block a raw completion handle (<c>BuildReplyBlock(IntPtr
/// capturedCompletion)</c>) rather than a managed callback, and AG0115 forbids the thin native-ops layer from touching
/// a <c>TaskCompletionSource</c> — so the orchestrator captures its reply handler, an
/// <see cref="Action{T1,T2}"/> of <c>(byte success, IntPtr error)</c>, in the <c>GCHandle</c> the block carries, and the
/// native reply callback merely recovers and invokes it (no branch, no mapping — those are the orchestrator's). This
/// fake models the OS by recovering that same handler from the captured <c>GCHandle</c> and invoking it with the
/// scripted <c>(success, error)</c> reply. On a failure reply it hands back a non-zero error pointer whose code
/// <see cref="GetErrorCode"/> returns, so the orchestrator maps the reply through the SAME
/// <see cref="IObjCRuntime.GetErrorCode"/> + <c>MapErrorCode</c> path it uses for a probe failure.
/// </summary>
internal sealed class FakeObjCRuntime : IObjCRuntime
{
    // Distinct non-zero sentinel handles the orchestrator threads through and releases; distinct values so the release
    // set can tell the fresh LAContext from the reason NSString.
    internal static readonly IntPtr ContextHandle = new(0x1A0);
    internal static readonly IntPtr ReasonHandle = new(0x2B0);
    internal static readonly IntPtr ReplyBlockHandle = new(0x3C0);
    internal static readonly IntPtr ErrorHandle = new(0x4D0);

    // The scripted reply's success byte, delivered when the orchestrator posts the interactive evaluation (the user
    // answering) or when it invalidates the context (the cancel path). Null means "deliver no reply on this event."
    private readonly byte? _replyOnEvaluate;
    private readonly byte? _replyOnInvalidate;

    private readonly SingleCallRecorder<int, IntPtr> _createContext;
    private readonly SingleCallRecorder<string, IntPtr> _createReason;
    private readonly SingleCallRecorder<nint, bool> _canEvaluateCall;
    private readonly SingleCallRecorder<IntPtr, long> _getErrorCode;
    private readonly SingleCallRecorder<IntPtr, IntPtr> _buildReplyBlock;
    private readonly SingleCallRecorder<nint, bool> _evaluate;
    private readonly SingleCallRecorder<IntPtr, bool> _invalidate;
    private readonly SingleCallRecorder<IntPtr, bool> _release;
    private readonly SingleCallRecorder<IntPtr, bool> _freeReplyBlock;

    private readonly List<IntPtr> _released = new();

    private IntPtr _capturedCompletion;

    private FakeObjCRuntime(bool canEvaluate, long errorCode, byte? replyOnEvaluate, byte? replyOnInvalidate)
    {
        _replyOnEvaluate = replyOnEvaluate;
        _replyOnInvalidate = replyOnInvalidate;

        _createContext = SingleCallRecorder<int, IntPtr>.Returning(ContextHandle);
        _createReason = SingleCallRecorder<string, IntPtr>.Returning(ReasonHandle);
        _canEvaluateCall = SingleCallRecorder<nint, bool>.Returning(canEvaluate);
        _getErrorCode = SingleCallRecorder<IntPtr, long>.Returning(errorCode);
        _buildReplyBlock = SingleCallRecorder<IntPtr, IntPtr>.Returning(ReplyBlockHandle);
        _evaluate = SingleCallRecorder<nint, bool>.Returning(result: false);
        _invalidate = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
        _release = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
        _freeReplyBlock = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
    }

    /// <summary>Gets the number of fresh <c>LAContext</c> allocations.</summary>
    internal int ContextsCreated => _createContext.CallCount;

    /// <summary>Gets the number of interactive <c>evaluatePolicy</c> posts.</summary>
    internal int EvaluateCalls => _evaluate.CallCount;

    /// <summary>Gets the number of <c>[context invalidate]</c> cancel messages.</summary>
    internal int InvalidateCalls => _invalidate.CallCount;

    /// <summary>Gets the number of reply-block frees.</summary>
    internal int ReplyBlocksFreed => _freeReplyBlock.CallCount;

    /// <summary>Gets the prompt passed to the most recent reason-string allocation, or <see langword="null"/> if none.</summary>
    internal string? LastReason => _createReason.LastArg;

    /// <summary>Gets the policy passed to the most recent capability probe.</summary>
    internal nint LastProbePolicy => _canEvaluateCall.LastArg;

    /// <summary>Gets the handles the orchestrator released, in release order.</summary>
    internal IReadOnlyList<IntPtr> ReleasedHandles => _released;

    /// <inheritdoc />
    public IntPtr CreateContext() => _createContext.Record(0);

    /// <inheritdoc />
    public IntPtr CreateReasonString(string reason) => _createReason.Record(reason);

    /// <inheritdoc />
    public bool CanEvaluate(IntPtr context, nint policy, out IntPtr error)
    {
        bool canEvaluate = _canEvaluateCall.Record(policy);
        error = canEvaluate ? IntPtr.Zero : ErrorHandle;
        return canEvaluate;
    }

    /// <inheritdoc />
    public long GetErrorCode(IntPtr error) => _getErrorCode.Record(error);

    /// <inheritdoc />
    public IntPtr BuildReplyBlock(IntPtr capturedCompletion)
    {
        _capturedCompletion = capturedCompletion;
        return _buildReplyBlock.Record(capturedCompletion);
    }

    /// <inheritdoc />
    public void Evaluate(IntPtr context, nint policy, IntPtr reason, IntPtr replyBlock)
    {
        _evaluate.Record(policy);
        Deliver(_replyOnEvaluate);
    }

    /// <inheritdoc />
    public void Invalidate(IntPtr context)
    {
        _invalidate.Record(context);
        Deliver(_replyOnInvalidate);
    }

    /// <inheritdoc />
    public void Release(IntPtr instance)
    {
        _release.Record(instance);
        _released.Add(instance);
    }

    /// <inheritdoc />
    public void FreeReplyBlock(IntPtr replyBlock) => _freeReplyBlock.Record(replyBlock);

    /// <summary>The non-interactive capability probe fails; the orchestrator maps the probe error and never prompts.</summary>
    /// <param name="errorCode">The raw <c>LAError</c> code <see cref="GetErrorCode"/> returns for the probe's NSError.</param>
    /// <returns>The configured fake.</returns>
    internal static FakeObjCRuntime ProbeFails(long errorCode) => new(canEvaluate: false, errorCode, null, null);

    /// <summary>The probe passes and the interactive evaluation replies with the given success/error when posted.</summary>
    /// <param name="success">The reply's success byte (non-zero = the human satisfied the policy).</param>
    /// <param name="errorCode">The raw <c>LAError</c> code <see cref="GetErrorCode"/> returns on a failure reply.</param>
    /// <returns>The configured fake.</returns>
    internal static FakeObjCRuntime InteractiveReply(byte success, long errorCode) =>
        new(canEvaluate: true, errorCode, replyOnEvaluate: success, null);

    /// <summary>
    /// The probe passes but the evaluation stays pending until the orchestrator invalidates the context (the cancel
    /// path); invalidation then delivers a failure reply carrying <paramref name="cancelCode"/>.
    /// </summary>
    /// <param name="cancelCode">The raw <c>LAError</c> code the invalidate-driven cancel reply carries.</param>
    /// <returns>The configured fake.</returns>
    internal static FakeObjCRuntime CancelledOnInvalidate(long cancelCode) =>
        new(canEvaluate: true, cancelCode, replyOnEvaluate: null, replyOnInvalidate: 0);

    // Models the OS invoking the reply block: recover the orchestrator's captured reply handler and hand it the scripted
    // (success, error) reply. A non-zero success carries no error (IntPtr.Zero); a failure carries the error pointer
    // whose code GetErrorCode returns, so the orchestrator reads and maps it exactly as it maps a probe failure.
    private void Deliver(byte? success)
    {
        if (success is null)
        {
            return;
        }

        IntPtr error = success.Value != 0 ? IntPtr.Zero : ErrorHandle;
        var reply = (Action<byte, IntPtr>)GCHandle.FromIntPtr(_capturedCompletion).Target!;
        reply(success.Value, error);
    }
}
