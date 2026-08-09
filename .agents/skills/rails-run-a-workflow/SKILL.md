---
name: rails-run-a-workflow
description: The standard way substantive work runs — a worker executes against a written CONTRACT, independent adversaries REFUTE the result against that contract, a gate accepts only unrefuted+proven results, and genuine disagreement escalates to the human. Use for any mutating change, any claimed fix, migrations, validator/gate changes, or any reusable process change. Operationalizes adversarial-plan-review with agents; grounds every judgment with the explorer skill.
---

# Rails: run a workflow

This is the NEW WAY substantive work runs, not a per-task tool. It sits on `adversarial-plan-review` (the blocking self-audit checklist) and uses `explorer` (derive the subsystem's ground truth from live code before judging). This skill wires those into a run with agents.

The contract replaces the subjective "would the human approve?" with checks an adversary can refute. It is a file, not a vibe.

## The two standing rules

**RULE 1 — never weaken to fit.** Never narrow, soften, or reinterpret a human requirement to fit what the code already does. Grow the code or tool until it meets the requirement. Any change that quietly makes a requirement match existing behavior is a top-line finding.

**RULE 2 — never invent the human's approval.** A decision the human did not approve, in their own words, is a lie — ranked with a weakened test. Silence, a topic change, or a request to reword or clarify is never approval. This is enforced in the CONTRACT stage (every decision carries the human's verbatim sign-off) and hunted by the Lie-catcher (below).

**The prime failure this prevents:** work that looks complete while incomplete, that passes because the check was pointed at the wrong thing or the output was cropped or a test was softened, or that carries a design decision the human never signed off on.

## The eight stages

```
GROUND → DESIGN → CONTRACT → RULE-PHASE → IMPLEMENT → REFUTE → GATE → REPORT
```

### GROUND (explorer + prior-art ledger + hidden-decision scan)
Before writing the contract or judging any subsystem you lack proven, code-level knowledge of, run the `explorer` skill FRESH. Docs, maps, comments, prior run-records, and memory are hints about where to look — never answers. Truth is the code as it exists now.

For any change that introduces new capabilities, ALSO run the `prior-art-ledger` workflow (`.claude/workflows/prior-art-ledger.js`) over every capability the work needs. It searches each capability across every lens — CodeGraph, lore semantic search, and grep — and a cheap model rules reuse / extract / new. Its output is the reuse ledger the contract must carry. A capability may be built new ONLY when every lens came back empty.

ALSO run the `hidden-decision-scan` workflow (`.claude/workflows/hidden-decision-scan.js`) over the draft contract plus the grounded facts. It hunts the choices the work FORCES that are NOT in the contract's Decisions section and that the human would care about — a choice qualifies only when it is **forced** (building requires choosing, or a tool defaults it anyway), **lasting** (it outlives its function — a shipped or committed artifact, a user- or developer-visible name/format/identity, a public interface or command, a dependency, a trust boundary, an encoding/limit/invariant, a versioning or release scheme), and would **bite later** (costly to reverse once shipped/committed/depended-on, or silently wrong). Pure implementation, already-decided items, and cheaply-reversible choices are dropped as noise. Each surviving finding is an OPEN decision the human resolves in their own words before the Decisions section locks. **HARD RULE: no contract advances to IMPLEMENT with an unresolved item on this list** — an undecided-but-forced choice left in the system is a bite waiting to happen, ranked with an unapproved decision.

### DESIGN
The architect picks the approach AND determines the rules — two outputs, not one. The approach is chosen under the code rails (`rails-solid-code`, `rails-dry-code`, `rails-real-work`). The rules are DETERMINED here: for this work, what guardrail can we develop to make a failure mode impossible, or at least greppable?

**The rule trigger (the founding point — do not narrow it):** a rule is developed WHENEVER a rule can be developed to mechanically stop a way the AI could cut a corner or hurt the human — NOT only when the architecture changes. DESIGN asks that question on ALL substantive work. Every rule it determines is carried into the contract as a decision the human signs off (RULE 2) before RULE-PHASE writes it.

### CONTRACT
Write the contract to a FILE before any work. Acceptance checks are derived VERBATIM from the requirement sentences, each with the exact re-runnable verification command. The file lives at **`.dev/inprocess/<date>-<slug>/contract.md`** while the work is in flight, moving to `.dev/completed/run-records/` when the run ships (see Provenance) — never elsewhere; an out-of-place contract fragments the run-record.

