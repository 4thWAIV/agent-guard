# Six things proposed or built that already existed

Written 26 October 2026, at Tim's request for an itemised record rather than a summary. Each entry
says what was duplicated, when, whether it reached code, what has to be undone, and how certain the
finding is.

The common cause in all six: starting from a list of things to build and asking whether each was
possible, instead of starting from what exists and asking whether it was already done. The check
that exists to catch this is `rails-dry-code` rail 1, a locked law, and it was not run on any of
them. Issue 96 records that the check's lenses stop at `src`, `tests`, `analyzers` and `eng`, so it
would not have reached the languages in `ts/metalang` even if it had been.

---

## 1. A source reader for each language

**When.** Proposed before 2026-10-25, withdrawn 2026-10-25 when Tim asked why a new method of
enforcing architectural decisions was being built when each language already has its own.

**Where.** A plan only. No code.

**What it duplicated.** The 57 Roslyn analyzer rules in `analyzers/AgentGuard.Analyzers`, which run
as build errors on macOS, Linux and Windows, and the type-aware ESLint fence in `eslint.config.js`.
Both already enforce architecture; the reader was a second enforcement mechanism beside them.

**What has to be undone.** Nothing. `.dev/backlog/91-arch-language-step-6-find-in-source/PLAN.md`
and `ROAD.md` record it as withdrawn, with the date and reason.

**How certain.** Certain. The analyzers and the ESLint config are in the build and were before this
was proposed.

**Degree.** A whole plan, costed at 6 to 9 days, put to Tim as a decision. Nothing was built.

---

## 2. A Roslyn analyzer that reads the architecture document, with a flat file format and six rule ids

**When.** Written and withdrawn on 2026-10-26, after Tim asked how five to seven days could be
needed for something the repository already has.

**Where.** A plan only. No code.

**What it duplicated.** The same analyzers again, in a different shape. It also invented a second
file format — a flat file the analyzer would read — when the architecture document is already YAML
and `AdditionalFiles` is already declared in `AgentGuard.Analyzers.csproj`.

**What has to be undone.** Nothing. Recorded as withdrawn in the same two documents.

**How certain.** Certain.

**Degree.** A whole plan, costed at 4½ to 6½ days, with six new analyzer rule ids attached. Nothing
was built. This is the worse of the first two, because it was written the day after the first was
withdrawn for the same reason.

---

## 3. ast-grep as one tool reading all four languages

**When.** Proposed and withdrawn on 2026-10-26, after Tim said to use the normal custom linting
system each language already has.

**Where.** A plan only. No code, and no dependency added.

**What it duplicated.** The standard linter each language already ships with, and its own mechanism
for custom rules: Roslyn analyzers in C#, an ESLint plugin in TypeScript, pylint checkers in Python,
dylint lints in Rust.

**What has to be undone.** Nothing. The settled answer is issue 93, whose documents record ast-grep
as rejected and why.

**How certain.** Certain for the rule-writing job. ast-grep genuinely covers four languages; it is
simply not the normal system any of them uses, and three of the four already had a fence here.

**Degree.** A whole plan, and a dependency that came within one answer of being approved.

---

## 4. `outside` — a closed set of nine types a document may name but does not own

**When.** Proposed 2026-10-26. Withdrawn in conversation the same day when Tim asked why an import
was not used instead.

**Where.** A proposal. **Still written in `handoff/propose/remaining.yaml` and
`handoff/remaining/arch_lines.yaml`,** because Tim asked that the page not change while he read it.

**What it duplicated.** MetaLang's `external` declaration, whose own description is that it gives
the definition and leaves the implementation out. Two further faults found with it: the nine names
are this project's borrowed types rather than any project's, so a closed set bakes one project into
the language; and three of the nine — `string`, `number`, `bool` — are already in the built-in types
proposed beside them.

**What has to be undone.** The decision `name-the-types-from-outside` comes out of the proposal, and
the `outside` entity and `outsides` enum come out of the scaffold, along with the worked example
`VerdictInUse` and the `borrowing` and `borrowingmember` entities that exist only to draw it. What
replaces it is letting `external` hold a type, so a document declares the outside types it uses.

