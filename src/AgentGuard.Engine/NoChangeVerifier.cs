// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.Engine;

/// <summary>
/// The default Verifier: any landed change to a protected file is unauthorized. It denies unconditionally,
/// which drives the Engine to revert the change to its pre-call bytes (or delete a created file).
/// </summary>
internal sealed class NoChangeVerifier : IVerifier
{
    private NoChangeVerifier()
    {
    }

    /// <inheritdoc />
    public string Name => "no-change";

    /// <inheritdoc />
    public Task<Verdict> VerifyAsync(FileChange change, Grant? grant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);
        return Task.FromResult(Verdict.Deny($"Unauthorized change to protected file: {change.Path}"));
    }

    /// <summary>
    /// Creates the no-change verifier.
    /// </summary>
    /// <returns>The verifier, as its interface.</returns>
    internal static IVerifier Create() => new NoChangeVerifier();
}
