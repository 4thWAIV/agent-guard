# 64 — appd 2 of 5: the channel — CLI talks to appd, no security yet

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/64

---

Part 2 of the appd work. Parent: #62. Depends on #63.

## What this delivers

The CLI connects to appd, sends a request, and gets an answer back. No identity checking and no key — a working channel, demonstrably carrying traffic.

Decided by Tim: this part can demonstrate success on its own, before any security is added.

## Requirements

**The channel lives where only the user's account can reach it.** A Unix domain socket on macOS and Linux in a directory the user alone can read, a named pipe on Windows. This stops other users without a single line of security code, because it is filesystem permissions doing the work.

**Never a TCP loopback port.** It carries no identity of the connecting process, any local process can connect, and no firewall rule fixes it — the `owner` match only applies to outbound traffic.

**Never a Linux abstract socket.** Socket permissions have no meaning for them; every process on the machine can connect regardless of ownership or mode.

**Set the socket mode explicitly.** systemd's `SocketMode=` defaults to 0666.

## The XPC spike, and why it belongs here

macOS should use XPC rather than a Unix socket, because XPC gives the receiving side the caller's verified code identity from the kernel — the OS does the identity work that parts 4 has to do by hand everywhere else.

XPC is a C API built around Objective-C blocks, and reaching it from a plain .NET binary means hand-building a block structure through P/Invoke. Opening a connection without one crashes the process outright. Research indicated this is achievable in roughly fifty lines with no native helper and no `net10.0-macos` workload, but it is unproven in this codebase.

**Settle it with a spike in this part**, because the answer decides what macOS uses. If XPC proves unreachable, or only reachable through a native helper or a macOS-specific build target — which Tim has already rejected once for the presence work — then macOS uses the same Unix socket as Linux. The identity check still works there; the fallback costs only the OS-supplied caller identity, not the identity check itself.

## Out of scope

Identity checking, the dashboard, and the key.

## Done when

On all three platforms the CLI reaches appd, a request goes out and an answer comes back, another user cannot reach the channel, and the XPC question is settled with evidence rather than opinion.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s
