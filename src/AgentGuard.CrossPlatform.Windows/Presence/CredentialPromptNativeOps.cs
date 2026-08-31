// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The sole implementer of <see cref="ICredentialPromptNativeOps"/> and the ONE class that binds the secure-desktop
/// credential prompt (<c>CredUIPromptForWindowsCredentials</c> + <c>LogonUser</c>) — the Windows credential-prompt
/// native-ops owner (AG0113 confines its raw P/Invoke and <c>Marshal</c> here; AG0114 permits one implementer). It is
/// fully stateless (AG0116): every native buffer it returns is a per-call local the <see cref="WindowsUserPresence"/>
/// orchestrator owns and frees, and it holds no field but compile-time <c>const</c>s. Each method is a straight-line raw
/// call with no branching (AG0106) and no task-completion wiring (AG0115): the status/unpack/logon decisions live in the
/// fake-testable orchestrator, which composes these primitives and owns every buffer's lifetime.
/// </summary>
internal sealed partial class CredentialPromptNativeOps : ICredentialPromptNativeOps
{
    private const string CredUiLibrary = "credui.dll";
    private const string AdvApiLibrary = "advapi32.dll";

    private const string CredentialCaption = "AgentGuard";

    // Credential-prompt flags.
    private const uint CredUiWinGeneric = 0x00000001;
    private const uint CredUiWinSecurePrompt = 0x00001000;

    // LogonUser (advapi32): LOGON32_LOGON_NETWORK = 3, LOGON32_PROVIDER_DEFAULT = 0.
    private const uint Logon32LogonNetwork = 3;
    private const uint Logon32ProviderDefault = 0;

    // The account domain LogonUser validates against — the local machine (windows-hello-or-password).
    private const string LocalDomain = ".";

    // A generous fixed size for the unpacked user/domain/password buffers, in characters.
    private const int CredentialFieldChars = 512;

    /// <inheritdoc />
    public CredentialPromptOutcome Prompt(string reason)
    {
        IntPtr messageText = Marshal.StringToHGlobalUni(reason);
        IntPtr captionText = Marshal.StringToHGlobalUni(CredentialCaption);
        var uiInfo = new CredUiInfo
        {
            Size = Marshal.SizeOf<CredUiInfo>(),
            Parent = IntPtr.Zero,
            MessageText = messageText,
            CaptionText = captionText,
            Banner = IntPtr.Zero,
        };
        try
        {
            uint authPackage = 0;
            int save = 0;
            uint status = CredUIPromptForWindowsCredentials(
                ref uiInfo,
                0,
                ref authPackage,
                IntPtr.Zero,
                0,
                out IntPtr outBuffer,
                out uint outBufferSize,
                ref save,
                CredUiWinGeneric | CredUiWinSecurePrompt);

            return new CredentialPromptOutcome(status, outBuffer, outBufferSize);
        }
        finally
        {
            Marshal.FreeHGlobal(messageText);
            Marshal.FreeHGlobal(captionText);
        }
    }

    /// <inheritdoc />
    public CredentialFields Unpack(IntPtr authBuffer, uint authBufferSize)
    {
        IntPtr userName = AllocFieldBuffer();
        IntPtr domain = AllocFieldBuffer();
        IntPtr password = AllocFieldBuffer();

        uint userLen = CredentialFieldChars;
        uint domainLen = CredentialFieldChars;
        uint passwordLen = CredentialFieldChars;
        bool unpacked = CredUnPackAuthenticationBuffer(
            0, authBuffer, authBufferSize, userName, ref userLen, domain, ref domainLen, password, ref passwordLen);

        return new CredentialFields(unpacked, userName, domain, password);
    }

    /// <inheritdoc />
    public bool Logon(IntPtr user, IntPtr password, out IntPtr token)
    {
        IntPtr localDomain = Marshal.StringToHGlobalUni(LocalDomain);
        try
        {
            return LogonUser(user, localDomain, password, Logon32LogonNetwork, Logon32ProviderDefault, out token);
        }
        finally
        {
            Marshal.FreeHGlobal(localDomain);
        }
    }

    /// <inheritdoc />
    public void CloseToken(IntPtr token)
    {
        // Own the raw logon token in a SafeFileHandle and dispose it at once: on Windows its release closes the handle
        // through CloseHandle — the exception-safe .NET pattern for closing a Win32 HANDLE, so no raw CloseHandle
        // P/Invoke is needed and the native-ops layer stays branch-free (AG0106).
        using var handle = new SafeFileHandle(token, ownsHandle: true);
    }

