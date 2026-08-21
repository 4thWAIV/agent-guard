---
name: rails-dry-code
description: The DRY guardrail (rail) against duplication — this project's most frequent defect — and the DRY adversary's PRIMARY PASS/FAIL checklist, paired with the prior-art-ledger tool. Load it when designing, implementing, or reviewing any new capability.
---

# Rails: DRY code

A rail against duplication — this project's most frequent defect, so it gets its own dedicated, full-time hunter and is never folded under SOLID. Duplication ONLY; design and structure are the SOLID rail's job. One source, three readers:

- **Architect** — follow every **Practice** while designing the work.
- **Worker** — follow every **Practice** while building; do not create any **Violation**.
- **DRY adversary** — this file is your PRIMARY PASS/FAIL checklist. Each **Violation** line is a concrete, checkable failure. Confirm each against the finished work and its live ground truth. **Any one confirmed Violation is a FAIL**; report it and its **Fix**. PASS only when all rails are clean.

**This rail REQUIRES the `prior-art-ledger` tool (`.claude/workflows/prior-art-ledger.js`), and the two are DIFFERENT things: this file is the CRITERIA, the tool produces the EVIDENCE.** The tool takes every capability the work needs, runs all three lenses per capability (CodeGraph, lore semantic search, grep), and a cheap model rules each one **reuse / extract / new** — emitting a ledger of verdicts (`{capability, decision, owner or copies, evidence, confidence}`). The practice is that **every new capability — including a one-off helper — must carry such a prior-art-ledger ruling**, and **"new" is valid only when every lens came back empty. The DRY adversary re-runs the prior-art-ledger itself to verify that ledger, never trusting it.**

Run the gate at the bottom before acting. If the work trips any rail below, STOP and collapse the duplication to one owner.

## The rails

### 1. Capability built with no prior-art-ledger ruling
- **Practice:** Before building any new capability — including a one-off helper or a new guardrail — run the `prior-art-ledger` tool over it and carry its ruling (reuse / extract / new) in the contract's reuse ledger. A helper or guardrail needing no sign-off is about approval, not a license to skip this check; reuse the right existing owner before adding a new one. Use the tool rather than hand-rolling the search so no lens is skipped; if it isn't available, run the same three lenses yourself.
- **Violation:** A capability was built with no prior-art-ledger ruling, or the reuse ledger is missing from the contract — the duplication check was never run at design time.
- **Fix:** Run the prior-art-ledger over every capability the change needs; record each reuse / extract / new ruling before building.

### 2. Ledger trusted instead of verified
- **Practice:** Own the reuse ledger. Re-run every discovery lens YOURSELF for each capability the change introduces — CodeGraph (`codegraph_explore`), lore semantic search (`search_code`), and grep. Do not rely on the worker's ledger entries.
- **Violation:** The ledger was accepted as written with no lens re-run, so a wrong ruling passes unchecked. (A DRY adversary that lists no re-run queries is rubber-stamping.)
- **Fix:** Re-run all three lenses per capability yourself; record each query and what it returned (file:line).

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
- **Practice:** No single lens finds every copy, so chain them: run semantic search to get the concept neighborhood, pivot on the shared primitive it exposes, ask CodeGraph for that symbol's callers and blast radius, then READ the callers.
- **Violation:** The search stopped at a single top-N result, leaving copies the chain would have found in place.
- **Fix:** Run the full chain per capability; never trust a single top-N result.

### 8. Reimplementing a primitive instead of reusing it behind an owner
- **Practice:** When work needs a capability a BCL, OS, or OSS primitive already provides, default to reusing that primitive — reuse holds until the existing one is proven insufficient. The one-owner rule (a raw primitive is legal only inside its owner) is a forcing function to pick the deliberate owner, never a bar to reuse. It requires three decisions — reuse vs compose, how to abstract it, and which one class owns it — which you bring to the human as a conversation, get approved, and record in the contract's Decisions section before building; RULE-PHASE then carves the owner exemption so the raw call is legal in that owner and banned everywhere else.
- **Violation:** A tested BCL/OS/OSS primitive was reimplemented from scratch or hand-composed from lower-level calls to avoid giving it an owner — more work, more risk, reuse defeated — or its owner and abstraction were chosen and built without the human's recorded decision, so a bad ownership choice becomes load-bearing before anyone approved it.
- **Fix:** Reuse the primitive behind its one deliberate owner; bring the reuse, abstraction, and owner choice to the human, get sign-off, and record it in the Decisions section before building. RULE-PHASE carves the owner exemption.

## The gate (answer before acting)

- Does every new capability — including one-off helpers — carry a prior-art-ledger ruling of reuse / extract / new?
- Did I re-run all three lenses (CodeGraph, lore, grep) myself per capability rather than trust the ledger?
- Is every "new" genuinely empty on every lens, and did every "reuse" / "extract" call the owner instead of a fresh copy?
- Is every duplicated value, block, and whole function — including copies across modules the analyzers can't see — collapsed to one owner?
- Did I chain the lenses (semantic → shared primitive → CodeGraph callers → read the callers) instead of trusting a single top-N result?
- When a capability is already provided by a BCL/OS/OSS primitive, am I reusing it behind a deliberate owner the human approved and the contract records — not reimplementing it to dodge the owner rule?

If any answer is no, there is duplication. Stop and collapse it to one owner.
