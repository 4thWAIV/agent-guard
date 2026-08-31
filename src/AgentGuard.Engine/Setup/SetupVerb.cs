// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the three mutating setup-command verb tokens — <c>install</c>, <c>init</c>, and <c>remove</c>
/// (mirroring <see cref="HookCommand.Verb"/>). The CLI registers its commands from these constants, the approval gate
/// keys the owned <see cref="PresenceDialogText"/> resource and stamps <c>ApprovalDecision.Action</c> with them, and the
/// setup commands reference them, so the verb strings live in exactly one place and the CLI name, the dialog text, and
/// the decision can never drift.
/// </summary>
public static class SetupVerb
{
    /// <summary>
    /// The install verb.
    /// </summary>
    public const string Install = "install";

    /// <summary>
    /// The init verb.
    /// </summary>
    public const string Init = "init";

    /// <summary>
    /// The remove verb.
    /// </summary>
    public const string Remove = "remove";
}
