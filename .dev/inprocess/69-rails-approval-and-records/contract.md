# Record and complete the rails changes made without a contract

## Decisions

Each decision below was settled in conversation and approved as written.

1. A recorded decision carries the exact wording the human approved. The orchestrator writes the decision in plain prose, shows it to the human, and records it unchanged once the human approves it as written. A record never carries the human's raw conversational words and never paraphrases the approved wording.

2. Every place in the rails and the workflow scripts that required the human's raw words now requires the approved wording instead — `rails-decisions`, `rails-write-a-contract`, `rails-run-a-workflow`, and the agent instructions inside `architecture.js`, `implement.js`, `refute.js`, `rule-phase.js` and `tdd.js`, including the copied block two of them share.

3. A delegated agent returns its verdict, its counts, its failures, and the structured fields the next stage consumes. It never returns a listing produced only to be read.

4. A mismatch that the next ordinary step erases on its own is never a blocker and never leads a report. It goes below the fold, in its own section, so the human sees it without it competing with what needs them.

5. `.private/` is ignored by git.

6. One folder per work item, created once and never duplicated. When work starts from an existing issue, its folder moves from `.dev/backlog/` to `.dev/inprocess/`. When the human says to do something and no issue exists, the work starts by filing the issue and creating its folder directly in `.dev/inprocess/<issue-number>-<slug>/`, skipping backlog. The folder moves to `.dev/completed/run-records/` when the work ships. The contract and the run records live inside it. A run never creates a second folder. Filing the issue and moving the folder are recordkeeping and never need the human's approval. This applies to every L1 and L2 run; L3 work produces no contract and no run record, so it needs neither.

7. Filing a GitHub issue creates its work folder at `.dev/backlog/<issue-number>-<slug>/`. The folder travels with the work through `backlog`, `inprocess` and `completed`, and is kept for good in whichever of the three the work currently sits in. It holds `issue.md` with the issue body, which the live GitHub issue always supersedes.

8. The prior-authorization exception is deleted everywhere it appears — `rails-write-a-contract`, `rails-run-a-workflow`, and the fix-round instructions inside `architecture.js`, `implement.js`, `rule-phase.js` and `tdd.js`. Scope changes only when the human edits the contract. There is no second path. This is a locked law: it cannot be changed, narrowed, or excepted except by a contract the human approves for that exact change.

9. The `ground-brief` run's work shares this working tree. It is governed by its own contract at `.dev/inprocess/70-ground-brief/contract.md` and is excluded from this contract's judgement. It is exactly this and nothing else: in `ground.js` and `design.js`, the new `workflow-brief` copied block, the `issueNumber`, `inputFolderPath` and `issueScope` inputs, and the brief prepended to every prompt each script builds; in `design.js`, the removal of the `goal` input; the removal of the `goal` field from the `design-stage` result contract inside the `stage-result-contracts` copied block in all nine scripts that carry it; in `ground.js` and `prior-art-ledger.js`, the optional brief added to the `prior-art-ledger` copied block; and in `rails-run-a-workflow` and `rails-read-me`, the documentation of that brief — the GROUND and DESIGN Inputs lines, the GROUND brief paragraph, the launch precondition requiring your approval of `conversation.md`, and the description of the work folder's contents. An adversary verifies each item against that contract. Anything in the tree beyond this list and this contract's own Surfaces is unauthorized and is a finding.

10. This work runs at L2.

11. `rails-read-me` states that the roadmap lives in two places, with two jobs: `.dev/backlog/` holds the design and plan documents, and the GitHub issues hold the tracked work items, their order, and what is still open.

12. A Decision is never filtered. It binds exactly as written — when it says every place, it means every place. The Surfaces section lists where the work is known to be, as an aid to whoever does it and never as a boundary. A place the list misses is still in scope, and a list that disagrees with the tree is the list being wrong. No section of a contract may narrow a Decision.

13. A stage's own record of which checks it ran is a structured field the pipeline requires, and every stage's Outputs line may name it.

14. The sentence forbidding a second path to a scope change has one owner: the `scope-boundary` copied module, shared byte-identically by every script that uses it.

## Rules to add

None — this change adds no analyzer rule.

## The standard / what we're building

The rails and the workflow scripts carry every decision above, consistently, with no passage left contradicting another.

Every decision above is applied in the working tree. Most were applied before this contract existed, which is why it exists: they are recorded here so the adversary panel judges them rather than being told to ignore them.

## Success definition

ALL criteria met AND no errors in the system as a result of the change.

For this run: every decision above is in force in the rails and the workflow scripts; no passage in any rail contradicts another; the prior-authorization exception appears nowhere; and every copied block stays byte-identical across its copies.

## Surfaces

The entries below name where the work is known to be. They document the decisions; they do not limit them. Decision 2 binds every place in the rails and the workflow scripts that required the human's raw words, whether or not this list names it. A place that this list misses is still in scope, and a list that disagrees with the tree is the mismatch.

