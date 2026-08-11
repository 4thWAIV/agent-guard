// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The read machine state and its readability, shared by every machine condition so none re-derives how the
/// record is read.
/// </summary>
/// <param name="Status">Whether the record is present, missing, or unreadable.</param>
/// <param name="State">The parsed state when <paramref name="Status"/> is
/// <see cref="MachineStateStatus.Present"/>; otherwise <see langword="null"/>.</param>
/// <param name="Detail">A human-readable detail describing a missing or unreadable record.</param>
internal sealed record MachineFacts(MachineStateStatus Status, InstallState? State, string Detail);
