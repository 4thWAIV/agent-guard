# 49 — Real-D-Bus stub test on CI: cover polkit reply-parsing + async, and prove CI real-integration testing

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/49

---

## What
A pointed-integration test on the Linux CI leg that stands up a real D-Bus (a session bus via `dbus-run-session`, or a system bus) with a **stub `org.freedesktop.PolicyKit1.Authority` service** returning canned `CheckAuthorization` replies, and drives `TmdsPolkitAuthority.CheckAuthorizationAsync` end to end against it.

## Why (two reasons)
1. **Coverage of the reply path.** `ReadAuthorizationResult` (parses `is_authorized` / `is_challenge` / details) and `CheckAuthorizationAsync` plus its async state machine — about 14 lines — cannot be tested off-bus: Tmds.DBus.Protocol makes the `Message`, `Reader`, `MessageWriter`, and `MessageBuffer` constructors all `internal`, so the only way to obtain a real reply `Message` is a real bus round-trip. This path is **security-relevant** — it decides approved-versus-denied.
2. **De-risk CI integration testing.** Proves we can run real-integration tests on CI (spin up a bus + a stub service), reducing unknowns for future OS-integration work.

## Sequencing
Immediately after the (A) serialization-only coverage sub-task. (A) gets macOS, Windows, and Linux all over the 75% floor via off-bus serialization + the Windows credential round-trip, with no coverage waiver; this issue (B) is the follow-on that covers the reply path and proves the CI harness.

## Approach notes
- Stub service implements `CheckAuthorization` with a **deterministic** reply (authorized true/false, challenge, a details entry) so the test asserts the `PolkitResult` mapping deterministically.
- Runs on the Linux CI leg; gated so it does not run where no bus/stub is available (mirrors how the native smoke is gated by `AgentGuardRunNativeSmoke`).
- Keep it inside the analyzer fence — the test drives `TmdsPolkitAuthority` through its owner, no Tmds interop leaks into the test beyond what the fence permits.

## Decision provenance
Tim: "Let's get (A) first but I want (B) right behind to prove we can work on CI so we have less unknowns."

---

## Guardrail shipping WITH this issue: ban reflection into Tmds internals (AG0117)

Folded in here per Tim (deferred out of coverage sub-task A on the condition it be recorded on this issue now).

Add an analyzer (proposed id AG0117) that bans using reflection to reach non-public members or constructors of any type in the `Tmds.DBus.Protocol` / `Tmds.DBus` namespace tree, anywhere in the repo (src/** and tests/**). It flags `BindingFlags.NonPublic` (or `Type.GetConstructor`/`GetMethod`/`GetField` with non-public flags, or an `Activator.CreateInstance` non-public overload) whose target type resolves — via the same `WellKnownType.IsInNamespaceTree` check `AG0110` already uses — to a type in that tree. No owner exemption. Preventive: RED against nothing today.

**Why it belongs here, not in sub-task A.** The read path (`TmdsPolkitAuthority.ReadAuthorizationResult`) needs a real reply `Message`, whose constructor Tmds keeps `internal`. The tempting shortcut while covering that read path is to reflect into that internal constructor to fabricate a fake `CheckAuthorization` reply and claim `ReadAuthorizationResult` is tested without a real bus — a contrived-shape rule-dodge that manufactures a green coverage number and re-introduces, inside test code, the ungoverned second D-Bus path `AG0110` exists to prevent. This analyzer makes that shortcut a build error and forces the read path through the real D-Bus stub this issue stands up. The dodge only becomes reachable when someone works the read path, which is this issue — so the guardrail ships with it.
