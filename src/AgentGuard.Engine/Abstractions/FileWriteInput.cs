// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// The input of a tool that writes files: every path it would write. The paths are extracted once, in the host
/// adapter, so no Guard re-derives them or handles only the first target.
/// </summary>
/// <param name="Paths">Every absolute path the tool would write.</param>
public sealed record FileWriteInput(IReadOnlyList<string> Paths) : ToolInput;
