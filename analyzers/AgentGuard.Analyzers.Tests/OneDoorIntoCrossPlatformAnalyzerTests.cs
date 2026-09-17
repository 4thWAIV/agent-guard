// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class OneDoorIntoCrossPlatformAnalyzerTests
{
    private const string RuleId = OneDoorIntoCrossPlatformAnalyzer.DiagnosticId;

    // The core AgentGuard.CrossPlatform assembly, exposing the one allowed adapter-factory (CrossPlatformAdapters), a
    // type that must NOT be reached directly, and a same-named decoy in another namespace of the same assembly.
    private const string CrossPlatformSource = """
        namespace AgentGuard.CrossPlatform
        {
            public class CrossPlatformAdapters
            {
                public static CrossPlatformAdapters Create() => null!;
            }

            public class FileReader
            {
                public static object Read(string path) => null!;
            }
        }

        namespace DecoyNamespace
        {
            public static class CrossPlatformAdapters
            {
                public static object Create() => null!;
            }
        }
        """;

    // A second fake assembly that is NOT the core AgentGuard.CrossPlatform but squats its namespace AND the door's
    // type name. The door identity pins the declaring assembly, so this cannot pose as the door.
    private const string DecoyAssemblySource = """
        namespace AgentGuard.CrossPlatform
        {
            public static class CrossPlatformAdapters
            {
                public static object Create() => null!;
            }
        }
        """;

    // A GENUINE per-OS implementation assembly, which shares the AgentGuard.CrossPlatform namespace with the core
    // assembly. It is AG0029's territory, not this rule's, and the per-OS exclusion in this rule's Engine-facing filter
    // is what keeps it so.
    private const string PerOsAssemblySource = """
        namespace AgentGuard.CrossPlatform
        {
            public static class PlatformServices
            {
                public static object Create() => null!;
            }
        }
        """;

    private const string CallAdapterFactorySource = $$"""
        {{SharedAnalyzerSources.CrossPlatformUsing}}

        public class Composition
        {
            public object Build() => CrossPlatformAdapters.Create();
        }
        """;

    private const string CallOtherCrossPlatformTypeSource = $$"""
        {{SharedAnalyzerSources.CrossPlatformUsing}}

        public class Composition
        {
            public object Build() => FileReader.Read("x");
        }
        """;

    // The door TYPE and the permitted CALL on it. One owner, so the name is not re-spelled at each fixture and
    // assertion.
    private const string DoorType = "CrossPlatformAdapters";

    private const string DoorMember = DoorType + ".Create";

    private const string DoorCall = DoorMember + "()";

    // The guarded CrossPlatform-core type that is NOT the door, named once for the shared reference-position table,
    // and the member on it a fixture calls.
    private const string GuardedType = "FileReader";

    private const string GuardedMember = GuardedType + ".Read";

    // The guarded type as the type-reference lenses spell it in a message and as a using alias binds it — fully
    // qualified. One owner, so the qualification is not re-spelled at the alias fixture and at each assertion.
    private const string GuardedTypeQualified =
        SharedAnalyzerSources.CrossPlatformNamespace + "." + GuardedType;

    // The accusation the type-reference lenses make about the guarded type. One owner, so it is not rebuilt at each
    // reference-position test.
    private static readonly string GuardedTypeAccusation =
        SharedAnalyzerSources.ReferencedTypeAccusation(GuardedTypeQualified);

    // Every position in which an Engine member can REACH the guarded FileReader type without calling a member on it,
    // built by the one owner of that table; AG0029's tests drive the same positions against their own guarded type.
    public static TheoryData<string> GuardedTypeReferences =>
        SharedAnalyzerSources.TypeReferencePositions(GuardedType);

    [Fact]
    public async Task CallIntoAdapterFactory_FromBoundaries_IsNotReported()
    {
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoCrossPlatformAnalyzer>(
            CallAdapterFactorySource, "AgentGuard.Boundaries", CrossPlatformSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task CallIntoOtherCrossPlatformType_FromBoundaries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoCrossPlatformAnalyzer>(
                CallOtherCrossPlatformTypeSource, "AgentGuard.Boundaries", CrossPlatformSource, "AgentGuard.CrossPlatform");

        Assert.Single(diagnostics).AssertReported(RuleId);
    }

    [Fact]
    public async Task ReferenceToOtherCrossPlatformType_FromBoundaries_IsNotReported()
    {
        // The Boundaries gate is unchanged by the Engine type-reference extension: it stays member-access only, so a
        // type reference that calls nothing is not reported there.
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoCrossPlatformAnalyzer>(
            SharedAnalyzerSources.BoundariesTypeReference(SharedAnalyzerSources.CrossPlatformUsing, GuardedType),
            "AgentGuard.Boundaries",
            CrossPlatformSource,
            "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task CallIntoOtherCrossPlatformType_FromEngine_IsReported()
    {
        // This input is byte for byte the one the pre-relocation test asserted was NOT reported from a non-Boundaries
        // assembly. After the relocation AgentGuard.Engine is gated too, so the prohibited call is reported — by the
        // member-use lens and again by the written-name lens, which both see the same prohibited reach.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoCrossPlatformAnalyzer>(
                CallOtherCrossPlatformTypeSource, SharedAnalyzerSources.EngineAssemblyName, CrossPlatformSource, "AgentGuard.CrossPlatform");

        // Both conditions failed: the reach is not the door and the site is not the container factory, so the message
        // names both.
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
    public async Task DoorLocalDeclaration_InsideContainerFactory_IsNotReported()
    {
        // The exact form Create() uses today: the door type as the declared type of a local inside Create().
        string statements = "            " + DoorType + " adapters = " + DoorCall + ";\n"
            + "            return adapters;";
        string source = SharedAnalyzerSources.InsideContainerFactoryWithBody(
            SharedAnalyzerSources.CrossPlatformUsing, statements);

        Assert.Empty(await RunFromEngineAsync(source));
    }

    [Fact]
    public async Task DoorLocalDeclaration_InAnotherEngineMethod_IsReported()
    {
        string statements = "            " + DoorType + " adapters = null!;\n"
            + "            return adapters;";
        string source = SharedAnalyzerSources.InsideOtherContainerMethodWithBody(
            SharedAnalyzerSources.CrossPlatformUsing, statements);

        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(source);

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

        // Both lenses see the same prohibited reach. The reach IS the door, so every message names the site alone —
        // never the falsehood that the door is not the door.
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
    public async Task DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported()
    {
        // The privileged CALLER's namespace — the leg of its identity that neither the assembly name nor the type
        // name covers. The class is named SystemServices, its Create() is static, it is compiled into the real
        // AgentGuard.Engine assembly, and the call it makes is the genuine door; only the namespace is wrong. The
        // caller identity is the conjunction, so the reach is reported, and because it IS the door every message
        // names the site alone.
        string source = SharedAnalyzerSources.InsideContainerFactoryInWrongNamespace(
            SharedAnalyzerSources.CrossPlatformUsing, DoorCall);

        ImmutableArray<Diagnostic> diagnostics = await RunFromEngineAsync(source);

        // All three of this rule's Engine-facing lenses see the one reach, so three diagnostics are required, not
        // one: the call lens and the carried-type lens both report at the call, and the written-name lens reports at
        // the door type's own name node. The expected spans are spelled from the fixture's own constants.
        diagnostics.AssertSpans(source, DoorCall, DoorCall, DoorType);
        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task SameNamedDoorInAnotherNamespace_FromContainerFactory_IsReported()
    {
        // Right assembly, wrong namespace: the door identity is the namespace-plus-assembly conjunction. The site is
        // the container factory, so the imitation is the only condition that failed.
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
        // Right namespace and type name, wrong declaring assembly. The Engine-facing scope filter admits the shared
        // AgentGuard.CrossPlatform namespace from any assembly that is not one of the three genuine per-OS
        // implementations, so the decoy lands in scope and then FAILS the door's assembly-pinned identity — instead of
        // falling out of scope and being silently accepted at the one site that may call the real door.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoCrossPlatformAnalyzer>(
                SharedAnalyzerSources.InsideContainerFactory(SharedAnalyzerSources.CrossPlatformUsing, DoorCall),
                SharedAnalyzerSources.EngineAssemblyName,
                DecoyAssemblySource,
                "AgentGuard.CrossPlatform.Decoy");

        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation(DoorMember),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task PerOsDoorCall_FromContainerFactory_IsNotReported()
    {
        // The other side of the widened Engine-facing filter, and the one that keeps the production call legal. The
        // AgentGuard.CrossPlatform namespace is shared with the three genuine per-OS implementations, so the filter
        // admits it only from an assembly that is NOT one of them. PlatformServices.Create() reached from
        // SystemServices.Create() is AG0029's permitted door, and this rule stays silent about it.
        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoCrossPlatformAnalyzer>(
            SharedAnalyzerSources.InsideContainerFactory(
                SharedAnalyzerSources.CrossPlatformUsing, "PlatformServices.Create()"),
            SharedAnalyzerSources.EngineAssemblyName,
            PerOsAssemblySource,
            "AgentGuard.CrossPlatform.MacOS"));
    }

    [Theory]
    [MemberData(nameof(GuardedTypeReferences))]
    public async Task GuardedTypeReference_FromEngine_IsReported(string declaration)
    {
        // Every one of these reaches a CrossPlatform-core type without calling a member on it, from an Engine class
        // that is not the container factory. Both lenses are live, so the count is not asserted — only what the
        // diagnostics say. The reach is not the door, so the site is irrelevant and must not be faulted.
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
        // A type outside the guarded assembly is in neither rule's set, whatever its namespace: IPlatformServices lives
        // in AgentGuard.Abstractions, so no reference to it is ever reported here.
        Assert.Empty(await RunFromEngineAsync(
            SharedAnalyzerSources.EngineAbstractionsTypeReference(SharedAnalyzerSources.CrossPlatformUsing)));
    }

    [Fact]
    public async Task DocumentationReferenceToGuardedType_FromEngine_IsNotReported()
    {
        // A cref names the guarded type without reaching it, so the existing documentation links survive the rule.
        string documented = SharedAnalyzerSources.DocumentedMember(GuardedType, GuardedMember);
        string source = SharedAnalyzerSources.EngineHolder(SharedAnalyzerSources.CrossPlatformUsing, documented);

        SharedAnalyzerSources.AssertHasDocumentationReference(source);
        Assert.Empty(await RunFromEngineAsync(source));
    }

    private static Task<ImmutableArray<Diagnostic>> RunFromEngineAsync(string source)
    {
        return AnalyzerRunner.RunWithReferenceAsync<OneDoorIntoCrossPlatformAnalyzer>(
            source, SharedAnalyzerSources.EngineAssemblyName, CrossPlatformSource, "AgentGuard.CrossPlatform");
    }
}
