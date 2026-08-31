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

    [Fact]
    public async Task SameNamedDllImportAttributeInDifferentNamespace_IsNotReported()
    {
        // ROUND 9 (attribute-identity): a user-declared attribute named DllImport in a DIFFERENT namespace resolves to
        // Custom.DllImportAttribute, not System.Runtime.InteropServices.DllImportAttribute. Because the rule now matches
        // by RESOLVED type, it is not native interop and is NOT reported — the companion to the LibraryImport case.
        const string source = """
            namespace Custom
            {
                using System;

                [AttributeUsage(AttributeTargets.Method)]
                public sealed class DllImportAttribute : Attribute
                {
                    public DllImportAttribute(string name)
                    {
                    }
                }

                public class Sample
                {
                    [DllImport("x")]
                    public void Go()
                    {
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task MalformedSameNamedDllImportAttribute_WithRequiredArgOmitted_IsNotReported()
    {
        // ROUND 10 (SOLID): pins the GetTypeInfo branch of AttributeIdentity.ResolveAttributeType as load-bearing.
        // Custom.DllImportAttribute in a DIFFERENT namespace declares a REQUIRED constructor parameter; applied here with
        // that argument OMITTED, GetSymbolInfo fails with OverloadResolutionFailure (no constructor symbol) while
        // GetTypeInfo STILL binds the user's Custom.DllImportAttribute type. Because the resolver recovers that type,
        // IsAnyOf reports it is not the BCL interop attribute and the P/Invoke-shaped method is NOT reported. Without the
        // GetTypeInfo branch this would fall through to the namespace-blind syntactic fallback and be misidentified as
        // [DllImport] — a false positive — so this fixture is what keeps a future reader from deleting the branch as dead.
        const string source = """
            namespace Custom
            {
                using System;

                [AttributeUsage(AttributeTargets.Method)]
                public sealed class DllImportAttribute : Attribute
                {
                    public DllImportAttribute(string name)
                    {
                    }
                }

                internal static class Native
                {
                    [DllImport]
                    internal static extern int Rename(string oldPath, string newPath);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task UnresolvedInteropAttribute_IsReportedViaSyntacticFallback()
    {
        // ROUND 9 (attribute-identity): with no `using System.Runtime.InteropServices;` and no such type in scope, the
        // [DllImport] attribute is a genuinely UNDEFINED identifier — neither its constructor nor its type binds. This is
        // the unresolved-identifier case (a missing reference or using), distinct from a malformed usage where the type
        // still binds; the shared resolver falls back to the syntactic name, so native interop is still caught
        // fail-closed and the declaration is reported.
        const string source = """
            internal static class Native
            {
                [DllImport("libc", EntryPoint = "rename")]
                internal static extern int Rename(string oldPath, string newPath);
            }
            """;

        Diagnostic diagnostic =
            Assert.Single(await AnalyzerRunner.RunAsync<InteropOnlyInCrossPlatformLibrariesAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0008", diagnostic.Id);
    }
}
