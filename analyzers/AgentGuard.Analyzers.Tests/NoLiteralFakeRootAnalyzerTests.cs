// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0035 (no-literal-fake-root): a compile-time literal passed at a fake-root seed site — the
/// <c>home</c>/<c>currentDirectory</c>/<c>tempDirectory</c> argument of <c>AgentGuard.TestHelpers.FakeEnvironment.Create</c>,
/// or the <c>tempRoot</c> argument of the overlay factory <c>SystemServicesBuilder.NewOverlay</c> — is a build error, so
/// the copy-on-write simulator takes a real-host value on every OS. The check targets the <c>NewOverlay</c> call, NOT
/// the <c>InMemoryFileSystemStore</c> constructor: the constructor is only ever reached through <c>NewOverlay</c>, which
/// forwards its own parameter, so a re-hardcode would be a literal at the <c>NewOverlay</c> call. Only those named
/// arguments of those members, and only a value the caller explicitly wrote, are inspected; a runtime value, an omitted
/// optional argument, and a same-named member on another type are all left alone.
/// </summary>
public class NoLiteralFakeRootAnalyzerTests
{
    // The overlay/store/NewOverlay stand-in is the shared owner SharedAnalyzerSources.OverlaySeedHelpers (spelled once,
    // consumed by both seed-site rule test classes); this preamble prepends it with the FakeEnvironment stand-in — the
    // extra seed site only AG0035 reads — matched by namespace + name AND the declaring assembly, plus a consumer whose
    // body each test replaces. NewOverlay forwards its own tempRoot/caseSensitive parameters to the store constructor,
    // exactly as the real builder does, so the preamble's own construction seeds from parameters (never a literal) and
    // adds no diagnostic. RunAsync compiles this preamble into an assembly named AgentGuard.TestHelpers so the seed
    // types carry the identity the analyzer pins on (WellKnownType.IsInAssembly); a foreign-assembly stand-in is left
    // alone, proven separately below.
    private const string Preamble = SharedAnalyzerSources.OverlaySeedHelpers + """

        namespace AgentGuard.TestHelpers
        {
            public sealed class FakeEnvironment
            {
                public static FakeEnvironment Create(
                    string home, string currentDirectory = null, string tempDirectory = null) => new FakeEnvironment();
            }
        }
        """;

    [Fact]
    public async Task LiteralHomeAtFakeEnvironmentCreate_IsReported()
    {
        const string body = """FakeEnvironment Make() => FakeEnvironment.Create("/literal-fake-home");""";

        Diagnostic diagnostic = Assert.Single(await RunAsync(body));

        Assert.Equal("AG0035", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task ConstHomeAtFakeEnvironmentCreate_IsReported()
    {
        // A const reference is a compile-time literal exactly as a bare "..." — the retired DefaultFakeHome shape.
        const string body = """
            private const string Home = "/literal-fake-home";
            FakeEnvironment Make() => FakeEnvironment.Create(Home);
            """;

        Diagnostic diagnostic = Assert.Single(await RunAsync(body));

        Assert.Equal("AG0035", diagnostic.Id);
    }

    [Fact]
    public async Task RuntimeHomeAtFakeEnvironmentCreate_IsNotReported()
    {
        const string body = "FakeEnvironment Make(string home) => FakeEnvironment.Create(home);";

        Assert.Empty(await RunAsync(body));
    }

    [Fact]
    public async Task LiteralNamedCurrentDirectory_IsReported()
    {
        // home is runtime (no fire); currentDirectory is a named literal (fires). Proves per-argument matching.
        const string body = """FakeEnvironment Make(string home) => FakeEnvironment.Create(home, currentDirectory: "/cur");""";

        Diagnostic diagnostic = Assert.Single(await RunAsync(body));

        Assert.Equal("AG0035", diagnostic.Id);
    }

    [Fact]
    public async Task OmittedOptionalArgument_IsNotCounted()
    {
        // home and currentDirectory are explicit literals (two diagnostics); the OMITTED tempDirectory must NOT add a
        // third, or every caller would fail on the compiler-supplied null default.
        const string body = """FakeEnvironment Make() => FakeEnvironment.Create("/literal-fake-home", currentDirectory: "/cur");""";

        ImmutableArray<Diagnostic> diagnostics = await RunAsync(body);

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0035", diagnostic.Id));
    }

    [Fact]
    public async Task LiteralTempRootAtNewOverlayCall_IsReported()
    {
        // The tempRoot seed is visible only at the NewOverlay CALL — NewOverlay forwards its own parameter to the store
        // constructor, so a re-hardcoded literal here is exactly the surface the constructor-site check cannot see.
        const string body = """InMemoryFileSystemStore Make(bool cs) => SystemServicesBuilder.NewOverlay(tempRoot: "/literal-fake-temp", caseSensitive: cs);""";

        Diagnostic diagnostic = Assert.Single(await RunAsync(body));

        Assert.Equal("AG0035", diagnostic.Id);
    }

    [Fact]
    public async Task RuntimeTempRootAtNewOverlayCall_IsNotReported()
    {
        // A real-host-valued tempRoot at the NewOverlay call — what Fake()/SimulateFileSystem actually pass — is left alone.
        const string body = "InMemoryFileSystemStore Make(string root, bool cs) => SystemServicesBuilder.NewOverlay(root, cs);";

        Assert.Empty(await RunAsync(body));
    }

    [Fact]
    public async Task LiteralCreateOnDecoyType_IsNotReported()
    {
        // A static Create with a home parameter on a type that is NOT AgentGuard.TestHelpers.FakeEnvironment — the
        // namespace+name pin leaves it alone, so a data string at an unrelated Create is never a false positive.
        const string source = """
            namespace App
            {
                public sealed class NotFakeEnvironment
                {
                    public static NotFakeEnvironment Create(string home) => new NotFakeEnvironment();
                    public NotFakeEnvironment Make() => Create("/repo");
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoLiteralFakeRootAnalyzer>(source));
    }

    [Fact]
    public async Task LiteralHomeAtFakeEnvironmentCreateInForeignAssembly_IsNotReported()
    {
        // A FakeEnvironment with the SAME namespace + name but compiled into a DIFFERENT assembly than
        // AgentGuard.TestHelpers cannot self-grant the rule: the identity now pins the declaring assembly through
        // WellKnownType.IsInAssembly, so a same-named type elsewhere is left alone even with a literal home (mirrors
        // the sibling assembly-gate tests such as AG0027's InNonTestHelpersAssembly case).
        const string referenceSource = """
            namespace AgentGuard.TestHelpers
            {
                public sealed class FakeEnvironment
                {
                    public static FakeEnvironment Create(
                        string home, string currentDirectory = null, string tempDirectory = null) => new FakeEnvironment();
                }
            }
            """;
        const string consumerSource = """
            namespace App
            {
                using AgentGuard.TestHelpers;
                public class Consumer
                {
                    FakeEnvironment Make() => FakeEnvironment.Create("/literal-fake-home");
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<NoLiteralFakeRootAnalyzer>(
            consumerSource, "AnalyzerUnderTest", referenceSource, "AgentGuard.Engine"));
    }

    // Binds this rule's preamble and analyzer type to the shared seed-site wrapper (SeedSiteAnalyzerRunner), which owns
    // the byte-identical source-construction-and-run logic once for both seed-site rule test classes.
    private static Task<ImmutableArray<Diagnostic>> RunAsync(string consumerBody)
        => SeedSiteAnalyzerRunner.RunAsync<NoLiteralFakeRootAnalyzer>(Preamble, consumerBody);
}
