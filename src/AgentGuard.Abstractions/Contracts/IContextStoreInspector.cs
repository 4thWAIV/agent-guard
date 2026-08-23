// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// A read-only view of the Context store's actual state for the in-flight call, handed to Precheck so the File
/// Guard's v1 snapshot-store check can run without any command parsing. It reports whether the store is in a
/// state that must fail closed — a snapshot already present for a call that has not captured yet, or a tampered
/// store directory.
/// </summary>
public interface IContextStoreInspector
{
    /// <summary>
    /// Inspects the snapshot store's on-disk state for the in-flight call: a record already present for this
    /// call before Capture could have written one, or a tampered store directory. A failure to determine the
    /// state must be reported as anomalous, so the check fails closed.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the inspection outcome; an anomalous result must deny the call.</returns>
    Task<StoreInspection> InspectAsync(CancellationToken cancellationToken);
}
