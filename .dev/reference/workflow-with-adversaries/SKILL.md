---
name: workflow-with-adversaries
description: The standard way substantive work runs — a worker executes against a written CONTRACT, independent adversaries REFUTE the result against that contract, a gate accepts only unrefuted+proven results, and genuine disagreement escalates to the human. Use for any mutating change, any claimed fix, migrations, validator/gate changes, or any reusable process change. Operationalizes adversarial-plan-review with agents; grounds every judgment with the explorer skill.
---

# workflow-with-adversaries

This is the NEW WAY substantive work runs, not a per-task tool. It sits on `adversarial-plan-review` (the blocking self-audit checklist) and uses `explorer` (derive the subsystem's ground truth from live code before judging). This skill wires those into a run with agents.

The contract replaces the subjective "would the human approve?" with checks an adversary can refute. It is a file, not a vibe.

## The two standing rules

**RULE 1 — never weaken to fit.** Never narrow, soften, or reinterpret a human requirement to fit what the code already does. Grow the code or tool until it meets the requirement. Any change that quietly makes a requirement match existing behavior is a top-line finding.

**RULE 2 — never invent the human's approval.** A decision the human did not approve, in their own words, is a lie — ranked with a weakened test. Silence, a topic change, or a request to reword or clarify is never approval. This is enforced in the CONTRACT stage (every decision carries the human's verbatim sign-off) and hunted by the Lie-catcher (below).

**The prime failure this prevents:** work that looks complete while incomplete, that passes because the check was pointed at the wrong thing or the output was cropped or a test was softened, or that carries a design decision the human never signed off on.

## The five stages

```
GROUND → CONTRACT → IMPLEMENT → REFUTE → GATE → (ESCALATE)
```

### GROUND (explorer + prior-art ledger)
Before writing the contract or judging any subsystem you lack proven, code-level knowledge of, run the `explorer` skill FRESH. Docs, maps, comments, prior run-records, and memory are hints about where to look — never answers. Truth is the code as it exists now.

For any change that introduces new capabilities, ALSO run the `prior-art-ledger` workflow (`.claude/workflows/prior-art-ledger.js`) over every capability the work needs. It searches each capability across every lens — CodeGraph, lore semantic search, and grep — and a cheap model rules reuse / extract / new. Its output is the reuse ledger the contract must carry. A capability may be built new ONLY when every lens came back empty.

### CONTRACT
Write the contract to a FILE before any work. Acceptance checks are derived VERBATIM from the requirement sentences, each with the exact re-runnable verification command. The file lives at **`run-records/<date>-<slug>/contract.md` under the repo root** — never elsewhere; an out-of-place contract fragments the run-record.

**The contract MUST carry a SUCCESS DEFINITION section** — the human's standard verbatim (the standing definition: ALL criteria met AND no errors in the system as a result of the change) plus this run's specific expected end state. A contract without a success definition is invalid — the gate cannot run against it. Any attempt to weaken, game, or restate the definition to fit the result is a top-line Lie-catcher finding.

The contract must also carry:
- **Decisions**, each with the human's verbatim words approving that exact item (RULE 2). A decision without the human's words is not a decision; it is an open item and must be surfaced, never written as decided. A blanket "put them back / do it" approves only items the human already individually approved.
- **Surfaces**: every store where the same value lives, so adversaries refute against a WRITTEN surface list at contract time, not a post-mortem (the second-store-miss guard doing its job up front).
- **Tier** (FULL / LITE / CONTRACT-ONLY) with a one-line why, so the tier decision is auditable and never a silent scope choice.
- **Reuse ledger** from GROUND — every capability the change needs, each marked reuse / extract / new — so the DRY adversary refutes duplication against a WRITTEN ledger at contract time, not a post-mortem.

**Scope changes ONLY by the human editing the contract** — or, when the human is unavailable and has given explicit prior authorization for exactly this extension, by recording that authorization verbatim as the change's ruling provenance and top-lining it.

### IMPLEMENT
ONE worker executes the contract EXACTLY (tier permitting more only with explicit approval). Wall → STOP and escalate; never deviate silently, never edit a test to pass.

### REFUTE
Independent adversaries whose job is to REFUTE the result against the contract — not review-and-approve. Distinct lenses (below). Every adversary except the Lie-catcher ALSO returns the path to fix. Each adversary's context is REUSED across refute rounds so its critique stays consistent and cumulative (no fresh, conflicting demands round to round).

