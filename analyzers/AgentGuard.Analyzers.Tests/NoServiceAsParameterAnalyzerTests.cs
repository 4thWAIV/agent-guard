// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class NoServiceAsParameterAnalyzerTests
{
    // AG0031's owner-interface set is now DERIVED from ISystemServices (derive-service-set-from-isystemservices), so
    // the fixture's container must expose IFileReader through an accessor for it to count as a service. The container
    // fixture is SharedAnalyzerSources.AbstractionsPrefix. The assertions below are unchanged from the hand-list era.
    [Fact]
    public async Task OrdinaryMethodTakingService_IsReported()
    {
        // Passing a lone service into a method is the service-locator smell; inject it through the constructor instead.
        string source = SharedAnalyzerSources.AbstractionsPrefix + """

            namespace App
            {
                public class Consumer
                {
                    public void Use(AgentGuard.Abstractions.Contracts.IFileReader reader) { }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0031", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Use", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConstructorTakingService_IsNotReported()
    {
        // Constructor injection is the sanctioned mechanism, so a constructor parameter is never flagged.
        string source = SharedAnalyzerSources.AbstractionsPrefix + """

            namespace App
            {
                public class Consumer
                {
                    private readonly AgentGuard.Abstractions.Contracts.IFileReader reader;

                    public Consumer(AgentGuard.Abstractions.Contracts.IFileReader reader) => this.reader = reader;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task MethodTakingService_AtCompositionPoint_IsNotReported()
    {
        // The composition method (the Program type in namespace AgentGuard.Cli) legitimately takes a service. It is
        // compiled into the CLI's REAL assembly name "guard" (<AssemblyName>guard</AssemblyName>), the name the
        // composition-point exemption matches on — not the root namespace.
        string source = SharedAnalyzerSources.AbstractionsPrefix + """

            namespace AgentGuard.Cli
            {
                internal static class Program
                {
                    private static void Compose(AgentGuard.Abstractions.Contracts.IFileReader reader) { }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "guard"));
    }

    [Fact]
    public async Task MethodTakingNonService_IsNotReported()
    {
        const string source = """
            namespace App
            {
                public class Consumer
                {
                    public void Use(string value) { }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
    }
}
