// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Globalization;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0034 (the guard of the guard): every member of ISystemServices must be a service accessor — a property or a
/// no-argument method returning a Contracts interface — or the TimeProvider clock, so the derived service set can never
/// silently miss a new service (derive-service-set-from-isystemservices). It passes on the current well-formed
/// container and fires on any off-convention member; it is scoped to ISystemServices only.
/// </summary>
public class SystemServicesMemberMustBeServiceAccessorAnalyzerTests
{
    [Fact]
    public async Task WellFormedContainer_IsNotReported()
    {
        // Interface-typed property accessors plus the TimeProvider clock — the current shape. AG0034 is preventive here.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileReader { }
                public interface ISystemServices
                {
                    IFileReader FileReader { get; }
                    System.TimeProvider Clock { get; }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<SystemServicesMemberMustBeServiceAccessorAnalyzer>(source));
    }

    [Fact]
    public async Task NoArgMethodAccessor_IsNotReported()
    {
        // A no-argument method returning a Contracts interface is a service accessor, exactly like a property.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileReader { }
                public interface ISystemServices { IFileReader GetFileReader(); }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<SystemServicesMemberMustBeServiceAccessorAnalyzer>(source));
    }

    [Fact]
    public async Task ParameterizedInterfaceReturningMethod_IsReported()
    {
        // A parameterized factory that returns a Contracts interface is NOT an accessor — the walk cannot see the
        // service it hands out — so it is a build error on the container.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFoo { }
                public interface ISystemServices { IFoo GetFoo(string key); }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<SystemServicesMemberMustBeServiceAccessorAnalyzer>(source));
        Assert.Equal("AG0034", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("GetFoo", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task NonServiceReturningMember_IsReported()
    {
        // A member whose value type is not a Contracts interface (and is not the clock) is off-convention and fires.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface ISystemServices { string Describe(); }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<SystemServicesMemberMustBeServiceAccessorAnalyzer>(source));
        Assert.Equal("AG0034", diagnostic.Id);
        Assert.Contains("Describe", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StaticField_IsReported()
    {
        // A field is not a service accessor; it is a build error on the container.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface ISystemServices { static int Counter = 0; }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<SystemServicesMemberMustBeServiceAccessorAnalyzer>(source));
        Assert.Equal("AG0034", diagnostic.Id);
        Assert.Contains("Counter", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OffConventionMemberInheritedFromBaseInterface_IsReported()
    {
        // The container surface AG0034 guards is its own members PLUS every interface it extends — the exact surface the
        // derivation walks (BoundaryServices.SurfaceMembers). A parameterized factory declared on a BASE interface
        // ISystemServices extends is inherited onto the container, yet the walk cannot see the service it hands out.
        // Both interfaces are declared here, so AG0034 fires on that inherited member: the guard-of-the-guard cannot be
        // sidestepped by hiding an off-convention member behind a base interface.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IWidget { }
                public interface IWidgetFactoryBase { IWidget GetWidget(string key); }
                public interface ISystemServices : IWidgetFactoryBase { }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<SystemServicesMemberMustBeServiceAccessorAnalyzer>(source));
        Assert.Equal("AG0034", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("GetWidget", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubContainerWithParameterizedFactory_IsNotChecked()
    {
        // The rule is scoped to ISystemServices only. A sub-container (IFileSystem) legitimately mixes a service
        // accessor and a parameterized factory; its parameterized factory is NOT flagged, because AG0034 checks only
        // the container's own members. On ISystemServices, FileSystem is a service accessor, so nothing fires.
        const string source = """
            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IFileInfo { }
                public interface IFileReader { }
                public interface IFileSystem
                {
                    IFileReader GetFileReader();
                    IFileInfo GetFileInfo(string path);
                }
                public interface ISystemServices { IFileSystem FileSystem { get; } }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<SystemServicesMemberMustBeServiceAccessorAnalyzer>(source));
    }
}
