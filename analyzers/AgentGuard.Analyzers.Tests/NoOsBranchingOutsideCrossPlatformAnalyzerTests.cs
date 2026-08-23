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

    // A single-OS POSITIVE branch — OperatingSystem.IsMacOS() — the inverted AG0037 rule bans inside a per-OS library.
    private const string SingleOsMacBranchSource = """
        public class Sample
        {
            public bool Check() => System.OperatingSystem.IsMacOS();
        }
        """;

    // The cross-POSIX gate a per-OS library MAY branch on: !OperatingSystem.IsWindows() partitions POSIX from Windows
    // and is live in every per-OS build, so AG0037 leaves it alone.
    private const string WindowsGateSource = """
        public class Sample
        {
            public bool Check() => !System.OperatingSystem.IsWindows();
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

    [Fact]
    public async Task SingleOsBranch_InPerOsImplementationLibrary_IsReportedAsAg0037()
    {
        // The inverted rule: a positive single-OS check inside a per-OS library compiles into the other OS's build as
        // permanently-dead code (the current PosixFileSystem.IsMacOS() violation).
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(SingleOsMacBranchSource, "AgentGuard.CrossPlatform.MacOS");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0037", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task CrossPosixWindowsGate_InPerOsImplementationLibrary_IsNotReported()
    {
        // !OperatingSystem.IsWindows() is the allowed cross-POSIX gate — live in every per-OS build, not a dead
        // single-OS branch — so neither rule fires inside a per-OS library.
        Assert.Empty(await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(WindowsGateSource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task SingleOsBranch_InCoreCrossPlatformLibrary_IsNotReported()
    {
        // The core AgentGuard.CrossPlatform contract library is shared, not per-OS, so it may branch freely; neither
        // AG0009 nor AG0037 constrains it.
        Assert.Empty(await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(SingleOsMacBranchSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task SingleOsBranch_OutsideCrossPlatformLibraries_IsReportedAsAg0009()
    {
        // The SAME OperatingSystem.IsMacOS() outside the platform libraries is the OUTWARD ban (AG0009), not AG0037 —
        // proving the two registrations are distinct and never both fire.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoOsBranchingOutsideCrossPlatformAnalyzer>(SingleOsMacBranchSource, "AgentGuard.Engine"));
        Assert.Equal("AG0009", diagnostic.Id);
    }
}
