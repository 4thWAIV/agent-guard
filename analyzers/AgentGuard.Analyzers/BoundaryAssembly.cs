// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Analyzers;

/// <summary>
/// Identifies the one assembly — <c>AgentGuard.Boundaries</c> — where a raw OS-uniform primitive (a
/// <c>System.IO</c>, <c>System.Environment</c>, <c>System.Random</c>, <c>Guid.NewGuid</c>, or <c>System.Console</c>
/// call) is allowed to live, behind the boundary adapters. Every other assembly reaches those primitives through
/// the owned interfaces pulled off <c>ISystemServices</c>, so a raw call anywhere else is a build error. This is
/// the OS-uniform counterpart to <see cref="CrossPlatformBoundary"/>, which locks the OS-divergent primitives to
/// the per-OS platform libraries.
/// </summary>
internal static class BoundaryAssembly
{
    /// <summary>
    /// The exact assembly name where the boundary adapters and the <c>SystemServices</c> container live, and the
    /// only place a raw OS-uniform primitive call compiles. The single owner of this value.
    /// </summary>
    internal const string Name = "AgentGuard.Boundaries";
}
