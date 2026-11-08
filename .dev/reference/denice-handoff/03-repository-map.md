# Every reference to this repository, and what a clone of it actually contains

Read this before deciding how to get at the material. The short version: a clone of the remote contains none of the language work and none of the design record. Reading the working tree on this machine is the only route that gives you the current state today.

**Corrected on 2026-11-08.** This file was written on 2026-11-06 and said the remote carried the language up to commit `e8394e5`. That was wrong, and the section below is rewritten. The branch holding every commit of the language has never been pushed, so the remote has no MetaLang at all. On 2026-11-08 Tim approved committing the 27 October vocabulary and this handover package, which landed as `41a084c` on the same unpushed branch. Committing moved nothing off this machine; only a push would, and no push has been authorised.

## The repository

| | |
|---|---|
| Name | `4thWAIV/agent-guard` |
| SSH remote | `git@github.com:4thWAIV/agent-guard.git` |
| HTTPS remote | `https://github.com/4thWAIV/agent-guard.git` |
| Visibility | public — anyone can clone it; no access grant is needed |
| Absolute path on this machine | `/Users/timothystockstill/code/macos/4thWAIV/agent-guard` |
| Branch holding the language work | `metalang` |
| Newest commit on it | `41a084c` Record the vocabulary approved on 27 October and the Denice handover package, 2026-11-08 |
| Is that branch on the remote? | **No.** There is no `origin/metalang`, the branch has no upstream configured, and all nineteen commits of the language work are on this machine only. |
| Default branch on GitHub | `dev`. Work integrates there and is cut to `main` only at a milestone. The local `origin/HEAD` still points at `main` and is stale. |

## What is on this machine only

**The whole language is on this machine only, and committing it did not change that.** Branch `metalang` holds nineteen commits that no remote branch contains, starting at `fc5fa76` Add MetaLang, its checker and its page, with the architecture language. There is no `origin/metalang` and the branch has no upstream. A clone of `4thWAIV/agent-guard` gets no `ts/metalang` at all: no MetaLang, no architecture language, no naming dictionary, no proposal language and none of the TypeScript toolchain. This file said on 2026-11-06 that the remote's copy stopped at `e8394e5`; that was wrong, because `e8394e5` is itself on no remote branch.

The 27 October 2026 vocabulary was committed on 2026-11-08 as `41a084c`, on Tim's word, together with this handover package. That commit sits on the same unpushed branch, so the material still exists in one place. Pushing the branch is a separate action and is not authorised.

**The entire design record is untracked.** `.dev/inprocess/` holds only a placeholder file in the remote, so the whole of `.dev/inprocess/82-guard-process-workflow/` is local only — 57 files of its own, including `ROAD.md`, `rebuild/STATE.md`, `rebuild/GENERAL-FORM.md`, `human-checkins-proposal.md` and every diagram, plus an archive of two superseded languages and a vendored `node_modules` tree, which bring the raw file count to 339. Fifteen issue folders under `.dev/backlog/` are local only too: 83, 84, 88 to 93, 95, and 97 to 102.

**`CLAUDE.md` is untracked.** The project instructions a clone gets do not include it.

**`handoff/` is ignored and local only.** It holds real documents, including `handoff/guard.yaml`, which is one of the three live documents written in the architecture language, and `handoff/propose/remaining.yaml`, the proposal that carried the last eleven language decisions and whose own first line now reads enacted. The ignore rule for the folder was committed on 2026-11-08 in `41a084c`; before that a fresh clone would not even have ignored it.

`.private/` is ignored as well, and `docs/agent-guard-report.html` and `docs/silent-failure-audit.md` are untracked but not ignored. `.playwright-mcp/` was deleted on 2026-11-08.

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
