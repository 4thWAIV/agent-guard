// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class EngineInternalsOneDoorAnalyzerTests
{
    private const string RuleId = EngineInternalsOneDoorAnalyzer.DiagnosticId;

    private const string TestHelpersAssembly = "AgentGuard.TestHelpers";

    private const string TestsAssembly = "AgentGuard.Tests";

    // The subjects a message names — every Engine internal the fixtures below reach, as the diagnostic spells it.
    private const string EngineInternalRead = "AgentGuard.Engine.EngineInternal.Read()";

    private const string EngineCacheOfInt = "AgentGuard.Engine.EngineCache<int>";

    private const string ListOfEngineInternal =
        "System.Collections.Generic.List<AgentGuard.Engine.EngineInternal>";

    private const string ListOfEngineInternalArray =
        "System.Collections.Generic.List<AgentGuard.Engine.EngineInternal[]>";

    private const string ContainerType = "AgentGuard.Engine.SystemServices";

    // The container factory in its two roles: the one reach the grant covers, named in every message's tail, and the
    // subject a message names when the source reaches it as anything other than that call. One identity, spelled once.
    private const string ContainerCreate = "AgentGuard.Engine.SystemServices.Create()";

    private const string ContainerOther = "AgentGuard.Engine.SystemServices.Other()";

    private const string ListOfContainer = "System.Collections.Generic.List<AgentGuard.Engine.SystemServices>";

    private const string DecoyContainerType = "AgentGuard.Engine.Decoy.SystemServices";

    private const string DecoyContainerCreate = "AgentGuard.Engine.Decoy.SystemServices.Create()";

    // A var local whose inferred type is an Engine internal — the carried-type lens with nothing written out.
    private const string InferredLocalDeclaration =
        "        internal static object Inferred() { var value = Declared(); return value; }\n"
        + "        private static EngineInternal Declared() => null!;";

    // The two gated consumers. Every reach is proved from BOTH, because the grant the relocation adds is to both.
    public static TheoryData<string> GatedAssemblies => new()
    {
        SharedAnalyzerSources.GuardAssemblyName,
        TestHelpersAssembly,
    };

    // Every position in which the PUBLIC Engine surface may be reached — each one its own case against each of the two
    // consumers, so the six forms stand as twelve independently reported scenarios rather than one bundled fixture.
    // A permitted row carries an EMPTY expected-message collection, which is what lets the one Cross helper build this
    // table and the two prohibited ones below: a permitted row and a prohibited row differ in nothing but what they
    // expect, so no scenario has to be bundled away to avoid a second near-identical helper.
    public static TheoryData<string, string, string[]> PermittedPublicReaches => Cross(
        Reach("        internal static object Typed() => typeof(EnginePublic);"),
        Reach("        internal static object Member() => EnginePublic.Read();"),
        Reach("        private static readonly EnginePublic Field = null!;"),
        Reach("        internal static EnginePublic Returned() => null!;"),
        Reach("        internal static object Parameter(EnginePublic value) => value;"),
        Reach("        internal static object Argument() => new System.Collections.Generic.List<EnginePublic>();"));

    // Every reach into an Engine internal that is NOT the approved call, proved from both gated consumers, each with
    // the diagnostics it must produce. The first group is the written-name lens (a name the source writes); the second
    // is the carried-type lens (a type a declaration or an invocation carries without naming it). The two lenses are
    // separate registrations that each report what they see, so a declaration standing in both lenses is named twice —
    // the expectation lists one subject per diagnostic, which is what pins the count.
    public static TheoryData<string, string, string[]> ProhibitedReaches => Cross(
        Reach("        internal static object Typed() => typeof(EngineInternal);", SharedAnalyzerSources.EngineInternalTypeName),
        Reach("        internal static string Named() => nameof(EngineInternal);", SharedAnalyzerSources.EngineInternalTypeName),
        Reach("        internal static object Cast(object value) => (EngineInternal)value;", SharedAnalyzerSources.EngineInternalTypeName),
        Reach("        internal static bool Pattern(object value) => value is EngineInternal;", SharedAnalyzerSources.EngineInternalTypeName),
        Reach(
            "        internal static object Argument() => new System.Collections.Generic.List<EngineInternal>();",
            ListOfEngineInternal,
            SharedAnalyzerSources.EngineInternalTypeName),
        Reach("        internal static object Constructed() => new EngineCache<int>();", EngineCacheOfInt),
        Reach(
            "        internal static object Member() => EngineInternal.Read();",
            SharedAnalyzerSources.EngineInternalTypeName,
            EngineInternalRead),
        Reach("        private static readonly EngineInternal Field = null!;", SharedAnalyzerSources.EngineInternalTypeName, SharedAnalyzerSources.EngineInternalTypeName),
        Reach("        internal static EngineInternal Property => null!;", SharedAnalyzerSources.EngineInternalTypeName, SharedAnalyzerSources.EngineInternalTypeName),
        Reach("        internal static EngineInternal Returned() => null!;", SharedAnalyzerSources.EngineInternalTypeName, SharedAnalyzerSources.EngineInternalTypeName),
        Reach(
            "        internal static object Parameter(EngineInternal value) => value;",
            SharedAnalyzerSources.EngineInternalTypeName,
            SharedAnalyzerSources.EngineInternalTypeName),
        Reach(
            "        internal static object Local() { EngineInternal value = null!; return value; }",
            SharedAnalyzerSources.EngineInternalTypeName,
            SharedAnalyzerSources.EngineInternalTypeName),
        Reach(
            InferredLocalDeclaration,
            SharedAnalyzerSources.EngineInternalTypeName,
            SharedAnalyzerSources.EngineInternalTypeName,
            SharedAnalyzerSources.EngineInternalTypeName,
            SharedAnalyzerSources.EngineInternalTypeName,
            SharedAnalyzerSources.EngineInternalTypeName),
        Reach(
            "        internal static object Nested() { System.Collections.Generic.List<EngineInternal[]> all = null!; return all; }",
            ListOfEngineInternalArray,
            SharedAnalyzerSources.EngineInternalTypeName,
            ListOfEngineInternalArray));

    // The container type is permitted in exactly ONE position — the receiver of an invocation of its static Create.
    // Every other position, and every other member of it, is reported.
    public static TheoryData<string, string, string[]> ProhibitedContainerReaches => Cross(
        Reach("        internal static object Typed() => typeof(SystemServices);", ContainerType),
        Reach("        internal static string Named() => nameof(SystemServices);", ContainerType),
        Reach("        internal static object Cast(object value) => (SystemServices)value;", ContainerType),
        Reach("        internal static bool Pattern(object value) => value is SystemServices;", ContainerType),
        Reach(
            "        internal static object Argument() => new System.Collections.Generic.List<SystemServices>();",
            ListOfContainer,
            ContainerType),
        Reach("        private static readonly SystemServices Field = null!;", ContainerType, ContainerType),
        Reach("        internal static SystemServices Property => null!;", ContainerType, ContainerType),
        Reach("        internal static SystemServices Returned() => null!;", ContainerType, ContainerType),
        Reach("        internal static object Parameter(SystemServices value) => value;", ContainerType, ContainerType),
        Reach(
            "        internal static object Local() { SystemServices value = null!; return value; }",
            ContainerType,
            ContainerType),
        Reach("        internal static object OtherMember() => SystemServices.Other();", ContainerType, ContainerOther),
        Reach(
            "        internal static System.Func<object> MethodGroup() => SystemServices.Create;",
            ContainerType,
            ContainerCreate));

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task ApprovedContainerFactoryCall_IsNotReported(string assemblyName)
    {
        // The compliant fixture: the one approved reach, from each gated consumer. The fake Engine grants both
        // assemblies internal access, so this genuinely compiles and the assertion turns on the analyzer.
        const string body = "        internal static object Build() => SystemServices.Create();";

        Assert.Empty(await RunAsync(Consumer(SharedAnalyzerSources.EngineUsing, body), assemblyName));
    }

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task FullyQualifiedApprovedContainerFactoryCall_IsNotReported(string assemblyName)
    {
        // However the source spells the path to it, the approved call is the same reach.
        const string body =
            "        internal static object Build() => AgentGuard.Engine.SystemServices.Create();";

        Assert.Empty(await RunAsync(Consumer(string.Empty, body), assemblyName));
    }

    // All three tables run through this ONE theory, and each of their rows is its own independently reported case:
    // twelve permitted public reaches, every reach into an Engine internal, and the container reached anywhere other
    // than the approved call. The tables stay separate because they prove separate things — that the public surface is
    // never reported, that the gate covers every Engine internal, and that the one exception is no wider than
    // SystemServices.Create() — but a row's assertion is the same for all three, so it is written once: a permitted
    // row expects nothing and a prohibited row expects its own messages.
    [Theory]
    [MemberData(nameof(PermittedPublicReaches))]
    [MemberData(nameof(ProhibitedReaches))]
    [MemberData(nameof(ProhibitedContainerReaches))]
    public async Task Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics(
        string assemblyName, string declaration, string[] expectedSubjects)
    {
        ArgumentNullException.ThrowIfNull(expectedSubjects);

        await AssertReportsAsync(
            Consumer(SharedAnalyzerSources.EngineUsing, declaration), assemblyName, expectedSubjects);
    }

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task EngineInternalInBaseList_FromGatedConsumer_IsReported(string assemblyName)
    {
        await AssertReportsAsync(
            SharedAnalyzerSources.GatedConsumer(
                SharedAnalyzerSources.EngineUsing, string.Empty, SharedAnalyzerSources.DerivedFrom("EngineInternal")),
            assemblyName,
            SharedAnalyzerSources.EngineInternalTypeName);
    }

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task ContainerInBaseList_FromGatedConsumer_IsReported(string assemblyName)
    {
        // The fake container is unsealed with a protected constructor precisely so this fixture COMPILES — its
        // namespace, assembly, name, internal visibility and base list are the governed identity and are untouched,
        // and production SystemServices stays sealed. What the fixture proves is that the exception covers the
        // invocation receiver and nothing else: the same type named in a base list is reported.
        await AssertReportsAsync(
            SharedAnalyzerSources.GatedConsumer(
                SharedAnalyzerSources.EngineUsing, string.Empty, SharedAnalyzerSources.DerivedFrom("SystemServices")),
            assemblyName,
            ContainerType);
    }

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task EngineInternalThroughUsingAlias_FromGatedConsumer_IsReported(string assemblyName)
    {
        // Both halves are a reach: the alias declaration names the guarded type, and the aliased name resolves to it.
        string aliasUsing =
            SharedAnalyzerSources.AliasUsing(SharedAnalyzerSources.EngineUsing, SharedAnalyzerSources.EngineInternalTypeName);

        await AssertReportsAsync(
            SharedAnalyzerSources.GatedConsumer(
                aliasUsing, SharedAnalyzerSources.TypeofAliasDeclaration, string.Empty),
            assemblyName,
            SharedAnalyzerSources.EngineInternalTypeName,
            SharedAnalyzerSources.EngineInternalTypeName);
    }

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task ContainerThroughUsingAlias_FromGatedConsumer_IsReported(string assemblyName)
    {
        // The aliased CALL is the approved reach and is accepted; the alias DECLARATION names the container outside
        // that call, which the exception does not cover, so exactly one diagnostic stands.
        string aliasUsing = SharedAnalyzerSources.AliasUsing(SharedAnalyzerSources.EngineUsing, ContainerType);
        string body = "        internal static object Build() => " + SharedAnalyzerSources.AliasName + ".Create();";

        await AssertReportsAsync(Consumer(aliasUsing, body), assemblyName, ContainerType);
    }

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task DocumentationReferenceToEngineInternal_IsNotReported(string assemblyName)
    {
        string documented = SharedAnalyzerSources.DocumentedMember("EngineInternal", "SystemServices.Other");
        string source = Consumer(SharedAnalyzerSources.EngineUsing, documented);

        SharedAnalyzerSources.AssertHasDocumentationReference(source);
        Assert.Empty(await RunAsync(source, assemblyName));
    }

    [Theory]
    [MemberData(nameof(GatedAssemblies))]
    public async Task SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported(string assemblyName)
    {
        // The privileged identity is the namespace-plus-assembly-plus-name conjunction: a class merely NAMED
        // SystemServices, with a static Create, declared in AgentGuard.Engine.Decoy, is just another Engine internal —
        // so both the decoy type and its Create are reported.
        const string body =
            "        internal static object Build() => AgentGuard.Engine.Decoy.SystemServices.Create();";

        await AssertReportsAsync(
            Consumer(string.Empty, body), assemblyName, DecoyContainerType, DecoyContainerCreate);
    }

    [Fact]
    public async Task EveryProhibitedReach_FromTestsAssembly_IsNotReported()
    {
        // AgentGuard.Tests is deliberately NOT gated, so its pre-existing grant is untouched. It carries the same
        // InternalsVisibleTo grant the two gated consumers do, so this fixture COMPILES and the empty result is the
        // analyzer's gate rather than an inaccessibility error.
        const string body = "        internal static object Typed() => typeof(EngineInternal);\n\n"
            + "        internal static object Member() => EngineInternal.Read();\n\n"
            + "        private static readonly SystemServices Field = null!;\n\n"
            + "        internal static object OtherMember() => SystemServices.Other();";

        Assert.Empty(await RunAsync(Consumer(SharedAnalyzerSources.EngineUsing, body), TestsAssembly));
    }

    // One reach and the diagnostics it must produce: the declaration to drop into the consumer, and one expected
    // subject per expected diagnostic. A PERMITTED reach names no subject at all, which is how the same row shape
    // carries a permitted case and a prohibited one.
    private static (string Declaration, string[] Subjects) Reach(string declaration, params string[] subjects)
    {
        return (declaration, subjects);
    }

    // The one helper that crosses a table of reaches with the two gated consumers, so every reach is its own case
    // against each of them and no scenario is bundled into another to save a helper.
    private static TheoryData<string, string, string[]> Cross(params (string Declaration, string[] Subjects)[] reaches)
    {
        var data = new TheoryData<string, string, string[]>();
        foreach ((string declaration, string[] subjects) in reaches)
        {
            data.Add(SharedAnalyzerSources.GuardAssemblyName, declaration, subjects);
            data.Add(TestHelpersAssembly, declaration, subjects);
        }

        return data;
    }

    // The one assertion for a reach: the exact number of diagnostics, every one of them this rule reported at its
    // declared severity, and each message exactly as spelled above — count and content, never mere non-emptiness. A
    // permitted reach hands over an EMPTY expectation, which these same assertions pin as nothing reported at all.
    private static async Task AssertReportsAsync(
        string source, string assemblyName, params string[] expectedSubjects)
    {
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(source, assemblyName).ConfigureAwait(false);

        Assert.Equal(expectedSubjects.Length, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => diagnostic.AssertReported(RuleId));
        Assert.Equal(
            expectedSubjects.Select(subject => ExpectedMessage(assemblyName, subject))
                .OrderBy(message => message, StringComparer.Ordinal),
            diagnostics.Select(diagnostic => diagnostic.Message())
                .OrderBy(message => message, StringComparer.Ordinal));
    }

    // The message AG0041 has to produce, SPELLED OUT here rather than read from the analyzer: an expectation taken
    // from the thing under test proves only that it equals itself. It names the gated assembly that reached, the
    // Engine internal it reached, and the one reach the grant covers.
    private static string ExpectedMessage(string assemblyName, string subject)
    {
        return "'" + assemblyName + "' reaches the AgentGuard.Engine internal '" + subject
            + "'; the only Engine internal it may reach is " + ContainerCreate;
    }

    // The consumer shell with no extra type declarations beside it — the shape all but two fixtures here need.
    private static string Consumer(string usings, string body)
    {
        return SharedAnalyzerSources.GatedConsumer(usings, body, string.Empty);
    }

    private static Task<ImmutableArray<Diagnostic>> RunAsync(string source, string assemblyName)
    {
        return SharedAnalyzerSources.RunAgainstFakeEngineAsync<EngineInternalsOneDoorAnalyzer>(source, assemblyName);
    }
}
