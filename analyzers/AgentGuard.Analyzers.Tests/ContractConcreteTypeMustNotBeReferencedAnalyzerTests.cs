// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ContractConcreteTypeMustNotBeReferencedAnalyzerTests
{
    [Fact]
    public async Task FieldTypedAsConcrete_IsReported()
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
                    private Sample.Impl.Widget _widget;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0006", diagnostic.Id);
    }

    [Fact]
    public async Task PropertyTypedAsConcrete_IsReported()
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
                    public Sample.Impl.Widget Current { get; set; }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0006", diagnostic.Id);
    }

    [Fact]
    public async Task ParameterTypedAsConcrete_IsReported()
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
                    public void Use(Sample.Impl.Widget widget)
                    {
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0006", diagnostic.Id);
    }

    [Fact]
    public async Task ReturnTypedAsConcrete_IsReported()
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
                    public Sample.Impl.Widget Get()
                    {
                        return null;
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0006", diagnostic.Id);
    }

    [Fact]
    public async Task LocalTypedAsConcrete_IsReported()
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
                    public void Use()
                    {
                        Sample.Impl.Widget widget = null;
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0006", diagnostic.Id);
    }

    [Fact]
    public async Task GenericArgumentOfConcrete_IsReported()
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
                    private System.Collections.Generic.List<Sample.Impl.Widget> _widgets;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0006", diagnostic.Id);
    }

    [Fact]
    public async Task InterfaceReferenceThroughFactory_IsNotReported()
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

                public sealed class Consumer
                {
                    private readonly Sample.Abstractions.Contracts.IWidget _widget = Sample.Impl.Widget.Create();
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractConcreteTypeMustNotBeReferencedAnalyzer>(source));
    }
}
