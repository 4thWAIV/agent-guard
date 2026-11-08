// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class EngineToBoundariesOneDoorAnalyzerTests
{
    private const string RuleId = EngineToBoundariesOneDoorAnalyzer.DiagnosticId;

    // The fake AgentGuard.Boundaries assembly: the four permitted adapter factories, a FIFTH factory that returns a
    // service interface but is not one of the four named identities, a non-factory member on one of the four, and a
    // same-named decoy in another namespace of the same assembly.
    private const string BoundariesSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IEnvironment { }
            public interface IConsole { }
            public interface ISignatureService { }
            public interface IBuildInfo { }
            public interface IGuidFactory { }
        }

        namespace AgentGuard.Boundaries
        {
            using AgentGuard.Abstractions.Contracts;

            public static class EnvironmentAdapter
            {
                public static IEnvironment Create() => null!;

                public static object Probe() => null!;
            }

            public static class ConsoleAdapter
            {
                public static IConsole Create() => null!;
            }

            public static class Ed25519SignatureService
            {
                public static ISignatureService Create() => null!;
            }

            public static class BuildInfoReader
            {
                public static IBuildInfo Create() => null!;
            }

            public static class GuidFactoryAdapter
            {
                public static IGuidFactory Create() => null!;
            }
        }

        namespace DecoyNamespace
        {
            public static class EnvironmentAdapter
            {
                public static object Create() => null!;
            }
        }
        """;

    // A second fake assembly that is NOT AgentGuard.Boundaries but squats its namespace and one of its four type
    // names. The permitted identity is matched on the declaring assembly too, so this cannot pose as a door.
    private const string DecoyAssemblySource = """
        namespace AgentGuard.Boundaries
        {
            public static class ConsoleAdapter
            {
                public static object Create() => null!;
            }
        }
        """;

    private const string BoundariesUsing = "using AgentGuard.Boundaries;";

    // The relocated container's full identity, spelled from the one owner of the Engine assembly name.
    private const string ContainerFactoryType = SharedAnalyzerSources.EngineAssemblyName + ".SystemServices";

    private const string ContainerFactoryMethod = "Create";

    // One of the four permitted identities, written as a fixture calls it. Held once because the compliant theory,
    // the wrong-site test, the wrong-namespace caller test and the wrong-namespace factory test all make this same
    // call and must make the same one.
    private const string PermittedFactoryCall = "EnvironmentAdapter.Create()";

    // An assembly that is NOT AgentGuard.Engine. The container declaration below is compiled into it unchanged, so
    // the only thing that differs from the real composition point is the declaring assembly.
    private const string DecoyEngineAssembly = SharedAnalyzerSources.EngineAssemblyName + ".Decoy";

    // The container alone — the same shell every permitted-site fixture in this class is built on — compiled twice:
    // once as the real Engine assembly and once as the decoy.
    private static readonly string ContainerSource = SharedAnalyzerSources.EngineCompilation(
        string.Empty, SharedAnalyzerSources.InertContainerFactory, string.Empty);

    public static TheoryData<string> PermittedFactoryCalls => new()
    {
        PermittedFactoryCall,
        "ConsoleAdapter.Create()",
        "Ed25519SignatureService.Create()",
        "BuildInfoReader.Create()",
    };

    [Theory]
    [MemberData(nameof(PermittedFactoryCalls))]
    public async Task PermittedFactory_FromContainerFactory_IsNotReported(string call)
    {
        // The compliant fixture: each of the four permitted identities, called from the one permitted site.
        Assert.Empty(await RunAsync(SharedAnalyzerSources.InsideContainerFactory(BoundariesUsing, call)));
    }

    [Fact]
    public async Task PermittedFactory_FromAnotherContainerMethod_IsReported()
    {
        // The site is the Create() METHOD, not the SystemServices type: another method on the same class is reported,
        // and the message names the site alone because the called member IS the door.
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(
            SharedAnalyzerSources.InsideOtherContainerMethod(BoundariesUsing, PermittedFactoryCall));

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task PermittedFactory_FromSameNamedContainerInAnotherNamespace_IsReported()
    {
        // The privileged CALLER's namespace — the leg of its identity that neither the assembly name nor the type
        // name covers. The class is named SystemServices, its Create() is static, it is compiled into the real
        // AgentGuard.Engine assembly, and the call it makes is the genuine permitted factory; only the namespace is
        // wrong. The caller identity is the conjunction, so the call is reported, and because the called member IS
        // the door the message names the site alone.
        string source = SharedAnalyzerSources.InsideContainerFactoryInWrongNamespace(
            BoundariesUsing, PermittedFactoryCall);

        ImmutableArray<Diagnostic> diagnostics = await RunAsync(source);

        // AG0040 registers the call lens alone, so the one prohibited call is reported once, at the call itself.
        diagnostics.AssertSpans(source, PermittedFactoryCall);
        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task PermittedFactory_FromAnotherEngineClass_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(
            SharedAnalyzerSources.InsideOtherEngineClass(BoundariesUsing, "ConsoleAdapter.Create()"));

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task PermittedFactory_FromLambdaInsideContainerFactory_IsReported()
    {
        // The caller test takes the narrowest reading: a lambda nested in Create()'s body is its own method symbol.
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(
            SharedAnalyzerSources.InsideLambdaInContainerFactory(BoundariesUsing, "BuildInfoReader.Create()"));

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task PermittedFactory_FromLocalFunctionInsideContainerFactory_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(
            SharedAnalyzerSources.InsideLocalFunctionInContainerFactory(
                BoundariesUsing, "Ed25519SignatureService.Create()"));

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            failedDoorAccusation: null,
            SharedAnalyzerSources.CallSiteFailureFragment);
    }

    [Fact]
    public async Task FifthFactoryReturningAServiceInterface_FromContainerFactory_IsReported()
    {
        // A return type grants nothing: GuidFactoryAdapter.Create() hands back a service interface and is still not
        // one of the four named identities, so it is rejected until the rule is changed to name it.
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(
            SharedAnalyzerSources.InsideContainerFactory(BoundariesUsing, "GuidFactoryAdapter.Create()"));

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation("GuidFactoryAdapter.Create"),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task NonFactoryMemberOnAPermittedType_FromContainerFactory_IsReported()
    {
        // The identity is the static Create METHOD, not the type: another member on EnvironmentAdapter is not a door.
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(
            SharedAnalyzerSources.InsideContainerFactory(BoundariesUsing, "EnvironmentAdapter.Probe()"));

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation("EnvironmentAdapter.Probe"),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task SameNamedFactoryInAnotherNamespace_IsReported()
    {
        // Right assembly, wrong namespace: the identity conjunction rejects it.
        ImmutableArray<Diagnostic> diagnostics = await RunAsync(
            SharedAnalyzerSources.InsideContainerFactory(
                "using DecoyNamespace;", PermittedFactoryCall));

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation("EnvironmentAdapter.Create"),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task SameNamedFactoryInAnotherAssembly_IsReported()
    {
        // Right namespace and type name, wrong declaring assembly: the identity conjunction rejects it too.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunWithReferenceAsync<EngineToBoundariesOneDoorAnalyzer>(
                SharedAnalyzerSources.InsideContainerFactory(BoundariesUsing, "ConsoleAdapter.Create()"),
                SharedAnalyzerSources.EngineAssemblyName,
                DecoyAssemblySource,
                "AgentGuard.Boundaries.Decoy");

        Assert.Single(diagnostics);
        diagnostics.AssertNamesExactly(
            RuleId,
            SharedAnalyzerSources.CalledMemberAccusation("ConsoleAdapter.Create"),
            failedSiteFragment: null);
    }

    [Fact]
    public async Task AnyBoundariesCall_FromAnotherAssembly_IsNotReported()
    {
        // The rule gates only the AgentGuard.Engine compilation; the same calls from another assembly are not its
        // business. The fixture makes every one of the rejected calls above and still reports nothing.
        const string everyRejectedCall = $$"""
            {{BoundariesUsing}}

            public class Elsewhere
            {
                public object Fifth() => GuidFactoryAdapter.Create();

                public object NonFactory() => EnvironmentAdapter.Probe();

                public object Permitted() => ConsoleAdapter.Create();
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunWithReferenceAsync<EngineToBoundariesOneDoorAnalyzer>(
            everyRejectedCall, "AgentGuard.CrossPlatform", BoundariesSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task ContainerFactoryIdentity_IsTheStaticCreateInTheEngineAssembly()
    {
        // The privileged CALLER, checked on the predicate this gate is built from rather than only through the
        // rule's diagnostics: every gate above accepts a call only when this returns true for the call site's own
        // containing method, so the identity itself is worth pinning directly.
        IMethodSymbol create = await ContainerFactoryAsync(SharedAnalyzerSources.EngineAssemblyName);

        Assert.True(CompositionPoint.IsContainerFactoryMethod(create));
    }

    [Fact]
    public async Task ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly()
    {
        // The wrong-assembly decoy for the CALLER, the counterpart of the wrong-assembly decoy for the CALLEE above.
        // The declaration is byte for byte the same, so the namespace, the type name, the method name and staticness
        // all still match and the ONLY difference is the assembly it is compiled into — and the conjunction rejects
        // it, so a same-named container elsewhere cannot self-grant the exemption. The identity match is the real
        // predicate; nothing here re-spells it.
        IMethodSymbol decoy = await ContainerFactoryAsync(DecoyEngineAssembly);

        Assert.Equal(ContainerFactoryType, decoy.ContainingType.ToDisplayString());
        Assert.Equal(ContainerFactoryMethod, decoy.Name);
        Assert.True(decoy.IsStatic);
        Assert.Equal(DecoyEngineAssembly, decoy.ContainingAssembly.Name);
        Assert.False(CompositionPoint.IsContainerFactoryMethod(decoy));
    }

    // Resolves the container's Create method out of a compilation of ContainerSource named <paramref name="assemblyName"/>.
    private static async Task<IMethodSymbol> ContainerFactoryAsync(string assemblyName)
    {
        Compilation compilation =
            await AnalyzerRunner.CompileAsync(ContainerSource, assemblyName).ConfigureAwait(false);
        INamedTypeSymbol container = SharedAnalyzerSources.TypeIn(compilation, ContainerFactoryType);

        return Assert.IsAssignableFrom<IMethodSymbol>(
            Assert.Single(container.GetMembers(ContainerFactoryMethod)));
    }

    private static Task<ImmutableArray<Diagnostic>> RunAsync(string source)
    {
        return AnalyzerRunner.RunWithReferenceAsync<EngineToBoundariesOneDoorAnalyzer>(
            source, SharedAnalyzerSources.EngineAssemblyName, BoundariesSource, "AgentGuard.Boundaries");
    }
}
