// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class RandomnessOnlyInBoundariesAnalyzerTests
{
    private const string NewGuidSource = """
        using System;

        public class Sample
        {
            public string Name() => Guid.NewGuid().ToString("N");
        }
        """;

    [Fact]
    public async Task GuidNewGuid_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(NewGuidSource, "AgentGuard.Engine"));
        Assert.Equal("AG0014", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task GuidNewGuid_InCrossPlatformLibrary_IsStillReported()
    {
        // The one Guid.NewGuid() lives in AgentGuard.CrossPlatform's shared helper today; CrossPlatform is not
        // Boundaries, so this is the RED that forces the GUID behind IGuidFactory.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(NewGuidSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0014", diagnostic.Id);
    }

    [Fact]
    public async Task GuidNewGuid_InBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(NewGuidSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task NewRandom_OutsideBoundaries_IsReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public Random Make() => new Random();
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0014", diagnostic.Id);
    }

    [Fact]
    public async Task GuidParse_IsNotReported()
    {
        // Parsing or building a GUID from data is deterministic and stays legal; only the NewGuid() factory is banned.
        const string source = """
            using System;

            public class Sample
            {
                public Guid Parse(string text) => Guid.Parse(text);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<RandomnessOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
    }
}
