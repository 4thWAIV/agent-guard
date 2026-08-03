# Work item: "How to work in this codebase" onboarding skill

## Goal
A checked-in skill that brings a new coding agent — or this agent after a compaction, or a teammate, or a different tool — up to speed on how work is done in this repo, without depending on any one agent's private memory.

## Why
The durable working rules currently live in the agent's private per-project memory (`~/.claude/.../memory/`), which is NOT in the repo. A fresh agent, a different tool, or a teammate gets none of it. This moves the rules into the repo where any agent sees them.

## Home (decided)
`./.agents/skills/how-to-work-here/SKILL.md` — repo-root `.agents/skills/` is not gitignored, so the skill is checked in and travels with the repo. (Tim, 2026-08-02: "it should live in .agents/skills folder of the repo"; "Go, as an inprocess work item".)

## Scope
- The skill states the working rules as IMPERSONAL repo norms (no "Tim reacts to X" framing — safe for teammates/other tools to read).
- It is an on-ramp that points to: the `.dev` flow (`.dev/README.md`), the process (`workflow-with-adversaries/SKILL.md`) and the contract guide, the frozen areas (`Abstractions/`, `analyzers/`), and the discovery tools (CodeGraph, lore, grep, the prior-art-ledger workflow).
- Hard rules to carry: decisions need the human's verbatim sign-off; silence / topic-change / reword is never consent; verify against live committed bytes; SOLID/DRY and stop-and-present on anything unexpected; no `any` without approval; lead with the answer, no babble.

## Cleanup (part of this work, per Tim's #2)
After the skill is written and checked in, delete/reset the memory files it supersedes so there is ONE canonical source (the repo skill), and update `MEMORY.md`. Project-specific state memories stay; the general working-rule memories go.

## Steps
1. GROUND — extract the durable rules from the memory files + reference docs (delegated to a Sonnet agent). DONE.
2. Draft the skill. DONE — `.agents/skills/how-to-work-here/SKILL.md`.
3. Validate: three cold Opus passes (round 1 fixed 6 executability gaps; round 2 all probes/stress-tests CLEAR + 3 refinements; round 3 adversarial closed the self-attestable-gate hole + 2 micro). Plus: "lazy" not "hostile" framing; reuse rule now points to the prior-art-ledger workflow instead of hand-rolled search. DONE.
4. Added the "Reconcile on load" directive to the skill: on reading the skill, update (not delete) any related private memory to match its rules. DONE.
5. **ON WAKEUP (after Tim's compact event) — run the reconciliation, per the skill's Reconcile-on-load directive:** go through the private memory files at `~/.claude/projects/-Users-timothystockstill-code-macos-4thWAIV-agent-guard/memory/`, and for every `feedback-*` / `practice-*` entry whose rule is now in the skill, UPDATE it to match the skill (point it at the skill as canonical / reconcile wording) — do NOT delete. Also scrub "hostile" → "lazy" from the kept project memories. Update `MEMORY.md` accordingly. Then move this folder to `.dev/completed/` and commit (skill + `.dev` reorg + this record) with Tim's go.
6. **THEN the next work item is the crypto-minting + presence foundation** — plan at `.dev/backlog/PLAN-crypto-minting-and-presence.md`. First contract is **1a (signable Apple binary + signing pipeline)**. Ground fresh, write the contract per HOW-TO-WRITE-A-CONTRACT, run it through workflow-with-adversaries.

## Approach on memory: UPDATE, not delete
Correction locked with Tim: the point is NOT to delete redundant memories. Go through memories related to this rule-set and update them to match the rules in the set (reconcile), keeping them as correct reflections of the canonical skill.

## DONE — 2026-08-02 (reconcile executed post-compact)
Tim corrected the direction before execution: memories carry the **full rule content** (for ready recall), not pointers back to the skill; the skill's "This file vs private memory" section was the badly-worded pointer and got rewritten to say so ("This file and your private memory" — content lives in memory, skill is the shared/checked-in source they sync from). Approved verbatim: "Approve. And sync the memories to the skills version."

Executed:
- Rewrote the skill's memory-relationship section; mirrored to the untracked `.claude/skills/how-to-work-here/SKILL.md` runtime copy (byte-identical).
- Folded the skill's cold-pass refinements into 4 memories that lacked them: `feedback-never-change-agreed-design` (existing-in-tree ≠ approval), `feedback-silence-is-never-consent` (blanket "go" covers only already-approved items), `feedback-no-commentary-lead-with-signal` (status-report structure), `practice-run-laziness-auditor` (verbatim-output-or-it-didn't-count).
- 9 rule-memories already carried the content verbatim-equivalent — left as-is (they are the source the skill was distilled from).
- "hostile"→"lazy" scrub: no occurrences existed. No-op.
- Fixed 3 stale reorg paths (`.dev/run-records/` → `.dev/completed/run-records/`) in MEMORY.md + `project-config-protection-open-defects` + `decision-guard-handles-its-config-files`; added an orienting line to MEMORY.md naming the skill as the shared source.

Open (flagged to Tim, not decided):
1. Recall gap — the skill's **Verification** section and **approval-scope test** have no memory, so they don't auto-recall. Candidate: add two memories.
2. Mirror drift — `.claude/skills/how-to-work-here/` is an untracked copy of the checked-in `.agents/` one; can diverge. Candidate: symlink.
