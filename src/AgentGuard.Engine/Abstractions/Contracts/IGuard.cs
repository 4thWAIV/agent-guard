// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions.Contracts;

/// <summary>
/// A pipeline mechanism with three behaviors. The behaviors are kept apart by their signatures: only
/// <see cref="CaptureAsync"/> is handed a writer, so a Precheck cannot write the Context, and a Postcheck can
/// only read it.
/// </summary>
public interface IGuard
{
    /// <summary>
    /// Gets the stable name of this Guard, used to key its Context records and to identify it in diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The Pre-phase verify: judge the call from read-only inputs, before it runs.
    /// </summary>
    /// <param name="toolCall">The normalized tool call.</param>
    /// <param name="environment">The environment the call runs in.</param>
    /// <param name="store">A read-only view of the Context store's state for the in-flight call.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to this Guard's verdict.</returns>
    Task<Verdict> PrecheckAsync(ToolCall toolCall, CallEnvironment environment, IContextStoreInspector store, CancellationToken cancellationToken);

    /// <summary>
    /// The Pre-phase prepare: write into the Context whatever the Post phase will need. Runs only for a call
    /// that is not being denied. This is the only behavior handed a writable Context handle.
    /// </summary>
    /// <param name="toolCall">The normalized tool call.</param>
    /// <param name="environment">The environment the call runs in.</param>
    /// <param name="context">The write-only handle scoped to this call and Guard.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to whether the pre-image was captured; a failure must make the Engine deny the call.</returns>
    Task<CaptureResult> CaptureAsync(ToolCall toolCall, CallEnvironment environment, IContextWriter context, CancellationToken cancellationToken);

    /// <summary>
    /// The Post-phase verify: judge what actually landed, reading back what Capture stored, and return any
    /// effects the Engine must execute.
    /// </summary>
    /// <param name="toolCall">The normalized tool call.</param>
    /// <param name="environment">The environment the call runs in.</param>
    /// <param name="context">The read-only handle scoped to this call and Guard.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to this Guard's verdict and requested effects.</returns>
    Task<PostcheckResult> PostcheckAsync(ToolCall toolCall, CallEnvironment environment, IContextReader context, CancellationToken cancellationToken);
}
