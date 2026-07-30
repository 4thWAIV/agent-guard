// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The base for machine-scoped conditions. It reads the machine state once and short-circuits to broken (the
/// machine is not installed) or cannot-verify (the record is unreadable), so each condition only expresses the
/// check that is unique to it.
/// </summary>
internal abstract class MachineCondition : ISetupCondition
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public SetupScope Scope => SetupScope.Machine;

    /// <inheritdoc />
    public ConditionState Detect(SetupContext context)
    {
        MachineFacts facts = MachineInspection.Read(context);
        return facts.Status switch
        {
            MachineStateStatus.Missing => ConditionState.Broken(facts.Detail),
            MachineStateStatus.Unreadable => ConditionState.CannotVerify(facts.Detail),
            _ => DetectInstalled(context, facts.State!),
        };
    }

    /// <inheritdoc />
    public abstract RepairOutcome Repair(SetupContext context);

    /// <summary>
    /// Detects the condition's own slice, given a readable machine state.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <param name="state">The readable machine state.</param>
    /// <returns>The detected state.</returns>
    protected abstract ConditionState DetectInstalled(SetupContext context, InstallState state);
}
