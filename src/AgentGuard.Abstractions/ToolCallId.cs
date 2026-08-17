// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The per-call correlation id (the host's <c>tool_use_id</c>) that ties a Pre invocation to its Post.
/// </summary>
/// <param name="Value">The raw identifier supplied by the host runtime.</param>
public readonly record struct ToolCallId(string Value);
