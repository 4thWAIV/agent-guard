# 5 — Guard-enforced reuse ledger: deny any mutating run whose contract lacks a prior-art ledger (plan governance)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/5

---

## What
Extend the guard from file protection to plan governance: a mutating run's contract must carry a reuse ledger before the worker starts, and the guard makes that unskippable — the same way it makes protected-file edits unskippable.

## Why
Duplication is the most frequent defect the AI introduces. Today the reuse ledger is enforced only by process discipline plus a dedicated DRY adversary. Process discipline is exactly what fails under an AI that wants the easy path; the guard is the mechanism that makes a required step un-bypassable.

## Scope
- Define where the ledger lives for a run (e.g. run-records/<run>/reuse-ledger.md).
- Guard check: a mutating run with no ledger — or a ledger with a 'new' capability not proven empty across every discovery lens — is denied.
- The prior-art-ledger workflow (.claude/workflows/prior-art-ledger.js) already produces the ledger; this issue is the enforcement, not the search.

## Related
- Design-time search: .claude/workflows/prior-art-ledger.js (committed 612131a)
- Supersedes the curve-fit duplicate-value analyzer idea in #4.