// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// Reads the project's <c>.claude/settings.json</c> for the callers that then merge or inspect it. It is the one
/// place that turns a read failure into a message, so each caller only maps the outcome to its own return type.
/// </summary>
internal static class ClaudeSettings
{
    /// <summary>
    /// Reads the settings file, distinguishing an absent file (readable, no content) from a present-and-read file
    /// and from a present-but-unreadable file (an I/O or permission failure).
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The read outcome.</returns>
    internal static SettingsRead Read(SetupContext context)
    {
        string path = ProjectPaths.ClaudeSettingsFile(context);
        if (!File.Exists(path))
        {
            return SettingsRead.Absent();
        }

        return SafeRead.TryReadText(path, out string content, out string error)
            ? SettingsRead.Present(content)
            : SettingsRead.Unreadable($".claude/settings.json is unreadable: {error}");
    }
}
