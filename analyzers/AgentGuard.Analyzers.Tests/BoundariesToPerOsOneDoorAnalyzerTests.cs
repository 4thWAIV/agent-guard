// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class BoundariesToPerOsOneDoorAnalyzerTests
{
    // A per-OS implementation assembly exposing the one allowed door (PlatformServices.Create) and another
    // internal-ish member Boundaries must NOT reach directly.
    private const string PerOsSource = """
        namespace AgentGuard.CrossPlatform
        {
            public static class PlatformServices
            {
                public static object Create() => null!;
            }

            public static class PosixFileSystem
            {
                public static object Probe(string path) => null!;
            }
        }
        """;

    private const string CallPlatformServicesCreateSource = """
        using AgentGuard.CrossPlatform;

        public class Composition
        {
            public object Build() => PlatformServices.Create();
        }
        """;

    private const string CallOtherPerOsMemberSource = """
        using AgentGuard.CrossPlatform;

        public class Composition
        {
            public object Build() => PosixFileSystem.Probe("x");
        }
        """;

    [Fact]
    public async Task CallPlatformServicesCreate_FromBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<BoundariesToPerOsOneDoorAnalyzer>(
            CallPlatformServicesCreateSource, "AgentGuard.Boundaries", PerOsSource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task CallOtherPerOsMember_FromBoundaries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<BoundariesToPerOsOneDoorAnalyzer>(
                CallOtherPerOsMemberSource, "AgentGuard.Boundaries", PerOsSource, "AgentGuard.CrossPlatform.MacOS");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0029", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CallOtherPerOsMember_FromNonBoundariesAssembly_IsNotReported()
    {
        // The rule gates only the AgentGuard.Boundaries compilation.
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<BoundariesToPerOsOneDoorAnalyzer>(
            CallOtherPerOsMemberSource, "AgentGuard.Engine", PerOsSource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task CallOldPlatformDoor_FromBoundaries_IsReported()
    {
        // The now-old separate Platform factory is NO LONGER the door: after the retarget the one legal door is
        // PlatformServices.Create(), so a Boundaries call to Platform.Create() into a per-OS assembly is reported.
        // This is the exact current-tree violation SystemServices.Create() carries until the IMPLEMENT worker
        // collapses Platform into the self-building PlatformServices.
        const string perOsWithOldPlatformDoor = """
            namespace AgentGuard.CrossPlatform
            {
                public static class Platform
                {
                    public static object Create() => null!;
                }
            }
            """;

        const string callOldPlatformDoor = """
            using AgentGuard.CrossPlatform;

            public class Composition
            {
                public object Build() => Platform.Create();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<BoundariesToPerOsOneDoorAnalyzer>(
                callOldPlatformDoor, "AgentGuard.Boundaries", perOsWithOldPlatformDoor, "AgentGuard.CrossPlatform.MacOS");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0029", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }
}
