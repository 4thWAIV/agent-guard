# Three forced choices the contract does not answer

The hidden-decision scan has no recorded result anywhere for this contract — not in the work folder and not in any workflow journal — so it was run before the adversary panel rather than pointing the panel at a file that is not that stage's result. It found three choices this work forces that no decision answers. Each is checked against the live tree below.

## The page asks for a script that may not be there, and says nothing when it is not

`ts/metalang/render.ts:68` holds the tag and `:84` writes it into every page the kit draws, unconditionally. Away from JARVIS that request is a 404 the browser ignores: the page draws, is never narratable, and nothing says so. On another local server, that server's script at `/api/packages/story.js` runs inside a page that draws document text.

This is the rule in `CLAUDE.md` — nothing may ever fail silently — inside the kit's own output. The kit readme at `ts/metalang/kit.ts:106` tells a taker that a drawn page "needs nothing from this folder afterwards, so a drawn page travels on its own". That sentence stays true — it is about the kit folder — but it is also the only thing the readme says about what a drawn page needs, and it does not mention that the page reaches out to its origin for a script.

GROUND flagged it (`ground/facts.json`: whether a missing `story.js` should be diagnosed on the page or stay silent is a design choice) and DESIGN flagged it again (`design/proposals.json`: flag it for Tim, do not decide). No decision answers it.

The escape hatch is closed from the other side: JARVIS checks that the exact tag string appears in the page, so an `onerror` or an integrity attribute on the tag itself would fail that check. A report has to come from the bundle.

## The release digest finding was wrong, and this is what is actually true

The scan reported that nothing proves a released kit is the one the build produced, and that the only route for the four values JARVIS pins is a session working the digest out by hand. The fresh package author refused to write that vote and was right to: JARVIS already does the verifying.

`jarvis/language_kit_updates.py:89` takes the asset's digest from GitHub's own release metadata, `:90` resolves the tag to its commit, `:91` reads that commit's check runs, and `jarvis/language_kit.py:194` refuses downloaded bytes whose digest is not that digest. A stale number, a typo or an asset re-uploaded afterwards is caught there, not missed.

What is true is narrower and is not a decision for Tim. Initial adoption is pinned to three constants in `jarvis/language_kit.py:29-31` — `PINNED_VERSION`, `PINNED_COMMIT` and `PINNED_DIGEST` — and `language_kit_updates.py:102` refuses any first install that does not match them. Someone updates those three by hand from the release, which is what Diagnostics is waiting for. Publishing a checksum file beside the zip would not change that step; it would add a build-produced record a reader can see and a value independent of GitHub's metadata. That is an improvement nobody has decided on, filed as issue 121 rather than put to him, because nothing forces it.

Writing the unverified claim into this record and into a package handoff was my error. The author caught it before it reached him.

## Two members with the same name inside one declaration both carry the same identity

`ts/metalang/app.tsx:748` writes each row's own name value with no uniqueness check, and `ts/metalang/arch.yaml` puts no distinct constraint on `element.members`, so both rows carry the same `data-member`. MetaLang already has the constraint that would refuse it, at `metalang.yaml:814`, and it is simply not applied here.

The contract's acceptance checks 3 and 5 cover a member name repeated across two declarations, which the tests prove. Inside one declaration is outside what the contract asked for, so the work meets its checks. What it does not meet is the Success definition's general sentence, because a beat pointing at an overloaded member matches two rows, the helper reports the target as ambiguous, and the beat points at nothing.

This one is filed as issue 120, whose folder is `.dev/backlog/120-overloaded-member-identity/`, rather than put to Tim: the contract's checks do not require it, the fix changes the published identity format, and the scan's own recommendation is to accept the ambiguous-target report now and choose the format deliberately later.

## What is being done with them

The first goes to Tim as a decision, because it changes what every page the kit draws does. The second was wrong as reported and leaves an improvement filed as an issue. The third is issue 120. The adversary panel waits until the first is answered: it would otherwise judge a surface that is about to move.

## The answer, and what it needs

Tim answered `helper-absent` with `page-says-so` on 2026-11-13 at 19:28, in pkg-fac2f7a24fe3: the page's own bundle waits for the helper to announce itself and draws one plain line when it did not arrive, and the kit readme gains a sentence saying the page reaches out for that script.

That is new behaviour, so it needs an acceptance check, and a check needs a test. The only agent allowed to write a test is the test stage, which has finished for this contract, and `rails-run-a-workflow` reserves restarting a finished stage to him: "Re-running any completed stage is the human's decision and needs their explicit yes — never the orchestrator's own call." The implementation worker cannot write it; neither can the orchestrator.

So one thing is left for him, and it is about timing rather than design: the behaviour is folded into this work before the release, or the kit releases as it stands and the behaviour becomes its own issue immediately after. Folding it in holds the release by roughly the length of a test round and an implementation round. Releasing first unblocks Diagnostics and the architecture package now, and leaves every page the kit draws silent about a narration it cannot do until the follow-up ships.

Asked as decision `when-the-page-says-so` in a package on `work-0ce6071af7d5`.
