// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Text.Json;

namespace AgentGuard.Setup;

/// <summary>
/// The project condition that the project's stamped guard version matches the installed machine version; a drift
/// is reported and, with <c>--fix</c>, re-stamped.
/// </summary>
internal sealed class VersionStampCondition : ISetupCondition
{
    /// <inheritdoc />
    public string Name => "project version stamp";

    /// <inheritdoc />
    public SetupScope Scope => SetupScope.Project;

    /// <inheritdoc />
    public ConditionState Detect(SetupContext context)
    {
        MachineFacts machine = MachineInspection.Read(context);
        if (machine.Status == MachineStateStatus.Missing)
        {
            return ConditionState.Broken(machine.Detail);
        }

        if (machine.Status == MachineStateStatus.Unreadable)
        {
            return ConditionState.CannotVerify(machine.Detail);
        }

        string path = ProjectPaths.StateFile(context);
        if (!File.Exists(path))
        {
            return ConditionState.Broken(".agentguard/state.json version stamp is missing");
        }

        if (!SafeRead.TryReadText(path, out string content, out string error))
        {
            return ConditionState.CannotVerify($".agentguard/state.json is unreadable: {error}");
        }

        ProjectState? project;
        try
        {
            project = SetupJson.DeserializeProjectState(content);
        }
        catch (JsonException exception)
        {
            return ConditionState.Broken($".agentguard/state.json does not parse: {exception.Message}");
        }

        if (project is null || string.IsNullOrEmpty(project.GuardVersion))
        {
            return ConditionState.Broken(".agentguard/state.json has no guard version");
        }

        string installed = machine.State!.Version;
        return string.Equals(project.GuardVersion, installed, StringComparison.Ordinal)
            ? ConditionState.Ok()
            : ConditionState.Broken($"version drift: project wired {project.GuardVersion}, installed {installed}");
    }

    /// <inheritdoc />
    public RepairOutcome Repair(SetupContext context)
    {
        MachineFacts machine = MachineInspection.Read(context);
        if (machine.Status != MachineStateStatus.Present)
        {
            return RepairOutcome.NotRepairable($"cannot stamp the project version: {machine.Detail}");
        }

        CreationHelper.WriteProjectState(context, machine.State!.Version);
        return RepairOutcome.Repaired("stamped the project with the installed version");
    }
}