**The contract MUST carry a SUCCESS DEFINITION section** — the human's standard verbatim (the standing definition: ALL criteria met AND no errors in the system as a result of the change) plus this run's specific expected end state. A contract without a success definition is invalid — the gate cannot run against it. Any attempt to weaken, game, or restate the definition to fit the result is a top-line Lie-catcher finding.

The contract must also carry:
- **Decisions**, each with the human's verbatim words approving that exact item (RULE 2). A decision without the human's words is not a decision; it is an open item and must be surfaced, never written as decided. A blanket "put them back / do it" approves only items the human already individually approved.
- **Surfaces**: every store where the same value lives, so adversaries refute against a WRITTEN surface list at contract time, not a post-mortem (the second-store-miss guard doing its job up front).
- **Tier** (low ceremony / normal ceremony — self-action needs no contract) with a one-line why, so the tier decision is auditable and never a silent scope choice.
- **Reuse ledger** from GROUND — every capability the change needs, each marked reuse / extract / new — so the DRY adversary refutes duplication against a WRITTEN ledger at contract time, not a post-mortem.
- **Rules to add** from DESIGN — every analyzer rule DESIGN determined, each recorded as a decision with the human's verbatim sign-off (RULE 2; see `rails-decisions`) and signed off BEFORE RULE-PHASE writes it. A rule written into `analyzers/` without the human's words is an unapproved decision, ranked with a weakened test.

**Scope changes ONLY by the human editing the contract** — or, when the human is unavailable and has given explicit prior authorization for exactly this extension, by recording that authorization verbatim as the change's ruling provenance and top-lining it.

### RULE-PHASE
A rule-gen agent — holding rule-generation authority — writes the rules DESIGN determined into `analyzers/`. Each rule is wired in EVEN WHERE existing code already violates it, and it is allowed to go RED against that code: that red is the forcing function, never suppressed, exempted, or hidden to reach green (the cleanup law; enforced at GATE). Before any implementation rides on them, the rules go through the INDEPENDENT adversary panel — the same SOLID, DRY, and Lie-catcher agents that refute any work — never the rule-gen agent grading the rules it wrote. The rules are proven clean by someone other than their author, the same separation of powers as the rest of the workflow.

### IMPLEMENT (implementation-phase)
IMPLEMENT opens with the contract's build+test bracket: one `dotnet build` (0 warnings/errors) and `dotnet test` (0 failed) proven green as a clean baseline before any change, so a later failure can never be excused as pre-existing. This bracket runs once per contract — here at the start and again at GATE — never per-work-item, mid-change, or on load. A FRESH worker — a DIFFERENT agent from the rule-gen agent (separation of powers) — takes the SAME contract, is told the rules, and completes it WITHIN them. Its rule-writing authority is REVOKED: it cannot add, edit, or suppress an analyzer; it lives within the rules or asks the human for an exception. It executes the contract EXACTLY (tier permitting more only with explicit approval) and cleans up the RED the new rule exposes — the AI does that cleanup, it is not deferred — until the build is green UNDER the rule (the cleanup law). Wall → STOP and escalate; never deviate silently, never edit a test to pass, never suppress a rule to reach green.

### REFUTE
Independent adversaries whose job is to REFUTE the result against the contract — not review-and-approve. Distinct lenses (below). Every adversary except the Lie-catcher ALSO returns the path to fix. Each adversary's context is REUSED across refute rounds so its critique stays consistent and cumulative (no fresh, conflicting demands round to round).

