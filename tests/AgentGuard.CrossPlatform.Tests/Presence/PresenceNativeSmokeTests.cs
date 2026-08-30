// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The per-OS native smoke (acceptance #8): the OS-agnostic pointed-integration test that drives the REAL per-OS
/// presence implementation selected onto this CI leg — macOS <c>MacOsPresenceCheck</c>, Windows
/// <c>WindowsPresenceCheck</c>, or Linux <c>LinuxPresenceCheck</c> — through the real container, and proves it returns a
/// non-approved reason on a headless runner (no physically-present human answers the out-of-band prompt). It calls the
/// real <c>Check</c> directly (AG0108 exempts the test assembly). Runs on the per-OS CI legs.
/// </summary>
public sealed class PresenceNativeSmokeTests
{
    // A near-zero bound: on a headless CI runner the real binding answers fast; if it would instead block on an
    // interactive prompt, this cancels it almost at once, and the port dismisses the dialog and returns a non-approved
    // reason — never a real wait, never a hang. Flows through the injected clock (AG0038). Tunable.
    private static readonly TimeSpan NearZeroBound = TimeSpan.FromSeconds(1);

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public async Task RealPresenceImplementation_ReturnsANonApprovedReason_OnAHeadlessRunner()
    {
        ISystemServices services = SystemServicesBuilder.Real().Build();
        IPresenceCheck presence = services.Platform.Presence;

        using var bound = new CancellationTokenSource(NearZeroBound, services.Clock);
        PresenceResult result = await presence.Check(
            new PresenceRequest("AgentGuard presence native smoke"), services, bound.Token);

        result.Reason.Should().NotBe(
            ApprovalReason.Approved,
            "no physically-present human answers the out-of-band prompt on a headless CI runner");
    }
}