**How certain.** High, with one qualification stated plainly: `external` today can only hold a
`cast`, so it does not yet hold a type. The duplication is of purpose rather than of a line already
written, and the fix is to extend the existing word rather than to add a parallel set beside it.

**Degree.** One of five items in a live proposal, carrying a word, a worked example and lines.

---

## 5. Four of the seventeen built-in types

**When.** Proposed 2026-10-26. Found the same day while checking the remaining four items after the
`outside` finding.

**Where.** A proposal. **Still written in `handoff/remaining/arch_lines.yaml`.**

**What it duplicated.** MetaLang already declares `boolean`, `byte`, `uuid` and `string`. The
proposal adds `bool`, `bytes`, `uuid` and `utf8` beside them.

**What has to be undone.** The collision has to be resolved before the item is approved. The likely
answer is that the exact case names only what MetaLang cannot already say — the widths, `int8`
through `uint64`, and the fractional types — and reuses the language's own words for the rest. That
has not been worked through and is not yet proposed.

**How certain.** Certain that the names collide. Less than certain that it is a true duplication:
MetaLang's types are what a document may hold, and these are what a described program's member
carries at a determined width. They are two levels, and the same name at both is how a reader stops
knowing which one a line means. Either way it cannot ship as written.

**Degree.** Four of seventeen words inside one of five items in a live proposal.

---

## 6. `bodiesReachedBy` in the proposal renderer

**When.** Written 2026-10-26, as part of the fix for marks being dropped from a drawn vocabulary.

**Where.** **Code, live in the repository now** — `ts/metalang/propose.ts`, 41 lines.

**What it duplicated.** The import walk `rulebookFor` already performs in `ts/metalang/rulebook.ts`:
the same `fileForScope` resolution, the same read of each document, the same recursion into what
each one brings in, and the same seen-set so a scope reaching round to itself ends. `rulebookFor`
also already collects the documents it loads, in order, and discards them — which is exactly what
the renderer needed.

**What has to be undone.** Have the rulebook return the documents it read, and delete the second
walker. This changes what a shared function hands back, so it needs Tim's approval before it is
made.

**How certain.** Certain. The two walks were read side by side.

**Degree.** The only one of the six that reached the repository. It works and is covered by two
tests, so nothing is broken; what is wrong is that the same walk now exists twice, and the next
change to how a scope resolves has two places to be made.

---

## What has been undone, and when

Tim ordered the cleanup on 27 October 2026. All three of the live items are done.

**Item 4, `outside`.** The entity, the `outsides` enum, the `borrowingmember` and `borrowing`
entities and the `VerdictInUse` worked example are deleted. The proposal now asks instead for one
word added to MetaLang's existing `external`, so it may hold a type and not only a conversion.

**Item 5, the colliding type names.** Two of the seventeen named something MetaLang already
declares: `bool` beside MetaLang's `boolean`, and `uuid` beside MetaLang's `uuid`. Both are out of
the architecture's set, which is now fifteen. Every architecture document brings MetaLang in, so a
member carrying a boolean or a uuid names MetaLang's own declaration rather than a second word for
the same thing. `bytes` and `utf8` stayed: MetaLang's `byte` is one byte where `bytes` is a run of
a stated length, and its `string` pins no encoding where the whole point of exact is that `utf8`
does.

An earlier attempt on 27 October renamed `bool` to `boolean` instead, which left two words spelled
exactly as MetaLang spells its own and removed no duplication. That was a judgement written into
the proposal in place of a cleanup, and it is replaced by the above.

**Item 6, `bodiesReachedBy`.** `rulebookFor` now returns the documents it read, deepest first, as
`documents` on `Rulebook`. The 41-line second walk in `ts/metalang/propose.ts` is gone; what
replaced it is eleven lines that read that list. Three tests in `ts/metalang/rulebook.test.ts` hold
the line that the rulebook hands back the language it was read against, every scope brought in with
what that scope itself brings in before it, and what it read even where a scope it needs is missing.

**Also cleaned.** `.gitignore` carried `handoff/` twice under two different comments, from my own
edit. One copy is left.
