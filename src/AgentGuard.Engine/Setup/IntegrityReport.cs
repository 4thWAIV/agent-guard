// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The result of the hook integrity self-check. A denial fails the hook closed; an allow lets the run proceed.
/// </summary>
public sealed record IntegrityReport
{
    private IntegrityReport(bool isAllowed, string detail)
    {
        IsAllowed = isAllowed;
        Detail = detail;
    }

    /// <summary>
    /// Gets a value indicating whether the hook run is allowed to proceed.
    /// </summary>
    public bool IsAllowed { get; }

    /// <summary>
    /// Gets the reason the run is denied; empty when allowed.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Creates an allow report.
    /// </summary>
    /// <returns>An allow report.</returns>
    internal static IntegrityReport Allow() => new(true, string.Empty);

    /// <summary>
    /// Creates a deny report.
    /// </summary>
    /// <param name="reason">Why the run is denied.</param>
    /// <returns>A deny report.</returns>
    internal static IntegrityReport Deny(string reason) => new(false, reason);
}
