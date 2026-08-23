// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The per-call store the Engine owns, persisted between the separate Pre and Post processes and addressed by
/// a <see cref="ContextKey"/>. It is never handed to Guards — they receive only the scoped writer, reader, and
/// inspector — which is what actually enforces the capability split.
/// </summary>
public interface IContextStore
{
    /// <summary>
    /// Writes a record under the given key, replacing any existing record with that key.
    /// </summary>
    /// <param name="key">The triple that addresses the record.</param>
    /// <param name="data">The bytes to store.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the record is written.</returns>
    Task WriteAsync(ContextKey key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the record stored under the given key.
    /// </summary>
    /// <param name="key">The triple that addresses the record.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the stored bytes, or a missing result when no record exists.</returns>
    Task<ContextRead> ReadAsync(ContextKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes every record belonging to the given call, sweeping the call's Context once Post is done.
    /// </summary>
    /// <param name="toolCall">The call whose records are removed.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the records are deleted.</returns>
    Task DeleteAsync(ToolCallId toolCall, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes orphaned records older than the given time-to-live, bounding the store if a Post phase never ran.
    /// </summary>
    /// <param name="timeToLive">The maximum age a record may reach before it is swept.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the sweep is done.</returns>
    Task SweepExpiredAsync(TimeSpan timeToLive, CancellationToken cancellationToken);
}
