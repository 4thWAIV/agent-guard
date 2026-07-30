// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The health of a single setup condition, as reported by its detection. <see cref="CannotVerify"/> is distinct
/// from <see cref="Ok"/> so an unreadable record is never mistaken for a healthy one.
/// </summary>
internal enum ConditionStatus
{
    /// <summary>
    /// The condition holds.
    /// </summary>
    Ok,

    /// <summary>
    /// The condition is definitely not met, with a stated reason.
    /// </summary>
    Broken,

    /// <summary>
    /// The condition could not be evaluated, with a stated reason; it is never treated as healthy.
    /// </summary>
    CannotVerify,
}
