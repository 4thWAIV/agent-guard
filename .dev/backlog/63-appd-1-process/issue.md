# 63 — appd 1 of 5: the process — a per-user guard daemon the CLI can start on demand

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/63

---

Part 1 of the appd work. Parent: #62.

## What this delivers

The same guard binary, run in daemon mode, staying up as a per-user process. Nothing else — no key, no dashboard, no identity checking, no channel. Just a process that starts, stays up, and can be found.

## Requirements

**One canonical start path.** Decided by Tim: autostart at login is the normal case, and an installer starts it where one exists. The CLI checks whether appd is running and, when it is not, triggers that same canonical start rather than launching it some other way. This covers installing onto an already-running login session without a second, divergent startup mechanism.

**It must be able to draw a window later**, which constrains what kind of process it is on every platform. Not a system service. On macOS a LaunchAgent in the user session rather than a LaunchDaemon; on Windows a per-user process rather than a service, because services run in session 0 and cannot reach the desktop; on Linux a systemd user unit rather than a system unit. This part does not draw anything, but choosing the wrong process type here blocks the dashboard entirely.

**Exactly one runs at a time.** Two CLI invocations racing to start it must not produce two daemons.

**The CLI detects whether it is already running** before deciding to start it.

**Login startup registration is part of the canonical path, not an optional extra.** The CLI's check-and-start is the fallback that covers a machine where login startup has not yet taken effect — it is not a replacement for it.

## Out of scope

The channel, the dashboard, identity checking, and the key. Each is its own part.

## Done when

On all three platforms: appd starts at login, the CLI starts it through the same canonical path when it is not running, no second instance starts when one is already up, appd survives the CLI exiting, and the process type chosen can draw a window when part 3 needs it to.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s
