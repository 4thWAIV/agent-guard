// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0107: a presence implementation (a class implementing IPresenceCheck) must not mint its own timeout — no
/// CancellationTokenSource, CancelAfter, Task.Delay, or Timer. The approval gate owns the one 60-second bound.
/// </summary>
public class NoTimeoutInPresenceImplAnalyzerTests
{
    // The IPresenceCheck/PresenceRequest stub is the shared owner SharedAnalyzerSources.PresenceContract (spelled once,
    // consumed by the three presence-rule test classes). A fixture here that declares an IPresenceCheck implementer
    // supplies the interface's Check method, because the analyzer runner requires each fixture to produce exactly the
    // compiler errors its call site declares and these call sites declare none. Other fixtures in this class
    // deliberately declare a class that does NOT implement IPresenceCheck — a native port owner, a native-ops owner,
    // or an ordinary class — because what the rule does with such a class is itself under test.
    private const string PresenceContract = SharedAnalyzerSources.PresenceContract;

    // The one IPresenceCheck member, spelled once and interpolated into each fixture below that declares an
    // IPresenceCheck implementer. The runner requires each fixture to produce exactly the compiler errors its call
    // site declares, and these declare none, so an implementer that omitted Check would fail the run. A fixture whose
    // class does not implement IPresenceCheck does not interpolate it.
    private const string PresenceCheckMember = """
                public int Check() => throw new System.NotImplementedException();
        """;

    [Fact]
    public async Task CancellationTokenSourceInPresenceImpl_IsReported()
    {
        string source = PresenceContract + $$"""

            namespace AgentGuard.CrossPlatform.Linux
            {
                using System;
                using System.Threading;
                using AgentGuard.Abstractions.Contracts;
                internal sealed class LinuxPresenceCheck : IPresenceCheck
                {
            {{PresenceCheckMember}}

                    internal CancellationTokenSource Arm() => new CancellationTokenSource(TimeSpan.FromSeconds(5));
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
        Assert.Equal("AG0107", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task TaskDelayInPresenceImpl_IsReported()
    {
        string source = PresenceContract + $$"""

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                using System.Threading.Tasks;
                using AgentGuard.Abstractions.Contracts;
                internal sealed class MacOsPresenceCheck : IPresenceCheck
                {
            {{PresenceCheckMember}}

                    internal Task Pause() => Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0107", diagnostic.Id);
    }

    [Fact]
    public async Task TaskWaitAsyncInPresenceImpl_IsReported()
    {
        // Task.WaitAsync mints a bounded wait; a presence impl may not introduce one — the gate owns the single bound.
        string source = PresenceContract + $$"""

            namespace AgentGuard.CrossPlatform.Windows
            {
                using System;
                using System.Threading.Tasks;
                using AgentGuard.Abstractions.Contracts;
                internal sealed class WindowsPresenceCheck : IPresenceCheck
                {
            {{PresenceCheckMember}}

                    internal Task Bounded(Task work) => work.WaitAsync(TimeSpan.FromSeconds(1));
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.Windows"));
        Assert.Equal("AG0107", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task TimeProviderCreateTimerInPresenceImpl_IsReportedByAg0107ButNotAg0038()
    {
        // TimeProvider.CreateTimer off the injected clock is exactly the path AG0038 mandates as correct, so AG0038 must
        // NOT flag it; but a presence impl must create no timeout at all, so AG0107 must. The same source proves both.
        string source = PresenceContract + $$"""

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                using System.Threading;
                using AgentGuard.Abstractions.Contracts;
                internal sealed class MacOsPresenceCheck : IPresenceCheck
                {
            {{PresenceCheckMember}}

                    internal ITimer Arm(TimeProvider clock) =>
                        clock.CreateTimer(_ => { }, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0107", diagnostic.Id);

        // AG0038 (timeout-must-use-TimeProvider) leaves CreateTimer alone — it IS the injected-clock timer path.
        Assert.Empty(await AnalyzerRunner.RunAsync<TimeoutMustUseTimeProviderAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task TimeoutInMacOsNativePortOwner_IsReported()
    {
        // FIX 3: the port layer one level below IPresenceCheck is gated too. A class owning the macOS native port
        // (ILocalAuthentication) that wraps its own CancellationTokenSource timeout around the native call is caught,
        // even though it implements the port, not IPresenceCheck. The port stub is the shared owner
        // SharedAnalyzerSources.LocalAuthenticationPort; the implementer sits in a second MacOS namespace block.
        string source = SharedAnalyzerSources.LocalAuthenticationPort + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                using System.Threading;
                internal sealed class MacLocalAuthentication : ILocalAuthentication
                {
                    internal CancellationTokenSource Arm() => new CancellationTokenSource(TimeSpan.FromSeconds(5));
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0107", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task TimeoutInLinuxPolkitPortOwner_IsReported()
    {
        // FIX 3: the Linux polkit port (IPolkitAuthority) is a presence port too; a Task.Delay wrapped around the
        // D-Bus call is caught even though the port does not implement IPresenceCheck. The port stub is the shared owner
        // SharedAnalyzerSources.PolkitAuthorityPort; the implementer sits in a second Linux namespace block.
        string source = SharedAnalyzerSources.PolkitAuthorityPort + """

            namespace AgentGuard.CrossPlatform.Linux
            {
                using System;
                using System.Threading.Tasks;
                internal sealed class TmdsPolkitAuthority : IPolkitAuthority
                {
                    internal Task Pause() => Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
        Assert.Equal("AG0107", diagnostic.Id);
    }

    [Fact]
    public async Task TimeoutOutsidePresenceImpl_IsNotReported()
    {
        // A class that does NOT implement IPresenceCheck is not this rule's concern (AG0038 governs its waits).
        string source = PresenceContract + """

            namespace App
            {
                using System;
                using System.Threading;
                internal sealed class Ordinary
                {
                    internal CancellationTokenSource Arm() => new CancellationTokenSource(TimeSpan.FromSeconds(5));
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
    }

    [Fact]
    public async Task PresenceImplWithoutTimeout_IsNotReported()
    {
        string source = PresenceContract + $$"""

            namespace AgentGuard.CrossPlatform.Linux
            {
                using AgentGuard.Abstractions.Contracts;
                internal sealed class LinuxPresenceCheck : IPresenceCheck
                {
            {{PresenceCheckMember}}

                    internal int Value() => 42;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
    }

    [Fact]
    public async Task TimeoutInMacOsNativeOpsOwner_IsReported()
    {
        // The coverage refactor extends AG0107 DOWN to the native-ops layer: a class owning the macOS native-ops
        // interface (IObjCRuntime) that wraps its own CancellationTokenSource timeout around the raw call is caught too —
        // the gate owns the one bound at every layer, native-ops included. The native-ops stub is the shared owner
        // SharedAnalyzerSources.ObjCRuntimeNativeOps; the implementer sits in a second MacOS namespace block.
        string source = SharedAnalyzerSources.ObjCRuntimeNativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                using System.Threading;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal CancellationTokenSource Arm() => new CancellationTokenSource(TimeSpan.FromSeconds(5));
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoTimeoutInPresenceImplAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0107", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }
}
