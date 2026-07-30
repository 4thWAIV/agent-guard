// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Inspects the snapshot store's actual on-disk state for the in-flight call, so the File Guard's Precheck can
/// fail closed without any command parsing. It reports anomalous — which denies — when a snapshot already
/// exists for this call before Capture could have written one (a poisoned pre-image), when the store path is
/// tampered (a file where the store directory belongs), or when the state cannot be determined at all.
/// </summary>
internal sealed class ContextStoreInspector : IContextStoreInspector
{
    private readonly string _projectRoot;
    private readonly ToolCallId _toolCall;
    private readonly string _guard;

    private ContextStoreInspector(string projectRoot, ToolCallId toolCall, string guard)
    {
        _projectRoot = projectRoot;
        _toolCall = toolCall;
        _guard = guard;
    }

    /// <inheritdoc />
    public Task<StoreInspection> InspectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            string baseDirectory = ContextStorePaths.BaseDirectory(_projectRoot);
            if (File.Exists(baseDirectory))
            {
                return Task.FromResult(StoreInspection.Anomalous(
                    "The snapshot store path is a file, not a directory."));
            }

            string callDirectory = ContextStorePaths.CallDirectory(_projectRoot, _toolCall);
            if (Directory.Exists(callDirectory))
            {
                string pattern = ContextStorePaths.GuardRecordSearchPattern(_guard);
                if (Directory.EnumerateFiles(callDirectory, pattern, SearchOption.TopDirectoryOnly).Any())
                {
                    return Task.FromResult(StoreInspection.Anomalous(
                        "A snapshot already exists for this call before capture; the store may be poisoned."));
                }
            }

            return Task.FromResult(StoreInspection.Clean());
        }
        catch (IOException exception)
        {
            return Task.FromResult(StoreInspection.Anomalous(
                $"The snapshot store state could not be determined: {exception.Message}"));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Task.FromResult(StoreInspection.Anomalous(
                $"The snapshot store state could not be determined: {exception.Message}"));
        }
    }

    /// <summary>
    /// Creates an inspector scoped to one call and Guard.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <param name="toolCall">The in-flight call whose store state is inspected.</param>
    /// <param name="guard">The owning Guard's name.</param>
    /// <returns>The inspector, as its interface.</returns>
    internal static IContextStoreInspector Create(string projectRoot, ToolCallId toolCall, string guard)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        ArgumentException.ThrowIfNullOrEmpty(guard);
        return new ContextStoreInspector(projectRoot, toolCall, guard);
    }
}