    /// <inheritdoc />
    public void FreeFields(CredentialFields fields)
    {
        ZeroFieldBuffer(fields.Password);
        Marshal.FreeHGlobal(fields.User);
        Marshal.FreeHGlobal(fields.Domain);
        Marshal.FreeHGlobal(fields.Password);
    }

    /// <inheritdoc />
    public void FreePromptBuffer(IntPtr buffer) => Marshal.FreeCoTaskMem(buffer);

    /// <summary>
    /// Packs a plaintext user name and password into a single authentication buffer
    /// (<c>CredPackAuthenticationBufferW</c>) — the OS counterpart to <see cref="Unpack"/> — into a <c>CoTaskMem</c> block
    /// sized on <see cref="CredentialFieldChars"/> so the existing <see cref="FreePromptBuffer"/> frees it. It is the
    /// round-trip seam the Windows native-ops spec test drives (pack → unpack → logon) to cover this owner end to end
    /// without showing the interactive prompt. It is deliberately NOT on <see cref="ICredentialPromptNativeOps"/>, so the
    /// production orchestrator — which only ever receives a prompt-produced buffer — can never reach it. Straight-line
    /// (AG0106): allocate, size, one raw call.
    /// </summary>
    /// <param name="userName">The plaintext user name to pack.</param>
    /// <param name="password">The plaintext password to pack.</param>
    /// <param name="buffer">The packed authentication buffer (a <c>CoTaskMem</c> block freed by <see cref="FreePromptBuffer"/>).</param>
    /// <param name="bufferSize">The size, in bytes, of <paramref name="buffer"/>.</param>
    /// <returns><see langword="true"/> when the credential packed; otherwise <see langword="false"/>.</returns>
    internal static bool TryPackCredential(string userName, string password, out IntPtr buffer, out uint bufferSize)
    {
        int packedBytes = CredentialFieldChars * sizeof(char);
        buffer = Marshal.AllocCoTaskMem(packedBytes);
        bufferSize = (uint)packedBytes;
        return CredPackAuthenticationBuffer(0, userName, password, buffer, ref bufferSize);
    }

    private static IntPtr AllocFieldBuffer() => Marshal.AllocHGlobal(CredentialFieldChars * sizeof(char));

    // Overwrite the password buffer with zeros. A zero-filled managed array copied over the native buffer clears it
    // without a loop, keeping the native-ops layer branch-free (AG0106); the orchestrator never hands a zero buffer here
    // on the real path (the unpack always allocates the field buffers).
    private static void ZeroFieldBuffer(IntPtr buffer)
    {
        byte[] zeros = new byte[CredentialFieldChars * sizeof(char)];
        Marshal.Copy(zeros, 0, buffer, zeros.Length);
    }

    [LibraryImport(CredUiLibrary, EntryPoint = "CredUIPromptForWindowsCredentialsW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint CredUIPromptForWindowsCredentials(
        ref CredUiInfo uiInfo,
        uint authError,
        ref uint authPackage,
        IntPtr inAuthBuffer,
        uint inAuthBufferSize,
        out IntPtr outAuthBuffer,
        out uint outAuthBufferSize,
        ref int save,
        uint flags);

    [LibraryImport(CredUiLibrary, EntryPoint = "CredUnPackAuthenticationBufferW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredUnPackAuthenticationBuffer(
        uint flags,
        IntPtr authBuffer,
        uint authBufferSize,
        IntPtr userName,
        ref uint maxUserName,
        IntPtr domainName,
        ref uint maxDomainName,
        IntPtr password,
        ref uint maxPassword);

    [LibraryImport(AdvApiLibrary, EntryPoint = "LogonUserW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LogonUser(
        IntPtr userName, IntPtr domain, IntPtr password, uint logonType, uint logonProvider, out IntPtr token);

    [LibraryImport(CredUiLibrary, EntryPoint = "CredPackAuthenticationBufferW", StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredPackAuthenticationBuffer(
        uint flags,
        string userName,
        string password,
        IntPtr packedCredentials,
        ref uint packedCredentialsSize);

    // The CREDUI_INFOW structure passed to the credential prompt; carries the reviewed message and a caption.
    [StructLayout(LayoutKind.Sequential)]
    private struct CredUiInfo
    {
        public int Size;
        public IntPtr Parent;
        public IntPtr MessageText;
        public IntPtr CaptionText;
        public IntPtr Banner;
    }
}
