# DRY review — L3 correction to the TDD round, SystemServices relocation

## Verdict: PASS

F1, F2 and F3 are each gone from the live files, and the correction introduced **no new duplication of any kind** —
not a literal, not a helper body, not a fixture fragment, not an assertion. Every line the correction added is
enumerated below; there are 44 of them and every one is either a reference to an existing owner or one of the two new
constants with its doc comment.

Both new constants are genuinely shared (two consumer classes each, proved by mutation, not by grep alone). No alias
and no second spelling of any of the three values was introduced anywhere. No whitespace-only edit exists in any of
the five files. The 616 / 221 / 64 (12 + 52) counts all reproduce on my own runs, and the 221 per-case display names
are **byte-identical** to the pre-correction run — which proves no scenario was bundled, split, renamed or lost, and
proves the routed constants resolve to exactly the strings the deleted private constants held.

Confirmed duplication that this correction did **not** create is reported at the end, for the record. One item there
was named inside F1's own body and needs Tim's call; it is not the basis of this verdict.

## Snapshot reviewed, and how before/after was established

Live working tree at HEAD `c1c2a1875c14b1a2fb76bf1affc04b386769dccb`, branch `appd-1-process`.

The correction's exact delta was read off real before/after content, not off any report:

- **Pre-correction** = `scratchpad/review-copy/analyzers/AgentGuard.Analyzers.Tests/` (the prior DRY reviewer's copy,
  22:49–23:13 on 2026-09-15). I verified all five files there are byte-identical to the digests recorded in
  `tdd-digests.txt`, which is the TDD round's delivered state.
- **Post-correction** = the live working-tree files. I verified all ten delivered files against
  `tdd-correction-scope-and-digests.txt`; all ten match.

Digest comparison across the ten delivered files shows **exactly five changed**, and they are exactly the five named
in the correction's scope — `SharedAnalyzerSources.cs`, `DeclaredTypeScannerTests.cs`,
`EngineInternalsOneDoorAnalyzerTests.cs`, `OneDoorIntoCrossPlatformAnalyzerTests.cs`,
`OneDoorIntoPerOsAnalyzerTests.cs`. `GuardedConstructionAnalyzerTests.cs` (`845745de…`),
`SymbolResolutionTests.cs`, `TypeTreeTests.cs`, `EngineToBoundariesOneDoorAnalyzerTests.cs` and
`WrittenNameScannerTests.cs` are byte-identical pre and post, so the correction did not reach into them.

Both discovery lenses were re-run by me, not taken from the author (rail 2):

- grep across `analyzers`, `src`, `tests`, `eng` for `"guard"`, `AgentGuard.Engine.EngineInternal`,
  `"AgentGuard.Engine"`, `"AgentGuard.TestHelpers"`, `"AgentGuard.Tests"`, every new and pre-existing owner name,
  and for any alias constant declared from the new owners.
- CodeGraph (`codegraph_explore`) over
  `GuardAssemblyName EngineInternalTypeName CliAssembly CompiledName EngineAssembly Name TestAssembly TestHelpersName SharedAnalyzerSources`,
  which returned the production owners `CliAssembly.CompiledName` (`analyzers/AgentGuard.Analyzers/CliAssembly.cs:30`),
  `EngineAssembly.Name` (`analyzers/AgentGuard.Analyzers/EngineAssembly.cs:20`) and `TestAssembly.TestHelpersName`
  (`analyzers/AgentGuard.Analyzers/TestAssembly.cs:20`), and showed no competing test-layer owner of either new value.
- A mechanical string-literal multiset diff of every one of the five files, pre vs post.

Destructive checks (three mutations, full suite runs) ran only in an isolated copy at
`scratchpad/dryrev/repo`, rsynced from the working tree and verified byte-identical on all five files before use.
The shared checkout was not mutated: the five digests and `git status` are unchanged from the start of this review.

## F1 — `GuardAssembly = "guard"` spelled twice: RESOLVED

Both private constants are deleted, verified by reading the live files:

