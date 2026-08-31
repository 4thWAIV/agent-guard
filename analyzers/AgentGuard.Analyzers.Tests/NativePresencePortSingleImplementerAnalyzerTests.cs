// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0114: each internal per-OS native presence interface — the FLOW ports (ILocalAuthentication / IWindowsUserPresence
/// / IPolkitAuthority) AND the native-OPS owners the coverage refactor adds (IObjCRuntime / IWindowsHelloNativeOps /
/// ICredentialPromptNativeOps) — must have exactly one implementer; the second and later implementer is reported.
/// </summary>
public class NativePresencePortSingleImplementerAnalyzerTests
{
    // The macOS native-port stub is the shared owner SharedAnalyzerSources.LocalAuthenticationPort (spelled once,
    // consumed by the three native-port rule test classes).
    private const string MacPort = SharedAnalyzerSources.LocalAuthenticationPort;

    [Fact]
    public async Task SecondImplementer_IsReported()
    {
        string source = MacPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class RealLocalAuthentication : ILocalAuthentication { }
                internal sealed class ExtraLocalAuthentication : ILocalAuthentication { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativePresencePortSingleImplementerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0114", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task SingleImplementer_IsNotReported()
    {
        string source = MacPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class RealLocalAuthentication : ILocalAuthentication { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativePresencePortSingleImplementerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task PolkitPort_SecondImplementer_IsReported()
    {
        // The Linux native-port stub is the shared owner SharedAnalyzerSources.PolkitAuthorityPort; the two implementers
        // sit in a second Linux namespace block that sees the interface across the two declarations.
        string source = SharedAnalyzerSources.PolkitAuthorityPort + """

            namespace AgentGuard.CrossPlatform.Linux
            {
                internal sealed class TmdsPolkitAuthority : IPolkitAuthority { }
                internal sealed class ExtraPolkitAuthority : IPolkitAuthority { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativePresencePortSingleImplementerAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
        Assert.Equal("AG0114", diagnostic.Id);
    }

    [Fact]
    public async Task NativeOpsOwner_SecondImplementer_IsReported()
    {
        // The coverage refactor extends the single-implementer guard to the native-ops interfaces: a native-ops owner
        // (IObjCRuntime) must have exactly one thin implementer too, so a second one is reported. The native-ops stub is
        // the shared owner SharedAnalyzerSources.ObjCRuntimeNativeOps.
        string source = SharedAnalyzerSources.ObjCRuntimeNativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class RealObjCRuntime : IObjCRuntime { }
                internal sealed class ExtraObjCRuntime : IObjCRuntime { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NativePresencePortSingleImplementerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0114", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task NativeOpsOwner_SingleImplementer_IsNotReported()
    {
        string source = SharedAnalyzerSources.ObjCRuntimeNativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class RealObjCRuntime : IObjCRuntime { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NativePresencePortSingleImplementerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }
}
