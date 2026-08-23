// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// The signed content of a grant token: what change it authorizes, over which paths, and until when. The
/// signature is computed over this payload's canonical bytes, so the verifier re-derives identical bytes
/// regardless of the stored file's formatting.
/// </summary>
/// <param name="Id">The unique identifier of the grant.</param>
/// <param name="Scope">The breadth of change authorized; only <see cref="GrantScope.All"/> is honored in v1.</param>
/// <param name="CoveredPaths">The path patterns the grant covers.</param>
/// <param name="ExpiresAt">The instant after which the grant is no longer honored.</param>
public sealed record GrantTokenPayload(
    string Id,
    GrantScope Scope,
    IReadOnlyList<string> CoveredPaths,
    DateTimeOffset ExpiresAt);
