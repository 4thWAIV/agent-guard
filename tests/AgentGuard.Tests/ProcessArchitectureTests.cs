// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace AgentGuard.Tests;

/// <summary>
/// Proves which CPU architecture the test process is actually executing as (decision intel-mac-via-rosetta,
/// contract item 8 / Acceptance 6). Building the osx-x64 binary does NOT make the tests run as x64 — only
/// launching the x64 .NET under Rosetta does. This test PRINTS <see cref="RuntimeInformation.ProcessArchitecture"/>
/// so the Rosetta CI leg's log shows <c>ProcessArchitecture=X64</c>, and ASSERTS it: membership always, and an
/// exact match when the leg pins the expectation via <c>AGENTGUARD_EXPECT_ARCH</c> (the Rosetta step sets X64,
/// so a leg that silently fell back to arm64 fails the test instead of quietly passing).
/// </summary>
public sealed class ProcessArchitectureTests
{
    private readonly ITestOutputHelper output;

    public ProcessArchitectureTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void ProcessArchitectureIsReportedAndAsserted()
    {
        Architecture process = RuntimeInformation.ProcessArchitecture;
        Architecture os = RuntimeInformation.OSArchitecture;

        // Printed to both the xUnit sink and stdout so the leg log shows it under `-l "console;verbosity=detailed"`.
        string line = $"ProcessArchitecture={process}";
        this.output.WriteLine(line);
        this.output.WriteLine($"OSArchitecture={os}");
        Console.WriteLine(line);
        Console.WriteLine($"OSArchitecture={os}");

        // Membership teeth: we only ever build/test the two 64-bit RIDs; anything else (e.g. X86) is a real bug.
        Assert.True(
            process is Architecture.X64 or Architecture.Arm64,
            $"Unexpected ProcessArchitecture '{process}' — expected X64 (Intel/Rosetta leg) or Arm64 (native leg).");
        Assert.Equal(8, IntPtr.Size);

        // Exact teeth when a leg pins its expectation. The osx-x64 Rosetta leg sets AGENTGUARD_EXPECT_ARCH=X64,
        // so a run that did not actually land on x64 FAILS here rather than passing on a log grep alone.
        string? expected = Environment.GetEnvironmentVariable("AGENTGUARD_EXPECT_ARCH");
        if (!string.IsNullOrWhiteSpace(expected))
        {
            Assert.True(
                Enum.TryParse(expected, ignoreCase: true, out Architecture want),
                $"AGENTGUARD_EXPECT_ARCH='{expected}' is not a valid Architecture name.");
            Assert.Equal(want, process);
        }
    }
}
