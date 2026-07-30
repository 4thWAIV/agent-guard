// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The write-only Context handle handed to Capture, pre-scoped by the Engine to one call and Guard. Its
/// existence is what lets Capture, and only Capture, produce Context records.
/// </summary>
internal sealed class ContextWriter : IContextWriter
{
    private readonly IContextStore _store;
    private readonly ToolCallId _toolCall;
    private readonly string _guard;

    private ContextWriter(IContextStore store, ToolCallId toolCall, string guard)
    {
        _store = store;
        _toolCall = toolCall;
        _guard = guard;
    }

    /// <inheritdoc />
    public Task WriteAsync(string kind, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        return _store.WriteAsync(new ContextKey(_toolCall, _guard, kind), data, cancellationToken);
    }

    /// <summary>
    /// Creates a writer scoped to one call and Guard.
    /// </summary>
    /// <param name="store">The Engine-owned store the handle writes through.</param>
    /// <param name="toolCall">The call the records belong to.</param>
    /// <param name="guard">The owning Guard's name.</param>
    /// <returns>The writer, as its interface.</returns>
    internal static IContextWriter Create(IContextStore store, ToolCallId toolCall, string guard)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrEmpty(guard);
        return new ContextWriter(store, toolCall, guard);
    }
}
