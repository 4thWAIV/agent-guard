# Claude handoff — finish the rails reorganization

## Outcome required

Finish the existing L1 workflow repair so the reorganized rails and workflow scripts can be used. Do not restart GROUND, DESIGN, CONTRACT, RULE-PHASE, ARCHITECTURE, or TDD. Resume at the current IMPLEMENT fix round, run one complete L1 REFUTE after the repair, run GATE only if REFUTE passes, and report the result.

The authoritative contract is `./.dev/inprocess/2026-09-01-reorganize-rails/contract.md`. Read it completely before changing anything. The fixed stage order is:

```text
GROUND → DESIGN → CONTRACT → RULE-PHASE → ARCHITECTURE → TDD → IMPLEMENT → REFUTE → GATE → REPORT
```

## Current state

The original rails reorganization and the later workflow corrections are present in the live working tree. Nothing has been committed. The tree intentionally contains staged, unstaged, and untracked work from this run.

Do not reset, restore, clean, stage, unstage, commit, push, open a pull request, or move the run record. Do not touch `.obsidian/` or any unrelated dirty-tree file.

The staged checkpoint must remain exactly:

```text
a46619f180a9d7d48ea0b6ae0b029c3e00aacabebe9ae1d1f8a97ee814571c89
```

Check it with:

```bash
git diff --cached --binary | shasum -a 256
```

The latest completed IMPLEMENT result is `./.dev/inprocess/2026-09-01-reorganize-rails/implement-round-4-output.json`.

The latest completed adversary result is `./.dev/inprocess/2026-09-01-reorganize-rails/refute-round-5.json`.

Round-five status:

- IMPLEMENT reported the six previously approved repairs complete, but REFUTE found that two were not complete through their downstream callers.
- L1 REFUTE returned `FAIL`.
- GATE did not run.
- The most recent `make build` passed with 0 warnings and 0 errors.
- The most recent `make test` passed with 642 passed, 0 failed, and 0 skipped.
- All thirteen live workflow scripts passed the in-memory syntax check.
- `git diff --check` passed.

## What has been completed

The nine rails were reorganized under the approved single-owner map. `rails-run-a-workflow` now owns the staged process, and `rails-write-a-contract` owns the contract section order. `rails-explorer` exists as the live-code grounding method. The code rails contain their reorganized Practice, Violation, and Fix rules.

The workflow scripts now implement the approved fixed stage order, default L1 selection, L1/L2/L3 REFUTE panels, prior-stage evidence passed into REFUTE, the contract-backed no-rule and no-test results, selected RULE warnings for ARCHITECTURE and TDD, and required-agent retry with preservation of valid results.

The round-four repair left this exact state:

- `stage-result-contracts-have-multiple-owners` — `stage-result-contracts.js` is now the shared owner of stage result schemas and semantic validation, but duplicate forwarding wrappers remain in `implement.js` and `refute.js`.
- `run-specific-recovery-is-in-reusable-workflows` — permanent compatibility for this run's stale outputs was removed.
- `l1-tdd-omission-is-not-authorized` — L1 requires a normal or approved no-test TDD result.
- `workflow-input-unwrapping-is-copied` — `workflow-input.js` is now the sole owner of five-level argument unwrapping.
- `rule-phase-does-not-prove-all-approved-rules` — shared validation rejects missing, duplicate, and extra rule names when it receives the approved rule set, but IMPLEMENT fails to supply that set.
- `implement-accepts-forged-no-op-proof` — exact no-rule and no-test sentinel values have one owner, and alternate nonblank proof text is rejected. The shared owner still permits those no-op sentinels in nonempty results and does not verify the required contract evidence in `notes`.

Preserve the completed portions while repairing the stated remaining gaps.

## Tim's controlling instructions

Do not stop to preserve or repair records created earlier in this run when the new workflow structure makes them stale. Tim's instruction is:

> THE bookkeeping of this task iS CAUSING constant inturruptions and provide NO LASTING VALUE!
>
> WHENEVER THEY conflict ... THE bookkeeping is ABANDONED and WE DO NOT TRY TO "make it right".  THIS is the second time you have wasted my time on FUCKING bookkeeping in this session when we know we are changing the strucutre of the system as we use it.
>
> I DO NOT value this data or its history as much as I need you to FINISH these tasks without STOPING over the existing values created by the run itself being out of date to the new system.
>
> WE ARE CHANGING THE SYSTEM and moving foward and if that BREAKS prior output SO FUCKING BE IT!!!