- `analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs` — `private const string GuardAssembly = "guard";`
  is gone (it stood at `:21`).
- `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs` — the same line is gone (it stood at
  `:17`).

One named owner now holds the value:

- `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:143` —
  `internal const string GuardAssemblyName = "guard";`

Three read sites, in two classes:

- `DeclaredTypeScannerTests.cs:68`
- `EngineInternalsOneDoorAnalyzerTests.cs:54` (the `GatedAssemblies` theory source) and `:288` (the `Cross` builder)

A grep for the old names across the whole test project returns nothing:
`grep -rn 'GuardAssembly\b' --include='*.cs' analyzers/AgentGuard.Analyzers.Tests` excluding `GuardAssemblyName`
exits 1. No alias constant was declared from it either —
`grep -rn '=\s*SharedAnalyzerSources\.GuardAssemblyName\s*;'` exits 1.

## F2 — `EngineInternalType = "AgentGuard.Engine.EngineInternal"` spelled twice: RESOLVED

Both private constants are deleted (they stood at `DeclaredTypeScannerTests.cs:23` and
`EngineInternalsOneDoorAnalyzerTests.cs:24`). One named owner now holds the value:

- `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:149` —
  `internal const string EngineInternalTypeName = EngineAssemblyName + ".EngineInternal";`

It is **composed** from the existing `EngineAssemblyName` (`:129`) rather than re-spelling `"AgentGuard.Engine"`, so
the correction added no second spelling of the assembly name while fixing the type name. Twenty-four read sites across
two classes: `DeclaredTypeScannerTests.cs:82` and `EngineInternalsOneDoorAnalyzerTests.cs:77-111, :188, :212, :218,
:219`.

`grep -rn 'EngineInternalType\b'` excluding `EngineInternalTypeName` exits 1 across the test project.

## F3 — six `"AgentGuard.Engine"` literals in the two one-door classes: RESOLVED

`grep -n '"AgentGuard\.Engine' OneDoorIntoCrossPlatformAnalyzerTests.cs OneDoorIntoPerOsAnalyzerTests.cs` exits 1 —
**zero** string literals of that value remain in either file. The only two surviving occurrences of the text in those
files are prose inside comments (`OneDoorIntoCrossPlatformAnalyzerTests.cs:146`,
`OneDoorIntoPerOsAnalyzerTests.cs:161`).

All six sites now read `SharedAnalyzerSources.EngineAssemblyName`:

- `OneDoorIntoCrossPlatformAnalyzerTests.cs:150`, `:286`, `:306`, `:368`
- `OneDoorIntoPerOsAnalyzerTests.cs:375`, `:383`

The owner now has three consumer classes — the two above plus `EngineToBoundariesOneDoorAnalyzerTests.cs`
(`:84`, `:90`, `:220`, `:259`, `:295`).

## No new duplication — the exhaustive check

**Every line the correction added, enumerated.** `diff` of pre vs post over all five files yields 44 added lines and
37 removed. The added lines, deduplicated with counts:

| count | file | added line |
|---|---|---|
| 9 | `EngineInternalsOneDoorAnalyzerTests` | `SharedAnalyzerSources.EngineInternalTypeName,` |
| 4 | `EngineInternalsOneDoorAnalyzerTests` | `SharedAnalyzerSources.EngineInternalTypeName),` |
| 2 | `EngineInternalsOneDoorAnalyzerTests` | `SharedAnalyzerSources.EngineInternalTypeName);` |
| 8 | `SharedAnalyzerSources` | the two doc comments (`/// <summary>`, six body lines, `/// </summary>`, ×2) |
| 1 | `SharedAnalyzerSources` | `internal const string GuardAssemblyName = "guard";` |
| 1 | `SharedAnalyzerSources` | `internal const string EngineInternalTypeName = EngineAssemblyName + ".EngineInternal";` |
| 9 | `EngineInternalsOneDoorAnalyzerTests` | `Reach(…)` rows and the `GatedAssemblies` / `Cross` sites, repointed |
| 2 | `DeclaredTypeScannerTests` | the two repointed call sites |
| 4 | `OneDoorIntoCrossPlatformAnalyzerTests` | the four repointed `EngineAssemblyName` sites |
| 2 | `OneDoorIntoPerOsAnalyzerTests` | the two repointed `EngineAssemblyName` sites |

