# Give GROUND and DESIGN a written brief

## Decisions

Each decision below was settled in conversation and approved as written. No decision here is open.

1. **One folder per work item.** It is created at `.dev/backlog/<issue-number>-<slug>/` when the issue is filed, and that same folder moves to `.dev/inprocess/` when work starts and to `.dev/completed/run-records/` when it ships. No second folder is created for a run.

2. **GROUND is given the issue number and the input folder path.** A scope line accompanies them when a run covers only part of an issue.

3. **The live GitHub issue is the authority.** Every agent reads it with `gh issue view`. Where the issue and anything in the input folder disagree, the issue wins.

4. **`conversation.md` lives in the input folder and is approved before GROUND launches.** The orchestrator writes it; it summarizes what the human decided in conversation.

5. **`design.js` takes the same three inputs in place of its `goal` string**, so DESIGN reads the brief GROUND read rather than a sentence composed in a tool call.

6. **A delegated agent returns a verdict, counts, and failures.** It never returns the enumeration of what it processed.

7. **The DESIGN stage's saved result no longer carries a `goal` field.** The field is removed from the `design-stage` result contract inside the `stage-result-contracts` block, in every workflow script that carries that block, with all copies left byte-identical.

8. **Every agent gets all the data that helps it.** The brief reaches every agent either script launches, including the prior-art ledger's search and judging agents, whose prompts live in the `prior-art-ledger` copied block.

9. **This work runs at L2.** A contract, one fresh worker, then Prove-It, DRY, and the Lie-catcher.

## Rules to add

None — this change adds no analyzer rule.

## The standard / what we're building

`ground.js` and `design.js` each accept three new inputs — `issueNumber`, `inputFolderPath`, and an optional `issueScope` — and render them into a brief paragraph that every agent the script launches receives. `design.js` loses its `goal` input.

The brief paragraph tells the agent to read the live issue with `gh issue view <n>`, to read the files in the input folder, and that the live issue supersedes the folder.

## Success definition

ALL criteria met AND no errors in the system as a result of the change.

For this run: both scripts validate the three new inputs and refuse to launch without the required two; every agent prompt either script produces carries the brief; `design.js` no longer accepts `goal`; the shared block is byte-identical in both scripts; and `eng/check-copied-modules.mjs` exits clean.

## Surfaces

- `.agents/workflows/ground.js` — the input block near the end of the file, `explorePrompt`, and the prior-art-ledger capability prompts it builds.
- `.agents/workflows/design.js` — the input block near the end of the file and every prompt it builds.
- Every workflow script carrying the `stage-result-contracts` block — the `design-stage` result contract, which declares the `goal` field.
- Every workflow script carrying the `prior-art-ledger` block — its search and judging prompts.
- `.agents/skills/rails-run-a-workflow/SKILL.md` and `.agents/skills/rails-read-me/SKILL.md` — where the brief is documented.
- `eng/check-copied-modules.mjs` — enforces byte identity of every copied block. Not edited; it must stay green.

## Reuse ledger

Produced by `.agents/workflows/prior-art-ledger.js`, recorded as the script returned it.

**render-issue-brief — `extract`, high confidence.** Copies at `.agents/workflows/design.js:42` and `.agents/workflows/ground.js:42`. Grep found byte-identical `__workflowBrief` bodies at both, tagged as the copied-module block `workflow-brief`, each with one call site (design.js:1309, ground.js:1442) doing the same rendering: issue number plus optional scope plus folder path into the same brief string telling the agent to read the live issue via `gh issue view` and the folder, with the live issue as authority. CodeGraph confirmed both resolutions and callers, and flagged no covering tests.

**share-code-between-workflow-scripts — `reuse`, high confidence.** Owner at `.agents/skills/rails-run-a-workflow/SKILL.md:269`. The rail already documents the mechanism: the Workflow runtime provides no module import and caps nesting at one level, so shared code is copied into each script under the `##COPIED-MODULE-BEGIN##` / `##COPIED-MODULE-END##` markers, held byte-identical by `eng/check-copied-modules.mjs`. Seven blocks already use it across nine scripts. The convention itself is the sanctioned answer to the missing module import, so new shared code follows it rather than inventing a mechanism.

## What to do

