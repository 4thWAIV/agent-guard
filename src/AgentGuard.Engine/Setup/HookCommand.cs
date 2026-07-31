// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the hook command's wire format — the <c>hook</c> subcommand, the <c>pre</c>/<c>post</c>
/// event tokens, the <c>--host</c> flag (whose value is the one host id, <see cref="GuardHost.ClaudeCodeHost"/>),
/// and the <c>--agentguard-owned</c> sentinel. Both the writer that composes the command into
/// <c>.claude/settings.json</c> and the CLI reader that parses it build from these tokens, so the two sides cannot
/// drift.
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
    /// The sentinel argument that marks a hook command as guard-owned; ignored at runtime.
    /// </summary>
    public const string OwnedFlag = "--agentguard-owned";

    /// <summary>
    /// Builds the exact hook command for an event, embedding the absolute launcher path, the host, and the
    /// sentinel.
    /// </summary>
    /// <param name="absolutePath">The absolute launcher path (<c>~/.agentguard/bin/guard</c>).</param>
    /// <param name="eventToken">The event token, <see cref="PreEvent"/> or <see cref="PostEvent"/>.</param>
    /// <returns>The command string.</returns>
    public static string ForEvent(string absolutePath, string eventToken) =>
        $"{absolutePath} {Verb} {eventToken} {HostOption} {GuardHost.ClaudeCodeHost} {OwnedFlag}";
}
