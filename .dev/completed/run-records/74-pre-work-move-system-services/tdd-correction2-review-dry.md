# DRY review — second correction to the TDD round, SystemServices relocation

## Verdict: FAIL

The sentence that separates resolved duplication from residual duplication is false, and the report contradicts it seven lines later.

`tdd-result.md:342` says of the residual list: "None of it was created by this round or by the correction." `tdd-result.md:393` says of the eighth item in that same list: "Both are in this round's new code." The live tree says the second sentence is the true one. That is finding D1 below.

A second record — the mutation captures being pre-correction — is written out twice in full, and the two copies have already drifted: one attaches a caveat that the byte-identity claim was never captured, the other states the same byte-identity as established fact. That is finding D2.

The three duplications the correction was sent to close are genuinely closed, the correction introduced no new duplication anywhere in the five files it touched, and every line reference in the residual list is correct against the delivered files. All of that is recorded under "What I attacked and could not break".

## Snapshot reviewed

Live working tree at `HEAD` `c1c2a1875c14b1a2fb76bf1affc04b386769dccb`, branch `appd-1-process`.

All ten delivered files match `tdd-correction-scope-and-digests.txt:43-52` byte for byte, checked with `shasum -a 256 -c`. The digest block is at lines 43 to 52 of that file, as the report says.

Two earlier snapshots survive on disk and I verified both before using them, rather than taking either at its label:

- Pre-correction: `.../c96ea1f7-.../scratchpad/review-copy/analyzers/AgentGuard.Analyzers.Tests/`. All ten files match `tdd-digests.txt` byte for byte, so this is the delivered state before the L3 correction.
- Pre-round: `.../c96ea1f7-.../scratchpad/lc-iso/analyzers/AgentGuard.Analyzers.Tests/` (Sep 15 17:23) and `.../scratchpad/SharedAnalyzerSources.orig.cs` (Sep 15 19:24), both earlier than this round's 22:34 baseline run.

Comparing `tdd-digests.txt` against `tdd-correction-scope-and-digests.txt:43-52`, exactly five rows differ: `DeclaredTypeScannerTests.cs`, `SharedAnalyzerSources.cs`, `EngineInternalsOneDoorAnalyzerTests.cs`, `OneDoorIntoCrossPlatformAnalyzerTests.cs` and `OneDoorIntoPerOsAnalyzerTests.cs`. Those are the five the report names at `:177-179` and at `:408-411`.

I did not mutate the shared checkout and I did not run the test suite. The suite result is not the duplication lens, and the delivered files are byte-identical to the tree the correction's DRY review already ran it against.

## D1 — the residual list carries a false provenance claim (blocking)

**Location.** `tdd-result.md:342`, closing the opening paragraph of "Duplication left unresolved in the delivered files": "What follows is the duplication those two reviews confirmed and left standing in the delivered files. None of it was created by this round or by the correction."

**What contradicts it.**

`tdd-result.md:390-394`, the eighth bullet of that same list: "`TypeTreeTests.cs` repeats the same three arrange-and-act lines in each of its four cases (`:39`, `:41`, `:43`; `:55`, `:57`, `:59`; `:69`, `:72`, `:74`; `:86`, `:90`, `:92`), and the "sort both sides ordinally, then `Assert.Equal`" shape is spelled in both `DeclaredTypeScannerTests.cs:75-78` and `EngineInternalsOneDoorAnalyzerTests.cs:305-309`. Both are in this round's new code."

`tdd-review-dry.md`, the source the report cites for those two items, keeps them under a heading of their own. Its heading at `:95` is "Confirmed duplication that this round did not create" and covers F4 through F7. Its heading at `:154` is "Minor, in this round's new code" and covers F8 and F9. The report attributes the first heading's provenance to items the source filed under the second.

**The live check.** The pre-round snapshot at `.../lc-iso/analyzers/AgentGuard.Analyzers.Tests/` contains no `TypeTreeTests.cs`, no `SymbolResolutionTests.cs` and no `DeclaredTypeScannerTests.cs`. Both files the eighth bullet names are this round's new code, so the duplication in them is duplication this round created.

I verified each of the four repeated triples in the live file. `TypeTreeTests.cs:39`, `:55`, `:69` and `:86` each spell `Compilation compilation = await AnalyzerRunner.CompileAsync(LeafTypesSource);`; `:41`, `:57`, `:72` and `:90` each spell `var offered = new List<string>();`; `:43`, `:59`, `:74` and `:92` each spell `bool found = TypeTree.Any(root, Record(offered));`. `DeclaredTypeScannerTests.cs:75-78` and `EngineInternalsOneDoorAnalyzerTests.cs:305-309` both build the sort-both-sides-then-`Assert.Equal` shape.

