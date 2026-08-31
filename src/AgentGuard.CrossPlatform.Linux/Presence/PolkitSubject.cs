// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.CrossPlatform.Linux;

/// <summary>
/// The classic unix-process subject naming the calling process to polkit — the triple
/// (presence-subject-triple), read from <c>/proc/self</c> through the owned <c>IFileReader</c> by
/// <see cref="LinuxPresenceCheck"/> and written to the D-Bus <c>a{sv}</c> dict by <see cref="TmdsPolkitAuthority"/> with
/// polkit's expected variant types (notably <see cref="StartTime"/> as a 64-bit value). The triple is required on RHEL 8
/// / older Ubuntu LTS, which reject a pidfd subject.
/// </summary>
/// <param name="ProcessId">The process id — field 1 of <c>/proc/self/stat</c>.</param>
/// <param name="StartTime">The process start time — field 22 of <c>/proc/self/stat</c>, written 64-bit on the wire.</param>
/// <param name="UserId">The real user id — the first column of the <c>Uid:</c> line of <c>/proc/self/status</c>.</param>
internal sealed record PolkitSubject(int ProcessId, ulong StartTime, uint UserId);
