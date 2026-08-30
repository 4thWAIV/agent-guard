// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions.Contracts;

/// <summary>
/// The input to a presence check: the resolved, reviewed prompt text the OS dialog shows the human. It carries the
/// prompt only — the gate already holds the verb, keys the owned dialog-text resource with it, and stamps the
/// resulting <c>ApprovalDecision.Action</c>, so the request never re-carries the verb.
/// </summary>
/// <param name="PromptText">The reviewed, action-specific prompt string the OS dialog displays.</param>
public sealed record PresenceRequest(string PromptText);
