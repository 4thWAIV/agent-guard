// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class EnvironmentOnlyInBoundariesAnalyzerTests
{
    private const string GetFolderPathSource = """
        using System;

        public class Sample
        {
            public string Home() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        """;

    private const string SingleArgGetFullPathSource = """
        using System.IO;

        public class Sample
        {
            public string Full(string path) => Path.GetFullPath(path);
        }
        """;

    private const string TwoArgGetFullPathSource = """
        using System.IO;

        public class Sample
        {
            public string Full(string path, string basePath) => Path.GetFullPath(path, basePath);
        }
        """;

    private const string GetCurrentDirectorySource = """
        using System.IO;

        public class Sample
        {
            public string Cwd() => Directory.GetCurrentDirectory();
        }
        """;

    // The single-owner proof: a stub owning interface in AgentGuard.Abstractions, and TWO classes in the SAME
    // assembly — the owner implementing IEnvironment (its Environment.GetFolderPath is exempt) and a sibling that
    // does not implement it (its single-arg Path.GetFullPath is RED). This proves the tightening from an
    // assembly-wide exemption to the one owner class.
    private const string OwnerAndSiblingSource = """
        using System;
        using System.IO;

        namespace AgentGuard.Abstractions
        {
            public interface IEnvironment { }
        }

        namespace Engine
        {
            public sealed class EnvironmentAdapter : AgentGuard.Abstractions.IEnvironment
            {
                public string Home() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            public sealed class Sibling
            {
                public string Full(string path) => Path.GetFullPath(path);
            }
        }
        """;

    // Wrong-assembly self-grant probe: the EnvironmentAdapter owner implementing IEnvironment and reading the
    // environment. Exempt only in its owner assembly AgentGuard.Boundaries; RED in any other assembly.
    private const string EnvironmentAdapterOwnerSource = """
        using System;

        namespace AgentGuard.Abstractions
        {
            public interface IEnvironment { }
        }

        namespace Boundaries
        {
            public sealed class EnvironmentAdapter : AgentGuard.Abstractions.IEnvironment
            {
                public string Home() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }
        """;

    [Fact]
    public async Task EnvironmentMember_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(GetFolderPathSource, "AgentGuard.Engine"));
        Assert.Equal("AG0012", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task SingleArgGetFullPath_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(SingleArgGetFullPathSource, "AgentGuard.Engine"));
        Assert.Equal("AG0012", diagnostic.Id);
    }

    [Fact]
    public async Task TwoArgGetFullPath_IsNotReported()
    {
        // The pure two-argument overload is the sanctioned replacement and stays legal everywhere.
        Assert.Empty(await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(TwoArgGetFullPathSource, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task GetCurrentDirectory_OutsideBoundaries_IsReported()
    {
        // Directory.GetCurrentDirectory is an environment read on a filesystem type; it belongs to AG0012, not AG0011.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(GetCurrentDirectorySource, "AgentGuard.Engine"));
        Assert.Equal("AG0012", diagnostic.Id);
    }

    [Fact]
    public async Task AssemblyLocation_OutsideBoundaries_IsReported()
    {
        const string source = """
            using System.Reflection;

            public class Sample
            {
                public string Where(Assembly assembly) => assembly.Location;
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0012", diagnostic.Id);
    }

    [Fact]
    public async Task EnvironmentTickCount_IsNotReported_ItBelongsToAG0015()
    {
        const string source = """
            using System;

            public class Sample
            {
                public int Ticks() => Environment.TickCount;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task OwnerImplementingInterface_IsExempt_SiblingInSameAssembly_IsStillReported()
    {
        // one-owner-class-per-primitive: only the class implementing IEnvironment is exempt, resolved structurally.
        // Compiled into AgentGuard.Boundaries to prove assembly membership no longer grants the exemption — the
        // owner's Environment.GetFolderPath is clean, but the sibling's single-arg Path.GetFullPath in the SAME
        // assembly is still RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(OwnerAndSiblingSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0012", diagnostic.Id);
        Assert.Contains(
            "GetFullPath", AnalyzerRunner.SpanText(OwnerAndSiblingSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnvironmentAdapterOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        // FIX 1: implementing IEnvironment is not enough — the class must also compile into AgentGuard.Boundaries,
        // where the EnvironmentAdapter lives. In AgentGuard.Engine (the wrong assembly) the owner's
        // Environment.GetFolderPath is RED, so a fake declaring ': IEnvironment' cannot launder it.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(
                EnvironmentAdapterOwnerSource, "AgentGuard.Engine"));
        Assert.Equal("AG0012", diagnostic.Id);
        Assert.Contains(
            "GetFolderPath", AnalyzerRunner.SpanText(EnvironmentAdapterOwnerSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnvironmentAdapterOwner_InOwnerAssembly_IsExempt()
    {
        // The same class compiled into its owner assembly AgentGuard.Boundaries is exempt: both halves pass.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(
                EnvironmentAdapterOwnerSource, "AgentGuard.Boundaries"));
    }
}
