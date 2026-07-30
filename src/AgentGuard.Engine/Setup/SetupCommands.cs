// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Versioning;

namespace AgentGuard.Setup;

/// <summary>
/// The setup commands behind thin CLI handlers. Each mutating command routes its work through the shared
/// condition set and creation helper, so the layout and merge logic is owned in one place. <c>install</c>,
/// <c>init</c>, and <c>remove</c> call the approval gate before mutating anything (ungated in this build);
/// <c>doctor</c> does not.
/// </summary>
public static class SetupCommands
{
    /// <summary>
    /// Installs or updates the machine: copies the running binary into the version store, flips <c>current</c>,
    /// ensures the launcher and PATH line, and records the version and hash. Refuses an older version unless
    /// <paramref name="allowDowngrade"/> is set.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <param name="allowDowngrade">Whether to permit installing an older version than the recorded one.</param>
    /// <returns>The command outcome.</returns>
    public static CommandOutcome Install(SetupContext context, bool allowDowngrade)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!ApprovalGate.RequireApproval("install").IsApproved)
        {
            return CommandOutcome.Failed("install was not approved");
        }

        if (string.IsNullOrEmpty(context.ResolvedBinaryPath) || !File.Exists(context.ResolvedBinaryPath))
        {
            return CommandOutcome.Failed("install could not resolve the running binary");
        }

        if (!SemVer.TryParse(context.RunningVersion, out NuGetVersion? running))
        {
            return CommandOutcome.Failed($"install could not parse the running version '{context.RunningVersion}'");
        }

        MachineFacts facts = MachineInspection.Read(context);
        if (facts.Status == MachineStateStatus.Present
            && SemVer.TryParse(facts.State!.Version, out NuGetVersion? installed)
            && SemVer.Compare(running, installed) < 0
            && !allowDowngrade)
        {
            return CommandOutcome.Failed(
                $"refusing to install {running.ToNormalizedString()} over the newer installed "
                + $"{installed.ToNormalizedString()}; pass --allow-downgrade to override");
        }

        string version = running.ToNormalizedString();
        string sha256 = Hashing.Sha256HexOfFile(context.ResolvedBinaryPath);

        var messages = new List<string>();
        string? failure = ApplyConditions(SetupConditions.MachineStructural, context, messages);
        if (failure is not null)
        {
            return CommandOutcome.Failed($"install {failure}");
        }

        CreationHelper.WriteMachineState(context, version, sha256);
        messages.Add($"recorded install: version {version}");
        return CommandOutcome.Ok(messages);
    }

    /// <summary>
    /// Initializes the project: creates <c>.agentguard/</c>, wires the Claude Code hooks, stamps the version, and
    /// updates <c>.gitignore</c>. Refuses when the machine is not installed, or on a real settings conflict.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The command outcome.</returns>
    public static CommandOutcome Init(SetupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!ApprovalGate.RequireApproval("init").IsApproved)
        {
            return CommandOutcome.Failed("init was not approved");
        }

        if (!File.Exists(MachinePaths.BinGuard(context)))
        {
            return CommandOutcome.Failed("the machine is not installed; run `guard install` first");
        }

        string? conflict = PreflightSettings(context);
        if (conflict is not null)
        {
            return CommandOutcome.Failed(conflict);
        }

        var messages = new List<string>();
        CreationHelper.EnsureAgentGuardBase(context);
        string? failure = ApplyConditions(SetupConditions.Project, context, messages);
        if (failure is not null)
        {
            return CommandOutcome.Failed($"init {failure}");
        }

        messages.Add("initialized the project");
        return CommandOutcome.Ok(messages);
    }

    /// <summary>
    /// Removes the guard's project wiring: strips the guard hook entries from <c>.claude/settings.json</c> and
    /// removes the <c>.agentguard/</c> directory, leaving all non-guard content intact. Idempotent.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The command outcome.</returns>
    public static CommandOutcome Remove(SetupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!ApprovalGate.RequireApproval("remove").IsApproved)
        {
            return CommandOutcome.Failed("remove was not approved");
        }

        var messages = new List<string>();
        CreationHelper.UnwireSettings(context);
        messages.Add("removed the guard hook entries from .claude/settings.json");
        CreationHelper.RemoveAgentGuardDirectory(context);
        messages.Add("removed .agentguard/");
        return CommandOutcome.Ok(messages);
    }

    /// <summary>
    /// Runs every in-scope condition's detection and, with <paramref name="fix"/>, repairs the broken structural
    /// ones through the shared helper. The machine conditions always run; the project conditions run only inside
    /// an initialized repository. It is not approval-gated.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <param name="fix">Whether to repair broken structural conditions.</param>
    /// <returns>The doctor outcome.</returns>
    public static DoctorOutcome Doctor(SetupContext context, bool fix)
    {
        ArgumentNullException.ThrowIfNull(context);
        var conditions = new List<ISetupCondition>(SetupConditions.Machine);
        if (ProjectPaths.IsInitialized(context))
        {
            conditions.AddRange(SetupConditions.Project);
        }

        if (fix)
        {
            foreach (ISetupCondition condition in conditions)
            {
                if (condition.Detect(context).Status == ConditionStatus.Broken)
                {
                    condition.Repair(context);
                }
            }
        }

        var reports = new List<DoctorReport>();
        bool healthy = true;
        foreach (ISetupCondition condition in conditions)
        {
            ConditionState state = condition.Detect(context);
            reports.Add(new DoctorReport(
                condition.Name, condition.Scope.ToString(), state.Status.ToString(), state.Reason));
            if (state.Status != ConditionStatus.Ok)
            {
                healthy = false;
            }
        }

        return DoctorOutcome.Create(healthy, reports);
    }

    private static string? ApplyConditions(
        IReadOnlyList<ISetupCondition> conditions,
        SetupContext context,
        List<string> messages)
    {
        foreach (ISetupCondition condition in conditions)
        {
            RepairOutcome outcome = condition.Repair(context);
            if (outcome.Kind == RepairKind.NotRepairable)
            {
                return $"failed at '{condition.Name}': {outcome.Detail}";
            }

            if (outcome.Kind == RepairKind.Repaired)
            {
                messages.Add($"{condition.Name}: {outcome.Detail}");
            }
        }

        return null;
    }

    private static string? PreflightSettings(SetupContext context)
    {
        string path = ProjectPaths.ClaudeSettingsFile(context);
        string? existing = null;
        if (File.Exists(path))
        {
            if (!SafeRead.TryReadText(path, out string content, out string error))
            {
                return $".claude/settings.json is unreadable: {error}";
            }

            existing = content;
        }

        SettingsMergeResult result = ClaudeSettingsWiring.AddGuardEntries(existing, MachinePaths.BinGuard(context));
        return result.Success ? null : $"init refused: {result.Conflict}";
    }
}
