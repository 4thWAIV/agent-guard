// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.IO;

namespace AgentGuard.Engine;

/// <summary>
/// The fixed, source-hardcoded locations of the guard's core system: the Sealed stores (the snapshot store and
/// the grant-token store) and the System runtime wiring (the hook settings file and the grant public key). These
/// are the only paths hardcoded in source; every consuming-project path is configured. The Bash reference-token
/// list is derived from these same constants, so the tokens the Precheck denies on cannot drift from the paths
/// the Sealed and System rules protect.
/// </summary>
internal static class CoreSystemPaths
{
    /// <summary>
    /// The repo-root-relative directory that holds the per-call pre-image snapshots (a Sealed store).
    /// </summary>
    internal const string SnapshotStoreRelative = ".protected-snapshots";

    /// <summary>
    /// The repo-root-relative directory that holds signed grant tokens (a Sealed store).
    /// </summary>
    internal const string GrantStoreRelative = ".agentguard/grants";

    /// <summary>
    /// The repo-root-relative file holding the committed Ed25519 public key that verifies grant signatures.
    /// </summary>
    internal const string GrantPublicKeyRelative = ".agentguard/grant-public-key";

    /// <summary>
    /// The repo-root-relative runtime-wiring file (a System path a grant can unlock).
    /// </summary>
    internal const string ClaudeSettingsRelative = ".claude/settings.json";

    /// <summary>
    /// Gets the distinctive path tokens a Bash command is denied for referencing at Precheck. It is derived from
    /// the Sealed-store constants and the System runtime-wiring paths, so the tokens the Precheck denies on
    /// cannot drift from the paths the rules protect. The check is deliberately best-effort and conservative,
    /// catching the distinctive names cleanly.
    /// </summary>
    internal static IReadOnlyList<string> BashReferenceTokens { get; } = new[]
    {
        SnapshotStoreRelative,
        GrantStoreRelative,
        GrantPublicKeyRelative,
        ClaudeSettingsRelative,
    };

    /// <summary>
    /// Combines a repo-root-relative core-system location with the given project root into an absolute path.
    /// </summary>
    /// <param name="projectRoot">The absolute project root.</param>
    /// <param name="relative">The repo-root-relative location.</param>
    /// <returns>The absolute path under the project root.</returns>
    internal static string Absolute(string projectRoot, string relative) =>
        Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
}
