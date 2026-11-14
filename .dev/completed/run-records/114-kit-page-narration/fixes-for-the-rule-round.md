# Rule round, re-run with the scan in hand

Tim answered `scan-ran-late` with `re-run-the-stages` on 2026-11-13: the stages that ran before the hidden-decision scan existed run again with its result in hand. This is that round for the rules. One confirmed finding is fixed in it; the three rules themselves are unchanged and are re-proved.

## The scan's result, which this round now has

`.dev/inprocess/114-kit-page-narration/ground/hidden-decision-scan.json`, with its provenance beside it. Its three findings are all settled: what a page does when the narration script is not there became Tim's decision `helper-absent` and is built; a claim about release digests was false and left issue 121 behind; and the member name repeated inside one declaration is now contract decision `member-identity-unique`, which Tim ordered fixed rather than deferred. Read it and rule the three approved rules against it. If anything in it bears on a rule, say so in the notes; nothing in it asked for a fourth rule.

## The confirmed finding to fix

The laziness auditor, against `rails-real-work` rail 11, which bans shipped code naming a path under `.dev/inprocess/` or `.dev/completed/`: `ts/metalang/guardrails.test.ts:3` reads "approved as written in `.dev/inprocess/114-kit-page-narration/contract.md` under `Rules to add`". That path stops existing when this folder moves to `.dev/completed/run-records/` on shipping.

Reword that header comment so it quotes the contract's `Rules to add` wording without naming any path under `.dev/`. Nothing else in the file changes.

## What this round does not touch

The three rules, their violating and compliant fixtures, the impact test rewritten under decision `impact-test`, and every file outside `ts/metalang/guardrails.test.ts`. The member identity work belongs to the test and implementation rounds that follow this one.

## Proving it

Re-prove all three approved rules with their violating and compliant fixtures, and run `bun test` from the repository root: it must report 0 fail. Also `bun run typecheck` and `bun run lint`. A single-file run always exits 1 here because the coverage floor cannot be reached by one file, so the whole-suite run is the proof.

## The result's rule names

Return `perRule` with exactly these three names, spelled exactly as they are here:

- No language word by spelling in the drawing path.
- No literal target in non-test source.
- One owning site per identity attribute.
