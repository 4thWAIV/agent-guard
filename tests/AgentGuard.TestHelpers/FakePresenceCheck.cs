// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The configurable presence recorder the above-the-seam gate tests drive. It is NOT itself an
/// <see cref="IPresenceCheck"/> — a contract implementation may expose only its interface members (AG0005), and the
/// recorded <see cref="CallCount"/>/<see cref="LastRequest"/> are the whole point — so it hands out an
/// <see cref="IPresenceCheck"/> through <see cref="Presence"/> (installed via
/// <c>SystemServicesBuilder.OnPlatform().With(IPresenceCheck)</c>). It stands in only for the presence boundary; the
/// system under test is the real <c>ApprovalGate</c> / <c>SetupCommands</c>. It pins one of the two shapes the gate must
/// handle: a boundary that returns a fixed <see cref="PresenceResult"/> (any <see cref="ApprovalReason"/> and its
/// detail), or a boundary that NEVER completes on its own (the fake-clock timeout case, where the gate's 60-second wait
/// must win). The default (<see cref="Approved"/>) is what the builder installs so the existing setup tests proceed past
/// the gate.
/// </summary>
public sealed class FakePresenceCheck
{
    // A non-null result is returned immediately; a null result models a boundary that never completes on its own, so
    // the gate's clock-driven timeout is the only thing that can end the race.
    private readonly PresenceResult? _result;
    private int _callCount;
    private IPresenceCheck? _presence;

    private FakePresenceCheck(PresenceResult? result) => _result = result;

    /// <summary>Gets the number of times the handed-out boundary's <c>Check</c> has been invoked.</summary>
    public int CallCount => Volatile.Read(ref _callCount);

    /// <summary>Gets the request passed to the most recent <c>Check</c> call, or <see langword="null"/> if none.</summary>
    public PresenceRequest? LastRequest { get; private set; }

    /// <summary>Gets the presence boundary to install on the builder; its <c>Check</c> records into this recorder.</summary>
    public IPresenceCheck Presence => _presence ??= FakePresenceBoundary.Create(this);

    /// <summary>Creates a recorder whose boundary approves presence.</summary>
    /// <param name="detail">The detail the boundary reports on its CLI line.</param>
    /// <returns>An approving recorder.</returns>
    public static FakePresenceCheck Approved(string detail = "presence confirmed") =>
        new(new PresenceResult(ApprovalReason.Approved, detail));

    /// <summary>Creates a recorder whose boundary returns the given reason and detail.</summary>
    /// <param name="reason">The reason the boundary returns.</param>
    /// <param name="detail">The detail the boundary reports on its CLI line.</param>
    /// <returns>A recorder returning that outcome.</returns>
    public static FakePresenceCheck Returning(ApprovalReason reason, string detail) =>
        new(new PresenceResult(reason, detail));

    /// <summary>Creates a recorder whose boundary never completes on its own, so the gate's timeout must win.</summary>
    /// <returns>A never-completing recorder.</returns>
    public static FakePresenceCheck NeverCompleting() => new(result: null);

    /// <summary>Records a boundary <c>Check</c> call and produces the configured task; called by the handed-out boundary.</summary>
    /// <param name="request">The presence request the boundary received.</param>
    /// <param name="ct">The gate's cancellation token, honored by the never-completing shape.</param>
    /// <returns>The configured result, or a task that ends only when <paramref name="ct"/> is cancelled.</returns>
    internal Task<PresenceResult> Invoke(PresenceRequest request, CancellationToken ct)
    {
        Interlocked.Increment(ref _callCount);
        LastRequest = request;

        if (_result is not null)
        {
            return Task.FromResult(_result);
        }

        // Never-completing boundary: hand back a task that only ends when the gate cancels the token because its
        // timeout won. Nothing here waits on real time, so the test advances the fake clock and returns at once.
        var pending = new TaskCompletionSource<PresenceResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ct.Register(() => pending.TrySetCanceled(ct));
        return pending.Task;
    }
}
