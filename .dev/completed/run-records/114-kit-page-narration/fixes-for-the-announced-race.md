# Test round: the announced case can lose a race with the DOM library's own event

One confirmed finding from the lie-catcher. Nothing else in this round.

## The finding

On this tree, with the machine busy — `make build && make test` running alongside — `bun test` exits 1 and the announced case fails as a plain assertion, not a timeout:

> `(fail) what a drawn page says about being narrated > a page the helper announced itself on says nothing about not being narrated [3294.62ms]`, received `["This page cannot be narrated here: ..."]`, at `ts/metalang/render.test.ts:468`.

So the coverage rows for acceptance checks 1 and 16 record a proof that does not reproduce.

## Why it happens, checked against the harness

The page is right and the product is right. The race is in the harness inside `ts/metalang/render.test.ts`.

The harness builds the DOM with the page's own scripts left unrun, imports the bundle — which registers a `DOMContentLoaded` listener with `{ once: true }` — waits for the root to draw, then writes the announcement, then dispatches a `DOMContentLoaded` of its own. The DOM library fires its own `DOMContentLoaded` when it finishes parsing, on its own schedule. When the machine is loaded, that event arrives after the bundle has registered and before the harness writes the announcement, so the bundle's one listener is spent on it, finds no announcement, and draws the line correctly. The harness's own event then arrives too late to matter.

## The fix

Make the announcement arrive the way a browser delivers it rather than by writing a global between two awaits. Registering the announcement as its own `DOMContentLoaded` listener BEFORE the bundle is imported does that: listeners run in the order they were registered, so whichever event arrives first, the announcement is already written when the bundle's listener runs. A bundle that read the announcement at mount still fails the announced case, because at mount nothing has been written, which is what the earlier round proved the order must preserve.

Take that shape or a better one, but the announced case must stop depending on which `DOMContentLoaded` arrives first. The silent case never announces and must stay exactly as it is.

## Proving it

Run `bun test` from the repository root several times with the machine deliberately busy — run `make build && make test` alongside it — and show every run exiting 0, with the run count and the load stated. Then re-prove both mutations the earlier round used: a bundle reading the announcement at mount must fail the announced case, and a bundle drawing no line must fail the silent one. Revert each mutation and show the tree unchanged.

## The coverage record

Record all eighteen checks under decision `passing-row-reason`, each row's truth read off this tree, with rows 1 and 16 resting on the runs under load rather than on a single quiet run.

## What this round does not touch

Production code, every other test, the three rules, and anything under `.agents/`.