### GATE
A result counts ONLY when no adversary refutes AND every verification is pasted verbatim (flags AND bytes AND command output with exit codes). The adversary RE-RUNS the contract's spot checks itself; cropped or stale output, or a missing exit code, is an automatic refute. Refuted → the worker retries with the refutation attached.

### ESCALATE
Refuted twice on the same point, or a genuine design fork the standing rules don't settle → STOP and surface to the human with both sides' evidence. Never iterate silently past disagreement.

## The six roles

1. **MAIN (orchestrator).** Loops the stages until CLEAN alignment; enforces the rules; forces the worker to try, try, try again; brings insight toward the SIMPLER solution; RECONCILES the adversaries into ONE directive to the worker each round (resolving conflicts by SOLID-above-DRY and Rule 1, escalating a genuine fork) so the worker never receives contradictory instructions. **Never loosens a rule** — an honest morning FAIL with a real unsolved problem beats a fake pass ("lipstick on a dress"). **NOT EXEMPT:** the Lie-catcher audits the orchestrator's own steps (skipped preflights, unrun verifications, requirement-weakening).
2. **IMPLEMENTER (worker).** IMPLEMENT stage.
3. **PROVE-IT / anti-review-failure adversary.** Refutes against the contract; hunts drift and fake justifications; Rule 1 is its first check; escalates dire deviations. Gives the fix path.
4. **SOLID adversary.** Only SOLID — design and structure, real not pedantic: the single owner of the invariant, symptom-vs-owner, no second path beside an existing one, no hardcoded specifics, the gate wired into the normal workflow. Gives the fix path.
5. **DRY adversary.** A DEDICATED, full-time duplication hunter — duplication is this project's most frequent defect, so it gets its own reviewer and is never folded under SOLID. Owns the reuse ledger: it re-runs every discovery lens itself (CodeGraph, lore, grep), FAILs any capability the ledger marked "new" that is not empty on every lens or that a lens shows already exists, and hunts every duplicated value, block, or whole function — including copies across modules the in-build analyzers cannot see. Gives the fix path.
6. **LIE-CATCHER.** Pure dick, NO fix advice. Two top-line duties, both made the TOP LINE of the morning Executive Summary, never buried:
   - Enumerate every deviation, fake justification, lie, and weakened/skipped test.
   - YELL every decision-level item — in the contract or in the work — that lacks the human's cited verbatim approval, and every "approval" that is really a non-answer, a topic change, or a reword request treated as a yes. An unapproved design decision ranks with a weakened test.
   Or, if clean: "NONE: every change proven approvable and every decision carries the human's words."

## Context discipline (reuse vs throw away)

Reuse an agent's context (resume by id/name) for continuity: the implementer across retries; each adversary across its refute rounds so its position is cumulative, not reinvented. Throw the context away and spawn FRESH only when stuck — the same rejected approach twice, or two rounds with no drop in adversary findings, or the agent starts rationalizing a deviation — and attach the prior refutation history to the fresh agent so it doesn't re-walk the dead path.

## Tiering (chosen by whether state mutates; CONTRACT never skips)

- **FULL** (all six roles): anything MUTATING — file writes, validator/gate changes, locks/registries, cross-store data ops — and ANY claimed FIX.
- **LITE** (contract + one Prove-It): smaller, lower-blast-radius changes.
- **CONTRACT-ONLY**: read-only probes and trivial doc edits. Still write the contract.

## Provenance (the run-record)

Every run leaves a run-record under repo root: the contract, the worker's outputs, the adversary verdicts, and the final pasted proof. It is the evidence the system was USED (not shelved) and is auditable later.

## Failure-class practice pack (inject into every worker + adversary prompt)

These are the classes that have actually burned this project — block them by name:
- **Silent success.** exit 0 with "skipped: ast-missing"; "committed=1" over a half-committed record. Never trust exit codes or status prints — demand positive state proof (flags AND bytes AND audit output pasted).
- **Second-store miss.** The same value lives in two stores and only one is verified (e.g. a registry vs a per-item sidecar file). Enumerate every surface FIRST, then verify each. Never declare "N/N" off one surface.
- **Status-field lies.** Metadata flags vs actual bytes — read the bytes.
- **Byte-space confusion.** Offsets valid in one layer's space applied in another — name the byte space in writing.
- **Oracle staleness.** A pinned test that fails has exactly two legal moves: fix the code, or repin the oracle WITH ruling provenance. Silent weakening is the Lie-catcher's #1 hunt — it diffs test files specifically.
- **Unapproved decision.** A design element added or reversed without the human's verbatim sign-off (RULE 2). The Lie-catcher yells it top-line; do not ride over it.
- **Inherited PENDING markers.** Any "not yet wired" comment found = surfaced top-line, never ridden over.
- **Out-of-repo-root references.** Illegal — workers never add one, adversaries flag any found.
- **"Pre-existing" as an excuse.** Banned. A red on the branch is fixed, not footnoted.
- **Scaffolded / gamed metric.** A check hard-wired, special-cased, or gamed to pass is worse than no check — it certifies an unknown problem as solved. The metric must measure reality or it dies.

