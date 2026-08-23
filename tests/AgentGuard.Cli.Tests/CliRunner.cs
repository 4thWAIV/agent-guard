// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Cli.Tests;

/// <summary>
/// Runs the CLI against a throwaway isolated machine and project — one fresh pair per call — for the tests that
/// only need a single command against an uninstalled machine. It drives the internal
/// <c>Program.Run(ISystemServices)</c> seam through a <see cref="CliMachine"/>, so every command runs the
/// production wiring with an injected environment and console and touches nothing on the developer's real machine.
/// Tests that drive several commands against one installed machine own a <see cref="CliMachine"/> directly.
/// </summary>
internal static class CliRunner
{
    /// <summary>
    /// Invokes the CLI with the given arguments and empty standard input against a throwaway isolated machine.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public static CliResult Run(params string[] args) => RunWithInput(string.Empty, args);

    /// <summary>
    /// Invokes the CLI with the given standard input and arguments against a throwaway isolated machine.
    /// </summary>
    /// <param name="standardInput">The text presented to the CLI on standard input.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public static CliResult RunWithInput(string standardInput, params string[] args)
    {
        using var machine = new CliMachine();
        return machine.RunWithInput(standardInput, args);
    }
}