Nothing in that list is a new literal, a new helper body, a new fixture fragment or a new assertion. No method was
added to any file. The repeated `SharedAnalyzerSources.EngineInternalTypeName` entries are argument-list elements of
distinct theory rows — one expected subject per expected diagnostic — and each row previously repeated the deleted
local constant in exactly the same shape. That is a reference to one owner repeated, which is what rail 5 asks for,
not a duplicated block under rail 6.

**Literal multiset diff, per file (count pre → post).** This is mechanical and covers every string in each file, not
only the ones the findings named:

```
SharedAnalyzerSources.cs                  '.EngineInternal'                    0 -> 1
                                          'guard'                              0 -> 1
                                          'EngineAssemblySource' (doc cref)    2 -> 3
DeclaredTypeScannerTests.cs               'AgentGuard.Engine.EngineInternal'   1 -> 0
                                          'guard'                              1 -> 0
EngineInternalsOneDoorAnalyzerTests.cs    'AgentGuard.Engine.EngineInternal'   1 -> 0
                                          'guard'                              1 -> 0
OneDoorIntoCrossPlatformAnalyzerTests.cs  'AgentGuard.Engine'                  4 -> 0
OneDoorIntoPerOsAnalyzerTests.cs          'AgentGuard.Engine'                  2 -> 0
```

Net: nine spellings removed, two added, both in the one owner. **No other literal in any of the five files changed
count in either direction.** Search beyond the first occurrence therefore finds nothing: there is nothing to find.

**Both new constants are genuinely shared, not single-use.** `GuardAssemblyName`: 3 read sites in 2 classes.
`EngineInternalTypeName`: 24 read sites in 2 classes. Proved live rather than by grep — see the mutations below.

**Rail 3 (false "new") — checked and cleared.** `CliAssembly.CompiledName = "guard"`
(`analyzers/AgentGuard.Analyzers/CliAssembly.cs:30`) is a pre-existing owner of the same character sequence, and the
analyzer project does grant `InternalsVisibleTo("AgentGuard.Analyzers.Tests")`, so reading it is possible. It must
not be read, and for a reason that is verifiable in the live code: `EngineInternalsOneDoorAnalyzer.cs:62-64` builds the
rule's `GatedAssemblies` out of `CliAssembly.CompiledName` and `TestAssembly.TestHelpersName`. If the test compiled
its consumer fixture from the same constant, a mutation of the production gate would move the gate and the fixture
together and every AG0041 gated-consumer test would keep passing through a real regression. This is the identical
adjudication the prior DRY review already made for `EngineAssemblyName` vs `EngineAssembly.Name`, and the pattern is
established in the file itself — `SharedAnalyzerSources.CrossPlatformNamespace` (`:465`) mirrors
`CrossPlatformBoundary.RootName` for the same reason. Separate test-layer ownership is correct, not a false "new".

**Rail 1 (prior-art-ledger) — not owed here.** The correction built no new capability. It executed rail 4's Fix —
"collapses the copies into a single shared owner and calls it" — on a duplication the prior DRY review had already
found and searched. Two constants hoisted into an existing owner class is the fix, not a new capability. The rail-1
process conflict the prior review raised for the TDD round is unchanged by this correction and still needs Tim.

## The whitespace claim

**No whitespace-only edit exists in any of the five files.** `diff -u` and `diff -u -w -B` return identical added and
removed line counts for all five files (`+11/-0`, `+2/-4`, `+25/-27`, `+4/-4`, `+2/-2`), which means no hunk is purely
whitespace or blank-line: every hunk carries a real code change. The blank lines that disappeared are the ones that
belonged to the four deleted `const` declarations, and they disappeared inside those same deletion hunks.

