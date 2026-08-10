// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AgentGuard.Engine.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// The single place that owns the on-disk layout of the per-call snapshot store, so the store and its inspector
/// never disagree about where a record lives. Snapshots live in-repo under
/// <c>.protected-snapshots/&lt;hash-of-project&gt;/&lt;toolUseId&gt;/</c>, one file per (guard, kind).
/// </summary>
internal static class ContextStorePaths
{
    /// <summary>
    /// Returns the store's base directory for a project — <c>.protected-snapshots/&lt;hash-of-project&gt;</c>.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The absolute base directory.</returns>
    internal static string BaseDirectory(string projectRoot)
    {
        string full = Path.GetFullPath(projectRoot);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(full));
        string projectHash = Convert.ToHexStringLower(hash)[..16];
        return Path.Combine(full, CoreSystemPaths.SnapshotStoreRelative, projectHash);
    }

    /// <summary>
    /// Returns the directory that holds all records for one call.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <param name="toolCall">The call whose records are addressed.</param>
    /// <returns>The absolute per-call directory.</returns>
    internal static string CallDirectory(string projectRoot, ToolCallId toolCall) =>
        Path.Combine(BaseDirectory(projectRoot), Sanitize(toolCall.Value));

    /// <summary>
    /// Returns the file that holds one record, addressed by its full key.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <param name="key">The record key.</param>
    /// <returns>The absolute record file path.</returns>
    internal static string RecordFile(string projectRoot, ContextKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Path.Combine(CallDirectory(projectRoot, key.ToolCall), RecordFileName(key.Guard, key.Kind));
    }

    /// <summary>
    /// Returns the glob that finds every record a given guard wrote for a call.
    /// </summary>
    /// <param name="guard">The guard name.</param>
    /// <returns>The search pattern.</returns>
    internal static string GuardRecordSearchPattern(string guard) => $"{Sanitize(guard)}__*.bin";

    private static string RecordFileName(string guard, string kind) => $"{Sanitize(guard)}__{Sanitize(kind)}.bin";

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            bool safe = char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_';
            builder.Append(safe ? character : '_');
        }

        return builder.Length == 0 ? "_" : builder.ToString();
    }
}
