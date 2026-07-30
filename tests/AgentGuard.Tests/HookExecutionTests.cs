// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Setup;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class HookExecutionTests
{
    [Fact]
    public async Task ComposedHook_HealthyInstall_BenignEdit_AllowsAndReportsWritable()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        using var fixture = new FixtureProject();
        string payload = TestPayloads.Edit(fixture.Root, fixture.PathOf("Notes.txt"));

        HookExecution execution = await GuardHost.ExecuteHookAsync(
            HookEvent.PreToolUse, harness.BinGuard, payload, GuardHost.ClaudeCodeHost, CancellationToken.None);

        execution.ExitCode.Should().Be(0);
        execution.BinaryWritable.Should().BeTrue();
    }

    [Fact]
    public async Task ComposedHook_TamperedInstall_DeniesBeforePipeline()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        await File.WriteAllTextAsync(harness.MachineStateFile, "{\"version\":\"0.1.0-alpha\",\"sha256\":\"deadbeef\"}");
        using var fixture = new FixtureProject();
        string benignPayload = TestPayloads.Edit(fixture.Root, fixture.PathOf("Notes.txt"));

        HookExecution execution = await GuardHost.ExecuteHookAsync(
            HookEvent.PreToolUse, harness.BinGuard, benignPayload, GuardHost.ClaudeCodeHost, CancellationToken.None);

        execution.ExitCode.Should().Be(2);
        execution.Message.Should().Contain("hash");
    }

    [Fact]
    public async Task ComposedHook_UnresolvableBinary_Denies()
    {
        HookExecution execution = await GuardHost.ExecuteHookAsync(
            HookEvent.PreToolUse, null, "{}", GuardHost.ClaudeCodeHost, CancellationToken.None);

        execution.ExitCode.Should().Be(2);
    }
}