Do not ask Tim to approve parameters or caller plumbing that are mechanically required to enforce an already-approved requirement. Tim's instruction is:

> YOU DO Not need to bring a best practice problem to me ... AND SOMETHIGN REQUIRING the paramter in order to FUNCTION is exactly what you are not supposed to be bringing to me as there IS NO DECISION to make... IT's a requirement so it's either (a) do it or (b) THE spec fails?

Do not broaden selected-warning validation into a diagnostic prefix, whitelist, predicted set, or uniqueness rule. The human supplies whichever diagnostic IDs apply to that run. Validate only what is necessary to keep each supplied value from becoming another command argument or property.

Do not add persistent workflow tests or wire workflow checks into build, test, CI, or gate paths. The contract records Tim's exact waiver:

> YEs, that is an unavoidable state we are not GOING to resolve and I'm going to have to live with ... but which I can survive becuase I know I use these scripts enough to find and fix any problems.  I DO NOT want more complexity.  I WANT working scripts that I can start putting to use.

Use one-time in-memory probes only. Do not change product code, product tests, analyzers, build scripts, CI, release behavior, external memory stores, or `.obsidian/`.

Do not actively poll background agents. Let them notify completion. Play `/System/Library/Sounds/Glass.aiff` and send a macOS notification when the complete repair-and-REFUTE work finishes.

## Work remaining

The accepted round-five findings are concrete defects in reusable workflow code. They reduce to three repair areas across seven files:

- `./.agents/workflows/stage-result-contracts.js`
- `./.agents/workflows/implement.js`
- `./.agents/workflows/rule-phase.js`
- `./.agents/workflows/tdd.js`
- `./.agents/workflows/selected-rule-warnings.js`
- `./.agents/workflows/required-agent-runtime.js`
- `./.agents/workflows/refute.js`

Do not launch a general malformed-input hardening project. Repair the findings below and the direct pass/fail cases stated with them.

### Repair area: RULE-PHASE and TDD handoff integrity

#### `implement-drops-approved-rule-set`

Accepted finding:

> IMPLEMENT validates a nonempty RULE-PHASE result without the contract's complete approved rule-name set, so the shared exact-set check is skipped and an incomplete or extra rule proof can reach the worker.

Carry the authoritative complete approved rule-name set into IMPLEMENT and pass it to `rule-phase-stage` validation. Context must be required whenever IMPLEMENT validates an L1 RULE-PHASE result. Reject missing, duplicate, extra, and incorrectly empty rule results before the implementation agent launches.

Use the minimum caller input needed to carry that already-required information. This plumbing is not a new product feature or a new human decision.

#### `normal-tdd-allows-non-red-coverage`

Accepted finding:

> The shared TDD contract and IMPLEMENT accept a normal test result whose coverage explicitly records red:false.

When `testFiles` is nonempty, require every `coverage[].red` value to be exactly `true`. Enforce this in the shared owner so producer and downstream validation use the same invariant.

#### `no-op-notes-do-not-prove-exact-contract-evidence`

Accepted finding:

> The shared no-rule and no-test result checks accept arbitrary nonblank notes instead of confirming that notes contain the required verbatim contract evidence.

Pass the expected verbatim contract excerpt to the shared validation owner and require exact containment in `notes` for no-rule and no-test results. Apply the check from `rule-phase.js`, `tdd.js`, and `implement.js`. Add only the caller context required to enforce this existing rule.

This is future workflow enforcement, not a request to repair old run records.

#### `normal-rule-phase-accepts-no-rule-sentinel`

Accepted finding:

> The normal nonempty RULE-PHASE result accepts the no-rules sentinel as buildProof.

Reject `Not applicable — the approved contract adds no rules.` when `ruleFiles` or `perRule` is nonempty. That value is valid only for the empty no-rule result.

#### `normal-tdd-accepts-no-test-sentinel`

Accepted finding:

> The normal nonempty TDD result accepts the no-test sentinel as redProof.

Reject `Not applicable — the approved contract authorizes no test files.` when `testFiles` is nonempty. That value is valid only for the empty no-test result.

### Repair area: selected warning command safety

#### `selected-rule-warning-items-are-not-command-safe`