- `.agents/skills/rails-decisions/SKILL.md`, `rails-write-a-contract/SKILL.md`, `rails-run-a-workflow/SKILL.md`, `rails-read-me/SKILL.md`, `rails-real-work/SKILL.md`
- Every workflow script under `.agents/workflows/` whose agent instructions require the human's raw words, including `architecture.js`, `implement.js`, `refute.js`, `refute-readiness.js`, `rule-phase.js` and `tdd.js`, and the `selected-rule-warnings` copied block shared by `architecture.js` and `tdd.js`.
- This run's work folder `.dev/inprocess/69-rails-approval-and-records/` and everything in it, including `issue.md` and `contract.md`, whose Scope line must carry the wording `rails-write-a-contract` mandates.
- `.dev/inprocess/70-ground-brief/` — the in-flight `ground-brief` run's folder.
- `.agents/workflows/design.js`, `ground.js`, `hidden-decision-scan.js` and `prior-art-ledger.js` — changed only by the in-flight `ground-brief` run and excluded from this contract's judgement by decision 9.
- `.dev/README.md` — where the work folders and their flow are described.
- `.gitignore` — the `.private/` entry
- `.dev/backlog/<issue-number>-<slug>/` — the 55 per-issue folders
- `eng/check-copied-modules.mjs` — enforces byte identity. Not edited; it must stay green.

## Reuse ledger

One capability: a single owner for the sentence forbidding a second path to a scope change.

**extract** — before this change the sentence was hand-typed independently in `architecture.js`, `implement.js`, `rule-phase.js` and `tdd.js`. `grep -rn "the human edits the contract" .agents/workflows/` found those four copies and no owner; CodeGraph returned no shared symbol holding it. Four copies with no owner is an extract, and the copies collapse into the `scope-boundary` copied module, which `eng/check-copied-modules.mjs` holds byte-identical.

Everything else in this change is wording and rules in existing rails and existing agent instructions, and introduces no capability.

## What to do

1. Delete the prior-authorization exception from `rails-write-a-contract` section 13 and from `rails-run-a-workflow`'s Scope paragraph, replacing each with the plain rule that scope changes only when the human edits the contract, and record in both that this is a locked law changeable only by a contract approved for that exact change.

2. Delete the same exception from the fix-round instruction strings in `architecture.js`, `implement.js`, `rule-phase.js` and `tdd.js`, so no agent is told a second path exists.

3. Correct `rails-write-a-contract`'s closing line, which still requires every decision line to carry the human's own words, so it requires the approved and accepted decision text instead.

4. Correct the reporting rule in `rails-real-work` rail 5, which currently drops a self-erasing mismatch entirely, so it files one below the fold in its own section and never treats it as a blocker.

5. Write decision 6, the single work folder, into `rails-run-a-workflow` where the contract's location is defined, and into `rails-read-me` where the work folders are described, replacing the `.dev/inprocess/<date>-<slug>/` convention.

6. Write decision 3, the delegated-agent return rule, and decision 7, the per-issue work folder, into the rails if any passage still contradicts them.

7. Search every file under `.agents/skills/` and `.agents/workflows/` for any remaining place that requires the human's own, raw, or quoted words, and correct each one. `refute-readiness.js` is known to carry two: the instruction that a conversational decision must appear "carrying the human's own words", and the check that the Decisions section authorizes it "in the human's own quoted words".

8. Write decision 12 into `rails-write-a-contract`, so every contract carries it.

9. Correct the delegated-agent return rule in `rails-run-a-workflow` so it states decision 3 as narrowed, and confirm no stage's Outputs line contradicts it. A structured field the next stage consumes is permitted; a listing produced only to be read is not.

10. Give the scope-boundary sentence one owner. "A fix that changes the contract or an approved design is allowed only after the human edits the contract. There is no second path." is spelled byte-identically in `architecture.js`, `implement.js`, `rule-phase.js` and `tdd.js`. Make it a copied module like the other six, enforced by `eng/check-copied-modules.mjs`.

11. Delete the sentence "The next work is the appd daemon, issues #62 through #67." from `rails-read-me`. Decision 11 does not include it, and it contradicts the rule two lines above it that state is derived and never read from a status document.

12. Correct the check count in the readiness stage. `refute-readiness.js` instructs "Run all six checks" but closes by asking for "all four checks", and `rails-run-a-workflow` says the stage runs four checks and lists only four, omitting EXCLUSION and OPEN ITEM. Make all three say six and list all six.

## What the agent MAY do

- Edit anything the Decisions above reach, except `eng/check-copied-modules.mjs`.

## What the agent MUST NOT do

- Edit `eng/check-copied-modules.mjs`, or edit anything the Decisions above do not reach.
- Let any copy of a copied block differ from another by a single byte.
- Leave any passage in any rail contradicting another rail or itself.
- Commit, stage, push, or open a pull request.
- Expand scope. Hit a wall, stop and report.

## Acceptance

1. The prior-authorization exception is gone. `grep -rn "prior authorization" .agents/ .dev/reference/` returns nothing. Paste it with its exit code.

2. `rails-write-a-contract` nowhere requires the human's own words. `grep -n "own words\|verbatim words" .agents/skills/rails-write-a-contract/SKILL.md` returns nothing. Paste it with its exit code.

3. `node eng/check-copied-modules.mjs` exits 0. Paste the command and its full output with the exit code.

4. Every workflow script parses. A workflow script is not valid standalone JavaScript, so the check builds each one through the async function constructor after replacing `export const meta` with `const meta`. Run it for all ten and paste each output with its exit code.

5. No rail contradicts another. An adversary reads every passage in the five rails that concerns recorded decisions, scope changes, self-erasing findings, delegated-agent returns, and work folders, and names each passage it checked and what it says.

6. `rails-run-a-workflow` and `rails-read-me` describe one work folder per work item and no longer describe a separate `<date>-<slug>` run folder. An adversary reads both and quotes what each now says.

## Level

L2 — normal ceremony. The change is rules and wording in existing rails and existing agent instructions, and adds no structure the SOLID rail governs.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path. This is a locked law: it cannot be changed, narrowed, or excepted except by a contract the human approves for that exact change.
