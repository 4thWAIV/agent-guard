// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// Proves the boundary service set is DERIVED from ISystemServices at compile time
/// (derive-service-set-from-isystemservices), through the observable behaviour of the three rules that consume it —
/// AG0031 (a lone service parameter) as the probe for "is a service interface", AG0024 (a static holder) as the probe
/// for "is a service type". The derivation is exercised over a synthetic ISystemServices that mirrors the real one, so
/// the derived set is asserted to equal the previously hard-coded ten, and over focused fixtures for the direct,
/// nested, parameterized-factory, and clock cases.
/// </summary>
public class DerivedBoundaryServicesTests
{
    // The synthetic ISystemServices mirroring the REAL shipped container lives once in
    // SharedAnalyzerSources.RealShapeContainer (shared byte-identical with the AG0034 whole-tree test): the IFileSystem
    // sub-container (whose no-arg accessors expose the four filesystem services and whose parameterized factories return
    // IFileInfo/IDirectoryInfo), environment, guids, console, signatures, build-info, the platform sub-container (which
    // nests IPlatformFileSystem), and the TimeProvider clock. The four filesystem services are reached THROUGH
    // IFileSystem's accessors, not as direct container properties; IFileInfo/IDirectoryInfo exist only as the return of
    // the parameterized factories on IFileSystem, so they are reachable but NOT accessors; IPlatformFileSystem's
    // char DirectorySeparator is a non-service leaf member, so it does not change the derived set. Deriving over this
    // must yield exactly the ten leaves and the three containers.
    private const string RealShapeContainer = SharedAnalyzerSources.RealShapeContainer;

    // The exact ordered leaf service interfaces the walk yields over the real shape — the pre-order DFS through
    // FileSystem's four sub-services, then Environment/Guids/Console, then Platform's nested IPlatformFileSystem, then
    // Signatures/BuildInfo. Held here as the regression oracle so the tree extraction (BoundaryServices.ResolveTree +
    // flatten) is proven BYTE-IDENTICAL to the former hand-rolled recursion (contract rule-phase item 1), which is what
    // keeps AG0024/AG0025/AG0031 provably unaffected.
    private static readonly ImmutableArray<(string Namespace, string Name)> ExpectedServiceInterfaces =
        ImmutableArray.Create(
            ("AgentGuard.Abstractions.Contracts", "IFileReader"),
            ("AgentGuard.Abstractions.Contracts", "IDirectoryEnumerator"),
            ("AgentGuard.Abstractions.Contracts", "IFileWriter"),
            ("AgentGuard.Abstractions.Contracts", "IDirectoryWriter"),
            ("AgentGuard.Abstractions.Contracts", "IEnvironment"),
            ("AgentGuard.Abstractions.Contracts", "IGuidFactory"),
            ("AgentGuard.Abstractions.Contracts", "IConsole"),
            ("AgentGuard.Abstractions.Contracts", "IPlatformFileSystem"),
            ("AgentGuard.Abstractions.Contracts", "ISignatureService"),
            ("AgentGuard.Abstractions.Contracts", "IBuildInfo"));

    [Fact]
    public async Task Resolve_OverRealShape_YieldsByteIdenticalServiceInterfacesAndTypes()
    {
        // Drive the internal BoundaryServices.Resolve directly (through the InternalsVisibleTo grant) and pin the EXACT
        // ordered arrays: ServiceInterfaces is the ten leaves in pre-order, ServiceTypes is those ten plus the container
        // ISystemServices and the System.TimeProvider clock, in that order. This is the byte-identical regression guard
        // the tree extraction must never break.
        Compilation compilation = await AnalyzerRunner.CompileAsync(RealShapeContainer, "AgentGuard.Abstractions");
        DerivedServices services = BoundaryServices.Resolve(compilation);

        Assert.Equal<(string, string)>(ExpectedServiceInterfaces, services.ServiceInterfaces);

        ImmutableArray<(string Namespace, string Name)> expectedServiceTypes = ExpectedServiceInterfaces
            .Add(("AgentGuard.Abstractions.Contracts", "ISystemServices"))
            .Add(("System", "TimeProvider"));
        Assert.Equal<(string, string)>(expectedServiceTypes, services.ServiceTypes);
    }

