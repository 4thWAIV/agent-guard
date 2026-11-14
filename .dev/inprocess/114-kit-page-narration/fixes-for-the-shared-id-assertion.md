# Test round: prove the shared-id report that already exists

One confirmed gap. Nothing else in this round.

## The gap

Acceptance check 5 ends "and two declarations sharing an id are reported as ambiguous rather than resolved to the first". The report exists: `ts/metalang/check.ts:1004-1005` fails the second declaration with `"<id>" is already the id of <where the first was>`, covered by `ts/metalang/check.test.ts:216`, and the page shows that problem in its problems section. The test covering check 5, at `ts/metalang/app.test.tsx:1866`, asserts only that both declarations draw their own section. It never asserts the report reaches the page, so the clause is true of the code and unproven by the test.

## The fix

Add the missing assertion to that test, against the page the existing fixture already renders: the page carries the checker's problem for the second declaration's id, naming where the first was. Nothing else changes — not the fixture, not the existing assertions, not any other test, and nothing in production.

Read the report's real wording off the live code rather than from this file, and assert what it actually says. If what the checker reports does not satisfy the clause as the contract words it, say so in the notes instead of bending the assertion to fit; that is a finding for the reviewers, not something to paper over.

## The coverage record

Record a row for every one of the contract's eighteen acceptance checks, under decision `passing-row-reason`: any check that passes today is recorded as passing and carries the reason it passes, whatever makes it pass, and a check that fails is recorded failing with an empty reason. Read each row's truth off this tree.

## Proving it

`bun test` from the repository root, pasted with its exit code. The suite is green before this round and must be green after it: this assertion covers behaviour that already works, so it passes the moment it is written, and its row says so.

## What this round does not touch

Every other test, the three rules, anything under `.agents/`, and all production code.
