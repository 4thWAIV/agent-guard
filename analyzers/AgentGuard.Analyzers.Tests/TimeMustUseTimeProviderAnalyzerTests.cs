// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class TimeMustUseTimeProviderAnalyzerTests
{
    private const string UtcNowSource = """
        using System;

        public class Sample
        {
            public DateTime When() => DateTime.UtcNow;
        }
        """;

    [Fact]
    public async Task DateTimeUtcNow_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(UtcNowSource, "AgentGuard.Engine"));
        Assert.Equal("AG0015", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task DateTimeUtcNow_IsReportedEvenInBoundaries_AllowedNowhere()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(UtcNowSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0015", diagnostic.Id);
    }

    [Fact]
    public async Task Stopwatch_IsReported()
    {
        const string source = """
            using System.Diagnostics;

            public class Sample
            {
                public Stopwatch Timer() => Stopwatch.StartNew();
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0015", diagnostic.Id);
    }

    [Fact]
    public async Task DateTimeOffsetUtcNow_IsReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public DateTimeOffset When() => DateTimeOffset.UtcNow;
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0015", diagnostic.Id);
    }

    [Fact]
    public async Task ConstructingADateTimeValue_IsNotReported()
    {
        // Building or comparing DateTime values is fine; only reading the ambient clock is banned.
        const string source = """
            using System;

            public class Sample
            {
                public DateTime Epoch() => new DateTime(1970, 1, 1);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
    }
}
