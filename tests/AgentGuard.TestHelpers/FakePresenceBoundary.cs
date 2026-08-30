// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The <see cref="IPresenceCheck"/> a <see cref="FakePresenceCheck"/> recorder hands out: a contract implementation with
/// a private constructor (AG0003), handed out only as its interface, whose <see cref="Check"/> records into the
/// recorder and returns the recorder's configured task. It never touches a native library — it is the above-the-seam
/// stand-in for the presence boundary.
/// </summary>
internal sealed class FakePresenceBoundary : IPresenceCheck
{
    private readonly FakePresenceCheck _owner;

    private FakePresenceBoundary(FakePresenceCheck owner) => _owner = owner;

    /// <inheritdoc />
    public Task<PresenceResult> Check(PresenceRequest request, ISystemServices services, CancellationToken ct) =>
        _owner.Invoke(request, ct);

    /// <summary>Creates the boundary over its recorder.</summary>
    /// <param name="owner">The recorder the boundary records into.</param>
    /// <returns>The presence boundary, as its interface.</returns>
    internal static IPresenceCheck Create(FakePresenceCheck owner) => new FakePresenceBoundary(owner);
}
