// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0110: the low-level Tmds.DBus.Protocol package is confined to the IPolkitAuthority owner in the Linux assembly;
/// the reflection-based high-level Tmds.DBus namespace is banned everywhere.
/// </summary>
public class DbusOnlyInPolkitAuthorityAnalyzerTests
{
    // Fake stand-ins for the package types (net9.0 references have neither Tmds namespace). The rule matches by the
    // referenced type's namespace, so a source-declared type in that namespace exercises it exactly. The IPolkitAuthority
    // owner stub is the shared owner SharedAnalyzerSources.PolkitAuthorityPort (spelled once, consumed by the three
    // Linux-port rule test classes), appended after the two Tmds package stubs.
    private const string DbusStubs = """
        namespace Tmds.DBus.Protocol
        {
            public sealed class Connection
            {
                public void Send() { }
            }
        }

        namespace Tmds.DBus
        {
            public sealed class HighLevelConnection
            {
                public void Send() { }
            }
        }

        """ + SharedAnalyzerSources.PolkitAuthorityPort;

    [Fact]
    public async Task LowLevelDbus_OutsideOwner_IsReported()
    {
        string source = DbusStubs + """

            namespace AgentGuard.CrossPlatform.Linux
            {
                using Tmds.DBus.Protocol;
                internal sealed class Rogue
                {
                    internal Connection Open() => new Connection();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<DbusOnlyInPolkitAuthorityAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
        Assert.Equal("AG0110", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task LowLevelDbus_InOwner_IsNotReported()
    {
        string source = DbusStubs + """

            namespace AgentGuard.CrossPlatform.Linux
            {
                using Tmds.DBus.Protocol;
                internal sealed class TmdsPolkitAuthority : IPolkitAuthority
                {
                    internal Connection Open() => new Connection();
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<DbusOnlyInPolkitAuthorityAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
    }

    [Fact]
    public async Task LowLevelDbus_InOwnerButWrongAssembly_IsReported()
    {
        // The owner is exempt only in the Linux assembly; implementing IPolkitAuthority elsewhere does not self-grant.
        string source = DbusStubs + """

            namespace AgentGuard.CrossPlatform.Linux
            {
                using Tmds.DBus.Protocol;
                internal sealed class TmdsPolkitAuthority : IPolkitAuthority
                {
                    internal Connection Open() => new Connection();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<DbusOnlyInPolkitAuthorityAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0110", diagnostic.Id);
    }

    [Fact]
    public async Task HighLevelDbus_InOwner_IsReported()
    {
        // The high-level reflection API is banned everywhere, owner or not.
        string source = DbusStubs + """

            namespace AgentGuard.CrossPlatform.Linux
            {
                using Tmds.DBus;
                internal sealed class TmdsPolkitAuthority : IPolkitAuthority
                {
                    internal HighLevelConnection Open() => new HighLevelConnection();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<DbusOnlyInPolkitAuthorityAnalyzer>(source, "AgentGuard.CrossPlatform.Linux"));
        Assert.Equal("AG0110", diagnostic.Id);
    }
}
