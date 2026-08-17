// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Setup;

/// <summary>
/// Appends the guard's PATH line to a shell profile, idempotently: an existing line carrying the marker is never
/// duplicated.
/// </summary>
internal static class PathProfileWiring
{
    /// <summary>
    /// Ensures the PATH export line is present in the profile, appending it only when absent.
    /// </summary>
    /// <param name="append">The owned idempotent appender that performs the read-and-write.</param>
    /// <param name="profilePath">The profile file.</param>
    /// <returns><see langword="true"/> when the line was appended; <see langword="false"/> when it was already
    /// present.</returns>
    internal static bool Ensure(IdempotentAppend append, string profilePath) =>
        append.Ensure(profilePath, existing =>
            existing.Contains(ShellProfile.Marker, StringComparison.Ordinal)
                ? null
                : "\n# Added by AgentGuard: put the guard launcher on PATH.\n" + ShellProfile.ExportLine() + "\n");
}
