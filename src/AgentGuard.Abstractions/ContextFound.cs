// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The requested Context record exists; its bytes are carried here.
/// </summary>
/// <param name="Data">The stored bytes.</param>
public sealed record ContextFound(ReadOnlyMemory<byte> Data) : ContextRead;
