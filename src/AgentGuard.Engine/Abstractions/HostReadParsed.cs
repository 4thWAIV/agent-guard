// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// The payload parsed cleanly into a normalized call.
/// </summary>
/// <param name="Call">The normalized call.</param>
public sealed record HostReadParsed(NormalizedCall Call) : HostReadResult;
