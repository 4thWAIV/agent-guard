// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// No Context record of the requested kind exists for the call. For the snapshot on a drifted protected file
/// this is a fail-closed condition, not an allow.
/// </summary>
/// <param name="Kind">The kind of record that was requested and not found.</param>
public sealed record ContextMissing(string Kind) : ContextRead;
