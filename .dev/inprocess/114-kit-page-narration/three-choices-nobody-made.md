# Three forced choices the contract does not answer

The hidden-decision scan has no recorded result anywhere for this contract — not in the work folder and not in any workflow journal — so it was run before the adversary panel rather than pointing the panel at a file that is not that stage's result. It found three choices this work forces that no decision answers. Each is checked against the live tree below.

## The page asks for a script that may not be there, and says nothing when it is not

`ts/metalang/render.ts:68` holds the tag and `:84` writes it into every page the kit draws, unconditionally. Away from JARVIS that request is a 404 the browser ignores: the page draws, is never narratable, and nothing says so. On another local server, that server's script at `/api/packages/story.js` runs inside a page that draws document text.

This is the rule in `CLAUDE.md` — nothing may ever fail silently — inside the kit's own output. The kit readme at `ts/metalang/kit.ts:106` still tells a taker that a drawn page "needs nothing from this folder afterwards, so a drawn page travels on its own", which stops being true the moment the tag ships.

GROUND flagged it (`ground/facts.json`: whether a missing `story.js` should be diagnosed on the page or stay silent is a design choice) and DESIGN flagged it again (`design/proposals.json`: flag it for Tim, do not decide). No decision answers it.

The escape hatch is closed from the other side: JARVIS checks that the exact tag string appears in the page, so an `onerror` or an integrity attribute on the tag itself would fail that check. A report has to come from the bundle.

## Nothing proves a released kit is the one CI built

`.github/workflows/ci.yml:599` zips `evolang-kit.zip` and `:634` uploads it in the release's asset list, with no checksum file and no signature, while every binary beside it goes through `eng/ci-stage-and-cosign.sh` and ships a `.sha256` and a cosign bundle. JARVIS pins the SHA-256 of the exact uploaded bytes against the version, the commit and the repository, and today the only route for those four values is a session computing the digest by hand and messaging it.

That number is the completion evidence this whole lane turns on, and nobody but the reporter can re-derive it.

## Two members with the same name inside one declaration both carry the same identity

`ts/metalang/app.tsx:748` writes each row's own name value with no uniqueness check, and `ts/metalang/arch.yaml` puts no distinct constraint on `element.members`, so both rows carry the same `data-member`. MetaLang already has the constraint that would refuse it, at `metalang.yaml:814`, and it is simply not applied here.

The contract's acceptance checks 3 and 5 cover a member name repeated across two declarations, which the tests prove. Inside one declaration is outside what the contract asked for, so the work meets its checks. What it does not meet is the Success definition's general sentence, because a beat pointing at an overloaded member matches two rows, the helper reports the target as ambiguous, and the beat points at nothing.

This one is filed as issue 120, whose folder is `.dev/backlog/120-overloaded-member-identity/`, rather than put to Tim: the contract's checks do not require it, the fix changes the published identity format, and the scan's own recommendation is to accept the ambiguous-target report now and choose the format deliberately later.

## What is being done with them

The first two go to Tim as decisions, because each changes something that outlives this work — what every drawn page does, and what the release publishes. The third is an issue. The adversary panel waits until the first two are answered: it would otherwise judge a surface that is about to move.