    [Fact]
    public async Task ContainerInterfaces_OverRealShape_AreExactlyTheTriple()
    {
        // AG0022's checked container set is DERIVED from the ONE tree walk (BoundaryServices.ResolveTree), not a
        // hardcoded triple: it is the root container plus every nested container reached through a Container accessor,
        // in pre-order. Over the real shape that is exactly ISystemServices, then IFileSystem (via the FileSystem
        // accessor), then IPlatformServices (via the Platform accessor). The leaf services and the clock are never
        // containers, so none of them appears. This proves the derivation yields the triple today AND would pick up a
        // future nested container with no edit to AG0022.
        Compilation compilation = await AnalyzerRunner.CompileAsync(RealShapeContainer, "AgentGuard.Abstractions");
        ContainerNode root = Assert.IsType<ContainerNode>(BoundaryServices.ResolveTree(compilation));

        ImmutableArray<(string Namespace, string Name)> containers = BoundaryServices.ContainerInterfaces(root);

        Assert.Equal<(string, string)>(
            ImmutableArray.Create(
                ("AgentGuard.Abstractions.Contracts", "ISystemServices"),
                ("AgentGuard.Abstractions.Contracts", "IFileSystem"),
                ("AgentGuard.Abstractions.Contracts", "IPlatformServices")),
            containers);
    }

