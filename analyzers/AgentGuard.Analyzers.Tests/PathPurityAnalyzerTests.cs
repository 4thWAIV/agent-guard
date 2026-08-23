// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class PathPurityAnalyzerTests
{
    // The GetTempPath owner: a class implementing IEnvironment in AgentGuard.Boundaries.
    private const string GetTempPathOwnerSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IEnvironment { }
        }

        namespace Boundaries
        {
            public sealed class EnvironmentAdapter : AgentGuard.Abstractions.Contracts.IEnvironment
            {
                public string Temp() => Path.GetTempPath();
            }
        }
        """;

    // The GetRandomFileName owner: a class implementing IRandomGenerator in AgentGuard.CrossPlatform.
    private const string GetRandomFileNameOwnerSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IRandomGenerator { }
        }

        namespace CrossPlatform
        {
            public sealed class RandomGeneratorAdapter : AgentGuard.Abstractions.Contracts.IRandomGenerator
            {
                public string Name() => Path.GetRandomFileName();
            }
        }
        """;

    // The DirectorySeparatorChar owner: a class implementing IPlatformFileSystem, in a per-OS library or the fake.
    private const string SeparatorOwnerSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IPlatformFileSystem { }
        }

        namespace CrossPlatform
        {
            public sealed class PosixFileSystem : AgentGuard.Abstractions.Contracts.IPlatformFileSystem
            {
                public char Sep() => Path.DirectorySeparatorChar;
            }
        }
        """;

    [Fact]
    public async Task GetTempPath_IsReported()
    {
        // Impure: GetTempPath reads process/OS state, so it is banned everywhere (default-deny) and routed through
        // IEnvironment.
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Temp() => Path.GetTempPath();
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("GetTempPath", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SingleArgumentGetFullPath_IsReported()
    {
        // The single-argument GetFullPath resolves a relative path against the current directory — impure — so it is
        // banned; only the two-argument overload is pure (AG0012's clause folded in here).
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Full(string path) => Path.GetFullPath(path);
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Contains("GetFullPath", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeparatorField_IsReported()
    {
        // The raw separator FIELD is not on the pure allowlist; reading it goes behind IPlatformFileSystem. The shared
        // scanner sees field reads (scanner-sees-field-reads), so a static-readonly field read is caught.
        const string source = """
            using System.IO;

            public class Sample
            {
                public char Separator() => Path.DirectorySeparatorChar;
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Contains("DirectorySeparatorChar", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetTempPath_InEnvironmentOwner_IsExempt()
    {
        // Path.GetTempPath is reused behind IEnvironment: legal inside the IEnvironment implementer in
        // AgentGuard.Boundaries.
        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(GetTempPathOwnerSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task GetTempPath_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        // Implementing IEnvironment in the wrong assembly does not grant the exemption — both halves of the conjunction
        // are required.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(GetTempPathOwnerSource, "AgentGuard.Engine"));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Contains("GetTempPath", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetRandomFileName_InRandomGeneratorOwner_IsExempt()
    {
        // Path.GetRandomFileName is reused behind IRandomGenerator: legal inside the IRandomGenerator implementer in
        // AgentGuard.CrossPlatform.
        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(GetRandomFileNameOwnerSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task GetRandomFileName_OutsideOwner_IsReported()
    {
        // Outside the IRandomGenerator owner it is banned (default-deny).
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Name() => Path.GetRandomFileName();
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Contains("GetRandomFileName", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DirectorySeparatorChar_InPlatformOwnerPerOs_IsExempt()
    {
        // The raw separator field is owned by the IPlatformFileSystem implementers: legal in a per-OS library.
        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(SeparatorOwnerSource, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task DirectorySeparatorChar_InFake_IsExempt()
    {
        // The in-memory fake in AgentGuard.TestHelpers is an owner of the separator read too
        // (directoryseparator-owned-passthrough), so it is exempt.
        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(SeparatorOwnerSource, "AgentGuard.TestHelpers"));
    }

    [Fact]
    public async Task DirectorySeparatorChar_InCoreCrossPlatformAssembly_IsReported_SelfGrantBlocked()
    {
        // Implementing IPlatformFileSystem in the core AgentGuard.CrossPlatform contract assembly (not a per-OS library,
        // not the fake) does not grant the separator exemption — the gate is the per-OS libraries plus TestHelpers only.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(SeparatorOwnerSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0020", diagnostic.Id);
        Assert.Contains("DirectorySeparatorChar", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PureCombine_IsNotReported()
    {
        // Combine is a pure function of its arguments — legal everywhere, no abstraction required.
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Join(string a, string b) => Path.Combine(a, b);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
    }

    [Fact]
    public async Task TwoArgumentGetFullPath_IsNotReported()
    {
        // The two-argument GetFullPath(path, basePath) is pure — it takes the base explicitly rather than reading the
        // current directory — so it stays legal.
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Full(string path, string basePath) => Path.GetFullPath(path, basePath);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<PathPurityAnalyzer>(source));
    }
}
