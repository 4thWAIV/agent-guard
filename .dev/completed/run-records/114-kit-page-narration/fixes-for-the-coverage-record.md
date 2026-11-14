# Fixes for the coverage record

The tests stay as they are. They are written and failing for the right reasons, and nothing in this round asks for a new test, a rewritten test, a changed assertion or a new dependency. What changed is the record the stage keeps of them, and two decisions that were open when the last round ran.

## The record may now carry a criterion that passes, with the reason it passes

The contract records this as decision `green-criterion`:

> The test stage may record an acceptance criterion that passes today, with the reason it passes. A criterion that only guards behavior this contract requires to keep working is recorded in the coverage list as `red: false` carrying the reason in `whyPassing`; every other criterion is still RED, a `red: false` row with no reason is refused, and a `red: true` row that carries one is refused.

Each coverage row now has five fields: `acceptanceItem`, `tests`, `kind`, `red` and `whyPassing`. The fix is one thing: record a row for every acceptance criterion in the contract, with `red` true and `whyPassing` empty for a criterion that fails now, and `red` false with the reason it passes for a criterion that only guards behavior the contract requires to keep working. Write the reason in the criterion's own terms, from what you observe when you run the tests — not from this file. A row that says both, or neither, is refused by the result contract.

Nothing here lets a criterion whose behavior this change builds be recorded as passing. If a criterion should fail and does not, the test is wrong and the fix is the test.

## The contract has fifteen criteria now, not twelve

Three were added after the last round, so read the Acceptance section again in full rather than working from the list you had. Criterion 13 is the typecheck and lint pair. Criteria 14 and 15 cover the change this decision authorizes, which was made before this round so that the stage could record its own result; `What to do` item 8 says the IMPLEMENT worker does not repeat it and changes nothing under `.agents/`.

## The jsdom import stays untyped in this round

The contract records this as decision `jsdom-types`:

> The jsdom import is typed by the published declaration package `@types/jsdom` 30.0.0. Adding it to `package.json` devDependencies and `bun.lock` at that exact version is what the answer authorizes, and nothing else; no declaration file for jsdom is written in this repository.

Adding it is `What to do` item 5, which the IMPLEMENT worker carries out. Do not add it here and do not write a declaration file. Criterion 13 is RED for exactly that reason, and that RED is what the worker clears.

## Proving the RED

`make test` is `dotnet test` alone and runs none of the kit's tests, which the contract records as issue 118. The failing output that proves the RED for this contract comes from `bun test`. Paste it verbatim with its exit code, and name in the notes that `dotnet test` covers none of these tests.
