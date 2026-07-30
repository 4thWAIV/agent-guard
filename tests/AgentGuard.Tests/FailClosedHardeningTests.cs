// Copyright (c) 4thWAIV. All rights reserved.

using System;
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

public sealed class FailClosedHardeningTests
{
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
        using var fixture = new FixtureProject();
        IPipeline pipeline = BuildPipelineWith(ThrowingGuard.Create(), fixture.Root);

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-throw-pre", fixture.PathOf("Directory.Build.props")),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task NonIoExceptionAtPost_IsDenied()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = BuildPipelineWith(ThrowingGuard.Create(), fixture.Root);

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PostToolUse,
            TestSupport.Edit("call-throw-post", fixture.PathOf("Directory.Build.props")),
            TestSupport.Env(fixture.Root, HookEvent.PostToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task CanonicalizationFailure_IsDenied()
    {
        using var fixture = new FixtureProject();
        string loopA = fixture.PathOf("loopA");
        string loopB = fixture.PathOf("loopB");
        File.CreateSymbolicLink(loopA, loopB);
        File.CreateSymbolicLink(loopB, loopA);
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

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
        using var fixture = new FixtureProject();
        IGuardRegistry registry = GuardRegistry.Create(Array.Empty<IGuard>());
        IContextStore store = ThrowingSweepStore.Create();
        IPrivilegedWriter writer = PrivilegedWriter.Create();
        IPipeline pipeline = Pipeline.Create(registry, store, writer);

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-sweep-throw", fixture.PathOf("Directory.Build.props")),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    private static IPipeline BuildPipelineWith(IGuard guard, string root)
    {
        IGuardRegistry registry = GuardRegistry.Create(new[] { guard });
        IContextStore store = ContextStore.Create(root, new FakeTimeProvider());
        IPrivilegedWriter writer = PrivilegedWriter.Create();
        return Pipeline.Create(registry, store, writer);
    }
}
