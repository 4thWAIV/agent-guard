// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Windows;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Windows extracted-mapper table test for the credential prompt (contract-coverage-refactor acceptance #5, "every
/// extracted mapper"): <c>WindowsUserPresence.MapCredentialStatus</c> is the pure function the coverage refactor pulls out
/// so the secure-desktop prompt's status branch is unit-testable. Its contract (from the frozen signature docs): a
/// null result means the prompt succeeded with a buffer and the orchestrator continues to unpack and validate; any early
/// outcome is either a user cancel or "no method." Compiled only on the Windows CI leg.
/// </summary>
public sealed class WindowsMapCredentialStatusTests
{
    // WindowsPresenceResult is internal (reached here through the Windows IVT grant), so it cannot appear in a public
    // theory-method signature; each expected outcome crosses as its underlying int, and a null expected means the mapper
    // returns null (continue to validation). The Win32 status codes are owned once by CredentialStatus, and the
    // cast-back-and-assert step by WindowsPresenceResultAssert.ShouldMatch (both shared with the other Windows tests).
    public static TheoryData<uint, bool, int?> Cases { get; } = new()
    {
        // Success WITH a buffer: continue to unpack and validate (null, no early outcome).
        { CredentialStatus.ErrorSuccess, true, null },

        // Success WITHOUT a buffer: nothing was typed, so there is no method to validate.
        { CredentialStatus.ErrorSuccess, false, (int)WindowsPresenceResult.NoMethod },

        // The user dismissed the secure-desktop prompt.
        { CredentialStatus.ErrorCancelled, true, (int)WindowsPresenceResult.Cancelled },
        { CredentialStatus.ErrorCancelled, false, (int)WindowsPresenceResult.Cancelled },

        // Any other failure: no interactive surface came up, so no method.
        { CredentialStatus.OtherFailure, true, (int)WindowsPresenceResult.NoMethod },
        { CredentialStatus.OtherFailure, false, (int)WindowsPresenceResult.NoMethod },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void MapCredentialStatus_BranchesTheStatus_ToContinueOrAnEarlyOutcome(
        uint promptStatus, bool hasBuffer, int? expected)
    {
        WindowsPresenceResult? actual = WindowsUserPresence.MapCredentialStatus(promptStatus, hasBuffer);

        actual.ShouldMatch(expected);
    }
}
