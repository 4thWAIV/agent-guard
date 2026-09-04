# appd-1-process — what Tim decided in conversation

This run covers GitHub issue #63 in full. The live issue is the authority; where it and this file disagree, the issue wins. Every decision below is Tim's, settled in conversation, and is the plan. This file records the choices the issue text does not already carry, plus the facts a spike established so no one re-derives them.

## Decisions

**The daemon verb is `daemon`.** `agentguard daemon` runs the guard binary in daemon mode. It joins the six verbs the binary already has: `hook`, `install`, `init`, `remove`, `doctor`, and `version`.

**Windows registers a Task Scheduler task with a logon trigger.** Not the Run registry key and not the Startup folder. `schtasks /Run` is the only one of the three that the CLI can also fire on demand, and issue #63 requires one start path to serve both login startup and the CLI's check-and-start.

**macOS registers a LaunchAgent plist under `~/Library/LaunchAgents`, and Linux registers a systemd user unit.** Both are user-session registrations rather than system ones, because appd has to be able to draw a window when the dashboard lands in issue #65. The CLI starts them on demand with `launchctl kickstart` and `systemctl --user start`.

**`agentguard install` writes the login registration.** It already places the binary under `~/.agentguard`; registering the login item becomes part of the same verb rather than a separate one.

**The CLI decides whether appd is running by testing the lock.** If the lock is held, appd is up. This is the same object that already enforces single instance, it is one filesystem call, and it behaves identically on all three platforms. It replaces shelling out to `launchctl print`, `systemctl --user is-active`, and `schtasks /Query`, which are three different tools with three different output formats to parse. Whether the login startup registration exists is a separate question that the service manager answers, and it belongs to `agentguard install` and `agentguard doctor` rather than to every command the user types.

**appd holds an exclusive file lock for as long as it runs, and exits immediately if it cannot acquire it.** The lock is `%LOCALAPPDATA%\AgentGuard\appd.lock` on Windows, `$XDG_RUNTIME_DIR/agentguard/appd.lock` on Linux, and `$TMPDIR/agentguard/appd.lock` on macOS. Each of those directories is already restricted to one account, so the lock is one-per-account without anything in the filename. The daemon holds it rather than relying on the service manager to refuse a second copy, because the service manager only knows about processes it started, and a daemon launched directly from a shell would otherwise become a second instance. A CLI probe and a starting daemon can collide over the lock; that collision fails closed, so it is not a concern. If it ever does need fixing, Tim's answer is a double-checked lock — a prep lock and a real lock.

**A named mutex is not used for the lock.** On macOS and Linux, .NET's named mutex without the `Global\` prefix is scoped to the POSIX session and silently permits a second holder in another session, which is the real deployment shape because the service manager starts appd in its own session while the CLI runs in the terminal's. With the `Global\` prefix it resolves to a single file under `/tmp/.dotnet/shm/global/` at mode 0666 inside a mode 0777 directory shared by every account on the machine, so two different users would collide on one name. On Windows a named mutex is a real kernel object and would work, but creating one in the `Global\` namespace requires `SeCreateGlobalPrivilege`, which a standard user does not hold, and appd runs unelevated.

**One appd runs per user account, not one per logon session.** Every `agentguard` command that account runs reaches the same process, in any terminal and in any session, console or remote. A second account logging into the same machine gets its own appd, and neither can see or reach the other's. This keeps one presence prompt and one key holder per account when the key work lands in issue #67.

## Facts a spike established

These were run against .NET 10 on macOS during the conversation. They are recorded so no one re-derives them.

**Named `Semaphore` and named `EventWaitHandle` do not exist on Unix.** Both throw `PlatformNotSupportedException: The named version of this synchronization primitive is not supported on this platform.` Named `Mutex` is the only named synchronisation primitive .NET offers on all three platforms.

**An unprefixed named mutex allows two simultaneous holders across POSIX sessions.** With a holder live in a separate POSIX session, a second process reported `TryOpenExisting=False`, `createdNew=True`, and `WaitOne(0)=True`, and acquired its own copy of the same name. With the `Global\` prefix the same test correctly reported `TryOpenExisting=True`, `createdNew=False`, and `WaitOne(0)=False`.

**An exclusive file lock behaves correctly across POSIX sessions.** A `FileStream` opened with `FileShare.None` was refused with an `IOException` while a holder in another POSIX session held the same path, and succeeded when nothing held it.

**Neither mechanism leaves a stale lock.** After the holder was killed with `SIGKILL`, both the named mutex and the file lock were immediately available again, so a killed daemon cannot block its own restart.

**The macOS per-user temporary directory is `$TMPDIR` at mode 0700.** On this machine that is `/var/folders/s5/lkpmbqsn17gcvx02hzxcdvr80000gn/T/`. A socket path beneath it comes to 69 bytes, inside the 104-byte limit for a Unix socket path on macOS.

**Nothing above was run on Linux or Windows.** Linux uses the same .NET Unix code path, and Windows is a genuinely different one. Both are proven on their own CI legs by the pointed-integration tests rather than taken on trust.

## Where the code stands today

Nothing daemon-related exists. There is no mention of a daemon, appd, a LaunchAgent, or systemd anywhere under `src` or `eng`. The per-OS assemblies this work extends are already in place as `AgentGuard.CrossPlatform` with `AgentGuard.CrossPlatform.Linux`, `.MacOS`, and `.Windows` beside it, which is the same shape the presence work used.
