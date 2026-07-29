// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// Where a <see cref="Rule"/> came from, in precedence order. The top two are the built-in anti-cheat floor.
/// </summary>
public enum RuleOrigin
{
    /// <summary>
    /// Built-in and unremovable; no Grant ever unlocks it. The Grant store and the Context store.
    /// </summary>
    Sealed,

    /// <summary>
    /// Built-in and unremovable, but a Grant can unlock it. The guard's own shipped code and runtime wiring.
    /// </summary>
    System,

    /// <summary>
    /// Contributed by an opt-in language <see cref="Contracts.IProvider"/>.
    /// </summary>
    Provider,

    /// <summary>
    /// Added by the consuming repository's own configuration.
    /// </summary>
    Project,
}
