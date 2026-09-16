// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class OneDoorIntoPerOsAnalyzerTests
{
    private const string RuleId = OneDoorIntoPerOsAnalyzer.DiagnosticId;

    // A per-OS implementation assembly exposing the one allowed door (PlatformServices.Create), another member that
    // must NOT be reached directly, and a same-named decoy in another namespace of the same assembly.
    private const string PerOsSource = """
        namespace AgentGuard.CrossPlatform
        {
            public class PlatformServices
            {
                public static object Create() => null!;
            }

            public class PosixFileSystem
            {
                public static object Probe(string path) => null!;
            }
        }

        namespace DecoyNamespace
        {
            public static class PlatformServices
            {
                public static object Create() => null!;
            }
        }
        """;

    private const string CallPlatformServicesCreateSource = $$"""
        {{SharedAnalyzerSources.CrossPlatformUsing}}

        public class Composition
        {
            public object Build() => PlatformServices.Create();
        }
        """;

    private const string CallOtherPerOsMemberSource = $$"""
        {{SharedAnalyzerSources.CrossPlatformUsing}}

        public class Composition
        {
            public object Build() => PosixFileSystem.Probe("x");
        }
        """;

    // The door TYPE, and the two forms a fixture reaches it through: the permitted CALL, and the type NAMED in a
    // typeof. One owner, so the name is not re-spelled at each fixture and assertion.
    private const string DoorType = "PlatformServices";

    private const string DoorMember = DoorType + ".Create";

    private const string DoorCall = DoorMember + "()";

    private const string DoorTypeReference = "typeof(" + DoorType + ")";

    private const string PerOsAssembly = "AgentGuard.CrossPlatform.MacOS";

    // The guarded per-OS type that is NOT the door, named once for the shared reference-position table, and the member
    // on it a fixture calls.
    private const string GuardedType = "PosixFileSystem";

    private const string GuardedMember = GuardedType + ".Probe";

    // The guarded type and the door type as the type-reference lenses spell them in a message and as a using alias
    // binds them — fully qualified. One owner each, so the qualification is not re-spelled per fixture and assertion.
    private const string GuardedTypeQualified =
        SharedAnalyzerSources.CrossPlatformNamespace + "." + GuardedType;

    private const string DoorTypeQualified = SharedAnalyzerSources.CrossPlatformNamespace + "." + DoorType;

    // An assembly that squats the shared AgentGuard.CrossPlatform namespace but is neither the core assembly nor one
    // of the three genuine per-OS implementations.
    private const string DecoyAssembly = "AgentGuard.CrossPlatform.Decoy";

    // The accusation the type-reference lenses make about the guarded type. One owner, so it is not rebuilt at each
    // reference-position test.
    private static readonly string GuardedTypeAccusation =
        SharedAnalyzerSources.ReferencedTypeAccusation(GuardedTypeQualified);

    // Every position in which an Engine member can REACH the guarded PosixFileSystem type without calling a member on
    // it, built by the one owner of that table; AG0023's tests drive the same positions against their own guarded type.
    public static TheoryData<string> GuardedTypeReferences =>
        SharedAnalyzerSources.TypeReferencePositions(GuardedType);

    [Fact]
    public async Task CallPlatformServicesCreate_FromBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoPerOsAnalyzer>(
            CallPlatformServicesCreateSource, "AgentGuard.Boundaries", PerOsSource, PerOsAssembly));
    }

    [Fact]
    public async Task CallOtherPerOsMember_FromBoundaries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoPerOsAnalyzer>(
                CallOtherPerOsMemberSource, "AgentGuard.Boundaries", PerOsSource, PerOsAssembly);

        Assert.Single(diagnostics).AssertReported(RuleId);
    }

    [Fact]
    public async Task CallOldPlatformDoor_FromBoundaries_IsReported()
    {
        // The now-old separate Platform factory is NO LONGER the door: after the retarget the one legal door is
        // PlatformServices.Create(), so a Boundaries call to Platform.Create() into a per-OS assembly is reported.
        const string perOsWithOldPlatformDoor = """
            namespace AgentGuard.CrossPlatform
            {
                public static class Platform
                {
                    public static object Create() => null!;
                }
            }
            """;

        const string callOldPlatformDoor = $$"""
            {{SharedAnalyzerSources.CrossPlatformUsing}}

            public class Composition
            {
                public object Build() => Platform.Create();
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoPerOsAnalyzer>(
                callOldPlatformDoor, "AgentGuard.Boundaries", perOsWithOldPlatformDoor, PerOsAssembly);

        Assert.Single(diagnostics).AssertReported(RuleId);
    }

    [Fact]
    public async Task ReferenceToOtherPerOsType_FromBoundaries_IsNotReported()
    {
        // The Boundaries gate is unchanged by the Engine type-reference extension: it stays member-access only, so a
        // type reference that calls nothing is not reported there.
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoPerOsAnalyzer>(
            SharedAnalyzerSources.BoundariesTypeReference(SharedAnalyzerSources.CrossPlatformUsing, GuardedType),
            "AgentGuard.Boundaries",
            PerOsSource,
            PerOsAssembly));
    }

    [Fact]
    public async Task CallOtherPerOsMember_FromEngine_IsReported()
    {
        // This input is byte for byte the one the pre-relocation test asserted was NOT reported from a non-Boundaries
        // assembly. After the relocation AgentGuard.Engine is gated too, so the prohibited call is reported. Both
        // conditions failed: the reach is not the door and the site is not the container factory.
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(CallOtherPerOsMemberSource);

        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation(GuardedMember),
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task DoorCall_FromContainerFactory_IsNotReported()
    {
        // The compliant fixture: the one door, reached from the one permitted site.
        Assert.Empty(await RunFromEngineAsync(
            SharedAnalyzerSources.InsideContainerFactory(SharedAnalyzerSources.CrossPlatformUsing, DoorCall)));
    }

    [Fact]
    public async Task DoorTypeReference_InsideContainerFactory_IsNotReported()
    {
        // The form Create() uses today: the door type named on a qualified call inside Create().
        string statements = "            object platform = global::AgentGuard.CrossPlatform." + DoorCall + ";\n"
            + "            return platform;";
        string source = SharedAnalyzerSources.InsideContainerFactoryWithBody(
            SharedAnalyzerSources.CrossPlatformUsing, statements);

        Assert.Empty(await RunFromEngineAsync(source));
    }

    [Fact]
    public async Task DoorTypeReference_InAnotherEngineMethod_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.InsideOtherContainerMethod(
                SharedAnalyzerSources.CrossPlatformUsing, DoorTypeReference));

        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.ReferenceSiteFailureFragment);
    }

    [Fact]
    public async Task DoorCall_FromAnotherContainerMethod_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.InsideOtherContainerMethod(SharedAnalyzerSources.CrossPlatformUsing, DoorCall));

        // The reach IS the door, so every message names the site alone — never the falsehood that the door is not
        // the door.
        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task DoorCall_FromAnotherEngineClass_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.InsideOtherEngineClass(SharedAnalyzerSources.CrossPlatformUsing, DoorCall));

        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task DoorCall_FromLambdaInsideContainerFactory_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.InsideLambdaInContainerFactory(
                SharedAnalyzerSources.CrossPlatformUsing, DoorCall));

        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task DoorCall_FromLocalFunctionInsideContainerFactory_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.InsideLocalFunctionInContainerFactory(
                SharedAnalyzerSources.CrossPlatformUsing, DoorCall));

        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task NonDoorCall_FromContainerFactory_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.InsideContainerFactory(
                SharedAnalyzerSources.CrossPlatformUsing, GuardedMember + "(\"x\")"));

        // The site is right and the member is wrong, so the message names the member alone.
        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation(GuardedMember),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task SameNamedDoorInAnotherNamespace_FromContainerFactory_IsReported()
    {
        // Right assembly, wrong namespace. The site is the container factory, so the imitation is the only condition
        // that failed.
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.InsideContainerFactory("using DecoyNamespace;", DoorCall));

        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation(DoorMember),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task SameNamedDoorInAnotherAssembly_FromContainerFactory_IsReported()
    {
        // The wrong-assembly axis, rejected by THIS rule. Right namespace and type name, wrong declaring assembly: a
        // decoy PlatformServices compiled into AgentGuard.CrossPlatform.Decoy. The Engine-facing scope filter admits a
        // foreign-assembly squatter of the shared AgentGuard.CrossPlatform namespace, so the decoy lands in this
        // rule's own scope and then FAILS this rule's own door test — whose identity conjoins the PlatformServices
        // name with the three genuine per-OS assemblies — instead of falling out of scope and being silently accepted
        // at the one site that may call the real door. AG0023 reports the same decoy from its own side; overlapping
        // there is expected, and neither rule may be silent about an imitation of its own door.
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAgainstDecoyAsync(
            SharedAnalyzerSources.InsideContainerFactory(SharedAnalyzerSources.CrossPlatformUsing, DoorCall));

        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation(DoorMember),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task SameNamedDoorTypeReferenceInAnotherAssembly_InsideContainerFactory_IsReported()
    {
        // The wrong-assembly axis on the TYPE half of the door test, isolated from the member half. The fixture above
        // writes the imitation as a CALL, which the call gate reports through the door MEMBER test; that leaves the
        // door TYPE test — the one the written-name lens consults — proved by nothing, because a non-empty result from
        // either lens satisfies the assertion. Here the imitation is named and never called, so the written-name lens
        // is the only lens that can fire and the door TYPE test is the only thing that can reject it.
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAgainstDecoyAsync(
            SharedAnalyzerSources.InsideContainerFactory(
                SharedAnalyzerSources.CrossPlatformUsing, DoorTypeReference));

        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.ReferencedTypeAccusation(DoorTypeQualified),
            failedSiteFragment: null);
    }

    [Theory]
    [MemberData(nameof(GuardedTypeReferences))]
    public async Task GuardedTypeReference_FromEngine_IsReported(string declaration)
    {
        // The reach is not the door, so the site is irrelevant and must not be faulted.
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.EngineHolder(SharedAnalyzerSources.CrossPlatformUsing, declaration));

        diagnostics.AssertNamesExactly(RuleId, GuardedTypeAccusation, failedSiteFragment: null);
    }

    [Fact]
    public async Task GuardedTypeInBaseList_FromEngine_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.EngineDerivedFrom(SharedAnalyzerSources.CrossPlatformUsing, GuardedType));

        diagnostics.AssertNamesExactly(RuleId, GuardedTypeAccusation, failedSiteFragment: null);
    }

    [Fact]
    public async Task GuardedTypeThroughUsingAlias_FromEngine_IsReported()
    {
        string aliasUsing =
            SharedAnalyzerSources.AliasUsing(SharedAnalyzerSources.CrossPlatformUsing, GuardedTypeQualified);

        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(
            SharedAnalyzerSources.EngineHolder(aliasUsing, SharedAnalyzerSources.TypeofAliasDeclaration));

        diagnostics.AssertNamesExactly(RuleId, GuardedTypeAccusation, failedSiteFragment: null);
    }

    [Fact]
    public async Task AbstractionsTypeReference_FromEngine_IsNotReported()
    {
        // A type outside the guarded assemblies is in neither rule's set, whatever its namespace: IPlatformServices
        // lives in AgentGuard.Abstractions, so no reference to it is ever reported here.
        Assert.Empty(await RunFromEngineAsync(
            SharedAnalyzerSources.EngineAbstractionsTypeReference(SharedAnalyzerSources.CrossPlatformUsing)));
    }

    [Fact]
    public async Task DocumentationReferenceToGuardedType_FromEngine_IsNotReported()
    {
        string documented = SharedAnalyzerSources.DocumentedMember(GuardedType, GuardedMember);
        string source = SharedAnalyzerSources.EngineHolder(SharedAnalyzerSources.CrossPlatformUsing, documented);

        SharedAnalyzerSources.AssertHasDocumentationReference(source);
        Assert.Empty(await RunFromEngineAsync(source));
    }

    private static Task<ImmutableArray<Diagnostic>> RunFromEngineAsync(string source)
    {
        return AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoPerOsAnalyzer>(
            source, SharedAnalyzerSources.EngineAssemblyName, PerOsSource, PerOsAssembly);
    }

    // The same Engine run against the SQUATTER assembly instead of a genuine per-OS one, so the referenced
    // PlatformServices is an imitation. Spelled once here rather than at each wrong-assembly test.
    private static Task<ImmutableArray<Diagnostic>> RunFromEngineAgainstDecoyAsync(string source)
    {
        return AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoPerOsAnalyzer>(
            source, SharedAnalyzerSources.EngineAssemblyName, PerOsSource, DecoyAssembly);
    }
}
