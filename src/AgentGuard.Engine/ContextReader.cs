// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The read-only Context handle handed to Postcheck, pre-scoped by the Engine to one call and Guard. It cannot
/// write, so Postcheck can never alter what Capture stored.
/// </summary>
internal sealed class ContextReader : IContextReader
{
    private readonly IContextStore _store;
    private readonly ToolCallId _toolCall;
    private readonly string _guard;

    private ContextReader(IContextStore store, ToolCallId toolCall, string guard)
    {
        _store = store;
        _toolCall = toolCall;
        _guard = guard;
    }

    /// <inheritdoc />
    public Task<ContextRead> ReadAsync(string kind, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        return _store.ReadAsync(new ContextKey(_toolCall, _guard, kind), cancellationToken);
    }

    /// <summary>
    /// Creates a reader scoped to one call and Guard.
    /// </summary>
    /// <param name="store">The Engine-owned store the handle reads from.</param>
    /// <param name="toolCall">The call the records belong to.</param>
    /// <param name="guard">The owning Guard's name.</param>
    /// <returns>The reader, as its interface.</returns>
    internal static IContextReader Create(IContextStore store, ToolCallId toolCall, string guard)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrEmpty(guard);
        return new ContextReader(store, toolCall, guard);
    }
}
