# Fix round: the acceptance-16 tests can time out

One confirmed finding from Prove-It. Nothing else in this round.

## The finding

Prove-It ran `bun test` five times. Four were green at 881 passing; one exited 1 with `Test "a page the helper never announced itself on says it cannot be narrated here, once" timed out after 5392ms (killed 1 dangling process)`, at `ts/metalang/render.test.ts:429` in `shownBy`.

Both tests at `ts/metalang/render.test.ts:446` and `:450` start a jsdom subprocess through `Bun.spawnSync`, which blocks while it runs, and each takes between 1.5 and 2.5 seconds against bun's 5 second default. Neither sets a timeout of its own. The macOS, Windows and Linux runners are slower than this machine, so a correct implementation can still go red there. Acceptance checks 1 and 16 both require the suite green.

## The fix

Give both tests an explicit generous per-test timeout, or run the subprocess without blocking. No assertion, expectation, name or skip changes, and neither case may stop being able to fail: the announced case must still fail against an implementation that reads the announcement at mount, which is what the harness order proves.

## Proving it

Run `bun test` from the repository root several times and show every run exiting 0, with the run count stated. Also `bun run typecheck` and `bun run lint`.

## What this round does not touch

Anything else. `ts/metalang/guardrails.test.ts` belongs to the rule round and has its own finding; do not edit it here.
