// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AgentGuard.Engine.Abstractions;
using AgentGuard.Engine.Abstractions.Contracts;

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

    private ClaudeCodeHostAdapter()
    {
    }

    /// <inheritdoc />
    public string Host => "claude-code";

    /// <summary>
    /// Gets the ordered edit-tool names, the single source the file-edit hook matcher is joined from so the wired
    /// matcher cannot drift from the tools this adapter normalizes.
    /// </summary>
    internal static IReadOnlyList<string> EditToolNames => EditToolNamesOrdered;

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
