// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ReturnTypesMustNotBeTuplesAnalyzerPositiveTests
{
    [Fact]
    public async Task PublicMethodReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                public (int, string) Get()
                {
                    return (1, "a");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
        Assert.Equal("Get", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task PrivateMethodReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                private (int, string) Get()
                {
                    return (1, "a");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
    }

    [Fact]
    public async Task MethodReturningTupleInsideGeneric_IsReported()
    {
        const string source = """
            using System.Collections.Generic;

            public class Sample
            {
                public List<(int, string)> Get()
                {
                    return new List<(int, string)>();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
    }

    [Fact]
    public async Task MethodReturningTaskOfTuple_IsReported()
    {
        const string source = """
            using System.Threading.Tasks;

            public class Sample
            {
                public Task<(int, string)> GetAsync()
                {
                    return Task.FromResult((1, "a"));
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
        Assert.Equal("GetAsync", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task PropertyReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                public (int, string) Pair => (1, "a");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
        Assert.Equal("Pair", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task IndexerReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                public (int, string) this[int index] => (index, "a");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
    }

    [Fact]
    public async Task DelegateReturningTuple_IsReported()
    {
        const string source = """
            public delegate (int, string) Combine();
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
        Assert.Equal("Combine", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task LocalFunctionReturningTuple_IsReported()
    {
        const string source = """
            public class Sample
            {
                public void Run()
                {
                    (int, string) Inner()
                    {
                        return (1, "a");
                    }

                    Inner();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ReturnTypesMustNotBeTuplesAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0002", diagnostic.Id);
        Assert.Equal("Inner", AnalyzerRunner.SpanText(source, diagnostic));
    }
}
