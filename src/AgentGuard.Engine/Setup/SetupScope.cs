// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The reach of a setup condition: whether it describes the machine-wide install under the user's home directory,
/// or the per-project wiring under a repository root. <c>doctor</c> always evaluates the machine conditions and
/// evaluates the project conditions only inside an initialized repository.
/// </summary>
internal enum SetupScope
{
    /// <summary>
    /// The machine-wide install under <c>~/.agentguard/</c>.
    /// </summary>
    Machine,

    /// <summary>
    /// The per-project wiring under a repository root.
    /// </summary>
    Project,
}
