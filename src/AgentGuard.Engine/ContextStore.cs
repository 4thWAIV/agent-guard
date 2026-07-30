// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The per-call snapshot store the Engine owns, persisted between the separate Pre and Post processes. It is
/// never handed to Guards — they receive only the scoped writer, reader, and inspector — which is what enforces
/// the capability split. Each call writes and reads only its own records; there is no content-addressing,
/// dedup, or cross-call sharing.
/// </summary>
internal sealed class ContextStore : IContextStore
{
    private readonly string _projectRoot;
    private readonly TimeProvider _timeProvider;

    private ContextStore(string projectRoot, TimeProvider timeProvider)
    {
        _projectRoot = projectRoot;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task WriteAsync(ContextKey key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        string recordFile = ContextStorePaths.RecordFile(_projectRoot, key);
        string directory = Path.GetDirectoryName(recordFile)!;
        Directory.CreateDirectory(directory);
        string temp = recordFile + ".tmp-" + Guid.NewGuid().ToString("N");
        await File.WriteAllBytesAsync(temp, data, cancellationToken).ConfigureAwait(false);
        File.Move(temp, recordFile, overwrite: true);
    }

    /// <inheritdoc />
    public async Task<ContextRead> ReadAsync(ContextKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        string recordFile = ContextStorePaths.RecordFile(_projectRoot, key);
        if (!File.Exists(recordFile))
        {
            return new ContextMissing(key.Kind);
        }

        byte[] bytes = await File.ReadAllBytesAsync(recordFile, cancellationToken).ConfigureAwait(false);
        return new ContextFound(bytes);
    }

    /// <inheritdoc />
    public Task DeleteAsync(ToolCallId toolCall, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string callDirectory = ContextStorePaths.CallDirectory(_projectRoot, toolCall);
        if (Directory.Exists(callDirectory))
        {
            Directory.Delete(callDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SweepExpiredAsync(TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string baseDirectory = ContextStorePaths.BaseDirectory(_projectRoot);
        if (!Directory.Exists(baseDirectory))
        {
            return Task.CompletedTask;
        }

        DateTimeOffset cutoff = _timeProvider.GetUtcNow() - timeToLive;
        foreach (string callDirectory in Directory.EnumerateDirectories(baseDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.GetLastWriteTimeUtc(callDirectory) < cutoff.UtcDateTime)
            {
                Directory.Delete(callDirectory, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates the store bound to a project root.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <param name="timeProvider">The time source used to age out orphaned snapshots.</param>
    /// <returns>The store, as its interface.</returns>
    internal static IContextStore Create(string projectRoot, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        ArgumentNullException.ThrowIfNull(timeProvider);
        return new ContextStore(projectRoot, timeProvider);
    }
}
