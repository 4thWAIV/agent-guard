# 58 — what changed in conversation on 2026-10-14

**The GitHub issue is the authority on the work. This document records what Tim decided in conversation and is subject to his approval.**

This item stays on the backlog. Nothing in it has been built.

## The toolchain it needed now exists

The language work under issue 82 moved to TypeScript on 2026-10-14, and building it created the project this item was going to have to create first: `package.json`, `tsconfig.json`, `bunfig.toml` and `eslint.config.js` at the repository root, with bun as the package manager, the bundler and the test runner, `tsc --noEmit` as a separate type-check step, and a `typescript` job in CI across macOS, Windows and Linux that the `gate` job now requires.

Tim's instruction was that the structure had to support moving the workflow scripts later. So when this item runs, the project already exists and the work is the conversion itself.

## The build mechanism is built and tested

`ts/metalang/bundle.ts` holds `bundleWorkflowScript`, which produces the shape the Workflow runtime demands. No workflow script is built through it yet and it has no entry points; it exists so this item is a matter of adding them.

It works the way the issue's spike recorded. The banner emits `export const meta = {...}` followed by `let __result`; the body assigns `__result` and exports nothing; the footer emits `return __result`. Nothing rewrites the bundler's output afterward, because the entry never produces a trailing export clause to rewrite.

Two things the spike found are carried as behaviour rather than as notes. A plain `const result = {...}` in a body is dropped by the bundler because nothing reads it, so a body must assign `__result`. And the build checks its own output for an export other than `meta`, for the meta literal being first, and for the trailing return, and refuses output that fails any of the three.

The underlying fact, established twice from separate runs: a file with `export` cannot carry a top-level `return`, so the script format the Workflow runtime requires is not valid JavaScript and no bundler can emit it directly. The banner and footer are how it is produced in one pass.

## Corrections this item needs before it runs

**Its scope names nine workflow files. There are eleven.** `preflight-contract` and `refute-readiness` were added after it was filed on 2026-09-02.

**Work item 2, the test system, is already partly answered.** Tim's instruction on 2026-10-14 was that tests come in with the work and stay above the project's coverage floor, so that CI does not cost later. That is in force for the language work now: the floor is 75 percent in `bunfig.toml`, matching the hard floor `eng/coverage-gate.sh` holds for the .NET side, and it fails the run rather than reporting. The same floor applies when these scripts move. What remains of work item 2 is writing tests for the eleven scripts themselves.

## Open — awaiting Tim's decision

**The eleven workflow scripts currently carry staged, uncommitted edits** to the `required-agent-runtime` shared block, adding a non-blocking `observations` array to the reviewer verdict schema — the same edit made eleven times. They sit on branch `74-pre-move-system-services`. Whether that is committed before this item runs, or carried into the TypeScript source as part of the conversion, is not decided.

## Consequence to weigh before this item runs

esbuild and bun both strip comments, and neither can keep them. The eleven workflow scripts are heavily commented and none of that survives into `.agents/workflows/*.js`. After the conversion, the file anyone reads to review a workflow is the TypeScript source, not the built script.
