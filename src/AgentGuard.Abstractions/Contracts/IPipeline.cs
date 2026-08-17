// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// Runs the registered Guards for a single hook event and aggregates their judgments into one decision.
/// </summary>
public interface IPipeline
{
    /// <summary>
    /// Runs every registered Guard for the given event in order and aggregates their verdicts, where any single
    /// deny blocks the call. For a Post event, the Engine executes any effects the Guards request before this
    /// returns.
    /// </summary>
    /// <param name="hookEvent">The lifecycle event being processed.</param>
    /// <param name="toolCall">The normalized tool call.</param>
    /// <param name="environment">The environment the call runs in.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the aggregate verdict for the call.</returns>
    Task<Verdict> RunAsync(HookEvent hookEvent, ToolCall toolCall, CallEnvironment environment, CancellationToken cancellationToken);
}
