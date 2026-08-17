// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The single place that owns the on-disk layout of the per-call snapshot store, so the store and its inspector
/// never disagree about where a record lives. Snapshots live in-repo under
/// <c>.protected-snapshots/&lt;hash-of-project&gt;/&lt;toolUseId&gt;/</c>, one file per (guard, kind). It is an
/// instance built from the container, receiving the owned <see cref="IEnvironment"/> by constructor so its one
/// non-pure path read — resolving the project root to an absolute path — routes through the owner rather than the
/// banned single-argument <c>Path.GetFullPath</c>.
/// </summary>
internal sealed class ContextStorePaths
{
    private readonly IEnvironment _environment;

    private ContextStorePaths(IEnvironment environment) => _environment = environment;

    /// <summary>
    /// Creates the store-path owner, drawing the environment from the container.
    /// </summary>
    /// <param name="services">The OS/CLR service container the environment is drawn from.</param>
    /// <returns>The store-path owner.</returns>
    internal static ContextStorePaths Create(ISystemServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new ContextStorePaths(services.Environment);
    }

    /// <summary>
    /// Returns the glob that finds every record a given guard wrote for a call.
    /// </summary>
    /// <param name="guard">The guard name.</param>
    /// <returns>The search pattern.</returns>
    internal static string GuardRecordSearchPattern(string guard) => $"{Sanitize(guard)}__*.bin";

    /// <summary>
    /// Returns the store's base directory for a project — <c>.protected-snapshots/&lt;hash-of-project&gt;</c>.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <returns>The absolute base directory.</returns>
    internal string BaseDirectory(string projectRoot)
    {
        string full = Path.GetFullPath(projectRoot, _environment.GetCurrentDirectory());
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
    internal string CallDirectory(string projectRoot, ToolCallId toolCall) =>
        Path.Combine(BaseDirectory(projectRoot), Sanitize(toolCall.Value));

    /// <summary>
    /// Returns the file that holds one record, addressed by its full key.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <param name="key">The record key.</param>
    /// <returns>The absolute record file path.</returns>
    internal string RecordFile(string projectRoot, ContextKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Path.Combine(CallDirectory(projectRoot, key.ToolCall), RecordFileName(key.Guard, key.Kind));
    }

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
