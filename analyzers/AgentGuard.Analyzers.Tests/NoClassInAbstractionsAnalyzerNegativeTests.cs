// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class NoClassInAbstractionsAnalyzerNegativeTests
{
    [Fact]
    public async Task InterfaceInAbstractions_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public interface IWidget
                {
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source));
    }

    [Fact]
    public async Task EnumInAbstractions_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public enum Color
                {
                    Red,
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source));
    }

    [Fact]
    public async Task RecordInAbstractions_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public record Point(int X, int Y);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source));
    }

    [Fact]
    public async Task DelegateInAbstractions_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public delegate void Handler();
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source));
    }

    [Fact]
    public async Task StructInAbstractions_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public struct Size
                {
                    public int Width;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source));
    }

    [Fact]
    public async Task ClassInSimilarlyNamedNamespace_IsNotReported()
    {
        const string source = """
            namespace Sample.AbstractionsHelper
            {
                public class Widget
                {
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source));
    }

    [Fact]
    public async Task ClassInUnrelatedNamespace_IsNotReported()
    {
        const string source = """
            namespace Sample.Services
            {
                public class Widget
                {
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoClassInAbstractionsAnalyzer>(source));
    }
}
