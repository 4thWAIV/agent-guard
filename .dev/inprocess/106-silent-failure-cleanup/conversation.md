# What Tim decided in conversation, 2026-11-08

This is the brief every explorer works from. The live GitHub issue 106 is the authority where this and the issue disagree.

## What the work is

Make the thirteen silent failures loud that the audit of 19 October 2026 found and did not fix. Six of the nineteen were fixed and shipped in pull request 105. The thirteen are grouped in `issue.md` by what the silence costs, with each file and line read against the live code on 2026-11-08.

## The decisions he made, in the wording he approved

Both came through the signed package `the-silent-failures-still-in-the-code-1.0.0-local07b` on 2026-11-08.

**An unreadable symlink throws, and the callers catch.** Decision `symlink-shape`: "Throw, and callers catch — a smaller change that raises an expected case as an exception." The recommendation put to him was a third answer on the contract, and he chose against it.

**All five groups ship as one piece of work.** Decision `scope`: "All five as one piece — one pass over every file, one adversary round for all of it." The recommendation put to him was the symlink fix first on its own, and he chose against it.

He approved the contract as written on 2026-11-08 through the package `approve-the-contract-for-the-silent-fail-1.0.0-local657`, decision `approve`: "Approved as written."

## Two things he was told when he approved the contract

The contract changes one test file under `analyzers/**`, which is a decision-gated area. The change is a test, not an analyzer rule, and he was told that approving the contract is the approval for it.

The contract adds no analyzer rule, because the seven analyzers that go inert do so when a type is absent from a compilation, which no rule can observe.

## What he has not decided

Nothing about this work was left open when the contract was approved. Two corrections to the audit were put to him and are recorded in `issue.md`: seven analyzers go inert rather than three, and the symlink read has four callers of which the path canonicaliser is the one that matters.

## Where the material is

- `issue.md` beside this file — the thirteen findings in five groups, with their file and line references.
- `contract.md` beside this file — the approved contract.
- `docs/silent-failure-audit.md` — the audit of 19 October 2026 with its fixed and unfixed lists.
