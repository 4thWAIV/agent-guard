// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers;

/// <summary>
/// Identifies the one shipping assembly — <c>AgentGuard.Cli</c> — that holds the single <c>Program</c> composition
/// method where <c>SystemServices.Create()</c> may be called (AG0017). Binding the exemption to this assembly name,
/// the same way the sibling gates do, stops a class merely named <c>Program</c> in any other assembly from
/// self-granting itself the right to reconstruct the container. The single owner of this value, mirroring
/// <see cref="BoundaryAssembly"/> and <see cref="TestAssembly"/>.
/// </summary>
internal static class CliAssembly
{
    /// <summary>
    /// The exact assembly name of the CLI, whose <c>Program</c> composition method is the one shipping place
    /// <c>SystemServices.Create()</c> is allowed. The single owner of this value.
    /// </summary>
    internal const string Name = "AgentGuard.Cli";
}
