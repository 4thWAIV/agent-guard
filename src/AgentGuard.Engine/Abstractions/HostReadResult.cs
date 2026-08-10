// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// The result of a host adapter reading a raw payload. It is a closed set of exactly two cases — parsed or
/// unparsable — so deny-on-unparsable cannot be forgotten: the Engine must handle both, rather than relying on
/// a thrown exception it might not catch.
/// </summary>
public abstract record HostReadResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HostReadResult"/> class. Declared <c>private protected</c>
    /// so the set of cases stays closed to this assembly.
    /// </summary>
    private protected HostReadResult()
    {
    }
}
