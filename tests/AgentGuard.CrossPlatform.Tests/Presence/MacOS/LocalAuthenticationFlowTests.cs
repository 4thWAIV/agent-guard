// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.CrossPlatform.MacOS;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The macOS ORCHESTRATOR-flow test (contract-coverage-refactor acceptance #5: "The orchestrators' flow ... are
/// unit-tested by a fake native-ops across all branches (probe-fail on each error code, probe-pass then interactive
/// success/fail, cancellation ...)"). It drives the REAL <see cref="LocalAuthentication"/> — the macOS presence flow
/// port, now a pure DI orchestrator over the <see cref="IObjCRuntime"/> native-ops seam — through a stateful
/// <see cref="FakeObjCRuntime"/>, so every decision, mapping, and cancel-invalidate path the coverage refactor pulled
/// UP into the orchestrator is exercised without a dialog or a framework binding. Compiled only on the macOS CI leg (it
/// reaches the internal macOS orchestrator through the IVT grant).
/// </summary>
public sealed class LocalAuthenticationFlowTests
{
    // The reviewed prompt the orchestrator must pass down as the localizedReason; owned once by the shared spec.
    private const string Prompt = PresenceCheckPortSpec.Prompt;

    [Theory]
    [MemberData(nameof(MacOsErrorCodeOutcomes.Cases), MemberType = typeof(MacOsErrorCodeOutcomes))]
    public async Task EvaluateAsync_WhenTheProbeFails_MapsTheProbeErrorAndNeverPrompts(long code, int expectedLaResult)
    {
        var objc = FakeObjCRuntime.ProbeFails(code);

        LaResult result = await Evaluate(objc);

        result.Should().Be((LaResult)expectedLaResult, "a failed probe maps its LAError code straight to the outcome");
        objc.EvaluateCalls.Should().Be(0, "a failed probe never shows the interactive dialog");
        objc.ContextsCreated.Should().Be(1, "a fresh LAContext is allocated for the probe");
        objc.LastProbePolicy.Should().Be(MacOsErrorCodeOutcomes.DeviceOwnerAuthentication);
        objc.ReleasedHandles.Should().Contain(FakeObjCRuntime.ContextHandle, "the fresh context is released even when the probe fails");
    }

    [Fact]
    public async Task EvaluateAsync_WhenTheProbePassesAndTheHumanSucceeds_ReturnsSuccess()
    {
        var objc = FakeObjCRuntime.InteractiveReply(success: 1, errorCode: 0);

        LaResult result = await Evaluate(objc);

        result.Should().Be(LaResult.Success);
        objc.EvaluateCalls.Should().Be(1, "the interactive evaluation is posted once after a passing probe");
        objc.LastReason.Should().Be(Prompt, "the reviewed prompt is passed down as the localizedReason");
        objc.ReleasedHandles.Should().Contain(FakeObjCRuntime.ContextHandle, "the fresh context is released after the reply");
        objc.ReleasedHandles.Should().Contain(FakeObjCRuntime.ReasonHandle, "the reason string is released after the reply");
        objc.ReplyBlocksFreed.Should().Be(1, "the reply block is freed after the reply");
    }

    [Theory]
    [MemberData(nameof(MacOsErrorCodeOutcomes.Cases), MemberType = typeof(MacOsErrorCodeOutcomes))]
    public async Task EvaluateAsync_WhenTheProbePassesAndTheReplyFails_MapsTheReplyErrorCode(
        long code, int expectedLaResult)
    {
        var objc = FakeObjCRuntime.InteractiveReply(success: 0, errorCode: code);

        LaResult result = await Evaluate(objc);

        result.Should().Be((LaResult)expectedLaResult, "a failed reply maps its LAError code through the same mapper as the probe");
        objc.EvaluateCalls.Should().Be(1, "the interactive evaluation is posted once");
        objc.ReleasedHandles.Should().Contain(FakeObjCRuntime.ContextHandle, "the fresh context is released after a failed reply");
        objc.ReleasedHandles.Should().Contain(FakeObjCRuntime.ReasonHandle, "the reason string is released after a failed reply");
    }

    [Fact]
    public async Task EvaluateAsync_WhenCancelledDuringTheEvaluation_InvalidatesAndReturnsNonApproved()
    {
        var objc = FakeObjCRuntime.CancelledOnInvalidate(MacOsErrorCodeOutcomes.LAErrorUserCancel);
        using var cts = new CancellationTokenSource();

        // The evaluation posts and then waits; cancelling fires the orchestrator's registered invalidate, which the fake
        // answers with a cancel reply — exactly as LA does when [context invalidate] dismisses the dialog.
        Task<LaResult> pending = Evaluate(objc, cts.Token);
        await cts.CancelAsync();
        LaResult result = await pending;

        result.Should().Be(LaResult.UserCancel, "invalidating a pending evaluation yields a cancel that is non-approved");
        result.Should().NotBe(LaResult.Success, "a cancelled evaluation must never read as approved");
        objc.InvalidateCalls.Should().Be(1, "cancellation invalidates the context exactly once");
    }

    // Construct the REAL macOS orchestrator over the injected fake native-ops and post the reviewed prompt — the one
    // construct-and-act pattern shared by every flow test (mirrors the Windows sibling's Verify helper).
    private static Task<LaResult> Evaluate(FakeObjCRuntime objc, CancellationToken ct = default) =>
        new LocalAuthentication(objc).EvaluateAsync(Prompt, ct);
}
