// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class OneOwnerPerInterfaceAnalyzerTests
{
    // The one ISystemServices container fixture the derived owner set (derive-service-set-from-isystemservices) walks
    // is SharedAnalyzerSources.AbstractionsPrefix (IFileReader is the leaf service accessor, ISystemServices the
    // container). It is prepended to every source below.

    // A Fake (InMemoryFileSystem) and a Wrap proxy base (RecordingFileReader) legitimately co-implement the same owner
    // interface — exactly what the test system (test-system decision) ships per service. In a shipping assembly this is
    // the second-implementer trick and RED; in the test/TestHelpers assemblies it is legitimate and gated off.
    private const string FakeAndProxySource = SharedAnalyzerSources.AbstractionsPrefix + """

        namespace AgentGuard.TestHelpers
        {
            public sealed class InMemoryFileSystem : AgentGuard.Abstractions.Contracts.IFileReader { }
            public class RecordingFileReader : AgentGuard.Abstractions.Contracts.IFileReader { }
        }
        """;

    [Fact]
    public async Task SecondImplementerOfOwnerInterface_IsReported()
    {
        // Two source classes implement the owned interface IFileReader; the second is RED, closing the trick of adding
        // ': IFileReader' with stub members to an inconvenient class to launder a raw call past the single-owner
        // exemption.
        string source = SharedAnalyzerSources.AbstractionsPrefix + """

            namespace App
            {
                public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader { }
                public sealed class SneakyFileReader : AgentGuard.Abstractions.Contracts.IFileReader { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<OneOwnerPerInterfaceAnalyzer>(source));
        Assert.Equal("AG0025", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("IFileReader", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleImplementerOfOwnerInterface_IsNotReported()
    {
        string source = SharedAnalyzerSources.AbstractionsPrefix + """

            namespace App
            {
                public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<OneOwnerPerInterfaceAnalyzer>(source));
    }

    [Fact]
    public async Task FakeAndProxyOfSameOwnerInterface_InTestHelpers_IsNotReported()
    {
        // AgentGuard.TestHelpers ships BOTH a fake and a Wrap proxy per service; two implementers of the same owner
        // interface there is legitimate, not the second-implementer trick. AG0025 is a shipping-code invariant and is
        // gated off the test-only helpers assembly.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<OneOwnerPerInterfaceAnalyzer>(FakeAndProxySource, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task FakeAndProxyOfSameOwnerInterface_InTestProject_IsNotReported()
    {
        // A .Tests project is also part of the test system and is gated off the same way (TestAssembly.IsTestAssembly).
        Assert.Empty(
            await AnalyzerRunner.RunAsync<OneOwnerPerInterfaceAnalyzer>(FakeAndProxySource, "AgentGuard.Engine.Tests"));
    }

    [Fact]
    public async Task SecondImplementerOfOwnerInterface_InShippingAssembly_IsReported()
    {
        // In a SHIPPING assembly the single-owner invariant holds: the same fake-plus-proxy pair is the
        // second-implementer trick and RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<OneOwnerPerInterfaceAnalyzer>(FakeAndProxySource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0025", diagnostic.Id);
        Assert.Contains("IFileReader", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecondImplementerThatIsAStruct_InShippingAssembly_IsReported()
    {
        // A struct (or record struct) declaring ': IFileReader' as a SECOND implementer must be caught too: the boundary
        // rules accept any named type implementing the owner interface as the exempt owner (no TypeKind gate), so a
        // struct owner would otherwise launder a raw call yet slip this duplicate scan. It is RED in a shipping assembly.
        string source = SharedAnalyzerSources.AbstractionsPrefix + """

            namespace App
            {
                public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader { }
                public struct SneakyFileReader : AgentGuard.Abstractions.Contracts.IFileReader { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<OneOwnerPerInterfaceAnalyzer>(source));
        Assert.Equal("AG0025", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("IFileReader", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TwoImplementersOfNonOwnerInterface_IsNotReported()
    {
        // The rule only guards the owned boundary interfaces derived from ISystemServices; an ordinary interface may
        // have any number of implementers. The container is present (so the derived owner set is the non-empty
        // {IFileReader}) yet IWidget — not reached from it — is correctly excluded.
        string source = SharedAnalyzerSources.AbstractionsPrefix + """

            namespace App
            {
                public interface IWidget { }
                public sealed class RedWidget : IWidget { }
                public sealed class BlueWidget : IWidget { }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<OneOwnerPerInterfaceAnalyzer>(source));
    }
}
