// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The result of normalizing a raw host payload: the tool call paired with its surrounding environment.
/// </summary>
/// <param name="Call">The normalized tool call.</param>
/// <param name="Environment">The environment the call runs in.</param>
public sealed record NormalizedCall(ToolCall Call, CallEnvironment Environment);