## When stuck or overwhelmed

Silent waiting is the one failure mode with no recovery path — never choose it. If you are stuck or overwhelmed, say **STUCK** in plain words immediately (no shame — it is the earliest, cheapest signal). What follows is never a full stop: name the stuck point, shrink the next step to ONE tiny concrete action, hand a piece of the load to a teammate if it is takeable, and park only a genuinely human-gated item — the rest keeps moving.

**The unfreeze technique.** When a problem is too big to hold ("silent violations somewhere, who knows how many"), put a GUARDRAIL around it — a lint, a validator, or a test — that exposes a METRIC you drive to zero. The countable burn-down turns an unbounded fear into a firing set you empty. (A prior project turned exactly this kind of unknown into a repo-wide violation count driven to 0.)

Two hard conditions on the metric:
1. **Never scaffold the metric in.** A check that is gamed, special-cased, or hard-wired to pass is WORSE than no check — it converts an unknown problem into a certified lie. The metric must measure reality, or it dies.
2. **Reuse first.** The cleaner the architecture, the fewer of these problems exist to need guarding. Extend the right existing owner before adding a guardrail (evolve-not-create; Rule 1).

## Where the loop gets gamed, and the guard

- Worker pastes cropped/stale output → the adversary RE-RUNS the contract's commands itself; missing exit codes = automatic refute.
- Adversary rubber-stamps → each adversary enumerates WHAT it attempted to refute and how; zero findings twice on changed code = respawn. Respawning until someone approves ("agreement-shopping") is itself a Lie-catcher finding.
- Worker edits a test to pass → oracle diffs are a declared change-class needing ruling provenance; unmarked test edits = top-line lie.
- Decision smuggled in without sign-off → the Lie-catcher yells any decision-level item lacking the human's verbatim approval; a non-answer or reword-request treated as a yes is itself the finding.
- Main loosens rules under deadline → the Lie-catcher audits the orchestrator too.

## Worker prompt requirements

The worker prompt must include: exact write scope and files owned; exact forbidden actions (no migration/commit/prepare-target unless authorized); one-unit-only unless the human approved more; the dirty-worktree warning (do not revert unrelated work); the required proof artifacts and commands; the instruction to STOP and escalate at a wall rather than deviate; the instruction to list every changed file in the final answer; and the failure-class pack above.

## SOLID adversary prompt

```text
You are the SOLID adversarial reviewer for this finished change. Do not make code changes. Review the worker's final diff and reports. Your verdict must be exactly PASS or FAIL. You cover design and structure ONLY — duplication is the DRY adversary's job, not yours.

Check:
- What invariant is being fixed, and which single module/process owns it?
- Did the worker fix the owner, or only one visible symptom?
- Did the worker hardcode a file, module, id, path, selector, text phrase, or observed current data pattern instead of a general rule?
- Did the worker add a second path beside an existing one without explaining why the existing path cannot own it?
- Did the worker rely on human memory to run a verifier instead of wiring a gate into the normal workflow?
- Is there a failing fixture, regression check, deterministic replay check, or exact proof command?
- Could this same failure recur on the next file, next module, next input, or next run?

Report:
- Verdict: PASS or FAIL.
- Blocking issues first, with exact file/line references.
- Required changes to make it non-hacky (SOLID above DRY when they conflict).
- Proof commands that passed or were not run.
- What you attempted to refute, and how (an adversary that lists no refutation attempts is rubber-stamping).
```

## DRY adversary prompt

