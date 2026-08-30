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
/// <c>WindowsPresenceCheck</c>, or Linux <c>LinuxPresenceCheck</c> — through the real container, and proves it HONORS
/// its cancellation token: when the token is cancelled the port dismisses its out-of-band prompt and returns a
/// non-approved reason WITHIN a bounded wall-clock, never blocking forever. This is the cross-platform cancellation
/// proof — the same test on all three legs. It calls the real <c>Check</c> directly (AG0108 exempts the test assembly).
/// Runs on the per-OS CI legs.
/// </summary>
public sealed class PresenceNativeSmokeTests
{
    // A near-zero bound: on a headless CI runner the real binding answers fast; if it would instead block on an
    // interactive prompt, this cancels it almost at once, and the port must dismiss the dialog and return a non-approved
    // reason. Flows through the injected clock (AG0038). Tunable.
    private static readonly TimeSpan NearZeroBound = TimeSpan.FromSeconds(1);

    // The outer no-hang bound: the cancelled Check must RETURN well within this. A port that ignores its token (the #47
    // defect the Windows leg currently hits) never returns, so this wait expires and the test FAILS cleanly instead of
    // the CI job hanging indefinitely. Comfortably larger than NearZeroBound so a real but slow dismiss does not flake;
    // flows through the injected clock. Tunable.
    private static readonly TimeSpan NoHangBound = TimeSpan.FromSeconds(30);

    [Fact]
    [Trait("Category", "NativeSmoke")]
    public async Task RealPresenceImplementation_HonorsCancellation_ReturnsNonApprovedWithoutHanging_OnAHeadlessRunner()
    {
        ISystemServices services = SystemServicesBuilder.Real().Build();
        IPresenceCheck presence = services.Platform.Presence;

        using var cancelSoon = new CancellationTokenSource(NearZeroBound, services.Clock);

        Task<PresenceResult> check = presence.Check(
            new PresenceRequest("AgentGuard presence cancellation proof"), services, cancelSoon.Token);

        // The cancellation must dismiss the OS prompt and let Check return; this bounded wait proves it does not hang.
        PresenceResult result = await check.WaitAsync(NoHangBound, services.Clock);

        result.Reason.Should().NotBe(
            ApprovalReason.Approved,
            "no physically-present human answers the out-of-band prompt on a headless CI runner");
    }
}
