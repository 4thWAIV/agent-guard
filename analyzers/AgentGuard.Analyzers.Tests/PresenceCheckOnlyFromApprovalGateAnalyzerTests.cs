// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0108: in production (src/**) IPresenceCheck.Check may be invoked only from AgentGuard.Setup.ApprovalGate. A test
/// assembly is exempt so the per-OS native smoke can call Check directly.
/// </summary>
public class PresenceCheckOnlyFromApprovalGateAnalyzerTests
{
    // The IPresenceCheck/PresenceRequest stub is the shared owner SharedAnalyzerSources.PresenceContract (spelled once,
    // consumed by the three presence-rule test classes).
    private const string PresenceContract = SharedAnalyzerSources.PresenceContract;

    [Fact]
    public async Task CheckFromOtherType_InProduction_IsReported()
    {
        string source = PresenceContract + """

            namespace AgentGuard.Setup
            {
                using AgentGuard.Abstractions.Contracts;
                internal sealed class InstallCommand
                {
                    internal int Run(IPresenceCheck presence) => presence.Check();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PresenceCheckOnlyFromApprovalGateAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0108", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CheckThroughConcreteReference_InProduction_IsReported()
    {
        // ROUND 5 FIX 1: a Check call made through a CONCRETE-typed reference (LinuxPresenceCheck) — not the interface —
        // is the same trust boundary and must still be caught outside the gate; the recognizer now resolves interface
        // membership through OwnerClass.Implements (AllInterfaces), so the concrete-reference bypass is closed.
        string source = PresenceContract + """

            namespace AgentGuard.CrossPlatform.Linux
            {
                using AgentGuard.Abstractions.Contracts;
                internal sealed class LinuxPresenceCheck : IPresenceCheck
                {
                    public int Check() => 0;
                }
            }

            namespace AgentGuard.Setup
            {
                using AgentGuard.CrossPlatform.Linux;
                internal sealed class InstallCommand
                {
                    internal int Run(LinuxPresenceCheck presence) => presence.Check();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PresenceCheckOnlyFromApprovalGateAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0108", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CheckFromApprovalGate_IsNotReported()
    {
        string source = PresenceContract + """

            namespace AgentGuard.Setup
            {
                using AgentGuard.Abstractions.Contracts;
                internal static class ApprovalGate
                {
                    internal static int RequireApproval(IPresenceCheck presence) => presence.Check();
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PresenceCheckOnlyFromApprovalGateAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task CheckFromOtherType_InTestAssembly_IsNotReported()
    {
        // The CrossPlatform.Tests native smoke calls the real Check directly; a *.Tests assembly is exempt.
        string source = PresenceContract + """

            namespace AgentGuard.CrossPlatform.Tests
            {
                using AgentGuard.Abstractions.Contracts;
                internal sealed class Smoke
                {
                    internal int Run(IPresenceCheck presence) => presence.Check();
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PresenceCheckOnlyFromApprovalGateAnalyzer>(source, "AgentGuard.CrossPlatform.Tests"));
    }
}
