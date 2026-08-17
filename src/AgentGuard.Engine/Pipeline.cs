// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Runs the registered Guards for a single hook event and aggregates their judgments into one decision. It owns
/// the Pre/Capture ordering (a denied call never reaches Capture), executes Post effects through the privileged
/// writer before deleting the call's snapshot, and maps any Post-phase error to a deny — never to an allow.
/// </summary>
internal sealed class Pipeline : IPipeline
{
    private static readonly TimeSpan OrphanSnapshotTimeToLive = TimeSpan.FromHours(24);

    private readonly IGuardRegistry _registry;
    private readonly IContextStore _store;
    private readonly IPrivilegedWriter _privilegedWriter;
    private readonly ISystemServices _services;
    private readonly ContextStorePaths _paths;

    private Pipeline(
        IGuardRegistry registry,
        IContextStore store,
        IPrivilegedWriter privilegedWriter,
        ISystemServices services,
        ContextStorePaths paths)
    {
        _registry = registry;
        _store = store;
        _privilegedWriter = privilegedWriter;
        _services = services;
        _paths = paths;
    }

    /// <inheritdoc />
    public Task<Verdict> RunAsync(
        HookEvent hookEvent,
        ToolCall toolCall,
        CallEnvironment environment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(environment);
        return hookEvent switch
        {
            HookEvent.PreToolUse => RunPreAsync(toolCall, environment, cancellationToken),
            HookEvent.PostToolUse => RunPostAsync(toolCall, environment, cancellationToken),
            _ => Task.FromResult(Verdict.Deny($"Unknown hook event: {hookEvent}")),
        };
    }

    /// <summary>
    /// Creates the pipeline.
    /// </summary>
    /// <param name="registry">The registered Guards.</param>
    /// <param name="store">The Engine-owned Context store.</param>
    /// <param name="privilegedWriter">The privileged effect executor.</param>
    /// <param name="services">The OS/CLR service container the per-call store inspector reads through.</param>
    /// <param name="paths">The owned store-path layout the per-call store inspector uses.</param>
    /// <returns>The pipeline, as its interface.</returns>
    internal static IPipeline Create(
        IGuardRegistry registry,
        IContextStore store,
        IPrivilegedWriter privilegedWriter,
        ISystemServices services,
        ContextStorePaths paths)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(privilegedWriter);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(paths);
        return new Pipeline(registry, store, privilegedWriter, services, paths);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: any caught exception must become a deny.")]
    private async Task<Verdict> RunPreAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SweepBestEffortAsync(cancellationToken).ConfigureAwait(false);

            var verdicts = new List<Verdict>();
            foreach (IGuard guard in _registry.Guards)
            {
                IContextStoreInspector inspector = ContextStoreInspector.Create(
                    _services, _paths, environment.ProjectRoot, toolCall.Id, guard.Name);
                verdicts.Add(await guard.PrecheckAsync(toolCall, environment, inspector, cancellationToken)
                    .ConfigureAwait(false));
            }

            Verdict aggregate = VerdictAggregator.Aggregate(verdicts);
            if (aggregate.Kind == VerdictKind.Deny)
            {
                return aggregate;
            }

            foreach (IGuard guard in _registry.Guards)
            {
                IContextWriter writer = ContextWriter.Create(_store, toolCall.Id, guard.Name);
                CaptureResult result = await guard.CaptureAsync(toolCall, environment, writer, cancellationToken)
                    .ConfigureAwait(false);
                if (result is CaptureFailed failed)
                {
                    return Verdict.Deny($"Capture failed, failing closed: {failed.Reason}");
                }
            }

            return aggregate;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Verdict.Deny($"Pre-phase error, failing closed: {exception.Message}");
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Fail-closed boundary: any caught exception must become a deny.")]
    private async Task<Verdict> RunPostAsync(
        ToolCall toolCall,
        CallEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var verdicts = new List<Verdict>();
            var effects = new List<Effect>();
            foreach (IGuard guard in _registry.Guards)
            {
                IContextReader reader = ContextReader.Create(_store, toolCall.Id, guard.Name);
                PostcheckResult result = await guard.PostcheckAsync(toolCall, environment, reader, cancellationToken)
                    .ConfigureAwait(false);
                verdicts.Add(result.Verdict);
                effects.AddRange(result.Effects);
            }

            await _privilegedWriter.ExecuteAsync(effects, cancellationToken).ConfigureAwait(false);
            return VerdictAggregator.Aggregate(verdicts);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Verdict.Deny($"Post-phase error, failing closed: {exception.Message}");
        }
        finally
        {
            await DeleteBestEffortAsync(toolCall.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SweepBestEffortAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _store.SweepExpiredAsync(OrphanSnapshotTimeToLive, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Best-effort housekeeping: a failed sweep must never block the call.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort housekeeping: a failed sweep must never block the call.
        }
    }

    private async Task DeleteBestEffortAsync(ToolCallId toolCall, CancellationToken cancellationToken)
    {
        try
        {
            await _store.DeleteAsync(toolCall, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Best-effort cleanup: an orphaned snapshot is swept later by TTL.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup: an orphaned snapshot is swept later by TTL.
        }
    }
}
