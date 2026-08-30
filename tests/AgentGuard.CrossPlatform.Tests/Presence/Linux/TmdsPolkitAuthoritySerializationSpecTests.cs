// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.CrossPlatform.Linux;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The Linux native-ops pointed-integration spec test (guardrail-1-nativespec-convention-test, linux-off-bus-seam). It
/// drives the REAL <see cref="TmdsPolkitAuthority"/> serialization — the whole
/// <c>org.freedesktop.PolicyKit1.Authority.CheckAuthorization</c> request assembly and D-Bus wire encoding
/// (<see cref="TmdsPolkitAuthority.BuildCheckAuthorizationRequest"/> then the private
/// <c>CreateCheckAuthorizationMessage</c> and its <c>WriteSubject</c> / <c>WriteSubjectEntry</c> / <c>WriteDetails</c>
/// writers) through the internal off-bus entry point <see cref="TmdsPolkitAuthority.SerializeCheckAuthorizationOffBus"/>.
/// That entry point serializes the message but never opens the system bus, so this real-boundary coverage runs on any
/// host (there is no polkit or system bus on the CI runner) — the coverage a unit test over a fake cannot give.
///
/// Compiled only on the Linux CI leg (it references the internal Linux <see cref="TmdsPolkitAuthority"/> through the
/// per-OS <c>InternalsVisibleTo</c> grant). It shows no prompt and reaches no live bus, so — like the macOS
/// <c>MacOsObjCRuntimeSpecTests</c> — it is NOT behind the interactive native-smoke opt-in and runs on every Linux build.
///
/// It is tagged <see cref="NativeSpecOwnerAttribute"/> for <see cref="IPolkitAuthority"/> so the guardrail-1 convention
/// test (<see cref="NativeSpecCoverageConventionTests"/>) recognizes this as the Linux native-ops owner's spec test.
/// </summary>
[NativeSpecOwner(typeof(IPolkitAuthority))]
public sealed class TmdsPolkitAuthoritySerializationSpecTests
{
    // The message is the reviewed per-call polkit.message.
    private const string Message = "AgentGuard native-ops serialization spec";

    [Fact]
    [Trait(NativeSpecOwnerAttribute.CategoryTraitName, NativeSpecOwnerAttribute.NativeSpecTraitValue)]
    public void RealTmdsPolkitAuthority_SerializesTheCheckAuthorizationCall_OffBus_WithoutThrowing()
    {
        var subject = PolkitSubjectFixtures.Representative;

        Action serialize = () =>
            TmdsPolkitAuthority.SerializeCheckAuthorizationOffBus(subject, PolkitAction.Id, Message);

        serialize.Should().NotThrow(
            "the real polkit CheckAuthorization request assembly and D-Bus serialization must run to completion "
            + "without opening a bus");
    }
}
