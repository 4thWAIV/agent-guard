// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Setup;

namespace AgentGuard.Engine;

/// <summary>
/// The per-call snapshot store the Engine owns, persisted between the separate Pre and Post processes. It is
/// never handed to Guards — they receive only the scoped writer, reader, and inspector — which is what enforces
/// the capability split. Each call writes and reads only its own records; there is no content-addressing,
/// dedup, or cross-call sharing. Every filesystem operation goes through an owned service received at
/// construction, never a raw call.
/// </summary>
internal sealed class ContextStore : IContextStore
{
    private readonly string _projectRoot;
    private readonly TimeProvider _timeProvider;
    private readonly ContextStorePaths _paths;
    private readonly AtomicFile _atomicFile;
    private readonly IFileReader _fileReader;
    private readonly IDirectoryEnumerator _directories;
    private readonly IDirectoryWriter _directoryWriter;

    private ContextStore(
        string projectRoot,
        TimeProvider timeProvider,
        ContextStorePaths paths,
        AtomicFile atomicFile,
        IFileReader fileReader,
        IDirectoryEnumerator directories,
        IDirectoryWriter directoryWriter)
    {
        _projectRoot = projectRoot;
        _timeProvider = timeProvider;
        _paths = paths;
        _atomicFile = atomicFile;
        _fileReader = fileReader;
        _directories = directories;
        _directoryWriter = directoryWriter;
    }

    /// <inheritdoc />
    public async Task WriteAsync(ContextKey key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        string recordFile = _paths.RecordFile(_projectRoot, key);
        await _atomicFile.WriteAllBytesAsync(recordFile, data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ContextRead> ReadAsync(ContextKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        string recordFile = _paths.RecordFile(_projectRoot, key);
        if (!_fileReader.Exists(recordFile))
        {
            return new ContextMissing(key.Kind);
        }

        byte[] bytes = await _fileReader.ReadAllBytesAsync(recordFile, cancellationToken).ConfigureAwait(false);
        return new ContextFound(bytes);
    }

    /// <inheritdoc />
    public Task DeleteAsync(ToolCallId toolCall, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string callDirectory = _paths.CallDirectory(_projectRoot, toolCall);
        if (_directories.DirectoryExists(callDirectory))
        {
            _directoryWriter.DeleteDirectory(callDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SweepExpiredAsync(TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string baseDirectory = _paths.BaseDirectory(_projectRoot);
        if (!_directories.DirectoryExists(baseDirectory))
        {
            return Task.CompletedTask;
        }

        DateTimeOffset cutoff = _timeProvider.GetUtcNow() - timeToLive;
        var options = new EnumerationOptions { IgnoreInaccessible = false };
        foreach (string callDirectory in _directories.EnumerateDirectories(baseDirectory, "*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_directories.GetLastWriteTimeUtc(callDirectory) < cutoff)
            {
                _directoryWriter.DeleteDirectory(callDirectory, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates the store bound to a project root, drawing its owned services and clock from the container.
    /// </summary>
    /// <param name="services">The OS/CLR service container the store draws its filesystem owners and clock from.</param>
    /// <param name="paths">The owned store-path layout.</param>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The store, as its interface.</returns>
    internal static IContextStore Create(ISystemServices services, ContextStorePaths paths, string projectRoot)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        var atomicFile = new AtomicFile(services.FileSystem.GetFileWriter(), services.FileSystem.GetDirectoryWriter(), services.Random);
        return new ContextStore(
            projectRoot,
            services.Clock,
            paths,
            atomicFile,
            services.FileSystem.GetFileReader(),
            services.FileSystem.GetDirectoryReader(),
            services.FileSystem.GetDirectoryWriter());
    }
}
