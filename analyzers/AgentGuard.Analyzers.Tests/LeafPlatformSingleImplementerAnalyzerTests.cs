// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Globalization;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0030 (leaf-platform-single-implementer-rule): in a <c>.Tests</c> compilation no source type may implement any of
/// the four filesystem leaf interfaces (<c>IFileReader</c>/<c>IDirectoryEnumerator</c>/<c>IFileWriter</c>/
/// <c>IDirectoryWriter</c>) or <c>IPlatformFileSystem</c>. The only sanctioned implementers are the fakes and
/// <c>Wrap</c> proxies in <c>AgentGuard.TestHelpers</c>, reached through <c>SystemServicesBuilder</c> and referenced —
/// never re-declared — by a test project. <c>AgentGuard.TestHelpers</c> (not a <c>.Tests</c> assembly) keeps AG0025's
/// fake-plus-proxy exemption, and a shipping assembly's own single implementer is never gated here.
/// </summary>
public class LeafPlatformSingleImplementerAnalyzerTests
{
    // A real container tree exposing all four filesystem leaves through IFileSystem and IPlatformFileSystem through
    // IPlatformServices, plus one ordinary interface (to prove the rule guards only the nested leaves). AG0030's checked
    // candidate set is the nested-leaf set DERIVED from the tree walk (BoundaryServices.NestedLeafInterfaces), not a
    // hardcoded list, so deriving over this shape yields exactly {IFileReader, IDirectoryEnumerator, IFileWriter,
    // IDirectoryWriter, IPlatformFileSystem}. IWidget is an ordinary App interface, never a leaf; the three CONTAINERS
    // (ISystemServices/IFileSystem/IPlatformServices) are AG0022's job and are not in this candidate set.
    private const string ContainersSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IDirectoryEnumerator { }
            public interface IFileWriter { }
            public interface IDirectoryWriter { }
            public interface IPlatformFileSystem { }
            public interface IFileSystem
            {
                IFileReader GetFileReader();
                IDirectoryEnumerator GetDirectoryReader();
                IFileWriter GetFileWriter();
                IDirectoryWriter GetDirectoryWriter();
            }
            public interface IPlatformServices { IPlatformFileSystem FileSystem { get; } }
            public interface ISystemServices
            {
                IFileSystem FileSystem { get; }
                IPlatformServices Platform { get; }
            }
        }

        namespace App
        {
            public interface IWidget { }
        }
        """;

    [Fact]
    public async Task PlatformFileSystemImplementer_InTestProject_IsReported()
    {
        // Mirrors the pre-migration tests/AgentGuard.Tests/ManagedPlatformFileSystem.cs: a .Tests source type
        // implementing IPlatformFileSystem. Zero source implementers are permitted, so the first is a build error.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class ManagedPlatformFileSystem : AgentGuard.Abstractions.Contracts.IPlatformFileSystem { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<LeafPlatformSingleImplementerAnalyzer>(source, "AgentGuard.Tests"));
        Assert.Equal("AG0030", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains(
            "IPlatformFileSystem", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DirectoryEnumeratorImplementer_InTestProject_IsReported()
    {
        // Mirrors the pre-migration tests/AgentGuard.Tests/ThrowingDirectoryEnumerator.cs: a .Tests source type
        // implementing IDirectoryEnumerator (the throwing enumerator, now folded into the overlay's Handle seam).
        string source = ContainersSource + """

            namespace App
            {
                public sealed class ThrowingDirectoryEnumerator : AgentGuard.Abstractions.Contracts.IDirectoryEnumerator { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<LeafPlatformSingleImplementerAnalyzer>(source, "AgentGuard.Tests"));
        Assert.Equal("AG0030", diagnostic.Id);
        Assert.Contains(
            "IDirectoryEnumerator", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MultipleLeafImplementers_InTestProject_AreEachReported()
    {
        // Two source implementers of two different leaves — each is the first (and only) implementer of its own leaf,
        // so each is reported (report-first, permit-zero).
        string source = ContainersSource + """

            namespace App
            {
                public sealed class FakeReader : AgentGuard.Abstractions.Contracts.IFileReader { }
                public sealed class FakeWriter : AgentGuard.Abstractions.Contracts.IFileWriter { }
            }
            """;

        var diagnostics = await AnalyzerRunner.RunAsync<LeafPlatformSingleImplementerAnalyzer>(source, "AgentGuard.Tests");
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0030", diagnostic.Id));
    }

    [Fact]
    public async Task LeafImplementer_InTestHelpers_IsNotReported()
    {
        // AgentGuard.TestHelpers is NOT a .Tests assembly, so it keeps AG0025's fake-plus-Wrap-proxy exemption: the
        // sanctioned fake implementers live here and are not gated by AG0030.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class InMemoryFileSystem : AgentGuard.Abstractions.Contracts.IFileReader { }
                public sealed class ManagedPlatformFileSystem : AgentGuard.Abstractions.Contracts.IPlatformFileSystem { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<LeafPlatformSingleImplementerAnalyzer>(
            source, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task LeafImplementer_InShippingAssembly_IsNotReported()
    {
        // A shipping assembly is not a .Tests project, so the rule registers nothing there — the per-OS PosixFileSystem
        // legitimately implements IPlatformFileSystem in AgentGuard.CrossPlatform.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class PosixFileSystem : AgentGuard.Abstractions.Contracts.IPlatformFileSystem { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<LeafPlatformSingleImplementerAnalyzer>(
            source, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task ContainerImplementer_InTestProject_IsNotReported()
    {
        // The three CONTAINER interfaces are NOT in AG0030's nested-leaf candidate set (they are AG0022's job), so a
        // .Tests type implementing a container is not reported here.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class FakeFileSystem : AgentGuard.Abstractions.Contracts.IFileSystem { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<LeafPlatformSingleImplementerAnalyzer>(source, "AgentGuard.Tests"));
    }

    [Fact]
    public async Task NonLeafInterfaceImplementer_InTestProject_IsNotReported()
    {
        // An ordinary interface (IWidget) may have any number of implementers — only the nested leaves are guarded.
        string source = ContainersSource + """

            namespace App
            {
                public sealed class RedWidget : IWidget { }
                public sealed class BlueWidget : IWidget { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<LeafPlatformSingleImplementerAnalyzer>(source, "AgentGuard.Tests"));
    }
}
