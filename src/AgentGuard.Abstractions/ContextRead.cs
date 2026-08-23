// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The result of reading a Context record. It is a closed set of exactly two cases — found or missing — so a
/// missing record cannot be silently coalesced into "no drift, allow." A caller must handle the missing case
/// explicitly, and for the snapshot that means failing closed.
/// </summary>
public abstract record ContextRead
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextRead"/> class. Declared <c>private protected</c> so
    /// the set of cases stays closed to this assembly.
    /// </summary>
    private protected ContextRead()
    {
    }
}
