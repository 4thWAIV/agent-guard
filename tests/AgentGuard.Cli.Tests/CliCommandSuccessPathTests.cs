// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using FluentAssertions;

namespace AgentGuard.Cli.Tests;

/// <summary>
/// Drives the CLI's command <em>success</em> paths through the real entry point end to end: a real
/// <c>guard install</c> that registers the machine, then <c>guard init</c> against that installed machine, then
/// <c>guard doctor</c> reporting the installed-and-wired machine healthy. These complete the wiring the
/// early-exit tests in <see cref="ProgramCliTests"/> never reach — where each command only proves its
/// "nothing installed yet" refusal — so the install-registration path and the init/doctor success handlers are
/// exercised, and each assertion checks real behavior (the files written, the exit code, the output) rather than
/// merely that a line executed.
/// </summary>
public sealed class CliCommandSuccessPathTests
{
    [PosixOnlyFact]
    public void Install_ThroughTheCli_RegistersTheMachine()
    {
        using var machine = new CliMachine();

        CliResult result = machine.Run("install");

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("recorded install");
        File.Exists(machine.MachineStateFile).Should().BeTrue();
        File.Exists(machine.BinGuard).Should().BeTrue();
        File.ReadAllText(machine.MachineStateFile).Should().Contain("sha256");
    }

    [PosixOnlyFact]
    public void Init_ThroughTheCli_AgainstAnInstalledMachine_WiresTheProject()
    {
        using var machine = new CliMachine();
        machine.Run("install").ExitCode.Should().Be(0);

        CliResult result = machine.Run("init");

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("initialized the project");
        File.Exists(machine.ClaudeSettings).Should().BeTrue();

        // The wired PreToolUse hook is a call to the installed launcher — the absolute bin/guard path this machine
        // registered — proving init wove the machine install into the project, not a placeholder.
        File.ReadAllText(machine.ClaudeSettings).Should().Contain(machine.BinGuard);
        File.Exists(machine.ProjectConfig).Should().BeTrue();
        File.ReadAllText(machine.ProjectConfig).Should().Contain("protectedPaths");
        Directory.Exists(machine.ProjectGrants).Should().BeTrue();
    }

    [PosixOnlyFact]
    public void Doctor_ThroughTheCli_OnAnInstalledAndWiredMachine_ReportsHealthy()
    {
        using var machine = new CliMachine();
        machine.Run("install").ExitCode.Should().Be(0);
        machine.Run("init").ExitCode.Should().Be(0);

        CliResult result = machine.Run("doctor");

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("doctor: healthy");
    }
}
