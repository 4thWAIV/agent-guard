// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The readability of the machine state record at <c>~/.agentguard/state.json</c>.
/// </summary>
internal enum MachineStateStatus
{
    /// <summary>
    /// The record was read and is complete.
    /// </summary>
    Present,

    /// <summary>
    /// The record is absent: the machine is not installed.
    /// </summary>
    Missing,

    /// <summary>
    /// The record exists but could not be read or parsed; it is never treated as healthy.
    /// </summary>
    Unreadable,
}
