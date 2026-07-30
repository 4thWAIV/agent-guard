// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class IntegrityCheckTests
{
    [Fact]
    public void Acceptance_3c_HealthyInstall_IsAllowed()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        InstallIntegrity.Check(harness.BinGuard).IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Acceptance_3c_HashMismatch_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        File.WriteAllText(harness.MachineStateFile, "{\"version\":\"0.1.0-alpha\",\"sha256\":\"deadbeef\"}");

        IntegrityReport report = InstallIntegrity.Check(harness.BinGuard);

        report.IsAllowed.Should().BeFalse();
        report.Detail.Should().Contain("hash");
    }

    [Fact]
    public void Acceptance_3c_MissingRecord_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        File.Delete(harness.MachineStateFile);

        InstallIntegrity.Check(harness.BinGuard).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3c_UnreadableRecord_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        File.WriteAllText(harness.MachineStateFile, "{ this is not json");

        InstallIntegrity.Check(harness.BinGuard).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3c_RunningBinaryMissing_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        Directory.Delete(Path.Combine(harness.AgentGuardRoot, "versions", "0.1.0-alpha"), recursive: true);

        InstallIntegrity.Check(harness.BinGuard).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3c_UnresolvableCurrent_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        // `current` still points at the installed version's directory, but that version's binary is gone, so
        // `current` no longer resolves to an installed binary. Pass a resolvable binary under the root so the
        // check reaches this branch rather than the "running binary missing" branch above it.
        File.Delete(harness.VersionBinary("0.1.0-alpha"));
        string resolvable = Path.Combine(harness.AgentGuardRoot, "versions", "0.1.0-alpha", "keeper");
        File.WriteAllText(resolvable, "x");

        IntegrityReport report = InstallIntegrity.Check(resolvable);

        report.IsAllowed.Should().BeFalse();
        report.Detail.Should().Contain("current");
    }

    [Fact]
    public void Acceptance_3c_SetupIsNotBlockedAtBootstrap()
    {
        using var harness = new SetupHarness();
        File.Exists(harness.MachineStateFile).Should().BeFalse();

        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
    }
}
