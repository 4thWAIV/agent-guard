// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// A host-agnostic tool call, normalized from the raw hook payload before any Guard sees it.
/// </summary>
/// <param name="Id">The per-call correlation id that ties a Pre invocation to its Post.</param>
/// <param name="ToolName">The host's name for the tool being invoked.</param>
/// <param name="Input">The typed input for a kind the guard acts on, or <see langword="null"/> for a tool it does not model.</param>
public sealed record ToolCall(ToolCallId Id, string ToolName, ToolInput? Input);
