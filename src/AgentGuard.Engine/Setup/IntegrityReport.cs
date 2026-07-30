// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The result of the hook integrity self-check. A denial fails the hook closed; when the run is allowed,
/// <see cref="BinaryWritable"/> reports (without blocking) whether the guard's own image is user-writable — a
/// same-user limitation this build states plainly and does not defend against.
/// </summary>
public sealed record IntegrityReport
{
    private IntegrityReport(bool isAllowed, string detail, bool binaryWritable)
    {
        IsAllowed = isAllowed;
        Detail = detail;
        BinaryWritable = binaryWritable;
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
    /// Gets a value indicating whether the running binary is user-writable. This is reported, not blocked.
    /// </summary>
    public bool BinaryWritable { get; }

    /// <summary>
    /// Creates an allow report.
    /// </summary>
    /// <param name="binaryWritable">Whether the running binary is user-writable.</param>
    /// <returns>An allow report.</returns>
    internal static IntegrityReport Allow(bool binaryWritable) => new(true, string.Empty, binaryWritable);

    /// <summary>
    /// Creates a deny report.
    /// </summary>
    /// <param name="reason">Why the run is denied.</param>
    /// <returns>A deny report.</returns>
    internal static IntegrityReport Deny(string reason) => new(false, reason, false);
}
