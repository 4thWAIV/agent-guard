// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The read-only Context handle handed to Postcheck, pre-scoped by the Engine to the current call and Guard.
/// It cannot write, so Postcheck can never alter what Capture stored.
/// </summary>
public interface IContextReader
{
    /// <summary>
    /// Reads back the record of the given kind that Capture stored for the current call and Guard.
    /// </summary>
    /// <param name="kind">The Guard-defined label for the kind of data to read.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the stored bytes, or a missing result the caller must handle by failing closed.</returns>
    Task<ContextRead> ReadAsync(string kind, CancellationToken cancellationToken);
}
