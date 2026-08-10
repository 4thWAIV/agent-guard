// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;
using AgentGuard.Setup;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AgentGuard.Tests;

public sealed class AcceptancePostRevertTests
{
    private static readonly string[] ConfigProtectedPaths = { "config-protected.txt" };

    [Fact]
    public async Task Acceptance_c_UnauthorizedEditToDirectoryBuildProps_IsRevertedAtPost()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));
        string original = fixture.ReadText("Directory.Build.props");

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Edit("call-c", fixture.PathOf("Directory.Build.props")),
            () => fixture.WriteFile("Directory.Build.props", "<Project><!-- tampered --></Project>"));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.ReadText("Directory.Build.props").Should().Be(original);
    }

    [Fact]
    public async Task Acceptance_d_BashDriftedDirectoryBuildProps_IsRevertedAtPost()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));
        string original = fixture.ReadText("Directory.Build.props");

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-d", "echo unrelated command that never names the target"),
            () => fixture.WriteFile("Directory.Build.props", "<Project><!-- drifted by a shell command --></Project>"));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.ReadText("Directory.Build.props").Should().Be(original);
    }

    [Fact]
    public async Task Acceptance_e_ProtectedFileCreatedByCall_IsDeletedAtPost()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-e", "echo creating a new protected file"),
            () => fixture.WriteFile("stylecop.json", "{ \"settings\": {} }"));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.Exists("stylecop.json").Should().BeFalse();
    }

    [Fact]
    public async Task Acceptance_ConfigProtectedPath_DriftIsRevertedAtPost()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(
            CoreSystemPaths.ProjectConfigRelative,
            SetupJson.Serialize(new ProjectConfig { ProtectedPaths = ConfigProtectedPaths }));
        fixture.WriteFile("config-protected.txt", "the original committed content\n");
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        await AssertDriftRevertedAsync(
            fixture,
            pipeline,
            TestSupport.Edit("call-config", fixture.PathOf("config-protected.txt")),
            "config-protected.txt",
            "tampered by an unauthorized edit\n");
    }

    private static async Task AssertDriftRevertedAsync(
        FixtureProject fixture, IPipeline pipeline, ToolCall call, string relativePath, string tampered)
    {
        string original = fixture.ReadText(relativePath);
        Verdict verdict = await RunPrePostAsync(fixture, pipeline, call, () => fixture.WriteFile(relativePath, tampered))
            .ConfigureAwait(false);
        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.ReadText(relativePath).Should().Be(original);
    }

    private static async Task<Verdict> RunPrePostAsync(
        FixtureProject fixture, IPipeline pipeline, ToolCall call, Action mutate)
    {
        await pipeline.RunAsync(
                HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None)
            .ConfigureAwait(false);
        mutate();
        return await pipeline.RunAsync(
                HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None)
            .ConfigureAwait(false);
    }
}
