// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.Setup;

/// <summary>
/// The result of <c>doctor</c>: whether every in-scope condition is healthy, and a per-condition report. The exit
/// code is non-zero whenever any condition is broken or could not be verified.
/// </summary>
public sealed record DoctorOutcome
{
    private DoctorOutcome(bool healthy, IReadOnlyList<DoctorReport> reports)
    {
        Healthy = healthy;
        Reports = reports;
    }

    /// <summary>
    /// Gets a value indicating whether every in-scope condition is healthy.
    /// </summary>
    public bool Healthy { get; }

    /// <summary>
    /// Gets the per-condition report lines.
    /// </summary>
    public IReadOnlyList<DoctorReport> Reports { get; }

    /// <summary>
    /// Gets the process exit code: zero when healthy, otherwise one.
    /// </summary>
    public int ExitCode => Healthy ? 0 : 1;

    /// <summary>
    /// Creates a doctor outcome.
    /// </summary>
    /// <param name="healthy">Whether every condition is healthy.</param>
    /// <param name="reports">The per-condition reports.</param>
    /// <returns>A doctor outcome.</returns>
    internal static DoctorOutcome Create(bool healthy, IReadOnlyList<DoctorReport> reports) => new(healthy, reports);
}
