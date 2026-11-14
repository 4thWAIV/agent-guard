# Fix round: build the page saying so

Two tests are failing and nothing else is. Build exactly what makes them pass.

## What is already in

Everything in `What to do` items 1 to 8 is built and committed: identity attributes written from the document being drawn, the role added to the architecture language with the role's meaning widened, the story helper loaded from the page shell, the fifteen literals cleared from `app.tsx`, the two stylesheets added to the CI required-file check, and the types package taken. That landed in commit cfbbbf5 and its criteria pass. Do not redo any of it.

## What is not in

`What to do` item 9, under decision `helper-absent`: the page says so when JARVIS's story helper is not there, and the kit readme says a drawn page reaches out to its origin for that script.

## The two reds to clear

`ts/metalang/render.test.ts` — `what a drawn page says about being narrated`: a page the helper never announced itself on shows exactly one line saying it cannot be narrated here, and a page it did announce itself on shows none.

`ts/metalang/kit.test.ts` — `the readme the kit ships with`: a paragraph of the readme names the narration script and says the page asks its origin for it.

## The one thing to get right

The helper sets `window.__jarvisStory` as it loads. The page's bundle is written inline by `pageHtml` and an inline script runs before any deferred script, so a reading taken at mount finds nothing even when the helper is about to announce itself. The reading happens once the deferred scripts have run. The test round proved this is what decides the announced case: a mount-time reader fails it.

## Proving it

`bun test` from the repository root: 881 pass, 0 fail. Also `make build && make test`, `bun run typecheck` and `bun run lint`, all clean. The whole-suite run is the proof because a single-file run cannot reach the coverage floor.

No test, rule or fixture changes. Nothing under `.agents/` changes.
