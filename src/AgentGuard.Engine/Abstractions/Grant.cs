// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Engine.Abstractions.Contracts;

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// A verified authorization for a change a Guard would otherwise block. Instances handed to Guards have already
/// had their signature checked by an <see cref="Contracts.IGrantStore"/>, so no raw signature is carried here.
/// </summary>
/// <param name="Id">The unique identifier of the grant.</param>
/// <param name="Scope">The breadth of change the grant authorizes.</param>
/// <param name="CoveredPaths">The matchers for the paths the grant covers, sharing the canonicalizing match logic Rules use.</param>
/// <param name="ExpiresAt">The instant after which the grant is no longer honored.</param>
public sealed record Grant(
    string Id,
    GrantScope Scope,
    IReadOnlyList<IPathMatcher> CoveredPaths,
    DateTimeOffset ExpiresAt);
