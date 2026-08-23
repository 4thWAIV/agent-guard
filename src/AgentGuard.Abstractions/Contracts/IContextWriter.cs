// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The write-only Context handle handed to Capture, pre-scoped by the Engine to the current call and Guard.
/// Its existence is what lets Capture, and only Capture, produce Context records.
/// </summary>
public interface IContextWriter
{
    /// <summary>
    /// Writes a record of the given kind for the current call and Guard.
    /// </summary>
    /// <param name="kind">The Guard-defined label for the kind of data (for the File Guard, the snapshot).</param>
    /// <param name="data">The bytes to store.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the record is written.</returns>
    Task WriteAsync(string kind, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
}
