# 62 — appd: a long-running guard process that holds the signing key and only serves verified guard clients

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/62

---

> **This is the parent issue. The work is split across five parts — build them in order.**
>
> 1. **#63 — the process.** appd runs as a per-user process and the CLI can start it on demand.
> 2. **#64 — the channel.** The CLI talks to appd and gets answers. No security yet. Settles the XPC-from-.NET question.
> 3. **#65 — the dashboard.** The first visible proof the shape works. Needs only the process and the channel.
> 4. **#66 — identity.** The three-rung ladder, run in both directions.
> 5. **#67 — the key and presence.** appd holds the key and signs for verified callers. This makes signed decisions possible.
>
> This issue holds the design, the decisions, and the research findings. The parts hold the work.

## Why

Signing a decision needs a private key. Today the guard never holds one — the grant path is verify-only and `GrantStore` never mints. Presence shipped, so proving Tim is physically present is solved, but nothing turns that into a signature.

The OS secure stores do not solve this. Research established that no cross-platform mechanism lets a CLI authenticate once and then sign without prompting on later runs: macOS Authorization Services caches credentials across processes but its Touch ID path is hard-coded to Apple-signed executables, so a third-party CLI only ever gets a password prompt; Windows `KeyCredentialCacheConfiguration` is entirely undocumented; and polkit on Linux caches per shell rather than per binary and suppresses the per-decision message on a second, different decision, which is a correctness failure for decision signing.

A long-running process avoids all of it. It authenticates once at start, holds the key, and signs on request.

## What appd is

The same guard binary run in daemon mode. Not a system service — a per-user process in the login session, because it must be able to draw a dashboard window. A macOS LaunchAgent, a Windows per-user logon process, a Linux systemd user unit. The CLI checks whether it is running and starts it if not.

## The flow

1. The client checks whether appd is running. If not, it starts it. If it cannot, it fails.
2. The client sends its request.
3. appd asks the OS which process connected — peer credentials on a Unix socket, the client process id on a Windows named pipe, the audit token on macOS XPC. The client never asserts its own identity.
4. appd verifies that process is guard.
5. It serves the request, or refuses.

The presence check sits behind that door. Because signing only happens through appd, and appd only answers a verified guard binary, the AI cannot sign by calling the crypto itself — it has to come through the door, and it cannot fake the presence check. **That is the point of the whole design.**

## Step 4 — the identity ladder

Three rungs, cheapest first, checked in order. Any one passing is sufficient.

1. **Same path as the running daemon.** It is literally the same file. A running binary cannot be swapped underneath itself. No further work.
2. **Different path, same hash.** It is the same binary in another directory. One file read.
3. **Different hash, signed as the same product.** A different version, still legitimate. Only this rung needs a signature check, and only when the first two miss.

Rung three is per-OS: macOS checks the code signature against the team identity and Windows checks Authenticode, both locally with no network. Linux verifies the binary against the cosign signature CI already produces — the only case needing a network call. Cache that result keyed by the binary's hash, so a given version is verified once and later requests from it hit the cache.

Hash the calling binary on every request. It is a local file read and it removes any stale-cache problem.

## Known holes, and why they are acceptable

**The connect-then-exec trick.** A client can connect, send its request, then `exec` a legitimate binary before appd looks. The request is already queued in the kernel and does not care what the process became. This was proven by running it, not argued from documentation. It defeats the Linux and Windows paths. Mitigation: check the caller again after the exchange rather than only before, so a process that swapped identity no longer matches.

**Linux cannot see past an interpreter.** A Python or Node client's `/proc/PID/exe` is the interpreter, never the script. Anything that checks the binary either rejects every legitimate script or is loose enough to be meaningless. Not fixable.

**Windows named-pipe squatting.** One line of PowerShell can take the pipe name before appd starts, and a permission-bit collision means no ACL prevents it. An impostor could sit there collecting whatever the real CLI sends. Needs a decision.

None of these matter much against the actual adversary. The threat model is a lazy AI taking the easy path, and a lazy AI does not attack the IPC channel — it lies about what was approved. This design's real value is that it forces every signing request through one door with the presence check on it.

## Decided by Tim

**The dashboard is owned by appd.** appd draws it. The CLI can tell it to pop for the user, the user can pop it themselves through the CLI, and a tray icon where the OS supports one. The AI can drive it when it needs to put something in front of the user for signoff. Decided by Tim: appd owns the UI. The CLI can raise it for the user, the user can raise it themselves through the CLI, and a tray icon where the OS supports one. The AI can drive it when it needs to put something in front of the user for signoff.

**Two modes, always in the spec; the promotion shows only when it applies.** appd detects the environment's protection state at start and runs either way — it never refuses to hold the key because of an OS setting. The dashboard shows visually which mode is active. When the OS is already in the strong mode there is nothing to display; when it is in the weaker one the dashboard shows the option and how to move up. Whether a given OS even has two modes is per-OS and unresearched; where it has only one, nothing is shown. Decided by Tim: where an OS offers a stronger posture, warn the user in the dashboard when appd starts and tell them how to reach it. The spec always describes both modes; the dashboard shows the upgrade path only where the OS has one and the machine is in the weaker mode.

**Windows pipe squatting is solved by running the identity ladder in both directions.** Windows gives the server the client's process id and gives the client the server's process id via `GetNamedPipeServerProcessId`, so the same three-rung check runs symmetrically. An impostor daemon fails the client's check exactly as a fake client fails the server's. No challenge-response protocol and no extra key needed. Decided by Tim: use the same OS facilities to identify the process on both ends of the pipe, and run the same check in both directions.

Open: whether Linux can do the same — whether a client on a Unix socket can learn what process is serving it. Probably yes through the same peer credentials, unverified. macOS on XPC is already mutual.

**macOS uses XPC.** XPC is strictly better than the alternative: it is always available, and the OS supplies the caller's verified code identity rather than the daemon having to work it out.

**The recorded fallback, if and only if XPC proves unreachable.** XPC is a C API built around Objective-C blocks, and reaching it from a plain .NET binary means hand-building a block structure through P/Invoke — opening a connection without one crashes the process outright. Research indicated this is achievable in roughly fifty lines with no native helper and no `net10.0-macos` workload, but it is unproven in this codebase. If it turns out unreachable, or unreachable without a native helper or a macOS-specific build target that Tim has already rejected once, then macOS uses the same Unix socket as Linux with the same three-rung identity ladder. The signature check on rung three is local on macOS with no network call, so the fallback costs only the OS-supplied caller identity, not the identity check itself. This is a build feasibility question and must be settled by a spike before the design locks — it is not a design preference.

## Not in scope

The decision format itself, what gets signed, and the enforcement that flags contradictions between an agent's claims and a signed decision. Those are separate work that depends on this.

## Established by research, for whoever picks this up

- Never use a TCP loopback port. It carries no identity of the connecting process and any local process can connect. No firewall rule fixes it — the `owner` match only works on outbound.
- Never use a Linux abstract socket. Socket permissions have no meaning for them; every process on the machine can connect. Use a pathname socket in `$XDG_RUNTIME_DIR`, which is mode 0700.
- Set the socket mode explicitly. systemd's `SocketMode=` defaults to 0666.
- Read peer credentials on the accepted connection, not the listening socket. Under socket activation the listening socket reports systemd, not the client.
- A token file in a 0600 file buys nothing over socket permissions. Both stop other users, both fall to one line from the same user, and the token adds a secret at rest that can be copied or committed.
- `SO_PEERPIDFD` requires Linux 6.5 or later.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s