    [Fact]
    public async Task DerivedServiceInterfaces_OverRealShape_AreExactlyTheTen()
    {
        // One method per prior hard-coded owner interface, INCLUDING the nested IPlatformFileSystem reached through the
        // Platform sub-container. Each fires AG0031, so the derived owner-interface set equals the ten.
        string source = RealShapeContainer + """

            namespace App
            {
                public class Consumer
                {
                    public void UseFileReader(AgentGuard.Abstractions.Contracts.IFileReader s) { }
                    public void UseDirectories(AgentGuard.Abstractions.Contracts.IDirectoryEnumerator s) { }
                    public void UseFileWriter(AgentGuard.Abstractions.Contracts.IFileWriter s) { }
                    public void UseDirectoryWriter(AgentGuard.Abstractions.Contracts.IDirectoryWriter s) { }
                    public void UseEnvironment(AgentGuard.Abstractions.Contracts.IEnvironment s) { }
                    public void UseGuids(AgentGuard.Abstractions.Contracts.IGuidFactory s) { }
                    public void UseConsole(AgentGuard.Abstractions.Contracts.IConsole s) { }
                    public void UsePlatformFileSystem(AgentGuard.Abstractions.Contracts.IPlatformFileSystem s) { }
                    public void UseSignatures(AgentGuard.Abstractions.Contracts.ISignatureService s) { }
                    public void UseBuildInfo(AgentGuard.Abstractions.Contracts.IBuildInfo s) { }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine");
        Assert.Equal(10, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0031", diagnostic.Id));
    }

    [Fact]
    public async Task DerivedServiceInterfaces_OverRealShape_ExcludeSubContainerFactoryReturnAndRoot()
    {
        // The two sub-containers IFileSystem and IPlatformServices, the parameterized-factory returns
        // IFileInfo/IDirectoryInfo, and the root ISystemServices itself are NOT owner interfaces, so none of these
        // parameters is flagged. Together with the ten firing above, this pins the derived set to exactly the ten.
        string source = RealShapeContainer + """

            namespace App
            {
                public class Consumer
                {
                    public void UseFileSystem(AgentGuard.Abstractions.Contracts.IFileSystem s) { }
                    public void UsePlatform(AgentGuard.Abstractions.Contracts.IPlatformServices s) { }
                    public void UseFileInfo(AgentGuard.Abstractions.Contracts.IFileInfo s) { }
                    public void UseDirectoryInfo(AgentGuard.Abstractions.Contracts.IDirectoryInfo s) { }
                    public void UseContainer(AgentGuard.Abstractions.Contracts.ISystemServices s) { }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task DirectInterfaceTypedProperty_IsAService()
    {
        // (a) A direct interface-typed property on ISystemServices is a service.
        string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFoo { }
                public interface ISystemServices { IFoo Foo { get; } }
            }

            namespace App
            {
                public class Consumer
                {
                    public void Use(AgentGuard.Abstractions.Contracts.IFoo foo) { }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0031", diagnostic.Id);
    }

    [Fact]
    public async Task InterfaceThroughSubContainerNoArgMethod_IsAService_SubContainerIsNot()
    {
        // Case b, the nested case: an interface reached through a sub-container no-arg method accessor is a service,
        // while the sub-container itself stays a structural pass-through and is not a service.
        string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface ILeaf { }
                public interface ISub { ILeaf GetLeaf(); }
                public interface ISystemServices { ISub Sub { get; } }
            }

            namespace App
            {
                public class Consumer
                {
                    public void UseLeaf(AgentGuard.Abstractions.Contracts.ILeaf leaf) { }
                    public void UseSub(AgentGuard.Abstractions.Contracts.ISub sub) { }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0031", diagnostic.Id);
        Assert.Contains("UseLeaf", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParameterizedFactoryReturn_IsNotAService_NoArgAccessorSiblingIs()
    {
        // (c) A parameterized factory method (GetFileInfo(string)) is NOT a service — its return interface is not
        // collected — while a no-arg accessor sibling (GetFileReader()) is; the sub-container IFileSystem itself is not.
        string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileInfo { }
                public interface IFileReader { }
                public interface IFileSystem
                {
                    IFileInfo GetFileInfo(string path);
                    IFileReader GetFileReader();
                }
                public interface ISystemServices { IFileSystem FileSystem { get; } }
            }

            namespace App
            {
                public class Consumer
                {
                    public void UseFileInfo(AgentGuard.Abstractions.Contracts.IFileInfo info) { }
                    public void UseFileReader(AgentGuard.Abstractions.Contracts.IFileReader reader) { }
                    public void UseFileSystem(AgentGuard.Abstractions.Contracts.IFileSystem fs) { }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0031", diagnostic.Id);
        Assert.Contains("UseFileReader", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TimeProviderTypedMember_IsAServiceType_ForAG0024()
    {
        // (d) A TimeProvider-typed member makes TimeProvider a service TYPE, so a static TimeProvider holder is flagged
        // by AG0024.
        string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileReader { }
                public interface ISystemServices
                {
                    IFileReader FileReader { get; }
                    System.TimeProvider Clock { get; }
                }
            }

            namespace App
            {
                public class Holder
                {
                    private static System.TimeProvider Clock = null!;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoStaticServiceHolderAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0024", diagnostic.Id);
    }

    [Fact]
    public async Task TimeProviderTypedMember_IsNotAnOwnerInterface_ForAG0031()
    {
        // (d) …but TimeProvider is not an OWNER interface, so a TimeProvider parameter is not flagged by AG0031 (only
        // the IFileReader parameter is).
        string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileReader { }
                public interface ISystemServices
                {
                    IFileReader FileReader { get; }
                    System.TimeProvider Clock { get; }
                }
            }

            namespace App
            {
                public class Consumer
                {
                    public void UseClock(System.TimeProvider clock) { }
                    public void UseFileReader(AgentGuard.Abstractions.Contracts.IFileReader reader) { }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<NoServiceAsParameterAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0031", diagnostic.Id);
        Assert.Contains("UseFileReader", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}
