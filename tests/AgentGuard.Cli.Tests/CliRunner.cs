// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Reflection;
using AgentGuard.TestSupport;

namespace AgentGuard.Cli.Tests;

/// <summary>
/// Runs the real CLI entry point (the compiled <c>guard</c> assembly) in-process against a throwaway HOME and
/// working directory, capturing its exit code and console output. Redirecting HOME (POSIX) isolates the machine
/// install the setup commands read, and redirecting the working directory isolates the project wiring, so each
/// command runs its production wiring without touching the developer's real machine. On Windows the guard resolves
/// home from the registry profile, which HOME does not redirect, so the tests that depend on an isolated machine
/// install are <c>[PosixOnlyFact]</c> (issue #23).
/// </summary>
internal static class CliRunner
{
    private static readonly Assembly CliAssembly = Assembly.Load("guard");

    /// <summary>
    /// Invokes the CLI with the given arguments and empty standard input, isolating HOME and the working directory.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public static CliResult Run(params string[] args) => RunWithInput(string.Empty, args);

    /// <summary>
    /// Invokes the CLI with the given standard input and arguments, isolating HOME and the working directory.
    /// </summary>
    /// <param name="standardInput">The text presented to the process on standard input.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public static CliResult RunWithInput(string standardInput, params string[] args)
    {
        string home = Directory.CreateTempSubdirectory("agentguard-cli-home-").FullName;
        string project = Directory.CreateTempSubdirectory("agentguard-cli-proj-").FullName;
        try
        {
            return RunIn(home, project, standardInput, args);
        }
        finally
        {
            TestTempDirectory.DeleteBestEffort(home);
            TestTempDirectory.DeleteBestEffort(project);
        }
    }

    /// <summary>
    /// Invokes the CLI against a caller-owned HOME and working directory, isolating HOME (POSIX), the working
    /// directory, and the console streams for the call. The caller owns the two directories' lifetime, so a
    /// multi-command session (install, then init, then a hook) can run against one installed machine and inspect
    /// the files each command wrote.
    /// </summary>
    /// <param name="home">The HOME directory the machine install reads and writes under.</param>
    /// <param name="project">The working directory the project wiring is written under.</param>
    /// <param name="standardInput">The text presented to the process on standard input.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The captured exit code and console output.</returns>
    public static CliResult RunIn(string home, string project, string standardInput, params string[] args)
    {
        string? originalHome = Environment.GetEnvironmentVariable("HOME");
        string originalDirectory = Directory.GetCurrentDirectory();
        TextReader originalIn = Console.In;
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        using var output = new StringWriter();
        using var error = new StringWriter();
        try
        {
            Environment.SetEnvironmentVariable("HOME", home);
            Directory.SetCurrentDirectory(project);
            Console.SetIn(new StringReader(standardInput));
            Console.SetOut(output);
            Console.SetError(error);

            int exitCode = Invoke(args);
            return new CliResult(exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalDirectory);
            Environment.SetEnvironmentVariable("HOME", originalHome);
        }
    }

    private static int Invoke(string[] args)
    {
        MethodInfo entryPoint = CliAssembly.EntryPoint
            ?? throw new InvalidOperationException("The CLI assembly exposes no entry point.");

        // The compiler synthesizes a synchronous entry point around `async Task<int> Main` that returns the exit
        // code; a `Task`/void entry point would return null, which maps to a zero exit code.
        object? result = entryPoint.Invoke(null, new object[] { args });
        return result is int code ? code : 0;
    }
}
