// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Inspects the snapshot store's actual on-disk state for the in-flight call, so the File Guard's Precheck can
/// fail closed without any command parsing. It reports anomalous — which denies — when a snapshot already
/// exists for this call before Capture could have written one (a poisoned pre-image), when the store path is
/// tampered (a file where the store directory belongs), or when the state cannot be determined at all. It reads
/// through owned services received at construction, and enumerates fail-closed so an inaccessible directory denies.
/// </summary>
internal sealed class ContextStoreInspector : IContextStoreInspector
{
    private readonly string _projectRoot;
    private readonly ToolCallId _toolCall;
    private readonly string _guard;
    private readonly ContextStorePaths _paths;
    private readonly IFileReader _fileReader;
    private readonly IDirectoryEnumerator _directories;

    private ContextStoreInspector(
        string projectRoot,
        ToolCallId toolCall,
        string guard,
        ContextStorePaths paths,
        IFileReader fileReader,
        IDirectoryEnumerator directories)
    {
        _projectRoot = projectRoot;
        _toolCall = toolCall;
        _guard = guard;
        _paths = paths;
        _fileReader = fileReader;
        _directories = directories;
    }

    /// <inheritdoc />
    public Task<StoreInspection> InspectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            string baseDirectory = _paths.BaseDirectory(_projectRoot);
            if (_fileReader.Exists(baseDirectory))
            {
                return Task.FromResult(StoreInspection.Anomalous(
                    "The snapshot store path is a file, not a directory."));
            }

            string callDirectory = _paths.CallDirectory(_projectRoot, _toolCall);
            if (_directories.DirectoryExists(callDirectory))
            {
                string pattern = ContextStorePaths.GuardRecordSearchPattern(_guard);
                var options = new EnumerationOptions { IgnoreInaccessible = false };
                if (_directories.EnumerateFiles(callDirectory, pattern, options).Any())
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
    /// <param name="services">The OS/CLR service container the inspector reads through.</param>
    /// <param name="paths">The owned store-path layout.</param>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <param name="toolCall">The in-flight call whose store state is inspected.</param>
    /// <param name="guard">The owning Guard's name.</param>
    /// <returns>The inspector, as its interface.</returns>
    internal static IContextStoreInspector Create(
        ISystemServices services, ContextStorePaths paths, string projectRoot, ToolCallId toolCall, string guard)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        ArgumentException.ThrowIfNullOrEmpty(guard);
        return new ContextStoreInspector(
            projectRoot, toolCall, guard, paths, services.FileSystem.GetFileReader(), services.FileSystem.GetDirectoryReader());
    }
}
