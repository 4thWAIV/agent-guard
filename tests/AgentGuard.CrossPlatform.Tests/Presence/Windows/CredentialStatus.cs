// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the public Win32 credential-UI status codes (winerror.h) the Windows presence tests use — spelled
/// exactly once here (DRY) so the mapper table test (<c>WindowsMapCredentialStatusTests</c>), the credential-prompt fake
/// (<c>FakeCredentialPromptNativeOps</c>), and the orchestrator-flow test (<c>WindowsUserPresenceFlowTests</c>) all read
/// the same constants rather than re-declaring them. These are the OS's stable return codes for
/// <c>CredUIPromptForWindowsCredentials</c>, not values this project invents.
/// </summary>
internal static class CredentialStatus
{
    /// <summary><c>ERROR_SUCCESS</c> — the prompt returned a typed credential.</summary>
    internal const uint ErrorSuccess = 0;

    /// <summary><c>ERROR_INVALID_FUNCTION</c> — stands in for any non-success failure status (no interactive surface).</summary>
    internal const uint OtherFailure = 1;

    /// <summary><c>ERROR_CANCELLED</c> — the human dismissed the secure-desktop prompt.</summary>
    internal const uint ErrorCancelled = 1223;
}
