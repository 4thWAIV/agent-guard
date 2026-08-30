// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The plain Windows presence outcome the native port returns, mapped to <c>ApprovalReason</c> by
/// <see cref="WindowsPresenceCheck.Map"/>. It collapses the Hello and credential-prompt paths to the outcomes the
/// decision needs.
/// </summary>
internal enum WindowsPresenceResult
{
    /// <summary>The human was verified (Hello succeeded, or the typed password validated).</summary>
    Verified,

    /// <summary>The human dismissed or cancelled the prompt.</summary>
    Cancelled,

    /// <summary>No presence method is available (no Hello and no password — a passwordless account).</summary>
    NoMethod,

    /// <summary>The human was present but the challenge failed (a wrong password, or Hello failed).</summary>
    Failed,

    /// <summary>Any other outcome, mapped to an error.</summary>
    Error,
}
