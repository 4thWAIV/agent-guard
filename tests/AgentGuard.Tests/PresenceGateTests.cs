// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Setup;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Above-the-seam tests of the asynchronous approval gate itself (<c>ApprovalGate.RequireApprovalAsync</c>), driving the
/// real gate over a configurable presence boundary and a fake clock. These pin the reason mapping every command relies
/// on (acceptance #2 — each reason on its own CLI line) and the 60-second fail-closed timeout (acceptance #3), proven by
/// advancing a fake clock with no real wait.
/// </summary>
public sealed class PresenceGateTests
{
    [Theory]
    [InlineData(ApprovalReason.Denied)]
    [InlineData(ApprovalReason.Unavailable)]
    [InlineData(ApprovalReason.Cancelled)]
    [InlineData(ApprovalReason.Error)]
    public async Task RequireApproval_DeniesEveryNonApprovedReason_AndSurfacesTheBoundaryDetail(ApprovalReason reason)
    {
        string detail = $"boundary-detail-{reason}";
        var presence = FakePresenceCheck.Returning(reason, detail);
        ISystemServices services = Configure(presence).Build();

        ApprovalDecision decision = await ApprovalGate.RequireApprovalAsync(services, SetupVerb.Install);

        decision.Reason.Should().Be(reason);
        decision.IsApproved.Should().BeFalse();
        decision.Detail.Should().Be(detail);
        decision.Action.Should().Be(SetupVerb.Install);
    }

    [Fact]
    public async Task RequireApproval_Approves_WhenTheBoundaryApproves()
    {
        var presence = FakePresenceCheck.Approved("presence confirmed");
        ISystemServices services = Configure(presence).Build();

        ApprovalDecision decision = await ApprovalGate.RequireApprovalAsync(services, SetupVerb.Install);

        decision.Reason.Should().Be(ApprovalReason.Approved);
        decision.IsApproved.Should().BeTrue();
        presence.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(SetupVerb.Install)]
    [InlineData(SetupVerb.Init)]
    [InlineData(SetupVerb.Remove)]
    public async Task RequireApproval_PassesTheOwnedPerVerbDialogText_ToTheBoundary(string verb)
    {
        var presence = FakePresenceCheck.Approved("ok");
        ISystemServices services = Configure(presence).Build();

        await ApprovalGate.RequireApprovalAsync(services, verb);

        presence.LastRequest.Should().NotBeNull();
        presence.LastRequest!.PromptText.Should().Be(PresenceDialogText.For(verb));
    }

    [Fact]
    public async Task RequireApproval_TimesOutAndDenies_WhenTheBoundaryNeverCompletes_AdvancingAFakeClockPast60s()
    {
        var clock = new FakeTimeProvider();
        var presence = FakePresenceCheck.NeverCompleting();
        ISystemServices services = Configure(presence, clock).Build();

        Task<ApprovalDecision> gate = ApprovalGate.RequireApprovalAsync(services, SetupVerb.Install);

        // The boundary never completes on its own, so nothing resolves until the clock-driven wait expires.
        gate.IsCompleted.Should().BeFalse();

        clock.Advance(PresencePolicy.Timeout + TimeSpan.FromSeconds(1));

        ApprovalDecision decision = await gate;
        decision.Reason.Should().Be(ApprovalReason.TimedOut);
        decision.IsApproved.Should().BeFalse();
    }

    // Returns the configured builder (never a built ISystemServices — a static method returning the container would be
    // an off-site composition, AG0017); the caller builds inline with the instance .Build().
    private static SystemServicesBuilder Configure(FakePresenceCheck presence, TimeProvider? clock = null)
    {
        SystemServicesBuilder builder = SystemServicesBuilder.Fake();
        if (clock is not null)
        {
            builder.With(clock);
        }

        builder.OnPlatform().With(presence.Presence);
        return builder;
    }
}
