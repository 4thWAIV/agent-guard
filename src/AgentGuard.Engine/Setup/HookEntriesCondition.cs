// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// The project condition that the guard's Pre/Post hook entries are present in <c>.claude/settings.json</c> and
/// point at the current absolute launcher path.
/// </summary>
internal sealed class HookEntriesCondition : ISetupCondition
{
    /// <inheritdoc />
    public string Name => "Claude hook entries";

    /// <inheritdoc />
    public SetupScope Scope => SetupScope.Project;

    /// <inheritdoc />
    public ConditionState Detect(SetupContext context)
    {
        string path = ProjectPaths.ClaudeSettingsFile(context);
        string? existing = null;
        if (File.Exists(path))
        {
            if (!SafeRead.TryReadText(path, out string content, out string error))
            {
                return ConditionState.CannotVerify($".claude/settings.json is unreadable: {error}");
            }

            existing = content;
        }

        SettingsInspection inspection = ClaudeSettingsWiring.Inspect(existing, MachinePaths.BinGuard(context));
        return inspection.Health switch
        {
            SettingsHealth.Ok => ConditionState.Ok(),
            SettingsHealth.Malformed => ConditionState.CannotVerify(inspection.Detail),
            _ => ConditionState.Broken(inspection.Detail),
        };
    }

    /// <inheritdoc />
    public RepairOutcome Repair(SetupContext context) => CreationHelper.WireSettings(context);
}
