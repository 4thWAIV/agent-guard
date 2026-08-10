// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the hook command's wire format — the <c>hook</c> subcommand, the <c>pre</c>/<c>post</c>
/// event tokens, and the <c>--host</c> flag (whose value is the one host id, <see cref="GuardHost.ClaudeCodeHost"/>).
/// Both the writer that composes the command into <c>.claude/settings.json</c> and the CLI reader that parses it
/// build from these tokens, so the two sides cannot drift. The guard's own hook groups are identified by this
/// command — a call to the guard binary running <c>hook pre</c>/<c>hook post --host claude-code</c> — never by a
/// marker (see <see cref="IsGuardCommand"/>).
/// </summary>
public static class HookCommand
{
    /// <summary>
    /// The hook subcommand name.
    /// </summary>
    public const string Verb = "hook";

    /// <summary>
    /// The pre-tool-use event token.
    /// </summary>
    public const string PreEvent = "pre";

    /// <summary>
    /// The post-tool-use event token.
    /// </summary>
    public const string PostEvent = "post";

    /// <summary>
    /// The host flag whose value selects the host runtime.
    /// </summary>
    public const string HostOption = "--host";

    /// <summary>
    /// Builds the exact hook command for an event, embedding the absolute launcher path and the host.
    /// </summary>
    /// <param name="absolutePath">The absolute launcher path (<c>~/.agentguard/bin/guard</c>).</param>
    /// <param name="eventToken">The event token, <see cref="PreEvent"/> or <see cref="PostEvent"/>.</param>
    /// <returns>The command string.</returns>
    public static string ForEvent(string absolutePath, string eventToken) =>
        $"{absolutePath} {EventSignature(eventToken)}";

    /// <summary>
    /// Determines whether a hook command is one of the guard's own — a call to the guard binary running
    /// <c>hook pre</c>/<c>hook post</c> for the Claude Code host. This is how the guard's groups are identified
    /// regardless of the launcher path they carry; it is identification, not authorization.
    /// </summary>
    /// <param name="command">The hook command string to test, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the command invokes the guard's pre or post hook.</returns>
    public static bool IsGuardCommand(string? command) =>
        command is not null
        && (command.Contains(EventSignature(PreEvent), StringComparison.Ordinal)
            || command.Contains(EventSignature(PostEvent), StringComparison.Ordinal));

    /// <summary>
    /// The single owner of the guard command's event-and-host suffix — <c>hook &lt;event&gt; --host claude-code</c>.
    /// Both <see cref="ForEvent"/> (composition) and <see cref="IsGuardCommand"/> (identification) build from it, so
    /// the emitted command and the recogniser cannot drift.
    /// </summary>
    /// <param name="eventToken">The event token, <see cref="PreEvent"/> or <see cref="PostEvent"/>.</param>
    /// <returns>The suffix string.</returns>
    private static string EventSignature(string eventToken) =>
        string.Join(' ', Verb, eventToken, HostOption, GuardHost.ClaudeCodeHost);
}
