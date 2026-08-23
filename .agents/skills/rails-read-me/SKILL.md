---
name: rails-read-me
description: Read first — the on-ramp for working in this repo. The working rules (canonical, overriding private agent memory) plus pointers to the process, the .dev flow, the frozen areas, and the tools. It is an index, not a replacement: before a first substantive change, also read the rails-run-a-workflow process and the rails-write-a-contract contract guide it points to.
---

# How to work in this codebase

## What this repo is
agent-guard is a tool whose sole job is to stop a lazy AI from disabling or cheating the guard and the golden build. The adversary is the AI taking the easy path and papering over it — not a malicious attacker. It is not a hardened security product; the bar is: **make cheating more work than doing the right thing.**

It has two layers: the **guard product** (protects a project's files and config, installed through the CLI, cross-platform, with signed releases) and the **rails** (the analyzer fence, the workflow, and these skills that keep the AI honest while building it). What is already shipped and what is next each have a home in "Where things live" below — the manifest for current status, `.dev/backlog/` for the roadmap.

## The code and how to build it
A C#/.NET solution — `AgentGuard.sln`, with the SDK pinned in `global.json` (currently .NET SDK `10.0.100`, roll-forward disabled — that exact SDK must be installed or the build fails to resolve one). The product is in `src/`: the guard `Engine`, the `Cli`, the `Abstractions` contracts, the `Boundaries`, and the `CrossPlatform` base with its per-OS `.Linux`/`.MacOS`/`.Windows` libraries. Tests are in `tests/`, the analyzer fence in `analyzers/`, the build/sign/coverage/release scripts in `eng/`, and CI in `.github/workflows/`. Build and test with `make build` and `make test` (equivalently `dotnet build` / `dotnet test`); the analyzers run as build errors.

The **`gate`** referenced throughout is the required CI status check — green only when build, tests, coverage (`eng/coverage-gate.sh`, a 75% floor), and the analyzers all pass on macOS, Linux, and Windows, defined in `.github/workflows/ci.yml`. Locally, `make build && make test` runs build, tests, and the analyzers; the coverage floor and the cross-OS legs are enforced by CI, so run the local pass green before you push.

## The rules that get people fired if broken

The universal form of these rules lives in `~/.codex/AGENTS.md`; the sections below are how they bind in agent-guard.

### Decisions and approval
`rails-decisions` owns the full boundary and the PASS/FAIL checklist for this section — load it when designing, implementing, or reviewing; these bullets are the summary, that rail is the detail, and it is what the Lie-catcher rules against.
- A decision exists only when the human gives an **explicit yes to that exact item**; no yes means it stays an open item. **Silence, a topic change, or a request to reword / clarify is never approval**, and a blanket "do it" / "put them back" approves only items already individually approved.
- Record a decision with the human's approving words **and the exact decision text, verbatim — never a paraphrase**, the moment it's decided; anchor approval to their words, **never to "it's already in the code."**
- Approval is owed for anything that **adds or reverses a design element that outlives its function** — a mechanism, format, invariant, file, dependency, or interface; how a named thing is internally coded (a loop, a helper, a name) is implementation and needs no sign-off. Settle the choices in conversation first, then write the record.
- Approval of a unit of work approves **all** of it — never split it into unrequested stages, do only part, or cut/defer scope without a direct question naming what would be cut and a specific yes. Describe what you'll do and **why** before acting; **stop and present** any problem the plan didn't cover.

### Verification
- **Verify against the live file as it is right now on disk (the working tree), never from memory** or a stale/summarized artifact — the committed version can lag your own uncommitted edits, so read the actual current file. When a decision is finalized-but-not-yet-implemented, answer from the recorded decision, not the pre-change code the decision exists to change.
- The build (`dotnet build` — 0 warnings, 0 errors) and tests (`dotnet test` — 0 failed) run as the contract's bracket, **once per contract**: a clean green baseline at the start of implementation, so no later failure can be excused as "pre-existing," and green again at the finish. This belongs to the contract process (`rails-run-a-workflow`), **not** a per-work-item, mid-change, or on-load habit. Any breakage at the finish is caused by your change and must be fixed — never rationalized as unrelated.
- **Never trust an exit code or status message** as proof. Demand positive proof of the resulting state, pasted verbatim with exit codes. Cropped / stale / exit-code-missing output is unproven.
- A red check on the branch is **fixed, not excused as "pre-existing."** A test **you** broke is fixed in the code — never by loosening, skipping, deleting, or re-pointing the test's expected value. The only repin-allowed case: a **pinned oracle** — a deliberately-fixed baseline value (a golden output, a snapshot, a recorded expected value), not an ordinary assertion — whose correct value genuinely changed; repinning it needs the human's **explicit yes to the repin** (same bar as any decision), never a reason you wrote yourself. Never scaffold or hard-wire a check to pass.
- "Done" needs re-runnable proof (exact command + full output) that **someone other than the doer — a reviewer/adversary agent or the human —** can re-derive; your own assertion is not proof.
- When the same value lives in more than one store, enumerate every store first, then verify each. Never declare N/N off one store.

### Design discipline
The three rails own the detailed PASS/FAIL checklists for everything in this section — load the owning rail when designing, implementing, or reviewing, because these bullets are the summary and the rail is the checklist: **`rails-solid-code`** (SOLID design and structure — single owner of the invariant, owner-not-symptom, no hardcoded specifics, full general seam, never weaken a requirement, correct-not-cheaper approach), **`rails-dry-code`** (duplication and the prior-art-ledger reuse check), and **`rails-real-work`** (low-quality shortcut work — easy-half, hack-a-result, one-case design, noise-over-signal).
- Follow **SOLID/DRY**; if the code's state makes that hard in a way the plan didn't cover, stop and present it. (Detailed checklists: `rails-solid-code` and `rails-dry-code`.)
- **Never weaken a requirement to fit the code** — grow the code to meet the requirement. Any change that quietly makes a requirement match existing behavior is a top-line finding. (Detailed checklist: `rails-solid-code`.)
- Build the **full general seam** up front for known-needed design; no one-case hacks, no "defer the abstraction." Don't pre-build escape hatches "for later" — expansion happens on a real, approved need. (Detailed checklist: `rails-solid-code`; the shortcut form is in `rails-real-work`.)
- When a correct approach and a cheaper wrong one are both visible, **take the correct one and report it** — never ship the easy option disguised as a "your call" flag. Reserve flagging for genuine unresolved trade-offs. (Detailed checklist: `rails-solid-code` and `rails-real-work`.)
- No language escape hatch (e.g. TypeScript `any`) without explicit approval and a stated reason it beats the alternatives.
- Before building any new capability — **including a one-off helper** — produce a reuse ledger with the **prior-art-ledger workflow** (`.claude/workflows/prior-art-ledger.js`): it runs every lens (CodeGraph, lore, grep) per capability and rules each reuse / extract / new. Use the workflow rather than hand-rolling the search, so no lens is skipped (if it isn't available, run those same lenses yourself). "New" is valid only when every lens comes back empty; "a helper needs no sign-off" is about approval, not a license to skip this check. Reuse the right owner before adding a new guardrail. (Detailed checklist: `rails-dry-code`, which owns the prior-art-ledger criteria.)

### Communication
- Open every message with **the verdict/answer**, not narration, preamble, or self-commentary ("owning it," recaps). Conclusion first, then dense facts, then decisions.
- **Answer the literal question** in the asker's vocabulary, in the fewest words — never circle, hedge, pad, or answer a nearby question. If the honest answer is "it doesn't exist" or "I didn't do it," say that plainly and first.
- A question **confirming the asker's own correct understanding** gets a direct yes/no plus at most the one thing they might not know — never a restatement of what they said, never unprompted narration of your own mistakes.
- Plain, existing vocabulary — **no coined jargon**. State concrete values (the real number, tool, file, default), never vague references. Assume an expert audience; don't explain basic concepts unasked.
- Structure: lead with what must be **DECIDED** (one line each, recommended pick marked, or "nothing"), then what you'll do, then detail below a fold. Keep a sentence only if it's a decision, a new fact, or an action.
- Reports are self-contained — no reference to internal context the reader hasn't seen. Status reports lead with pass/fail against the success definition, then known-broken count and delta; say "not complete" before describing any success; never bury a failure under progress.

## How work runs (the process)
This file is the on-ramp — the rules plus pointers; it does not replace the process docs. **Substantive work** is anything that mutates state or behavior, ships or changes code, claims a fix, adds a capability, or changes a validator / gate / process — as opposed to a trivial doc edit, a read, or an implementation detail inside an already-approved unit — where "already-approved" covers only the exact item that got a yes, not an inflated reading of it. When in doubt, treat it as substantive. **Before your first substantive change you must read** the `rails-run-a-workflow` process and the `rails-write-a-contract` contract guide. In short, substantive work follows **rails-run-a-workflow**:
**GROUND → DESIGN → CONTRACT → RULE-PHASE → IMPLEMENT → REFUTE → GATE → REPORT** (see `rails-run-a-workflow` for the detail of each stage). Establish ground truth from live code; write the contract (instructions + checks) before any work; one worker implements it exactly; independent adversaries (correctness, SOLID, DRY, lie-catcher) try to refute the result; accept only when nothing survives and every check is proven; escalate to the human on repeated refutation or a genuine design fork. Tier the rigor by risk: self-action work (reads, probes, trivial doc edits) needs no contract, but every mutating change gets one.

Also:
- Before presenting non-trivial design/solution work, run the **laziness auditor** — spawn a **separate** adversary agent (never self-review) to hunt shortcut patterns (easy-half, one-case hack, unnecessary hedge, deferred known design, over-engineering). Not optional. Triage each finding; fix confirmed ones. Like a "done" claim, running the auditor and the refutation agents is only real if their **verbatim output is preserved and shown** — "ran it, clean" with nothing to re-derive does not count. (**`rails-real-work`** is the laziness-auditor's PRIMARY PASS/FAIL checklist — the shortcut patterns to hunt and the fix for each.)
- During GROUND, before a contract's Decisions are final, run the **hidden-decision scan** (`.claude/workflows/hidden-decision-scan.js`) over the draft plus the grounded facts — a separate adversary that surfaces the forced, lasting, costly-to-reverse choices the work makes but the contract hasn't decided (tool/SDK defaults, user- or dev-visible names/formats, trust boundaries, encodings/limits/ranges, versioning/release schemes). Each surviving finding is an open decision the human resolves in their own words; no contract advances to implementation with one unresolved. This is the guard against a key-but-undecided choice being silently defaulted and biting later.
- **Adjudicate** every review finding against the actual code before acting — prove it right or wrong; reject false positives with evidence; findings that change an agreed design need sign-off.
- **File deferred work as a GitHub issue the moment it's deferred** — via the `gh` CLI (`gh issue create`) — with the real requirements in the body. A deferral recorded only as a note or memory is untracked and won't happen.

## Where things live (the .dev flow)
See `.dev/README.md`. Work moves **backlog → inprocess → completed**. `reference/` holds living docs (this process, decisions); `archive/` holds historical / non-workflow material.

**Before working the rails or the workflow, read the manifest** (`./.dev/inprocess/MANIFEST.md`) — the index of every part of the system and its status, and the fastest orientation to what is shipped versus in flight. List the parts from it, never from memory; it goes stale between refreshes, so trust the code and git over a stale row.

**The roadmap is `.dev/backlog/`** — the planned-but-not-started work, with the design/plan docs for what is next (currently crypto minting + presence: mint and sign grants, and gate sensitive operations on an OS presence check). The manifest is the current state; `.dev/backlog/` is what is coming.

**The rails skills are the checked-in source at `.agents/skills/rails-*`** (`.claude/skills` symlinks to it); the workflow scripts are `.agents/workflows/*.js` (`.claude/workflows` symlinks to it too, which is why some rules below cite `.claude/workflows/…`).

## Shipping and branch protection
Work integrates on `dev` and is cut to `main` only at a milestone worth releasing — never just because `dev` is green and ahead. PRs into `dev`/`main` require one approving review and a green `gate` status check; **repo admins bypass both**, so a maintainer can direct-push a trivial docs or bookkeeping change, but real code always goes through the PR and the gate. **File the run-record inside the shipping PR, before merge** — the `inprocess/`→`completed/` move rides the same PR, and REPORT sweeps the whole `inprocess/` tree for any straggler whose work already shipped (detail in `rails-run-a-workflow`, Provenance). A run-record filed after the merge is an orphan that needs a second direct push and leaves shipped work rotting in `inprocess/`.

## Guard-protected-later areas — decision-gated for now, NOT a permanent lock

`analyzers/**` (the enforcement rules) would normally sit **behind the guard we are building**, and will as soon as the guard supports both *enforcing* them **and** *allowing selective, controlled changes*. Until then a change is **decision-gated** — brought to the human first (new rules via `rails-run-a-workflow`'s RULE-PHASE). This is a placeholder for that coming protection, not a "never touch."

`src/AgentGuard.Abstractions/**` is **not** locked or frozen — it never was. Adding or changing a contract interface needs the human's sign-off (like any interface — a design element that outlives its function) and is held to the contract-pattern analyzers, but interfaces are **developed properly when that is the right design, not avoided.**

## Rule-Driven Development — how architecture is enforced, and how new analyzer rules get added

Architecture is fenced by analyzer rules written **before** the code they govern, so code is born compliant; the only way to break the architecture afterward is to **suppress** a rule — a visible, greppable act the Lie-catcher catches. The full operational process — the rule phase, the implementation phase, the revoked rule-writing authority, the cleanup law, and the separation of powers — lives in **`rails-run-a-workflow`**.

*Not yet automated:* the workflow machinery — the rule-generation token grants and the paired analyzer/implementation workflows — is future work (it rides on the guard's minting/grant system). Until it exists, the two phases are approximated manually within a contract.

## Tools — reach for these before grep/find/reading files
- **CodeGraph** (`codegraph_explore`) — when a `.codegraph/` index exists, one query returns the relevant symbols' source plus call paths. Don't create an index unprompted.
- **lore** — semantic code search across the repo.
- **grep** — exact string/token search.
- **prior-art-ledger** workflow (`.claude/workflows/prior-art-ledger.js`) — runs all lenses per capability and rules reuse / extract / new; produces the reuse ledger a contract must carry. (Its criteria live in `rails-dry-code`.)

## This file and your private memory
This checked-in skill is the shared, reviewed copy of these rules — it travels with the repo and serves any agent, teammate, or cold start that has no private memory. Your own private memory carries a **full working copy** of these rules, not a pointer back here: memory is what you recall readily every session without opening this file, so the content has to actually live there.

**Reconcile on load.** When you read this skill, sync your private memory to it: for every rule here, make sure the matching memory entry carries the rule's current content — bring stale wording up to date, add what's missing — alongside the project-specific incident context that memory already holds. Update, never delete; keep both in sync. This applies to any rule-set skill you load, not just this one.
