// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using AgentGuard.CrossPlatform.Windows;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Windows ORCHESTRATOR-flow test (contract-coverage-refactor acceptance #5: "The orchestrators' flow ... are
/// unit-tested by a fake native-ops across all branches (... Hello 7-way + credential fallback)"). It drives the REAL
/// <see cref="WindowsUserPresence"/> — the Windows presence flow port, now a pure DI orchestrator over the two native-ops
/// seams (<see cref="IWindowsHelloNativeOps"/> and <see cref="ICredentialPromptNativeOps"/>) — through stateful fakes, so
/// the Hello-then-credential-fallback decision the coverage refactor pulled UP into the orchestrator is exercised on
/// every branch without a Hello device or an interactive surface. Compiled only on the Windows CI leg (it reaches the
/// internal Windows orchestrator through the IVT grant); it needs no real Windows API because every native call is the
/// injected fake — the point of the deep seam.
/// </summary>
public sealed class WindowsUserPresenceFlowTests
{
    // The reviewed prompt the orchestrator must pass down to Hello / the credential prompt; owned once by the shared spec.
    private const string Prompt = PresenceCheckPortSpec.Prompt;

    // Hello answered — no fallback.
    [Fact]
    public async Task VerifyAsync_WhenHelloVerifiesTheHuman_ReturnsVerified()
    {
        var result = await Verify(FakeWindowsHelloNativeOps.Answers(AsyncStatusCode.Completed, HelloResultCode.Verified));

        result.Should().Be(WindowsPresenceResult.Verified);
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloIsCancelled_ReturnsCancelled()
    {
        var result = await Verify(FakeWindowsHelloNativeOps.Answers(AsyncStatusCode.Completed, HelloResultCode.Canceled));

        result.Should().Be(WindowsPresenceResult.Cancelled);
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloDeviceIsBusy_ReturnsFailed()
    {
        var result = await Verify(FakeWindowsHelloNativeOps.Answers(AsyncStatusCode.Completed, HelloResultCode.DeviceBusy));

        result.Should().Be(WindowsPresenceResult.Failed);
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloRetriesAreExhausted_ReturnsFailed()
    {
        var result = await Verify(
            FakeWindowsHelloNativeOps.Answers(AsyncStatusCode.Completed, HelloResultCode.RetriesExhausted));

        result.Should().Be(WindowsPresenceResult.Failed);
    }

    // Hello unavailable — falls back to the secure-desktop credential prompt.
    [Fact]
    public async Task VerifyAsync_WhenHelloUnavailableAndPasswordValidates_ReturnsVerified()
    {
        var credential = FakeCredentialPromptNativeOps.PasswordValidates();

        var result = await Verify(HelloUnavailable(), credential);

        result.Should().Be(WindowsPresenceResult.Verified);
        credential.TokensClosed.Should().Be(1, "a validated logon token is closed");
        credential.FieldsFreed.Should().Be(1, "the typed credential's fields are freed (password zeroed)");
        credential.PromptBufferFreed.Should().BeTrue("the packed prompt buffer is freed");
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloUnavailableAndPasswordWrong_ReturnsFailed()
    {
        var credential = FakeCredentialPromptNativeOps.PasswordRejected();

        var result = await Verify(HelloUnavailable(), credential);

        result.Should().Be(WindowsPresenceResult.Failed);
        credential.TokensClosed.Should().Be(0, "a rejected password yields no logon token to close");
        credential.FieldsFreed.Should().Be(1, "the typed credential's fields are still freed on a wrong password");
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloUnavailableAndPromptCancelled_ReturnsCancelled()
    {
        var credential = FakeCredentialPromptNativeOps.Cancelled();

        var result = await Verify(HelloUnavailable(), credential);

        result.Should().Be(WindowsPresenceResult.Cancelled);
        credential.Unpacks.Should().Be(0, "a cancelled prompt is never unpacked");
        credential.PromptBufferFreed.Should().BeTrue("the prompt buffer is freed even on cancel");
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloUnavailableAndNoInteractiveSurface_ReturnsNoMethod()
    {
        var result = await Verify(HelloUnavailable(), FakeCredentialPromptNativeOps.NoInteractiveSurface());

        result.Should().Be(WindowsPresenceResult.NoMethod);
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloUnavailableAndBufferCannotUnpack_ReturnsError()
    {
        var credential = FakeCredentialPromptNativeOps.UnpackFails();

        var result = await Verify(HelloUnavailable(), credential);

        result.Should().Be(WindowsPresenceResult.Error);
        credential.TokensClosed.Should().Be(0, "an unpack failure never reaches logon");
    }

    [Fact]
    public async Task VerifyAsync_WhenHelloCannotBind_FallsBackToTheCredentialPrompt()
    {
        var result = await Verify(FakeWindowsHelloNativeOps.CannotBind(), FakeCredentialPromptNativeOps.PasswordValidates());

        result.Should().Be(WindowsPresenceResult.Verified, "a Hello that cannot bind falls back to the password prompt");
    }

    // The availability fast-fail guard (#47): an unavailable Hello short-circuits to NoMethod BEFORE any blocking prompt.
    [Fact]
    public async Task VerifyAsync_WhenHelloIsUnavailable_ReturnsNoMethodWithoutIssuingAnyPrompt()
    {
        var hello = FakeWindowsHelloNativeOps.Unavailable();

        // A credential fake that WOULD verify if the fallback ran, so a NoMethod result proves the prompt never ran.
        var credential = FakeCredentialPromptNativeOps.PasswordValidates();

        var result = await Verify(hello, credential);

        result.Should().Be(
            WindowsPresenceResult.NoMethod, "an unavailable Hello fast-fails to NoMethod and never prompts");
        hello.AvailabilityChecks.Should().Be(1, "the availability guard runs first");
        hello.VerificationsBegun.Should().Be(0, "no blocking Hello verification is issued when Hello is unavailable");
        credential.PromptsShown.Should().Be(0, "the secure-desktop credential prompt is never shown on the unavailable path");
    }

    // Honoring the token (#47): a cancel during a pending verification asks the native op to cancel and maps to Cancelled.
    [Fact]
    public async Task VerifyAsync_WhenTokenCancelsDuringVerification_AsksTheNativeOpToCancelAndReturnsCancelled()
    {
        var hello = FakeWindowsHelloNativeOps.PendingUntilCancelled();
        using var cts = new CancellationTokenSource();

        Task<WindowsPresenceResult> pending = new WindowsUserPresence(
            hello, FakeCredentialPromptNativeOps.NoInteractiveSurface()).VerifyAsync(Prompt, cts.Token);

        pending.IsCompleted.Should().BeFalse("the Hello verification stays pending until the token cancels it");

        await cts.CancelAsync();
        var result = await pending;

        result.Should().Be(WindowsPresenceResult.Cancelled, "a cancelled Hello verification maps to Cancelled");
        hello.CancelsRequested.Should().Be(1, "the port asks the native op to cancel the pending verification");
        hello.VerificationsBegun.Should().Be(1, "the interactive verification was issued before the cancel");
    }

    // Hello is unavailable / un-configured (a null-mapping consent), so the orchestrator falls back.
    private static FakeWindowsHelloNativeOps HelloUnavailable() =>
        FakeWindowsHelloNativeOps.Answers(AsyncStatusCode.Completed, HelloResultCode.DeviceNotPresent);

    private static Task<WindowsPresenceResult> Verify(FakeWindowsHelloNativeOps hello) =>
        Verify(hello, FakeCredentialPromptNativeOps.NoInteractiveSurface());

    private static Task<WindowsPresenceResult> Verify(
        FakeWindowsHelloNativeOps hello, FakeCredentialPromptNativeOps credential) =>
        new WindowsUserPresence(hello, credential).VerifyAsync(Prompt, CancellationToken.None);
}
