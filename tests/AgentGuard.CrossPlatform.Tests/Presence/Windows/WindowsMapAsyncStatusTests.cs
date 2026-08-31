// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Windows;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Windows extracted-mapper test for the async completion status (contract-coverage-refactor acceptance #5, "every
/// extracted mapper"): <c>WindowsUserPresence.MapAsyncStatus</c> is the pure function the coverage refactor pulls out so
/// the Hello completed-handler's status branch is unit-testable. It pins the substitute-code table this refactor
/// preserves from the pre-refactor completed handler: a COMPLETED operation returns null (the orchestrator then reads the
/// real verification result via <c>GetResults</c>); a CANCELED operation substitutes the cancel consent code; any other
/// status (an error) substitutes the retries-exhausted consent code. Compiled only on the Windows CI leg.
/// </summary>
public sealed class WindowsMapAsyncStatusTests
{
    // The WinRT AsyncStatus values and the substitute UserConsentVerificationResult codes are owned once by
    // AsyncStatusCode / HelloResultCode. A null expected means the operation completed, so the mapper returns null.
    public static TheoryData<int, int?> Cases { get; } = new()
    {
        { AsyncStatusCode.Completed, null },
        { AsyncStatusCode.Canceled, HelloResultCode.Canceled },
        { AsyncStatusCode.Error, HelloResultCode.RetriesExhausted },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void MapAsyncStatus_SubstitutesTheConsentCode_OnlyWhenTheOperationDidNotComplete(int asyncStatus, int? expected)
    {
        int? actual = WindowsUserPresence.MapAsyncStatus(asyncStatus);

        actual.Should().Be(expected);
    }
}
