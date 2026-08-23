// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// Exposes the Guards the Pipeline runs, in order. Guards are compiled in and registered at build time, so
/// adding one is adding a module rather than editing a central switch.
/// </summary>
public interface IGuardRegistry
{
    /// <summary>
    /// Gets the registered Guards in the order the Pipeline runs them.
    /// </summary>
    IReadOnlyList<IGuard> Guards { get; }
}
