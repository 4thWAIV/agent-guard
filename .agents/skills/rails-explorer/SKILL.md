---
name: rails-explorer
description: Use before judging, planning, or changing any subsystem you do not currently have PROVEN, code-level knowledge of — to come up to speed on how it ACTUALLY works right now. It is the GROUND stage's grounding step in rails-run-a-workflow. This skill is a METHOD for deriving ground truth from live code; it hands you no answers and no map, only how to explore and what to look for.
---

# rails-explorer

Derive how a subsystem works RIGHT NOW from the live code. You are given a TARGET and this METHOD — never the answers.

## Absolute rule

Trust NOTHING as an answer: not docs, not comments, not skills, not prior maps, not this skill's own phrasing, not your memory. Each is at most a HINT about WHERE to look; you CONFIRM or REFUTE it against the code as it exists now. Report inference as inference. Report a fact only with evidence: `path:line`, the actual bytes, or pasted command output with exit code.

**Verify against the live file as it is right now on disk (the working tree), never from memory** or a stale/summarized artifact — the committed version can lag your own uncommitted edits, so read the actual current file.

## Exploration steps

Use both discovery lenses: CodeGraph (`codegraph_explore`), and grep across `src`, `tests`, `analyzers`, and `eng`.

1. **Find real entry points by grepping for the OPERATION and for OWNERSHIP CLAIMS.** Code here often declares its own owner — grep `-rn "single entry point|the only path|ONE tool|source of truth|canonical"`. Then confirm no second path re-implements it.
2. **Reconcile HEAD vs working tree before calling anything "live."** `git status` / `git show --stat HEAD`. A reverted-but-restaged commit will lie about current state if you trust only one.
3. **Follow the actual call/data flow.** Read every function you traverse; note what it reads and writes (files, fields, records, surfaces).
4. **Enumerate EVERY surface/store before concluding.** Grep for the things that READ and WRITE state — the loading and reading routines, the reader or repository types, the helpers that build paths or keys — and list every store they reach. Never answer from one store. When the same value lives in more than one store, enumerate every store first, then verify each. Never declare N/N off one store.
5. **Find the extend-points, because that is where a new capability belongs.** Every codebase declares its pluggable seams somewhere — an abstract type or interface others implement, a registration call, a lookup table or map of implementations, a discovery convention such as a directory every file in which is loaded. Grep for the registration verb and for the collection that holds the implementations.
6. **VERIFY every path a doc, skill, config, or lint rule asserts actually exists.** List it. Dead directories and wrong hardcoded roots are common and high-value finds, because they silently turn a gate into a no-op.
7. **Spot superseded generations by clustering sibling and archive-suffixed directories**, then date each one from version control — `git log -1 --format=%ci -- <path>` — to tell which is live from which is abandoned.
8. **Check any step that shells out to an external service, model, or binary.** Grep for the command names and client entry points. A stale path can invoke a different, older, or forbidden target than the current one, and nothing in the calling code will say so.
9. **Read the real entry contract from the tool itself, not from prose.** Run the command's own help output, or read the argument parser or option definitions.
10. **Check symlink and config fan-out for drift.** Where the same asset is expected in more than one location — parallel skill or plugin directories, per-tool config trees, generated copies — confirm each one. Present in one place and missing in another is the failure.
11. **Confirm with a cheap probe that reads the real working-tree bytes, and PASTE the output.** One command that parses an actual record and prints the field proves its shape better than any amount of reading comments — especially to tell a per-record field from a shared one. Use whatever the repo already has to hand for this.
12. **Consult the best-practices guide before carrying a forced choice forward.** Record every guide-covered choice as `autoResolved: [{ choice, resolution, principle, evidence }]`. The array is required and empty when no approved principle resolves a choice. Leave a choice unresolved when no approved principle covers it.

## Required output

- Entry points + the true call/data flow.
- Every surface/store, and which is CANONICAL for what the downstream consumer reads.
- The canonical owner of each computation/invariant, and any duplicate or second path beside it.
- Which existing tool/validator/skill ALREADY does the job — with paths and how it is invoked.
- Every doc/comment/skill that is stale or wrong — named, with what is wrong.
- Byte-space / layer boundaries where offsets or state are valid.
- Gates/checks that exist vs. the gaps.
- `autoResolved: [{ choice, resolution, principle, evidence }]`, required and empty when none.

The ground-truth map of the subsystem as it exists NOW: entry points, data flow, every surface, the canonical owner of each job, existing assets to reuse/extend (with paths), duplicates found, and every stale/wrong doc named. Then — if asked — method notes on what exploration steps were effective, for the next explorer.

## Prohibitions

- Trust a doc/comment/skill/instruction/memory as the answer — verify against code.
- Conclude from one surface/store.
- Report inference as fact — paste `path:line`, bytes, or command output.
- Carry a prior map forward without re-verifying it THIS run.

## Failure examples

- Building a second validator/tool because nobody checked that the right one already exists.
- Trusting a stale doc/skill/comment that describes how things "used to" work.
- Concluding "absent / not backed / done" from one store when the value lives in several.
- Registering a check on the wrong surface because two same-shaped surfaces existed.
- **Second-store miss.** The same value lives in two stores and only one is verified (e.g. a registry vs a per-item sidecar file). Enumerate every surface FIRST, then verify each. Never declare "N/N" off one surface.
- **Status-field lies.** Metadata flags vs actual bytes — read the bytes.
- **Byte-space confusion.** Offsets valid in one layer's space applied in another — name the byte space in writing.
