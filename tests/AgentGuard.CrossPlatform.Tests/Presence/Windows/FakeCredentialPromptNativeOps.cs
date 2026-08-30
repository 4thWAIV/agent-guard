// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.CrossPlatform.Windows;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// A stateful fake of the internal Windows CREDENTIAL-PROMPT native-ops seam (<see cref="ICredentialPromptNativeOps"/>)
/// for the orchestrator-flow test (<c>WindowsUserPresenceFlowTests</c>, contract-coverage-refactor acceptance #5). It
/// stands in for the live secure-desktop prompt / <c>LogonUser</c> so the REAL <see cref="WindowsUserPresence"/>
/// orchestrator's fallback path can be driven through its branches — success, wrong password, cancel, no method, and an
/// unpack failure — with no interactive surface. It returns the scripted <see cref="CredentialPromptOutcome"/> and
/// <see cref="CredentialFields"/> and records the cleanup calls (unpack, logon, close-token, free-fields,
/// free-buffer) so the flow test can assert the orchestrator both branched correctly AND released what it acquired. It
/// is stateful, legal in <c>AgentGuard.CrossPlatform.Tests</c> because AG0116/AG0106/AG0115 register only in the
/// PRODUCTION per-OS assemblies; per-method call counts compose the shared <see cref="SingleCallRecorder{TArg,TResult}"/>
/// (reuse-ledger).
/// </summary>
internal sealed class FakeCredentialPromptNativeOps : ICredentialPromptNativeOps
{
    internal const uint BufferSize = 128;

    // Non-zero sentinel handles the orchestrator threads through and frees.
    internal static readonly IntPtr BufferHandle = new(0x710);
    internal static readonly IntPtr UserHandle = new(0x720);
    internal static readonly IntPtr DomainHandle = new(0x730);
    internal static readonly IntPtr PasswordHandle = new(0x740);
    internal static readonly IntPtr TokenHandle = new(0x750);

    private readonly SingleCallRecorder<string, CredentialPromptOutcome> _prompt;
    private readonly SingleCallRecorder<IntPtr, CredentialFields> _unpack;
    private readonly SingleCallRecorder<IntPtr, bool> _logon;
    private readonly SingleCallRecorder<IntPtr, bool> _closeToken;
    private readonly SingleCallRecorder<bool, bool> _freeFields;
    private readonly SingleCallRecorder<IntPtr, bool> _freeBuffer;

    private FakeCredentialPromptNativeOps(
        CredentialPromptOutcome promptOutcome, CredentialFields unpacked, bool logonSucceeds)
    {
        _prompt = SingleCallRecorder<string, CredentialPromptOutcome>.Returning(promptOutcome);
        _unpack = SingleCallRecorder<IntPtr, CredentialFields>.Returning(unpacked);
        _logon = SingleCallRecorder<IntPtr, bool>.Returning(logonSucceeds);
        _closeToken = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
        _freeFields = SingleCallRecorder<bool, bool>.Returning(result: false);
        _freeBuffer = SingleCallRecorder<IntPtr, bool>.Returning(result: false);
    }

    /// <summary>Gets whether the packed prompt buffer was freed.</summary>
    internal bool PromptBufferFreed => _freeBuffer.CallCount > 0;

    /// <summary>Gets the number of times the typed credential's fields were freed (password zeroed).</summary>
    internal int FieldsFreed => _freeFields.CallCount;

    /// <summary>Gets the number of buffers unpacked.</summary>
    internal int Unpacks => _unpack.CallCount;

    /// <summary>Gets the number of logon tokens closed.</summary>
    internal int TokensClosed => _closeToken.CallCount;

    /// <summary>Gets the prompt passed to the most recent prompt, or <see langword="null"/> if none.</summary>
    internal string? LastReason => _prompt.LastArg;

    /// <inheritdoc />
    public CredentialPromptOutcome Prompt(string reason) => _prompt.Record(reason);

    /// <inheritdoc />
    public CredentialFields Unpack(IntPtr authBuffer, uint authBufferSize) => _unpack.Record(authBuffer);

    /// <inheritdoc />
    public bool Logon(IntPtr user, IntPtr password, out IntPtr token)
    {
        bool ok = _logon.Record(user);
        token = ok ? TokenHandle : IntPtr.Zero;
        return ok;
    }

    /// <inheritdoc />
    public void CloseToken(IntPtr token) => _closeToken.Record(token);

    /// <inheritdoc />
    public void FreeFields(CredentialFields fields) => _freeFields.Record(fields.Success);

    /// <inheritdoc />
    public void FreePromptBuffer(IntPtr buffer) => _freeBuffer.Record(buffer);

    /// <summary>The human dismissed the secure-desktop prompt (<c>ERROR_CANCELLED</c>, no buffer).</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeCredentialPromptNativeOps Cancelled() =>
        new(new CredentialPromptOutcome(CredentialStatus.ErrorCancelled, IntPtr.Zero, 0u), UnpackFailure(), logonSucceeds: false);

    /// <summary>No interactive secure desktop was available (a non-success status, no buffer) — a passwordless / headless host.</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeCredentialPromptNativeOps NoInteractiveSurface() =>
        new(new CredentialPromptOutcome(CredentialStatus.OtherFailure, IntPtr.Zero, 0u), UnpackFailure(), logonSucceeds: false);

    /// <summary>The prompt succeeded with a buffer; the typed credential unpacks and validates, so the human is verified.</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeCredentialPromptNativeOps PasswordValidates() =>
        new(SuccessOutcome(), UnpackSuccess(), logonSucceeds: true);

    /// <summary>The prompt succeeded with a buffer; the typed credential unpacks but fails validation (a wrong password).</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeCredentialPromptNativeOps PasswordRejected() =>
        new(SuccessOutcome(), UnpackSuccess(), logonSucceeds: false);

    /// <summary>The prompt succeeded with a buffer but it cannot be unpacked, so the orchestrator maps it to an error.</summary>
    /// <returns>The configured fake.</returns>
    internal static FakeCredentialPromptNativeOps UnpackFails() =>
        new(SuccessOutcome(), UnpackFailure(), logonSucceeds: false);

    private static CredentialPromptOutcome SuccessOutcome() =>
        new(CredentialStatus.ErrorSuccess, BufferHandle, BufferSize);

    private static CredentialFields UnpackSuccess() =>
        new(Success: true, UserHandle, DomainHandle, PasswordHandle);

    private static CredentialFields UnpackFailure() =>
        new(Success: false, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
}
