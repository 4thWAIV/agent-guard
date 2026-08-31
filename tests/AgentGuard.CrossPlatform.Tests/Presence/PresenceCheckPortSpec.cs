// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using FluentAssertions;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single OS-agnostic owner of the per-OS presence-check spec: drive an <see cref="IPresenceCheck"/> over its native
/// port fake once, and assert it reached the port exactly once (a fresh interaction per <see cref="IPresenceCheck.Check"/>),
/// passed the reviewed prompt through, and mapped the port's outcome to the expected <see cref="ApprovalReason"/>. The
/// macOS and Windows check tests differ only in the fake type, the factory, and the outcome→reason table, so that
/// arrange/act/assert body lives here once (DRY) instead of a near-identical copy per OS; each per-OS test still supplies
/// its own fake and full-reason table so its coverage is unchanged. It lives directly under <c>Presence/</c> so it
/// compiles on every OS leg.
/// </summary>
internal static class PresenceCheckPortSpec
{
    /// <summary>The reviewed prompt every per-OS check test passes and this spec asserts reaches the port.</summary>
    internal const string Prompt = "please confirm the install";

    /// <summary>
    /// Drives <paramref name="port"/> through the presence check the <paramref name="createCheck"/> factory builds and
    /// asserts the single-fresh-call, prompt-passthrough, and outcome mapping.
    /// </summary>
    /// <typeparam name="TFake">The per-OS native-port fake, exposing its call count and last prompt.</typeparam>
    /// <param name="port">The per-OS fake configured to return one native outcome.</param>
    /// <param name="createCheck">The per-OS <c>IPresenceCheck</c> factory over the port.</param>
    /// <param name="expected">The <see cref="ApprovalReason"/> the configured outcome must map to.</param>
    /// <returns>A task that completes when the assertions have run.</returns>
    internal static async Task AssertMapsOutcomeInOneFreshCall<TFake>(
        TFake port, Func<TFake, IPresenceCheck> createCheck, ApprovalReason expected)
        where TFake : IRecordingPresencePortFake
    {
        IPresenceCheck check = createCheck(port);
        ISystemServices services = SystemServicesBuilder.Fake().Build();

        PresenceResult result = await check
            .Check(new PresenceRequest(Prompt), services, CancellationToken.None)
            .ConfigureAwait(false);

        result.Reason.Should().Be(expected);
        port.CallCount.Should().Be(1, "a fresh interaction is one native call per Check");
        port.LastReason.Should().Be(Prompt);
    }
}
