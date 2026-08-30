// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.CrossPlatform.MacOS;

/// <summary>
/// The internal macOS presence NATIVE-OPS owner — the thin, stateless seam the coverage refactor splits out from below
/// the <see cref="ILocalAuthentication"/> flow port to hold the raw Objective-C runtime calls (<c>objc_getClass</c>,
/// <c>objc_msgSend</c>, <c>sel_registerName</c>, <c>dlsym</c>, the block build, and their <c>Marshal</c> uses). Its sole
/// implementer (<see cref="ObjCRuntime"/>) is the only class that binds the runtime, so it is where the relocated
/// raw-presence-interop exemption lives (AG0113) and the only presence class that may hold no field (AG0116). Every
/// method is a SYNCHRONOUS straight-line raw call with no branching (AG0106) and no task-completion wiring (AG0115): the
/// flow, mapping, <c>TaskCompletionSource</c>/<c>GCHandle</c> wiring, and cancel-invalidate logic all live in the
/// fake-testable <see cref="ILocalAuthentication"/> orchestrator that composes these primitives. Every handle it returns
/// is a per-call local the orchestrator owns and releases; the native-ops class caches nothing.
///
/// It is marked <see cref="RequiresNativeSpecTestAttribute"/> — the production-side, single source of the guardrail-1
/// requirement that this native-ops owner have its own <c>[Trait("Category","NativeSpec")]</c> spec test (the macOS
/// <c>MacOsObjCRuntimeSpecTests</c> already satisfies it).
/// </summary>
[RequiresNativeSpecTest]
internal interface IObjCRuntime
{
    /// <summary>
    /// Creates a fresh <c>LAContext</c> (<c>[[LAContext alloc] init]</c>) — a new context per evaluation
    /// (fresh-interaction-no-cached-yes); the orchestrator releases it.
    /// </summary>
    /// <returns>The new <c>LAContext</c> instance pointer.</returns>
    IntPtr CreateContext();

    /// <summary>
    /// Creates a <c>+1</c>-owned <c>NSString</c> from the reviewed prompt (<c>[[NSString alloc] initWithUTF8String:]</c>);
    /// the orchestrator releases it.
    /// </summary>
    /// <param name="reason">The reviewed prompt text passed as the <c>localizedReason</c>.</param>
    /// <returns>The new <c>NSString</c> instance pointer.</returns>
    IntPtr CreateReasonString(string reason);

    /// <summary>
    /// Runs the non-interactive capability probe (<c>canEvaluatePolicy:error:</c>) for the given policy: whether the
    /// device can authenticate its owner without showing a dialog.
    /// </summary>
    /// <param name="context">The <c>LAContext</c> to probe.</param>
    /// <param name="policy">The <c>LAPolicy</c> value to probe.</param>
    /// <param name="error">The <c>NSError</c> pointer the runtime writes when the probe fails, else <see cref="IntPtr.Zero"/>.</param>
    /// <returns><see langword="true"/> when the policy can be evaluated; otherwise <see langword="false"/>.</returns>
    bool CanEvaluate(IntPtr context, nint policy, out IntPtr error);

    /// <summary>
    /// Reads the integer <c>-code</c> of an <c>NSError</c> (<c>[error code]</c>) so the orchestrator can map it to a
    /// plain native outcome.
    /// </summary>
    /// <param name="error">The <c>NSError</c> pointer to read.</param>
    /// <returns>The error's native <c>code</c>.</returns>
    long GetErrorCode(IntPtr error);

    /// <summary>
    /// Assembles the Objective-C reply block the interactive evaluation invokes on completion, capturing the
    /// orchestrator's completion handle so the block's invoke can hand the raw reply back to the flow port.
    /// </summary>
    /// <param name="capturedCompletion">The orchestrator-owned completion handle the block captures.</param>
    /// <returns>The native reply-block pointer to pass to <see cref="Evaluate"/>; freed by <see cref="FreeReplyBlock"/>.</returns>
    IntPtr BuildReplyBlock(IntPtr capturedCompletion);

    /// <summary>
    /// Issues the interactive evaluation (<c>evaluatePolicy:localizedReason:reply:</c>): the OS shows its out-of-band
    /// dialog and later invokes <paramref name="replyBlock"/>. Returns as soon as the request is posted; the reply
    /// arrives through the block.
    /// </summary>
    /// <param name="context">The <c>LAContext</c> to evaluate.</param>
    /// <param name="policy">The <c>LAPolicy</c> value to evaluate.</param>
    /// <param name="reason">The <c>NSString</c> prompt shown as the <c>localizedReason</c>.</param>
    /// <param name="replyBlock">The reply block built by <see cref="BuildReplyBlock"/>.</param>
    void Evaluate(IntPtr context, nint policy, IntPtr reason, IntPtr replyBlock);

    /// <summary>
    /// Invalidates a pending evaluation (<c>[context invalidate]</c>) — the cancel path: it dismisses the dialog and
    /// makes the runtime fire the reply block with a cancel error.
    /// </summary>
    /// <param name="context">The <c>LAContext</c> to invalidate.</param>
    void Invalidate(IntPtr context);

    /// <summary>
    /// Releases an Objective-C object (<c>[instance release]</c>); the orchestrator guards the non-zero pointer before
    /// calling.
    /// </summary>
    /// <param name="instance">The instance pointer to release.</param>
    void Release(IntPtr instance);

    /// <summary>
    /// Frees the native memory of a reply block built by <see cref="BuildReplyBlock"/>.
    /// </summary>
    /// <param name="replyBlock">The reply-block pointer to free.</param>
    void FreeReplyBlock(IntPtr replyBlock);
}
