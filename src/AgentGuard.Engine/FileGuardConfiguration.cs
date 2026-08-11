// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.Engine;

/// <summary>
/// Everything the File Guard needs to run: its name, its collaborators, the ruleset fingerprint it stamps into
/// each snapshot, the literal core-system path tokens its Bash reference-check denies on, and its size limits.
/// </summary>
/// <param name="Name">The guard's stable name, used to key its Context records.</param>
/// <param name="Services">The guard's collaborators.</param>
/// <param name="RulesetFingerprint">The fingerprint of the assembled protected set.</param>
/// <param name="CoreSystemPathTokens">The literal path tokens a Bash command is denied for referencing.</param>
/// <param name="Limits">The snapshot size ceilings.</param>
internal sealed record FileGuardConfiguration(
    string Name,
    FileGuardServices Services,
    string RulesetFingerprint,
    IReadOnlyList<string> CoreSystemPathTokens,
    FileGuardLimits Limits);