**Why it matters.** This is the one sentence a reader uses to decide whether the TDD round left new duplication behind, and it says no when the answer is yes. The two source reviews drew the distinction in two separate sections; flattening them into one list and stamping a single provenance on the whole erases it.

**Fix.** Strike the blanket sentence at `:342`. Label the two groups the way `tdd-review-dry.md` does: seven items that predate this round, and one item — the `TypeTreeTests.cs` arrange-and-act repetition, plus the `DeclaredTypeScannerTests.cs` half of the sorted-multiset shape — that this round's new files introduced.

## D2 — one record written twice, and the copies already disagree (blocking)

**The duplication.** The caveat that the mutation captures describe the pre-correction tree is written out in full in two places, neither pointing at the other.

`tdd-result.md:194-199`: "Two of these were not re-run after the correction and no post-correction capture of either exists. The intermediate build is one: `tdd-intermediate-build.txt` was captured at `04:58:29Z`, before the correction. The mutation proofs are the other: `tdd-mutation-proofs.txt` ran against the pre-correction tree. The correction changed no analyzer source and no expectation, and its own DRY review re-ran the full suite and the per-case run over the corrected files and mutated each of the three routed constants in an isolated copy, but neither of the two captures above was repeated."

`tdd-result.md:236-240`: "Two gaps first. None of the six mutations was re-run after the L3 correction, so no capture of any of them describes the delivered files; the correction changed no analyzer source and no expectation, and `tdd-correction-review-dry.md` mutated the three routed constants instead — `GuardAssemblyName`, `EngineInternalTypeName` and `EngineAssemblyName` — recording 34, 31 and 141 failures respectively, which shows the routed sites are load-bearing, but that is a different set of mutations from the six below."

Three facts stand in both: the six mutations were not re-run after the correction, the correction changed no analyzer source and no expectation, and the correction's DRY review mutated the three routed constants instead.

**The drift.** `tdd-result.md:240-242` says: "And the byte-identity of the isolated copy before and after the mutations was asserted in `tdd-mutation-proofs.txt` without a captured command; it is restated here as an assertion, not as captured evidence."

`tdd-result.md:257-259` then states the same record with the caveat dropped: "The six mutations in `tdd-mutation-proofs.txt` ran in an isolated rsync copy of the working tree as first delivered, including the uncommitted and untracked files. The copy was verified byte-identical to that analyzers tree before the run and again after it, and the shared checkout was never mutated."

The first copy is the accurate one. `tdd-mutation-proofs.txt:3-4` carries the claim as prose — "The shared checkout was never mutated; the copy was verified byte-identical to the delivered analyzers tree before and after this run" — and `:106` carries "ISOLATED COPY RESTORED — identical to the delivered tree". Neither is a captured command with output. A reader who lands on `tdd-result.md:258` is told the copy "was verified", which is the exact wording the document itself disclaims sixteen lines earlier.

**Fix.** Keep the record once, in the mutation section, with its caveat attached. Have the Evidence table's note at `:194-199` point to it rather than restate it.

## Secondary findings

These are confirmed but the verdict does not rest on them.

**S1 — the residual scope figure is inherited rather than re-derived.** `tdd-result.md:395-396` says `"AgentGuard.Engine"` is "spelled roughly 90 times across 25 test files", citing `tdd-review-dry.md` F3's scope note. Live, under `analyzers` and `tests`, the exact literal `"AgentGuard.Engine"` stands 100 times in 21 files, and literals beginning `"AgentGuard.Engine` stand 111 times in 25 files. The 25-file figure matches the second measure; the 90 figure matches neither. Whoever files the cleanup issue should carry 100 occurrences in 21 files, or 111 in 25, not 90 in 25.

**S2 — the rail-1 paragraph counts eight owners where the rest of the document counts ten.** `tdd-result.md:352` says "This round added its eight shared test helpers to `SharedAnalyzerSources.cs`", while `:103-108` records ten owners added, eight during authoring plus `GuardAssemblyName` and `EngineInternalTypeName` during the correction. Eight is the right scope for the rail-1 question, because `tdd-correction-review-dry.md` adjudicated the correction's two constants as rail 4's Fix rather than a new capability. The report never says that, so a reader cannot reconcile the two counts.

**S3 — the review history is written in full twice.** `tdd-result.md:29-35` and `:437-440` both name the five review files and both state that the second correction has not been reviewed. The copies agree today; they are two places to keep in step.

## What I attacked and could not break

