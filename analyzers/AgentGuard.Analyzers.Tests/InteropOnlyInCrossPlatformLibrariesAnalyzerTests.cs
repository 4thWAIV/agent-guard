// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class InteropOnlyInCrossPlatformLibrariesAnalyzerTests
{
    private const string DllImportSource = """
        using System.Runtime.InteropServices;

        internal static class Native
        {
            [DllImport("libc", EntryPoint = "rename")]
            internal static extern int Rename(string oldPath, string newPath);
        }
        """;

    private const string LibraryImportSource = """
        using System.Runtime.InteropServices;

        internal static partial class Native
        {
            [LibraryImport("libc", EntryPoint = "rename", StringMarshalling = StringMarshalling.Utf8)]
            internal static partial int Rename(string oldPath, string newPath);
        }
        """;

    [Fact]
    public async Task DllImport_OutsideCrossPlatformLibraries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(DllImportSource, "AgentGuard.Engine");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0008", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task LibraryImport_OutsideCrossPlatformLibraries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(LibraryImportSource, "AgentGuard.Engine");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0008", diagnostic.Id);
    }

    [Fact]
    public async Task DllImport_InCrossPlatformTestsAssembly_IsReported()
    {
        // The AgentGuard.CrossPlatform.Tests spec project is OS-agnostic and NOT one of the four platform
        // implementation libraries, so native interop is still forbidden there — the boundary is matched exactly,
        // not by an "AgentGuard.CrossPlatform." prefix.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(DllImportSource, "AgentGuard.CrossPlatform.Tests");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0008", diagnostic.Id);
    }

    [Fact]
    public async Task DllImport_InPerOsImplementationLibrary_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(DllImportSource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task LibraryImport_InLinkSharedLinuxLibrary_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(LibraryImportSource, "AgentGuard.CrossPlatform.Linux"));
    }

    [Fact]
    public async Task Interop_InContractAssembly_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(DllImportSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task MethodWithoutInterop_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public int Add(int a, int b) => a + b;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task SameNamedAttributeInDifferentNamespace_IsNotReported()
    {
        const string source = """
            namespace Custom
            {
                using System;

                [AttributeUsage(AttributeTargets.Method)]
                public sealed class LibraryImportAttribute : Attribute
                {
                    public LibraryImportAttribute(string name)
                    {
                    }
                }

                public class Sample
                {
                    [LibraryImport("x")]
                    public void Go()
                    {
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(source, "AgentGuard.Engine"));
    }
}
