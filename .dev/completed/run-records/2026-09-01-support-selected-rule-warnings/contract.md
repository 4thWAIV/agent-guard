# Support selected RULE diagnostics as warnings during TDD

## Decisions

### Selected RULE warnings need an argument

> “We'll need an arrgument that can be supprised to make RULE warnings as warnings again.”

Tim approved the work with:

> “Yes, okay that's fine... Stage them so you have a revert point but do not commit and then have the L2 run to support the selected RULES as warnings.”

### Selected RULES are supplied by the human for the exact case

> “"selected RULES" means whatever RULES the human supplies for that exact case at that exact time and which cna not be predected now as apposed to a blanking turn all to warnings... was there another term that would have been more clear?”

> “I CAN NOT have you misinturpet things without getting clarity and going off crwating requirements I HAVE NOT APPROVED.”

### This L2 uses only the Lie-catcher

> “START the L2 WITH ONLY lie-catcher to FIX ONLY the EXACT changes AND MINIMAL changes necissary to provide the selected RULEs as warnings that you SHOWED me ....”

> “DO NOTHING ELSE>?”

### Stop instead of expanding the work

> “DO NOT allow wandering if SOMETHING else comes up you must stop AND REPORT not decide to do it.  I MAY NOT WANT IT DONE and you will never know unless you ask.”

## Rules to add

None. This run changes workflow support for an existing MSBuild warning property and does not add an analyzer rule.

## The standard / what we're building

`ruleWarningIds` carries whatever diagnostic IDs the human supplies and approves for that exact case. It has no predefined diagnostic prefix, regex, or predicted set. The selected diagnostics remain enabled and visible but are not promoted from warnings to errors during that TDD build. Every unselected warning remains governed by the repository's normal warnings-as-errors policy.

## Success definition

ALL criteria met AND no errors in the system as a result of the change.

`tdd.js` converts the human-supplied list to the MSBuild-safe `WarningsNotAsErrors` argument without filtering the IDs by prefix or rejecting duplicates, uses no override by default, and never changes GATE behavior.

## Surfaces

- `.agents/skills/rails-run-a-workflow/SKILL.md`
- `.agents/workflows/tdd.js`
- `.dev/inprocess/2026-09-01-support-selected-rule-warnings/`

## Reuse ledger

| Capability | Ruling | Evidence |
|---|---|---|
| Selected-rule warning support during TDD | `new` | CodeGraph found no existing workflow support. `rg -n 'WarningsNotAsErrors|ruleWarningIds' src tests analyzers eng .agents --glob '!**/bin/**' --glob '!**/obj/**'` returned no match. Reuse MSBuild's existing `WarningsNotAsErrors` property and `tdd.js`'s existing `args` parser; do not add a second build path or change build configuration. |

## What to do

- Add optional `ruleWarningIds` input to `tdd.js`.
- Treat missing `ruleWarningIds` and `ruleWarningIds: []` identically: use the existing plain `dotnet test` command.
- Do not restrict the supplied diagnostic IDs by prefix, regex, predicted set, or uniqueness. Human approval for that exact case is the selection authority.
- Convert approved IDs to one command argument: `-p:WarningsNotAsErrors=<IDs joined with %3B>`.
- Require the contract used by TDD to contain Tim's verbatim approval for every selected diagnostic ID to remain a warning during TDD. Stop when that approval is absent.
- Keep the selected diagnostics enabled and visible as warnings. Do not use `NoWarn`, a suppression, an `.editorconfig` severity change, or either warnings-as-errors property set to `false`.
- Add the reusable policy to the TDD stage in `rails-run-a-workflow`. Keep the executable argument construction in `tdd.js` only.
- Keep the normal closing `make build` and `make test` commands unchanged and free of `WarningsNotAsErrors`.

## What the agent MAY do

- Edit only the two workflow surfaces listed above.
- Add L2 proof and verdict files only inside this run-record directory.

## What the agent MUST NOT do

- Do not edit any other skill, workflow, source, test, analyzer, build, CI, or reference file.
- Do not change stage order, RULE-PHASE RED/GREEN behavior, existing schema gaps, analyzer ID allocation, or any half-ready work from the staged baseline.
- Do not edit, unstage, revert, or re-stage the existing index snapshot.
- Do not touch `.obsidian/`.
- Do not commit, push, or open a pull request.
- Do not run a TDD stage, Prove-It, a harness, build, test, or any adversary other than the one Lie-catcher Tim required.
- Stop and report any unexpected issue. Do not fix, redesign, broaden, or work around it.

## Acceptance

1. `git diff --name-only` lists only `.agents/skills/rails-run-a-workflow/SKILL.md`, `.agents/workflows/tdd.js`, and files under this run-record directory.
2. `tdd.js` accepts missing or empty `ruleWarningIds` and leaves the TDD command exactly `dotnet test`.
3. `tdd.js` converts `['AG0117', 'CS0219']` to `-p:WarningsNotAsErrors=AG0117%3BCS0219`.
4. `tdd.js` contains no diagnostic-prefix regex and does not reject duplicate IDs.
5. The TDD prompt requires Tim's verbatim approval for every selected ID and stops when the approval is absent.
6. The TDD prompt keeps selected diagnostics visible, forbids suppression, and uses the override only for the TDD build.
7. `rails-run-a-workflow` owns the same TDD policy and states that GATE uses the normal commands without the override.
8. `rg -n 'NoWarn|severity = none|TreatWarningsAsErrors=false|CodeAnalysisTreatWarningsAsErrors=false' .agents/workflows/tdd.js` finds no newly added bypass.
9. The Lie-catcher returns PASS against the decision, scope, approval, and suppression boundaries.

## Level

L2, as Tim required: “then have the L2 run to support the selected RULES as warnings.”

## Scope

Scope changes ONLY by the human editing the contract — or, when the human is unavailable and has given explicit prior authorization for exactly this extension, by recording that authorization verbatim as the change's ruling provenance and top-lining it.
