// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The payload could not be parsed. The Engine must fail closed and deny.
/// </summary>
/// <param name="Reason">Why the payload could not be parsed.</param>
public sealed record HostReadUnparsable(string Reason) : HostReadResult;
