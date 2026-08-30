// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0112: a class implementing a native presence port (ILocalAuthentication / IWindowsUserPresence) must not cache a
/// native auth-context handle (IntPtr/nint/SafeHandle) in a field — the handle is created per Check call.
/// </summary>
public class NoCachedNativeAuthContextAnalyzerTests
{
    // The macOS native-port stub is the shared owner SharedAnalyzerSources.LocalAuthenticationPort (spelled once,
    // consumed by the three native-port rule test classes).
    private const string MacPort = SharedAnalyzerSources.LocalAuthenticationPort;

    [Fact]
    public async Task IntPtrFieldInPort_IsReported()
    {
        string source = MacPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class LocalAuthentication : ILocalAuthentication
                {
                    private IntPtr _context;
                    internal IntPtr Context => _context;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoCachedNativeAuthContextAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0112", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task StaticIntPtrFieldInPort_IsReported()
    {
        string source = MacPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class LocalAuthentication : ILocalAuthentication
                {
                    private static IntPtr s_context;
                    internal static IntPtr Context => s_context;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoCachedNativeAuthContextAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0112", diagnostic.Id);
    }

    [Fact]
    public async Task IntPtrFieldOutsidePort_IsNotReported()
    {
        string source = MacPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class Ordinary
                {
                    private IntPtr _handle;
                    internal IntPtr Handle => _handle;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoCachedNativeAuthContextAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task PortWithNoHandleField_IsNotReported()
    {
        string source = MacPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class LocalAuthentication : ILocalAuthentication
                {
                    private readonly string _reason = "authenticate";
                    internal string Reason => _reason;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoCachedNativeAuthContextAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task CachedHandleInTestAssembly_IsNotReported()
    {
        // The GUARD for the production-only gate (mirrors AG0116's StatefulNativeOpsInTestAssembly_IsNotReported): the
        // SAME handle-caching shape IntPtrFieldInPort_IsReported reports when compiled into the macOS production assembly
        // — a native IntPtr field on an ILocalAuthentication implementer — is NOT reported when the class is compiled into
        // a non-production (test) assembly. AgentGuard.CrossPlatform.Tests is where a presence-port fake can implement the
        // internal port interface through the verified InternalsVisibleTo grants, so the internal-interface visibility
        // alone would NOT keep AG0112 off it; the production-assembly gate baked into FieldOwnershipScan.RegisterField
        // (PresenceContracts.RegisterInMacOsOrWindows) does. This pins that AG0112 fires only inside the macOS/Windows
        // production assemblies, letting a test fake hold the handle it needs to observe.
        string source = MacPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class FakeLocalAuthentication : ILocalAuthentication
                {
                    private IntPtr _context;
                    internal IntPtr Context => _context;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoCachedNativeAuthContextAnalyzer>(source, "AgentGuard.CrossPlatform.Tests"));
    }
}
