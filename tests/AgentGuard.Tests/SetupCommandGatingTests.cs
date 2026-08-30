// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;
using AgentGuard.Setup;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Above-the-seam tests that the three mutating setup commands are gated on a physically-present human (acceptance #2):
/// <c>install</c>/<c>init</c>/<c>remove</c> each deny and return early — with no mutation — on every non-approved reason
/// (<c>Denied</c>/<c>Unavailable</c>/<c>Cancelled</c>/<c>Error</c>), each surfacing the boundary detail on its own CLI
/// line; each proceeds on <c>Approved</c>; and <c>doctor</c> makes no presence call.
/// </summary>
public sealed class SetupCommandGatingTests
{
    [Theory]
    [InlineData(ApprovalReason.Denied)]
    [InlineData(ApprovalReason.Unavailable)]
    [InlineData(ApprovalReason.Cancelled)]
    [InlineData(ApprovalReason.Error)]
    public void Install_DeniesAndReturnsEarly_OnEveryNonApprovedReason(ApprovalReason reason)
    {
        string detail = $"install-detail-{reason}";
        var presence = FakePresenceCheck.Returning(reason, detail);
        using var harness = new SetupHarness(presence.Presence);

        CommandOutcome outcome = harness.Install("0.1.0-alpha");

        outcome.Success.Should().BeFalse();
        outcome.Messages.Should().ContainSingle().Which.Should().Be(detail);

        // Returned early — nothing was installed.
        harness.Files.Exists(harness.BinGuard).Should().BeFalse();
        harness.Files.Exists(harness.MachineStateFile).Should().BeFalse();
    }

    [Fact]
    public void Install_Proceeds_WhenApproved()
    {
        var presence = FakePresenceCheck.Approved("ok");
        using var harness = new SetupHarness(presence.Presence);

        CommandOutcome outcome = harness.Install("0.1.0-alpha");

        outcome.Success.Should().BeTrue();
        harness.Files.Exists(harness.BinGuard).Should().BeTrue();
        presence.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(ApprovalReason.Denied)]
    [InlineData(ApprovalReason.Unavailable)]
    [InlineData(ApprovalReason.Cancelled)]
    [InlineData(ApprovalReason.Error)]
    public void Init_DeniesAndReturnsEarly_OnEveryNonApprovedReason(ApprovalReason reason)
    {
        string detail = $"init-detail-{reason}";
        var presence = FakePresenceCheck.Returning(reason, detail);
        using var harness = new SetupHarness(presence.Presence);

        CommandOutcome outcome = harness.Init();

        outcome.Success.Should().BeFalse();

        // The failure is the gate's own detail — not the downstream "machine is not installed" message — which proves
        // the command returned at the gate before touching anything.
        outcome.Messages.Should().ContainSingle().Which.Should().Be(detail);
        harness.Directories.EnumerateChildren(harness.Project).Should().BeEmpty();
    }

    [Fact]
    public void Init_Proceeds_WhenApproved()
    {
        var presence = FakePresenceCheck.Approved("ok");
        using var harness = new SetupHarness(presence.Presence);
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        CommandOutcome outcome = harness.Init();

        outcome.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData(ApprovalReason.Denied)]
    [InlineData(ApprovalReason.Unavailable)]
    [InlineData(ApprovalReason.Cancelled)]
    [InlineData(ApprovalReason.Error)]
    public void Remove_DeniesAndReturnsEarly_OnEveryNonApprovedReason(ApprovalReason reason)
    {
        string detail = $"remove-detail-{reason}";
        var presence = FakePresenceCheck.Returning(reason, detail);
        using var harness = new SetupHarness(presence.Presence);

        CommandOutcome outcome = harness.Remove();

        outcome.Success.Should().BeFalse();
        outcome.Messages.Should().ContainSingle().Which.Should().Be(detail);
    }

    [Fact]
    public void Remove_Proceeds_WhenApproved()
    {
        var presence = FakePresenceCheck.Approved("ok");
        using var harness = new SetupHarness(presence.Presence);
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();

        CommandOutcome outcome = harness.Remove();

        outcome.Success.Should().BeTrue();
    }

    [Fact]
    public void GatedCommandsConsultPresence_ButDoctorDoesNot()
    {
        var presence = FakePresenceCheck.Approved("ok");
        using var harness = new SetupHarness(presence.Presence);

        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        presence.CallCount.Should().Be(1, "install is gated and must consult presence");

        harness.Doctor(fix: false);

        presence.CallCount.Should().Be(1, "doctor is ungated and must make no presence call");
    }
}
