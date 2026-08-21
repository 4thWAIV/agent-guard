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

        harness.Integrity.Check(harness.BinGuard).IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Acceptance_3c_HashMismatch_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.FileWriter.WriteAllText(harness.MachineStateFile, "{\"version\":\"0.1.0-alpha\",\"sha256\":\"deadbeef\"}");

        IntegrityReport report = harness.Integrity.Check(harness.BinGuard);

        report.IsAllowed.Should().BeFalse();
        report.Detail.Should().Contain("hash");
    }

    [Fact]
    public void Acceptance_3c_MissingRecord_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.FileWriter.DeleteFile(harness.MachineStateFile);

        harness.Integrity.Check(harness.BinGuard).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3c_UnreadableRecord_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.FileWriter.WriteAllText(harness.MachineStateFile, "{ this is not json");

        harness.Integrity.Check(harness.BinGuard).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3c_RunningBinaryMissing_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.DirectoryWriter.DeleteDirectory(Path.Combine(harness.AgentGuardRoot, "versions", "0.1.0-alpha"), recursive: true);

        harness.Integrity.Check(harness.BinGuard).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Acceptance_3c_UnresolvableCurrent_IsDenied()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();

        // `current` still points at the installed version's directory, but that version's binary is gone, so
        // `current` no longer resolves to an installed binary. Pass a resolvable binary under the root so the
        // check reaches this branch rather than the "running binary missing" branch above it.
        harness.FileWriter.DeleteFile(harness.VersionBinary("0.1.0-alpha"));
        string resolvable = Path.Combine(harness.AgentGuardRoot, "versions", "0.1.0-alpha", "keeper");
        harness.FileWriter.WriteAllText(resolvable, "x");

        IntegrityReport report = harness.Integrity.Check(resolvable);

        report.IsAllowed.Should().BeFalse();
        report.Detail.Should().Contain("current");
    }

    [Fact]
    public void Acceptance_3c_SetupIsNotBlockedAtBootstrap()
    {
        using var harness = new SetupHarness();
        harness.Files.Exists(harness.MachineStateFile).Should().BeFalse();

        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
    }
}
