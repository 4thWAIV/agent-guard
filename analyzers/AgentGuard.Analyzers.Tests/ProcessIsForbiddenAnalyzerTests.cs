// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ProcessIsForbiddenAnalyzerTests
{
    private const string ProcessStartSource = """
        using System.Diagnostics;

        public class Sample
        {
            public void Launch() => Process.Start("guard");
        }
        """;

    [Fact]
    public async Task ProcessStart_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ProcessIsForbiddenAnalyzer>(ProcessStartSource, "AgentGuard.Engine"));
        Assert.Equal("AG0013", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task Process_IsReportedEvenInBoundaries_AllowedNowhere()
    {
        // Process launching is not a wrapped primitive; there is no assembly where it is allowed.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ProcessIsForbiddenAnalyzer>(ProcessStartSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0013", diagnostic.Id);
    }

    [Fact]
    public async Task ProcessStartInfoConstruction_IsReported()
    {
        const string source = """
            using System.Diagnostics;

            public class Sample
            {
                public ProcessStartInfo Info() => new ProcessStartInfo("guard");
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ProcessIsForbiddenAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0013", diagnostic.Id);
    }

    [Fact]
    public async Task NonProcessType_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public int Add(int a, int b) => a + b;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ProcessIsForbiddenAnalyzer>(source, "AgentGuard.Engine"));
    }
}
