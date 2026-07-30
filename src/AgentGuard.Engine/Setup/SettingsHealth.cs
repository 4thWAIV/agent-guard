// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The health of the guard's hook entries in <c>.claude/settings.json</c>, as detected by <c>doctor</c>.
/// </summary>
internal enum SettingsHealth
{
    /// <summary>
    /// Every guard hook entry is present and points at the current absolute launcher path.
    /// </summary>
    Ok,

    /// <summary>
    /// One or more guard hook entries are absent.
    /// </summary>
    Missing,

    /// <summary>
    /// A guard hook entry is present but points at a stale path.
    /// </summary>
    Stale,

    /// <summary>
    /// The settings file could not be parsed or has a shape that cannot be evaluated.
    /// </summary>
    Malformed,
}
