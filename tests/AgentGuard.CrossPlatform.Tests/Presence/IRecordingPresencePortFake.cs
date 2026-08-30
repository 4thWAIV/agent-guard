// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The small OS-agnostic surface every per-OS presence-port fake exposes for the shared check-test spec
/// (<see cref="PresenceCheckPortSpec"/>) — the call count and the last prompt it was given. Each per-OS fake
/// (<c>FakeLocalAuthentication</c>, <c>FakeWindowsUserPresence</c>) already surfaces these off its composed
/// <see cref="SingleCallRecorder{TArg,TResult}"/>; declaring them behind this one interface lets the spec assert
/// "exactly one native call per Check, with the reviewed prompt" once for every OS instead of a copy per OS. It lives
/// directly under <c>Presence/</c> so it compiles on every OS leg, while each implementer stays in its per-OS folder.
/// </summary>
internal interface IRecordingPresencePortFake
{
    /// <summary>Gets the number of times the port's single method has been invoked.</summary>
    int CallCount { get; }

    /// <summary>Gets the prompt passed to the most recent invocation, or <see langword="null"/> if none.</summary>
    string? LastReason { get; }
}