**"The three duplications the correction was sent to close are not actually closed."** Refuted against the live files. No `private const string GuardAssembly` and no `private const string EngineInternalType` survives anywhere under `analyzers/AgentGuard.Analyzers.Tests`. `grep -rn 'GuardAssembly\b\|EngineInternalType\b' *.cs` filtered to exclude the two new owner names exits 1, so no copy and no alias remains. `grep -n '"AgentGuard\.Engine' OneDoorIntoCrossPlatformAnalyzerTests.cs OneDoorIntoPerOsAnalyzerTests.cs` exits 1, and the only surviving occurrences of that text in those two files are prose in comments at `OneDoorIntoCrossPlatformAnalyzerTests.cs:146` and `OneDoorIntoPerOsAnalyzerTests.cs:161`. The owners stand at `SharedAnalyzerSources.cs:143` (`internal const string GuardAssemblyName = "guard";`) and `:149` (`internal const string EngineInternalTypeName = EngineAssemblyName + ".EngineInternal";`), and `:149` composes from `EngineAssemblyName` rather than respelling the assembly name.

**"The correction created duplication somewhere the earlier reviews did not look."** Refuted by my own diff of the verified pre-correction snapshot against the live files, over all five changed files, enumerating every added line. `SharedAnalyzerSources.cs` gained the two constants and their two doc comments. `DeclaredTypeScannerTests.cs` gained two repointed call sites. `EngineInternalsOneDoorAnalyzerTests.cs` gained repointed `Reach` rows, the `GatedAssemblies` entry and the `Cross` builder site. The two one-door classes gained four and two repointed `EngineAssemblyName` reads. Every added line is either a read of a shared owner or one of the two new constants with its doc comment. No new literal, no new method, no new fixture fragment, no new assertion.

**"The report describes a snapshot other than the one on disk."** Refuted. All ten delivered files match `tdd-correction-scope-and-digests.txt:43-52`, and the surviving pre-correction copy matches `tdd-digests.txt`. Exactly five rows differ between the two digest files and they are the five the report names.

**"The routed constants do not resolve to the values the deleted private constants held."** Refuted from the captures. Stripping the timing suffix and sorting, the 221 per-case display names in `tdd-affected-classes-detailed.txt` and in `tdd-correction-per-case.txt` are identical, and all 221 are distinct. The post-correction run prints `assemblyName: "guard"` 40 times and `AgentGuard.Engine.EngineInternal` 26 times inside those names, so the routed reads resolve to the deleted constants' values. The per-class counts in `tdd-correction-per-case.txt` are `SymbolResolutionTests` 8, `TypeTreeTests` 4, `DeclaredTypeScannerTests` 2, `WrittenNameScannerTests` 5, `EngineToBoundariesOneDoorAnalyzerTests` 15, `EngineInternalsOneDoorAnalyzerTests` 81, `OneDoorIntoCrossPlatformAnalyzerTests` 33, `OneDoorIntoPerOsAnalyzerTests` 34, `GuardedConstructionAnalyzerTests` 20, `ContractConcreteTypeMustNotBeReferencedAnalyzerTests` 7 and `TimeMustUseTimeProviderAnalyzerTests` 12, summing to 221, exactly as the report lists them. `Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` reports 64 cases, 12 carrying `expectedSubjects: []` and 52 carrying a non-empty expectation.

**"The residual list drops an item one of the reviews confirmed."** Refuted. Every item is carried: F4 through F9 from `tdd-review-dry.md`, R1 through R4 from `tdd-correction-review-dry.md`, plus F3's project-wide scope note and the rail-1 process question. Nothing was dropped; the defect is in the provenance label, not in the coverage.

**"A line reference in the residual list is stale."** Refuted for every one of them, read off the delivered files rather than off the earlier reviews, which carry the pre-correction numbering. `SharedAnalyzerSources.cs:72`, `:73` and `:74` hold the three `InternalsVisibleTo` grants, 71 lines above the new owner at `:143`. `ProhibitedReaches` is at `EngineInternalsOneDoorAnalyzerTests.cs:76` and `ProhibitedContainerReaches` at `:116`; `TypeReferencePositions` is at `SharedAnalyzerSources.cs:749` with fourteen rows, and the two AG0041 tables between them re-spell twelve of those fourteen positions. `SharedAnalyzerSources.cs:765` and `EngineInternalsOneDoorAnalyzerTests.cs:48` are byte-identical. `EngineInternalsOneDoorAnalyzerTests.cs:22`, `:27` and `:30` still spell `AgentGuard.Engine.EngineInternal`, in a file that reads `SharedAnalyzerSources.EngineInternalTypeName` exactly 23 times, and the file spells `AgentGuard.Engine` in ten further places — the constants at `:24`, `:32`, `:36`, `:38`, `:40`, `:42` and `:44`, and the fixtures and message at `:156`, `:253` and `:317`. `"AgentGuard.CrossPlatform"` stands at `OneDoorIntoCrossPlatformAnalyzerTests.cs:117`, `:125`, `:139`, `:150` and `:368` while `SharedAnalyzerSources.CrossPlatformNamespace` at `:465` owns it and `:101` reads it. `OneDoorIntoCrossPlatformAnalyzerTests.cs:148-150` duplicates `RunFromEngineAsync` at `:365-369`, differing only in the first argument. `WrittenNameScannerTests.cs:119` re-spells `WrittenNameScanner.cs:86`. `GuardedConstructionAnalyzerTests.cs:413-414` asserts `"AG0017"` as a literal where `SharedAnalyzerSources.AssertReported` at `:855` is the owner; that file spells an AG identifier as a literal 11 times and 46 test files carry the pattern. `"AgentGuard.CrossPlatform.Decoy"` and `"AgentGuard.CrossPlatform.MacOS"` are spelled in both one-door classes with no owner, inline at `OneDoorIntoCrossPlatformAnalyzerTests.cs:288` and `:308` and named at `OneDoorIntoPerOsAnalyzerTests.cs:85` and `:68`. `"guard"` stands at `GuardedConstructionAnalyzerTests.cs:421` and `:458`, `TimeMustUseTimeProviderAnalyzerTests.cs:208`, `NoServiceAsParameterAnalyzerTests.cs:75`, and `NoStaticServiceHolderAnalyzerTests.cs:75` and `:99`; the two `Process.Start("guard")` sites in `RawPrimitiveOnlyInOwnerAnalyzerTests.cs` are a process name and are correctly left out.

