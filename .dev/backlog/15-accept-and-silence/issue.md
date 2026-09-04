# 15 — Accept-and-silence: let the human acknowledge a known limitation once so agents stop re-flagging it

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/15

---

## Problem
When a change has a known, accepted limitation (e.g. a Windows path only provable on real CI), agents re-flag it in every report — the same PSA over and over. The human has already accepted it and wants it to go silent.

## Interim (works today)
A per-contract "Accepted known limitations (acknowledged — do NOT re-surface)" list in the contract/run-record. Once the human says "I accept X," it goes on the list, and every agent (worker, adversary, orchestrator report) checks that list and never raises an item on it again. First use: the cross-platform contract's Windows-re-point-CI-pending item.

## Fuller mechanism (Tim's idea — deferred until enough of the guard system works)
A first-class acknowledgement the human issues once that durably marks a risk/limitation as accepted so it is never re-surfaced — likely tied to the acknowledgement/grant/minting system rather than a hand-maintained list. Design TBD when the system supports it.

## Acceptance
- Agents consult the accepted-limitations list and never re-raise an accepted item.
- The rule is written into the rails (report/refute prompts) so it is enforced, not remembered.