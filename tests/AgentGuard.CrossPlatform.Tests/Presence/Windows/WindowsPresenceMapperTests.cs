// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;
using AgentGuard.CrossPlatform.Windows;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Windows pure-mapper table test (contract "What to do" #7): <c>WindowsPresenceCheck.Map</c> translates EVERY
/// native <see cref="WindowsPresenceResult"/> (collapsing the Hello and credential-prompt paths) to the shared
/// <see cref="ApprovalReason"/> the gate expects. Compiled only on the Windows CI leg.
/// </summary>
public sealed class WindowsPresenceMapperTests
{
    // WindowsPresenceResult is internal (reached here through the Windows IVT grant), so it cannot appear in a public
    // test-method signature; the outcome is passed as its underlying int constant and cast back inside. The
    // outcome->reason table is owned once by WindowsPresenceOutcomes and shared with the check-calls-the-port test (DRY).
    [Theory]
    [MemberData(nameof(WindowsPresenceOutcomes.Cases), MemberType = typeof(WindowsPresenceOutcomes))]
    public void Map_TranslatesEveryNativeOutcome_ToItsApprovalReason(int nativeOutcome, ApprovalReason expected)
    {
        WindowsPresenceCheck.Map((WindowsPresenceResult)nativeOutcome).Should().Be(expected);
    }
}
