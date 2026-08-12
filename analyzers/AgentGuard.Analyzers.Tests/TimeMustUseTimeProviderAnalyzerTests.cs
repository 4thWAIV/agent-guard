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

    [Fact]
    public async Task TimeProviderSystem_OutsideComposition_IsReported()
    {
        // A direct TimeProvider acquisition outside the composition point is RED — code reads the clock off
        // ISystemServices.Clock (timeprovider-on-the-container). This is the RED that forces GuardHost's raw
        // TimeProvider.System onto the injected clock.
        const string source = """
            using System;

            public class Sample
            {
                public TimeProvider Clock() => TimeProvider.System;
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0015", diagnostic.Id);
    }

    [Fact]
    public async Task TimeProviderSystem_AtCompositionPoint_IsNotReported()
    {
        // TimeProvider.System is legal at the one composition point: the Program type in namespace AgentGuard.Cli,
        // compiled into the CLI's REAL assembly name "guard" (<AssemblyName>guard</AssemblyName>), where the clock is
        // wired into ISystemServices.
        const string source = """
            using System;

            namespace AgentGuard.Cli
            {
                internal static class Program
                {
                    private static TimeProvider Compose() => TimeProvider.System;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "guard"));
    }

    [Fact]
    public async Task TimeProviderSystem_InProgramNamespaceButAssemblyNamedAsTheRootNamespace_IsReported()
    {
        // LESSON 1 regression guard: the CLI's real compiled assembly name is "guard", NOT its root namespace
        // "AgentGuard.Cli". A Program compiled into an assembly literally named "AgentGuard.Cli" is not the real
        // composition point, so the direct TimeProvider acquisition IS reported.
        const string source = """
            using System;

            namespace AgentGuard.Cli
            {
                internal static class Program
                {
                    private static TimeProvider Compose() => TimeProvider.System;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.Cli"));
        Assert.Equal("AG0015", diagnostic.Id);
    }

    [Fact]
    public async Task InjectedClockInstanceCall_IsNotReported()
    {
        // An instance call on an already-injected clock is not an acquisition and stays legal.
        const string source = """
            using System;

            public class Sample
            {
                private readonly TimeProvider clock;

                public Sample(TimeProvider clock) => this.clock = clock;

                public DateTimeOffset Now() => this.clock.GetUtcNow();
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
    }
}
