// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// Resolves the shell profile the PATH line is appended to from the detected login shell, and owns the exact
/// export line and the marker used to keep the append idempotent.
/// </summary>
internal static class ShellProfile
{
    /// <summary>
    /// The substring the PATH export line always contains, used to detect an existing line so no duplicate is
    /// ever appended.
    /// </summary>
    internal const string Marker = MachinePaths.RootDirectoryName + "/bin";

    /// <summary>
    /// Resolves the profile file for a login shell under the given home directory. Zsh maps to <c>.zshrc</c> and
    /// bash to <c>.bashrc</c> — both take the same POSIX <c>export</c> line — and every other shell maps to
    /// <c>.profile</c>. Shells whose PATH syntax differs (such as fish) are deliberately not special-cased, so the
    /// <c>export</c> line is never written into a profile a shell would source and choke on.
    /// </summary>
    /// <param name="shellPath">The login shell path (for example the value of <c>$SHELL</c>).</param>
    /// <param name="home">The home directory.</param>
    /// <returns>The absolute profile path.</returns>
    internal static string Resolve(string shellPath, string home)
    {
        string shell = shellPath ?? string.Empty;
        if (shell.Contains("zsh", StringComparison.Ordinal))
        {
            return Path.Combine(home, ".zshrc");
        }

        if (shell.Contains("bash", StringComparison.Ordinal))
        {
            return Path.Combine(home, ".bashrc");
        }

        return Path.Combine(home, ".profile");
    }

    /// <summary>
    /// Returns the exact PATH export line that puts the guard launcher on PATH.
    /// </summary>
    /// <returns>The export line.</returns>
    internal static string ExportLine() =>
        "export PATH=\"$HOME/" + MachinePaths.RootDirectoryName + "/bin:$PATH\"";
}
