// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// The machine condition that <c>current</c> resolves to the installed version and reaches its binary.
/// </summary>
internal sealed class CurrentSymlinkCondition : MachineCondition
{
    /// <inheritdoc />
    public override string Name => "current version pointer";

    /// <inheritdoc />
    public override RepairOutcome Repair(SetupContext context) => CreationHelper.PointCurrent(context);

    /// <inheritdoc />
    protected override ConditionState DetectInstalled(SetupContext context, InstallState state)
    {
        if (!File.Exists(MachinePaths.CurrentBinary(context)))
        {
            return ConditionState.Broken("current does not resolve to an installed version's binary");
        }

        string expected = MachinePaths.CurrentRelativeTarget(state.Version);
        string? actual = SymlinkOps.ReadRawTarget(MachinePaths.Current(context));
        return string.Equals(actual, expected, StringComparison.Ordinal)
            ? ConditionState.Ok()
            : ConditionState.Broken($"current points at '{actual}', expected '{expected}'");
    }
}
