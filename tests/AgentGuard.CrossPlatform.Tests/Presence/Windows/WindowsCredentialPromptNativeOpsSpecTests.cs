// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.CrossPlatform.Windows;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Windows native-ops pointed-integration spec test (guardrail-1-nativespec-convention-test,
/// windows-credential-roundtrip-seam). It drives the REAL <see cref="CredentialPromptNativeOps"/> through its
/// NON-interactive operations end to end: pack a plaintext user/password into an authentication buffer
/// (<see cref="CredentialPromptNativeOps.TryPackCredential"/>, the OS counterpart to the prompt's packed buffer), unpack
/// it back into the per-field buffers (<see cref="ICredentialPromptNativeOps.Unpack"/>), validate a made-up nonexistent
/// local account against the machine (<see cref="ICredentialPromptNativeOps.Logon"/> — which must fail), then close the
/// token and free every buffer (<c>CloseToken</c> / <c>FreeFields</c> / <c>FreePromptBuffer</c>). It never calls
/// <see cref="ICredentialPromptNativeOps.Prompt"/>, the one operation that shows the secure-desktop dialog, so no prompt
/// is shown and it is safe on a headless CI runner. This is the real-boundary coverage a unit test over the fake cannot
/// give.
///
/// It drives real Win32 (credui.dll / advapi32.dll), so it PASSES only on the <c>windows-latest</c> CI leg; on a
/// non-Windows host the win-x64 cross-sim throws <c>DllNotFoundException</c> at the first native call, exactly like the
/// other real-boundary Windows presence tests. It is deliberately NOT OS-skipped (AG0026): the Windows leg is where it
/// runs. Compiled only on the Windows CI leg (it constructs the internal Windows
/// <see cref="CredentialPromptNativeOps"/> through the per-OS <c>InternalsVisibleTo</c> grant), and — like the macOS
/// <c>MacOsObjCRuntimeSpecTests</c> — it is NOT behind the interactive native-smoke opt-in because it shows no prompt.
///
/// It is tagged <see cref="NativeSpecOwnerAttribute"/> for <see cref="ICredentialPromptNativeOps"/> so the guardrail-1
/// convention test (<see cref="NativeSpecCoverageConventionTests"/>) recognizes this as the Windows native-ops owner's
/// spec test.
/// </summary>
[NativeSpecOwner(typeof(ICredentialPromptNativeOps))]
public sealed class WindowsCredentialPromptNativeOpsSpecTests
{
    // A made-up local account name that cannot exist on the CI runner, so LogonUser rejects it, and a stand-in password.
    private const string MadeUpAccountName = "AgentGuardSpecNoSuchLocalAccount";
    private const string StandInPassword = "not-a-real-password";

    [Fact]
    [Trait(NativeSpecOwnerAttribute.CategoryTraitName, NativeSpecOwnerAttribute.NativeSpecTraitValue)]
    public void RealCredentialPromptNativeOps_PacksUnpacksAndRejectsAMadeUpLocalAccount()
    {
        var ops = new CredentialPromptNativeOps();

        bool packed = CredentialPromptNativeOps.TryPackCredential(
            MadeUpAccountName, StandInPassword, out IntPtr buffer, out uint bufferSize);
        packed.Should().BeTrue("a plaintext user name and password pack into an authentication buffer");

        CredentialFields fields = ops.Unpack(buffer, bufferSize);
        try
        {
            fields.Success.Should().BeTrue("the packed authentication buffer unpacks back into the per-field buffers");

            bool loggedOn = ops.Logon(fields.User, fields.Password, out IntPtr token);
            loggedOn.Should().BeFalse("a made-up nonexistent local account must not validate against the machine");

            ops.CloseToken(token);
        }
        finally
        {
            ops.FreeFields(fields);
            ops.FreePromptBuffer(buffer);
        }
    }
}
