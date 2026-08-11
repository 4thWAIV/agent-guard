// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;
using AgentGuard.Setup;

namespace AgentGuard.Engine;

/// <summary>
/// Bridges the Claude Code runtime to the Engine: it normalizes the PreToolUse / PostToolUse payload into a
/// host-agnostic call with a typed input — edit tools carry every target path, the shell tool carries its
/// command, and every other tool carries no input — and it renders the aggregate verdict into Claude Code's
/// exit-code protocol. It fails closed: an empty or malformed payload yields an unparsable result the Engine
/// denies on.
/// </summary>
internal sealed class ClaudeCodeHostAdapter : IHostAdapter
{
    /// <summary>
    /// The shell tool name; project wiring adds a separate matcher for it.
    /// </summary>
    internal const string ShellToolName = "Bash";

    private const string MonitorToolName = "Monitor";
    private const string PowerShellToolName = "PowerShell";
    private const string McpToolPattern = "mcp__.*";

    private const int BlockExitCode = 2;
    private const int AllowExitCode = 0;

    private static readonly string[] EditToolNamesOrdered =
    {
        "Edit",
        "Write",
        "MultiEdit",
        "NotebookEdit",
    };

    private static readonly HashSet<string> EditTools = new(EditToolNamesOrdered, StringComparer.Ordinal);

    private static readonly string[] HookMatcherTokensOrdered = BuildHookMatcherTokens();

    private ClaudeCodeHostAdapter()
    {
    }

    /// <inheritdoc />
    public string Host => GuardHost.ClaudeCodeHost;

    /// <summary>
    /// Gets the complete, ordered set of tool-name tokens the guard's Pre/Post hook matcher fires on — the parsed
    /// edit tools and shell tool, plus the hook-firing-only Monitor, PowerShell, and <c>mcp__.*</c> tokens. This is
    /// the single source the settings matcher is assembled from, so the wired matcher cannot drift from the tools
    /// the guard watches. Input is parsed only for the edit-family and shell tools (see <see cref="BuildInput"/>);
    /// the remaining tokens fire the hook but carry no parsed input.
    /// </summary>
    internal static IReadOnlyList<string> HookMatcherToolTokens => HookMatcherTokensOrdered;

    /// <inheritdoc />
    public HostReadResult Read(HookEvent hookEvent, string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return new HostReadUnparsable("The hook payload was empty.");
        }

        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new HostReadUnparsable("The hook payload was not a JSON object.");
            }

            string? toolName = ReadString(root, "tool_name");
            if (string.IsNullOrEmpty(toolName))
            {
                return new HostReadUnparsable("The hook payload had no tool_name.");
            }

            string? toolUseId = ReadString(root, "tool_use_id");
            if (string.IsNullOrEmpty(toolUseId))
            {
                return new HostReadUnparsable("The hook payload had no tool_use_id.");
            }

            string projectRoot = ReadString(root, "cwd") ?? Directory.GetCurrentDirectory();
            string? sessionId = ReadString(root, "session_id");
            ToolInput? input = BuildInput(toolName, root);

            var call = new ToolCall(new ToolCallId(toolUseId), toolName, input);
            var environment = new CallEnvironment(projectRoot, hookEvent, sessionId);
            return new HostReadParsed(new NormalizedCall(call, environment));
        }
        catch (JsonException exception)
        {
            return new HostReadUnparsable($"The hook payload was not well-formed JSON: {exception.Message}");
        }
    }

    /// <inheritdoc />
    public HostDecision Render(Verdict verdict, HookEvent hookEvent)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        return verdict.Kind switch
        {
            VerdictKind.Deny => new HostDecision(BlockExitCode, verdict.Message),
            VerdictKind.Warn => new HostDecision(AllowExitCode, verdict.Message),
            VerdictKind.Allow => new HostDecision(AllowExitCode, null),
            _ => new HostDecision(BlockExitCode, "Unknown verdict; failing closed."),
        };
    }

    /// <summary>
    /// Creates the Claude Code adapter.
    /// </summary>
    /// <returns>The adapter, as its interface.</returns>
    internal static IHostAdapter Create() => new ClaudeCodeHostAdapter();

    private static string[] BuildHookMatcherTokens()
    {
        var tokens = new List<string>(EditToolNamesOrdered)
        {
            ShellToolName,
            MonitorToolName,
            PowerShellToolName,
            McpToolPattern,
        };
        return tokens.ToArray();
    }

    private static ToolInput? BuildInput(string toolName, JsonElement root)
    {
        bool isEditTool = EditTools.Contains(toolName);
        bool isShellTool = string.Equals(toolName, ShellToolName, StringComparison.Ordinal);
        if (!isEditTool && !isShellTool)
        {
            return null;
        }

        JsonElement toolInput = root.TryGetProperty("tool_input", out JsonElement element)
            && element.ValueKind == JsonValueKind.Object
            ? element
            : default;

        if (isEditTool)
        {
            var paths = new List<string>();
            AddIfPresent(paths, ReadString(toolInput, "file_path"));
            AddIfPresent(paths, ReadString(toolInput, "notebook_path"));
            return new FileWriteInput(paths);
        }

        return new ShellCommandInput(ReadString(toolInput, "command") ?? string.Empty);
    }

    private static void AddIfPresent(List<string> paths, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            paths.Add(value);
        }
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
