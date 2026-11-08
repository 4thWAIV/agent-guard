# Lie-catcher review — the second correction to `tdd-result.md`

## Verdict

**FAIL.** Four confirmed findings. Three are in one paragraph and one bullet of the duplication section; the
fourth is an item put in front of Tim that the round's own instruction already answers. None is in the tests.

The six findings of `tdd-correction-review-lie-catcher.md` are all closed, and I closed each one against the
live files rather than against the correction's word. The tested snapshot is now identified by the digest file
that actually describes it, the per-agent mutation and digest accounting is exact, the owner count is ten and
names the two the correction added, all four correction captures are cited with working line references, and
the two files whose routing was omitted are now described in full.

What replaced the false completeness claim is a duplication list that is itself wrong in three ways: it opens
by saying none of the duplication was created by this round, which its own later bullet contradicts; it never
names the ten `"AgentGuard.Engine"` literal sites standing in `GuardedConstructionAnalyzerTests.cs`, a
delivered file, which `tdd-review-dry.md` F3 confirmed; and it relays a pre-correction count as a live figure.

Everything else holds. I re-ran the full suite in my own isolated copy and got 616/616. All 29 acceptance
quotes are byte-identical to `contract.md`, compared by script. Every other quotation resolves verbatim in the
file the report names. All eleven per-class counts, the 221 total, the 64/12/52 split, the eleven
`EngineInternalsOneDoorAnalyzerTests` line references, the six routed `EngineAssemblyName` sites, the ten
delivered digests, the intermediate build's single diagnostic, and every mutation row reproduce exactly.
Authority and scope are clean: nothing staged, no commit, no stash, `HEAD` unchanged, nothing under `src/` or
`tests/`, and only `tdd-result.md` has changed since the correction reviews were written.

Reviewed snapshot: `HEAD` = `c1c2a1875c14b1a2fb76bf1affc04b386769dccb`, branch `appd-1-process`, with the
uncommitted and untracked delivered files in place. The ten delivered files were byte-identical to
`tdd-correction-scope-and-digests.txt:43-52` at the start and at the end of this review, `git status --short`
outside this work folder and `analyzers/` is one line (`contract.md`), and nothing is staged.

---

## Findings

### 1. "None of it was created by this round or by the correction" is false, and the report says so itself

`tdd-result.md:341-342`, opening the duplication section:

> What follows is the duplication those two reviews confirmed and left standing in the delivered files. **None
> of it was created by this round or by the correction.**

`tdd-result.md:390-394`, the eighth bullet of that same list:

> `TypeTreeTests.cs` repeats the same three arrange-and-act lines in each of its four cases (`:39`, `:41`,
> `:43`; `:55`, `:57`, `:59`; `:69`, `:72`, `:74`; `:86`, `:90`, `:92`), and the "sort both sides ordinally,
> then `Assert.Equal`" shape is spelled in both `DeclaredTypeScannerTests.cs:75-78` and
> `EngineInternalsOneDoorAnalyzerTests.cs:305-309`. **Both are in this round's new code.** `tdd-review-dry.md`
> F8 and F9, reported there rather than blocked on.

The source review agrees with the bullet, not with the opening. `tdd-review-dry.md` files F8 and F9 under a
section heading that is literally:

> ## Minor, in this round's new code

And the subjects are this round's own new files. `TypeTreeTests.cs` and `DeclaredTypeScannerTests.cs` are both
listed at `tdd-result.md:402` as New.

I verified all twelve `TypeTreeTests.cs` lines and both sorted-multiset sites live:

```
TypeTreeTests.cs:39/41/43, :55/57/59, :69/72/74, :86/90/92
    Compilation compilation = await AnalyzerRunner.CompileAsync(LeafTypesSource);
    var offered = new List<string>();
    bool found = TypeTree.Any(root, Record(offered));      ← all four cases, verbatim
DeclaredTypeScannerTests.cs:75-78              the OrderBy(…, StringComparer.Ordinal) pair
EngineInternalsOneDoorAnalyzerTests.cs:305-309 the same shape over messages
```

This is the same defect class as prior finding 4 — a completeness claim contradicted by its own subject. The
old form ("no fixture shell, assertion or reader is spelled twice") was removed and a new absolute was written
three lines above the evidence that refutes it.

### 2. The duplication section never names a confirmed duplication in a delivered file

