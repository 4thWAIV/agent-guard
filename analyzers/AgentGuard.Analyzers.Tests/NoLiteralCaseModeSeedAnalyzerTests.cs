// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0036 (no-literal-case-mode-seed): a <c>true</c>/<c>false</c> literal passed as the <c>caseSensitive</c> argument
/// of the overlay factory <c>SystemServicesBuilder.NewOverlay</c> is a build error, so the simulator's case mode is
/// seeded from the real host. The check targets the <c>NewOverlay</c> call, NOT the <c>InMemoryFileSystemStore</c>
/// constructor: the constructor is only ever reached through <c>NewOverlay</c>, which forwards its own parameter, so a
/// re-hardcode would be a literal at the <c>NewOverlay</c> call. Only the <c>caseSensitive</c> argument at that site is
/// inspected; the deliberate <c>SetCaseSensitive</c>/<c>SimulateCaseSensitivity</c> entry points are
/// methods, neither the constructor nor <c>NewOverlay</c>, and are never flagged.
/// </summary>
public class NoLiteralCaseModeSeedAnalyzerTests
{
    // Stand-ins for the overlay, the overlay factory NewOverlay, and the builder entry points come from the shared owner
    // SharedAnalyzerSources.OverlaySeedHelpers (spelled once, consumed by both seed-site rule test classes), matched by
    // namespace + name AND the declaring assembly. NewOverlay forwards its own tempRoot/caseSensitive parameters to the
    // store constructor, exactly as the real builder does, so the preamble's own construction seeds from parameters
    // (never a literal) and adds no diagnostic. RunAsync compiles this preamble into an assembly named
    // AgentGuard.TestHelpers so the overlay carries the identity the analyzer pins on (WellKnownType.IsInAssembly).
    // SetCaseSensitive (on the store) and SimulateCaseSensitivity (on the builder) are the deliberate post-construction
    // switches the rule must leave alone.
    private const string Preamble = SharedAnalyzerSources.OverlaySeedHelpers;

    [Fact]
    public async Task LiteralTrueCaseSensitiveAtNewOverlayCall_IsReported()
    {
        // The case mode seed is visible only at the NewOverlay CALL — NewOverlay forwards its own parameter to the store
        // constructor, so a re-hardcoded literal here is exactly the surface the constructor-site check cannot see.
        const string body = "InMemoryFileSystemStore Make(string root) => SystemServicesBuilder.NewOverlay(root, caseSensitive: true);";

        Diagnostic diagnostic = Assert.Single(await RunAsync(body));

        Assert.Equal("AG0036", diagnostic.Id);
    }

    [Fact]
    public async Task RuntimeCaseSensitiveAtNewOverlayCall_IsNotReported()
    {
        // A real-host-valued caseSensitive at the NewOverlay call — what Fake()/SimulateFileSystem actually pass — is left alone.
        const string body = "InMemoryFileSystemStore Make(string root, bool cs) => SystemServicesBuilder.NewOverlay(root, cs);";

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

    // Binds this rule's preamble and analyzer type to the shared seed-site wrapper (SeedSiteAnalyzerRunner), which owns
    // the byte-identical source-construction-and-run logic once for both seed-site rule test classes.
    private static Task<ImmutableArray<Diagnostic>> RunAsync(string consumerBody)
        => SeedSiteAnalyzerRunner.RunAsync<NoLiteralCaseModeSeedAnalyzer>(Preamble, consumerBody);
}