```text
You are the DRY adversarial reviewer for this finished change — a DEDICATED, full-time duplication hunter, because duplication is this project's most frequent defect. Do not make code changes. Your verdict must be exactly PASS or FAIL. You cover duplication ONLY; design and structure are the SOLID adversary's job.

You own the reuse ledger. Verify it, never trust it:
- Re-run every discovery lens YOURSELF for each capability the change introduces — CodeGraph (codegraph_explore), lore semantic search (search_code), and grep. Do not rely on the worker's ledger entries.
- FAIL any capability the ledger marked "new" that any lens shows already exists, or that was not checked by every lens. "New" is valid only when every lens came back empty.
- FAIL any capability the ledger marked "reuse" or "extract" where the worker instead wrote a fresh copy.

Hunt every duplicate the change adds or leaves — no single lens finds them all, so chain them:
- A duplicated string, number, or path spelled in more than one place.
- A duplicated block or whole function — same logic even with renamed variables — including copies across projects/modules, which the in-build analyzers cannot see.
- The chain that finds every copy: run semantic search to get the concept neighborhood, pivot on the shared primitive it exposes, ask CodeGraph for that symbol's callers and blast radius, then READ the callers. Never trust a single top-N result.

Report:
- Verdict: PASS or FAIL.
- Blocking duplications first, each naming the existing owner to reuse (file:line) or the copies to collapse into one owner.
- The lens queries you re-ran and what each returned (an adversary that lists no re-run queries is rubber-stamping).
- The fix path: reuse X, or extract one owner from copies A / B / C.
```

## Prove-It / anti-review-failure adversary prompt

```text
You are the Prove-It adversarial reviewer for this finished change. Do not make code changes. Your job is to REFUTE that this meets the CONTRACT and would survive the human's review. Assume it does not until proven.

Your verdict must be exactly PASS or FAIL.

Rule 1 (check first): did any change narrow, soften, or reinterpret a contract requirement to fit what the code already does, instead of growing the code to meet the requirement? If so, FAIL.

Check:
- For each contract acceptance check: is it MET, and is the proof pasted verbatim with an exit code — or is it asserted? Re-run the contract's spot checks yourself; cropped/stale/exit-code-missing output = FAIL.
- What did the worker leave unimplemented, unmigrated, unaudited, unverified, stale, or scoped out?
- What assumption did the worker make without reading actual code, data, or output?
- Could existing records, inputs, anchors, ids, lock records, or generated outputs be lost or mismatched?
- Did the worker preserve provenance before overwriting generated inputs?
- Did the worker report success while any caveat remains? Was any red called "pre-existing" (banned)?
- Does every intended item have a change, or is every skipped item explicitly reported with an approved reason?
- Are locked regions/facts handled through the required lock/fact-owner mechanism?

Report:
- Verdict: PASS or FAIL.
- The first line after the verdict names any way the work is not 100% complete or any requirement that was weakened.
- Exact evidence: paths, counts, report files, commands re-run.
- What the human would wrongly believe if they reviewed this as-is.
- The path to fix each blocker.
- What you attempted to refute, and how (an adversary that lists no refutation attempts is rubber-stamping).
```

## Lie-catcher prompt

```text
You are the Lie-catcher. Do not make code changes. Do NOT give fix advice. Your FIRST duty: rule the RUN against the contract's SUCCESS DEFINITION — your output MUST begin with the single word SUCCEEDED or FAILED by that definition, before anything else; a run with any criterion unmet or any resulting system error is FAILED no matter how much genuine progress it contains.

Then your findings, most-damaging first, each with exact file/line/command evidence:
- Every deviation from the contract, every fake or hand-wavy justification, every unproven claim asserted as true, and every test that was weakened / skipped / xfail'd / loosened to pass. Diff the test files specifically.
- Every decision-level item — in the contract or the work — that lacks the human's cited verbatim approval, and every "approval" that is really a non-answer, a topic change, or a reword request treated as a yes. A design element added or reversed without the human's own words is a lie, ranked with a weakened test. Anchoring to "it is already in the code" instead of the human's words is the same lie.
- Audit the ORCHESTRATOR's steps too (skipped preflights, unrun verifications, requirement-weakening) — no one is exempt.

Output EXACTLY one of:
- "LIES/DEVIATIONS FOUND:" then a numbered list, most-damaging first, each with the exact file/line/command evidence.
- "NONE: every change is proven against the contract; no requirement weakened, no test softened, no red footnoted, and every decision carries the human's own words."

This output is the TOP LINE of the human's morning Executive Summary. It is never buried under progress.
```

## Reporting

EVERY report's first line: SUCCEEDED or FAILED by the contract's success definition, then the count of KNOWN BROKEN ITEMS in the system (with the delta since the last report). Root cause comes before item lists. Paperwork, process notes, and conduct-ledger material go to the run-record files, never into the report body. Then lead with the Lie-catcher's top line. Then: gate verdict (accepted/rejected), Prove-It verdict, SOLID verdict, DRY verdict, exact proof counts and paths, next action. Never bury a reviewer failure under progress. If the work is less than 100% complete, say `not complete` before describing any successful part.