`tdd-review-dry.md` F3 names ten sites in `GuardedConstructionAnalyzerTests.cs`, one of the ten delivered
files, and says so in as many words:

> Within the delivered set the literal is also spelled ten times in `GuardedConstructionAnalyzerTests.cs`
> (`:377`, `:385`, `:436`, `:470`, `:488`, `:496`, `:512`, `:532`, `:560`, `:572`), another file this round
> edited.

All ten are live, unchanged — the L3 correction did not touch that file (`845745de…` pre and post):

```
:377  StoreConstructedInsideAndOutsideBuilderSource, "AgentGuard.Engine"));
:385  CallFromDeepClassSource, "AgentGuard.Engine", BoundariesSource, "AgentGuard.Boundaries");
:436  CallFromProgramSource, "AgentGuard.Engine", BoundariesSource, "AgentGuard.Boundaries"));
:470  CallDifferentlyNamedFactoryFromDeepClassSource, "AgentGuard.Engine", …
:488  DecoySystemServicesReturningNonContainerSource, "AgentGuard.Engine"));
:496  ConsumerCallingWrapperSource, "AgentGuard.Engine", WrapperInCrossPlatformSource, …
:512  DifferentlyNamedWrapperOutsideFactorySource, "AgentGuard.Engine"));
:532  WrapperFactoryReturningDecoyInfoSource, "AgentGuard.Engine"));
:560  InstanceMethodReturningInfoOutsideFactorySource, "AgentGuard.Engine"));
:572  WrapperCalledFromFileInfoFactorySource, "AgentGuard.Engine"));
```

`tdd-result.md:337` scopes the section to "Duplication left unresolved **in the delivered files**", and
`:341-342` says it is "the duplication those two reviews confirmed and left standing in the delivered files".
The ten sites qualify on every clause and they are absent. The only sentence that reaches them,
`tdd-result.md:395-396`, is:

