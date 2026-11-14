# The shared declaration id is already reported, and the test does not say so

The readiness check found that acceptance check 5 promises something this repository does not do — "two declarations sharing an id are reported as ambiguous rather than resolved to the first" — and said nothing here reports anything about a shared id, pointing at `ts/metalang/check.ts:1060`, which reports duplicates among a choice's options. I took that finding at its word and built a package asking Tim to choose between building the report here and re-pointing the sentence to the narrator.

The fresh author refused to write that package and was right. Both of its options rested on a claim that is false.

## What is actually true

`ts/metalang/check.ts:1004-1005` keeps one map of ids for the whole document and fails the second declaration with `"<id>" is already the id of <where the first was>`. `ts/metalang/check.test.ts:216` covers it: two declarations with one id is a problem naming where the first was. The page shows that problem in its problems section like any other, and the author reproduced it on the very fixture acceptance check 5's test uses, where the first problem reads `declare[1].constant.id: "IGuard" is already the id of declare[0].constant`.

So the report exists, in this repository, and has since October.

## The real gap

One assertion. The test covering check 5, at `ts/metalang/app.test.tsx:1866`, asserts that both declarations draw their own section and that the two sections' first members read `First` and `Second`. It never asserts that the page carries the checker's report. So the clause is true of the code and unproven by the test.

Whether the checker's words — "is already the id of", naming both places — satisfy "reported as ambiguous" is for the reviewers to rule on once the assertion is there. Neither the author nor I decide that for them.

## What this changes in the record

Nothing goes to Tim. The contract does not change, acceptance check 5 stands as written, and no scope moves.

Issue 115 stays open and means what it says: whether the checker should *refuse* a document that declares one id twice, rather than report it. Refusing is not reporting, and the contract's own note that refusing is out of scope remains correct.

`tdd/result.json` records that the test for check 5 "proves the half that is in scope" because the report was believed to be elsewhere. That belief was wrong; the test round adding the assertion supersedes it.

## My part in it

I passed a readiness finding into a package handoff without checking its central claim against the code, and the claim was false. The author checked and stopped. The cost was one author run; the cost of not checking would have been Tim ruling on a choice that does not exist.
