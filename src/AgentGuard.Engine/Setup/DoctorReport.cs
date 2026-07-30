// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// One line of a <c>doctor</c> report: a condition's name, scope, detected status, and detail.
/// </summary>
/// <param name="Name">The condition name.</param>
/// <param name="Scope">The condition scope (<c>Machine</c> or <c>Project</c>).</param>
/// <param name="Status">The detected status (<c>Ok</c>, <c>Broken</c>, or <c>CannotVerify</c>).</param>
/// <param name="Detail">The reason when not healthy; empty when healthy.</param>
public sealed record DoctorReport(string Name, string Scope, string Status, string Detail);
