// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;

namespace AgentGuard.Setup;

/// <summary>
/// The project condition that the runtime-store directories are ignored in <c>.gitignore</c>.
/// </summary>
internal sealed class GitignoreCondition : ISetupCondition
{
    /// <inheritdoc />
    public string Name => "gitignore runtime-store lines";

    /// <inheritdoc />
    public SetupScope Scope => SetupScope.Project;

    /// <inheritdoc />
    public ConditionState Detect(SetupContext context)
    {
        string path = ProjectPaths.GitignoreFile(context);
        if (!File.Exists(path))
        {
            return ConditionState.Broken("the runtime-store lines are missing (no .gitignore)");
        }

        if (!SafeRead.TryReadText(path, out string content, out string error))
        {
            return ConditionState.CannotVerify($".gitignore is unreadable: {error}");
        }

        return GitignoreWiring.HasAllLines(content)
            ? ConditionState.Ok()
            : ConditionState.Broken("the runtime-store lines are missing from .gitignore");
    }

    /// <inheritdoc />
    public RepairOutcome Repair(SetupContext context) => CreationHelper.EnsureGitignore(context);
}
