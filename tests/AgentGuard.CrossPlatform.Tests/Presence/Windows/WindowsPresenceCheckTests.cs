// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.CrossPlatform.Windows;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Windows <c>IPresenceCheck</c> implementation driven over its port fake (contract "What to do" #7): each
/// <c>Check</c> reaches the native port exactly once (a fresh request per call), passes the request's prompt text, and
/// maps the port's <see cref="WindowsPresenceResult"/> to the <see cref="PresenceResult"/> the gate consumes. Compiled
/// only on the Windows CI leg.
/// </summary>
public sealed class WindowsPresenceCheckTests
{
    // WindowsPresenceResult is internal (reached here through the Windows IVT grant), so it cannot appear in a public
    // test-method signature; the outcome is passed as its underlying int constant and cast back inside. The
    // outcome->reason table is owned once by WindowsPresenceOutcomes and shared with the pure-mapper test (DRY). The
    // single-fresh-call / prompt-passthrough / mapping arrange-act-assert is owned once by PresenceCheckPortSpec and
    // shared with macOS.
    [Theory]
    [MemberData(nameof(WindowsPresenceOutcomes.Cases), MemberType = typeof(WindowsPresenceOutcomes))]
    public Task Check_CallsThePortOncePerCheck_AndMapsTheOutcome(int nativeOutcome, ApprovalReason expected) =>
        PresenceCheckPortSpec.AssertMapsOutcomeInOneFreshCall(
            new FakeWindowsUserPresence((WindowsPresenceResult)nativeOutcome), WindowsPresenceCheck.Create, expected);
}
