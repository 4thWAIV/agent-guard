---
name: how-to-work-here
description: Read first — the on-ramp for working in this repo. The working rules (canonical, overriding private agent memory) plus pointers to the process, the .dev flow, the frozen areas, and the tools. It is an index, not a replacement: before a first substantive change, also read the workflow-with-adversaries process and the contract guide it points to.
---

# How to work in this codebase

## What this repo is
agent-guard is a tool whose sole job is to stop a lazy AI from disabling or cheating the guard and the golden build. The adversary is the AI taking the easy path and papering over it — not a malicious attacker. It is not a hardened security product; the bar is: **make cheating more work than doing the right thing.**

## The rules that get people fired if broken

### Decisions and approval
- A decision exists only when the human gives an **explicit yes to that exact item**. No yes → it is not decided; it stays an open item. When you record a decision, quote the human's approving words next to it.
- **Silence, a topic change, or a request to reword / clarify / define is never approval.** A blanket "do it" / "put them back" approves only items already individually approved — never a new one. Reversing a previously-approved design needs a fresh explicit yes (a higher bar, not lower).
- Anchor approval to the human's actual words, **never to "it's already in the code"** — existing-in-the-tree is how an unapproved change launders itself into looking approved.
- Approval is owed for anything that **adds or reverses a design element that outlives its function** — a mechanism, data/wire format, invariant, file, dependency, or interface. How a named thing is internally coded (a loop, a helper, a name) is implementation, not a decision, and needs no sign-off.
- Settle decisions **in conversation first** (a short numbered list of open choices, recommended pick marked), then write the record — which holds only what was decided, never open questions.
- Approval of a unit of work approves **all** of it: never split it into unrequested stages, do only part, stop at an unrequested checkpoint, or add/ship/decide anything unapproved. To cut or defer any part, ask a direct question naming exactly what would be cut and get a specific yes.
- Describe what you're about to do and **why** before submitting the actions that do substantial work. When an unexpected problem the plan doesn't cover appears, **stop and present it** for a decision — don't improvise around it.

### Verification
- **Verify against the live file as it is right now on disk (the working tree), never from memory** or a stale/summarized artifact — the committed version can lag your own uncommitted edits, so read the actual current file. When a decision is finalized-but-not-yet-implemented, answer from the recorded decision, not the pre-change code the decision exists to change.
- Build (`dotnet build` — 0 warnings, 0 errors) and all tests (`dotnet test` — 0 failed) must be green **before** you start a work item and **again after**. Any breakage after is caused by your change and must be fixed — never rationalized as unrelated.
- **Never trust an exit code or status message** as proof. Demand positive proof of the resulting state, pasted verbatim with exit codes. Cropped / stale / exit-code-missing output is unproven.
- A red check on the branch is **fixed, not excused as "pre-existing."** A test **you** broke is fixed in the code — never by loosening, skipping, deleting, or re-pointing the test's expected value. The only repin-allowed case: a **pinned oracle** — a deliberately-fixed baseline value (a golden output, a snapshot, a recorded expected value), not an ordinary assertion — whose correct value genuinely changed; repinning it needs the human's **explicit yes to the repin** (same bar as any decision), never a reason you wrote yourself. Never scaffold or hard-wire a check to pass.
- "Done" needs re-runnable proof (exact command + full output) that **someone other than the doer — a reviewer/adversary agent or the human —** can re-derive; your own assertion is not proof.
- When the same value lives in more than one store, enumerate every store first, then verify each. Never declare N/N off one store.

