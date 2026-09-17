# 47 — macOS native smoke pops an interactive Touch ID dialog on a developer Mac and never self-dismisses

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/47

---

## Symptom
Running the full test suite on an interactive Mac with Touch ID pops the LocalAuthentication dialog and it does not go away on its own — the developer has to cancel it manually. Observed during the REFUTE panel of the OS presence-check contract, where adversaries run the acceptance-#1 command verbatim.

## Cause
`tests/AgentGuard.CrossPlatform.Tests/Presence/PresenceNativeSmokeTests.cs` (`RealPresenceImplementation_ReturnsANonApprovedReason_OnAHeadlessRunner`, `Category=NativeSmoke`) drives the REAL `MacOsPresenceCheck.Check` through the real container. On an interactive Mac, `canEvaluatePolicy` returns true, so it calls `evaluatePolicy`, which shows the OS dialog. The presence port mints no timeout of its own (by design — the gate owns the 60-second bound), and the smoke calls the port directly with `CancellationToken.None`, bypassing the gate, so the dialog waits for the user indefinitely.

It only surfaces because the smoke is meant for a headless CI runner (where `canEvaluatePolicy` is false → returns Unavailable without prompting). Local runs are supposed to exclude it (`--filter "Category!=NativeSmoke"`), but acceptance-#1's literal command in the contract is an unfiltered `dotnet test -c Release`, so any adversary or developer running it verbatim triggers the dialog.

## Not a correctness problem
Cancelling the dialog yields `LAError.UserCancel` → `Cancelled`, which is non-approved, so the smoke still PASSES (it only asserts `Reason != Approved`). Authenticating successfully would be the only thing that fails it. So this is a developer-experience/interactivity bug, not a test-correctness bug.

## Options to evaluate (with Tim)
1. Make the `NativeSmoke` category self-skip when the environment is interactive / not-CI (e.g., skip unless a `CI` env var is set) — cleanest, since the smoke is defined as a headless-CI test.
2. Change the contract's acceptance-#1 command to exclude `NativeSmoke` for local runs and keep the unfiltered run only on the CI legs (aligns the command with the contract's own "smoke is CI/manual" documentation).
3. Give the smoke a short cancellation token so it auto-cancels instead of hanging (changes what the smoke proves).

Deferred per Tim: "We should evaluate that when we are done."