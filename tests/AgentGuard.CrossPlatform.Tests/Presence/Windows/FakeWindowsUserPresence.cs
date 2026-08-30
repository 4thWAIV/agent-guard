// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.CrossPlatform.Windows;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A fake of the internal Windows native port (<see cref="IWindowsUserPresence"/>) for the <c>WindowsPresenceCheck</c>
/// unit test: it records how many times <see cref="VerifyAsync"/> was called and the reason it was given, and returns a
/// configured <see cref="WindowsPresenceResult"/>. It touches no native library. It is the single source implementer of
/// the port in this test compilation (AG0114 permits one). The call-count/last-argument capture is not hand-built here —
/// it composes the shared <see cref="SingleCallRecorder{TArg,TResult}"/>.
/// </summary>
internal sealed class FakeWindowsUserPresence : IWindowsUserPresence, IRecordingPresencePortFake
{
    private readonly SingleCallRecorder<string, WindowsPresenceResult> _recorder;

    public FakeWindowsUserPresence(WindowsPresenceResult result) =>
        _recorder = SingleCallRecorder<string, WindowsPresenceResult>.Returning(result);

    public int CallCount => _recorder.CallCount;

    public string? LastReason => _recorder.LastArg;

    public Task<WindowsPresenceResult> VerifyAsync(string reason, CancellationToken ct) =>
        Task.FromResult(_recorder.Record(reason));
}
