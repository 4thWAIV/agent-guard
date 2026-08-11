// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class ContractPublicSurfaceMustMatchInterfaceAnalyzerTests
{
    [Fact]
    public async Task PublicMethodNotOnInterface_IsReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                    void Do();
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

                    public void Do()
                    {
                    }

                    public void Extra()
                    {
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerRunner.RunAsync<ContractPublicSurfaceMustMatchInterfaceAnalyzer>(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0005", diagnostic.Id);
        Assert.Equal("Extra", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task PublicSurfaceMatchingInterface_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions.Contracts
            {
                public interface IWidget
                {
                    void Do();
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

                    public void Do()
                    {
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractPublicSurfaceMustMatchInterfaceAnalyzer>(source));
    }

    [Fact]
    public async Task NonContractClassWithExtraMethod_IsNotReported()
    {
        const string source = """
            namespace Sample.Abstractions
            {
                public interface IWidget
                {
                    void Do();
                }
            }

            namespace Sample.Impl
            {
                public sealed class Widget : Sample.Abstractions.IWidget
                {
                    public void Do()
                    {
                    }

                    public void Extra()
                    {
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContractPublicSurfaceMustMatchInterfaceAnalyzer>(source));
    }
}
