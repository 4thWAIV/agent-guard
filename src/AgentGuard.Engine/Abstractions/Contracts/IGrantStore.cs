// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions.Contracts;

/// <summary>
/// Loads the active Grants for the current repository, verifying each signature against the trusted public key
/// and dropping any that are expired or fail verification. Minting Grants is a separate concern; this only
/// answers which Grants may be honored.
/// </summary>
public interface IGrantStore
{
    /// <summary>
    /// Returns every currently valid Grant — signature-verified and unexpired.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the active Grants, or an empty list when none apply.</returns>
    Task<IReadOnlyList<Grant>> GetActiveGrantsAsync(CancellationToken cancellationToken);
}
