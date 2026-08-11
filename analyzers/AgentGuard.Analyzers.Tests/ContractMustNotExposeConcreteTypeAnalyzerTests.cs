// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ContractMustNotExposeConcreteTypeAnalyzerTests
{
    [Fact]
    public async Task StaticMethodReturningConcreteType_IsReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Impl
            {
                public sealed class Widget : Sample.Abstractions.Contracts.IWidget
                {
                    private Widget()
                    {
                    }

                    public static Widget Build()
                    {
                        return new Widget();
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractMustNotExposeConcreteTypeAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0004", diagnostic.Id);
    }

    [Fact]
    public async Task StaticPropertyReturningConcreteType_IsReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Impl
            {
                public sealed class Widget : Sample.Abstractions.Contracts.IWidget
                {
                    private Widget()
                    {
                    }

                    public static Widget Instance { get; } = new Widget();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractMustNotExposeConcreteTypeAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0004", diagnostic.Id);
    }

    [Fact]
    public async Task CreateOverloadsReturningInterface_AreNotReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Impl
            {
                public sealed class Widget : Sample.Abstractions.Contracts.IWidget
                {
                    private Widget()
                    {
                    }

                    public static Sample.Abstractions.Contracts.IWidget Create()
                    {
                        return new Widget();
                    }

                    public static Sample.Abstractions.Contracts.IWidget Create(int seed)
                    {
                        return new Widget();
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractMustNotExposeConcreteTypeAnalyzer>(source));
    }

    [Fact]
    public async Task StaticInstancePropertyReturningInterface_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Impl
            {
                public sealed class Widget : Sample.Abstractions.Contracts.IWidget
                {
                    private static readonly Sample.Abstractions.Contracts.IWidget _instance = new Widget();

                    private Widget()
                    {
                    }

                    public static Sample.Abstractions.Contracts.IWidget Instance => _instance;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractMustNotExposeConcreteTypeAnalyzer>(source));
    }

    [Fact]
    public async Task DifferentlyNamedStaticFactoriesReturningInterface_AreNotReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Impl
            {
                public sealed class Widget : Sample.Abstractions.Contracts.IWidget
                {
                    private Widget()
                    {
                    }

                    public static Sample.Abstractions.Contracts.IWidget Create()
                    {
                        return new Widget();
                    }

                    public static Sample.Abstractions.Contracts.IWidget FromConfig()
                    {
                        return new Widget();
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractMustNotExposeConcreteTypeAnalyzer>(source));
    }
}
