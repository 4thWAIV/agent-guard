# 40 — Presence subject identity: prefer pidfd where supported, fall back to the triple

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/40

---

## Problem

The Linux presence check names the calling process to polkit with the classic **unix-process triple** — process id, process start time, and user id — all read from `/proc/self/stat` and `/proc/self/status` through the owned file reader, with zero native code. The triple was chosen because it works everywhere: the polkit shipped on current enterprise distros (RHEL 8, older Ubuntu LTS) does not accept a `pidfd` subject at all.

The modern `pidfd` subject is stronger: a kernel handle that closes the process-id-reuse race the triple only hardens against via start-time. That race is outside the current lazy-AI threat model, so the triple is sufficient now — but on systems whose polkit supports pidfd, using it would tighten the presence boundary.

## Ask

Support **both**: prefer the `pidfd` subject where the running polkit supports it, and fall back to the triple otherwise, selected by a runtime capability-detection process (probe polkit version / pidfd-subject support, or attempt-pidfd-then-fall-back-on-fault). Keep the triple as the guaranteed floor.

## Cost / notes
- `pidfd` requires a small `pidfd_open(2)` P/Invoke on Linux (a `SafeHandle` wrapper — no callbacks, no marshalling), which brings the Linux presence owner under AG0101's native-call ownership. That is already prepared: AG0101's owner set was extended to include `IPresenceCheck` in the presence contract, so the analyzer side needs no further change for this upgrade.
- Detection must fail safe: if pidfd support can't be determined, use the triple; never let a detection failure open the gate.

## Pointers
- The Linux presence adapter (`TmdsPolkitAuthority` / `LinuxPresenceCheck`) built by the OS presence-check contract (`.dev/inprocess/2026-08-23-os-presence-check/`).
- `research-polkit.md` §1 (subject forms) and §6 (.NET mechanism), in that run-record.
