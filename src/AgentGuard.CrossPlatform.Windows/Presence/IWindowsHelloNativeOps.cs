// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The internal Windows HELLO native-ops owner — the thin, stateless seam holding the raw WinRT
/// <c>UserConsentVerifier</c> interop (apartment init, activation, <c>CheckAvailabilityAsync</c>,
/// <c>RequestVerificationForWindowAsync</c>, the COM-visible completed handler, <c>GetResults</c>, the
/// <c>IAsyncInfo.Cancel</c> cancel primitive, and their <c>Marshal</c> uses), split out from below the
/// <see cref="IWindowsUserPresence"/> flow port. Windows gets TWO native-ops owners because Hello and the credential
/// prompt are independent native subsystems sharing no handle (macos-one-windows-two-native-ops); this is the Hello one.
/// Its sole implementer (<see cref="WindowsHelloNativeOps"/>) is where the relocated raw-interop exemption lives (AG0113)
/// and it holds no field (AG0116). Every method is a SYNCHRONOUS raw call with no branching (AG0106) and no
/// task-completion wiring (AG0115): the <c>TaskCompletionSource</c>, the <c>await</c>, the async-status decision, the
/// availability short-circuit, the cancel-registration, and the 7-way Hello-result mapping all live in the fake-testable
/// orchestrator, which composes these primitives.
/// </summary>
internal interface IWindowsHelloNativeOps
{
    /// <summary>
    /// Begins the non-interactive Hello availability check: it activates the <c>UserConsentVerifier</c> statics and issues
    /// <c>CheckAvailabilityAsync</c>, wiring a completed handler that invokes <paramref name="onCompleted"/> with the WinRT
    /// async status when the operation finishes. Returns as soon as the request is posted. On a headless runner with no
    /// Hello hardware this answers a non-<c>Available</c> code promptly, so the orchestrator can fail closed WITHOUT ever
    /// issuing the blocking <see cref="BeginVerification"/> request.
    /// </summary>
    /// <param name="onCompleted">The orchestrator callback the completed handler invokes with the raw async status.</param>
    /// <returns>The async-operation pointer, passed to <see cref="GetAvailabilityResult"/> and <see cref="ReleaseOperation"/>.</returns>
    IntPtr BeginAvailabilityCheck(Action<int> onCompleted);

    /// <summary>
    /// Reads the availability result (<c>GetResults</c>) of a completed availability async operation — the raw
    /// <c>UserConsentVerifierAvailability</c> code the orchestrator compares against <c>Available</c>.
    /// </summary>
    /// <param name="asyncOperation">The completed async operation from <see cref="BeginAvailabilityCheck"/>.</param>
    /// <returns>The raw <c>UserConsentVerifierAvailability</c> code.</returns>
    int GetAvailabilityResult(IntPtr asyncOperation);

    /// <summary>
    /// Begins one Hello verification: it activates the <c>UserConsentVerifier</c> interop and issues
    /// <c>RequestVerificationForWindowAsync</c>, wiring a completed handler that invokes <paramref name="onCompleted"/>
    /// with the WinRT async status when the operation finishes. Returns as soon as the request is posted.
    /// </summary>
    /// <param name="reason">The reviewed prompt text shown to the human.</param>
    /// <param name="onCompleted">The orchestrator callback the completed handler invokes with the raw async status.</param>
    /// <returns>The async-operation pointer, passed to <see cref="GetVerificationResult"/> and <see cref="ReleaseOperation"/>.</returns>
    IntPtr BeginVerification(string reason, Action<int> onCompleted);

    /// <summary>
    /// Reads the verification result (<c>GetResults</c>) of a completed async operation — the raw
    /// <c>UserConsentVerificationResult</c> the orchestrator maps.
    /// </summary>
    /// <param name="asyncOperation">The completed async operation from <see cref="BeginVerification"/>.</param>
    /// <returns>The raw <c>UserConsentVerificationResult</c> code.</returns>
    int GetVerificationResult(IntPtr asyncOperation);

    /// <summary>
    /// Asks a pending async operation to cancel (<c>IAsyncInfo.Cancel</c>) — the cancel primitive the orchestrator
    /// registers on the caller's cancellation token. WinRT cancellation is cooperative: cancelling unblocks the pending
    /// await promptly (the completed handler fires with a cancelled async status), even if it does not guarantee the
    /// on-screen dialog is dismissed.
    /// </summary>
    /// <param name="asyncOperation">The pending async operation from <see cref="BeginAvailabilityCheck"/> or <see cref="BeginVerification"/>.</param>
    void CancelOperation(IntPtr asyncOperation);

    /// <summary>
    /// Releases the async operation returned by <see cref="BeginAvailabilityCheck"/> or <see cref="BeginVerification"/>
    /// once the orchestrator has read its result.
    /// </summary>
    /// <param name="asyncOperation">The async-operation pointer to release.</param>
    void ReleaseOperation(IntPtr asyncOperation);
}
