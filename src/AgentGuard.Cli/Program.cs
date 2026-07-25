// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Engine;

namespace AgentGuard.Cli;

/// <summary>
/// Hosts the entry point for the AgentGuard command-line interface.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs the AgentGuard command-line interface.
    /// </summary>
    /// <param name="args">The command-line arguments supplied to the process.</param>
    /// <returns>The process exit code returned to the operating system.</returns>
    private static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        Console.Out.WriteLine(EngineInfo.Name);
        return args.Length;
    }
}
