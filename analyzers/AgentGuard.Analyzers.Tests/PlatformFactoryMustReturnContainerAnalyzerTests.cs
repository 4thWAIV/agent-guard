// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class PlatformFactoryMustReturnContainerAnalyzerTests
{
    [Fact]
    public async Task Factory_ReturningBareFileSystem_IsReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform
            {
                public interface IPlatformServices { }

                public interface IPlatformFileSystem { }

                public static class Platform
                {
                    public static IPlatformFileSystem Create() => null!;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0010", diagnostic.Id);
        Assert.Equal("Create", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task Factory_ReturningConcreteType_IsReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform
            {
                public interface IPlatformServices { }

                public sealed class PlatformServices : IPlatformServices { }

                public static class Platform
                {
                    public static PlatformServices Create() => new PlatformServices();
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0010", diagnostic.Id);
    }

    [Fact]
    public async Task Factory_ReturningContainerFromDifferentNamespace_IsReported()
    {
        // The return type's simple name is IPlatformServices, but it is declared in a different namespace, so it
        // is NOT the AgentGuard.CrossPlatform container. A bare name compare would miss this; the namespace-
        // qualified identity check catches it.
        const string source = """
            namespace Other
            {
                public interface IPlatformServices { }
            }

            namespace AgentGuard.CrossPlatform
            {
                public static class Platform
                {
                    public static Other.IPlatformServices Create() => null!;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0010", diagnostic.Id);
    }

    [Fact]
    public async Task Factory_ReturningContainer_IsNotReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform
            {
                public interface IPlatformServices { }

                public static class Platform
                {
                    public static IPlatformServices Create() => null!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task CreateOnPlatform_InDifferentNamespace_IsNotReported()
    {
        const string source = """
            namespace Other
            {
                public interface IPlatformFileSystem { }

                public static class Platform
                {
                    public static IPlatformFileSystem Create() => null!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task NonCreateMethodOnPlatform_IsNotReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform
            {
                public interface IPlatformFileSystem { }

                public static class Platform
                {
                    public static IPlatformFileSystem Build() => null!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }
}
