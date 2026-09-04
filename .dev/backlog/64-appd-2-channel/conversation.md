# appd-2-channel — what Tim decided in conversation

This run covers GitHub issue #64 in full. The live issue is the authority; where it and this file disagree, the issue wins.

These two decisions were settled while working on issue #63 and are recorded here because #63 puts the channel out of its own scope.

## Decisions

**The Windows pipe name carries the user's SID**, as `\\.\pipe\agentguard.<SID>`, read from `WindowsIdentity.GetCurrent().User.Value`. The daemon and the CLI each build that name the same way and arrive at it independently. The SID appears there because the `\\.\pipe\` namespace is flat and shared by every account on the machine, and it is the only name in this design with no per-user directory to sit inside. On Linux and macOS the socket sits in the same per-user directory as the lock and needs no suffix.

**The Windows pipe is created with a security descriptor granting only the owning SID, and with `PipeOptions.FirstPipeInstance`.** The SID in the name keeps two accounts from aiming at the same pipe in ordinary use. The security descriptor makes the kernel refuse a deliberate attempt, which is what stops one user from being shown a presence prompt about another user's work — in issue #67 that would be one user signing another's decision. Squatting, where an impostor holds the pipe name before appd starts, is a separate problem that the mutual identity check in issue #66 answers; the security descriptor is not offered as a solution to it.
