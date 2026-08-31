// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.MacOS;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the macOS raw-<c>LAError</c>-code-to-<see cref="LaResult"/> correspondence the coverage refactor
/// extracts into <c>LocalAuthentication.MapErrorCode</c> — every native error code the <see cref="LaResult"/> enum's own
/// XML documentation names, paired with the outcome that doc requires, spelled exactly once here (DRY). The pure-mapper
/// table test (<c>LocalAuthenticationMapErrorCodeTests</c>) drives it, and the orchestrator probe-fail flow test
/// (<c>LocalAuthenticationFlowTests</c>) consumes the same table (DRY).
///
/// The numeric codes are Apple's public, stable <c>LAError</c> constants (<c>LocalAuthentication/LAError.h</c>), NOT
/// values invented by this project; the outcome each maps to is taken verbatim from the <see cref="LaResult"/> member the
/// frozen ARCHITECTURE enum pairs it with. <see cref="LaResult"/> is internal (reached here through the macOS IVT grant),
/// so it cannot appear in a public xUnit theory-method signature; each row carries the outcome as its underlying
/// <see cref="int"/> constant, cast back inside the test.
/// </summary>
internal static class MacOsErrorCodeOutcomes
{
    // Apple's public LAPolicy value for "Touch ID or the account password" (LocalAuthentication framework) — the one
    // policy this project probes and evaluates; the orchestrator and the native-ops spec must probe/evaluate exactly
    // this value. Owned here (the single macOS LAError/LAPolicy owner) so it is spelled once, not per test.
    internal const nint DeviceOwnerAuthentication = 2;

    // Apple's LAErrorUserCancel (LocalAuthentication/LAError.h): the code LA reports when an invalidate cancels a pending
    // evaluation; it maps to LaResult.UserCancel. Internal (not private) because the orchestrator-flow cancellation test
    // consumes it too — one owner for the constant. Grouped with the other LAError codes below despite the ordering.
    internal const long LAErrorUserCancel = -2;

    // Apple's public LAError codes (LocalAuthentication framework). Named here so the table reads as the OS contract it
    // encodes, not as bare magic numbers.
    private const long LAErrorAuthenticationFailed = -1;
    private const long LAErrorPasscodeNotSet = -5;
    private const long LAErrorBiometryNotAvailable = -6;
    private const long LAErrorBiometryNotEnrolled = -7;

    // A code that matches no LAError constant, to exercise the enum's documented catch-all ("Any other native outcome,
    // mapped to an error" -> LaResult.Unknown). Two arbitrary out-of-range values, one negative and one non-negative.
    private const long UnmappedNegativeCode = -9999;
    private const long UnmappedZeroCode = 0;

    /// <summary>
    /// Gets every documented macOS <c>LAError</c> code paired with the <see cref="LaResult"/> the frozen enum requires it
    /// to map to, plus the catch-all rows for an unrecognized code.
    /// </summary>
    public static TheoryData<long, int> Cases { get; } = new()
    {
        // LaResult.AuthenticationFailed: "The human was present but the challenge failed (LAErrorAuthenticationFailed)."
        { LAErrorAuthenticationFailed, (int)LaResult.AuthenticationFailed },

        // LaResult.UserCancel: "The human dismissed or cancelled the prompt (LAErrorUserCancel)."
        { LAErrorUserCancel, (int)LaResult.UserCancel },

        // LaResult.NotEnrolled: "No presence method is enrolled (LAErrorBiometryNotEnrolled/no passcode set)."
        { LAErrorBiometryNotEnrolled, (int)LaResult.NotEnrolled },
        { LAErrorPasscodeNotSet, (int)LaResult.NotEnrolled },

        // LaResult.NotAvailable: "No presence method is available on this host (LAErrorBiometryNotAvailable/policy
        // unavailable)."
        { LAErrorBiometryNotAvailable, (int)LaResult.NotAvailable },

        // LaResult.Unknown: "Any other native outcome, mapped to an error."
        { UnmappedNegativeCode, (int)LaResult.Unknown },
        { UnmappedZeroCode, (int)LaResult.Unknown },
    };
}
