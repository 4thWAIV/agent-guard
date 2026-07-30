// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AgentGuard.Tests;

public sealed class AcceptanceFailClosedTests
{
    [Fact]
    public async Task Acceptance_f_PreScanCannotEnumerate_DeniesAndWritesNoSnapshot()
    {
        if (System.OperatingSystem.IsWindows())
        {
            return;
        }

        using var fixture = new FixtureProject();
        string lockedDirectory = fixture.PathOf("locked");
        Directory.CreateDirectory(lockedDirectory);
        File.SetUnixFileMode(lockedDirectory, UnixFileMode.None);
        try
        {
            IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

            Verdict verdict = await pipeline.RunAsync(
                HookEvent.PreToolUse,
                TestSupport.Bash("call-f", "echo hi"),
                TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
                CancellationToken.None);

            verdict.Kind.Should().Be(VerdictKind.Deny);
            SnapshotRecordCount(fixture.Root).Should().Be(0);
        }
        finally
        {
            File.SetUnixFileMode(
                lockedDirectory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public async Task Acceptance_g_DriftWithMissingSnapshot_IsDenied()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));
        fixture.WriteFile("Directory.Build.props", "<Project><!-- drifted with no captured snapshot --></Project>");

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PostToolUse,
            TestSupport.Edit("call-g-never-captured", fixture.PathOf("Directory.Build.props")),
            TestSupport.Env(fixture.Root, HookEvent.PostToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task Acceptance_h_CaptureFailure_DeniesAndWritesNoSnapshot()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.OptionsWithCeiling(fixture.Root, new FakeTimeProvider(), perFileCeiling: 4));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-h", fixture.PathOf("Directory.Build.props")),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        verdict.Message.Should().Contain("Capture failed");
        SnapshotRecordCount(fixture.Root).Should().Be(0);
    }

    private static int SnapshotRecordCount(string root)
    {
        string directory = GuardEngine.SnapshotStoreDirectory(root);
        return Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.bin", SearchOption.AllDirectories).Length
            : 0;
    }
}
