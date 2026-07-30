// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Text.Json.Nodes;
using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class RemoveCommandTests
{
    private const string SettingsWithUserHook = """
        {
          "permissions": { "allow": ["Read"] },
          "hooks": {
            "PreToolUse": [
              { "matcher": "Bash", "hooks": [ { "type": "command", "command": "echo user-hook" } ] }
            ]
          }
        }
        """;

    [Fact]
    public void Acceptance_3j_Remove_DeletesGuardEntriesLeavesNonGuardIntactAndValidJson()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.WriteSettings(SettingsWithUserHook);
        harness.Init().Success.Should().BeTrue();
        SettingsProbe.HasAnySentinel(harness.ReadSettings()).Should().BeTrue();

        harness.Remove().Success.Should().BeTrue();

        JsonObject settings = harness.ReadSettings();
        SettingsProbe.HasAnySentinel(settings).Should().BeFalse();
        settings["permissions"]!["allow"]![0]!.GetValue<string>().Should().Be("Read");
        settings.ToJsonString().Should().Contain("echo user-hook");
        Directory.Exists(harness.ProjectAgentGuard).Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3j_Remove_IsIdempotent()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();

        harness.Remove().Success.Should().BeTrue();
        harness.Remove().Success.Should().BeTrue();

        SettingsProbe.HasAnySentinel(harness.ReadSettings()).Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3k_Remove_TouchesOnlyTheRepoNotTheMachine()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();
        var machineStamp = File.GetLastWriteTimeUtc(harness.MachineStateFile);

        harness.Remove().Success.Should().BeTrue();

        File.GetLastWriteTimeUtc(harness.MachineStateFile).Should().Be(machineStamp);
        File.Exists(harness.BinGuard).Should().BeTrue();
    }
}
