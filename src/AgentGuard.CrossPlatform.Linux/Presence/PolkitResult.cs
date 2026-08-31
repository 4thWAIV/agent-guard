// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Generic;

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The plain reply from polkit's <c>CheckAuthorization</c>, returned by the port and interpreted by
/// <see cref="PolkitDecision.Map"/>. It carries exactly what the decision needs: whether the action was authorized,
/// whether the reply is a challenge (interaction was offered but not completed), and the reply details — in which a
/// <c>polkit.temporary_authorization_id</c> marks a retained (non-fresh) grant the decision rejects.
/// </summary>
/// <param name="IsAuthorized">Whether polkit authorized the action.</param>
/// <param name="IsChallenge">Whether the reply is a challenge (not-authorized, interaction available).</param>
/// <param name="Details">The reply detail keys/values, including any <c>polkit.temporary_authorization_id</c>.</param>
internal sealed record PolkitResult(bool IsAuthorized, bool IsChallenge, IReadOnlyDictionary<string, string> Details);
