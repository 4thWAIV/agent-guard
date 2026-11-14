# Fix round: the page says so when the helper is not there

Two acceptance checks are new and have no tests. Everything else in the contract is covered and passing; nothing already written changes.

## What Tim decided

Decision `helper-absent`, answered `page-says-so` on 2026-11-13: a page the kit draws says so when JARVIS's story helper is not there. Decision `when-the-page-says-so`, answered `fold-it-in`: it is built before the kit is released, and that answer is his authorization for this round.

## The two checks to cover

Criterion 16: a page drawn where the helper never announces itself shows exactly one plain line saying it cannot be narrated here, and a page where the helper announced itself shows none. Both cases named in the output.

Criterion 17: the kit readme says a drawn page reaches out to its origin for the narration script.

## What the code does today, so the tests are RED for the right reason

`ts/metalang/render.ts:68` writes `<script defer src="/api/packages/story.js"></script>` into every page, and `ts/metalang/client.tsx` mounts the page. Nothing reads whether the helper arrived and nothing draws a line, so a test asserting the line fails because the behaviour is absent.

The helper is JARVIS's `jarvis/package_story.js`. It sets `window.__jarvisStory = true` as it loads, at its lines 29 and 30, and posts `story.ready` to its parent. The page's bundle is written inline by `pageHtml` and an inline script runs before any deferred script, so a reading taken at mount finds nothing even when the helper is about to load; `What to do` item 9 says the reading happens once the deferred scripts have run.

## What this round does not touch

The fifteen criteria already covered, their tests, the three rules in `ts/metalang/guardrails.test.ts`, and anything under `.agents/`. The coverage record keeps the rows it has; the two new criteria join it with their own rows.

## Proving the RED

`bun test` from the repository root is the proof, and `0 fail` is the signal: a single-file run always exits 1 here because `bunfig.toml` turns coverage on with a floor one file cannot reach. Paste the failing output with its exit code. Every other criterion must stay as it is — fifteen rows unchanged, eight of them red for the reasons already recorded and seven carrying the reason they pass.
