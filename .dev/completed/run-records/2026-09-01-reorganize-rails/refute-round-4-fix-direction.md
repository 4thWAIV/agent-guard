# REFUTE round four fix direction

## Authorization

Tim authorized one IMPLEMENT repair followed by one L1 REFUTE round:

> Go with another round.

Tim's standing authority for the shared-owner DRY refactors remains:

> THE ONE thing I have given BLANKET approval for is REFACTORING code into a shared module in ourder to avoide the DEATH of DRY violations.  DRY violations can be fixed on their own.

## Repair these accepted findings

### `stage-result-contracts-have-multiple-owners`

> GROUND, DESIGN, hidden-decision, RULE-PHASE, and TDD result contracts are defined by their producer workflows and reimplemented by refute.js or implement.js.

Extract the stage-result schemas and semantic validation into one shared workflow owner. Route producer acceptance and downstream handoff validation through it. Remove the copied result validators and helpers from `implement.js` and `refute.js`. Leave `autoResolved` validation in `required-agent-runtime.js`.

### `run-specific-recovery-is-in-reusable-workflows`

> refute.js and required-agent-runtime.js contain compatibility branches for this run's noncanonical GROUND, DESIGN, and RULE-PHASE records.

Preserve the original historical records. Produce canonical current GROUND, DESIGN, and RULE-PHASE handoffs for this run, then remove `groundResultForRefute`, `historicalResult`, optional success metadata, and `caseInsensitiveLens` compatibility from reusable workflow code.

### `l1-tdd-omission-is-not-authorized`

> implement.js permits TDD to be omitted at L1 even though the approved workflow requires TDD and defines a no-test result for contracts that authorize no test files.

Remove TDD from `omittedStageAuthorizations`. At L1, require either a normal TDD result or the approved no-test TDD result. Retain only the approved L1 RULE-PHASE exception and the existing L2 conditional-TDD behavior.

### `workflow-input-unwrapping-is-copied`

> The same JSON argument-unwrapping block exists in all eleven workflow scripts, including both shared owners added by this change.

Extract the existing five-level JSON unwrapping into one shared workflow-input owner. Make all eleven scripts call it. Preserve the current accepted inputs and caller-specific error text.

### `rule-phase-does-not-prove-all-approved-rules`

> A RULE-PHASE result with proof for only one of two approved rules passes validation.

Require `perRule[].rule` to match the complete `rulesToAdd[].rule` set exactly, with no missing, duplicated, or extra rules. Put reusable exact-set validation in the existing shared runtime owner when doing so avoids another copy.

### `implement-accepts-forged-no-op-proof`

> IMPLEMENT accepts any nonblank proof string for empty no-rule and no-test results instead of requiring the approved exact sentinel values.

Give each exact no-rule and no-test result contract one shared executable owner used by its producer and by IMPLEMENT. Reject any other proof before launching the worker.

## Boundaries

Do not repair `opening-proof-is-incomplete`. Tim excluded that historical bookkeeping from the required repair:

> I don't care that it was summarized we will probably have to do more runs later right?

Do not add persistent workflow tests or wire workflow checks into build, test, CI, or gate paths. Do not change product code, tests, analyzers, build scripts, CI, release behavior, external memory stores, or `.obsidian/`. Do not commit, stage, unstage, push, open a pull request, or move the run record. Stop and report any unexpected condition instead of expanding the work.

Use only one-time in-memory probes for the repaired workflow behavior. Preserve the staged checkpoint fingerprint `a46619f180a9d7d48ea0b6ae0b029c3e00aacabebe9ae1d1f8a97ee814571c89`.

After IMPLEMENT, run one complete L1 REFUTE panel. Report its result and stop. Do not begin another repair round.
