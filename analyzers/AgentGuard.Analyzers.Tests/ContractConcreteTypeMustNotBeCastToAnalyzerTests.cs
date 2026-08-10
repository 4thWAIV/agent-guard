// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ContractConcreteTypeMustNotBeCastToAnalyzerTests
{
    [Fact]
    public async Task CastToConcrete_IsReported()
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
                }

                public sealed class Consumer
                {
                    public void Use(Sample.Abstractions.Contracts.IWidget widget)
                    {
                        var concrete = (Sample.Impl.Widget)widget;
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeCastToAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0007", diagnostic.Id);
    }

    [Fact]
    public async Task AsToConcrete_IsReported()
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
                }

                public sealed class Consumer
                {
                    public void Use(Sample.Abstractions.Contracts.IWidget widget)
                    {
                        var concrete = widget as Sample.Impl.Widget;
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeCastToAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0007", diagnostic.Id);
    }

    [Fact]
    public async Task TypePatternToConcrete_IsReported()
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
                }

                public sealed class Consumer
                {
                    public void Use(Sample.Abstractions.Contracts.IWidget widget)
                    {
                        if (widget is Sample.Impl.Widget concrete)
                        {
                            _ = concrete;
                        }
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeCastToAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0007", diagnostic.Id);
    }

    [Fact]
    public async Task NoCastToConcrete_IsNotReported()
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
                }

                public sealed class Consumer
                {
                    public void Use(Sample.Abstractions.Contracts.IWidget widget)
                    {
                        _ = widget;
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeCastToAnalyzer>(source));
    }
}
