// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the public WinRT <c>UserConsentVerificationResult</c> values
/// (<c>Windows.Security.Credentials.UI</c>) the Windows presence tests use — spelled exactly once here (DRY) so the Hello
/// mapper table test (<c>WindowsMapHelloResultTests</c>), the async-status mapper test (whose substitute codes are these
/// same values), and the orchestrator-flow test (<c>WindowsUserPresenceFlowTests</c>) share one set of ordinals rather
/// than re-declaring them. These are the OS's stable enum values, not values this project invents.
/// </summary>
internal static class HelloResultCode
{
    /// <summary>The human was verified by Windows Hello.</summary>
    internal const int Verified = 0;

    /// <summary>No Hello-capable device is present.</summary>
    internal const int DeviceNotPresent = 1;

    /// <summary>Hello is not configured for this user.</summary>
    internal const int NotConfiguredForUser = 2;

    /// <summary>Hello is disabled by policy.</summary>
    internal const int DisabledByPolicy = 3;

    /// <summary>The Hello device is busy.</summary>
    internal const int DeviceBusy = 4;

    /// <summary>The human exhausted the Hello retries.</summary>
    internal const int RetriesExhausted = 5;

    /// <summary>The human cancelled the Hello prompt.</summary>
    internal const int Canceled = 6;
}
