# Every reference to this repository, and what a clone of it actually contains

Read this before deciding how to get at the material. The short version: the language is on the remote now, on the `metalang` branch, which is not the branch a clone lands on. The design record is still on this machine only.

**Corrected twice on 2026-11-08.** This file was written on 2026-11-06 and said the remote carried the language up to commit `e8394e5`. That was wrong: the branch holding every commit of the language had never been pushed, so the remote had none of it. Tim then approved committing the 27 October vocabulary and this handover package, which landed as `41a084c`, and approved pushing the branch, which is now done. The section below is written against the state after the push.

## The repository

| | |
|---|---|
| Name | `4thWAIV/agent-guard` |
| SSH remote | `git@github.com:4thWAIV/agent-guard.git` |
| HTTPS remote | `https://github.com/4thWAIV/agent-guard.git` |
| Visibility | public — anyone can clone it; no access grant is needed |
| Absolute path on this machine | `/Users/timothystockstill/code/macos/4thWAIV/agent-guard` |
| Branch holding the language work | `metalang` |
| Newest commit on it | `a83f598` Say what the browser capture folder actually holds, 2026-11-08 |
| Is that branch on the remote? | Yes, since 2026-11-08: `origin/metalang` tracks it and carries all twenty commits. It is not the default branch, so a clone does not land on it. |
| Has CI ever run on it? | No. The workflow triggers only on pushes to `dev` and `main`, so pushing this branch started nothing. |
| Default branch on GitHub | `dev`. Work integrates there and is cut to `main` only at a milestone. The local `origin/HEAD` still points at `main` and is stale. |

## What a clone gets, and what is still on this machine only

**The language is on the remote, on a branch a clone does not land on.** `origin/metalang` carries all twenty commits, starting at `fc5fa76` Add MetaLang, its checker and its page, with the architecture language, and ending at `a83f598`. The default branch is `dev`, which holds no `ts/` directory at all, so reaching the language is `git clone` followed by `git checkout metalang`. Fetching that one branch works too.

No CI has ever run against any of it. The workflow triggers only on pushes to `dev` and `main`, so the push on 2026-11-08 started nothing, and the language reaches a green gate only when a pull request is opened against `dev`.

This file said on 2026-11-06 that the remote's copy of the language stopped at `e8394e5`. That was wrong at the time: `e8394e5` was on no remote branch either, and the remote had none of it.

**The entire design record is untracked.** `.dev/inprocess/` holds only a placeholder file in the remote, so the whole of `.dev/inprocess/82-guard-process-workflow/` is local only — 57 files of its own, including `ROAD.md`, `rebuild/STATE.md`, `rebuild/GENERAL-FORM.md`, `human-checkins-proposal.md` and every diagram, plus an archive of two superseded languages and a vendored `node_modules` tree, which bring the raw file count to 339. Fifteen issue folders under `.dev/backlog/` are local only too: 83, 84, 88 to 93, 95, and 97 to 102.

**`CLAUDE.md` is untracked.** The project instructions a clone gets do not include it.

**`handoff/` is ignored and local only.** It holds real documents, including `handoff/guard.yaml`, which is one of the three live documents written in the architecture language, and `handoff/propose/remaining.yaml`, the proposal that carried the last eleven language decisions and whose own first line now reads enacted. The ignore rule for the folder was committed on 2026-11-08 in `41a084c`; before that a fresh clone would not even have ignored it.

`.private/` is ignored as well, and `docs/agent-guard-report.html`, `docs/silent-failure-audit.md` and `.playwright-mcp/` are untracked but not ignored. `.playwright-mcp/` holds browser capture artifacts from sessions between September and November 2026, one console log of 66 MB among them.

## The files to read, in order

1. `ts/metalang/metalang.yaml` — MetaLang, the language that defines languages. Its own header says: "THE DECLARATIONS AT THE FOOT OF THIS FILE ARE THE VOCABULARY. Every word of the language is declared there and every script reads it from there, so there is no second copy anywhere." The comment blocks above the declarations are the dated record of what was approved and when.
2. `ts/metalang/stage.yaml` — "A sample second document, written to show that a file other than the language file can be written and checked."
3. `ts/metalang/arch.yaml` — "The architecture language — a second document written in MetaLang."
4. `ts/metalang/parts.yaml` — "The parts of this toolchain, written in the architecture language rather than in MetaLang."
5. `ts/metalang/names.yaml` — "What each language calls each form — a document written in the architecture language."
6. `.dev/inprocess/82-guard-process-workflow/ROAD.md` — the seven-step road for the architecture language and where it stands. Untracked, so it is unreadable from a clone.

Two files are deliberately not in that list. `ts/metalang/propose.yaml` says of itself "NOT APPROVED. This file is itself the thing being proposed," and `.dev/inprocess/82-guard-process-workflow/rebuild/GENERAL-FORM.md` says "Nothing here is approved." Both are worth reading, as proposals rather than as the language.

## The toolchain

Each line is the file's own top-of-file comment.

- `words.ts` — "The vocabulary, read from the language file rather than held here."
- `rulebook.ts` — "The vocabulary a document is read against, loaded from files."
- `check.ts` — "Checks a document against the APPROVED vocabulary and nothing else."
- `constraints.ts` — "What the reader makes of what is written: how a value of each primitive is read, and what a type says its values may be once it has been read."
- `expression.ts` — "A regular expression written in a file: reading one, and matching a value against it."
- `syntax.ts` — "How a document written out as YAML is coloured."
- `highlight.ts` — "What to mark in a render, and where."
- `app.tsx` — "The page, in React. Draw is the only path from data to page."
- `client.tsx` — "Mounts the page. The only thing in the bundle that touches the DOM."
- `bundle.ts` — "The bundler. One module, two shapes of output."
- `render.ts` — "Builds a document into ONE self-contained page that React draws in the browser."
- `propose.ts` — "The proposal renderer — one page from one document written in the proposal language."
- `examples.ts` — "Colours the MetaLang examples in a page written by hand."

Twelve of the thirteen source files have a test file beside them; `client.tsx`, which mounts the page, has none. `propose.test.ts` was untracked until `41a084c` committed it on 2026-11-08.

## The commands

The `scripts` block of `package.json`, verbatim:

```json
  "scripts": {
    "check": "bun run ts/metalang/check.ts",
    "render": "bun run ts/metalang/render.ts",
    "examples": "bun run ts/metalang/examples.ts",
    "typecheck": "tsc --noEmit",
    "lint": "eslint ts",
    "test": "bun test",
    "coverage": "bun test --coverage",
    "verify": "bun run typecheck && bun run lint && bun test --coverage"
  },
```

So: `bun run check <file.yaml>` checks one document against its language, `bun run render <file.yaml> <file.html>` draws its page, `bun test` runs the suite, and `bun run verify` adds the type check, the lint and coverage. The `examples` entry was uncommitted until `41a084c` committed it on 2026-11-08.

## The state of the tree as it stands

`bun test` reports 789 pass, 0 fail, 1322 assertions across 12 files, with function coverage at 98.25% overall and `propose.ts` the one file below the rest at 78.95%.

`bun run check ts/metalang/arch.yaml` reads all 21 declarations and reports "Checked, and nothing is wrong."
