// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Tests;

/// <summary>
/// A guard that throws a non-IO exception in every phase, used to prove the Engine maps any thrown exception to
/// a deny at both Pre and Post rather than letting it escape to the process boundary.
/// </summary>
internal sealed class ThrowingGuard : IGuard
{
    private ThrowingGuard()
    {
    }

    /// <inheritdoc />
    public string Name => "throwing-guard";

    /// <inheritdoc />
    public Task<Verdict> PrecheckAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        IContextStoreInspector store,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("injected pre-phase failure");

    /// <inheritdoc />
    public Task<CaptureResult> CaptureAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        IContextWriter context,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("injected capture failure");

    /// <inheritdoc />
    public Task<PostcheckResult> PostcheckAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        IContextReader context,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("injected post-phase failure");

    /// <summary>
    /// Creates the throwing guard.
    /// </summary>
    /// <returns>The guard, as its interface.</returns>
    internal static IGuard Create() => new ThrowingGuard();
}
