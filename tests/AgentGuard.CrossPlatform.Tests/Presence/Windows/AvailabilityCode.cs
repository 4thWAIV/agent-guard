// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the public WinRT <c>UserConsentVerifierAvailability</c> values
/// (<c>Windows.Security.Credentials.UI</c>) the Windows presence tests use — spelled exactly once here (DRY), the sibling
/// of <see cref="AsyncStatusCode"/> and <see cref="HelloResultCode"/>, so the orchestrator-flow test
/// (<c>WindowsUserPresenceFlowTests</c>) drives the availability guard through the OS's stable enum ordinals rather than
/// re-declaring them. These are the OS's stable enum values, not values this project invents.
/// </summary>
internal static class AvailabilityCode
{
    /// <summary>Windows Hello is available; the interactive verification may proceed.</summary>
    internal const int Available = 0;

    /// <summary>No Hello-capable device is present (the value a headless runner reports fast).</summary>
    internal const int DeviceNotPresent = 1;
}
