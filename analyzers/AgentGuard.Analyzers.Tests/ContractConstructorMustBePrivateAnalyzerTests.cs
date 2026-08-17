// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ContractConstructorMustBePrivateAnalyzerTests
{
    [Fact]
    public async Task ContractImplementationWithPublicConstructor_IsReported()
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
                    public Widget()
                    {
                    }

                    public static Sample.Abstractions.Contracts.IWidget Create()
                    {
                        return new Widget();
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConstructorMustBePrivateAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0003", diagnostic.Id);
    }

    [Fact]
    public async Task ContractImplementationWithPrivateConstructor_IsNotReported()
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
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractConstructorMustBePrivateAnalyzer>(source));
    }

    [Fact]
    public async Task ContractImplementationRecordWithPublicConstructor_IsReported()
    {
        // records-skip fixed (containers-are-locked-classes-not-records): a record that implements a contract interface
        // is still held to the private-constructor rule — a container declared as a record no longer slips through.
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Impl
            {
                public sealed record Widget : Sample.Abstractions.Contracts.IWidget
                {
                    public Widget()
                    {
                    }

                    public static Sample.Abstractions.Contracts.IWidget Create()
                    {
                        return new Widget();
                    }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<ContractConstructorMustBePrivateAnalyzer>(source));
        Assert.Equal("AG0003", diagnostic.Id);
    }

    [Fact]
    public async Task PlainDataRecordImplementingNoContractInterface_IsNotReported()
    {
        // A plain data record that implements no contract interface stays exempt through the AllInterfaces gate — only
        // records that implement a contract interface are held to the rule.
        const string source = """
            namespace Sample.Impl
            {
                public sealed record Point(int X, int Y);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractConstructorMustBePrivateAnalyzer>(source));
    }

    [Fact]
    public async Task NonContractClassWithPublicConstructor_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public interface IWidget
                {
                }
            }

            namespace Sample.Impl
            {
                public sealed class Widget : Sample.Abstractions.IWidget
                {
                    public Widget()
                    {
                    }

                    public void DoWork()
                    {
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractConstructorMustBePrivateAnalyzer>(source));
    }
}
