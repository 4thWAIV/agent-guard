// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The single owner of the polkit action id — one action for all three verbs, differentiated by the per-call
/// <c>polkit.message</c> (polkit-action-id). This C# const is one of exactly two places the id lives; the other is the
/// <c>&lt;action id&gt;</c> in the shipped <c>.policy</c> file, and the policy-integrity test pins them equal. IMPLEMENT
/// confirms polkit accepts the digit-leading <c>4thwaiv</c> segment; if not, it is spelled
/// <c>com.fourthwaiv.agentguard.presence</c> here and in the policy.
/// </summary>
internal static class PolkitAction
{
    /// <summary>
    /// The polkit action id for the presence check.
    /// </summary>
    internal const string Id = "com.4thwaiv.agentguard.presence";
}
