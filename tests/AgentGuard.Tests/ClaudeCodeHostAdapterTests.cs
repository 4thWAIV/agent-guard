// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.Tests;

public sealed class ClaudeCodeHostAdapterTests
{
    [Fact]
    public void Read_EmptyPayload_IsUnparsable()
    {
        IHostAdapter adapter = Adapter();

        adapter.Read(HookEvent.PreToolUse, string.Empty).Should().BeOfType<HostReadUnparsable>();
    }

    [Fact]
    public void Read_MalformedJson_IsUnparsable()
    {
        IHostAdapter adapter = Adapter();

        adapter.Read(HookEvent.PreToolUse, "{ not valid json").Should().BeOfType<HostReadUnparsable>();
    }

    [Fact]
    public void Read_EditPayload_CarriesEveryTargetPath()
    {
        IHostAdapter adapter = Adapter();
        const string payload = """
            { "tool_name": "Edit", "tool_use_id": "t1", "cwd": "/repo", "tool_input": { "file_path": "/repo/a.cs" } }
            """;

        HostReadResult result = adapter.Read(HookEvent.PreToolUse, payload);

        HostReadParsed parsed = result.Should().BeOfType<HostReadParsed>().Subject;
        FileWriteInput input = parsed.Call.Call.Input.Should().BeOfType<FileWriteInput>().Subject;
        input.Paths.Should().ContainSingle().Which.Should().Be("/repo/a.cs");
        parsed.Call.Environment.ProjectRoot.Should().Be("/repo");
    }

    [Fact]
    public void Read_BashPayload_CarriesCommand()
    {
        IHostAdapter adapter = Adapter();
        const string payload = """
            { "tool_name": "Bash", "tool_use_id": "t2", "cwd": "/repo", "tool_input": { "command": "ls -la" } }
            """;

        HostReadResult result = adapter.Read(HookEvent.PreToolUse, payload);

        HostReadParsed parsed = result.Should().BeOfType<HostReadParsed>().Subject;
        parsed.Call.Call.Input.Should().BeOfType<ShellCommandInput>().Which.Command.Should().Be("ls -la");
    }

    [Fact]
    public void Read_UnmodeledTool_HasNullInput()
    {
        IHostAdapter adapter = Adapter();
        const string payload = """
            { "tool_name": "Read", "tool_use_id": "t3", "cwd": "/repo", "tool_input": { "file_path": "/repo/a.cs" } }
            """;

        HostReadResult result = adapter.Read(HookEvent.PreToolUse, payload);

        result.Should().BeOfType<HostReadParsed>().Subject.Call.Call.Input.Should().BeNull();
    }

    [Fact]
    public void Render_MapsVerdictsToExitCodes()
    {
        IHostAdapter adapter = Adapter();

        adapter.Render(Verdict.Deny("blocked"), HookEvent.PreToolUse).ExitCode.Should().Be(2);
        adapter.Render(Verdict.Allow(), HookEvent.PostToolUse).ExitCode.Should().Be(0);
    }

    private static IHostAdapter Adapter() =>
        GuardEngine.CreateClaudeCodeAdapter(SystemServicesBuilder.Real().Build());
}
