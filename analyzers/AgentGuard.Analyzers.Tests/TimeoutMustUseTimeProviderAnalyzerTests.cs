// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0038 (timeout-uses-timeprovider): a bounded wait or timeout must flow through the injected clock. Thread.Sleep is
/// banned outright; Task.Delay/Task.WaitAsync/CancellationTokenSource carrying a timeout must take the TimeProvider
/// overload.
/// </summary>
public class TimeoutMustUseTimeProviderAnalyzerTests
{
    [Fact]
    public async Task ThreadSleep_IsReported()
    {
        const string source = """
            using System.Threading;
            internal static class Waiter
            {
                internal static void Wait() => Thread.Sleep(100);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<TimeoutMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0038", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task TaskDelay_WithoutTimeProvider_IsReported()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            internal static class Waiter
            {
                internal static Task Wait() => Task.Delay(TimeSpan.FromSeconds(1));
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<TimeoutMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0038", diagnostic.Id);
    }

    [Fact]
    public async Task CancellationTokenSource_WithTimeout_WithoutTimeProvider_IsReported()
    {
        const string source = """
            using System;
            using System.Threading;
            internal static class Waiter
            {
                internal static CancellationTokenSource Make() => new CancellationTokenSource(TimeSpan.FromSeconds(1));
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<TimeoutMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0038", diagnostic.Id);
    }

    [Fact]
    public async Task TaskDelay_WithTimeProvider_IsNotReported()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            internal static class Waiter
            {
                internal static Task Wait(TimeProvider clock) => Task.Delay(TimeSpan.FromSeconds(1), clock);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<TimeoutMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task ParameterlessCancellationTokenSource_IsNotReported()
    {
        const string source = """
            using System.Threading;
            internal static class Waiter
            {
                internal static CancellationTokenSource Make() => new CancellationTokenSource();
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<TimeoutMustUseTimeProviderAnalyzer>(source, "AgentGuard.Engine"));
    }
}