### Design discipline
- Follow **SOLID/DRY**; if the code's state makes that hard in a way the plan didn't cover, stop and present it.
- **Never weaken a requirement to fit the code** — grow the code to meet the requirement. Any change that quietly makes a requirement match existing behavior is a top-line finding.
- Build the **full general seam** up front for known-needed design; no one-case hacks, no "defer the abstraction." Don't pre-build escape hatches "for later" — expansion happens on a real, approved need.
- When a correct approach and a cheaper wrong one are both visible, **take the correct one and report it** — never ship the easy option disguised as a "your call" flag. Reserve flagging for genuine unresolved trade-offs.
- No language escape hatch (e.g. TypeScript `any`) without explicit approval and a stated reason it beats the alternatives.
- Before building any new capability — **including a one-off helper** — produce a reuse ledger with the **prior-art-ledger workflow** (`.claude/workflows/prior-art-ledger.js`): it runs every lens (CodeGraph, lore, grep) per capability and rules each reuse / extract / new. Use the workflow rather than hand-rolling the search, so no lens is skipped (if it isn't available, run those same lenses yourself). "New" is valid only when every lens comes back empty; "a helper needs no sign-off" is about approval, not a license to skip this check. Reuse the right owner before adding a new guardrail.

### Communication
- Open every message with **the verdict/answer**, not narration, preamble, or self-commentary ("owning it," recaps). Conclusion first, then dense facts, then decisions.
- **Answer the literal question** in the asker's vocabulary, in the fewest words — never circle, hedge, pad, or answer a nearby question. If the honest answer is "it doesn't exist" or "I didn't do it," say that plainly and first.
- A question **confirming the asker's own correct understanding** gets a direct yes/no plus at most the one thing they might not know — never a restatement of what they said, never unprompted narration of your own mistakes.
- Plain, existing vocabulary — **no coined jargon**. State concrete values (the real number, tool, file, default), never vague references. Assume an expert audience; don't explain basic concepts unasked.
- Structure: lead with what must be **DECIDED** (one line each, recommended pick marked, or "nothing"), then what you'll do, then detail below a fold. Keep a sentence only if it's a decision, a new fact, or an action.
- Reports are self-contained — no reference to internal context the reader hasn't seen. Status reports lead with pass/fail against the success definition, then known-broken count and delta; say "not complete" before describing any success; never bury a failure under progress.

## How work runs (the process)
This file is the on-ramp — the rules plus pointers; it does not replace the process docs. **Substantive work** is anything that mutates state or behavior, ships or changes code, claims a fix, adds a capability, or changes a validator / gate / process — as opposed to a trivial doc edit, a read, or an implementation detail inside an already-approved unit — where "already-approved" covers only the exact item that got a yes, not an inflated reading of it. When in doubt, treat it as substantive. **Before your first substantive change you must read** `.dev/reference/workflow-with-adversaries/SKILL.md` and `HOW-TO-WRITE-A-CONTRACT.md`. In short, substantive work follows **workflow-with-adversaries**:
**GROUND → CONTRACT → IMPLEMENT → REFUTE → GATE → (ESCALATE).** Establish ground truth from live code; write the contract (instructions + checks) before any work; one worker implements it exactly; independent adversaries (correctness, SOLID, DRY, lie-catcher) try to refute the result; accept only when nothing survives and every check is proven; escalate to the human on repeated refutation or a genuine design fork. Tier the rigor by risk, but never skip the written contract.

Also:
- Before presenting non-trivial design/solution work, run the **laziness auditor** — spawn a **separate** adversary agent (never self-review) to hunt shortcut patterns (easy-half, one-case hack, unnecessary hedge, deferred known design, over-engineering). Not optional. Triage each finding; fix confirmed ones. Like a "done" claim, running the auditor and the refutation agents is only real if their **verbatim output is preserved and shown** — "ran it, clean" with nothing to re-derive does not count.
- **Adjudicate** every review finding against the actual code before acting — prove it right or wrong; reject false positives with evidence; findings that change an agreed design need sign-off.
- **File deferred work as a GitHub issue the moment it's deferred** — via the `gh` CLI (`gh issue create`) — with the real requirements in the body. A deferral recorded only as a note or memory is untracked and won't happen.

## Where things live (the .dev flow)
See `.dev/README.md`. Work moves **backlog → inprocess → completed**. `reference/` holds living docs (this process, decisions); `archive/` holds historical / non-workflow material.

## Frozen areas — never touch without a separate contract
- `src/AgentGuard.Engine/Abstractions/**` (the contract interfaces)
- `analyzers/**`

## Tools — reach for these before grep/find/reading files
- **CodeGraph** (`codegraph_explore`) — when a `.codegraph/` index exists, one query returns the relevant symbols' source plus call paths. Don't create an index unprompted.
- **lore** — semantic code search across the repo.
- **grep** — exact string/token search.
- **prior-art-ledger** workflow (`.claude/workflows/prior-art-ledger.js`) — runs all lenses per capability and rules reuse / extract / new; produces the reuse ledger a contract must carry.

## This file and your private memory
This checked-in skill is the shared, reviewed copy of these rules — it travels with the repo and serves any agent, teammate, or cold start that has no private memory. Your own private memory carries a **full working copy** of these rules, not a pointer back here: memory is what you recall readily every session without opening this file, so the content has to actually live there.

**Reconcile on load.** When you read this skill, sync your private memory to it: for every rule here, make sure the matching memory entry carries the rule's current content — bring stale wording up to date, add what's missing — alongside the project-specific incident context that memory already holds. Update, never delete; keep both in sync. This applies to any rule-set skill you load, not just this one.