> `"AgentGuard.Engine"` is spelled roughly 90 times across 25 test files, most of them untouched by this work
> (`tdd-review-dry.md` F3's scope note).

That is F3's scope note, which exists to size the **project-wide** cleanup. F3 kept the delivered-file arm
separate on purpose; the report merges the two, and the delivered-file instance reads as legacy in files this
work never opened. The report's own routing standard is the opposite one: `:116-123` names the six routed
sites in the two one-door classes line by line.

### 3. "roughly 90 times across 25 test files" is a pre-correction figure stated as a live one

Same sentence, `tdd-result.md:395-396`. Live counts of the exact literal `"AgentGuard.Engine"`, run now:

```
analyzers/AgentGuard.Analyzers.Tests   19 files,  98 occurrences
repository-wide (*.cs, excluding bin/obj and .claude/worktrees)
                                       22 files, 112 occurrences
                                       of which 20 are test files
```

The count the report gives is the one `tdd-review-dry.md` wrote before the L3 correction, which then removed
six occurrences and emptied two of the files it counted. The report relays it in the present tense — "is
spelled roughly 90 times across 25 test files" — without re-deriving it. `rails-real-work` rail 6: a
conclusion resting on an old run instead of live truth. "roughly 90" survives against 98; "25 test files" does
not survive against 19 in the analyzer test project, or 20 repository-wide.

### 4. The placement item is brought to Tim as a decision the round's own instruction already settles

`tdd-result.md:11-12`, second item in the Status block, above everything except the uncovered branch:

> **One placement decision needs Tim's sign-off** — where Acceptance 25's direct `CompositionPoint` tests live.

`tdd-worker-instructions.md`, this round's own governing text, verbatim:

> Add the direct wrong-assembly `CompositionPoint` check required by Acceptance 25 in a contract-listed test
> surface.

`contract.md`, Surfaces, verbatim:

> `analyzers/AgentGuard.Analyzers.Tests/BoundariesToCrossPlatformOneDoorAnalyzerTests.cs` and
> `analyzers/AgentGuard.Analyzers.Tests/BoundariesToPerOsOneDoorAnalyzerTests.cs`, renamed to match their
> analyzers, `analyzers/AgentGuard.Analyzers.Tests/GuardedConstructionAnalyzerTests.cs`,
> `analyzers/AgentGuard.Analyzers.Tests/TimeMustUseTimeProviderAnalyzerTests.cs`, and two new files,
> `analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs` and
> `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs`.

The two tests are in `EngineToBoundariesOneDoorAnalyzerTests.cs` (`:254`, `:265`), which that list names. The
instruction's requirement is met and nothing is open. The report's own section concedes the choice is local
and cheap: `tdd-result.md:334-335` — "if a reviewer prefers a different listed surface it is a one-file move."
Under `rails-decisions` that is the agent's call, not Tim's: it does not outlive its function and no other code
depends on it. The rail's wording — "Bringing a settled answer as a decision is the same failure" — covers it.

The cost is not abstract. It sits second in a four-item Status block, ahead of the two items that genuinely
need Tim (the `EngineAssemblySource` interpolation and the rail-1 ledger conflict).

---

## Prior findings — disposition

Every one of the six in `tdd-correction-review-lie-catcher.md` is closed. Each checked against the live files,
not against the correction's account of itself.

| # | Prior finding | Disposition |
| --- | --- | --- |
| 1 | Tested snapshot recorded by a digest file that no longer describes it | **RESOLVED.** See below. |
| 2 | "Both reviewers re-ran these mutations … and reproduced every pass/fail split" false | **RESOLVED.** See below. |
| 3 | "Eight shared owners" undercounts the delivered state by two | **RESOLVED.** See below. |
| 4 | "No fixture shell, assertion or reader is spelled twice" false | **RESOLVED** as a claim — it is gone, and `:66-67` now says "this document makes no claim that the delivered files are free of it". The list that replaced it carries findings 1, 2 and 3 above. |
| 5 | Three of the four correction captures returned nowhere | **RESOLVED.** See below. |
| 6 | Changed-files account omits the correction's edits to two files | **RESOLVED.** See below. |

### Prior 1 — the snapshot is now identified by the file that describes it

`tdd-result.md:406-408` now reads "Their SHA-256 digests as delivered are recorded in
`tdd-correction-scope-and-digests.txt:43-52`", and `:164-165` says the same. I computed all ten live and diffed
them against lines 43-52: identical, all ten. Lines 43-52 are the digest block — line 42 is the command line,
line 52 is the last digest, the file is 52 lines.

`tdd-digests.txt` is now described for what it is. `:188` calls it "the digests of that earlier state. Five of
its ten rows no longer match the delivered files"; `:408-411` names the five. I re-computed every row against
the live files: exactly five mismatch, and they are exactly
`DeclaredTypeScannerTests.cs`, `SharedAnalyzerSources.cs`, `EngineInternalsOneDoorAnalyzerTests.cs`,
`OneDoorIntoCrossPlatformAnalyzerTests.cs`, `OneDoorIntoPerOsAnalyzerTests.cs`.

### Prior 2 — the mutation accounting is now per agent and exact

`tdd-result.md:244-249`. Checked each clause against the review it names:

- `tdd-review-test-quality.md` — "Each applied in my isolated copy, run, and reverted. All six reproduce the
  author's recorded outcome, including every '…and the pre-existing tests still pass' claim", and a second
  table headed "Four further mutations of my own" listing M7, M9, M11, M12. The report's sentence matches.
- `tdd-review-lie-catcher.md:13` — "re-ran four of the six mutations in my own isolated copy"; its table is
  headed "The mutation proofs — four re-run, all reproduced" and lists M3, M4, M6, M5. The report says "M3,
  M4, M5 and M6". Same set.
- `tdd-review-dry.md:18-19` — quoted verbatim by the report, and the quote is exact: "The shared checkout was
  not mutated; no destructive check was needed, so no isolated copy was written."

The digest half at `:251-255` is also exact. `tdd-review-test-quality.md` — "I verified all ten delivered files
against `tdd-digests.txt` with `shasum -a 256` before and after my run"; `tdd-review-lie-catcher.md` —
"whose SHA-256 digests in `tdd-digests.txt` I verified byte-identical in the shared checkout, before and after
this review"; `tdd-review-dry.md` — verified once, in its "Snapshot reviewed" opening. And both correction
reviews state they verified all ten against `tdd-correction-scope-and-digests.txt`.

### Prior 3 — ten owners, and the two new ones are where the report says

`tdd-result.md:65-66` and `:103-108` now say ten, split eight during authoring and two during the correction.
Live:

```
SharedAnalyzerSources.cs:143   internal const string GuardAssemblyName = "guard";
SharedAnalyzerSources.cs:149   internal const string EngineInternalTypeName = EngineAssemblyName + ".EngineInternal";
```

The eight authoring owners the report enumerates are exactly the eight `tdd-review-dry.md` verified against its
own pre-round snapshot: `EngineAssemblyName`, `EngineUsing`, `GatedConsumer`, `SemanticModelAsync`, `TypeIn`,
`OnlyNode`, `RunAgainstFakeEngineAsync`, `AssertHasDocumentationReference`. `git diff --numstat HEAD` on that
file is `720	0`, so "nothing removed or altered" holds.

### Prior 5 — all four correction captures are now cited, and the citations work

The Evidence table at `tdd-result.md:157-162` names all four. Each line reference checked:

- `tdd-correction-analyzer-tests.txt:13-14` — `Passed!  - Failed:     0, Passed:   616, Skipped:     0, Total:   616` / `EXIT=0`.
- `tdd-correction-per-case.txt` — 221 case lines, `Total tests: 221`, `Passed: 221`, `EXIT=0`.
- `tdd-correction-diff-check.txt` — one line, `git diff --check EXIT=0`.
- `tdd-correction-scope-and-digests.txt:5-6` scoped status and `EXIT=0`; `:8-40` the analyzers status block,
  which diffs clean against `git status --short -- analyzers` run now; `:43-52` the ten digests.

The two capture gaps the report discloses at `:167-171` are real and correctly described.

### Prior 6 — the two one-door files are now described in full

`tdd-result.md:116-123` names all six routed sites. Live: `grep -n '"AgentGuard\.Engine'` over
`OneDoorIntoCrossPlatformAnalyzerTests.cs` and `OneDoorIntoPerOsAnalyzerTests.cs` exits 1, and the only
surviving occurrences of the text are comment prose at `OneDoorIntoCrossPlatformAnalyzerTests.cs:146` and
`OneDoorIntoPerOsAnalyzerTests.cs:161` — exactly as the report states. Per-class counts 33 and 34 before and
after, confirmed in both per-case runs.

---

## What I tried to refute and could not

### The suite, re-run in my own isolated copy

`rsync -rlptgo --delete --no-devices --no-specials` of the working tree (excluding `.git`, `bin`, `obj`,
`.codegraph`) into the session scratchpad, verified digest-identical on all ten delivered files before the run.

```
Passed!  - Failed:     0, Passed:   616, Skipped:     0, Total:   616, Duration: 6 s
EXIT=0
```

### Every count, re-derived from the retained runs rather than from the report

| Claim | My result |
| --- | --- |
| 221 affected cases, both runs | 221 case lines in `tdd-correction-per-case.txt` and 221 in `tdd-affected-classes-detailed.txt` |
| The eleven per-class counts | Identical in both files, and identical to the report: `SymbolResolutionTests` 8, `TypeTreeTests` 4, `DeclaredTypeScannerTests` 2, `WrittenNameScannerTests` 5, `EngineToBoundariesOneDoorAnalyzerTests` 15, `EngineInternalsOneDoorAnalyzerTests` 81, `OneDoorIntoCrossPlatformAnalyzerTests` 33, `OneDoorIntoPerOsAnalyzerTests` 34, `GuardedConstructionAnalyzerTests` 20, `ContractConcreteTypeMustNotBeReferencedAnalyzerTests` 7, `TimeMustUseTimeProviderAnalyzerTests` 12 |
| 64 / 12 / 52 | `Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` → 64 case lines, 12 carrying `expectedSubjects: []`, 52 left |
| The same split, from the source | `PermittedPublicReaches` 6 rows, `ProhibitedReaches` 14, `ProhibitedContainerReaches` 12; `Cross()` (`EngineInternalsOneDoorAnalyzerTests.cs:283-293`) doubles each → 12 + 28 + 24 = 64. Matches rows 8, 9 and 10 |
| 600 baseline | `tdd-baseline-analyzer-tests.txt` — 600 passed, `EXIT=0` |
| All 221 display names distinct, and the same set pre and post | 221 distinct in each; sorted and stripped of timing, the two lists diff to zero |
| `TypeReferencePositions` 14 positions | 14 entries, `SharedAnalyzerSources.cs:749-768` |
| `EngineInternalTypeName` read 23 times in AG0041's class | 23 lines (26 occurrences); `GuardAssemblyName` at `:54` and `:288` |
| `AssertHasDocumentationReference` called by three classes | `OneDoorIntoCrossPlatformAnalyzerTests.cs:361`, `OneDoorIntoPerOsAnalyzerTests.cs:368`, `EngineInternalsOneDoorAnalyzerTests.cs:241` |
| `RunAgainstFakeEngineAsync` has one owner, three consumers | `GuardedConstructionAnalyzerTests.cs:410`, `EngineInternalsOneDoorAnalyzerTests.cs:329`, `DeclaredTypeScannerTests.cs:67` |
| No `Assert.Single` in the AG0041 class | `grep -c` → 0 |
| No expected compiler error in the ten delivered files | `grep -n '"CS[0-9]'` over all ten exits 1 |

### All 29 acceptance quotes — compared by script, zero mismatches

I parsed the 29 numbered items out of `contract.md`'s Acceptance section and the 29 quoted requirements out of
`tdd-result.md`'s table and compared them by string equality. **All 29 are byte-identical.** No paraphrase, no
truncation, no reordering, none missing.

### Every other quotation resolves verbatim in its named source

Twenty-two quotations located by exact substring match in the live files: the four bullets of the
direct-dispatch exception and the AG0015 allowance (`contract.md`); Acceptance 24's node clause, Acceptance
26's malformed-attribute sentence, Acceptance 27's traversal sentence, the `CompositionPoint` method-level
check sentence, the "mechanical consequence" sentence, and both halves of contract step 6 (`contract.md`); the
class-move assignment, the unresolved-name-fixture ban, "in a contract-listed test surface", and the
no-repository-edits sentence (`tdd-worker-instructions.md`); the rail-1 conflict sentence, "the largest fixture
duplication left in the delivered tree", and the no-isolated-copy sentence (`tdd-review-dry.md`); the
"live rail-5 violation" sentence and the "Independently reported cases — re-run, not trusted" heading
(`tdd-correction-review-dry.md`). Every one present, word for word.

### Every test name the report cites exists

I extracted 61 backticked test identifiers from `tdd-result.md` and grepped each across
`analyzers/AgentGuard.Analyzers.Tests`. Two are absent, and both are absent by design: they are the OLD names
inside the verbatim Acceptance 7 quote, which that criterion itself orders renamed
(`CallIntoOtherCrossPlatformType_FromNonBoundariesAssembly_IsNotReported`,
`CallOtherPerOsMember_FromNonBoundariesAssembly_IsNotReported`). The report's own row 7 gives the new names,
and both exist. The other 59 all resolve.

### Every mutation row matches the retained log

| Row | `tdd-mutation-proofs.txt` |
| --- | --- |
| M1 — all 4 `TypeTreeTests` FAIL, the nonmatching case on its recorded walk | `Failed: 4, Passed: 0, Total: 4`; `Expected: ["Sample.Other"] / Actual: []` |
| M2 — the `Declared()` span disappears | `Failed: 1, Passed: 1`; `Expected ["Declared","Declared()","EngineInternal"] / Actual ["Declared","EngineInternal"]` |
| M3 — both FAIL, AG0006's seven still PASS | `Failed: 2, Passed: 7, Total: 9` |
| M4 — one FAILS, the other 14 AG0040 tests PASS | `Failed: 1, Passed: 14, Total: 15` |
| M5 — all 4 documentation cases FAIL at the node assertion | `Failed: 4, Passed: 3, Total: 7`, each on `Assert.Contains() Failure: Filter not matched` |
| M6 — one FAILS (`Sample.Guarded` for `Sample.Holder`), 40 PASS | `Failed: 1, Passed: 40, Total: 41`; `Expected: "Sample.Holder" / Actual: "Sample.Guarded"` |

The correction-DRY mutation figures the report cites at `:238-240` are also exact: `GuardAssemblyName` 34
failures, `EngineInternalTypeName` 31, `EngineAssemblyName` 141.

### The uncovered branch — accurate, and honestly disposed

`SymbolResolution.cs:36`, live:

```csharp
return bound is INamedTypeSymbol { TypeKind: TypeKind.Error } ? null : bound;
```

Byte-identical to the report's quotation at `:313`. The reachability claim holds:
`GetSymbolInfo(node).Symbol` only hands back an `INamedTypeSymbol` of `TypeKind.Error` when the compilation
has an error, so no fixture compiling under the runner's default empty expectation reaches it. The `NamedType`
analogue the report cites is real and intact —
`InteropOnlyInCrossPlatformLibrariesAnalyzerTests.cs:202` declares
`UnresolvedInteropAttribute_IsReportedViaSyntacticFallback` and `:222` passes `"CS0246", "CS0246"`; that file
and `NativeCallbackBodyMustBeGuardedAnalyzerTests.cs` are both absent from `git status --short`, and their
`CS7036` and `CS0246` expectations stand. It leads the document at `:5-9` and is claimed nowhere as covered.

### The new tests do what the report says they do

Read in full, not taken from the report:

- `DeclaredTypeScannerTests.cs` — the gated half asserts the three spans `EngineInternal`, `Declared`,
  `Declared()` off the diagnostics' own source spans (`:75-78`); the declaration half asserts from the
  semantic model that the invocation really returns `Sample.Impl.Widget` (`:92-95`) before asserting AG0006
  reports once at the declaration (`:97-102`), so the negative cannot pass by the call being absent.
- `EngineToBoundariesOneDoorAnalyzerTests.cs:265-280` — the decoy case asserts containing type, method name,
  staticness and containing assembly before `Assert.False(CompositionPoint.IsContainerFactoryMethod(decoy))`,
  and `ContainerFactoryAsync` compiles one `ContainerSource` under two assembly names, so only the assembly
  differs. The real predicate is called; nothing re-spells it.
- `TypeTreeTests.cs` — all four cases record the offered types and assert the recorded walk, including the
  nonmatching pointer (`Assert.False(found)` with `Assert.Equal(new[] { OtherLeaf }, offered)`) and the
  generic-then-array-then-pointer descent asserting the order.
- `SharedAnalyzerSources.AssertHasDocumentationReference` (`:884-889`) parses with
  `CSharpSyntaxTree.ParseText`, the same call `AnalyzerRunner` uses at `:54`, `:90`, `:98` and `:124`, and
  asserts a `CrefSyntax` node. No `Assert.Contains("cref"` survives anywhere in the test project.
- `SymbolResolutionTests.cs` has exactly the eight tests the report describes, one per behavior it names.

### The intermediate build carries one diagnostic and nothing else

`tdd-intermediate-build.txt` — `STARTED: 2026-09-16T04:58:29Z`, matching the report's `:195`. One
`warning AG0015` at `src/AgentGuard.Boundaries/SystemServices.cs(88,30)`; `Build succeeded`, `1 Warning(s)`,
`0 Error(s)`, `EXIT=0`. A case-insensitive scan for any other `warning <id>` or `error <id>` in that log
returns nothing, so the override masks nothing.

### The pre-correction evidence rows are exact

`tdd-run1-analyzer-tests.txt` — `EXIT=1`, six `: error ` lines over four distinct rules, counted:
`CA1849` ×1, `CA1859` ×2, `MA0004` ×2, `S6966` ×1, and no test-result line at all. `tdd-run2`, `tdd-run3`,
`tdd-final` — 616 passed, 0 failed, `EXIT=0` each. `tdd-diff-check-and-status.txt` — `git diff --check`
`EXIT=0` then an unscoped `git status --short`, with zero `src/` or `tests/` paths.

### Authority and scope — clean

- Only `tdd-result.md` has changed since the two correction reviews were written. A whole-tree
  `find -newermt "2026-09-16 10:22:00"` (excluding `.git`, `bin`, `obj`, `.codegraph`) returns three paths:
  the two review files themselves and `tdd-result.md` (`11:34:15`). No test file, no analyzer source, no
  project file, no contract. The report's `:24-25` — "the second correction changed this document alone" — is
  true.
- `HEAD` still `c1c2a1875c14b1a2fb76bf1affc04b386769dccb`; `origin/appd-1-process` equal to it; nothing
  staged; no stash.
- `git status --short -- src tests` is empty.
- `AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs` are absent from `git status --short`; their mtimes are
  Sep 13 14:08 and Sep 12 21:00, long before this round.
- `contract.md` mtime is `2026-09-15T22:28:39`, before the round's own baseline run at `04:33:51Z`
  (22:33 local). The round did not edit it.
- The ten delivered files were byte-identical to `tdd-correction-scope-and-digests.txt:43-52` at the start and
  the end of this review, and `git status --short -- analyzers` diffs clean against the retained block. Every
  destructive run happened only in the isolated rsync copy.

### The three items that genuinely need Tim

All three survive attack and are correctly placed at the top:

- The uncovered `SymbolResolution.Symbol` branch — verified above.
- `SharedAnalyzerSources.cs:72` `[assembly: InternalsVisibleTo("guard")]`, 71 lines above the owner at `:143`,
  with `"AgentGuard.TestHelpers"` (`:73`) and `"AgentGuard.Tests"` (`:74`) in the same shape. Live, and the
  quotation from `tdd-correction-review-dry.md` R1 is verbatim.
- The rail-1 conflict — `tdd-review-dry.md`'s sentence is quoted word for word, and the instruction sentence
  that creates the conflict is verbatim from `tdd-worker-instructions.md`.

---

## Filed below the fold — observations, not findings

- **"Byte-identical" is used loosely for the display-name comparison.** `tdd-result.md:113-114` and `:189` say
  the 221 display names are "byte-identical" before and after; the raw name lines differ in 122 positions
  because xUnit's parallel runner emits them in a different order. Sorted and stripped of the timing suffix
  they diff to zero, which is exactly what `:222-223` says and what `tdd-correction-review-dry.md` did. The
  accurate statement is in the document; the two loose ones are not separately wrong about the underlying
  fact.
- **`git diff --numstat` is cited for a claim it does not carry.** `tdd-result.md:103-104` — "ten additions and
  nothing removed or altered (`git diff --numstat` against `HEAD` is `720 0` for this file)". `720	0` proves
  the second half only; the ten is established elsewhere and correctly.
- **The disclosed capture gap names one of two elisions on the same line.** `tdd-correction-per-case.txt:5`
  reads `$ dotnet test ... --logger 'console;verbosity=detailed' --filter <eleven affected classes>`. The
  report's gap note at `:167-170` names the `--filter` placeholder but not the `dotnet test ...` elision of
  the project path, which its Evidence table row at `:160` asserts ("the same with …").
- **The capture window's opening second equals the last test-file edit.**
  `tdd-correction-analyzer-tests.txt` header is `16:06:15Z`; `DeclaredTypeScannerTests.cs` and
  `EngineInternalsOneDoorAnalyzerTests.cs` have mtimes `1789574775.000` and `1789574775.022`, i.e. the same
  second. `tdd-result.md:154-155` says the captures are "after the correction's last edit to a test file",
  which second-granularity headers cannot show either way. It does not matter to the result: I ran the live
  delivered files myself and got the same 616, and the digest capture at `16:07:12Z` matches them exactly.
- **Row 29's "EXISTING and complete" does not cross-reference the tautology note.** `tdd-result.md:307` calls
  Acceptance 29 complete; `tdd-review-dry.md` F5 records that two of the five cases are true by construction of
  their own input, and `tdd-result.md:381-383` carries that in the duplication section without a pointer from
  the row. The test-quality reviewer's M9 mutation
  (`IsDocumentationReference` always false) does fail `EveryNameInsideACref_IsADocumentationReference`, so the
  case is not vacuous, which is why this is not a finding.
- **"Green on its first run"** (`tdd-result.md:52-53`) means the first run in which tests executed;
  `tdd-run1-analyzer-tests.txt` is `EXIT=1` with no test-result line, which the report states plainly at
  `:184`.

## Method

- Isolated copy: `rsync -rlptgo --delete --no-devices --no-specials` of the full working tree, including the
  uncommitted and untracked delivered files, into the session scratchpad, verified digest-identical on all ten
  delivered files before any run. `dotnet test` over the full analyzer suite executed there. The shared
  checkout was never mutated; its ten digests, `git status --short -- analyzers`, `git status --short -- src tests`,
  `HEAD`, the index and the stash list were checked at the start and at the end.
- The 29 acceptance quotes compared to `contract.md` by string equality in a script, not by eye.
- Every other quotation located in its named source file by exact substring match against the live file.
- Every per-class count, the 221, the 64/12/52 split and the display-name comparison computed from the
  retained per-case logs directly, and the table row counts cross-checked against the test source.
- Every line reference in the duplication section opened with `sed -n` against the live file.
- Every test identifier the report backticks grepped across the analyzer test project.
- Live `shasum -a 256`, `git diff --numstat`, `git status`, `git diff --cached`, `git stash list`,
  `git rev-parse`, `stat` mtimes and a whole-tree `find -newermt` run by me.
