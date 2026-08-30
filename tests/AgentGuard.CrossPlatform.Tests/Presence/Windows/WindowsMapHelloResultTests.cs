// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Windows;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Windows extracted-mapper table test for Windows Hello (contract-coverage-refactor acceptance #5, "every extracted
/// mapper ... Hello 7-way"): <c>WindowsUserPresence.MapHelloResult</c> is the pure function the coverage refactor pulls
/// out so the WinRT <c>UserConsentVerificationResult</c> translation is unit-testable. It pins the FULL seven-way table
/// this refactor preserves from the pre-refactor behavior: a verified consent to the verified outcome; a cancel to the
/// cancel outcome; device-busy and retries-exhausted to the failed outcome; and the three unavailable / un-configured
/// results (device-not-present, not-configured, disabled-by-policy) to null so the orchestrator falls back to the
/// credential prompt. Compiled only on the Windows CI leg.
/// </summary>
public sealed class WindowsMapHelloResultTests
{
    // WindowsPresenceResult is internal (reached here through the Windows IVT grant), so it cannot appear in a public
    // theory-method signature; the expected outcome crosses as its underlying int, and a null expected means the mapper
    // returns null (fall back to the credential prompt). The WinRT consent ordinals are owned once by HelloResultCode.
    public static TheoryData<int, int?> Cases { get; } = new()
    {
        // Hello verified the human.
        { HelloResultCode.Verified, (int)WindowsPresenceResult.Verified },

        // The human dismissed the Hello prompt.
        { HelloResultCode.Canceled, (int)WindowsPresenceResult.Cancelled },

        // The human was present but Hello did not verify — a transient/exhausted challenge maps to the failed outcome.
        { HelloResultCode.DeviceBusy, (int)WindowsPresenceResult.Failed },
        { HelloResultCode.RetriesExhausted, (int)WindowsPresenceResult.Failed },

        // Hello is unavailable or un-configured on this host: fall back to the credential prompt (null).
        { HelloResultCode.DeviceNotPresent, null },
        { HelloResultCode.NotConfiguredForUser, null },
        { HelloResultCode.DisabledByPolicy, null },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void MapHelloResult_TranslatesConsent_OrFallsBackWhenHelloIsUnavailable(int consentResult, int? expected)
    {
        WindowsPresenceResult? actual = WindowsUserPresence.MapHelloResult(consentResult);

        actual.ShouldMatch(expected);
    }
}
