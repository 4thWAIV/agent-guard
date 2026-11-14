# What the five adversaries found

Prove-It, the laziness auditor and the lie-catcher each returned FAIL. SOLID and DRY passed with observations only. Every finding below was checked against the live tree before it was accepted.

## A test can fail on a slower machine, and did here

Prove-It ran `bun test` five times. Four runs were green at 881 passing; one exited 1 with `Test "a page the helper never announced itself on says it cannot be narrated here, once" timed out after 5392ms`. The two tests at `ts/metalang/render.test.ts:446` and `:450` each start a jsdom subprocess through `Bun.spawnSync`, which blocks while it runs, and they take between 1.5 and 2.5 seconds against bun's 5 second default. On the slower macOS, Windows and Linux runners that margin is not enough, so CI can go red on work that is correct.

Confirmed. The fix is an explicit generous timeout on those two tests, or spawning the subprocess without blocking, with no assertion touched.

## A rule file names a path that will stop existing

`ts/metalang/guardrails.test.ts:3` says the rules were "approved as written in `.dev/inprocess/114-kit-page-narration/contract.md` under `Rules to add`". `rails-real-work` rail 11 bans shipped code naming a path under `.dev/inprocess/` or `.dev/completed/`, and this one stops resolving the moment this folder moves to `completed/run-records/` on shipping.

Confirmed. The fix is to quote the contract's wording in that comment without naming the path.

## The Success definition is unmet, and I accepted that myself

The contract's Success definition says, without qualification, that a selector naming one member of one declaration matches exactly one element. The lie-catcher built a document in the architecture language with one element carrying two members both named `Name`, checked it clean — no problems, no blockers — drew the page, and found `[data-declaration="IGuard"] [data-member="Name"]` matching two elements.

The cause is `ts/metalang/app.tsx:746-755`, where each row's identity is its own name value with no uniqueness handling, and `ts/metalang/arch.yaml`, which puts no distinct constraint on a declaration's members, so the duplicate is a valid document.

Confirmed, and the finding is against me rather than the code: I filed this as issue 120 and recorded the gap as accepted. Accepting a Success-definition criterion as unmet is Tim's, not mine. It goes to him.

## The scan that hunts undecided choices ran after the work, not before it

`rails-run-a-workflow` has the hidden-decision scan run before the contract locks. For this contract it had never run, and I ran it after the implementation was committed. The lie-catcher confirmed the timing from the workflow journals rather than from my note: the scan is stamped 13 November 19:05, while the rule rounds, the architecture stage, both test rounds and the first implementation round all ran between 11 and 13 November before it.

Confirmed. All three of its findings were surfaced and handled — one became Tim's decision `helper-absent`, one was wrong on its facts and left issue 121, and one is issue 120 — but whether a run that reached its gate this way is accepted is his call, not mine.

## What happens to each

The two mechanical findings are fixed by the round that owns each file: the test round owns `render.test.ts`, the rule round owns `guardrails.test.ts`. The two the lie-catcher raised go to Tim as decisions `overload-target` and `scan-ran-late`, because both are acceptances I made that are his to make.

## Two more, from the round that fixed the flaky test

The timeout fix itself passed its test-quality and duplication reviewers and is committed at c10d147: both page-reading cases now state a sixty-second budget, names and assertions untouched, eight consecutive whole-suite runs exiting 0, and two mutations re-proving each case can still fail. The lie-catcher refused the round on two counts, neither about the fix.

**The record says passing where the approved wording says RED.** Decision `green-criterion` reads: "A criterion that only guards behavior this contract requires to keep working is recorded in the coverage list as `red: false` carrying the reason in `whyPassing`; every other criterion is still RED." A criterion that passes because its code was already built is neither of those. It cannot be RED, because the code exists, and it is not a guard on behaviour that merely keeps working. Both rounds since the implementation have hit this, and both recorded the truth and were refused for it. The wording is Tim's and only he widens it.

**This was a third run of the test stage, and nothing authorized it.** The contract carries two re-run authorizations, `impact-test` and `when-the-page-says-so`, and each says one round. Both are spent. I ran this one on my own reading that a fix round driven by a confirmed reviewer finding is part of the loop, and the rails say the opposite in plain words: a fix round loops IMPLEMENT and REFUTE only. The work is good and the authorization is missing, which is mine, not the author's.

The rule round that would fix the folder path in `ts/metalang/guardrails.test.ts` is held for the same reason: its authorization also says one round and is spent. Nothing further runs until Tim answers.

Asked as decisions `passing-row-reason` and `fix-round-authority` in a second package, so the two already with him are not re-asked.
