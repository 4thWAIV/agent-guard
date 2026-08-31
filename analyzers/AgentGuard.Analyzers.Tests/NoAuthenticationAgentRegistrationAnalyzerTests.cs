// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0111: a compile-time string constant whose value names a polkit authentication-agent registration or spawn is a
/// build error in production source; a test assembly may name the value in a fixture.
/// </summary>
public class NoAuthenticationAgentRegistrationAnalyzerTests
{
    [Fact]
    public async Task PkttyagentLiteral_InProduction_IsReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform.Linux
            {
                internal static class Agent
                {
                    internal static string Helper() => "pkttyagent";
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoAuthenticationAgentRegistrationAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
        Assert.Equal("AG0111", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task AuthenticationAgentInterfaceLiteral_InProduction_IsReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform.Linux
            {
                internal static class Agent
                {
                    private const string Interface = "org.freedesktop.PolicyKit1.AuthenticationAgent";
                    internal static string Name() => Interface;
                }
            }
            """;

        // The const declaration's initializer literal is caught (the field reference folds to the same banned value,
        // so both sites may report; at least the literal must).
        var diagnostics = await AnalyzerRunner.RunAsync<NoAuthenticationAgentRegistrationAnalyzer>(source, "AgentGuard.CrossPlatform.Linux");
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0111", diagnostic.Id));
    }

    [Fact]
    public async Task BannedLiteral_InTestAssembly_IsNotReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform.Tests
            {
                internal static class Fixture
                {
                    internal static string Helper() => "pkttyagent";
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoAuthenticationAgentRegistrationAnalyzer>(source, "AgentGuard.CrossPlatform.Tests"));
    }

    [Fact]
    public async Task UnrelatedLiteral_IsNotReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform.Linux
            {
                internal static class Agent
                {
                    internal static string Message() => "please authenticate to continue";
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoAuthenticationAgentRegistrationAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
    }
}
