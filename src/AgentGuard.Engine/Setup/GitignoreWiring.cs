// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgentGuard.Engine;

namespace AgentGuard.Setup;

/// <summary>
/// Adds the two runtime-store directories to a repository's <c>.gitignore</c>, idempotently. The lines are the
/// snapshot store and the grant store, derived from the core-system constants; <c>config.json</c> and the public
/// key are never ignored.
/// </summary>
internal static class GitignoreWiring
{
    /// <summary>
    /// Gets the ignore lines the guard adds: the snapshot store and the grant store, each a directory.
    /// </summary>
    internal static IReadOnlyList<string> RequiredLines { get; } = new[]
    {
        CoreSystemPaths.SnapshotStoreRelative + "/",
        CoreSystemPaths.GrantStoreRelative + "/",
    };

    /// <summary>
    /// Gets a value indicating whether the content already contains every required ignore line.
    /// </summary>
    /// <param name="content">The current <c>.gitignore</c> content.</param>
    /// <returns><see langword="true"/> when all required lines are present.</returns>
    internal static bool HasAllLines(string content) =>
        RequiredLines.All(line => ContainsLine(content, line));

    /// <summary>
    /// Ensures every required ignore line is present, appending only the missing ones.
    /// </summary>
    /// <param name="append">The owned idempotent appender that performs the read-and-write.</param>
    /// <param name="gitignorePath">The <c>.gitignore</c> path.</param>
    /// <returns><see langword="true"/> when a line was appended; <see langword="false"/> when all were present.</returns>
    internal static bool Ensure(IdempotentAppend append, string gitignorePath) =>
        append.Ensure(gitignorePath, existing =>
        {
            List<string> missing = RequiredLines.Where(line => !ContainsLine(existing, line)).ToList();
            if (missing.Count == 0)
            {
                return null;
            }

            var block = new StringBuilder("\n# Added by AgentGuard: runtime stores are never committed.\n");
            foreach (string line in missing)
            {
                block.Append(line).Append('\n');
            }

            return block.ToString();
        });

    private static bool ContainsLine(string content, string line) =>
        content.Split('\n').Any(existing => string.Equals(existing.Trim(), line, StringComparison.Ordinal));
}
