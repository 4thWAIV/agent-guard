// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class NoOsBranchingOutsideCrossPlatformAnalyzerTests
{
    private const string OperatingSystemBranchSource = """
        public class Sample
        {
            public bool Check() => System.OperatingSystem.IsWindows();
        }
        """;

    private const string RuntimeInformationBranchSource = """
        using System.Runtime.InteropServices;

        public class Sample
        {
            public bool Check() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
        """;

    private const string PlatformIfDirectiveSource = """
        public class Sample
        {
            public void Run()
            {
        #if WINDOWS
                System.Console.WriteLine("win");
        #endif
            }
        }
        """;

    [Fact]
    public async Task OperatingSystemIsWindows_OutsideCrossPlatformLibraries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(OperatingSystemBranchSource, "AgentGuard.Engine");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0009", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task RuntimeInformationIsOsPlatform_OutsideCrossPlatformLibraries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(RuntimeInformationBranchSource, "AgentGuard.Engine");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0009", diagnostic.Id);
    }

    [Fact]
    public async Task PlatformIfDirective_OutsideCrossPlatformLibraries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(PlatformIfDirectiveSource, "AgentGuard.Engine");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0009", diagnostic.Id);
    }

    [Fact]
    public async Task OperatingSystemBranch_InCrossPlatformTestsAssembly_IsReported()
    {
        // The AgentGuard.CrossPlatform.Tests spec project is OS-agnostic and NOT one of the four platform
        // implementation libraries, so OS branching is still forbidden there — the boundary is matched exactly,
        // not by an "AgentGuard.CrossPlatform." prefix.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(OperatingSystemBranchSource, "AgentGuard.CrossPlatform.Tests");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0009", diagnostic.Id);
    }

    [Fact]
    public async Task OperatingSystemBranch_InPerOsImplementationLibrary_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(OperatingSystemBranchSource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task PlatformIfDirective_InPerOsImplementationLibrary_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(PlatformIfDirectiveSource, "AgentGuard.CrossPlatform.Windows"));
    }

    [Fact]
    public async Task NonPlatformIsCall_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public bool Check() => string.IsNullOrEmpty("x");
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task NonPlatformIfDirective_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public void Run()
                {
            #if DEBUG
                    System.Console.WriteLine("debug");
            #endif
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(source, "AgentGuard.Engine"));
    }
}
