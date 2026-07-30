// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// The machine condition that the shell profile carries the PATH line for the launcher directory.
/// </summary>
internal sealed class PathProfileCondition : MachineCondition
{
    /// <inheritdoc />
    public override string Name => "PATH profile line";

    /// <inheritdoc />
    public override RepairOutcome Repair(SetupContext context) => CreationHelper.EnsurePathLine(context);

    /// <inheritdoc />
    protected override ConditionState DetectInstalled(SetupContext context, InstallState state)
    {
        string profile = context.ShellProfilePath;
        if (!File.Exists(profile))
        {
            return ConditionState.Broken($"the PATH line is missing (no profile at {profile})");
        }

        if (!SafeRead.TryReadText(profile, out string content, out string error))
        {
            return ConditionState.CannotVerify($"{profile} is unreadable: {error}");
        }

        return content.Contains(ShellProfile.Marker, StringComparison.Ordinal)
            ? ConditionState.Ok()
            : ConditionState.Broken($"the PATH line is missing from {profile}");
    }
}
