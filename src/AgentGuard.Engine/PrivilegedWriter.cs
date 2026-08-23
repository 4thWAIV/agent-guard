// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The Engine-internal privileged writer. It restores a file to its pre-call bytes and deletes a file the call
/// created, writing the working tree directly so the revert is never re-intercepted. It is best-effort: one
/// effect's failure does not stop the others, and the deny verdict stands regardless. Every write goes through an
/// owned service received at construction, never a raw call.
/// </summary>
internal sealed class PrivilegedWriter : IPrivilegedWriter
{
    private readonly IFileReader _fileReader;
    private readonly IFileWriter _fileWriter;
    private readonly IDirectoryWriter _directoryWriter;

    private PrivilegedWriter(IFileReader fileReader, IFileWriter fileWriter, IDirectoryWriter directoryWriter)
    {
        _fileReader = fileReader;
        _fileWriter = fileWriter;
        _directoryWriter = directoryWriter;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IReadOnlyList<Effect> effects, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(effects);
        foreach (Effect effect in effects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ExecuteOneAsync(effect, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // Best-effort: a single failed restore or delete must not abort the remaining effects.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort: a single failed restore or delete must not abort the remaining effects.
            }
        }
    }

    /// <summary>
    /// Creates the privileged writer, drawing its owned write services from the container.
    /// </summary>
    /// <param name="services">The OS/CLR service container the writer draws its filesystem owners from.</param>
    /// <returns>The writer, as its interface.</returns>
    internal static IPrivilegedWriter Create(ISystemServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new PrivilegedWriter(services.FileSystem.GetFileReader(), services.FileSystem.GetFileWriter(), services.FileSystem.GetDirectoryWriter());
    }

    private async Task ExecuteOneAsync(Effect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case RestoreFileEffect restore:
                string? directory = Path.GetDirectoryName(restore.Path);
                if (!string.IsNullOrEmpty(directory))
                {
                    _directoryWriter.CreateDirectory(directory);
                }

                await _fileWriter.WriteAllBytesAsync(restore.Path, restore.Content, cancellationToken).ConfigureAwait(false);
                break;
            case DeleteFileEffect delete:
                if (_fileReader.Exists(delete.Path))
                {
                    _fileWriter.DeleteFile(delete.Path);
                }

                break;
            default:
                throw new InvalidOperationException($"Unhandled effect kind: {effect.GetType().Name}");
        }
    }
}
