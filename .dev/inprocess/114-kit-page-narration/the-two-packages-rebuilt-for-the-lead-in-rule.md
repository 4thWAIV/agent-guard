# The two packages rebuilt for the lead-in rule

Tim's order, relayed by Diagnostics on 2026-11-12 at about 23:15: every decision in every package must, before its choices and on its own slide, state the question, explain why it needs Tim's decision rather than an agent's, and show the evidence needed to decide. Diagnostics ruled two of this session's decisions short of it: `green-criterion` in "Unblock the kit acceptance tests", whose question is also hard to read on its own, and `sign-interface-surface` in "Sign the presence and grants interfaces", which shows what signing commits him to and not why an agent cannot sign it. `jsdom-types`, `loaded-reuse` and `grant-why` pass.

## Every one of the five is still unanswered

The live JARVIS answers, asked through `work_report.py answer`: `jsdom-types` and `green-criterion` on pkg-f150a25e3b73, and `sign-interface-surface`, `loaded-reuse` and `grant-why` on pkg-dedc4434d455, each reply "has not been answered yet".

Diagnostics' message says `jsdom-types` is answered by jsdom-31 and must not be asked again. That answer is Tim's answer to a different question: `dom-library` on pkg-eeea4f159ee9, recorded as `jsdom-31` at 21:06 on 2026-11-12, which chose the library. `jsdom-types` chooses how its import is typed, and nothing has answered it. It stays in the rebuilt package.

## Unblock the kit acceptance tests, version 1.0.1

Both decisions stay. `green-criterion` gains the lead-in slide, and its question is rewritten to stand on its own.

Why the answer is Tim's: the check is part of the rails, whose job `rails-read-me` states as "stop a lazy AI from disabling or cheating the guard and the golden build". The check exists to stop a stage handing in tests that do not fail, so the agent is the party it constrains, and an agent loosening it is the move the rails exist to catch. `rails-read-me` also puts it on his side of the line: substantive work is "anything that mutates state or behavior, ships or changes code, claims a fix, adds a capability, or changes a validator / gate / process". And it outlives this run: the block is byte-identical in nine of the eleven files in `.agents/workflows/`, so the edit lands in all nine and governs every future contract's TDD stage, not just issue 114's.

The evidence the slide must carry: the check `itemsAllTrue: [{ field: 'coverage', itemField: 'red' }]` at `.agents/workflows/tdd.js:1118`, inside the `tdd-author` case of the `stage-result-contracts` copied module; the prompt at `.agents/workflows/tdd.js:1408` asking the author for "whether it is RED now"; the rejection `coverage[6].red must be exactly true when testFiles is not empty`; the five of twelve acceptance criteria that cannot honestly fail; and `eng/check-copied-modules.mjs`, which requires every copy of the block to be byte-identical.

## The signature ask does not go back in a deck

`sign-interface-surface` is dropped. He refused that route on 11 November, recorded in `approval-belongs-in-the-architecture-package.md`: "pkg-dedc4434d455 stays unopened; it is the separate signature deck he is refusing, and a sender cannot withdraw a package." `joint-sequence-to-acceptance.md` records where that approval now waits: it needs an exact signature representation in the language, the ARCHITECTURE stage emitting and binding it, and JARVIS projecting it, and the execution plan assigns that amendment to Planning, who is unreachable, so it needs Tim's authorization directly. Rebuilding the deck with a better lead-in would put the refused ask back in front of him.

`loaded-reuse` and `grant-why` are still live, still unanswered, and already asked, so they cannot be retracted or decided here. They were only ever carried by the package he will not open, so they move to a deck of their own titled "Two silent-failure choices", keeping the package name `sign-the-presence-and-grants-interfaces` so the refused 1.0.1 closes as superseded. Supersession is by package name and sender, at `jarvis/package_inbox.py:593`; the title is what he says to open it, and an explicit `name` beside a new `title` is accepted by the factory.

## What this does not do

It does not withdraw a package: there is no sender route, and `work_report.py` refuses `decision` and `withdraw` with the instruction to ask with a package. It does not answer anything, and it does not start the exact-signature amendment.
