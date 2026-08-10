// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Tests;

/// <summary>
/// A Context store whose orphan sweep throws a non-IO exception, used to prove the Engine runs the best-effort
/// sweep inside the Pre fail-closed boundary: an unexpected sweep failure must become a deny, never escape the
/// pipeline. The remaining members are never reached on that path and throw if they are.
/// </summary>
internal sealed class ThrowingSweepStore : IContextStore
{
    private ThrowingSweepStore()
    {
    }

    /// <inheritdoc />
    public Task WriteAsync(ContextKey key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
        throw new NotSupportedException("unreachable: the sweep fails before any record is written");

    /// <inheritdoc />
    public Task<ContextRead> ReadAsync(ContextKey key, CancellationToken cancellationToken) =>
        throw new NotSupportedException("unreachable: the sweep fails before any record is read");

    /// <inheritdoc />
    public Task DeleteAsync(ToolCallId toolCall, CancellationToken cancellationToken) =>
        throw new NotSupportedException("unreachable: the sweep fails before any record is deleted");

    /// <inheritdoc />
    public Task SweepExpiredAsync(TimeSpan timeToLive, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("injected non-IO sweep failure");

    /// <summary>
    /// Creates the throwing-sweep store.
    /// </summary>
    /// <returns>The store, as its interface.</returns>
    internal static IContextStore Create() => new ThrowingSweepStore();
}
