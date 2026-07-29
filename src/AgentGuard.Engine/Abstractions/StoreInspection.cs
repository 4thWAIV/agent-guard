// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// The outcome of inspecting the snapshot store's state before a call, carrying a reason when the store must
/// not be trusted so a Precheck can say why it blocked. An implementation that cannot determine the state must
/// return <see cref="Anomalous"/>, never <see cref="Clean"/>, so an inspection failure fails closed.
/// </summary>
public sealed record StoreInspection
{
    private StoreInspection(bool isAnomalous, string? reason)
    {
        this.IsAnomalous = isAnomalous;
        this.Reason = reason;
    }

    /// <summary>
    /// Gets a value indicating whether the store state is anomalous and the call must be denied.
    /// </summary>
    public bool IsAnomalous { get; }

    /// <summary>
    /// Gets the reason the store was judged anomalous, or <see langword="null"/> when it is clean.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Creates a result meaning the store is in good order and the call may proceed.
    /// </summary>
    /// <returns>A clean inspection result.</returns>
    public static StoreInspection Clean() => new(isAnomalous: false, reason: null);

    /// <summary>
    /// Creates a result meaning the store must not be trusted, so the call is denied. This is also the correct
    /// result when the state could not be determined.
    /// </summary>
    /// <param name="reason">Why the store was judged anomalous or indeterminate.</param>
    /// <returns>An anomalous inspection result.</returns>
    public static StoreInspection Anomalous(string reason) => new(isAnomalous: true, reason);
}
