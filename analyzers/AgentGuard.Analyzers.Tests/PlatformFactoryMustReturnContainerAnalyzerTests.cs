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
        // The container IPlatformServices lives at its real post-move home, AgentGuard.Abstractions.Contracts, while
        // the PlatformServices factory class stays in AgentGuard.CrossPlatform. PlatformServices.Create() — the
        // mandated self-building door (container-is-one-class-with-its-own-create) — returns a bare service
        // (IPlatformFileSystem) instead of the container, so it is reported.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IPlatformServices { }

                public interface IPlatformFileSystem { }
            }

            namespace AgentGuard.CrossPlatform
            {
                public static class PlatformServices
                {
                    public static AgentGuard.Abstractions.Contracts.IPlatformFileSystem Create() => null!;
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
        // The self-building PlatformServices.Create() returns the concrete PlatformServices rather than the
        // IPlatformServices container interface, so it is reported.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IPlatformServices { }
            }

            namespace AgentGuard.CrossPlatform
            {
                public sealed class PlatformServices : AgentGuard.Abstractions.Contracts.IPlatformServices
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
    public async Task Factory_ReturningContainerFromOldNamespace_IsReported()
    {
        // The return type's simple name is IPlatformServices, but it is declared in the OLD pre-move namespace
        // AgentGuard.CrossPlatform, so it is NOT the container at its real home AgentGuard.Abstractions.Contracts.
        // A bare name compare would miss this; the namespace-qualified identity check catches it. The factory must
        // fire until IPlatformServices is relocated.
        const string source = """
            namespace AgentGuard.CrossPlatform
            {
                public interface IPlatformServices { }

                public static class PlatformServices
                {
                    public static IPlatformServices Create() => null!;
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
        // Clean: PlatformServices.Create() returns the IPlatformServices container declared at its real post-move
        // home, AgentGuard.Abstractions.Contracts, while the PlatformServices factory stays in AgentGuard.CrossPlatform.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IPlatformServices { }
            }

            namespace AgentGuard.CrossPlatform
            {
                public static class PlatformServices
                {
                    public static AgentGuard.Abstractions.Contracts.IPlatformServices Create() => null!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task CreateOnPlatformServices_InDifferentNamespace_IsNotReported()
    {
        const string source = """
            namespace Other
            {
                public interface IPlatformFileSystem { }

                public static class PlatformServices
                {
                    public static IPlatformFileSystem Create() => null!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task NonCreateMethodOnPlatformServices_IsNotReported()
    {
        const string source = """
            namespace AgentGuard.CrossPlatform
            {
                public interface IPlatformFileSystem { }

                public static class PlatformServices
                {
                    public static IPlatformFileSystem Build() => null!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PlatformFactoryMustReturnContainerAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }
}
