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

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IPlatformFileSystem { }
        }

        namespace CrossPlatform
        {
            public sealed class PosixFileSystem : AgentGuard.Abstractions.Contracts.IPlatformFileSystem
            {
                public void Chmod(string path) => File.SetUnixFileMode(path, UnixFileMode.UserRead);
            }

            public sealed class Sibling
            {
                public void Link(string a, string b) => Directory.CreateSymbolicLink(a, b);
            }
        }
        """;

    // The shared helper class AgentGuard.CrossPlatform.PlatformFileSystemShared — NOT an owner under
    // ag0101-one-owner-per-os. Its static OS-divergent call (File.SetUnixFileMode) is RED everywhere, including inside a
    // CrossPlatform.* platform library: the shared-helper carve-out is gone, so its divergent logic must move into the
    // one per-OS class. Uses a static member AG0101 still owns after the shrink (the *Info instance members and
    // construction moved to AG0011, fileinfo-abstraction-stays-in-ag0011).
    private const string SharedHelperOnlySource = """
        using System.IO;

        namespace AgentGuard.CrossPlatform
        {
            public static class PlatformFileSystemShared
            {
                public static void Chmod(string path) => File.SetUnixFileMode(path, UnixFileMode.UserRead);
            }
        }
        """;

    // Just the IPlatformFileSystem implementer, no sibling — so the ONE diagnostic (or its absence) is the owner's own
    // SetUnixFileMode. Used to prove the interface owner is exempt only inside a CrossPlatform.* platform library and
    // RED (self-grant blocked) in any other assembly, closing the round-2 hole where implementing IPlatformFileSystem
    // exempted the class in ANY assembly.
    private const string PlatformFileSystemOwnerOnlySource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IPlatformFileSystem { }
        }

        namespace CrossPlatform
        {
            public sealed class PosixFileSystem : AgentGuard.Abstractions.Contracts.IPlatformFileSystem
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
    public async Task LinkTargetRead_IsNotReported_ItBelongsToAG0011()
    {
        // After the shrink, the *Info instance member LinkTarget is owned wholesale by the wrapper interfaces under
        // AG0011 (fileinfo-abstraction-stays-in-ag0011), not by AG0101 — so AG0101 no longer reports it.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                SharedAnalyzerSources.LinkTargetSource, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task InfoConstruction_IsNotReported_ItBelongsToAG0011()
    {
        // After the shrink, a raw new FileInfo / new DirectoryInfo is owned wholesale by IFileInfo/IDirectoryInfo under
        // AG0011, not AG0101 — so AG0101 no longer reports the construction.
        const string source = """
            using System.IO;

            public class Sample
            {
                public FileInfo File(string path) => new FileInfo(path);
                public DirectoryInfo Dir(string path) => new DirectoryInfo(path);
            }
            """;

        Assert.Empty(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(source, "AgentGuard.Engine"));
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
        // compile into one of the three per-OS implementation libraries (.MacOS/.Linux/.Windows). Declared in
        // AgentGuard.Engine, the owner's own File.SetUnixFileMode is RED, closing the round-2 hole where the interface
        // implementer self-granted in any assembly.
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
        // The same IPlatformFileSystem implementer compiled into a real per-OS implementation library (.MacOS) stays
        // exempt: both halves of the conjunction pass. This is the per-OS exemption the ag0101-one-owner-per-os fix
        // keeps green.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                PlatformFileSystemOwnerOnlySource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task PlatformFileSystemOwner_InCoreCrossPlatformAssembly_IsReported()
    {
        // ag0101-one-owner-per-os: the owner exemption is the THREE per-OS implementation libraries
        // (.MacOS/.Linux/.Windows), NOT the core AgentGuard.CrossPlatform contract assembly — home of
        // PlatformFileSystemShared. A class implementing IPlatformFileSystem compiled into the CORE assembly (name
        // exactly "AgentGuard.CrossPlatform") does NOT self-grant: its raw File.SetUnixFileMode is RED. This closes the
        // latent hole where the earlier four-library assembly gate (IsCrossPlatformLibrary) exempted a core-assembly
        // implementer, reopening the shared-helper escape this work closed.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                PlatformFileSystemOwnerOnlySource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains(
            "SetUnixFileMode", AnalyzerRunner.SpanText(PlatformFileSystemOwnerOnlySource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformFileSystemShared_InCrossPlatformLibrary_IsReported_NoSharedHelperCarveOut()
    {
        // ag0101-one-owner-per-os: the shared helper PlatformFileSystemShared is no longer an owner. Even compiled into
        // a real CrossPlatform.* platform library, its File.SetUnixFileMode call is RED — the shared-helper carve-out is
        // gone, so its divergent logic must move into the one per-OS class implementing IPlatformFileSystem. This is the
        // RED that forces PlatformFileSystemShared's divergent calls out.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                SharedHelperOnlySource, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0101", diagnostic.Id);
        Assert.Contains(
            "SetUnixFileMode", AnalyzerRunner.SpanText(SharedHelperOnlySource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformFileSystemShared_InNonCrossPlatformAssembly_IsReported()
    {
        // The same helper is RED in any non-platform assembly too — it is never exempt.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OsDivergentFilesystemOnlyInCrossPlatformAnalyzer>(
                SharedHelperOnlySource, "AgentGuard.Engine"));
        Assert.Equal("AG0101", diagnostic.Id);
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
