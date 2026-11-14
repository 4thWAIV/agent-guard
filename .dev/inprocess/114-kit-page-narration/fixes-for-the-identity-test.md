# Test round, re-run with the scan in hand

Tim answered `scan-ran-late` with `re-run-the-stages`, so this is the test round running again with the hidden-decision scan's result available, and it is also the round that covers the one acceptance check nothing tests yet.

## The scan's result, which this round now has

`.dev/inprocess/114-kit-page-narration/ground/hidden-decision-scan.json`, with its provenance beside it. All three of its findings are settled: the page saying so when the narration script is absent became decision `helper-absent` and is built and covered by checks 16 and 17; a claim about release digests was false and left issue 121; and the member name repeated inside one declaration is decision `member-identity-unique`, which is what this round covers.

## The one check with no test

Acceptance check 18, in the contract's own words:

> A declaration with two members both named `PrecheckAsync` draws one row with `data-member="PrecheckAsync#1"` and one with `data-member="PrecheckAsync#2"`, each selector matching exactly one element, while a selector naming `PrecheckAsync` alone matches none. A declaration with no repeated name is drawn exactly as before. All three cases named in the output.

Decision `member-identity-unique` is the wording that check comes from, including why the count runs among the rows sharing the name rather than among all of a declaration's members. Write the test against the component render or the executed bundle, as checks 2 to 5 already do, in `ts/metalang/app.test.tsx`.

It is RED because the behaviour is not built: `ts/metalang/app.tsx:747-754` writes each row's own name with nothing making it unique, so both rows carry `PrecheckAsync` today and the first two cases fail.

## The coverage record

Record a row for every one of the contract's eighteen acceptance checks. Decision `passing-row-reason`, which Tim approved on 2026-11-13, now governs how a check that passes is recorded:

> Any acceptance check that passes today is recorded as passing, carrying the reason it passes, whatever makes it pass — a guard on behaviour that must keep working, a prior stage's finished work, or an implementation this contract has already built. Both bans in `green-criterion` stand: a passing row with no reason is refused, and a failing row that carries one is refused.

So seventeen rows carry `red: false` with the true reason each passes, read off this tree rather than from an earlier record, and check 18 carries `red: true` with an empty reason. No row may claim a check is failing when it passes.

## What this round does not touch

Every test already written, the three rules in `ts/metalang/guardrails.test.ts`, and anything under `.agents/`.

## Proving the RED

`bun test` from the repository root, pasted with its exit code. The only failure in it may be the new check-18 test.
