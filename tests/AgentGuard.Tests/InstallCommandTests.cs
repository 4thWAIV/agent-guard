// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class InstallCommandTests
{
    [Fact]
    public void Acceptance_3a_InstallIntoCleanHome_BuildsLayoutAndRecordsHash()
    {
        using var harness = new SetupHarness();

        CommandOutcome outcome = harness.Install("0.1.0-alpha");

        outcome.Success.Should().BeTrue();
        File.Exists(harness.VersionBinary("0.1.0-alpha")).Should().BeTrue();
        File.Exists(harness.CurrentBinary).Should().BeTrue();
        File.Exists(harness.BinGuard).Should().BeTrue();

        InstallState state = ReadInstallState(harness);
        state.Version.Should().Be("0.1.0-alpha");
        state.Sha256.Should().Be(HashOf(harness.VersionBinary("0.1.0-alpha")));
        File.ReadAllText(harness.ShellProfilePath).Should().Contain(".agentguard/bin");
    }

    [Fact]
    public void Acceptance_3b_NewerInstall_FlipsCurrentWithoutBreakingPriorVersion()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0").Success.Should().BeTrue();

        harness.Install("0.2.0").Success.Should().BeTrue();

        File.Exists(harness.VersionBinary("0.1.0")).Should().BeTrue();
        File.ReadAllText(harness.VersionBinary("0.1.0")).Should().Be("GUARD BINARY v0.1.0");
        File.ReadAllText(harness.CurrentBinary).Should().Be("GUARD BINARY v0.2.0");
    }

    [Fact]
    public void Acceptance_3b_SameVersionReinstall_IsNoOpAndDoesNotOverwriteRunningImage()
    {
        using var harness = new SetupHarness();
        harness.Install("0.2.0").Success.Should().BeTrue();
        string installed = harness.VersionBinary("0.2.0");
        var writtenAt = File.GetLastWriteTimeUtc(installed);

        // Re-run installing the already-installed binary itself: it must be skipped, never overwritten.
        CommandOutcome outcome = SetupCommands.Install(harness.Context("0.2.0", installed), allowDowngrade: false);

        outcome.Success.Should().BeTrue();
        File.GetLastWriteTimeUtc(installed).Should().Be(writtenAt);
        File.ReadAllText(installed).Should().Be("GUARD BINARY v0.2.0");
    }

    [Fact]
    public void Acceptance_3b_OlderInstall_IsRefusedUnlessDowngradeAllowed()
    {
        using var harness = new SetupHarness();
        harness.Install("0.2.0").Success.Should().BeTrue();

        CommandOutcome refused = harness.Install("0.1.5");

        refused.Success.Should().BeFalse();
        File.Exists(harness.VersionBinary("0.1.5")).Should().BeFalse();
        File.ReadAllText(harness.CurrentBinary).Should().Be("GUARD BINARY v0.2.0");

        CommandOutcome allowed = harness.Install("0.1.5", allowDowngrade: true);

        allowed.Success.Should().BeTrue();
        File.Exists(harness.VersionBinary("0.1.5")).Should().BeTrue();
        File.ReadAllText(harness.CurrentBinary).Should().Be("GUARD BINARY v0.1.5");
    }

    [Fact]
    public void Acceptance_3k_Install_TouchesOnlyAgentGuardAndTheProfileLine()
    {
        using var harness = new SetupHarness();

        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        Directory.EnumerateFileSystemEntries(harness.Project).Should().BeEmpty();
        Directory.EnumerateFileSystemEntries(harness.Home)
            .Select(Path.GetFileName)
            .Should()
            .BeEquivalentTo(".agentguard", ".zshrc");
    }

    private static InstallState ReadInstallState(SetupHarness harness) =>
        SetupJson.DeserializeInstallState(File.ReadAllText(harness.MachineStateFile))!;

    private static string HashOf(string path) =>
        System.Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
