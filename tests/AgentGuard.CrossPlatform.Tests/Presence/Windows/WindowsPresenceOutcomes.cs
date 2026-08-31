// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;
using AgentGuard.CrossPlatform.Windows;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the Windows native-outcome-to-<see cref="ApprovalReason"/> correspondence — every
/// <see cref="WindowsPresenceResult"/> (collapsing the Hello and credential-prompt paths) and the reason it must map to,
/// spelled exactly once here (DRY). Both the pure-mapper test (<c>WindowsPresenceMapperTests</c>) and the
/// check-calls-the-port test (<c>WindowsPresenceCheckTests</c>) drive this same table, so the mapping is written in one
/// place while each still proves full-reason coverage. <see cref="WindowsPresenceResult"/> is internal and cannot appear
/// in a public xUnit theory-method signature, so each row carries the outcome as its underlying <see cref="int"/>
/// constant, cast back inside the test.
/// </summary>
internal static class WindowsPresenceOutcomes
{
    /// <summary>Gets every Windows native outcome paired with the <see cref="ApprovalReason"/> it maps to.</summary>
    public static TheoryData<int, ApprovalReason> Cases { get; } = new()
    {
        { (int)WindowsPresenceResult.Verified, ApprovalReason.Approved },
        { (int)WindowsPresenceResult.Cancelled, ApprovalReason.Cancelled },
        { (int)WindowsPresenceResult.NoMethod, ApprovalReason.Unavailable },
        { (int)WindowsPresenceResult.Failed, ApprovalReason.Denied },
        { (int)WindowsPresenceResult.Error, ApprovalReason.Error },
    };
}
