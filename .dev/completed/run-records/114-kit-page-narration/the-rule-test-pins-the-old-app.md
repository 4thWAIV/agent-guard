# One test pins `app.tsx` as it was before the work

The implementation is in and sixteen of the seventeen reds are cleared. One test is still red, and no agent in the pipeline may clear it.

## What it asserts

`ts/metalang/guardrails.test.ts:1362`, in the rule file RULE-PHASE wrote:

```
test('rule 1 weighs all twenty-four literals of app.tsx and leaves nine to the three kinds', () => {
  const app = sourceOf('ts/metalang/app.tsx')
  expect(languageWordsIn(app).length).toBe(24)
  expect(wordsBySpelling(app).length).toBe(15)
  expect(outsideTheRule(app).map(reported)).toEqual([ ...nine entries, each `file:line: "word" is <reason>` ])
})
```

The comment above it says what it is: "this is the rule's impact on existing code rather than proof that the rule works: the fifteen literals the contract records under `Rules to add` rule 1 as what IMPLEMENT clears."

## Why it cannot pass

The contract orders the state it denies. Rule 1's recorded impact reads "The fifteen are the rule's impact on existing code and IMPLEMENT clears them." Clearing them makes the weighed count nine and the flagged count zero, and the new `data-declaration` and `data-member` code moves the nine exempt literals down the file. Measured on the tree as it stands: weighed 9, flagged 0, and the same nine exemptions in the same three kinds at lines 86, 97, 108, 116, 151, 940, 1442 twice and 1636. No exemption changed kind and no literal changed status; only the numbers moved.

The IMPLEMENT worker left the file untouched — `git diff --quiet ts/metalang/guardrails.test.ts` returns 0 — because its rule-writing and test-writing authority is revoked, and it reported the wall instead.

## Why it needs Tim

Only a RULE-PHASE round may write that file, and `rails-run-a-workflow` reserves re-running a completed stage to him: "Re-running any completed stage is the human's decision and needs their explicit yes — never the orchestrator's own call." The shape the test takes afterwards is a real choice, so it goes with the same question.

Asked as decision `impact-test` in a package on `work-0ce6071af7d5`.

## The two shapes

Pinning what rather than where: the test drops the two totals and the line numbers and asserts the exemptions by the word and the reason, so a new exemption appearing still fails it while moving code does not. Repinning the numbers: the test keeps its shape with nine, zero and the nine current lines, and is repinned by hand whenever `app.tsx` moves.

## Two things the worker declared rather than hid

It removed the three tokens `as Document` at `ts/metalang/app.test.tsx:1784`. That line is where the TDD result records criterion 13's RED; installing `@types/jsdom` cleared the unsafe-call error and made the assertion redundant, which the linter then reported at the same position. No assertion, expectation, name or skip changed. The alternatives were a NoWarn, an eslint-disable or an eslint config change, all suppressions.

`bun add --dev --exact @types/jsdom@30.0.0` wrote one transitive resolution into `bun.lock` that was not there before, `@types/tough-cookie@4.0.5`. The lock cannot record the authorized package without it. Nothing else in the lock moved, and the `package.json` description line the command rewrote was reverted.

## The first package asking this was refused

Tim answered `impact-test` with none of these on 2026-11-13 and gave his reason: the package opened with data before saying what it was about and why that data mattered, and a package system exists to stop exactly that. The question stands; the presentation was the problem.

The rebuild opens by saying what the package is about and what it needs from him — the kit work is finished, one test disagrees with it, and only he can authorize the round that changes it — and shows the test and the moved line numbers afterwards, as the evidence for that ask rather than as the opening.