1. Add a copied module named `workflow-brief` to both `ground.js` and `design.js`, carrying the same header comment every other block uses. It validates the three inputs and returns the brief paragraph. `issueNumber` and `inputFolderPath` are required; `issueScope` is optional and, when absent, the paragraph omits any scope sentence.

2. In `ground.js`, read the three inputs alongside the existing ones, extend the existing input-validation error message to name the two new required inputs, and prepend the brief to `explorePrompt` and to the prior-art-ledger capability prompts.

3. Document the brief in `rails-run-a-workflow` and `rails-read-me`: the GROUND and DESIGN Inputs lines name the issue number, the optional scope line and the work folder path; a paragraph in GROUND states the brief is the issue plus that folder and that the live issue wins; `## Launch preconditions` carries "The brief is approved"; and `rails-read-me` describes the work folder's contents.

4. In `design.js`, read the same three inputs, delete the `goal` input and every use of it, and prepend the brief to every prompt the script builds.

4. Prepend the brief to the prior-art ledger's search and judging prompts inside the `prior-art-ledger` copied block, and apply the identical edit to every workflow script carrying that block so all copies stay byte-identical. The brief is optional inside that block: when the block runs with no brief, as `prior-art-ledger.js` does when invoked on its own, the prompts omit it and are otherwise unchanged.

5. Remove the `goal` field from the `design-stage` result contract in the `stage-result-contracts` copied block — the field declaration, its entry in the required list, and its `nonEmptyStringFields` validation entry — and apply the identical edit to every workflow script carrying that block so all copies stay byte-identical.

## What the agent MAY do

- Edit `.agents/workflows/ground.js` and `.agents/workflows/design.js`.
- Edit the `stage-result-contracts` copied block in every workflow script that carries it, for the sole purpose of removing the `design-stage` `goal` field, keeping every copy byte-identical.
- Edit the `prior-art-ledger` copied block in every workflow script that carries it, for the sole purpose of adding the optional brief to its prompts, keeping every copy byte-identical.
- Choose the internal wording of the brief paragraph, provided it instructs the agent to read the live issue with `gh issue view`, to read the input folder, and that the issue supersedes the folder.

## What the agent MUST NOT do

These bind the IMPLEMENT worker.

- Edit any file outside Surfaces, including `eng/check-copied-modules.mjs`.
- Change anything in another workflow script beyond the two copied-block edits named above.
- Change, reorder, or remove any existing input of either script other than `design.js`'s `goal`.
- Let the two copies of the `workflow-brief` block differ by a single byte.
- Commit, stage, push, or open a pull request.
- Expand scope. Hit a wall, stop and report.

## Acceptance

1. `node eng/check-copied-modules.mjs` exits 0. Paste the command and its full output with the exit code.

2. Both scripts parse. A workflow script is not valid standalone JavaScript, so the check builds each one through the async function constructor after replacing `export const meta` with `const meta`:
   `node -e "const s=require('fs').readFileSync(process.argv[1],'utf8').replace('export const meta','const meta'); new (Object.getPrototypeOf(async function(){}).constructor)('args','budget','agent','parallel','pipeline','log','phase','workflow',s); console.log('parsed')" <path>`
   Run it for both files. Paste both outputs with exit codes.

3. The `goal` input and the `goal` result field are both gone. Run all three and paste each with its exit code:
   `grep -c "input.goal" .agents/workflows/design.js` returns 0.
   `grep -rn "goal: STRING" .agents/workflows/` returns nothing.
   `grep -rn "'goal'" .agents/workflows/` returns nothing.
   An adversary then reads every remaining occurrence of the word in `design.js` and confirms each one is ordinary prose inside a prompt, naming each.

4. Every prompt in both scripts carries the brief. An adversary re-derives this by reading each prompt-building expression in both files and naming each one it checked.

5. The `workflow-brief` block is byte-identical across both files, proven independently of check 1: extract the block from each file and diff them. Paste the diff command and its empty output with the exit code.

6. Neither script's existing inputs changed except `design.js`'s `goal`. An adversary diffs the input blocks and names what it compared.

## Level

L2 — normal ceremony. The change touches two scripts and adds no structure the SOLID rail governs.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path. This is a locked law: it cannot be changed, narrowed, or excepted except by a contract the human approves for that exact change.
