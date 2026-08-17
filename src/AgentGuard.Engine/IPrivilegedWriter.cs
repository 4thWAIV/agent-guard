// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;

namespace AgentGuard.Engine;

/// <summary>
/// Executes the privileged side effects a Guard requests — restoring pre-call bytes and deleting created files.
/// It is the one place a protected path is written, kept out of the Guard-facing seams. It is Engine-internal
/// wiring, not a contract seam.
/// </summary>
internal interface IPrivilegedWriter
{
    /// <summary>
    /// Executes the given effects in order, best-effort: a single effect's failure does not abort the rest.
    /// </summary>
    /// <param name="effects">The effects to execute, in order.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when execution is done.</returns>
    Task ExecuteAsync(IReadOnlyList<Effect> effects, CancellationToken cancellationToken);
}
