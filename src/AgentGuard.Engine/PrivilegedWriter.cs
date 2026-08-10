// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// The Engine-internal privileged writer. It restores a file to its pre-call bytes and deletes a file the call
/// created, writing the working tree directly so the revert is never re-intercepted. It is best-effort: one
/// effect's failure does not stop the others, and the deny verdict stands regardless.
/// </summary>
internal sealed class PrivilegedWriter : IPrivilegedWriter
{
    private PrivilegedWriter()
    {
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
    /// Creates the privileged writer.
    /// </summary>
    /// <returns>The writer, as its interface.</returns>
    internal static IPrivilegedWriter Create() => new PrivilegedWriter();

    private static async Task ExecuteOneAsync(Effect effect, CancellationToken cancellationToken)
    {
        switch (effect)
        {
            case RestoreFileEffect restore:
                string? directory = Path.GetDirectoryName(restore.Path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(restore.Path, restore.Content, cancellationToken).ConfigureAwait(false);
                break;
            case DeleteFileEffect delete:
                if (File.Exists(delete.Path))
                {
                    File.Delete(delete.Path);
                }

                break;
            default:
                throw new InvalidOperationException($"Unhandled effect kind: {effect.GetType().Name}");
        }
    }
}
