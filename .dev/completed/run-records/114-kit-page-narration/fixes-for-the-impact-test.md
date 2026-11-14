# Fix for the impact test

One test changes. Nothing else in the rule file, and no other rule, fixture or test, is touched.

## What Tim decided

He answered `impact-test` with `pin-what-not-where` on 2026-11-13: "Pin words + reasons — keep exemptions, drop counts and lines." The contract records it in Decisions. That answer authorizes this one round and nothing else.

## The one fix

`ts/metalang/guardrails.test.ts:1362`, the test named `rule 1 weighs all twenty-four literals of app.tsx and leaves nine to the three kinds`, asserts the state of `app.tsx` before the implementation: twenty-four literals weighed, fifteen flagged, and nine exemptions at nine named line numbers. The implementation cleared the fifteen the contract assigned to it, so all three assertions are now false.

Rewrite that test so it checks what the exemptions are rather than where they sit:

- no total of weighed literals and no total of flagged literals, because the test beside it, `writes no word of the language out`, already requires zero flagged across the whole drawing path;
- the exemptions asserted by the word and the reason, so a tenth exemption appearing, or one of the nine changing kind, still fails it;
- no line number anywhere in it, so editing anything above the exemptions does not fail it.

Give it a name that says what it checks now. The name it has describes the old state.

## What is true on the tree as it stands

Nine literals weighed, zero flagged. The nine exemptions are the same words with the same reasons as before: six where `pending` is a member of the page's own union of finding kinds, one where `object` is a comparison against a JavaScript type, and two where `property` and `properties` are English prose shown to the reader. Read them from the live file rather than from this list.

## What must not change

The three rules themselves, their violating and compliant fixtures, and the passing `writes no word of the language out` test. `git diff` on the rule file after this round should show that one test and nothing else.

## Proving it

Re-prove all three approved rules with their violating and compliant fixtures, and run `bun test` from the repository root: it must report 0 fail. A single-file `bun test` always exits 1 in this repository because `bunfig.toml` turns coverage on with a 0.75 floor that one file cannot reach, so the whole-suite run is the proof and `0 fail` is the signal.

## The result's rule names

Return `perRule` with exactly these three names, spelled exactly as they are here, because the stage checks the set against the contract:

- No language word by spelling in the drawing path.
- No literal target in non-test source.
- One owning site per identity attribute.
