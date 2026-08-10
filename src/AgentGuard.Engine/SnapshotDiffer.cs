// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using AgentGuard.Engine.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// Compares a materialized pre-image map against a materialized current map and returns the per-path changes.
/// It performs no IO, so it stays pure: created, modified, and removed are decided entirely from the two maps,
/// and prior existence — which the fail-closed capture makes reliable — is what tells a created file from an
/// untouched one.
/// </summary>
internal static class SnapshotDiffer
{
    /// <summary>
    /// Produces the changes between the pre-image and the current tree.
    /// </summary>
    /// <param name="preImage">The pre-call bytes of every protected file that existed at capture.</param>
    /// <param name="current">The current bytes of every protected file present now.</param>
    /// <returns>The per-path changes.</returns>
    internal static IReadOnlyList<FileChange> Diff(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> preImage,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> current)
    {
        ArgumentNullException.ThrowIfNull(preImage);
        ArgumentNullException.ThrowIfNull(current);
        var changes = new List<FileChange>();
        foreach (KeyValuePair<string, ReadOnlyMemory<byte>> before in preImage)
        {
            if (current.TryGetValue(before.Key, out ReadOnlyMemory<byte> after))
            {
                if (!before.Value.Span.SequenceEqual(after.Span))
                {
                    changes.Add(new FileModified(before.Key, before.Value, after));
                }
            }
            else
            {
                changes.Add(new FileRemoved(before.Key, before.Value));
            }
        }

        foreach (KeyValuePair<string, ReadOnlyMemory<byte>> after in current.Where(after => !preImage.ContainsKey(after.Key)))
        {
            changes.Add(new FileCreated(after.Key, after.Value));
        }

        return changes;
    }
}
