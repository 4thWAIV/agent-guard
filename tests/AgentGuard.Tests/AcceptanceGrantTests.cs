// Copyright (c) 4thWAIV. All rights reserved.

using System;
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

public sealed class AcceptanceGrantTests
{
    [Fact]
    public async Task Acceptance_i_ChangeCoveredByValidGrant_IsAllowedAndNotReverted()
    {
        using var fixture = new FixtureProject();
        var authority = new EphemeralGrantAuthority();
        var time = new FakeTimeProvider();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, time, authority.PublicKey));
        ToolCall call = TestSupport.Edit("call-i", fixture.PathOf("Directory.Build.props"));

        await pipeline.RunAsync(HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None);

        var payload = new GrantTokenPayload(
            "grant-i",
            GrantScope.All,
            new[] { fixture.PathOf("Directory.Build.props") },
            time.GetUtcNow().AddDays(1));
        fixture.WriteFile(".agentguard/grants/grant-i.token", GrantTokenCodec.Serialize(authority.Sign(payload)));

        const string granted = "<Project><!-- authorized change --></Project>";
        fixture.WriteFile("Directory.Build.props", granted);

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None);

        verdict.Kind.Should().NotBe(VerdictKind.Deny);
        fixture.ReadText("Directory.Build.props").Should().Be(granted);
    }

    [Fact]
    public async Task GrantWithInvalidSignature_DoesNotAuthorize_IsReverted()
    {
        using var fixture = new FixtureProject();
        var authority = new EphemeralGrantAuthority();
        var time = new FakeTimeProvider();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, time, authority.PublicKey));
        string original = fixture.ReadText("Directory.Build.props");
        ToolCall call = TestSupport.Edit("call-badsig", fixture.PathOf("Directory.Build.props"));

        await pipeline.RunAsync(HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None);

        var payload = new GrantTokenPayload(
            "grant-badsig",
            GrantScope.All,
            new[] { fixture.PathOf("Directory.Build.props") },
            time.GetUtcNow().AddDays(1));
        var tampered = new GrantToken(payload, Convert.ToBase64String(new byte[64]));
        fixture.WriteFile(".agentguard/grants/badsig.token", GrantTokenCodec.Serialize(tampered));
        fixture.WriteFile("Directory.Build.props", "<Project><!-- unauthorized: signature does not verify --></Project>");

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.ReadText("Directory.Build.props").Should().Be(original);
    }

    [Fact]
    public async Task MalformedGrantTokens_AreDropped_AndDriftStillReverted()
    {
        using var fixture = new FixtureProject();
        var authority = new EphemeralGrantAuthority();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider(), authority.PublicKey));
        string original = fixture.ReadText("Directory.Build.props");
        ToolCall call = TestSupport.Edit("call-malformed", fixture.PathOf("Directory.Build.props"));

        await pipeline.RunAsync(HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None);

        fixture.WriteFile(".agentguard/grants/empty.token", "{}");
        fixture.WriteFile(
            ".agentguard/grants/nullsig.token",
            "{ \"Payload\": { \"Id\": \"x\", \"Scope\": \"All\", \"CoveredPaths\": [], \"ExpiresAt\": \"2999-01-01T00:00:00+00:00\" }, \"Signature\": null }");
        fixture.WriteFile("Directory.Build.props", "<Project><!-- tampered --></Project>");

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.ReadText("Directory.Build.props").Should().Be(original);
    }
}
