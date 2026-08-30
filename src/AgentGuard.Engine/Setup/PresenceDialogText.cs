// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The single owner of the reviewed, action-specific prompt strings the OS presence dialog shows — one English string
/// per setup verb (<c>install</c>/<c>init</c>/<c>remove</c>), keyed by <see cref="SetupVerb"/>
/// (os-dialog-text-owned-resource). The approval gate resolves the prompt through <see cref="For"/> and never bakes a
/// literal in at the call site (AG0109 recognizes a read through this accessor as the one sanctioned prompt source).
/// Localization is deferred.
/// </summary>
internal static class PresenceDialogText
{
    /// <summary>
    /// Resolves the reviewed prompt string for a setup verb.
    /// </summary>
    /// <param name="verb">The setup verb (<see cref="SetupVerb.Install"/>/<see cref="SetupVerb.Init"/>/
    /// <see cref="SetupVerb.Remove"/>).</param>
    /// <returns>The reviewed, action-specific prompt string for that verb.</returns>
    internal static string For(string verb) => verb switch
    {
        SetupVerb.Install =>
            "AgentGuard is about to install the guard on this machine. "
            + "Confirm you are physically present to authorize this change.",
        SetupVerb.Init =>
            "AgentGuard is about to wire the guard into this repository. "
            + "Confirm you are physically present to authorize this change.",
        SetupVerb.Remove =>
            "AgentGuard is about to remove the guard's wiring from this repository. "
            + "Confirm you are physically present to authorize this change.",
        _ => throw new System.ArgumentOutOfRangeException(
            nameof(verb), verb, "No reviewed presence prompt is defined for this setup verb."),
    };
}
