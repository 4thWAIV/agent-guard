// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class FilesystemOnlyInBoundariesAnalyzerTests
{
    private const string FileExistsSource = """
        using System.IO;

        public class Sample
        {
            public bool Check(string path) => File.Exists(path);
        }
        """;

    private const string DirectoryCreateSource = """
        using System.IO;

        public class Sample
        {
            public void Make(string path) => Directory.CreateDirectory(path);
        }
        """;

    private const string FileStreamSource = """
        using System.IO;

        public class Sample
        {
            public Stream Open(string path) => new FileStream(path, FileMode.Open);
        }
        """;

    private const string LinkTargetSource = """
        using System.IO;

        public class Sample
        {
            public string? Read(string path) => new FileInfo(path).LinkTarget;
        }
        """;

    private const string DirectoryInfoEnumerateSource = """
        using System.IO;

        public class Sample
        {
            public FileSystemInfo[] Walk(string path) => new DirectoryInfo(path).GetFileSystemInfos();
        }
        """;

    [Fact]
    public async Task FileExists_OutsideBoundaries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(FileExistsSource, "AgentGuard.Engine");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task DirectoryCreate_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(DirectoryCreateSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task FileStreamConstruction_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(FileStreamSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task FileCall_InCrossPlatformLibrary_IsStillReported()
    {
        // A raw OS-uniform File call is allowed only in AgentGuard.Boundaries. In a per-OS platform library it is
        // still RED, the forcing function that moves it out to the Boundaries adapter.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(FileExistsSource, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task FileExists_InBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(FileExistsSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task LinkTargetRead_IsNotReported_ItBelongsToAG0101()
    {
        // The *Info construction and the LinkTarget member are OS-divergent — AG0101 owns them, not AG0011.
        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(LinkTargetSource, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task DirectoryInfoEnumerateMember_OutsideBoundaries_IsReported()
    {
        // The construction is inert, but the GetFileSystemInfos member is an OS-uniform read — reported outside
        // Boundaries so the directory-enumerator adapter is forced to move there.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(DirectoryInfoEnumerateSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task DirectoryInfoEnumerate_InBoundaries_IsNotReported()
    {
        // Once the enumerator adapter lives in Boundaries, its DirectoryInfo walk is green — the inert construction
        // and the OS-uniform member are both allowed there. This is the case the inert-construction rule protects.
        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(DirectoryInfoEnumerateSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task PurePathMember_IsNotReported()
    {
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Join(string a, string b) => Path.Combine(a, b);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
    }
}
