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

    // The relocated container reading the clock: AgentGuard.Engine's SystemServices acquiring TimeProvider.System.
    // ONE source run under two different assembly names — the real Engine, where it is the construction site and is
    // accepted, and a same-named decoy assembly, where it is not — so the two tests differ only in that argument.
    // Same technique as UtcNowSource above, which the two DateTime.UtcNow tests share the same way.
    private const string SystemServicesInEngineSource = """
        using System;

        namespace AgentGuard.Engine
        {
            internal static class SystemServices
            {
                private static TimeProvider Compose() => TimeProvider.System;
            }
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
    public async Task TimeProviderSystem_InSystemServicesCreate_IsNotReported()
    {
        // clock-legal-in-create-and-builder: TimeProvider.System is legal inside SystemServices in
        // AgentGuard.Engine — whose Create() wires the clock into the container. This is the construction site the
        // clock is acquired at, distinct from the composition CALLERS (Program + builder) that only call the factory.
        // The construction site moved to Engine with the container; AG0015 keeps its TYPE-level test, so anywhere
        // inside the container class qualifies.
        Assert.Empty(await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(
            SystemServicesInEngineSource, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task TimeProviderSystem_InSystemServicesLeftBehindInBoundaries_IsReported()
    {
        // The retarget, proved from the other side: the construction-site exemption is anchored on the container in
        // AgentGuard.Engine, so the same class left in AgentGuard.Boundaries no longer carries it.
        const string source = """
            using System;

            namespace AgentGuard.Boundaries
            {
                internal static class SystemServices
                {
                    private static TimeProvider Compose() => TimeProvider.System;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.Boundaries"));
        Assert.Equal("AG0015", diagnostic.Id);
    }

    [Fact]
    public async Task TimeProviderSystem_InSameNamedContainerInAnotherAssembly_IsReported()
    {
        // The construction-site identity is the namespace-plus-assembly conjunction, so a class merely NAMED
        // SystemServices in the AgentGuard.Engine namespace but compiled elsewhere cannot self-grant the exemption.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(
                SystemServicesInEngineSource, "AgentGuard.Engine.Decoy"));
        Assert.Equal("AG0015", diagnostic.Id);
    }

    [Fact]
    public async Task TimeProviderSystem_InSystemServicesBuilder_IsNotReported()
    {
        // The other construction site: the test SystemServicesBuilder in AgentGuard.TestHelpers.
        const string source = """
            using System;

            namespace AgentGuard.TestHelpers
            {
                public sealed class SystemServicesBuilder
                {
                    private TimeProvider Compose() => TimeProvider.System;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task TimeProviderSystem_InProgramCaller_IsReported()
    {
        // The composition CALLER (Program in AgentGuard.Cli, compiled into the real assembly name "guard") is NOT the
        // clock construction site: Program calls the already-built factory, it does not acquire the clock. So a direct
        // TimeProvider.System there is RED (clock-legal-in-create-and-builder — the "painted into a corner" fix).
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
            await AnalyzerRunner.RunAsync<TimeMustUseTimeProviderAnalyzer>(source, "guard"));
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