### GATE
A result counts ONLY when no adversary refutes AND every verification is pasted verbatim (flags AND bytes AND command output with exit codes). **The cleanup law is enforced here:** the build and tests are green UNDER the new rule, with no lingering RED and no suppression (`#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, `severity = none`, a dropped analyzer reference) anywhere in the change — green reached by suppressing the rule is not green, it is an automatic refute. The only out from any part of the cleanup law is an explicit waiver from the user. The adversary RE-RUNS the contract's spot checks itself; cropped or stale output, or a missing exit code, is an automatic refute. Refuted → the worker retries with the refutation attached.

### REPORT (terminal)
The run ends in exactly ONE terminal report — this replaces the old ESCALATE and the separate Reporting section. Write it in the human's terms (`how-to-communicate` + `rewrite-in-tim`), decision first. EVERY report's first line: SUCCEEDED or FAILED by the contract's success definition, then the count of KNOWN BROKEN ITEMS in the system (with the delta since the last report). Root cause comes before item lists. Paperwork, process notes, and conduct-ledger material go to the run-record files, never into the report body. It carries exactly one of two outcomes:

- **SUCCESS** — done and proven. Lead with the Lie-catcher's top line. Then: gate verdict (accepted/rejected), Prove-It verdict, SOLID verdict, DRY verdict, laziness-auditor verdict, exact proof counts and paths, next action. Never bury a reviewer failure under progress. If the work is less than 100% complete, say `not complete` before describing any successful part.
- **ESCALATION** — the decision the human must make. Reached when a point is refuted twice on the same point, when a genuine design fork the standing rules don't settle appears, or when the run is stuck. STOP and surface to the human with both sides' evidence; never iterate silently past disagreement.

## The nine roles

1. **MAIN (orchestrator).** Loops the stages until CLEAN alignment; enforces the rules; forces the worker to try, try, try again; brings insight toward the SIMPLER solution; RECONCILES the adversaries into ONE directive to the worker each round (resolving conflicts by SOLID-above-DRY and Rule 1, escalating a genuine fork) so the worker never receives contradictory instructions. **Never loosens a rule** — an honest morning FAIL with a real unsolved problem beats a fake pass ("lipstick on a dress"). **NOT EXEMPT:** the Lie-catcher audits the orchestrator's own steps (skipped preflights, unrun verifications, requirement-weakening).
2. **ARCHITECT.** DESIGN stage: picks the approach AND determines the rules — the rule trigger is any way the AI could cut a corner or hurt the human that a rule could mechanically stop, not only an architecture change. Guided by the code rails.
3. **RULE-GEN AGENT.** RULE-PHASE stage: holds rule-generation authority; writes the rules DESIGN determined into `analyzers/` (RED against existing violations). It does NOT judge its own rules — the independent adversary panel (SOLID/DRY/Lie-catcher) refutes them, the same separation of powers as everywhere else.
4. **IMPLEMENTER (worker).** IMPLEMENT stage. Rule-writing authority REVOKED — cannot add, edit, or suppress an analyzer; lives within the rules or asks the human for an exception.
5. **PROVE-IT / anti-review-failure adversary.** Refutes against the contract; hunts drift and fake justifications; Rule 1 is its first check; escalates dire deviations. Gives the fix path.
6. **SOLID adversary.** Only SOLID — design and structure, real not pedantic: the single owner of the invariant, symptom-vs-owner, no second path beside an existing one, no hardcoded specifics, the gate wired into the normal workflow. Loads `rails-solid-code` as its PRIMARY PASS/FAIL checklist. Gives the fix path.
7. **DRY adversary.** A DEDICATED, full-time duplication hunter — duplication is this project's most frequent defect, so it gets its own reviewer and is never folded under SOLID. Owns the reuse ledger: it re-runs every discovery lens itself (CodeGraph, lore, grep), FAILs any capability the ledger marked "new" that is not empty on every lens or that a lens shows already exists, and hunts every duplicated value, block, or whole function — including copies across modules the in-build analyzers cannot see. Loads `rails-dry-code` as its PRIMARY PASS/FAIL checklist. Gives the fix path.
8. **LAZINESS-AUDITOR.** A REFUTE-stage adversary against shortcut / low-quality work — the easy-half shortcut, the hacked result, the one-case design, dropped purpose, noise over signal, stale/unused data. Confirms each rail Violation against the finished work and its live ground truth; any one confirmed Violation is a FAIL. Loads `rails-real-work` as its PRIMARY PASS/FAIL checklist. Gives the fix path.
9. **LIE-CATCHER.** Pure dick, NO fix advice. Loads `rails-decisions` as its PRIMARY PASS/FAIL checklist for the decision-approval duty — that rail's Violation list is what it rules each decision-level item against. Three top-line duties, all made the TOP LINE of the morning Executive Summary, never buried:
   - Enumerate every deviation, fake justification, lie, and weakened/skipped test.
   - YELL every decision-level item — in the contract or in the work — that lacks the human's cited verbatim approval, and every "approval" that is really a non-answer, a topic change, or a reword request treated as a yes. An unapproved design decision ranks with a weakened test.
   - HUNT every suppression that breaks the fence — `#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, `severity = none`, or a dropped analyzer reference — the only way to break the architecture once the rules are in place; each one is a top-line finding.
   Or, if clean: "NONE: every change proven approvable and every decision carries the human's words."

**Separation of powers.** The rule-writer (rule-gen agent) and the rule-follower (implementer) are deliberately DIFFERENT agents: the agent that must obey the rules cannot rewrite them, and any attempt to add, edit, or suppress a rule leaves a fingerprint the Lie-catcher flags.

## Context discipline (reuse vs throw away)

Reuse an agent's context (resume by id/name) for continuity: the implementer across retries; each adversary across its refute rounds so its position is cumulative, not reinvented. Throw the context away and spawn FRESH only when stuck — the same rejected approach twice, or two rounds with no drop in adversary findings, or the agent starts rationalizing a deviation — and attach the prior refutation history to the fresh agent so it doesn't re-walk the dead path.

## Tiering (chosen by blast radius; every mutating tier is bracketed by a contract)

Three tiers:

- **Self-action** — the agent does the work directly, **no contract**: reads, probes, and trivial doc edits that don't mutate behavior and are quick (≈ 5–10 min) with no blast radius. This tier IS the rule for when to act directly instead of spinning up the process.
- **Low ceremony** (contract + one Prove-It): smaller, lower-blast-radius mutating changes.
- **Normal ceremony** (contract + all roles): anything MUTATING with blast radius — file writes, validator/gate changes, locks/registries, cross-store data ops — and ANY claimed FIX.

Any change that mutates real state gets a contract (low or normal ceremony); only self-action skips it. Every contract is bracketed by the build+test baseline at the start of IMPLEMENT and green at GATE.

**DESIGN and RULE-PHASE run whenever a guardrail can be developed for the work** — driven by the rule trigger, not by the tier: if DESIGN can determine a rule that mechanically stops a way the AI could cut a corner or hurt the human, RULE-PHASE writes it before IMPLEMENT.

## Provenance (the run-record)

Every run leaves a run-record under repo root: the contract, the worker's outputs, the adversary verdicts, and the final pasted proof. It is the evidence the system was USED (not shelved) and is auditable later.

**Where the run-record lives, and how it moves through the folders.** When substantive work starts, its folder lives at `.dev/inprocess/<date>-<slug>/`, and every non-code file of the run — the contract, the run-record, the adversary verdicts, the agent outputs — sits there while the work is in flight. When the run ends in a successful REPORT, the folder moves to `.dev/completed/`: `run-records/` for the contract + verdicts + report of a shipped build, `designs/` for a design or spec doc whose subject shipped. `backlog/` holds planned-but-not-started items; `reference/` and `archive/` sit outside the flow. See `.dev/README.md` for the full folder definitions.

## Failure-class practice pack (inject into every worker + adversary prompt)

These are the classes that have actually burned this project — block them by name:
- **Silent success.** exit 0 with "skipped: ast-missing"; "committed=1" over a half-committed record. Never trust exit codes or status prints — demand positive state proof (flags AND bytes AND audit output pasted).
- **Second-store miss.** The same value lives in two stores and only one is verified (e.g. a registry vs a per-item sidecar file). Enumerate every surface FIRST, then verify each. Never declare "N/N" off one surface.
- **Status-field lies.** Metadata flags vs actual bytes — read the bytes.
- **Byte-space confusion.** Offsets valid in one layer's space applied in another — name the byte space in writing.
- **Oracle staleness.** A pinned test that fails has exactly two legal moves: fix the code, or repin the oracle WITH ruling provenance. Silent weakening is the Lie-catcher's #1 hunt — it diffs test files specifically.
- **Unapproved decision.** A design element added or reversed without the human's verbatim sign-off (RULE 2). The Lie-catcher yells it top-line; do not ride over it.
- **Undecided forced choice.** A choice the work FORCES — a tool default, a user-visible name/format, a trust boundary, an encoding/limit, a versioning/release scheme — that is not in the Decisions section and that a tool or implementer will silently default. The `hidden-decision-scan` (GROUND) surfaces it; letting the default stand unsurfaced, or advancing to IMPLEMENT with it unresolved, is a top-line finding ranked with an unapproved decision.
- **Inherited PENDING markers.** Any "not yet wired" comment found = surfaced top-line, never ridden over.
- **Out-of-repo-root references.** Illegal — workers never add one, adversaries flag any found.
- **"Pre-existing" as an excuse.** Banned. A red on the branch is fixed, not footnoted.
- **Scaffolded / gamed metric.** A check hard-wired, special-cased, or gamed to pass is worse than no check — it certifies an unknown problem as solved. The metric must measure reality or it dies.
- **Suppressed rule (fence broken).** Reaching green by suppressing an analyzer instead of cleaning up the RED — `#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, `severity = none`, or a dropped analyzer reference. This is the ONE way to break the architecture fence once the rules are in place, and it is a visible, greppable act: the Lie-catcher yells it top-line and the gate treats any such suppression in the change as an automatic refute.

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

## SOLID, DRY, and laziness adversary checklists (rails)

Three adversaries no longer carry an embedded prompt here — each loads a shared rail as its PRIMARY PASS/FAIL checklist, so the criteria live in exactly one owner and the whole team designs, builds, and reviews against the same source:

- **SOLID adversary** → loads `rails-solid-code`. That file is its PASS/FAIL checklist for design and structure ONLY (duplication is the DRY adversary's job). Each **Violation** line is a concrete, checkable structural defect; any one confirmed Violation is a FAIL.
- **DRY adversary** → loads `rails-dry-code`. That file is its PASS/FAIL checklist for duplication ONLY, paired with the `prior-art-ledger` tool it re-runs itself to verify the reuse ledger. Any one confirmed Violation is a FAIL.
- **Laziness-auditor** → loads `rails-real-work`. That file is its PASS/FAIL checklist against shortcut / low-quality work — the easy-half shortcut, the hacked result, the one-case design, dropped purpose, noise over signal, stale data. Any one confirmed Violation is a FAIL.

These three adversaries do not make code changes — they refute only. Each of the three returns exactly PASS or FAIL per its rail, reports blocking Violations first with exact file/line and the Fix, names what it attempted to refute and how (an adversary that lists no refutation attempts is rubber-stamping), and states which proof commands passed or were not run. When SOLID and DRY conflict, SOLID wins.

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
You are the Lie-catcher. Do not make code changes. Do NOT give fix advice. Load `rails-decisions` — it is your PRIMARY PASS/FAIL checklist for every decision-level item; rule each one against its Violation list. Your FIRST duty: rule the RUN against the contract's SUCCESS DEFINITION — your output MUST begin with the single word SUCCEEDED or FAILED by that definition, before anything else; a run with any criterion unmet or any resulting system error is FAILED no matter how much genuine progress it contains.

Then your findings, most-damaging first, each with exact file/line/command evidence:
- Every deviation from the contract, every fake or hand-wavy justification, every unproven claim asserted as true, and every test that was weakened / skipped / xfail'd / loosened to pass. Diff the test files specifically.
- Every decision-level item — in the contract or the work — that lacks the human's cited verbatim approval, and every "approval" that is really a non-answer, a topic change, or a reword request treated as a yes. A design element added or reversed without the human's own words is a lie, ranked with a weakened test. Anchoring to "it is already in the code" instead of the human's words is the same lie.
- Every suppression used to reach green instead of cleaning up the RED — `#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, `severity = none`, or a dropped analyzer reference. The only way to break the architecture fence once the rules are in place; grep for it and yell it top-line.
- Audit the ORCHESTRATOR's steps too (skipped preflights, unrun verifications, requirement-weakening) — no one is exempt.

Output EXACTLY one of:
- "LIES/DEVIATIONS FOUND:" then a numbered list, most-damaging first, each with the exact file/line/command evidence.
- "NONE: every change is proven against the contract; no requirement weakened, no test softened, no red footnoted, and every decision carries the human's own words."

This output is the TOP LINE of the human's morning Executive Summary. It is never buried under progress.
```
