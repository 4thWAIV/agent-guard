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

public sealed class FailClosedHardeningTests
{
    private const string InjectedFailure = "injected non-IO failure";

    [Fact]
    public async Task NulByteInFilePath_IsDenied_NotCrashed()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-nul", "a\0b"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task NonIoExceptionAtPre_IsDenied()
    {
        // A non-IO exception raised during the Pre guard run (here: the capture scan) must map to a deny at the
        // fail-closed boundary, never escape the pipeline. The failure is injected through the public pipeline's
        // per-path seam rather than a hand-rolled guard.
        using var fixture = new FixtureProject();
        SystemServicesBuilder builder = TestSupport.FakeServices();
        ISystemServices services = builder.Build();
        services.FileSystem.GetDirectoryWriter().CreateDirectory(fixture.Root);
        IPipeline pipeline = GuardEngine.CreatePipeline(new GuardEngineOptions(fixture.Root, services));
        builder.OnFileSystem().Handle(fixture.Root, ThrowOn(FileSystemOperation.EnumerateChildren));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-throw-pre", "echo hi"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        verdict.Message.Should().Contain(InjectedFailure);
    }

    [Fact]
    public async Task NonIoExceptionAtPost_IsDenied()
    {
        // A clean Pre captures a snapshot; a non-IO exception during the Post re-scan must then map to a deny, never
        // escape. The failure is injected only after capture succeeds.
        using var fixture = new FixtureProject();
        SystemServicesBuilder builder = TestSupport.FakeServices();
        ISystemServices services = builder.Build();
        services.FileSystem.GetDirectoryWriter().CreateDirectory(fixture.Root);
        IPipeline pipeline = GuardEngine.CreatePipeline(new GuardEngineOptions(fixture.Root, services));
        ToolCall call = TestSupport.Bash("call-throw-post", "echo hi");

        Verdict pre = await pipeline.RunAsync(
            HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None);
        pre.Kind.Should().Be(VerdictKind.Allow);

        builder.OnFileSystem().Handle(fixture.Root, ThrowOn(FileSystemOperation.EnumerateChildren));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        verdict.Message.Should().Contain(InjectedFailure);
    }

    [Fact]
    public async Task CanonicalizationFailure_IsDenied()
    {
        // A symlink cycle makes canonicalization exceed its link-depth ceiling; the pipeline must deny rather than
        // let the resulting exception escape. The cycle is created through the owned platform file system.
        using var fixture = new FixtureProject();
        ISystemServices services = TestSupport.FakeServices().Build();
        string loopA = Path.Combine(fixture.Root, "loopA");
        string loopB = Path.Combine(fixture.Root, "loopB");
        services.Platform.FileSystem.MakeLinkTarget(loopA, loopB);
        services.Platform.FileSystem.MakeLinkTarget(loopB, loopA);
        IPipeline pipeline = GuardEngine.CreatePipeline(new GuardEngineOptions(fixture.Root, services));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-loop", loopA),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task SweepFailureAtPre_IsDenied_NotEscaped()
    {
        // The best-effort orphan sweep runs inside the Pre fail-closed boundary: a non-IO sweep failure must become a
        // deny, never escape. The failure is injected on the store base's directory enumeration.
        using var fixture = new FixtureProject();
        SystemServicesBuilder builder = TestSupport.FakeServices();
        ISystemServices services = builder.Build();
        string storeBase = GuardEngine.SnapshotStoreDirectory(services, fixture.Root);
        services.FileSystem.GetDirectoryWriter().CreateDirectory(storeBase);
        IPipeline pipeline = GuardEngine.CreatePipeline(new GuardEngineOptions(fixture.Root, services));
        builder.OnFileSystem().Handle(storeBase, ThrowOn(FileSystemOperation.EnumerateDirectories));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-sweep-throw", "echo hi"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        verdict.Message.Should().Contain(InjectedFailure);
    }

    private static PathHandler ThrowOn(FileSystemOperation target) =>
        (operation, passThrough) => operation == target
            ? throw new InvalidOperationException(InjectedFailure)
            : passThrough();
}
