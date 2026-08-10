// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The outcome of detecting one setup condition: a <see cref="ConditionStatus"/> and, when the condition is not
/// healthy, the reason it is broken or could not be verified.
/// </summary>
internal sealed record ConditionState
{
    private ConditionState(ConditionStatus status, string reason)
    {
        Status = status;
        Reason = reason;
    }

    /// <summary>
    /// Gets the detected status.
    /// </summary>
    internal ConditionStatus Status { get; }

    /// <summary>
    /// Gets the reason the condition is broken or could not be verified; empty when the condition holds.
    /// </summary>
    internal string Reason { get; }

    /// <summary>
    /// Creates a healthy state.
    /// </summary>
    /// <returns>An <see cref="ConditionStatus.Ok"/> state.</returns>
    internal static ConditionState Ok() => new(ConditionStatus.Ok, string.Empty);

    /// <summary>
    /// Creates a broken state.
    /// </summary>
    /// <param name="reason">Why the condition is not met.</param>
    /// <returns>A <see cref="ConditionStatus.Broken"/> state.</returns>
    internal static ConditionState Broken(string reason) => new(ConditionStatus.Broken, reason);

    /// <summary>
    /// Creates a cannot-verify state.
    /// </summary>
    /// <param name="reason">Why the condition could not be evaluated.</param>
    /// <returns>A <see cref="ConditionStatus.CannotVerify"/> state.</returns>
    internal static ConditionState CannotVerify(string reason) => new(ConditionStatus.CannotVerify, reason);
}
