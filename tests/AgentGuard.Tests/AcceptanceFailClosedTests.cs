// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AgentGuard.Tests;

public sealed class AcceptanceFailClosedTests
{
    [Fact]
    public async Task Acceptance_f_PreScanCannotEnumerate_DeniesAndWritesNoSnapshot()
    {
        // The scanner reaches the filesystem through IDirectoryEnumerator; a directory it cannot enumerate must
        // surface as a denial with no snapshot, never a silent partial walk. MarkInaccessible on the project root
        // reproduces the un-enumerable directory OS-agnostically (no chmod, no OS branch), through the public pipeline.
        using var fixture = new FixtureProject();
        SystemServicesBuilder builder = TestSupport.FakeServices();
        ISystemServices services = builder.Build();
        builder.OnFileSystem().MarkInaccessible(fixture.Root);
        IPipeline pipeline = GuardEngine.CreatePipeline(new GuardEngineOptions(fixture.Root, services));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-f", "echo hi"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        SnapshotRecordCount(services, fixture.Root).Should().Be(0);
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
        ISystemServices services = SystemServicesBuilder.Real().With((TimeProvider)new FakeTimeProvider()).Build();
        IPipeline pipeline = GuardEngine.CreatePipeline(
            new GuardEngineOptions(fixture.Root, services, PerFileSnapshotByteCeiling: 4));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-h", fixture.PathOf("Directory.Build.props")),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        verdict.Message.Should().Contain("Capture failed");
        SnapshotRecordCount(services, fixture.Root).Should().Be(0);
    }

    private static int SnapshotRecordCount(ISystemServices services, string root)
    {
        string directory = GuardEngine.SnapshotStoreDirectory(services, root);
        IDirectoryEnumerator directories = services.FileSystem.GetDirectoryReader();
        return directories.DirectoryExists(directory)
            ? directories.EnumerateFiles(directory, "*.bin", new EnumerationOptions { RecurseSubdirectories = true }).Count
            : 0;
    }
}
