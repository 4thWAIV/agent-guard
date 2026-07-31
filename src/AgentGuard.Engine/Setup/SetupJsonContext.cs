// Copyright (c) 4thWAIV. All rights reserved.

using System.Text.Json.Serialization;
using AgentGuard.Engine;

namespace AgentGuard.Setup;

/// <summary>
/// The source-generated serialization context for the setup records. Source generation keeps JSON handling free
/// of runtime reflection so the self-contained single-file binary carries no dynamic-code dependency.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(InstallState))]
[JsonSerializable(typeof(ProjectState))]
[JsonSerializable(typeof(ProjectConfig))]
internal sealed partial class SetupJsonContext : JsonSerializerContext
{
}
