// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Linq;
using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class DoctorCommandTests
{
    [Fact]
    public void Acceptance_3h_HealthyMachineOutsideRepo_AllHealthyProjectSkippedNoChange()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        DoctorOutcome outcome = harness.Doctor(fix: false);

        outcome.Healthy.Should().BeTrue();
        outcome.ExitCode.Should().Be(0);
        outcome.Reports.Should().OnlyContain(report => report.Scope == "Machine");
        Directory.Exists(harness.ProjectAgentGuard).Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3h_HealthyInitializedRepo_AllHealthy()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();

        DoctorOutcome outcome = harness.Doctor(fix: false);

        outcome.Healthy.Should().BeTrue();
        outcome.Reports.Should().Contain(report => report.Scope == "Project");
    }

    [Fact]
    public void Acceptance_3h_MisWiredHook_IsReportedBrokenWithNonZeroExit()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();
        harness.MakeGuardEntryStale("PreToolUse", "Bash");

        DoctorOutcome outcome = harness.Doctor(fix: false);

        outcome.Healthy.Should().BeFalse();
        outcome.ExitCode.Should().Be(1);
        StatusOf(outcome, "Claude hook entries").Should().Be("Broken");
    }

    [Fact]
    public void Acceptance_3h_VersionDrift_IsReportedBroken()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();
        File.WriteAllText(harness.ProjectStateFile, "{\"guardVersion\":\"9.9.9\"}");

        DoctorOutcome outcome = harness.Doctor(fix: false);

        outcome.Healthy.Should().BeFalse();
        StatusOf(outcome, "project version stamp").Should().Be("Broken");
    }

    [Fact]
    public void Acceptance_3h_HashMismatch_IsReportedBroken()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        File.WriteAllText(harness.MachineStateFile, "{\"version\":\"0.1.0-alpha\",\"sha256\":\"deadbeef\"}");

        DoctorOutcome outcome = harness.Doctor(fix: false);

        outcome.Healthy.Should().BeFalse();
        StatusOf(outcome, "binary hash").Should().Be("Broken");
    }

    [Fact]
    public void Acceptance_3h_UnreadableRecord_IsReportedCannotVerify()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        File.WriteAllText(harness.MachineStateFile, "{ this is not json");

        DoctorOutcome outcome = harness.Doctor(fix: false);

        outcome.Healthy.Should().BeFalse();
        outcome.ExitCode.Should().Be(1);
        outcome.Reports.Should().Contain(report => report.Status == "CannotVerify");
    }

    [Fact]
    public void Acceptance_3i_DoctorFix_RepairsStaleHookMissingGitignoreAndMissingConfig()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.Init().Success.Should().BeTrue();
        harness.MakeGuardEntryStale("PreToolUse", "Bash");
        File.WriteAllText(harness.Gitignore, "# nothing here\n");
        File.Delete(harness.ProjectConfig);

        harness.Doctor(fix: true);

        DoctorOutcome after = harness.Doctor(fix: false);
        StatusOf(after, "Claude hook entries").Should().Be("Ok");
        StatusOf(after, "gitignore runtime-store lines").Should().Be("Ok");
        StatusOf(after, "project config.json").Should().Be("Ok");
    }

    [Fact]
    public void Acceptance_3i_DoctorFix_HashMismatchIsReportedNotRegenerated()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        string recordedBefore = File.ReadAllText(harness.MachineStateFile);
        File.WriteAllText(harness.VersionBinary("0.1.0-alpha"), "TAMPERED BYTES");

        harness.Doctor(fix: true);

        StatusOf(harness.Doctor(fix: false), "binary hash").Should().Be("Broken");
        File.ReadAllText(harness.MachineStateFile).Should().Be(recordedBefore);
    }

    [Fact]
    public void Acceptance_3k_DoctorFix_OutsideRepo_TouchesOnlyTheMachine()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        File.WriteAllText(harness.ShellProfilePath, string.Empty);

        harness.Doctor(fix: true);

        File.ReadAllText(harness.ShellProfilePath).Should().Contain(".agentguard/bin");
        Directory.EnumerateFileSystemEntries(harness.Project).Should().BeEmpty();
    }

    private static string StatusOf(DoctorOutcome outcome, string conditionName) =>
        outcome.Reports.First(report => string.Equals(report.Name, conditionName, System.StringComparison.Ordinal)).Status;
}
