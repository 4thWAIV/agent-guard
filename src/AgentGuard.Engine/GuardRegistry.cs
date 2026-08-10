// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// Exposes the compiled-in Guards the Pipeline runs, in order. For v1 this is the single File Guard; adding a
/// Guard is registering a module here, not editing a central switch.
/// </summary>
internal sealed class GuardRegistry : IGuardRegistry
{
    private readonly IReadOnlyList<IGuard> _guards;

    private GuardRegistry(IReadOnlyList<IGuard> guards) => _guards = guards;

    /// <inheritdoc />
    public IReadOnlyList<IGuard> Guards => _guards;

    /// <summary>
    /// Creates a registry over the given Guards, in run order.
    /// </summary>
    /// <param name="guards">The Guards to register.</param>
    /// <returns>The registry, as its interface.</returns>
    internal static IGuardRegistry Create(IReadOnlyList<IGuard> guards)
    {
        ArgumentNullException.ThrowIfNull(guards);
        return new GuardRegistry(guards);
    }
}
