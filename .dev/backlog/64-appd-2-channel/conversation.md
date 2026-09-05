# appd-2-channel — what Tim decided in conversation

This run covers GitHub issue #64 in full. The live issue is the authority; where it and this file disagree, the issue wins.

These two decisions were settled while working on issue #63 and are recorded here because #63 puts the channel out of its own scope.

## Decisions

**The Windows pipe name carries the user's SID and the Terminal Services session id**, as `\\.\pipe\agentguard.<SID>.<sessionId>`. The SID comes from `WindowsIdentity.GetCurrent().User.Value`; the session id is the same number Windows itself puts in a `Local\` object's path, `\Sessions\<SessionId>\BaseNamedObjects`, read through `Process.GetCurrentProcess().SessionId` or the `ProcessIdToSessionId` call underneath it. The daemon and the CLI each build the name the same way and arrive at it independently. Both parts are needed: the `\\.\pipe\` namespace is flat and shared by every account on the machine, and appd is scoped per logon session on Windows, so two sessions belonging to the same person would otherwise reach for one name. Session ids are recycled after a logoff, and the SID is what the security descriptor grants to, so keeping both makes the name and the permissions say the same thing. On Linux and macOS the socket sits in the account's own runtime directory and needs no suffix at all.

**The Windows pipe is created with a security descriptor granting only the owning SID, and with `PipeOptions.FirstPipeInstance`.** The SID in the name keeps two accounts from aiming at the same pipe in ordinary use. The security descriptor makes the kernel refuse a deliberate attempt, which is what stops one user from being shown a presence prompt about another user's work — in issue #67 that would be one user signing another's decision. Squatting, where an impostor holds the pipe name before appd starts, is a separate problem that the mutual identity check in issue #66 answers; the security descriptor is not offered as a solution to it.