**"The residual items other than the two in this round's new files were created by this round after all."** Refuted against the pre-round snapshot. The three `InternalsVisibleTo` grants stood at `SharedAnalyzerSources.orig.cs:68-70`. `EngineInternalsOneDoorAnalyzerTests.cs` already spelled `"AgentGuard.Engine.EngineInternal"` at its own `:26` and `:28`. `OneDoorIntoCrossPlatformAnalyzerTests.cs` already carried five `"AgentGuard.CrossPlatform"` literals, the same five that stand today.

**"The ten-owner inventory is wrong."** Refuted. Comparing member declarations in `SharedAnalyzerSources.orig.cs` against the live file, ten members were added and none removed: `EngineAssemblyName`, `EngineUsing`, `GatedConsumer`, `RunAgainstFakeEngineAsync`, `AssertHasDocumentationReference`, `SemanticModelAsync`, `TypeIn`, `OnlyNode`, `GuardAssemblyName` and `EngineInternalTypeName`. `git diff --numstat HEAD` for that file is `720 0`, so nothing was removed there since the commit either. `AliasUsing`, `CrossPlatformNamespace`, `AssertReported` and `TypeReferencePositions` all predate the round, so none of them was re-created.

## Rail-by-rail

| rail | ruling |
|---|---|
| 1 — no prior-art-ledger ruling | Not applicable to this correction, which changed the report alone and built no capability. The round's own rail-1 conflict over its eight helpers is unchanged and still needs Tim; see S2 for the count mismatch in how the report states it. |
| 2 — ledger trusted, not verified | Satisfied. Every duplication claim, resolution claim, line reference and count in this review was re-derived from the live files, the two verified snapshots and the raw captures. No claim was taken from `tdd-result.md`, `tdd-review-dry.md` or `tdd-correction-review-dry.md`. |
| 3 — false "new" | Clear. No capability was introduced. The two owners the correction added were adjudicated against `CliAssembly.CompiledName` and `EngineAssembly.Name` in the prior round and the live code still supports that ruling: `EngineInternalsOneDoorAnalyzer.cs` builds the rule's own gate from those production constants, so a test reading them would move gate and fixture together under mutation. |
| 4 — "reuse" or "extract" written as a fresh copy | Clear. Both copies of each value are deleted and every use site reads the one owner. |
| 5 — duplicated value | FAIL on the reporting. The mutation-capture caveat and the byte-identity claim are each spelled in two places rather than held once and referenced, and the two byte-identity copies carry different qualifications. See D2. |
| 6 — duplicated block | FAIL on the reporting, same finding. Two full paragraphs state one record. In the delivered test code the correction added no block anywhere; the pre-existing blocks are listed in the residual section and are correctly located, apart from the provenance label in D1. |
| 7 — single-lens search | Satisfied. grep across the test project and across `analyzers`, `src`, `tests` and `eng` for each literal and owner name, pivoted on the owners, then read the call sites; plus a line-level diff of the pre-correction snapshot against the live files and a member-level diff of `SharedAnalyzerSources.cs` against the pre-round snapshot. |
| 8 — reimplemented primitive | Not applicable. |

## Nothing here needs a design change

Both blocking findings are corrections to this document's own text. Neither asks for a change to any test, any analyzer, any decided design or any contract term.

One item in the residual list continues to need Tim rather than an agent, unchanged by this correction: whether `SharedAnalyzerSources.EngineAssemblySource` interpolates its three `InternalsVisibleTo` grant names from their owners instead of spelling them at `:72-74`, which turns the fixture into an interpolated raw string.
