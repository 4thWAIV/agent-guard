# 66 — appd 4 of 5: identity — appd and the CLI each verify the other is guard

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/66

---

Part 4 of the appd work. Parent: #62. Depends on #64.

## What this delivers

appd refuses to serve anything that is not guard, and the CLI refuses to talk to anything that is not guard. The same check, run in both directions.

## The three-rung ladder

Checked in order, cheapest first. Any one passing is sufficient.

1. **Same path as the running process.** It is literally the same file. A running binary cannot be swapped underneath itself. No further work.
2. **Different path, same hash.** The same binary in another directory. One file read.
3. **Different hash, signed as the same product.** A different version, still legitimate. Only this rung needs a signature check, and only when the first two miss.

**Hash the calling binary on every request.** It is a local file read, it costs nothing, and it removes any stale-cache problem. There is no expected value to store — appd compares against its own binary, because the CLI and appd are the same application in different modes.

## Rung three is per-OS

**macOS** checks the code signature against the team identity. Local, no network.

**Windows** checks Authenticode on the process image. Local, no network.

**Linux** verifies the binary against the cosign signature CI already produces. This is the only case needing a network call, so cache the result keyed by the binary's hash — a given version is verified once and later requests from it hit the cache. A machine realistically sees one or two versions, so that is one call after an upgrade and none after.

Decided by Tim: cache whatever material further validation needs on the platform that requires it, which is Linux and cosign. macOS and Windows use their built-in verification and cache nothing, since neither makes a network call.

## Getting the caller's identity from the OS, never from the caller

Peer credentials on a Unix socket, the client process id on a Windows named pipe, the audit token on macOS XPC. The client never asserts who it is.

## Both directions

Windows gives the server the client's process id and gives the client the server's process id via `GetNamedPipeServerProcessId`, so the same ladder runs symmetrically. This is what defeats pipe squatting — one line of PowerShell can take the pipe name before appd starts and no ACL prevents it, but an impostor fails the client's check exactly as a fake client fails the server's. No challenge-response protocol and no extra key.

Decided by Tim: use the same OS facilities to identify the process on both ends of the pipe, and run the same check in both directions.

**Open, and needs answering in this part:** whether a Linux client can learn which process is serving it. Probably yes through the same peer credentials, unverified. macOS on XPC is already mutual.

## Known holes, and why they are acceptable

**The connect-then-exec trick.** A client can connect, send its request, then `exec` a legitimate binary before appd looks. The request is already queued in the kernel and does not care what the process became. This was proven by running it, not argued from documentation, and it defeats the Linux and Windows paths. Mitigation: check the caller again after the exchange as well as before, so a process that swapped identity no longer matches.

**Linux cannot see past an interpreter.** A Python or Node client's `/proc/PID/exe` is the interpreter, never the script. Anything checking the binary either rejects every legitimate script or is loose enough to be meaningless. Not fixable.

Neither matters much against the actual adversary. The threat model is a lazy AI taking the easy path, and it does not attack the IPC channel — it lies about what was approved. The value here is that signing can only happen through appd, and appd only answers verified guard, so the presence check in part 5 cannot be bypassed.

## Done when

On all three platforms appd refuses a non-guard caller, the CLI refuses a non-guard server, all three rungs work, the Linux cosign result is cached by hash, and the connect-then-exec case is handled.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s
