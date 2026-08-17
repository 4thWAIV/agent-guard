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
    // A synthetic ISystemServices mirroring the REAL shipped container: the IFileSystem sub-container (whose no-arg
    // accessors expose the four filesystem services and whose parameterized factories return IFileInfo/IDirectoryInfo),
    // environment, guids, console, signatures, build-info, the platform sub-container (which nests IPlatformFileSystem),
    // and the TimeProvider clock. The four filesystem services are reached THROUGH IFileSystem's accessors, not as
    // direct container properties (the pre-bridge four-property shape is gone). IFileInfo/IDirectoryInfo exist only as
    // the return of the parameterized factories on IFileSystem, so they are reachable but NOT accessors. Deriving over
    // this must yield exactly the ten.
    private const string RealShapeContainer = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IDirectoryEnumerator { }
            public interface IFileWriter { }
            public interface IDirectoryWriter { }
            public interface IEnvironment { }
            public interface IGuidFactory { }
            public interface IConsole { }
            public interface ISignatureService { }
            public interface IBuildInfo { }
            public interface IFileInfo { }
            public interface IDirectoryInfo { }
            public interface IFileSystem
            {
                IFileInfo GetFileInfo(string path);
                IDirectoryInfo GetDirectoryInfo(string path);
                IFileReader GetFileReader();
                IDirectoryEnumerator GetDirectoryReader();
                IFileWriter GetFileWriter();
                IDirectoryWriter GetDirectoryWriter();
            }
            public interface IPlatformFileSystem
            {
                bool NeedsExecutableFlag();
            }
            public interface IPlatformServices
            {
                IPlatformFileSystem FileSystem { get; }
            }
            public interface ISystemServices
            {
                IFileSystem FileSystem { get; }
                IEnvironment Environment { get; }
                IGuidFactory Guids { get; }
                IConsole Console { get; }
                IPlatformServices Platform { get; }
                ISignatureService Signatures { get; }
                IBuildInfo BuildInfo { get; }
                System.TimeProvider Clock { get; }
            }
        }
        """;

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