Accepted finding:

> selected-rule-warnings.js joins array values directly into an MSBuild command without first requiring each value to be one nonblank command-safe diagnostic token.

Require every `ruleWarningIds` item to be a string containing one nonblank command-safe token before constructing the command or policy prompt. Reject whitespace, delimiters, newlines, objects, and anything that can create another shell argument or MSBuild property.

The round-five narrowing is controlling:

> The accepted defect is command safety and single-token validity only. An eventual repair must not restrict diagnostic IDs by product prefix, predicted set, or uniqueness; those extra restrictions are explicitly outside the approved selected-warning contract.

### Repair area: remove the remaining duplicate and dead paths

#### `unused-auto-resolved-validation-path`

Accepted finding:

> required-agent-runtime.js retains an autoResolvedCollections input and validation branch that no workflow calls.

Delete `autoResolvedCollections` and its no-role validation branch. Keep `autoResolved` validation on the live `validationResult` and role path.

#### `downstream-stage-validation-wrappers-are-copied`

Accepted finding:

> implement.js and refute.js each retain a local helper that wraps the same call to stage-result-contracts and rethrows its error.

Remove both local wrappers and call `stage-result-contracts` directly. Do not add another shared wrapper unless caller-specific behavior genuinely cannot remain at the caller; none has been established.

These two findings have no current runtime effect, but leaving either one guarantees another SOLID or DRY `FAIL`. They are deletions in files already inside the repair.

## Required one-time checks before REFUTE

Do not test every imaginable malformed object. Run exact one-time in-memory probes for these cases:

1. IMPLEMENT rejects one returned proof for two approved rules before launching its worker.
2. IMPLEMENT rejects an extra rule proof when the contract approves none.
3. The normal RULE-PHASE result rejects the no-rule sentinel.
4. The empty RULE-PHASE result accepts only the exact no-rule sentinel and exact contract evidence.
5. The normal TDD result rejects `coverage[].red: false`.
6. The normal TDD result rejects the no-test sentinel.
7. The empty TDD result accepts only the exact no-test sentinel and exact contract evidence.
8. `selected-rule-warnings.js` accepts arbitrary human-selected diagnostic tokens that are command-safe.
9. `selected-rule-warnings.js` rejects blank values, non-strings, whitespace, delimiters, newlines, and injected MSBuild switches.
10. `rg -n 'autoResolvedCollections' .agents/workflows/*.js` returns no matches.
11. `implement.js` and `refute.js` contain no local downstream stage-validation wrapper.
12. All thirteen workflow scripts pass the existing in-memory `AsyncFunction` syntax check after the final edit.
13. `git diff --check` passes.
14. The staged checkpoint hash remains `a46619f180a9d7d48ea0b6ae0b029c3e00aacabebe9ae1d1f8a97ee814571c89`.
15. `make build` passes with 0 warnings and 0 errors.
16. `make test` passes with 0 failed tests.

Run these checks before REFUTE. If a named check fails, return it to the same IMPLEMENT worker and continue inside IMPLEMENT. Do not stop to ask Tim about an implementation miss already covered here.

## REFUTE and finish

After all named checks pass, run one fresh L1 REFUTE panel with:

- Prove-It.
- SOLID.
- DRY.
- Laziness-auditor.
- Lie-catcher.

The adversaries must read the complete contract, the live tree, `implement-round-4-output.json`, `refute-round-5.json`, and the fresh implementation proof. They must not treat stale historical run records or the waived opening-proof bookkeeping as defects. They must not demand persistent workflow tests.

If REFUTE reports a finding, adjudicate it against live code. Do not automatically start another repair round. Report the exact result to Tim.

If REFUTE passes, run GATE with plain `make build` and `make test`. Do not pass `WarningsNotAsErrors` to GATE. Then report the result and stop. Do not stage, commit, push, open a pull request, or move the run record without new authorization.

## Completion definition

This handoff is complete when:

- The eight accepted round-five findings are repaired.
- The sixteen named one-time checks pass.
- One fresh L1 REFUTE panel returns no accepted finding.
- GATE passes with plain `make build` and `make test`.
- No product, test, analyzer, build, CI, release, memory-store, `.obsidian/`, staged-checkpoint, or unrelated file changed.
- Tim receives the raw result and no additional repair round begins without his instruction.
