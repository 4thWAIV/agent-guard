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

    private const string CallFromProgramSource = """
        using AgentGuard.Boundaries;

        internal static class Program
        {
            private static ISystemServices Compose() => SystemServices.Create();
        }
        """;

    private const string CallFromBuilderSource = """
        using AgentGuard.Boundaries;

        public sealed class SystemServicesBuilder
        {
            public ISystemServices Build() => SystemServices.Create();
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
}
