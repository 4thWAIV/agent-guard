// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.MacOS;

/// <summary>
/// The plain macOS LocalAuthentication outcome the native port returns, mapped to <c>ApprovalReason</c> by
/// <see cref="MacOsPresenceCheck.Map"/>. It names the native conditions the <c>evaluatePolicy</c> result distinguishes,
/// with <see cref="Unknown"/> as the catch-all for any code the mapper treats as an error.
/// </summary>
internal enum LaResult
{
    /// <summary>The human satisfied the policy (Touch ID or the account password).</summary>
    Success,

    /// <summary>The human dismissed or cancelled the prompt (<c>LAErrorUserCancel</c>).</summary>
    UserCancel,

    /// <summary>The human was present but the challenge failed (<c>LAErrorAuthenticationFailed</c>).</summary>
    AuthenticationFailed,

    /// <summary>No presence method is available on this host (<c>LAErrorBiometryNotAvailable</c>/policy unavailable).</summary>
    NotAvailable,

    /// <summary>No presence method is enrolled (<c>LAErrorBiometryNotEnrolled</c>/no passcode set).</summary>
    NotEnrolled,

    /// <summary>Any other native outcome, mapped to an error.</summary>
    Unknown,
}
