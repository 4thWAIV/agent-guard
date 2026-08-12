// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class VersionReadsOwnedAnalyzerTests
{
    private const string GetEntryAssemblySource = """
        using System.Reflection;

        namespace App
        {
            public class Sample
            {
                public Assembly? Entry() => Assembly.GetEntryAssembly();
            }
        }
        """;

    // The owner class implementing IBuildInfo, using the same reflection read — exempt only in AgentGuard.Boundaries.
    private const string OwnerReadingVersionSource = """
        using System.Reflection;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IBuildInfo { }
        }

        namespace App
        {
            public sealed class BuildInfo : AgentGuard.Abstractions.Contracts.IBuildInfo
            {
                public Assembly? Entry() => Assembly.GetEntryAssembly();
            }
        }
        """;

    [Fact]
    public async Task GetEntryAssembly_OutsideOwner_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<VersionReadsOwnedAnalyzer>(GetEntryAssemblySource, "AgentGuard.Engine"));
        Assert.Equal("AG0028", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("GetEntryAssembly", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssemblyNameVersion_OutsideOwner_IsReported()
    {
        const string source = """
            using System.Reflection;

            namespace App
            {
                public class Sample
                {
                    public object? Version(AssemblyName name) => name.Version;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<VersionReadsOwnedAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0028", diagnostic.Id);
        Assert.Contains("Version", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task VersionRead_InOwnerClassAndAssembly_IsExempt()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<VersionReadsOwnedAnalyzer>(
            OwnerReadingVersionSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task VersionRead_InOwnerAssembly_ButNotOwnerClass_IsReported()
    {
        // AgentGuard.Boundaries is the owner assembly, but a class that does not implement IBuildInfo is still RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<VersionReadsOwnedAnalyzer>(GetEntryAssemblySource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0028", diagnostic.Id);
    }

    [Fact]
    public async Task NonVersionAssemblyMember_IsNotReported()
    {
        // Assembly.Location and other non-version reads belong to AG0012, not this rule.
        const string source = """
            using System.Reflection;

            namespace App
            {
                public class Sample
                {
                    public string Where(Assembly assembly) => assembly.FullName!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<VersionReadsOwnedAnalyzer>(source, "AgentGuard.Engine"));
    }
}
