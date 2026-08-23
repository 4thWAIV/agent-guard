// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Setup;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class HookExecutionTests
{
    [Fact]
    public async Task ComposedHook_HealthyInstall_BenignEdit_Allows()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        string payload = TestPayloads.Edit(harness.Project, Path.Combine(harness.Project, "Notes.txt"));

        HookExecution execution = await GuardHost.ExecuteHookAsync(
            HookEvent.PreToolUse, harness.BinGuard, payload, GuardHost.ClaudeCodeHost, harness.Services, CancellationToken.None);

        execution.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task ComposedHook_TamperedInstall_DeniesBeforePipeline()
    {
        using var harness = new SetupHarness();
        harness.Install("0.1.0-alpha").Success.Should().BeTrue();
        harness.FileWriter.WriteAllText(harness.MachineStateFile, "{\"version\":\"0.1.0-alpha\",\"sha256\":\"deadbeef\"}");
        string benignPayload = TestPayloads.Edit(harness.Project, Path.Combine(harness.Project, "Notes.txt"));

        HookExecution execution = await GuardHost.ExecuteHookAsync(
            HookEvent.PreToolUse, harness.BinGuard, benignPayload, GuardHost.ClaudeCodeHost, harness.Services, CancellationToken.None);

        execution.ExitCode.Should().Be(2);
        execution.Message.Should().Contain("hash");
    }

    [Fact]
    public async Task ComposedHook_UnresolvableBinary_Denies()
    {
        HookExecution execution = await GuardHost.ExecuteHookAsync(
            HookEvent.PreToolUse, null, "{}", GuardHost.ClaudeCodeHost, SystemServicesBuilder.Fake().Build(), CancellationToken.None);

        execution.ExitCode.Should().Be(2);
    }
}
