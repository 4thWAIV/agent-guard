// Copyright (c) 4thWAIV. All rights reserved.

using FluentAssertions;
using Xunit;

namespace AgentGuard.Cli.Tests;

/// <summary>
/// Drives the CLI entry point through every command handler in <c>Program.cs</c>. Each test invokes the real
/// process entry point against an isolated HOME and working directory, so the production command wiring runs end
/// to end without a real install and without touching the developer's machine.
/// </summary>
public sealed class ProgramCliTests
{
    [Fact]
    public void Version_PrintsTheThreeBuildVersions()
    {
        CliResult result = CliRunner.Run("version");

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("SemVer:");
        result.StandardOutput.Should().Contain("AssemblyVersion:");
        result.StandardOutput.Should().Contain("FileVersion:");
    }

    [Fact]
    public void HookPre_WithoutAnInstall_FailsClosedWithExitTwo()
    {
        CliResult result = CliRunner.RunWithInput("{}", "hook", "pre");

        // No machine install is present under the isolated HOME, so the integrity self-check denies before the
        // pipeline runs and the hook fails closed (exit 2).
        result.ExitCode.Should().Be(2);
    }

    [Fact]
    public void HookPost_WithoutAnInstall_FailsClosedWithExitTwo()
    {
        CliResult result = CliRunner.RunWithInput("{}", "hook", "post");

        result.ExitCode.Should().Be(2);
    }

    [Fact]
    public void Hook_WithAnUnknownEvent_PrintsUsageAndFailsClosed()
    {
        CliResult result = CliRunner.Run("hook", "bogus");

        result.ExitCode.Should().Be(2);
        result.StandardError.Should().Contain("Usage: guard hook <pre|post>");
    }

    [Fact]
    public void Hook_WithNoEvent_PrintsUsageAndFailsClosed()
    {
        CliResult result = CliRunner.Run("hook");

        result.ExitCode.Should().Be(2);
        result.StandardError.Should().Contain("Usage: guard hook <pre|post>");
    }

    [PosixOnlyFact]
    public void Init_WhenTheMachineIsNotInstalled_FailsWithGuidance()
    {
        CliResult result = CliRunner.Run("init");

        result.ExitCode.Should().Be(1);
        result.StandardOutput.Should().Contain("the machine is not installed");
    }

    [Fact]
    public void WhenTheProjectIsUnwired_RemoveSucceedsIdempotently()
    {
        CliResult result = CliRunner.Run("remove");

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("removed .agentguard/");
    }

    [PosixOnlyFact]
    public void Doctor_OnAnUninstalledMachine_ReportsNotHealthy()
    {
        CliResult result = CliRunner.Run("doctor");

        result.ExitCode.Should().Be(1);
        result.StandardOutput.Should().Contain("doctor: not healthy");
    }
}
