// Copyright (c) 4thWAIV. All rights reserved.

using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentGuard.Setup;

/// <summary>
/// The project condition that <c>.agentguard/config.json</c> exists and parses. This build does not judge whether
/// a provider is "enabled" — nothing consumes the file yet — so the check is structural only.
/// </summary>
internal sealed class ConfigJsonCondition : ISetupCondition
{
    /// <inheritdoc />
    public string Name => "project config.json";

    /// <inheritdoc />
    public SetupScope Scope => SetupScope.Project;

    /// <inheritdoc />
    public ConditionState Detect(SetupContext context)
    {
        string path = ProjectPaths.ConfigFile(context);
        if (!File.Exists(path))
        {
            return ConditionState.Broken(".agentguard/config.json is missing");
        }

        if (!SafeRead.TryReadText(path, out string content, out string error))
        {
            return ConditionState.CannotVerify($".agentguard/config.json is unreadable: {error}");
        }

        try
        {
            return JsonNode.Parse(content) is JsonObject
                ? ConditionState.Ok()
                : ConditionState.Broken(".agentguard/config.json is not a JSON object");
        }
        catch (JsonException exception)
        {
            return ConditionState.Broken($".agentguard/config.json does not parse: {exception.Message}");
        }
    }

    /// <inheritdoc />
    public RepairOutcome Repair(SetupContext context) => CreationHelper.EnsureConfig(context);
}
