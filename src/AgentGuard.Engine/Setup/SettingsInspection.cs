// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Setup;

/// <summary>
/// The result of inspecting <c>.claude/settings.json</c> for the guard's hook entries.
/// </summary>
/// <param name="Health">The detected health.</param>
/// <param name="Detail">A human-readable detail describing what was found.</param>
internal sealed record SettingsInspection(SettingsHealth Health, string Detail);
