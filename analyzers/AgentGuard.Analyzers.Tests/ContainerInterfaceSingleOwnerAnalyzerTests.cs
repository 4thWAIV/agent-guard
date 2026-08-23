// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0022 (container-interface-single-owner-rule): each of the three container-shaped interfaces — ISystemServices,
/// IFileSystem, IPlatformServices — may be implemented by at most one type per compilation, with NO test-system
/// exemption. This closes the bypass where a test hand-rolls a container implementation and builds the container
/// directly, sidestepping SystemServicesBuilder — the one hole AG0025 (leaf services, test-exempt) and AG0017 (static
/// factory) do not cover.
/// </summary>
public class ContainerInterfaceSingleOwnerAnalyzerTests
{
    // A real container tree plus one ordinary interface (to prove the rule guards only the containers). AG0022's checked
    // container set is DERIVED from the tree walk (BoundaryServices.ResolveTree, container-interface-single-owner-rule),
    // not a hardcoded triple, so IFileSystem and IPlatformServices are guarded only because they are genuinely reachable
    // as nested containers from ISystemServices: ISystemServices exposes each through a service accessor, and each has a
    // service accessor of its own (so it is a container, not a leaf). Deriving over this shape yields exactly
    // {ISystemServices, IFileSystem, IPlatformServices}. IWidget is an ordinary App interface, never a container.
    private const string ContainersSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IPlatformFileSystem { }
            public interface IFileSystem { IFileReader GetFileReader(); }
            public interface IPlatformServices { IPlatformFileSystem FileSystem { get; } }
            public interface ISystemServices
            {
                IFileSystem FileSystem { get; }
                IPlatformServices Platform { get; }
            }
        }

        namespace App
        {
            public interface IWidget { }
        }
        """;

    [Fact]
    public async Task SecondImplementerOfContainer_IsReported()
    {
        // Two classes implement ISystemServices; the second is a build error — a hand-rolled container built directly
        // instead of through SystemServicesBuilder.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class Services : AgentGuard.Abstractions.Contracts.ISystemServices { }
                public sealed class SneakyServices : AgentGuard.Abstractions.Contracts.ISystemServices { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ContainerInterfaceSingleOwnerAnalyzer>(source));
        Assert.Equal("AG0022", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("ISystemServices", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleImplementerOfContainer_IsNotReported()
    {
        string source = ContainersSource + """

            namespace App
            {
                public sealed class Services : AgentGuard.Abstractions.Contracts.ISystemServices { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContainerInterfaceSingleOwnerAnalyzer>(source));
    }

    [Fact]
    public async Task SecondImplementerOfContainer_InTestAssembly_IsStillReported_NoExemption()
    {
        // The defining difference from AG0025: NO test-system exemption. In a .Tests compilation, two implementers of a
        // container is still a build error — the only legal container implementers there are the fakes nested inside
        // SystemServicesBuilder, so a test-declared second implementer is caught.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class Services : AgentGuard.Abstractions.Contracts.ISystemServices { }
                public sealed class FakeServices : AgentGuard.Abstractions.Contracts.ISystemServices { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<ContainerInterfaceSingleOwnerAnalyzer>(source, "AgentGuard.Cli.Tests"));
        Assert.Equal("AG0022", diagnostic.Id);
        Assert.Contains("ISystemServices", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecondImplementerOfSubContainer_IsReported()
    {
        // The rule guards all three containers, including the nested IFileSystem and IPlatformServices.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class Fs : AgentGuard.Abstractions.Contracts.IFileSystem { }
                public sealed class OtherFs : AgentGuard.Abstractions.Contracts.IFileSystem { }
                public sealed class Plat : AgentGuard.Abstractions.Contracts.IPlatformServices { }
                public sealed class OtherPlat : AgentGuard.Abstractions.Contracts.IPlatformServices { }
            }
            """;

        var diagnostics = await AnalyzerRunner.RunAsync<ContainerInterfaceSingleOwnerAnalyzer>(source);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0022", diagnostic.Id));
    }

    [Fact]
    public async Task SecondImplementerOfNonContainerInterface_IsNotReported()
    {
        // An ordinary interface (IWidget) may have any number of implementers — only the three containers are guarded.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class RedWidget : IWidget { }
                public sealed class BlueWidget : IWidget { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<ContainerInterfaceSingleOwnerAnalyzer>(source));
    }
}
