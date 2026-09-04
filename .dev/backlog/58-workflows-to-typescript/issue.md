# 58 — Move the workflow scripts to a TypeScript source tree bundled with esbuild

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/58

---

## Why

The 13 scripts in `.agents/workflows/` could not execute through Claude Code's Workflow tool. Two runtime facts collide:

1. Scripts get no filesystem and no Node API — there is no `import` or `require` of a local module at runtime.
2. `workflow('name')` nesting is limited to ONE level. A child workflow calling another throws: `workflow() cannot be called from within a child workflow`.

Fact 1 is why four shared modules — `workflow-input`, `required-agent-runtime`, `stage-result-contracts`, `selected-rule-warnings` — were invoked via `workflow()`. It is the only code-sharing mechanism the runtime offers. Fact 2 is why that design could not run.

This was invisible until the scripts were executed for the first time, because they had only ever been syntax-checked. A parse check cannot see a runtime nesting limit.

## Current state — unblocked by hand, at a cost

The pipeline now runs. Every shared module was copied into each script that needs it and the four module-substitutes were retired to `.agents/workflows/_retired/`. There are now nine workflow files and zero `workflow()` calls, so the single nesting level is free for a case that genuinely needs it.

That cost 2,738 lines becoming **11,658**, across 30 copied blocks with 5 distinct block names. DRY is suspended for this area under Tim's explicit authorization, held together by a marker convention and a drift check.

## Work item 1 — move the workflow code into a proper TypeScript project

Not a loose set of `.ts` files. A real project: **`package.json` and `tsconfig.json`**, a `src/` source tree with the shared code as ordinary modules, and a build that emits the nine workflow scripts.

The shared modules become normal TypeScript modules imported the usual way. The duplication disappears at source. Source returns to roughly 2,700 lines; the `.js` files under `.agents/workflows/` become build output rather than maintained code.

**`eng/check-copied-modules.mjs` is DELETED as part of this work item.** It exists only to police hand-copied blocks. Once the source has real imports there are no copies to compare and the script has no purpose. Leaving it would be dead weight guarding a problem that no longer exists. The `// ##COPIED-MODULE-BEGIN##` / `// ##COPIED-MODULE-END##` markers go with it.

### Spike result — WORKS, with caveats

Run in an isolated worktree. The bundle was not merely parsed but **executed** end to end with stubbed globals. A depth-2 module ran as an ordinary function call and `workflow()` was called zero times.

**A plain CLI call fails.** `npx esbuild src/workflows/ground.ts --bundle --format=esm --platform=neutral` hoists inlined modules above `meta`, emits it as `var meta`, and rewrites the entry export into a trailing `export { … }` clause. The harness rejects that with `SyntaxError: Unexpected token 'export'`. No flag changes it.

**A ~60-line `build.mjs` using the esbuild JS API works.** Per entry:

```js
banner: { js: `export const meta = ${JSON.stringify(meta, null, 2)}\n\nlet __result\n` },
footer: { js: '\nreturn __result' },
entryPoints: [`src/workflows/${name}.ts`], bundle: true, format: 'esm',
platform: 'neutral', target: 'node22', charset: 'utf8', write: false
```

`meta` is authored in a sibling `<name>.meta.ts`, evaluated at build time, validated as JSON-shaped, and re-emitted as a literal in the banner. The entry exports nothing, so no trailing clause appears. The build greps its own output for `^export (?!const meta)` and throws.

| Constraint | Verdict |
|---|---|
| A — `meta` a pure literal at top | PASS with build script; FAILS with plain CLI |
| B — top-level `await` survives | PASS, no IIFE wrapper |
| C — no export but `meta` | PASS with build script; FAILS with plain CLI |

### Required source change in every script

Top-level `return` is illegal in ESM and seven scripts end with one. Both `tsc` and esbuild reject it.

The working fix: the banner declares `let __result`, the source assigns `__result = {…}`, the footer emits `return __result`. **A plain `const result = {…}` is silently tree-shaken away** because nothing references it — the assignment must target a free identifier the bundler cannot drop or rename.

### Other spike findings

- No runtime preamble above `meta`. Even with CommonJS helpers forced in, they landed below the banner. `meta` on line 1 is structurally guaranteed.
- Injected globals work from shared modules. Declare them in a `.d.ts`; esbuild leaves the bare identifier, which resolves to the AsyncFunction parameter. `tsc --noEmit` is clean.
- Name collisions handled correctly. A module's top-level `const log` was renamed to `log2` because the entry referenced the free `log`.
- `charset: 'utf8'` is required, or every em-dash becomes an escape.
- **Comments are stripped and esbuild has no option to keep them.** The current scripts are heavily commented and none of it survives. The reviewable artifact becomes the TypeScript source, not the bundle.
- Bundles come out smaller than source, because each entry tree-shakes to only what it uses.
- A build script is required for nine entry points. `--banner` is per-build, not per-entry, and each entry needs its own `meta`.

### Done when

`package.json` and `tsconfig.json` exist; the shared code lives in `src/` as imported modules with no duplication; `.agents/workflows/*.js` are build output; every root executes through the Workflow tool; no script calls `workflow()` for code sharing; `eng/check-copied-modules.mjs` and the copied-module markers are deleted.

## Work item 2 — a test system for the TypeScript project

**Depends on work item 1. Do not start it first.** There is nothing to build a test system against until the source tree exists.

Today there is **no test system for this code at all**. The repository's 642 tests are xUnit across four .NET assemblies covering `src/` and `analyzers/`; none of it can see a JavaScript file. What exists for the workflow scripts is a retyped one-line syntax check, throwaway probes written into `/tmp`, and the drift check — which work item 1 deletes.

Build a real test system for the TypeScript project and **bring coverage to 75%, the same floor the rest of the repository holds** (`eng/coverage-gate.sh`), **with a build gate** so it is enforced rather than remembered.

### Done when

The TypeScript project has a test runner and a test suite; coverage is at or above 75%; the gate fails the build below that floor, matching how the .NET coverage floor is enforced today.

### Note on the existing waiver

The contract records Tim's `workflow-runtime-test-waiver` in his words: *"I DO NOT want more complexity. I WANT working scripts that I can start putting to use."* That waiver was given when the scripts were 2,738 lines with one owner per capability, to avoid blocking a run. This work item is Tim's decision to lift it once the TypeScript project exists.

## Scope

Nine workflow files: `ground`, `design`, `rule-phase`, `architecture`, `tdd`, `implement`, `refute`, `hidden-decision-scan`, `prior-art-ledger`.

Node 23.5.0 and npm 10.9.2 are installed. The repository has no `package.json` or `tsconfig.json` today. The spike lives in a worktree at `.claude/worktrees/agent-ad5849648391239d1/spike-ts/`.