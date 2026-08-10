// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.CrossPlatform;
using AgentGuard.Engine;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Setup;

/// <summary>
/// Runs the File Guard pipeline for a hook event: it resolves the host adapter, normalizes the payload, runs the
/// pipeline, and renders the host decision. This is the unchanged hook behavior, lifted behind the CLI so the
/// integrity gate can precede it and so it can be exercised directly.
/// </summary>
public static class GuardHost
{
    /// <summary>
    /// The only host this build wires.
    /// </summary>
    public const string ClaudeCodeHost = "claude-code";

    /// <summary>
    /// Runs a complete hook: the integrity self-check gates the run, and only when it allows does the File Guard
    /// pipeline run. A denied integrity check fails closed before the pipeline is ever reached, so a mismatched or
    /// unresolvable install denies even a benign payload. This is the exact composition the CLI hook handler runs.
    /// </summary>
    /// <param name="hookEvent">The lifecycle event.</param>
    /// <param name="resolvedBinaryPath">The resolved running binary path, for the integrity self-check.</param>
    /// <param name="rawPayload">The raw host payload from standard input.</param>
    /// <param name="host">The host identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="fileSystem">The platform file system for the integrity self-check, or <see langword="null"/> to
    /// use the real per-OS implementation. A test injects a managed test double so it never touches native, the same
    /// optional-parameter injection seam <see cref="GuardEngine.CreatePipeline(GuardEngineOptions)"/> uses for its
    /// directory enumerator.</param>
    /// <returns>The composed hook execution.</returns>
    public static async Task<HookExecution> ExecuteHookAsync(
        HookEvent hookEvent,
        string? resolvedBinaryPath,
        string rawPayload,
        string host,
        CancellationToken cancellationToken,
        IPlatformFileSystem? fileSystem = null)
    {
        IntegrityReport integrity = InstallIntegrity.Check(
            fileSystem ?? Platform.Create().FileSystem, resolvedBinaryPath);
        if (!integrity.IsAllowed)
        {
            return new HookExecution(2, integrity.Detail);
        }

        HostDecision decision = await RunPipelineAsync(hookEvent, rawPayload, host, cancellationToken)
            .ConfigureAwait(false);
        return new HookExecution(decision.ExitCode, decision.Message);
    }

    /// <summary>
    /// Runs the pipeline for a hook event and returns the host's exit-code decision. An unknown host, an
    /// unparsable payload, or an unreadable result all fail closed (deny).
    /// </summary>
    /// <param name="hookEvent">The lifecycle event.</param>
    /// <param name="rawPayload">The raw host payload from standard input.</param>
    /// <param name="host">The host identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The host decision.</returns>
    public static async Task<HostDecision> RunPipelineAsync(
        HookEvent hookEvent,
        string rawPayload,
        string host,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(host, ClaudeCodeHost, StringComparison.Ordinal))
        {
            return new HostDecision(2, $"Unknown host '{host}'; failing closed.");
        }

        IHostAdapter adapter = GuardEngine.CreateClaudeCodeAdapter();
        HostReadResult read = adapter.Read(hookEvent, rawPayload);
        return read switch
        {
            HostReadUnparsable unparsable => adapter.Render(Verdict.Deny(unparsable.Reason), hookEvent),
            HostReadParsed parsed => await RunAsync(adapter, parsed.Call, hookEvent, cancellationToken).ConfigureAwait(false),
            _ => adapter.Render(Verdict.Deny("The host payload could not be read; failing closed."), hookEvent),
        };
    }

    private static async Task<HostDecision> RunAsync(
        IHostAdapter adapter,
        NormalizedCall call,
        HookEvent hookEvent,
        CancellationToken cancellationToken)
    {
        var options = new GuardEngineOptions(call.Environment.ProjectRoot, TimeProvider.System);
        IPipeline pipeline = GuardEngine.CreatePipeline(options);
        Verdict verdict = await pipeline
            .RunAsync(hookEvent, call.Call, call.Environment, cancellationToken)
            .ConfigureAwait(false);
        return adapter.Render(verdict, hookEvent);
    }
}
