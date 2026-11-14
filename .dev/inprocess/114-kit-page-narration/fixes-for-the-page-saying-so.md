# Fix round: the page says so when the helper is not there

Two reviewers refused the first attempt at this round. Both findings are confirmed against the tree and both must be applied. Nothing else changes: the tests written for the two new checks stay, and no test, rule or fixture written in any earlier round is touched.

## What Tim decided

Decision `helper-absent`, answered `page-says-so` on 2026-11-13: a page the kit draws says so when JARVIS's story helper is not there. Decision `when-the-page-says-so`, answered `fold-it-in`: it is built before the kit is released, and that answer is his authorization for this round.

## Finding one, from the test-quality reviewer: the announced case cannot fail

The harness in `ts/metalang/render.test.ts`, around lines 365 to 395, sets `dom.window.__jarvisStory` and `globalThis.__jarvisStory` before importing the bundle. A bundle that read the announcement at mount would therefore pass the announced case, so nothing pins what `What to do` item 9 requires — that the reading happens once the deferred scripts have run.

The fix the reviewer gives: in the harness, for the announced case, import the bundle first, wait until the root has drawn, then set the announcement, then dispatch `DOMContentLoaded` and `load`. That is the real browser order, an inline bundle followed by the deferred helper announcing itself. Leave the silent case as it is. With that order a mount-time reader draws the line in the announced case and the test fails, which is what makes the case able to fail at all. Prove it satisfiable with a throwaway implementation that reads the announcement after load, then put the tree back as you found it and say in the notes that you did.

## Finding two, from the lie-catcher: the record claims criteria are failing when they pass

The result restated all seventeen criteria. Seven rows said `red: true` for criteria that pass in this tree — acceptance items 2, 3, 4, 5, 9, 12 and 13 — because the implementation for them landed in commit cfbbbf5. Four `red: false` rows carried reasons that are untrue now, each asserting the implementing code has not been written.

The fix: this round's coverage record holds exactly the two criteria this round is for, 16 and 17, both `red: true` with an empty `whyPassing`. Do not restate the other fifteen. They are recorded in `.dev/inprocess/114-kit-page-narration/tdd/result.json`, which describes the tree as it stood before the implementation, and that record is not reopened here. Say that in the notes, in one sentence, so no reader takes a two-row record for the whole contract's coverage.

No row may say a criterion is failing when it passes in the tree you are submitting against, and no `whyPassing` may state anything untrue of that tree.

## What the code does today, so the tests are RED for the right reason

`ts/metalang/render.ts:68` writes `<script defer src="/api/packages/story.js"></script>` into every page, and `ts/metalang/client.tsx` mounts the page. Nothing reads whether the helper arrived and nothing draws a line. The helper is JARVIS's `jarvis/package_story.js`: it sets `window.__jarvisStory = true` as it loads, at its lines 29 and 30. The kit readme written in `ts/metalang/kit.ts` says nothing about the request the page makes.

## Proving the RED

`bun test` from the repository root is the proof and `0 fail` is the signal for a single file, because `bunfig.toml` turns coverage on with a floor one file cannot reach. Paste the failing output with its exit code. The only failures in it may be the two tests for criteria 16 and 17.
