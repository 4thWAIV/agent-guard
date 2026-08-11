// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Cli.Tests;

/// <summary>
/// The captured result of a CLI invocation: the process exit code and the text written to standard output and
/// standard error.
/// </summary>
/// <param name="ExitCode">The process exit code the CLI returned.</param>
/// <param name="StandardOutput">The text written to standard output.</param>
/// <param name="StandardError">The text written to standard error.</param>
internal sealed record CliResult(int ExitCode, string StandardOutput, string StandardError);
