// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The result of a composed hook run: the integrity gate followed by the File Guard pipeline. The
/// <see cref="ExitCode"/> is the host's blocking decision (2 denies) and <see cref="Message"/> is the line to write
/// to standard error.
/// </summary>
/// <param name="ExitCode">The host exit code; 2 denies.</param>
/// <param name="Message">The message to surface on standard error, or <see langword="null"/>.</param>
public sealed record HookExecution(int ExitCode, string? Message);
