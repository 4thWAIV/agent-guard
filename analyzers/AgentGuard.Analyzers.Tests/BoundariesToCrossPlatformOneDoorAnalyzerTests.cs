// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class BoundariesToCrossPlatformOneDoorAnalyzerTests
{
    // The core AgentGuard.CrossPlatform assembly, exposing the one allowed adapter-factory (CrossPlatformAdapters) and
    // another type that Boundaries must NOT call directly.
    private const string CrossPlatformSource = """
        namespace AgentGuard.CrossPlatform
        {
            public static class CrossPlatformAdapters
            {
                public static object Create() => null!;
            }

            public static class FileReader
            {
                public static object Read(string path) => null!;
            }
        }
        """;

    private const string CallAdapterFactorySource = """
        using AgentGuard.CrossPlatform;

        public class Composition
        {
            public object Build() => CrossPlatformAdapters.Create();
        }
        """;

    private const string CallOtherCrossPlatformTypeSource = """
        using AgentGuard.CrossPlatform;

        public class Composition
        {
            public object Build() => FileReader.Read("x");
        }
        """;

    [Fact]
    public async Task CallIntoAdapterFactory_FromBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<BoundariesToCrossPlatformOneDoorAnalyzer>(
            CallAdapterFactorySource, "AgentGuard.Boundaries", CrossPlatformSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task CallIntoOtherCrossPlatformType_FromBoundaries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<BoundariesToCrossPlatformOneDoorAnalyzer>(
                CallOtherCrossPlatformTypeSource, "AgentGuard.Boundaries", CrossPlatformSource, "AgentGuard.CrossPlatform");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0023", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CallIntoOtherCrossPlatformType_FromNonBoundariesAssembly_IsNotReported()
    {
        // The rule gates only the AgentGuard.Boundaries compilation; the same call from another assembly is not its
        // concern (the internal/private-ctor wall stops that path).
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<BoundariesToCrossPlatformOneDoorAnalyzer>(
            CallOtherCrossPlatformTypeSource, "AgentGuard.Engine", CrossPlatformSource, "AgentGuard.CrossPlatform"));
    }
}
