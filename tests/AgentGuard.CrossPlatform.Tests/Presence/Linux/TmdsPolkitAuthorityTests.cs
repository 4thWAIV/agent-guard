// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Linux;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The polkit wire-assembly test (contract acceptance #4's wire clause, resolved by the <c>polkit-wire-test-seam</c>
/// option-2 decision). <see cref="TmdsPolkitAuthority.BuildCheckAuthorizationRequest"/> is the single owner of the whole
/// <c>org.freedesktop.PolicyKit1.Authority.CheckAuthorization</c> call, split off from the bus send so the assembled
/// request is inspectable without opening a system bus. This drives that pure builder with a known subject / action id /
/// message and asserts the WHOLE <see cref="PolkitCheckAuthorizationRequest"/>: the flags carry
/// <c>AllowUserInteraction=1</c>; <c>start-time</c> is pinned to the 64-bit D-Bus variant <c>"t"</c> (never the 32-bit
/// <c>"u"</c>, the truncation guard); the subject kind is <c>unix-process</c>; the action id and the reviewed
/// <c>polkit.message</c> are threaded through; the per-call cancellation id is written into the request (polkit's cancel
/// mechanism is now used, so an in-flight check can be cancelled); and the subject triple is carried unchanged. Compiled
/// only on the Linux CI leg.
/// </summary>
public sealed class TmdsPolkitAuthorityTests
{
    private const string Message = "please confirm the install";

    // A representative non-empty cancellation id the flow port would mint per call; the builder threads it into the
    // request so a matching CancelCheckAuthorization can cancel the in-flight check.
    private const string CancellationId = "cid-0123456789abcdef";

    [Fact]
    public void BuildCheckAuthorizationRequest_AssemblesTheWholeCall_WithAllowUserInteractionAnd64BitStartTime()
    {
        var subject = PolkitSubjectFixtures.Representative;

        PolkitCheckAuthorizationRequest request =
            TmdsPolkitAuthority.BuildCheckAuthorizationRequest(subject, PolkitAction.Id, Message, CancellationId);

        // (1) AllowUserInteraction is always set so the session's own authentication agent shows the dialog.
        request.Flags.Should().Be(1u, "AllowUserInteraction must always be set (fresh-interaction-no-cached-yes)");

        // (2) start-time is a 64-bit D-Bus variant ("t"), never 32-bit ("u"): a 32-bit start-time truncates past ~497
        // days of uptime and silently mismatches the process. Be("t") also excludes "u".
        request.SubjectVariantTypes["start-time"].Should().Be(
            "t", "start-time must be the 64-bit D-Bus variant so it does not truncate and mismatch the process");

        // (3) the subject is named to polkit as a classic unix-process subject.
        request.SubjectKind.Should().Be("unix-process");

        // (4) the action id is the single owned const, threaded through unchanged.
        request.ActionId.Should().Be(PolkitAction.Id);

        // (5) the reviewed prompt reaches polkit as the polkit.message detail.
        request.Details.Should().ContainKey("polkit.message")
            .WhoseValue.Should().Be(Message, "the reviewed prompt maps to the polkit.message detail entry");

        // (6) the per-call cancellation id is threaded into the request so a matching CancelCheckAuthorization can cancel
        // the in-flight check (non-empty — polkit's own cancel mechanism is now used).
        request.CancellationId.Should().Be(CancellationId);

        // (7) the unix-process triple (pid / start-time / uid) is carried through unchanged (record value equality).
        request.Subject.Should().Be(subject);
    }
}
