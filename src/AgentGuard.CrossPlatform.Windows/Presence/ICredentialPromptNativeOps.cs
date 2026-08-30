// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The internal Windows CREDENTIAL-PROMPT native-ops owner — the thin, stateless seam holding the raw secure-desktop
/// credential prompt (<c>CredUIPromptForWindowsCredentials</c>, <c>CredUnPackAuthenticationBuffer</c>, <c>LogonUser</c>,
/// <c>CloseHandle</c>, and their <c>Marshal</c> uses), split out from below the <see cref="IWindowsUserPresence"/> flow
/// port. It is the second of the two Windows native-ops owners (macos-one-windows-two-native-ops): Hello and the
/// credential prompt are independent native subsystems sharing no handle. Its sole implementer
/// (<see cref="CredentialPromptNativeOps"/>) is where the relocated raw-interop exemption lives (AG0113) and it holds no
/// field (AG0116). Every method is a SYNCHRONOUS raw call with no branching (AG0106): the status/unpack/logon decisions
/// live in the fake-testable orchestrator, which composes these primitives and owns every buffer's lifetime.
///
/// It is marked <see cref="RequiresNativeSpecTestAttribute"/> — the production-side, single source of the guardrail-1
/// requirement that this native-ops owner have its own <c>[Trait("Category","NativeSpec")]</c> spec test (the Windows
/// spec test the IMPLEMENT step adds). <c>IWindowsHelloNativeOps</c> is interactive-only and is deliberately NOT marked.
/// </summary>
[RequiresNativeSpecTest]
internal interface ICredentialPromptNativeOps
{
    /// <summary>
    /// Shows the secure-desktop credential prompt (<c>CREDUIWIN_SECURE_PROMPT</c>) and returns its plain outcome — the
    /// status and the packed authentication buffer — for the orchestrator to branch on.
    /// </summary>
    /// <param name="reason">The reviewed prompt text shown to the human.</param>
    /// <returns>The prompt status and packed buffer.</returns>
    CredentialPromptOutcome Prompt(string reason);

    /// <summary>
    /// Unpacks a packed authentication buffer (<c>CredUnPackAuthenticationBuffer</c>) into per-field native buffers for
    /// validation.
    /// </summary>
    /// <param name="authBuffer">The packed buffer from <see cref="Prompt"/>.</param>
    /// <param name="authBufferSize">The size, in bytes, of <paramref name="authBuffer"/>.</param>
    /// <returns>The unpacked field buffers (and whether the unpack succeeded).</returns>
    CredentialFields Unpack(IntPtr authBuffer, uint authBufferSize);

    /// <summary>
    /// Validates the typed credential against the local account (<c>LogonUser</c> with <c>LOGON32_LOGON_NETWORK</c>),
    /// yielding the logon token on success for the orchestrator to close.
    /// </summary>
    /// <param name="user">The unpacked user-name buffer.</param>
    /// <param name="password">The unpacked password buffer.</param>
    /// <param name="token">The logon token when validation succeeds, else <see cref="IntPtr.Zero"/>.</param>
    /// <returns><see langword="true"/> when the credential validated; otherwise <see langword="false"/>.</returns>
    bool Logon(IntPtr user, IntPtr password, out IntPtr token);

    /// <summary>
    /// Closes a logon token returned by <see cref="Logon"/>.
    /// </summary>
    /// <param name="token">The logon token to close.</param>
    void CloseToken(IntPtr token);

    /// <summary>
    /// Zeroes the password buffer and frees the per-field native buffers of an unpacked credential.
    /// </summary>
    /// <param name="fields">The field buffers from <see cref="Unpack"/>.</param>
    void FreeFields(CredentialFields fields);

    /// <summary>
    /// Frees the packed authentication buffer (<c>CoTaskMemFree</c>) the prompt allocated.
    /// </summary>
    /// <param name="buffer">The packed buffer from <see cref="CredentialPromptOutcome.Buffer"/>.</param>
    void FreePromptBuffer(IntPtr buffer);
}
