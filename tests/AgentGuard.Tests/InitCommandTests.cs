// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Text.Json.Nodes;
using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class InitCommandTests
{
    [Fact]
    public void Acceptance_3d_Init_WhenMachineNotInstalled_IsRefused()
    {
        using var harness = new SetupHarness();

        CommandOutcome outcome = harness.Init();

        outcome.Success.Should().BeFalse();
        harness.Directories.DirectoryExists(harness.ProjectAgentGuard).Should().BeFalse();
        harness.Files.Exists(harness.ClaudeSettings).Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3d_Init_WithNoSettings_CreatesAbsolutePathGuardHooks()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        harness.Init().Success.Should().BeTrue();

        JsonObject settings = harness.ReadSettings();
        AssertGuardCommand(settings, "PreToolUse", harness, "pre");
        AssertGuardCommand(settings, "PostToolUse", harness, "post");
    }

    [Fact]
    public void Acceptance_3d_Init_CreatesConfigGrantsAndVersionStamp()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        harness.Init().Success.Should().BeTrue();

        harness.Files.Exists(harness.ProjectConfig).Should().BeTrue();
        harness.Files.ReadAllText(harness.ProjectConfig).Should().Contain("protectedPaths");
        harness.Directories.DirectoryExists(Path.Combine(harness.ProjectAgentGuard, "grants")).Should().BeTrue();
        harness.Files.ReadAllText(harness.ProjectStateFile).Should().Contain("0.1.0-alpha");
    }

    [Fact]
    public void Acceptance_3e_Init_OverExistingSettings_PreservesNonGuardAndAddsGuardEntries()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.WriteSettings("""
            {
              "permissions": { "allow": ["Read"] },
              "sandbox": { "enabled": true },
              "hooks": {
                "PreToolUse": [
                  { "matcher": "Bash", "hooks": [ { "type": "command", "command": "echo user-hook" } ] }
                ]
              }
            }
            """);

        harness.Init().Success.Should().BeTrue();

        JsonObject settings = harness.ReadSettings();
        settings["permissions"]!["allow"]![0]!.GetValue<string>().Should().Be("Read");
        settings["sandbox"]!["enabled"]!.GetValue<bool>().Should().BeTrue();
        settings.ToJsonString().Should().Contain("echo user-hook");
        SettingsProbe.GuardGroup(settings, "PreToolUse").Should().NotBeNull();
        SettingsProbe.GuardGroupCount(settings, "PreToolUse").Should().Be(1);
    }

    [Fact]
    public void Acceptance_3e_SecondInit_AddsNoDuplicates()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();

        harness.Init().Success.Should().BeTrue();

        JsonObject settings = harness.ReadSettings();
        SettingsProbe.GuardGroupCount(settings, "PreToolUse").Should().Be(1);
        SettingsProbe.GuardGroupCount(settings, "PostToolUse").Should().Be(1);
    }

    [Fact]
    public void Acceptance_3e_StalePathGuardEntry_IsUpdated()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();
        harness.MakeGuardEntryStale("PreToolUse");

        harness.Init().Success.Should().BeTrue();

        JsonObject settings = harness.ReadSettings();
        SettingsProbe.CommandOf(SettingsProbe.GuardGroup(settings, "PreToolUse")!)
            .Should().Be(HookCommand.ForEvent(harness.BinGuard, HookCommand.PreEvent));
        SettingsProbe.GuardGroupCount(settings, "PreToolUse").Should().Be(1);
    }

    [Fact]
    public void Acceptance_3e_RealConflict_IsRefusedLeavingFileUntouched()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.WriteSettings("{ \"hooks\": \"not-an-object\" }");

        CommandOutcome outcome = harness.Init();

        outcome.Success.Should().BeFalse();
        harness.Files.ReadAllText(harness.ClaudeSettings).Should().Contain("not-an-object");
    }

    [Fact]
    public void Acceptance_3f_Gitignore_IsIdempotentAndNeverIgnoresConfigOrKey()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();

        string gitignore = harness.Files.ReadAllText(harness.Gitignore);
        CountLines(gitignore, ".protected-snapshots/").Should().Be(1);
        CountLines(gitignore, ".agentguard/grants/").Should().Be(1);
        gitignore.Should().NotContain("config.json");
        gitignore.Should().NotContain("grant-public-key");
    }

    [Fact]
    public void Acceptance_3k_Init_TouchesOnlyTheRepoNotTheMachine()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        var machineStamp = harness.Files.GetLastWriteTimeUtc(harness.MachineStateFile);

        harness.Init().Success.Should().BeTrue();

        harness.Files.GetLastWriteTimeUtc(harness.MachineStateFile).Should().Be(machineStamp);
        harness.Directories.DirectoryExists(harness.ProjectAgentGuard).Should().BeTrue();
        harness.Files.Exists(harness.ClaudeSettings).Should().BeTrue();
    }

    private static void AssertGuardCommand(JsonObject settings, string eventKey, SetupHarness harness, string token)
    {
        JsonObject? group = SettingsProbe.GuardGroup(settings, eventKey);
        group.Should().NotBeNull();
        group!["matcher"]!.GetValue<string>().Should().Be(ClaudeSettingsWiring.ToolMatcher);
        SettingsProbe.CommandOf(group)
            .Should().Be(HookCommand.ForEvent(harness.BinGuard, token));
    }

    private static int CountLines(string content, string line)
    {
        int count = 0;
        foreach (string candidate in content.Split('\n'))
        {
            if (string.Equals(candidate.Trim(), line, System.StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }
}
