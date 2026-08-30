// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.CrossPlatform.Linux;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A fake of the internal Linux polkit port (<see cref="IPolkitAuthority"/>) for the <c>LinuxPresenceCheck</c> unit
/// tests: it records how many times <see cref="CheckAuthorizationAsync"/> was called and the subject / action id /
/// message it was given, and returns a configured <see cref="PolkitResult"/> — or throws, to model a D-Bus fault
/// (a bus-less runner) so the fault-to-<c>Error</c> path can be exercised. It touches no D-Bus. It is the single source
/// implementer of the port in this test compilation (AG0114 permits one). The call-count/last-argument capture and the
/// fault behavior are not hand-built here — it composes the shared <see cref="SingleCallRecorder{TArg,TResult}"/>.
/// </summary>
internal sealed class FakePolkitAuthority : IPolkitAuthority
{
    private readonly SingleCallRecorder<PolkitCall, PolkitResult> _recorder;

    private FakePolkitAuthority(SingleCallRecorder<PolkitCall, PolkitResult> recorder) => _recorder = recorder;

    internal int CallCount => _recorder.CallCount;

    internal PolkitSubject? LastSubject => _recorder.LastArg?.Subject;

    internal string? LastActionId => _recorder.LastArg?.ActionId;

    internal string? LastMessage => _recorder.LastArg?.Message;

    /// <inheritdoc />
    public Task<PolkitResult> CheckAuthorizationAsync(
        PolkitSubject subject, string actionId, string message, CancellationToken ct) =>
        Task.FromResult(_recorder.Record(new PolkitCall(subject, actionId, message)));

    /// <summary>Creates a fake that returns the given polkit reply.</summary>
    /// <param name="result">The reply every call returns.</param>
    /// <returns>The fake authority.</returns>
    internal static FakePolkitAuthority Returning(PolkitResult result) =>
        new(SingleCallRecorder<PolkitCall, PolkitResult>.Returning(result));

    /// <summary>Creates a fake that throws on every call, modelling a D-Bus fault.</summary>
    /// <returns>The faulting fake authority.</returns>
    internal static FakePolkitAuthority Faulting() =>
        new(SingleCallRecorder<PolkitCall, PolkitResult>.Faulting(
            () => new InvalidOperationException("simulated D-Bus fault (no system bus)")));
}
