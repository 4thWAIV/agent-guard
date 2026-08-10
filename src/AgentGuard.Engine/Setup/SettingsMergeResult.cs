// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The result of merging or stripping the guard's hook entries in <c>.claude/settings.json</c>: either the
/// merged JSON, or a conflict message when the existing file cannot be safely merged and must be left untouched.
/// </summary>
internal sealed record SettingsMergeResult
{
    private SettingsMergeResult(bool success, string? json, string? conflict)
    {
        Success = success;
        Json = json;
        Conflict = conflict;
    }

    /// <summary>
    /// Gets a value indicating whether the merge succeeded.
    /// </summary>
    internal bool Success { get; }

    /// <summary>
    /// Gets the merged JSON when <see cref="Success"/> is <see langword="true"/>; otherwise <see langword="null"/>.
    /// </summary>
    internal string? Json { get; }

    /// <summary>
    /// Gets the conflict message when <see cref="Success"/> is <see langword="false"/>; otherwise
    /// <see langword="null"/>.
    /// </summary>
    internal string? Conflict { get; }

    /// <summary>
    /// Creates a successful merge result.
    /// </summary>
    /// <param name="json">The merged JSON.</param>
    /// <returns>A successful result.</returns>
    internal static SettingsMergeResult Merged(string json) => new(true, json, null);

    /// <summary>
    /// Creates a conflict result; the existing file is left untouched.
    /// </summary>
    /// <param name="reason">Why the file cannot be safely merged.</param>
    /// <returns>A conflict result.</returns>
    internal static SettingsMergeResult Refused(string reason) => new(false, null, reason);
}
