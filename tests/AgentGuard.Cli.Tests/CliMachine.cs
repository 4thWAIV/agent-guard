// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using AgentGuard.TestSupport;

namespace AgentGuard.Cli.Tests;

/// <summary>
/// An inspectable, isolated machine and project the CLI runs against across several commands. It owns a persistent
/// HOME and working directory (unlike <see cref="CliRunner.Run(string[])"/>, which uses a throwaway pair per call),
/// so a test can complete a real <c>guard install</c>, then <c>guard init</c> against that installed machine, and
/// assert the files each command actually wrote. Every command runs the production wiring through the real CLI
/// entry point (<see cref="CliRunner.RunIn(string, string, string, string[])"/>) with HOME and the working
/// directory redirected here, so nothing touches the developer's real machine. The HOME redirect isolates on POSIX
/// only; on Windows the guard resolves home from the registry profile, so the tests that drive a full install here
/// are <c>[PosixOnlyFact]</c> (issue #23).
/// </summary>
internal sealed class CliMachine : IDisposable
{
    private readonly AgentGuardLayout _layout;

    public CliMachine()
    {
        Home = Directory.CreateTempSubdirectory("agentguard-clisession-home-").FullName;
        Project = Directory.CreateTempSubdirectory("agentguard-clisession-proj-").FullName;
        _layout = new AgentGuardLayout(Home, Project);
    }

    /// <summary>Gets the isolated HOME directory the machine install lives under.</summary>
    public string Home { get; }

    /// <summary>Gets the isolated working directory the project wiring is written under.</summary>
    public string Project { get; }

    /// <summary>Gets the machine install root, <c>{Home}/.agentguard</c>.</summary>
    public string AgentGuardRoot => _layout.AgentGuardRoot;

    /// <summary>Gets the machine state record, <c>{Home}/.agentguard/state.json</c>.</summary>
    public string MachineStateFile => _layout.MachineStateFile;

    /// <summary>Gets the stable launcher, <c>{Home}/.agentguard/bin/guard</c>.</summary>
    public string BinGuard => _layout.BinGuard;

    /// <summary>Gets the project's <c>.claude/settings.json</c> path.</summary>
    public string ClaudeSettings => _layout.ClaudeSettings;

    /// <summary>Gets the project's guard directory, <c>{Project}/.agentguard</c>.</summary>
    public string ProjectAgentGuard => _layout.ProjectAgentGuard;

    /// <summary>Gets the project's config path, <c>{Project}/.agentguard/config.json</c>.</summary>
    public string ProjectConfig => _layout.ProjectConfig;

    /// <summary>Gets the project's grants directory, <c>{Project}/.agentguard/grants</c>.</summary>
    public string ProjectGrants => _layout.ProjectGrants;

    /// <summary>
    /// Runs the CLI with the given arguments and empty standard input against this machine and project.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public CliResult Run(params string[] args) => CliRunner.RunIn(Home, Project, string.Empty, args);

    /// <summary>
    /// Runs the CLI with the given standard input and arguments against this machine and project.
    /// </summary>
    /// <param name="standardInput">The text presented to the process on standard input.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public CliResult RunWithInput(string standardInput, params string[] args) =>
        CliRunner.RunIn(Home, Project, standardInput, args);

    /// <inheritdoc />
    public void Dispose()
    {
        TestTempDirectory.DeleteBestEffort(Home);
        TestTempDirectory.DeleteBestEffort(Project);
    }
}
