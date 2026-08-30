// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.CrossPlatform.Windows;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A stateful fake of the internal Windows HELLO native-ops seam (<see cref="IWindowsHelloNativeOps"/>) for the
/// orchestrator-flow test (<c>WindowsUserPresenceFlowTests</c>, contract-coverage-refactor acceptance #5). It stands in
/// for the live WinRT <c>UserConsentVerifier</c> interop so the REAL <see cref="WindowsUserPresence"/> orchestrator can
/// be driven through the Hello branches — verified, cancelled, busy, retries-exhausted, and the unavailable /
/// cannot-bind paths that fall back to the credential prompt — with no Hello device. Modelling the OS, its
/// <see cref="BeginVerification"/> invokes the orchestrator's completed callback SYNCHRONOUSLY with the scripted async
/// status; the orchestrator then reads <see cref="GetVerificationResult"/> for the raw consent code. It is stateful,
/// which is legal in <c>AgentGuard.CrossPlatform.Tests</c> because AG0116/AG0106/AG0115 register only in the Windows and
/// macOS PRODUCTION assemblies; per-method call counts compose the shared <see cref="SingleCallRecorder{TArg,TResult}"/>
/// (reuse-ledger).
/// </summary>
internal sealed class FakeWindowsHelloNativeOps : IWindowsHelloNativeOps
{
    // A non-zero sentinel async-operation handle the orchestrator threads through and releases.
    internal static readonly IntPtr OperationHandle = new(0x510);

    private readonly int _asyncStatus;

    private readonly SingleCallRecorder<string, IntPtr> _begin;
    private readonly SingleCallRecorder<IntPtr, int> _getResult;
    private readonly SingleCallRecorder<IntPtr, bool> _release;

    private FakeWindowsHelloNativeOps(int asyncStatus, int verificationResult, Func<Exception>? bindFault)
    {
        _asyncStatus = asyncStatus;
        _begin = bindFault is null
            ? SingleCallRecorder<string, IntPtr>.Returning(OperationHandle)
            : SingleCallRecorder<string, IntPtr>.Faulting(bindFault);
        _getResult = SingleCallRecorder<IntPtr, int>.Returning(verificationResult);
        _release = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
    }

    /// <summary>Gets the number of async-operation releases.</summary>
    internal int OperationsReleased => _release.CallCount;

    /// <summary>Gets the prompt passed to the most recent verification, or <see langword="null"/> if none.</summary>
    internal string? LastReason => _begin.LastArg;

    /// <inheritdoc />
    public IntPtr BeginVerification(string reason, Action<int> onCompleted)
    {
        ArgumentNullException.ThrowIfNull(onCompleted);
        IntPtr operation = _begin.Record(reason);
        onCompleted(_asyncStatus);
        return operation;
    }

    /// <inheritdoc />
    public int GetVerificationResult(IntPtr asyncOperation) => _getResult.Record(asyncOperation);

    /// <inheritdoc />
    public void ReleaseOperation(IntPtr asyncOperation) => _release.Record(asyncOperation);

    /// <summary>Hello answers: the request completes with <paramref name="asyncStatus"/> and yields <paramref name="verificationResult"/>.</summary>
    /// <param name="asyncStatus">The raw WinRT <c>AsyncStatus</c> the completed handler reports.</param>
    /// <param name="verificationResult">The raw <c>UserConsentVerificationResult</c> code <c>GetResults</c> returns.</param>
    /// <returns>The configured fake.</returns>
    internal static FakeWindowsHelloNativeOps Answers(int asyncStatus, int verificationResult) =>
        new(asyncStatus, verificationResult, bindFault: null);

    /// <summary>Hello cannot bind on this host: <see cref="BeginVerification"/> throws, so the orchestrator falls back.</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeWindowsHelloNativeOps CannotBind() =>
        new(asyncStatus: 0, verificationResult: 0, bindFault: () => new InvalidOperationException("Hello cannot bind"));
}
