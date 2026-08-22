// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0036 (no-literal-case-mode-seed): a <c>true</c>/<c>false</c> literal passed as the <c>caseSensitive</c> argument
/// of the <c>AgentGuard.TestHelpers.InMemoryFileSystemStore</c> overlay is a build error, so the simulator's case mode
/// is seeded from the real host. Only the store constructor's <c>caseSensitive</c> argument is inspected; the
/// deliberate <c>SetCaseSensitive</c>/<c>SimulateCaseSensitivity</c> entry points are methods, not the constructor, and
/// are never flagged.
/// </summary>
public class NoLiteralCaseModeSeedAnalyzerTests
{
    // Stand-ins for the overlay and the builder entry points, matched by namespace + name AND the declaring assembly.
    // RunAsync compiles this preamble into an assembly named AgentGuard.TestHelpers so the overlay carries the identity
    // the analyzer pins on (WellKnownType.IsInAssembly); a foreign-assembly stand-in is left alone, proven separately
    // below. SetCaseSensitive (on the store) and SimulateCaseSensitivity (on the builder) are the deliberate
    // post-construction switches the rule must leave alone.
    private const string Preamble = """
        namespace AgentGuard.TestHelpers
        {
            public sealed class InMemoryFileSystemStore
            {
                public InMemoryFileSystemStore(string tempRoot, bool caseSensitive) { }
                public void SetCaseSensitive(bool caseSensitive) { }
            }

            public sealed class SystemServicesBuilder
            {
                public void SimulateCaseSensitivity(bool caseSensitive) { }
            }
        }
        """;

    [Fact]
    public async Task LiteralTrueCaseSensitive_IsReported()
    {
        const string body = "InMemoryFileSystemStore Make(string root) => new InMemoryFileSystemStore(root, caseSensitive: true);";

        Diagnostic diagnostic = Assert.Single(await RunAsync(body));

        Assert.Equal("AG0036", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task LiteralFalseCaseSensitive_IsReported()
    {
        const string body = "InMemoryFileSystemStore Make(string root) => new InMemoryFileSystemStore(root, false);";

        Diagnostic diagnostic = Assert.Single(await RunAsync(body));

        Assert.Equal("AG0036", diagnostic.Id);
    }

    [Fact]
    public async Task RuntimeCaseSensitive_IsNotReported()
    {
        const string body = "InMemoryFileSystemStore Make(string root, bool cs) => new InMemoryFileSystemStore(root, cs);";

        Assert.Empty(await RunAsync(body));
    }

    [Fact]
    public async Task SetCaseSensitiveMethodWithLiteral_IsNotReported()
    {
        // SetCaseSensitive is a post-construction switch, not the constructor's caseSensitive parameter, so a literal
        // there is the deliberate test entry point the rule excludes by construction.
        const string body = "void Flip(InMemoryFileSystemStore store) => store.SetCaseSensitive(true);";

        Assert.Empty(await RunAsync(body));
    }

    [Fact]
    public async Task SimulateCaseSensitivityMethodWithLiteral_IsNotReported()
    {
        const string body = "void Flip(SystemServicesBuilder builder) => builder.SimulateCaseSensitivity(false);";

        Assert.Empty(await RunAsync(body));
    }

    [Fact]
    public async Task LiteralCaseSensitiveOnDecoyStore_IsNotReported()
    {
        // A decoy store in a FOREIGN namespace whose caseSensitive is not the owned overlay — the namespace+name pin
        // leaves it alone.
        const string source = """
            namespace Decoy
            {
                public sealed class InMemoryFileSystemStore
                {
                    public InMemoryFileSystemStore(string tempRoot, bool caseSensitive) { }
                }

                public sealed class Consumer
                {
                    public InMemoryFileSystemStore Make(string root) => new InMemoryFileSystemStore(root, true);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoLiteralCaseModeSeedAnalyzer>(source));
    }

    [Fact]
    public async Task LiteralCaseSensitiveOnStoreInForeignAssembly_IsNotReported()
    {
        // An InMemoryFileSystemStore with the SAME namespace + name but compiled into a DIFFERENT assembly than
        // AgentGuard.TestHelpers cannot self-grant the rule: the identity now pins the declaring assembly through
        // WellKnownType.IsInAssembly, so a same-named overlay elsewhere is left alone even with a literal caseSensitive
        // (mirrors the sibling assembly-gate tests such as AG0027's InNonTestHelpersAssembly case).
        const string referenceSource = """
            namespace AgentGuard.TestHelpers
            {
                public sealed class InMemoryFileSystemStore
                {
                    public InMemoryFileSystemStore(string tempRoot, bool caseSensitive) { }
                }
            }
            """;
        const string consumerSource = """
            namespace App
            {
                using AgentGuard.TestHelpers;
                public class Consumer
                {
                    InMemoryFileSystemStore Make(string root) => new InMemoryFileSystemStore(root, caseSensitive: true);
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<NoLiteralCaseModeSeedAnalyzer>(
            consumerSource, "AnalyzerUnderTest", referenceSource, "AgentGuard.Engine"));
    }

    // Wraps a consumer body in the shared preamble and runs the analyzer. The compilation is named
    // AgentGuard.TestHelpers so the stand-in overlay carries the declaring-assembly identity the analyzer pins on
    // (the real construction site lives inside that assembly), exactly as GuardedConstructionAnalyzerTests does for
    // AG0027.
    private static Task<ImmutableArray<Diagnostic>> RunAsync(string consumerBody)
    {
        string source = Preamble
            + "\n\nnamespace App\n{\n    using AgentGuard.TestHelpers;\n    public class Consumer\n    {\n        "
            + consumerBody
            + "\n    }\n}\n";
        return AnalyzerRunner.RunAsync<NoLiteralCaseModeSeedAnalyzer>(source, "AgentGuard.TestHelpers");
    }
}
