// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The host-specific rendering of an aggregate verdict: the process exit code the host reads, plus any message
/// the host surfaces. Produced by the host adapter so the exit-code protocol, which differs between Claude Code
/// and Codex, lives in the same one place that parses their payloads.
/// </summary>
/// <param name="ExitCode">The process exit code to return to the host.</param>
/// <param name="Message">A message for the host to surface, when one applies.</param>
public sealed record HostDecision(int ExitCode, string? Message);
