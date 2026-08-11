// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ConsoleOnlyInBoundariesAnalyzerTests
{
    private const string WriteLineSource = """
        using System;

        public class Sample
        {
            public void Say() => Console.WriteLine("hi");
        }
        """;

    private const string ConsoleOutSource = """
        using System;

        public class Sample
        {
            public void Say() => Console.Out.Write("hi");
        }
        """;

    [Fact]
    public async Task ConsoleWriteLine_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(WriteLineSource, "AgentGuard.Cli"));
        Assert.Equal("AG0016", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ConsoleOut_OutsideBoundaries_IsReported()
    {
        // A read of the Console.Out stream is caught the same as a direct Console call.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(ConsoleOutSource, "AgentGuard.Cli"));
        Assert.Equal("AG0016", diagnostic.Id);
    }

    [Fact]
    public async Task ConsoleWriteLine_InBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<ConsoleOnlyInBoundariesAnalyzer>(WriteLineSource, "AgentGuard.Boundaries"));
    }
}
