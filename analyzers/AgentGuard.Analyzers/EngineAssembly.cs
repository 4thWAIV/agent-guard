// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers;

/// <summary>
/// Identifies the one application assembly — <c>AgentGuard.Engine</c> — that holds the <c>SystemServices</c>
/// container and its <c>Create()</c> composition factory. Engine is the only assembly permitted to reach into
/// <c>AgentGuard.Boundaries</c> (AG0040), into the core <c>AgentGuard.CrossPlatform</c> assembly (AG0023), and into a
/// per-OS implementation assembly (AG0029), and then only from <c>SystemServices.Create()</c>; it is also the assembly
/// whose internals the CLI and the test helpers may reach only through that one factory call (AG0041). This is the
/// single owner of the name, mirroring <see cref="BoundaryAssembly"/>, <see cref="CliAssembly"/>, and
/// <see cref="TestAssembly"/>, so a rename touches one place.
/// </summary>
internal static class EngineAssembly
{
    /// <summary>
    /// The exact assembly name of the engine — <c>AgentGuard.Engine</c> — which is also its root namespace. The
    /// single owner of this value.
    /// </summary>
    internal const string Name = "AgentGuard.Engine";
}
