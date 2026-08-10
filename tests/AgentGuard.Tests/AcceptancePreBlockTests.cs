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

public sealed class AcceptancePreBlockTests
{
    [Fact]
    public async Task Acceptance_a_EditToClaudeSettings_IsDeniedAtPre()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(".claude/settings.json", "{ \"hooks\": {} }");
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-a", fixture.PathOf(".claude/settings.json")),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        verdict.Message.Should().Contain("System");
    }

    [Fact]
    public async Task Acceptance_b_BashReferencingSnapshotStore_IsDeniedAtPre()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-b1", "cat .protected-snapshots/anything > /dev/null"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task Acceptance_b_BashReferencingGrantStore_IsDeniedAtPre()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-b2", "rm -rf .agentguard/grants"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task Acceptance_j_WriteThroughSymlinkToProtectedFile_IsDeniedAtPre()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(".claude/settings.json", "{ \"hooks\": {} }");
        string linkPath = fixture.PathOf("settings-link.json");
        File.CreateSymbolicLink(linkPath, fixture.PathOf(".claude/settings.json"));
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Edit("call-j", linkPath),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }
}
