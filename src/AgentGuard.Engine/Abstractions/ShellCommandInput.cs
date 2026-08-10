// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// The input of a shell tool: the command line it would run.
/// </summary>
/// <param name="Command">The shell command line.</param>
public sealed record ShellCommandInput(string Command) : ToolInput;
