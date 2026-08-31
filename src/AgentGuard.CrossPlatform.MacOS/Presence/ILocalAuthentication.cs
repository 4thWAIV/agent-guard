// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace AgentGuard.CrossPlatform.MacOS;

/// <summary>
/// The internal macOS presence native port — the one seam through which the macOS presence check reaches
/// LocalAuthentication. Its sole implementer (<see cref="LocalAuthentication"/>) is the fake-testable orchestrator that
/// composes the native ops; the raw <c>objc_msgSend</c> binding lives one layer below in <see cref="IObjCRuntime"/>'s
/// owner, <c>ObjCRuntime</c> (per-os-native-behind-a-port; enforced by AG0113/AG0114). It creates a fresh
/// <c>LAContext</c> per call (fresh-interaction-no-cached-yes) and returns a plain <see cref="LaResult"/>; the
/// <c>ApprovalReason</c> mapping is the caller's pure function, so no native detail leaks above this seam.
/// </summary>
internal interface ILocalAuthentication
{
    /// <summary>
    /// Evaluates <c>LAPolicy.DeviceOwnerAuthentication</c> (Touch ID or the account password) through the OS's own
    /// out-of-band dialog, with a fresh <c>LAContext</c> per call, and returns the plain native outcome.
    /// </summary>
    /// <param name="reason">The reviewed prompt text passed as <c>localizedReason</c>.</param>
    /// <param name="ct">The gate's cancellation token; the port honors it but mints no timeout of its own.</param>
    /// <returns>The plain native evaluation outcome.</returns>
    Task<LaResult> EvaluateAsync(string reason, CancellationToken ct);
}
