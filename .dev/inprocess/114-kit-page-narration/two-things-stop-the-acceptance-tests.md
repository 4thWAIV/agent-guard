# Two things stop the acceptance tests

The tests are written and failing for the right reasons: fifteen of them across `ts/metalang/app.test.tsx`, `ts/metalang/render.test.ts` and `ts/metalang/kit.test.ts`, with jsdom 31.0.0 pinned in `package.json` and `bun.lock`. Two things stopped the stage finishing. Tim answered both on 13 November.

## jsdom carries no type declarations

jsdom 31.0.0 ships no `.d.ts` and names no `types`, `typings` or `exports` entry in its own `package.json`. Both TypeScript gates fail on the import:

```
$ bun run typecheck
ts/metalang/app.test.tsx(12,23): error TS7016: Could not find a declaration file for module 'jsdom'.
$ bunx eslint ts/metalang/app.test.tsx
1784:18  error  Unsafe construction of a(n) `error` type typed value  @typescript-eslint/no-unsafe-call
```

`.github/workflows/ci.yml:437` and `:439` run both, so CI cannot go green while the import is untyped. The test author reduced it to one crossing — `matched` in `app.test.tsx` is the only place a DOM is built from a page — and suppressed nothing.

Two ways to type it. `@types/jsdom`, the published declarations, latest 30.0.0: types only, no runtime code, maintained with the library. Or a declaration file written in this repository: no new dependency, and a second copy of what `@types/jsdom` already states, kept by us.

Checked: `@types/jsdom` 30.0.0 types jsdom 31.0.0's used surface — the `JSDOM` constructor, `window.document` and `window.CSS.escape` — and compiles clean under this repository's own settings, which are `strict` with `skipLibCheck`.

The contract's `dom-library` decision reads: "Adding it to `package.json` devDependencies and `bun.lock` at that exact version is what the answer authorizes, and nothing else." A second dependency, even a types-only one, is outside it.

Recommendation: `@types/jsdom` 30.0.0. Writing our own declarations duplicates a published, maintained thing, which is what the prior-art rule exists to stop.

Answered. Tim chose `types-package` on 2026-11-13 at 12:44, decision `jsdom-types` in pkg-6d9db6691d8c, recorded in `results/decisions.json` in `unblock-the-kit-acceptance-tests-1.0.1-localea7`. The contract now carries it as decision `jsdom-types`, with `@types/jsdom` 30.0.0 in Surfaces, in `What to do` item 5 and as acceptance criterion 13.

## The TDD stage demands every acceptance criterion be failing now

`.agents/workflows/tdd.js:1118` requires every entry in the test author's coverage list to be RED whenever any test file is written: `itemsAllTrue: [{ field: 'coverage', itemField: 'red' }]`. It validates the `tdd-author` result. The prompt that asks the author for that list, at `.agents/workflows/tdd.js:1408`, asks for "whether it is RED now", so the field the author is told to report truthfully is the field the check demands always be true.

The check sits inside the `stage-result-contracts` copied module, which is byte-identical in nine of the eleven files in `.agents/workflows/`, and `eng/check-copied-modules.mjs` enforces that. Changing it means making the same edit in all nine copies and rerunning that check. The other eight scripts carry the `tdd-author` case but never run it, so their behaviour does not change.

Seven of this contract's fifteen acceptance criteria cannot honestly be made to fail:

- 6, that every `data-part` and `data-col` target passing today still passes, "by the existing assertions running unchanged" — the criterion's own words;
- 7, that one document rendered twice under one built kit produces identical bytes, which holds today and would break the day anything non-deterministic entered the page;
- 8, that each of the three rules has a violating and a compliant fixture, which RULE-PHASE built and proved;
- 10, the two literal targets, which the contract itself puts on the JARVIS side after the kit release;
- 11, the coverage floor, which is a gate run;
- 14 and 15, which check the change this decision authorizes, made before the test stage could record its own result.

The stage rejected the author's result twice on shape alone. Its second rejection was `coverage[6].red must be exactly true when testFiles is not empty`, which asks the author to record a passing guard as failing.

Two ways out. Change that check so a criterion may be recorded as passing with its reason, which is what the field already claims to hold. Or leave the rails untouched and record only the criteria with new failing tests in the coverage list, naming the rest in the notes, which leaves seven of fifteen criteria out of the structured record the later stages read.

Recommendation: change the check. A guard against breaking what works is a real acceptance criterion, and a pipeline that cannot record one pushes every future contract toward either a false record or a thinner one.

Answered. Tim chose `record-it-green` on 2026-11-13 at 12:47, decision `green-criterion` in pkg-6d9db6691d8c. The change is made and the contract carries it: a coverage row is `red: false` only when `whyPassing` says why it passes, a row that claims both is refused, and acceptance criteria 14 and 15 prove it.

The same sentence turned out to be written in four places, not one. The condition the result contract is checked with lives in the `required-agent-runtime` copied block, which is in all eleven workflow scripts, not the nine that carry `stage-result-contracts`; the coverage record's shape and the `tdd-author` contract are in the nine; the test author's, the reviewers' and the implementer's instructions say it in prose; and `rails-test-code` and `rails-run-a-workflow` state the all-RED requirement as a rail. All four now say the same new thing, and `eng/check-copied-modules.mjs` holds the twenty copies byte-identical.

## Two defects in the contract, found by the test author

Both are corrections to what the contract already requires, so I am amending it rather than asking.

1. The story helper has no acceptance criterion. "The standard" says "The page loads JARVIS's story helper so a beat can ring and point inside the package frame", and `What to do` item 4 gives the exact tag and position, but no numbered criterion names it, so criterion 1 would pass with the helper absent. The author wrote four tests for it anyway.
2. Criterion 11 names the wrong command. It says `eng/coverage-gate.sh`, which is the .NET gate: `grep -n bun eng/coverage-gate.sh` returns nothing and its header says it merges the cobertura reports `dotnet test` produced. The bun floor is enforced by `bun test` itself through `coverage = true` and `coverageThreshold = 0.75` in `bunfig.toml`.
