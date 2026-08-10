// Copyright (c) 4thWAIV. All rights reserved.

using System.Text.Json.Nodes;

namespace AgentGuard.Tests;

/// <summary>
/// Builds host payloads for driving the hook surface in tests.
/// </summary>
internal static class TestPayloads
{
    /// <summary>Builds an Edit-tool PreToolUse/PostToolUse payload.</summary>
    /// <param name="cwd">The project root.</param>
    /// <param name="filePath">The edited file path.</param>
    /// <returns>The JSON payload.</returns>
    internal static string Edit(string cwd, string filePath) => new JsonObject
    {
        ["tool_name"] = "Edit",
        ["tool_use_id"] = "t1",
        ["cwd"] = cwd,
        ["tool_input"] = new JsonObject { ["file_path"] = filePath },
    }.ToJsonString();
}
