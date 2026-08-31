// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The internal Windows presence native port — the one seam through which the Windows presence check reaches Windows
/// Hello and the secure-desktop credential prompt. Its sole implementer (<see cref="WindowsUserPresence"/>) is the
/// fake-testable orchestrator that composes the native ops; the raw interop lives one layer below — the WinRT
/// <c>UserConsentVerifier</c> binding in <see cref="IWindowsHelloNativeOps"/>'s owner and the
/// <c>CredUIPromptForWindowsCredentials</c>/<c>LogonUser</c> P/Invoke in <see cref="ICredentialPromptNativeOps"/>'s owner
/// (per-os-native-behind-a-port; enforced by AG0113/AG0114). It creates a fresh Hello request per call and caches no
/// auth context (AG0112), and returns a plain <see cref="WindowsPresenceResult"/>.
/// </summary>
internal interface IWindowsUserPresence
{
    /// <summary>
    /// Verifies a physically-present human: it tries Windows Hello (the WinRT <c>UserConsentVerifier</c> interop) and,
    /// where Hello is unavailable, un-configured, or cannot bind, falls back to the secure-desktop credential prompt
    /// validated with <c>LogonUser</c> — both out-of-band. It creates a fresh request per call and returns the plain
    /// outcome.
    /// </summary>
    /// <param name="reason">The reviewed prompt text shown to the human.</param>
    /// <param name="ct">The gate's cancellation token; the port honors it but mints no timeout of its own.</param>
    /// <returns>The plain verification outcome.</returns>
    Task<WindowsPresenceResult> VerifyAsync(string reason, CancellationToken ct);
}
