// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.CrossPlatform.MacOS;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The macOS <c>IPresenceCheck</c> implementation driven over its port fake (contract "What to do" #7): each
/// <c>Check</c> reaches the native port exactly once (a fresh interaction per call), passes the request's prompt text as
/// the <c>localizedReason</c>, and maps the port's <see cref="LaResult"/> to the <see cref="PresenceResult"/> the gate
/// consumes. Compiled only on the macOS CI leg.
/// </summary>
public sealed class MacOsPresenceCheckTests
{
    // LaResult is internal (reached here through the macOS IVT grant), so it cannot appear in a public test-method
    // signature; the outcome is passed as its underlying int constant and cast back inside. The outcome->reason table is
    // owned once by MacOsPresenceOutcomes and shared with the pure-mapper test (DRY). The single-fresh-call /
    // prompt-passthrough / mapping arrange-act-assert is owned once by PresenceCheckPortSpec and shared with Windows.
    [Theory]
    [MemberData(nameof(MacOsPresenceOutcomes.Cases), MemberType = typeof(MacOsPresenceOutcomes))]
    public Task Check_CallsThePortOncePerCheck_AndMapsTheOutcome(int nativeOutcome, ApprovalReason expected) =>
        PresenceCheckPortSpec.AssertMapsOutcomeInOneFreshCall(
            new FakeLocalAuthentication((LaResult)nativeOutcome), MacOsPresenceCheck.Create, expected);
}
