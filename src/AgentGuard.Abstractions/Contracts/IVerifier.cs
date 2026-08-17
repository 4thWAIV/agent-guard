// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// A reusable change-judgment strategy a Rule names for its Postcheck. It answers Conformance: is the change
/// that actually landed inside the authorizing Grant's envelope. Exists only at the Post phase, because before
/// the write there is no change to inspect.
/// </summary>
public interface IVerifier
{
    /// <summary>
    /// Gets the stable name of this Verifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Judges whether a landed change conforms.
    /// </summary>
    /// <param name="change">The difference between the pre-call snapshot and what landed on disk.</param>
    /// <param name="grant">The Grant whose scope selects the envelope, or <see langword="null"/> when no Grant covers the path.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to allow when the change conforms, or deny when it must be reverted.</returns>
    Task<Verdict> VerifyAsync(FileChange change, Grant? grant, CancellationToken cancellationToken);
}
