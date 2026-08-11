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
    public async Task EnvironmentMember_InBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<EnvironmentOnlyInBoundariesAnalyzer>(GetFolderPathSource, "AgentGuard.Boundaries"));
    }
}
