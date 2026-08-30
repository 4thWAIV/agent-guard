// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The internal Windows HELLO native-ops owner — the thin, stateless seam holding the raw WinRT
/// <c>UserConsentVerifier</c> interop (apartment init, activation, <c>RequestVerificationForWindowAsync</c>, the
/// COM-visible completed handler, <c>GetResults</c>, and their <c>Marshal</c> uses), split out from below the
/// <see cref="IWindowsUserPresence"/> flow port. Windows gets TWO native-ops owners because Hello and the credential
/// prompt are independent native subsystems sharing no handle (macos-one-windows-two-native-ops); this is the Hello one.
/// Its sole implementer (<see cref="WindowsHelloNativeOps"/>) is where the relocated raw-interop exemption lives (AG0113)
/// and it holds no field (AG0116). Every method is a SYNCHRONOUS raw call with no branching (AG0106) and no
/// task-completion wiring (AG0115): the <c>TaskCompletionSource</c>, the <c>await</c>, the async-status decision, and the
/// 7-way Hello-result mapping all live in the fake-testable orchestrator, which composes these primitives.
/// </summary>
internal interface IWindowsHelloNativeOps
{
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
    /// Releases the async operation returned by <see cref="BeginVerification"/> once the orchestrator has read its result.
    /// </summary>
    /// <param name="asyncOperation">The async-operation pointer to release.</param>
    void ReleaseOperation(IntPtr asyncOperation);
}
