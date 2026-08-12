// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class NoCoverageOptOutAnalyzerTests
{
    private const string ExcludeOnTypeSource = """
        using System.Diagnostics.CodeAnalysis;

        namespace App
        {
            [ExcludeFromCodeCoverage]
            public class Untested { }
        }
        """;

    [Fact]
    public async Task ExcludeFromCodeCoverage_OnType_InProductAssembly_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoCoverageOptOutAnalyzer>(ExcludeOnTypeSource, "AgentGuard.Engine"));
        Assert.Equal("AG0032", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("Untested", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExcludeFromCodeCoverage_OnMethod_InProductAssembly_IsReported()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            namespace App
            {
                public class Sample
                {
                    [ExcludeFromCodeCoverage]
                    public void Work() { }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoCoverageOptOutAnalyzer>(source, "AgentGuard.Cli"));
        Assert.Equal("AG0032", diagnostic.Id);
        Assert.Contains("Work", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExcludeFromCodeCoverage_InTestAssembly_IsNotReported()
    {
        // Test projects are out of the coverage scope, so they may exclude freely.
        Assert.Empty(await AnalyzerRunner.RunAsync<NoCoverageOptOutAnalyzer>(ExcludeOnTypeSource, "AgentGuard.Cli.Tests"));
    }

    [Fact]
    public async Task ExcludeFromCodeCoverage_InAbstractionsAssembly_IsNotReported()
    {
        // AgentGuard.Abstractions (interfaces only) is out of the coverage scope.
        Assert.Empty(await AnalyzerRunner.RunAsync<NoCoverageOptOutAnalyzer>(ExcludeOnTypeSource, "AgentGuard.Abstractions"));
    }

    [Fact]
    public async Task NoExcludeAttribute_IsNotReported()
    {
        const string source = """
            namespace App
            {
                public class Sample
                {
                    public void Work() { }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoCoverageOptOutAnalyzer>(source, "AgentGuard.Engine"));
    }
}
