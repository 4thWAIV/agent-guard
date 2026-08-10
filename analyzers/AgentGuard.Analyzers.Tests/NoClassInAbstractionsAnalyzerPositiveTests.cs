// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class NoClassInAbstractionsAnalyzerPositiveTests
{
    [Fact]
    public async Task PlainClassInAbstractions_IsReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public class Widget
                {
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Widget", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task PlainClassInContractsSubNamespace_IsReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public class Widget
                {
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0001", diagnostic.Id);
        Assert.Equal("Widget", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task PlainClassDeepUnderAbstractions_IsReported()
    {
        const string source = """
            namespace Sample.Abstractions.Foo.Bar
            {
                public class Widget
                {
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0001", diagnostic.Id);
        Assert.Equal("Widget", AnalyzerRunner.SpanText(source, diagnostic));
    }
}
