// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0113 (presence-native-owner-rule, family-scoped; fence-relocation): presence-family native interop — a P/Invoke
/// that is not one of the filesystem-native bindings, or a Marshal use that is not inside the filesystem owner — is
/// allowed only in the per-OS native-OPS owner (IObjCRuntime / IWindowsHelloNativeOps / ICredentialPromptNativeOps). The
/// coverage refactor RELOCATED this exemption off the flow port (ILocalAuthentication / IWindowsUserPresence) and onto
/// the thin native-ops layer, so a raw presence call left in the flow-port orchestrator is now reported. The
/// filesystem-native family (PosixNativeMethods calls, the filesystem owner's Marshal) stays AG0101's and is skipped here.
/// </summary>
public class PresenceNativeInteropOwnerAnalyzerTests
{
    // The native-ops owner, the flow port, the filesystem owner interface, a presence-native binding, and a
    // filesystem-native binding. The IObjCRuntime native-ops owner, the ILocalAuthentication flow port, and the
    // LocalAuthNative objc_msgSend binding come from the shared owners (SharedAnalyzerSources); the bare
    // IPlatformFileSystem marker and the filesystem-native PosixNativeMethods binding stay inline (marker-stub
    // convention; PosixNativeMethods is this file's own filesystem-family fixture).
    private const string Fixtures = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IPlatformFileSystem { }
        }
        """
        + "\n\n" + SharedAnalyzerSources.ObjCRuntimeNativeOps
        + "\n\n" + SharedAnalyzerSources.LocalAuthenticationPort
        + "\n\n" + SharedAnalyzerSources.LocalAuthNativeBinding
        + "\n\n" + """
        namespace AgentGuard.CrossPlatform.Posix
        {
            using System.Runtime.InteropServices;
            internal static class PosixNativeMethods
            {
                [DllImport("libc")]
                internal static extern long PathConf(System.IntPtr path, int name);
            }
        }
        """;

    [Fact]
    public async Task PresencePInvoke_OutsidePort_IsReported()
    {
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class Rogue
                {
                    internal IntPtr Call() => LocalAuthNative.ObjcMsgSend(IntPtr.Zero, IntPtr.Zero);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0113", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task PresencePInvoke_InNativeOpsOwner_IsNotReported()
    {
        // The relocated exemption: raw presence P/Invoke is legal inside the native-ops owner (IObjCRuntime), the thin
        // raw-call layer the refactor introduces.
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal IntPtr Call() => LocalAuthNative.ObjcMsgSend(IntPtr.Zero, IntPtr.Zero);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task PresencePInvoke_InFlowPort_IsReported()
    {
        // fence-relocation: the flow port (ILocalAuthentication) is NO LONGER exempt — the raw call must move down into
        // the native-ops owner. A raw presence P/Invoke left in the orchestrator is a build error (the RED-first forcing
        // function that flags the pre-refactor LocalAuthentication raw calls).
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class LocalAuthentication : ILocalAuthentication
                {
                    internal IntPtr Call() => LocalAuthNative.ObjcMsgSend(IntPtr.Zero, IntPtr.Zero);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0113", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task FilesystemPInvoke_OutsidePort_IsNotReported()
    {
        // pathconf is the FILESYSTEM native family (AG0101's), so AG0113 skips it — no false positive on existing code.
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class CaseSensitivity
                {
                    internal long Query(IntPtr path) => AgentGuard.CrossPlatform.Posix.PosixNativeMethods.PathConf(path, 11);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task Marshal_OutsideOwners_IsReported()
    {
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Runtime.InteropServices;
                internal sealed class Rogue
                {
                    internal int LastError() => Marshal.GetLastPInvokeError();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0113", diagnostic.Id);
    }

    [Fact]
    public async Task Marshal_InFilesystemOwner_IsNotReported()
    {
        // The filesystem owner's Marshal.GetLastPInvokeError (read after a filesystem syscall) is AG0101's family.
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Runtime.InteropServices;
                using AgentGuard.Abstractions.Contracts;
                internal sealed class PosixFileSystem : IPlatformFileSystem
                {
                    internal int LastError() => Marshal.GetLastPInvokeError();
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task Marshal_InNativeOpsOwner_IsNotReported()
    {
        // The native-ops owner's Marshal is presence-family native interop, allowed there through the shared native-ops
        // owner check. Complements PresencePInvoke_InNativeOpsOwner_IsNotReported so BOTH native branches (P/Invoke and
        // Marshal) are proven clean in the native-ops owner under AG0113.
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Runtime.InteropServices;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal int LastError() => Marshal.GetLastPInvokeError();
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task Marshal_InFlowPort_IsReported()
    {
        // fence-relocation: the flow port (ILocalAuthentication) is no longer exempt on the Marshal branch either, so a
        // Marshal use left in the orchestrator is reported.
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System.Runtime.InteropServices;
                internal sealed class LocalAuthentication : ILocalAuthentication
                {
                    internal int LastError() => Marshal.GetLastPInvokeError();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0113", diagnostic.Id);
    }

    [Fact]
    public async Task PresencePInvoke_InLinuxAssembly_IsNotReported()
    {
        // Linux is native-call-free (managed D-Bus); AG0113 registers only in the macOS/Windows assemblies.
        string source = Fixtures + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class Rogue
                {
                    internal IntPtr Call() => LocalAuthNative.ObjcMsgSend(IntPtr.Zero, IntPtr.Zero);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PresenceNativeInteropOwnerAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
    }
}
