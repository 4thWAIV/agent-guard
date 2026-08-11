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

    // The single-owner proof: a stub owning interface in AgentGuard.Abstractions, and TWO classes in the SAME
    // assembly — the owner implementing IPlatformFileSystem (its File.SetUnixFileMode is exempt) and a sibling that
    // does not implement it (its Directory.CreateSymbolicLink is RED). This proves the tightening from the
    // CrossPlatform-libraries assembly-set exemption to the owner classes.
    private const string OwnerAndSiblingSource = """
        using System.IO;

        namespace AgentGuard.Abstractions
        {
            public interface IPlatformFileSystem { }
        }

        namespace CrossPlatform
        {
            public sealed class PosixFileSystem : AgentGuard.Abstractions.IPlatformFileSystem
            {
                public void Chmod(string path) => File.SetUnixFileMode(path, UnixFileMode.UserRead);
            }

            public sealed class Sibling
            {
                public void Link(string a, string b) => Directory.CreateSymbolicLink(a, b);
            }
        }
        """;

    // The second AG0101 owner: the one shared helper class AgentGuard.CrossPlatform.PlatformFileSystemShared the
    // per-OS implementations delegate to, matched by full name AND gated to the CrossPlatform.* platform libraries.
    // Compiled into a platform library, its LinkTarget read is exempt; a sibling in the SAME namespace that is not
    // that helper is RED.
    private const string SharedHelperSource = """
        using System.IO;

        namespace AgentGuard.CrossPlatform
        {
            public static class PlatformFileSystemShared
            {
                public static string? Read(string path) => new FileInfo(path).LinkTarget;
            }

            public static class Sibling
            {
                public static void Link(string a, string b) => Directory.CreateSymbolicLink(a, b);
            }
        }
        """;

    // Just the shared helper, no sibling — so the ONE diagnostic (or its absence) is the helper's own LinkTarget read.
    // Used to prove the helper is exempt only inside a CrossPlatform.* library and RED (self-grant blocked) anywhere
    // else, even under its exact AgentGuard.CrossPlatform.PlatformFileSystemShared name.
    private const string SharedHelperOnlySource = """
        using System.IO;

        namespace AgentGuard.CrossPlatform
        {
            public static class PlatformFileSystemShared
            {
                public static string? Read(string path) => new FileInfo(path).LinkTarget;
            }
        }
        """;

    // Just the IPlatformFileSystem implementer, no sibling — so the ONE diagnostic (or its absence) is the owner's own
    // SetUnixFileMode. Used to prove the interface owner is exempt only inside a CrossPlatform.* platform library and
    // RED (self-grant blocked) in any other assembly, closing the round-2 hole where implementing IPlatformFileSystem
    // exempted the class in ANY assembly.
    private const string PlatformFileSystemOwnerOnlySource = """
        using System.IO;

        namespace AgentGuard.Abstractions
        {
            public interface IPlatformFileSystem { }
        }

        namespace CrossPlatform
        {
            public sealed class PosixFileSystem : AgentGuard.Abstractions.IPlatformFileSystem
            {
                public void Chmod(string path) => File.SetUnixFileMode(path, UnixFileMode.UserRead);
            }
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
        // Uses the one shared LinkTargetSource fixture the AG0011 test also reads (byte-identical, hoisted to one owner).
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                SharedAnalyzerSources.LinkTargetSource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains(
            "LinkTarget",
            AnalyzerRunner.SpanText(SharedAnalyzerSources.LinkTargetSource, diagnostic),
            System.StringComparison.Ordinal);
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
    public async Task OwnerImplementingInterface_IsExempt_SiblingInSameAssembly_IsStillReported()
    {
        // one-owner-class-per-primitive: only a class implementing IPlatformFileSystem is exempt, resolved
        // structurally. Compiled into a per-OS library to prove assembly membership no longer grants the exemption —
        // the owner's File.SetUnixFileMode is clean, but the sibling's Directory.CreateSymbolicLink in the SAME
        // assembly is still RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                OwnerAndSiblingSource, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains(
            "CreateSymbolicLink", AnalyzerRunner.SpanText(OwnerAndSiblingSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformFileSystemOwner_InNonCrossPlatformAssembly_IsReported_SelfGrantBlocked()
    {
        // FIX 1 (AG0101 interface self-grant): implementing IPlatformFileSystem is not enough — the class must also
        // compile into one of the four AgentGuard.CrossPlatform.* platform libraries. Declared in AgentGuard.Engine,
        // the owner's own File.SetUnixFileMode is RED, closing the round-2 hole where the interface implementer
        // self-granted in any assembly.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                PlatformFileSystemOwnerOnlySource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains(
            "SetUnixFileMode", AnalyzerRunner.SpanText(PlatformFileSystemOwnerOnlySource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformFileSystemOwner_InCrossPlatformLibrary_IsExempt()
    {
        // The same IPlatformFileSystem implementer compiled into a real CrossPlatform.* platform library stays exempt:
        // both halves of the conjunction pass.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                PlatformFileSystemOwnerOnlySource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task PlatformFileSystemSharedHelper_InCrossPlatformLibrary_IsExempt_SiblingInSameNamespace_IsStillReported()
    {
        // AG0101 exempts the interface implementers PLUS the one shared helper class they delegate to,
        // AgentGuard.CrossPlatform.PlatformFileSystemShared — matched by full name AND gated to the CrossPlatform.*
        // platform libraries. Compiled into a per-OS library, its LinkTarget read is clean, but a sibling class in
        // the SAME namespace that is not that helper is still RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                SharedHelperSource, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains(
            "CreateSymbolicLink", AnalyzerRunner.SpanText(SharedHelperSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformFileSystemSharedHelper_InNonCrossPlatformAssembly_IsReported_SelfGrantBlocked()
    {
        // Self-grant blocked: a type named exactly AgentGuard.CrossPlatform.PlatformFileSystemShared declared in an
        // assembly that is NOT one of the four AgentGuard.CrossPlatform.* platform libraries does not earn the
        // exemption — its LinkTarget read is RED, so no assembly can launder raw OS-divergent calls by naming a type.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                SharedHelperOnlySource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains(
            "LinkTarget", AnalyzerRunner.SpanText(SharedHelperOnlySource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformFileSystemSharedHelper_InCrossPlatformLibrary_IsExempt()
    {
        // The same helper compiled into a real CrossPlatform.* platform library stays exempt: exact type name AND the
        // assembly gate both pass, so its LinkTarget read is clean.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                SharedHelperOnlySource, "AgentGuard.CrossPlatform.MacOS"));
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
