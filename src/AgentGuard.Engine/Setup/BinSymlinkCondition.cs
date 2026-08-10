// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// The machine condition that the stable launcher <c>bin/guard</c> resolves to <c>current/guard</c>.
/// </summary>
internal sealed class BinSymlinkCondition : MachineCondition
{
    /// <inheritdoc />
    public override string Name => "launcher bin/guard";

    /// <inheritdoc />
    public override RepairOutcome Repair(SetupContext context) => CreationHelper.EnsureBinGuard(context);

    /// <inheritdoc />
    protected override ConditionState DetectInstalled(SetupContext context, InstallState state)
    {
        if (!File.Exists(MachinePaths.BinGuard(context)))
        {
            return ConditionState.Broken("bin/guard does not resolve to the current binary");
        }

        string expected = MachinePaths.BinGuardRelativeTarget();
        string? actual = context.FileSystem.ReadLinkTarget(MachinePaths.BinGuard(context));
        return string.Equals(actual, expected, StringComparison.Ordinal)
            ? ConditionState.Ok()
            : ConditionState.Broken($"bin/guard points at '{actual}', expected '{expected}'");
    }
}
