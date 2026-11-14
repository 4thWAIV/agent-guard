# Test round: the one finding against it is cured

The round that wrote the test for acceptance check 18 was passed by its test-quality and duplication reviewers and refused by the lie-catcher on one count, which no longer stands.

## The finding

The identity spelling those tests pin — `PrecheckAsync#1` and `PrecheckAsync#2`, numbered among the rows sharing the name — is a format other people write their narration beats against, and the contract carried it on the orchestrator's word rather than Tim's.

## Why it no longer stands

Tim approved it on 2026-11-13, answering `identity-spelling` with `among-the-shared` in pkg-f1d7e85abb9f. The contract now carries it as decision `identity-spelling`, in the wording he approved, including why the number counts among the rows sharing the name rather than among all of a declaration's members.

## What this round does

Nothing to the tests. They are written, they fail for the right reason, and they pin exactly what he approved. Re-return the same result — the same three cases in `ts/metalang/app.test.tsx`, the same whole-suite RED proof, and the same eighteen-row coverage record — so the panel judges it against a contract that now carries the approval.

Read the tests and the contract yourself rather than taking this file's word for either. If anything in them does not match decision `identity-spelling` as approved, say so instead of returning a result.

## Proving the RED

`bun test` from the repository root, pasted with its exit code. The only failures in it may be the two check-18 cases that cannot pass until the implementation lands.