Nothing beyond whitespace changed, with one consequence worth naming so a later reader is not surprised: in
`EngineInternalsOneDoorAnalyzerTests.cs:21`, deleting `EngineInternalType` left the group comment
`// The subjects a message names — every Engine internal the fixtures below reach, as the diagnostic spells it.`
sitting directly above `EngineInternalRead` (`:22`) instead of above the deleted constant. The comment is plural and
heads the whole block, so it still reads true. No code semantics changed.

Non-blocking observation, not a DRY item: the repointing lengthened lines.
`EngineInternalsOneDoorAnalyzerTests.cs` now has a longest line of 171 characters (`:90`, `:91`, `:92` — the `Reach`
rows carrying two subjects each), against ~110 before. There is no `max_line_length` rule in `.editorconfig` and the
build is clean, so this is readability only.

## Independently reported cases — re-run, not trusted

I re-ran everything myself in the isolated copy rather than reading `tdd-correction-per-case.txt`:

- **Full analyzer suite:** `Passed! - Failed: 0, Passed: 616, Skipped: 0, Total: 616`. Matches the required 616.
- **The eleven affected classes** (the same filter set, derived from the per-case file's own class list):
  `Total tests: 221, Passed: 221`. Matches the required 221.
- **AG0041's `Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics`:** 64 case lines, all `Passed`, of which
  **12** carry `expectedSubjects: []` and **52** carry a non-empty expectation. All 64 display names are distinct, so
  no two theory rows collapsed into one.
- **Nothing bundled, split, renamed or lost.** I extracted the 221 per-case display names from the pre-correction run
  (`tdd-affected-classes-detailed.txt`) and from my own post-correction run, normalised only the timing suffix, sorted
  both, and diffed: **identical, 221 vs 221, zero differences.** All 221 are distinct. The `assemblyName` distribution
  is unchanged at 40 `"guard"` / 40 `"AgentGuard.TestHelpers"` in both runs.

That last check is also the strongest available proof that the routing is value-preserving: xUnit prints the resolved
argument values into the display name, so identical names mean `SharedAnalyzerSources.GuardAssemblyName` resolves to
`"guard"` and `SharedAnalyzerSources.EngineInternalTypeName` resolves to `"AgentGuard.Engine.EngineInternal"` at every
routed site. A sample post-correction line:

    Passed …Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics(assemblyName: "guard",
      declaration: "        internal static bool Pattern(object value)"···,
      expectedSubjects: ["AgentGuard.Engine.EngineInternal"])

## What I tried to refute, and how it came out

**"The repointing is inert — the shared constants are not actually load-bearing at the routed sites."** Refuted by
three mutations in the isolated copy, each reverted and the file digest re-verified afterwards:

| mutation | result | classes that went red |
|---|---|---|
| `GuardAssemblyName` → `"guard-MUTANT"` | 34 failed / 582 passed | `EngineInternalsOneDoorAnalyzerTests` 33, `DeclaredTypeScannerTests` 1 |
| `EngineInternalTypeName` → `… + ".EngineInternalMUTANT"` | 31 failed / 585 passed | `EngineInternalsOneDoorAnalyzerTests` 30, `DeclaredTypeScannerTests` 1 |
| `EngineAssemblyName` → `"AgentGuard.EngineMUTANT"` | 141 failed / 475 passed | `OneDoorIntoCrossPlatformAnalyzerTests` 25, `OneDoorIntoPerOsAnalyzerTests` 26, plus 79 / 10 / 1 in the three Engine classes |

The third mutation is the direct proof for F3: pre-correction those two classes spelled the literal and would not have
noticed the owner moving; now 51 of their cases depend on it.

**"The correction created new duplication somewhere other than where the findings were."** Refuted exhaustively — the
44-line enumeration and the per-file literal multiset diff above cover every added line and every string in all five
files. Nine literals out, two in, both in the one owner, nothing else moved.

**"`GuardAssemblyName` is a false 'new' — `CliAssembly.CompiledName` already owns `"guard"`."** Refuted, on the code:
`EngineInternalsOneDoorAnalyzer.cs:62-64` builds the rule's own gate from that constant, so a test that read it would
move gate and fixture together under mutation. Same adjudication the prior review already made for `EngineAssemblyName`,
same pattern as `CrossPlatformNamespace` vs `CrossPlatformBoundary.RootName`.

**"`EngineInternalTypeName` silently re-spells `"AgentGuard.Engine"`, adding a spelling while removing another."**
Refuted by reading `:149`: it is `EngineAssemblyName + ".EngineInternal"`. The multiset diff confirms only
`'.EngineInternal'` was added, never `'AgentGuard.Engine'`.

**"An alias or second name for one of the three values was introduced."** Refuted.
`grep -rn 'GuardAssembly\b\|EngineInternalType\b'` excluding the new names exits 1 across the test project, and
`grep -rn '=\s*SharedAnalyzerSources\.\(GuardAssemblyName\|EngineInternalTypeName\|EngineAssemblyName\)\s*;'` exits 1 —
no class re-wrapped the owner under a local name.

**"The correction reached beyond its five files."** Refuted by digest. All ten delivered files were digested pre and
post; exactly five differ, and they are the five the correction was authorised to touch.
`git status --short -- src tests` is empty.

**"A test's behaviour changed under the routing."** Refuted by the identical 221 display names, the identical
`assemblyName` distribution, and 616/616 on my own run.

## Confirmed duplication this correction did NOT create

Reported for the record and for whoever schedules the cleanup. None of it is the basis of the verdict; all of it is
verified against the before/after snapshots as pre-dating this correction, and all of it sits outside Tim's two
instructions, which named the two constants and the six Engine literals and said "No new design, helper framework,
test restructuring."

### R1 — `"guard"` is still spelled inside the fixture in the file that now owns it (needs Tim's call)

- `SharedAnalyzerSources.cs:72` — `[assembly: InternalsVisibleTo("guard")]`, inside the `EngineAssemblySource` raw
  string (`:69-122`).
- `SharedAnalyzerSources.cs:143` — the new owner, 71 lines below it.

This is the third spelling F1's own body named: *"The value also appears inside the shared fixture itself at
`SharedAnalyzerSources.cs:72` … so there are now three spellings of one identity with no owner."* The prior review's
fix put routing it under a "While there:" clause that also asked for `TestHelpersAssembly` and `TestsAssembly` to move
and for `EngineAssemblySource` to interpolate all three grants. **Tim's correction instruction did not carry that
clause**, and turning `EngineAssemblySource` into an interpolated raw string is a change to how the fixture is built —
squarely inside "No new design, helper framework, test restructuring." The correction was right not to take it. It is
a live rail-5 violation and it needs a yes or a no from Tim, not an agent's judgement.

Same shape, same file, same clause, also untouched: `"AgentGuard.TestHelpers"` at `:73` vs
`EngineInternalsOneDoorAnalyzerTests.cs:17`, and `"AgentGuard.Tests"` at `:74` vs `:19`.

Outside the five files, the consumer-assembly name `"guard"` is also spelled at
`GuardedConstructionAnalyzerTests.cs:421` and `:458`, `TimeMustUseTimeProviderAnalyzerTests.cs:208`,
`NoServiceAsParameterAnalyzerTests.cs:75`, and `NoStaticServiceHolderAnalyzerTests.cs:75` and `:99`. (The two hits in
`RawPrimitiveOnlyInOwnerAnalyzerTests.cs` are `Process.Start("guard")` fixtures — a process name, a different concept,
correctly excluded.) These belong with the project-wide cleanup issue the prior review's fix item 3 already called for.

### R2 — three literals in `EngineInternalsOneDoorAnalyzerTests.cs` still spell the value it now reads from the owner 24 times

- `:22` — `private const string EngineInternalRead = "AgentGuard.Engine.EngineInternal.Read()";`
  (would be `SharedAnalyzerSources.EngineInternalTypeName + ".Read()"`)
- `:27` — `"System.Collections.Generic.List<AgentGuard.Engine.EngineInternal>"`
- `:30` — `"System.Collections.Generic.List<AgentGuard.Engine.EngineInternal[]>"`

All three are unchanged from the pre-correction file. The same file spells `AgentGuard.Engine` in a further eight
constants (`:24`, `:32`, `:36`, `:38`, `:40`, `:42`, `:44`) and three inline fixtures/messages (`:156`, `:253`,
`:317`), all pre-existing. This is the same cleanup as R1 and the same issue.

### R3 — `"AgentGuard.CrossPlatform"` stands un-routed on two lines the correction rewrote

`SharedAnalyzerSources.CrossPlatformNamespace` (`:465`) is the owner, and
`OneDoorIntoCrossPlatformAnalyzerTests.cs:101` already reads it. The literal is nevertheless spelled at `:117`,
`:125`, `:139`, `:150` and `:368` — and `:150` and `:368` are two of the six lines this correction rewrote, so the
Engine half of each line was routed while the CrossPlatform half on the same line was not. Sibling spellings at
`:288` (`"AgentGuard.CrossPlatform.Decoy"`) and `:308` (`"AgentGuard.CrossPlatform.MacOS"`) sit one and two lines
below two more rewritten lines; those two are the prior review's F7.

All five predate the correction, and Tim's instruction 2 is explicit that it covers "the six remaining **Engine**
assembly-name literals". Routing them is mechanical and behaviour-free, and belongs on the same cleanup issue.

### R4 — `OneDoorIntoCrossPlatformAnalyzerTests.cs:150` is an inline copy of the file's own `RunFromEngineAsync`

`:365-369` owns the call. `:148-150` re-spells it with a different first argument:

    :150   CallOtherCrossPlatformTypeSource, SharedAnalyzerSources.EngineAssemblyName, CrossPlatformSource, "AgentGuard.CrossPlatform");
    :368   source,                           SharedAnalyzerSources.EngineAssemblyName, CrossPlatformSource, "AgentGuard.CrossPlatform");

