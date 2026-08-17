// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The one seam through which a GUID enters the codebase, so the single randomness call is owned and tests are
/// deterministic.
/// </summary>
public interface IGuidFactory
{
    /// <summary>
    /// Creates a new GUID.
    /// </summary>
    /// <returns>A new GUID.</returns>
    Guid NewGuid();
}
