// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class SystemServicesCreateOnlyAtCompositionAnalyzerTests
{
    private const string BoundariesSource = """
        namespace AgentGuard.Boundaries
        {
            public interface ISystemServices { }

            public static class SystemServices
            {
                public static ISystemServices Create() => null!;
            }
        }
        """;

    private const string CallFromDeepClassSource = """
        using AgentGuard.Boundaries;

        public class Deep
        {
            public ISystemServices Get() => SystemServices.Create();
        }
        """;

    // The real composition point: Program in its real namespace AgentGuard.Cli. The tightened exemption anchors on
    // full type identity (namespace + name) AND the assembly, so the namespace here must be the real one.
    private const string CallFromProgramSource = """
        using AgentGuard.Boundaries;

        namespace AgentGuard.Cli
        {
            internal static class Program
            {
                private static ISystemServices Compose() => SystemServices.Create();
            }
        }
        """;

    // The real test builder: SystemServicesBuilder in its real namespace AgentGuard.TestHelpers.
    private const string CallFromBuilderSource = """
        using AgentGuard.Boundaries;

        namespace AgentGuard.TestHelpers
        {
            public sealed class SystemServicesBuilder
            {
                public ISystemServices Build() => SystemServices.Create();
            }
        }
        """;

    // Nominal-collision self-grant probe: a second class NAMED Program, but in a DIFFERENT namespace than the real
    // composition point (AgentGuard.Cli). Compiled into the right assembly, it still must not self-grant the container.
    private const string CallFromProgramInDifferentNamespaceSource = """
        using AgentGuard.Boundaries;

        namespace Some.Other.Place
        {
            internal static class Program
            {
                private static ISystemServices Compose() => SystemServices.Create();
            }
        }
        """;

    // The same nominal collision for the builder: a class NAMED SystemServicesBuilder in a DIFFERENT namespace than
    // the real one (AgentGuard.TestHelpers), compiled into the right assembly.
    private const string CallFromBuilderInDifferentNamespaceSource = """
        using AgentGuard.Boundaries;

        namespace Some.Other.Place
        {
            public sealed class SystemServicesBuilder
            {
                public ISystemServices Build() => SystemServices.Create();
            }
        }
        """;

    [Fact]
    public async Task CreateCall_DeepInTheChain_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<SystemServicesCreateOnlyAtCompositionAnalyzer>(
                CallFromDeepClassSource, "AgentGuard.Engine", BoundariesSource, "AgentGuard.Boundaries");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0017", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CreateCall_InProgramCompositionMethod_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<SystemServicesCreateOnlyAtCompositionAnalyzer>(
            CallFromProgramSource, "AgentGuard.Cli", BoundariesSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task CreateCall_InSystemServicesBuilder_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<SystemServicesCreateOnlyAtCompositionAnalyzer>(
            CallFromBuilderSource, "AgentGuard.TestHelpers", BoundariesSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task CreateCall_InProgramNamedTypeInNonAllowedAssembly_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<SystemServicesCreateOnlyAtCompositionAnalyzer>(
                CallFromProgramSource, "AgentGuard.Engine", BoundariesSource, "AgentGuard.Boundaries");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0017", diagnostic.Id);
    }

    [Fact]
    public async Task CreateCall_InSystemServicesBuilderNamedTypeInNonAllowedAssembly_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<SystemServicesCreateOnlyAtCompositionAnalyzer>(
                CallFromBuilderSource, "AgentGuard.Engine", BoundariesSource, "AgentGuard.Boundaries");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0017", diagnostic.Id);
    }

    [Fact]
    public async Task CreateCall_InProgramNamedTypeInDifferentNamespaceOfCliAssembly_IsReported()
    {
        // Nominal collision: a second class named Program in a DIFFERENT namespace of the right assembly
        // (AgentGuard.Cli) cannot self-grant the container. The exemption anchors on namespace + name, so this is RED.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<SystemServicesCreateOnlyAtCompositionAnalyzer>(
                CallFromProgramInDifferentNamespaceSource, "AgentGuard.Cli", BoundariesSource, "AgentGuard.Boundaries");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0017", diagnostic.Id);
    }

    [Fact]
    public async Task CreateCall_InSystemServicesBuilderNamedTypeInDifferentNamespaceOfTestHelpers_IsReported()
    {
        // The same nominal collision for the builder: a class named SystemServicesBuilder in a DIFFERENT namespace of
        // the right assembly (AgentGuard.TestHelpers) is RED — the self-grant is blocked by namespace identity.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<SystemServicesCreateOnlyAtCompositionAnalyzer>(
                CallFromBuilderInDifferentNamespaceSource, "AgentGuard.TestHelpers", BoundariesSource, "AgentGuard.Boundaries");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0017", diagnostic.Id);
    }
}
