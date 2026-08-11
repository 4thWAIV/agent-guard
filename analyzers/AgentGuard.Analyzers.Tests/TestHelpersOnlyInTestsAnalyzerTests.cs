// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class TestHelpersOnlyInTestsAnalyzerTests
{
    private const string TestHelpersSource = """
        namespace AgentGuard.TestHelpers
        {
            public sealed class SystemServicesBuilder
            {
                public static SystemServicesBuilder Fake() => new();
            }
        }
        """;

    private const string UseBuilderSource = """
        using AgentGuard.TestHelpers;

        public class Sample
        {
            public object Build() => SystemServicesBuilder.Fake();
        }
        """;

    [Fact]
    public async Task TestHelpersType_UsedFromShippingAssembly_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<TestHelpersOnlyInTestsAnalyzer>(
                UseBuilderSource, "AgentGuard.Engine", TestHelpersSource, "AgentGuard.TestHelpers");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0018", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task TestHelpersType_UsedFromTestAssembly_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<TestHelpersOnlyInTestsAnalyzer>(
            UseBuilderSource, "AgentGuard.Tests", TestHelpersSource, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task TestHelpersType_UsedFromTestHelpersItself_IsNotReported()
    {
        // The helpers assembly references its own types freely; the rule only stops shipping code reaching them.
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<TestHelpersOnlyInTestsAnalyzer>(
            UseBuilderSource, "AgentGuard.TestHelpers", TestHelpersSource, "AgentGuard.TestHelpers"));
    }
}
