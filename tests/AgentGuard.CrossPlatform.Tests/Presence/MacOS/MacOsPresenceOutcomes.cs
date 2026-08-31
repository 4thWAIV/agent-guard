// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;
using AgentGuard.CrossPlatform.MacOS;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the macOS native-outcome-to-<see cref="ApprovalReason"/> correspondence — every
/// <see cref="LaResult"/> and the reason it must map to, spelled exactly once here (DRY). Both the pure-mapper test
/// (<c>MacOsPresenceMapperTests</c>) and the check-calls-the-port test (<c>MacOsPresenceCheckTests</c>) drive this same
/// table, so the mapping is written in one place while each still proves full-reason coverage. <see cref="LaResult"/> is
/// internal and cannot appear in a public xUnit theory-method signature, so each row carries the outcome as its
/// underlying <see cref="int"/> constant, cast back inside the test.
/// </summary>
internal static class MacOsPresenceOutcomes
{
    /// <summary>Gets every macOS native outcome paired with the <see cref="ApprovalReason"/> it maps to.</summary>
    public static TheoryData<int, ApprovalReason> Cases { get; } = new()
    {
        { (int)LaResult.Success, ApprovalReason.Approved },
        { (int)LaResult.UserCancel, ApprovalReason.Cancelled },
        { (int)LaResult.AuthenticationFailed, ApprovalReason.Denied },
        { (int)LaResult.NotAvailable, ApprovalReason.Unavailable },
        { (int)LaResult.NotEnrolled, ApprovalReason.Unavailable },
        { (int)LaResult.Unknown, ApprovalReason.Error },
    };
}
