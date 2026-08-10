// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The outcome of reading <c>.claude/settings.json</c>: an absent file is readable with no content; a present
/// file is readable with its content; a present-but-unreadable file carries the reason it could not be read.
/// </summary>
/// <param name="Readable">Whether the file was read or is absent; <see langword="false"/> only on an I/O or
/// permission failure.</param>
/// <param name="Content">The file content, or <see langword="null"/> when the file is absent.</param>
/// <param name="UnreadableReason">The reason the file could not be read, or <see langword="null"/> when it was
/// readable.</param>
internal readonly record struct SettingsRead(bool Readable, string? Content, string? UnreadableReason)
{
    /// <summary>Creates the outcome for an absent file: readable, with no content.</summary>
    /// <returns>The absent outcome.</returns>
    internal static SettingsRead Absent() => new(true, null, null);

    /// <summary>Creates the outcome for a file that was read.</summary>
    /// <param name="content">The file content.</param>
    /// <returns>The present outcome.</returns>
    internal static SettingsRead Present(string content) => new(true, content, null);

    /// <summary>Creates the outcome for a file that exists but could not be read.</summary>
    /// <param name="reason">The failure reason.</param>
    /// <returns>The unreadable outcome.</returns>
    internal static SettingsRead Unreadable(string reason) => new(false, null, reason);
}
