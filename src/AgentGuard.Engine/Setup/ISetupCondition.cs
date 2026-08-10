// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// One structural condition the setup surface establishes and reports. It is the single object that both
/// <c>install</c>/<c>init</c> (which apply it) and <c>doctor</c> (which detects and, with <c>--fix</c>, repairs
/// it) share, so the two never re-derive the same layout logic. Every repair routes through the shared creation
/// helper.
/// </summary>
internal interface ISetupCondition
{
    /// <summary>
    /// Gets the short, human-readable name printed by <c>doctor</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the scope this condition belongs to.
    /// </summary>
    SetupScope Scope { get; }

    /// <summary>
    /// Detects the current state of the condition without changing anything.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The detected state.</returns>
    ConditionState Detect(SetupContext context);

    /// <summary>
    /// Establishes or corrects the condition through the shared creation helper. Conditions that must not be
    /// fabricated (such as a hash record) return <see cref="RepairKind.NotRepairable"/>.
    /// </summary>
    /// <param name="context">The setup context.</param>
    /// <returns>The repair outcome.</returns>
    RepairOutcome Repair(SetupContext context);
}
