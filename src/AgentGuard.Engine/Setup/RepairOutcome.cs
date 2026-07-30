// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The outcome of a condition's repair attempt: what happened and a human-readable detail.
/// </summary>
internal sealed record RepairOutcome
{
    private RepairOutcome(RepairKind kind, string detail)
    {
        Kind = kind;
        Detail = detail;
    }

    /// <summary>
    /// Gets the kind of repair result.
    /// </summary>
    internal RepairKind Kind { get; }

    /// <summary>
    /// Gets the human-readable detail describing what was done, or why it could not be done.
    /// </summary>
    internal string Detail { get; }

    /// <summary>
    /// Creates a repaired outcome.
    /// </summary>
    /// <param name="detail">What was established or corrected.</param>
    /// <returns>A <see cref="RepairKind.Repaired"/> outcome.</returns>
    internal static RepairOutcome Repaired(string detail) => new(RepairKind.Repaired, detail);

    /// <summary>
    /// Creates a no-change-needed outcome.
    /// </summary>
    /// <returns>A <see cref="RepairKind.NoChangeNeeded"/> outcome.</returns>
    internal static RepairOutcome NoChangeNeeded() => new(RepairKind.NoChangeNeeded, string.Empty);

    /// <summary>
    /// Creates a not-repairable outcome.
    /// </summary>
    /// <param name="detail">Why the condition cannot be auto-repaired.</param>
    /// <returns>A <see cref="RepairKind.NotRepairable"/> outcome.</returns>
    internal static RepairOutcome NotRepairable(string detail) => new(RepairKind.NotRepairable, detail);
}
