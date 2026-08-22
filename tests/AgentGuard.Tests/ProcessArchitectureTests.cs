// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace AgentGuard.Tests;

/// <summary>
/// Proves which CPU architecture the test process is actually executing as (decision intel-mac-via-rosetta,
/// contract item 8 / Acceptance 6). Building the osx-x64 binary does NOT make the tests run as x64 — only
/// launching the x64 .NET under Rosetta does. This test PRINTS the process architecture (read through the owned
/// <see cref="IEnvironment.GetProcessArchitecture"/>) so the Rosetta CI leg's log shows <c>ProcessArchitecture=X64</c>,
/// and ASSERTS it: membership always, and an exact match when the leg pins the expectation via
/// <c>AGENTGUARD_EXPECT_ARCH</c> (the Rosetta step sets X64, so a leg that silently fell back to arm64 fails the test
/// instead of quietly passing). It runs on <see cref="SystemServicesBuilder.Real"/> so the architecture, the env-var
/// read, and the stdout write all hit the real host.
/// </summary>
public sealed class ProcessArchitectureTests
{
    private readonly ITestOutputHelper output;

    public ProcessArchitectureTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void ProcessArchitectureIsReportedAndAsserted()
    {
        ISystemServices services = SystemServicesBuilder.Real().Build();
        Architecture process = services.Environment.GetProcessArchitecture();
        Architecture os = services.Environment.GetOSArchitecture();

        // Printed to both the xUnit sink and real stdout (a Real() console) so the leg log shows it under
        // `-l "console;verbosity=detailed"`.
        string line = $"ProcessArchitecture={process}";
        this.output.WriteLine(line);
        this.output.WriteLine($"OSArchitecture={os}");
        services.Console.WriteLine(line);
        services.Console.WriteLine($"OSArchitecture={os}");

        // Membership teeth: we only ever build/test the two 64-bit RIDs; anything else (e.g. X86) is a real bug.
        Assert.True(
            process is Architecture.X64 or Architecture.Arm64,
            $"Unexpected ProcessArchitecture '{process}' — expected X64 (Intel/Rosetta leg) or Arm64 (native leg).");
        Assert.Equal(8, IntPtr.Size);

        // Exact teeth when a leg pins its expectation. The osx-x64 Rosetta leg sets AGENTGUARD_EXPECT_ARCH=X64,
        // so a run that did not actually land on x64 FAILS here rather than passing on a log grep alone.
        string? expected = services.Environment.GetEnvironmentVariable("AGENTGUARD_EXPECT_ARCH");
        if (!string.IsNullOrWhiteSpace(expected))
        {
            Assert.True(
                Enum.TryParse(expected, ignoreCase: true, out Architecture want),
                $"AGENTGUARD_EXPECT_ARCH='{expected}' is not a valid Architecture name.");
            Assert.Equal(want, process);
        }
    }

    /// <summary>
    /// Proves the <see cref="SystemServicesBuilder.Fake"/> default environment reports the REAL host CPU architecture —
    /// the builder sources it once through <see cref="SystemServicesBuilder.Real"/> — rather than the hardcoded
    /// <c>Architecture.X64</c> the fake used to default to. The rest of the fake environment stays controlled; only the
    /// two architecture values are delegated to the real host.
    /// </summary>
    [Fact]
    public void FakeDefaultEnvironmentReportsRealHostArchitecture()
    {
        IEnvironment real = SystemServicesBuilder.Real().Build().Environment;
        IEnvironment fake = SystemServicesBuilder.Fake().Build().Environment;

        Assert.Equal(real.GetProcessArchitecture(), fake.GetProcessArchitecture());
        Assert.Equal(real.GetOSArchitecture(), fake.GetOSArchitecture());
    }

    /// <summary>
    /// Proves the architecture stays overridable: a test that substitutes its own <see cref="FakeEnvironment"/> through
    /// the builder's <c>With</c> reports the simulated architecture, which wins over the real-host default.
    /// </summary>
    [Fact]
    public void FakeEnvironmentArchitectureOverrideWins()
    {
        IEnvironment realEnvironment = SystemServicesBuilder.Real().Build().Environment;
        Architecture host = realEnvironment.GetProcessArchitecture();
        Architecture simulated = host == Architecture.Arm64 ? Architecture.X64 : Architecture.Arm64;
        Assert.NotEqual(host, simulated);

        IEnvironment fake = SystemServicesBuilder.Fake()
            .With(FakeEnvironment.Create(
                realEnvironment.GetHomeDirectory(),
                processArchitecture: simulated,
                osArchitecture: simulated))
            .Build()
            .Environment;

        Assert.Equal(simulated, fake.GetProcessArchitecture());
        Assert.Equal(simulated, fake.GetOSArchitecture());
    }
}
