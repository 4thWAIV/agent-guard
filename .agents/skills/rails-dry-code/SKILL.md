---
name: rails-dry-code
description: The DRY guardrail (rail) against duplication — this project's most frequent defect — and the DRY adversary's PRIMARY PASS/FAIL checklist, paired with the prior-art-ledger tool. Load it when designing, implementing, or reviewing any new capability.
---

# Rails: DRY code

A rail against duplication — this project's most frequent defect, so it gets its own dedicated, full-time hunter and is never folded under SOLID. Duplication ONLY; design and structure are the SOLID rail's job. One source, three readers:

- **Architect** — follow every **Practice** while designing the work.
- **Worker** — follow every **Practice** while building; do not create any **Violation**.
- **DRY adversary** — use every **Violation** as the PRIMARY checklist for the finished work and its live ground truth.

This file owns the prior-art criteria. The `prior-art-ledger` workflow (`.claude/workflows/prior-art-ledger.js`) produces the evidence.

## The rails

### 1. Capability built with no prior-art-ledger ruling
- **Practice:** Before building any new capability — including a one-off helper or a new guardrail — run the `prior-art-ledger` tool over it and carry its ruling (reuse / extract / new) in the contract's reuse ledger. A helper or guardrail needing no sign-off is about approval, not a license to skip this check; reuse the right existing owner before adding a new one. Use the tool rather than hand-rolling the search so no lens is skipped; if it isn't available, run the same lenses yourself. When the work introduces no new capability, use an empty capability list and record `None — this change introduces no new capability` in the contract. Do not invent a fake capability merely to satisfy the workflow.
- **Violation:** A new capability was built with no prior-art-ledger ruling, the reuse ledger is missing from the contract, or the work invents a fake capability instead of recording `None — this change introduces no new capability` — the duplication check was not recorded correctly at design time.
- **Fix:** Run the prior-art-ledger over every new capability before building and record each reuse / extract / new ruling; when there is no new capability, record `None — this change introduces no new capability`.

### 2. Ledger trusted instead of verified
- **Practice:** Own the reuse ledger. Re-run every discovery lens YOURSELF for each capability the change introduces — CodeGraph (`codegraph_explore`), and grep across `src`, `tests`, `analyzers`, and `eng`. Do not rely on the worker's ledger entries.
- **Violation:** The ledger was accepted as written with no lens re-run, so a wrong ruling passes unchecked.
- **Fix:** Re-run both lenses per capability yourself; record each query and what it returned (file:line).

### 3. False "new"
- **Practice:** A capability is "new" only when every lens came back empty.
- **Violation:** A capability the ledger marked "new" that any lens shows already exists, or that was not checked by every lens.
- **Fix:** Reclassify to reuse or extract against the owner a lens found; reuse that owner instead of building.

### 4. "Reuse" / "extract" written as a fresh copy
- **Practice:** A capability ruled "reuse" calls the existing owner; one ruled "extract" collapses the copies into a single shared owner and calls it.
- **Violation:** A capability the ledger marked "reuse" or "extract" where the worker instead wrote a fresh copy.
- **Fix:** Delete the copy; call the existing owner (reuse) or the one owner extracted from copies A / B / C (extract).

### 5. Duplicated value
- **Practice:** Every literal string, number, or path lives in exactly one named owner and is referenced from there.
- **Violation:** A duplicated string, number, or path spelled in more than one place.
- **Fix:** Hoist it to one named constant/owner and reference it everywhere.

### 6. Duplicated block or whole function
- **Practice:** Every block of logic and every function exists once.
- **Violation:** A duplicated block or whole function — same logic even with renamed variables — **including copies across projects/modules, which the in-build analyzers cannot see.**
- **Fix:** Extract one owner and collapse every copy into it.

### 7. Single-lens search that misses copies
- **Practice:** No single lens finds every copy, so chain them: grep the distinctive tokens across `src`, `tests`, `analyzers`, and `eng` to get the concept neighborhood, pivot on the shared primitive those hits expose, ask CodeGraph for that symbol's callers and blast radius, then READ the callers.
- **Violation:** The search stopped at a single top-N result, leaving copies the chain would have found in place.
- **Fix:** Run the full chain per capability; never trust a single top-N result.

### 8. Reimplementing a primitive instead of reusing it behind an owner
- **Practice:** When work needs a capability a BCL, OS, or OSS primitive already provides, default to reusing that primitive — reuse holds until the existing one is proven insufficient. The one-owner rule (a raw primitive is legal only inside its owner) is a forcing function to pick the deliberate owner, never a bar to reuse. It requires three decisions — reuse vs compose, how to abstract it, and which one class owns it — which you bring to the human as a conversation, get approved, and record in the contract's Decisions section before building; RULE-PHASE then carves the owner exemption so the raw call is legal in that owner and banned everywhere else.
- **Violation:** A tested BCL/OS/OSS primitive was reimplemented from scratch or hand-composed from lower-level calls to avoid giving it an owner, or its owner and abstraction were chosen and built without the human's recorded decision.
- **Fix:** Reuse the primitive behind its one deliberate owner; bring the reuse, abstraction, and owner choice to the human, get sign-off, and record it in the Decisions section before building. RULE-PHASE carves the owner exemption.

## Verdict

Return PASS only when no Violation is confirmed. Return FAIL when any Violation is confirmed.
