// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Linux;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The single owner of the representative unix-process subject the Linux presence tests share. Its start-time
/// (5,000,000,000) exceeds <c>uint.MaxValue</c> so the 64-bit D-Bus <c>"t"</c> variant path is exercised — the case a
/// fresh, low-uptime CI runner never reproduces — the pid is plausible, and the uid (1000) is the REAL uid, distinct
/// from the effective uid. Owned once here so the polkit request-serialization tests
/// (<c>TmdsPolkitAuthorityTests</c>, <c>TmdsPolkitAuthoritySerializationSpecTests</c>) and the <c>/proc</c> parser test
/// (<c>LinuxPresenceCheckTests</c>) all read the same values — the fabricated <c>/proc</c> fixtures and the assertions
/// that check the parse result can never drift apart. Compiled only on the Linux CI leg.
/// </summary>
internal static class PolkitSubjectFixtures
{
    /// <summary>The representative process id.</summary>
    internal const int Pid = 12345;

    /// <summary>The representative start-time, above <c>uint.MaxValue</c> so the 64-bit variant path is exercised.</summary>
    internal const ulong StartTime = 5_000_000_000UL;

    /// <summary>The representative REAL uid (the first <c>Uid:</c> column), distinct from the effective uid.</summary>
    internal const uint Uid = 1000u;

    /// <summary>The representative unix-process subject built from <see cref="Pid"/>, <see cref="StartTime"/>, and <see cref="Uid"/>.</summary>
    internal static PolkitSubject Representative => new(Pid, StartTime, Uid);
}