`RunFromEngineAsync(CallOtherCrossPlatformTypeSource)` is exactly the call at `:150`. Rail 6, pre-existing — the two
argument lists were already identical modulo the source argument before the correction. The correction made the
identity more visible without creating it.

## Rail-by-rail

| rail | ruling |
|---|---|
| 1 — no prior-art-ledger ruling | Not applicable. No new capability; this is rail 4's Fix being executed on a duplication already found and searched. |
| 2 — ledger trusted, not verified | Satisfied. grep and CodeGraph re-run by me; queries and results recorded above. |
| 3 — false "new" | Clear. `GuardAssemblyName` is a deliberate test-layer owner, separate from `CliAssembly.CompiledName` for a reason provable at `EngineInternalsOneDoorAnalyzer.cs:62-64`, matching the established `EngineAssemblyName` and `CrossPlatformNamespace` precedent. |
| 4 — "extract" written as a fresh copy | Clear. Both copies of each value were deleted and every use site calls the one owner. Proved by mutation, not by grep. |
| 5 — duplicated value | Clear for everything the correction touched. Nine literals removed, two added, both in the one owner; no literal anywhere in the five files changed count otherwise. R1–R3 are pre-existing and outside the instruction. |
| 6 — duplicated block or function | Clear. No method or block was added to any file. R4 is pre-existing. |
| 7 — single-lens search | Satisfied. grep → pivot on the shared owners → CodeGraph blast radius → read the callers; plus a mechanical literal multiset diff that no lens-chain would have caught on its own. |
| 8 — reimplemented primitive | Not applicable. |

## Nothing here needs a design change

Nothing in this review asks for a change to a decided design. The one item that needs Tim rather than an agent is R1 —
whether `EngineAssemblySource`'s three `InternalsVisibleTo` grants get interpolated from their owners, which the prior
review asked for under "While there:" and Tim's instruction did not carry. R2, R3 and R4 belong on the project-wide
cleanup issue the prior review's fix item 3 already called for.
