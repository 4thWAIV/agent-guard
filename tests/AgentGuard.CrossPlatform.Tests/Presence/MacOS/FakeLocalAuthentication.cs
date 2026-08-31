// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.CrossPlatform.MacOS;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A fake of the internal macOS native port (<see cref="ILocalAuthentication"/>) for the <c>MacOsPresenceCheck</c> unit
/// test: it records how many times <see cref="EvaluateAsync"/> was called and the reason it was given, and returns a
/// configured <see cref="LaResult"/>. It touches no native library, so it stands in for the boundary while the real
/// mapping-and-single-call behavior of <c>MacOsPresenceCheck</c> is exercised. It is the single source implementer of
/// the port in this test compilation (AG0114 permits one). The call-count/last-argument capture is not hand-built here —
/// it composes the shared <see cref="SingleCallRecorder{TArg,TResult}"/>.
/// </summary>
internal sealed class FakeLocalAuthentication : ILocalAuthentication, IRecordingPresencePortFake
{
    private readonly SingleCallRecorder<string, LaResult> _recorder;

    /// <summary>Initializes a new instance of the <see cref="FakeLocalAuthentication"/> class returning a fixed outcome.</summary>
    /// <param name="result">The native outcome every evaluation returns.</param>
    public FakeLocalAuthentication(LaResult result) =>
        _recorder = SingleCallRecorder<string, LaResult>.Returning(result);

    /// <summary>Gets the number of times <see cref="EvaluateAsync"/> has been invoked.</summary>
    public int CallCount => _recorder.CallCount;

    /// <summary>Gets the reason passed to the most recent evaluation, or <see langword="null"/> if none.</summary>
    public string? LastReason => _recorder.LastArg;

    /// <inheritdoc />
    public Task<LaResult> EvaluateAsync(string reason, CancellationToken ct) =>
        Task.FromResult(_recorder.Record(reason));
}
