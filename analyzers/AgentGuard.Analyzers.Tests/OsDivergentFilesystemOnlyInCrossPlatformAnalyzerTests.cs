// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class OsDivergentFilesystemOnlyInCrossPlatformAnalyzerTests
{
    private const string SetUnixFileModeSource = """
        using System.IO;

        public class Sample
        {
            public void Chmod(string path) => File.SetUnixFileMode(path, UnixFileMode.UserRead);
        }
        """;

    private const string LinkTargetSource = """
        using System.IO;

        public class Sample
        {
            public string? Read(string path) => new FileInfo(path).LinkTarget;
        }
        """;

    private const string CreateSymbolicLinkSource = """
        using System.IO;

        public class Sample
        {
            public void Link(string a, string b) => Directory.CreateSymbolicLink(a, b);
        }
        """;

    private const string MarshalSource = """
        using System.Runtime.InteropServices;

        public class Sample
        {
            public int LastError() => Marshal.GetLastPInvokeError();
        }
        """;

    [Fact]
    public async Task SetUnixFileMode_OutsideCrossPlatform_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(SetUnixFileModeSource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task LinkTargetRead_OutsideCrossPlatform_IsReported()
    {
        // The LinkTarget member read is the OS-divergent access; the bare FileInfo construction beside it is inert.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(LinkTargetSource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains("LinkTarget", AnalyzerRunner.SpanText(LinkTargetSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task BareInfoConstruction_IsNotReported_ItIsInert()
    {
        // Constructing a FileInfo/DirectoryInfo opens no handle; it is flagged by neither rule. This is what lets
        // the directory-enumerator adapter build a DirectoryInfo in Boundaries and the link reader build a FileInfo
        // in CrossPlatform without either being wrongly forced out.
        const string source = """
            using System.IO;

            public class Sample
            {
                public FileInfo Info(string path) => new FileInfo(path);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task CreateSymbolicLink_OutsideCrossPlatform_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(CreateSymbolicLinkSource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
    }

    [Fact]
    public async Task Marshal_OutsideCrossPlatform_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(MarshalSource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
    }

    [Fact]
    public async Task OsDivergentCall_InPerOsLibrary_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
            SetUnixFileModeSource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task LinkTarget_InContractLibrary_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
            LinkTargetSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task ReadingUnixFileModeEnumValue_IsNotReported()
    {
        // Reading an enum value as data is a legal edge — the UnixFileMode enum itself is not the File member.
        const string source = """
            using System.IO;

            public class Sample
            {
                public UnixFileMode Bits() => UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task OsUniformFileCall_IsNotReported_ItBelongsToAG0011()
    {
        const string source = """
            using System.IO;

            public class Sample
            {
                public bool Check(string path) => File.Exists(path);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(source, "AgentGuard.Engine"));
    }
}
