// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Generic;
using AgentGuard.Abstractions;
using AgentGuard.CrossPlatform.Linux;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Linux pure-mapper table test (acceptance #4 and "What to do" #7): <c>PolkitDecision.Map</c> translates every
/// polkit reply shape to the shared <see cref="ApprovalReason"/>, including the fresh-interaction backstop —
/// a reply that is authorized but carries a <c>polkit.temporary_authorization_id</c> is a retained (non-fresh) grant
/// and maps to <see cref="ApprovalReason.Denied"/>, never <see cref="ApprovalReason.Approved"/>. Compiled only on the
/// Linux CI leg (it reaches the internal Linux mapper and reply record).
/// </summary>
public sealed class PolkitDecisionTests
{
    private const string TempAuthIdKey = "polkit.temporary_authorization_id";
    private const string DismissedKey = "polkit.dismissed";

    [Fact]
    public void Map_AuthorizedWithoutTempAuthId_IsApproved()
    {
        var reply = new PolkitResult(IsAuthorized: true, IsChallenge: false, PolkitTestReplies.NoDetails());

        PolkitDecision.Map(reply).Should().Be(ApprovalReason.Approved);
    }

    [Fact]
    public void Map_AuthorizedWithTempAuthId_IsDenied_NotApproved()
    {
        var reply = new PolkitResult(
            IsAuthorized: true,
            IsChallenge: false,
            new Dictionary<string, string>(StringComparer.Ordinal) { [TempAuthIdKey] = "tmp-auth-42" });

        PolkitDecision.Map(reply).Should().Be(ApprovalReason.Denied);
    }

    [Fact]
    public void Map_NotAuthorizedWithChallenge_IsUnavailable()
    {
        var reply = new PolkitResult(IsAuthorized: false, IsChallenge: true, PolkitTestReplies.NoDetails());

        PolkitDecision.Map(reply).Should().Be(ApprovalReason.Unavailable);
    }

    [Fact]
    public void Map_Dismissed_IsCancelled()
    {
        var reply = new PolkitResult(
            IsAuthorized: false,
            IsChallenge: false,
            new Dictionary<string, string>(StringComparer.Ordinal) { [DismissedKey] = "true" });

        PolkitDecision.Map(reply).Should().Be(ApprovalReason.Cancelled);
    }

    [Fact]
    public void Map_NotAuthorizedWithoutChallenge_IsDenied()
    {
        var reply = new PolkitResult(IsAuthorized: false, IsChallenge: false, PolkitTestReplies.NoDetails());

        PolkitDecision.Map(reply).Should().Be(ApprovalReason.Denied);
    }
}
