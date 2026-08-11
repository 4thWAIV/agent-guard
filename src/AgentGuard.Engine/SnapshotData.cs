// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.Engine;

/// <summary>
/// The full content of a per-call pre-image snapshot: the ruleset fingerprint the call was captured under, and
/// one entry per protected path present at capture. A Post whose fingerprint does not match this one is denied
/// rather than diffed across a changed membership.
/// </summary>
/// <param name="RulesetFingerprint">The fingerprint of the assembled protected set at capture time.</param>
/// <param name="Entries">The captured per-path entries.</param>
internal sealed record SnapshotData(string RulesetFingerprint, IReadOnlyList<SnapshotEntry> Entries);
