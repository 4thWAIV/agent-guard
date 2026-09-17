# DRY review — TDD round, SystemServices relocation

## Verdict: FAIL

Two duplicated values were introduced by this round's own new file, both naming the one shared fake-Engine fixture whose
other identity constants this same round hoisted into the shared owner. `rails-dry-code` rail 5 — "Every literal string,
number, or path lives in exactly one named owner and is referenced from there" — is violated, and its verdict rule is
"Return FAIL when any Violation is confirmed."

A third confirmed duplication sits in the round's changed files: the new `EngineAssemblyName` owner was routed into one
test class while six spellings of the same literal remain in the two other one-door classes the round edited.

The suite being green at 616/616 does not bear on any of this, and neither does another reviewer's PASS.

## Snapshot reviewed

The ten delivered files under `analyzers/AgentGuard.Analyzers.Tests/`, verified byte-identical to the digests in
`tdd-digests.txt` (`shasum -a 256`, all ten match). The shared checkout was not mutated; no destructive check was needed,
so no isolated copy was written.

Both discovery lenses were re-run by me, not taken from the author's report (rail 2):

- grep across `analyzers`, `src`, `tests`, `eng` for every new owner name, every candidate literal, and every
  pre-existing owner the new helpers could have duplicated.
- CodeGraph (`codegraph_explore`) over `SemanticModelAsync TypeIn OnlyNode AnalyzerRunner CompileAsync`, which returned
  `CompileAsync` (`analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs:115`) as the one compile owner with 13 callers
  in `SharedAnalyzerSources.cs` plus `DerivedBoundaryServicesTests`, `AnalyzerRunnerTests`,
  `EngineToBoundariesOneDoorAnalyzerTests` and `TypeTreeTests`, and no competing semantic-model, type-lookup or
  node-lookup owner anywhere.
- A mechanical cross-file literal scan over all ten delivered files for values spelled in more than one of them.

Pre-round comparison used `.../scratchpad/SharedAnalyzerSources.orig.cs` (captured 2026-09-15 19:24, three hours before
this round's 22:34 baseline run) and `.../scratchpad/lc-iso/analyzers/AgentGuard.Analyzers.Tests/` (17:02–17:12), so the
claims below about what this round changed are read off real before/after content, not off `tdd-result.md`.

## Blocking findings

### F1 — `GuardAssembly = "guard"` spelled twice (rail 5)

- `analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs:21` — `private const string GuardAssembly = "guard";`
- `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs:17` — `private const string GuardAssembly = "guard";`

Identical constant name, identical value, same meaning: the gated consumer assembly that reaches the one shared fake
Engine source. `DeclaredTypeScannerTests.cs` is new in this round; the copy in `EngineInternalsOneDoorAnalyzerTests.cs`
already existed at the round's baseline. The value also appears inside the shared fixture itself at
`SharedAnalyzerSources.cs:72` (`[assembly: InternalsVisibleTo("guard")]`), so there are now three spellings of one
identity with no owner.

### F2 — `EngineInternalType = "AgentGuard.Engine.EngineInternal"` spelled twice (rail 5)

- `analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs:23`
- `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs:24`

Again identical name and value, and again it names a member of the one shared fixture
(`SharedAnalyzerSources.EngineAssemblySource:96`, `internal class EngineInternal`). Both files use it for the same
purpose — the subject an AG0041 message must name (`DeclaredTypeScannerTests.cs:86`,
`EngineInternalsOneDoorAnalyzerTests.cs:81-116`).

**Why F1 and F2 are blocking rather than cosmetic.** This round was editing `SharedAnalyzerSources.cs` and, in that same
edit, hoisted two other constants of exactly this kind into it — `EngineAssemblyName` (`SharedAnalyzerSources.cs:129`)
and `EngineUsing` (`:136`) — and deleted the local `EngineUsing` copy from `EngineInternalsOneDoorAnalyzerTests.cs`
(verified against the 17:12 snapshot). It removed one named duplication of the fake-Engine identity and created two more
in the file it wrote in the same round. The instruction it was given says, in its own words:

> Inspect all new and changed test code for repeated fixture construction, assertion logic and literals before
> submitting. Search beyond the first named occurrence and reuse the appropriate existing owner.

The precedent is also four hours old and lives in this same work folder:
`rule-phase-using-literal-correction.md` records six fixture literals being routed to their existing owners through
interpolated raw strings (`$$"""` with `{{owner}}`), after a DRY reviewer found that a completeness claim about exactly
this defect class was false. The mechanism for the complete fix is already in the repo.

**Fix.** Move both values to `SharedAnalyzerSources`, beside `EngineAssemblyName`/`EngineUsing`, and reference them from
both classes. While there: `TestHelpersAssembly` (`EngineInternalsOneDoorAnalyzerTests.cs:19`) and `TestsAssembly`
(`:21`) are the two other consumer identities of the same fixture and belong in the same place, and
`EngineAssemblySource` can interpolate all three grant names rather than spelling them at `:72-74`.

### F3 — the new `EngineAssemblyName` owner is routed into one file while six copies stand in the other files this round edited (rail 5, rail 7)

`SharedAnalyzerSources.EngineAssemblyName` (`:129`) is read at five sites, all in
`EngineToBoundariesOneDoorAnalyzerTests.cs` (`:84`, `:90`, `:220`, `:259`, `:295`) plus internally at
`SharedAnalyzerSources.cs:820`. The same literal stands unrouted in the other one-door classes this round edited:

- `OneDoorIntoCrossPlatformAnalyzerTests.cs:150`, `:286`, `:306`, `:368`
- `OneDoorIntoPerOsAnalyzerTests.cs:375`, `:383`

Within the delivered set the literal is also spelled ten times in `GuardedConstructionAnalyzerTests.cs`
(`:377`, `:385`, `:436`, `:470`, `:488`, `:496`, `:512`, `:532`, `:560`, `:572`), another file this round edited.

Scope note, so the fix is not mis-sized: the literal is spelled roughly 90 times across 25 test files, most of them
untouched by this work. Rewriting those is a separate cleanup and should be filed as an issue rather than smuggled into
a TDD round. The six sites above are in files this round already edited, and routing them is a mechanical change with no
behavioural effect.

## Confirmed duplication that this round did not create

Reported for the record and for whoever schedules the cleanup. None of it is the basis of the verdict, and all of it is
verified against the before/after snapshots as pre-dating this round.

### F4 — AG0041's reach table re-spells the shared position table

`SharedAnalyzerSources.TypeReferencePositions` (`:736-757`) is the one owner of "every position an Engine member can
reach a guarded type through", built for AG0023 and AG0029. Twelve of its fourteen rows are spelled again, verbatim
modulo the type name, in `EngineInternalsOneDoorAnalyzerTests.ProhibitedReaches` (`:80-116`) and
`ProhibitedContainerReaches` (`:120-141`). The clearest single instance:

- `SharedAnalyzerSources.cs:752-753` —
  `"        internal static object Inferred() { var value = Declared(); return value; }\n" + "        private static " + guardedType + " Declared() => null!;"`
- `EngineInternalsOneDoorAnalyzerTests.cs:51-53` — the same two lines with `guardedType` resolved to `EngineInternal`.

These are byte-identical strings. The AG0041 tables carry per-row expected subjects that the shared table does not, so
the collapse is not free — the shared table would have to yield the declaration and let each consumer attach its own
expectation. It is the largest fixture duplication left in the delivered tree and it sits in a file this round edited.

### F5 — the documentation predicate is re-spelled in the test's own selection helper

- `analyzers/AgentGuard.Analyzers/WrittenNameScanner.cs:86` — `return node.FirstAncestorOrSelf<CrefSyntax>() is not null;`
- `analyzers/AgentGuard.Analyzers.Tests/WrittenNameScannerTests.cs:119` — `.Where(name => (name.FirstAncestorOrSelf<CrefSyntax>() is not null) == inCref)`

The test partitions the fixture's names with the same expression it then asserts `IsDocumentationReference` against, so
`EveryNameInsideACref_IsADocumentationReference` (`:50`) and `TheSameNameWrittenInCode_IsNotADocumentationReference`
(`:74`) are true by construction of their own input. The class is not worthless — `APreprocessorDirectiveName_…`
(`:84`) is a genuine discriminator that a switch to `IsPartOfStructuredTrivia` would fail — but the duplication is real
and the tautology is worth the test-quality reviewer's attention. This round touched this file on one line only
(`:100`, routing the compile-then-model pair to `SharedAnalyzerSources.SemanticModelAsync`), verified against the
01:40 snapshot.

### F6 — hardcoded `"AG0017"` where a typed constant and an assertion owner both exist

`GuardedConstructionAnalyzerTests.cs:413-414`:

    Assert.Equal("AG0017", diagnostic.Id);
    Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);

`GuardedConstructionAnalyzer.ContainerDiagnosticId` (`analyzers/AgentGuard.Analyzers/GuardedConstructionAnalyzer.cs:44`)
is the typed constant, and `SharedAnalyzerSources.AssertReported` (`:842`) is the one owner of that exact pair of
assertions — its own doc says it exists "so a test class proves its rule fired by handing over its analyzer's own
`DiagnosticId` constant instead of re-spelling the sequence and the identifier". This is the Acceptance 28 regression
test, and this round rewrote the statement immediately above these two lines (`:409-411`), so the owner was one line
away. The same file spells an AG identifier as a literal 11 times, and the pattern is the project-wide legacy style: 46
test files, hundreds of occurrences. Routing the whole file is the right fix; routing this one occurrence alone would
be another partial dedup.

### F7 — sibling one-door classes duplicate each other's decoy identities

- `"AgentGuard.CrossPlatform.Decoy"` — `OneDoorIntoCrossPlatformAnalyzerTests.cs:288` (inline) and
  `OneDoorIntoPerOsAnalyzerTests.cs:85` (named `DecoyAssembly`).
- `"AgentGuard.CrossPlatform.MacOS"` — `OneDoorIntoCrossPlatformAnalyzerTests.cs:308` (inline) and
  `OneDoorIntoPerOsAnalyzerTests.cs:68` (named `PerOsAssembly`).

Both classes drive the same decoy and per-OS assemblies through the same shared fixture builders; neither name has an
owner. Pre-existing (RULE-PHASE), in files this round edited.

## Minor, in this round's new code

### F8 — `TypeTreeTests` repeats its arrange/act four times

`TypeTreeTests.cs:39/41/43`, `:55/57/59`, `:69/72/74`, `:86/90/92` each spell
`Compilation compilation = await AnalyzerRunner.CompileAsync(LeafTypesSource);`, `var offered = new List<string>();`
and `bool found = TypeTree.Any(root, Record(offered));`. One `WalkAsync(Func<Compilation, ITypeSymbol> build)` returning
the root, the answer and the recorded walk would collapse twelve lines to four without touching what any case proves.
Borderline against rail 6, and it mirrors the pre-existing style of `WrittenNameScannerTests` (`:54`, `:66`, `:76`,
`:88`), so it is reported rather than treated as blocking.

### F9 — the sorted-multiset comparison is spelled twice

`DeclaredTypeScannerTests.cs:79-82` (span texts) and `EngineInternalsOneDoorAnalyzerTests.cs:309-313` (messages) both
build the "sort both sides ordinally, then `Assert.Equal`" shape. Different subjects, same shape; one owner taking two
sequences would serve both.

## Rail 1 — the ledger, and the conflict in it

This round built eight new shared test helpers with no `prior-art-ledger` run and no reuse-ledger row. Rail 1 calls
that a Violation and calls itself a locked law. It is also impossible for this author to satisfy as written: the ruling
has to be "carried in the contract's reuse ledger", and this round's instruction says "The manager makes no repository
edits, including edits to this file, the contract, test code or results", with the contract itself the task authority.
I am not resting the FAIL on it. Instead I ran the check the ledger exists to force, myself, per rail 2 and rail 3, and
the result is below — no ruling would have come out wrong.

Bring the conflict to Tim as a process question: either TDD-stage helpers are inside rail 1 and the stage needs a way to
record a ruling, or they are outside it and the rail should say so.

## What I tried to refute, and how it came out

**"The eight additions to `SharedAnalyzerSources.cs` removed or altered something."** Refuted. `diff` against the
pre-round snapshot (`scratchpad/SharedAnalyzerSources.orig.cs`, 19:24, three hours before the round's baseline) returns
**zero** removed lines — every addition is new text: four `using` directives, `EngineAssemblyName`, `EngineUsing`,
`GatedConsumer`, `SemanticModelAsync`, `TypeIn`, `OnlyNode`, `RunAgainstFakeEngineAsync`,
`AssertHasDocumentationReference`. Nothing existing was edited.

**"The new owners are single-use speculation."** Refuted. External use sites, counted by grep:
`EngineUsing` 9, `TypeIn` 6, `EngineAssemblyName` 5, `GatedConsumer` 5, `RunAgainstFakeEngineAsync` 3,
`AssertHasDocumentationReference` 3, `SemanticModelAsync` 3, `OnlyNode` 3. Every one is read by at least two classes
(`RunAgainstFakeEngineAsync`: `EngineInternalsOneDoorAnalyzerTests`, `DeclaredTypeScannerTests`,
`GuardedConstructionAnalyzerTests`; `AssertHasDocumentationReference`: the two one-door classes and AG0041).

**"A new helper duplicates an owner that already existed."** Refuted for all eight. Grep across the solution returns
exactly one `GetSemanticModel` (`SharedAnalyzerSources.cs:771`), one `GetTypeByMetadataName` (`:784`) and one
`DescendantNodes().OfType<T>()` walk (`:800`), all of them the new owners; CodeGraph returns no competing owner and
confirms `AnalyzerRunner.CompileAsync` is reused rather than reimplemented. `GatedConsumer` is the verbatim body of the
old private `Consumer` in `EngineInternalsOneDoorAnalyzerTests`, which was deleted in the same edit (17:12 snapshot,
lines 296-311) — an extract, not a copy.

**"`SharedAnalyzerSources.EngineAssemblyName` is a false 'new' — `EngineAssembly.Name` already owns that value."**
Refuted, twice over. `analyzers/AgentGuard.Analyzers/EngineAssembly.cs:20` does hold
`internal const string Name = "AgentGuard.Engine"`, and the analyzer project does grant
`InternalsVisibleTo("AgentGuard.Analyzers.Tests")` (`AgentGuard.Analyzers.csproj:27`), so reading it is possible. It
should still not be read: the value is the assembly name a fixture is **compiled into**, and if the test took it from
the rule, a mutation of `EngineAssembly.Name` would move the gate and the fixture together and every Engine-gate test
would keep passing through a real regression. The test layer's separate ownership is also the established pattern here,
not an invention of this round — `SharedAnalyzerSources.CrossPlatformNamespace` (`:452`) already mirrors
`CrossPlatformBoundary.RootName` (`analyzers/AgentGuard.Analyzers/CrossPlatformBoundary.cs:19`) for exactly this reason,
and the file says so of the message fragments: "spelled here once — in the test layer, never read back off the
analyzer". Not a violation.

**"The new fixtures duplicate an existing fixture."** Refuted. `SymbolResolutionTests.ResolutionSource` (`:23-53`) and
`WrittenNameScannerTests.DocumentedSource` (`:20-47`) both declare a `Sample.Guarded` and a `Sample.Cache<T>`, but they
cannot be merged: the resolution tests pick nodes with `OnlyNode<GenericNameSyntax>` and `OnlyNode<QualifiedNameSyntax>`
(`:103`, `:176`, `:181`), which require exactly one such node in the tree, and the documentation fixture's crefs add
more. `TypeTreeTests.LeafTypesSource` needs a second unrelated leaf (`Sample.Other`) that neither other fixture has.
`"Sample.Guarded"` being spelled in both `SymbolResolutionTests.cs:57` and `TypeTreeTests.cs:30` is two independent
fixtures naming their own sample type, each behind its own class constant; hoisting that would be false sharing, and
the round's instruction pushes the other way — "Share setup and test data rather than bundle separate cases into one
source fixture to avoid duplication."

**"The round bundled independently reported scenarios."** Refuted, from the per-case output rather than the totals.
`tdd-affected-classes-detailed.txt` carries 64 `Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` case
lines, all `Passed`: 12 carry `expectedSubjects: []` and 52 carry a non-empty expectation. The 12 are the six public
reaches (`Typed`, `Member`, `Field`, `Returned`, `Parameter`, `Argument`) crossed with the two gated consumers, each its
own reported case. The class total is 81, and `rule-phase-using-literal-correction.md` records the same 81 at 20:02,
before this round opened — so no case was merged, split or lost here. The `PublicEngineApi_IsNotReported` bundle did
become the 12-case table, but in RULE-PHASE fix5, not in this round: the round's test count moved 600 → 616, and the +16
is exactly 8 `SymbolResolutionTests` + 4 `TypeTreeTests` + 2 `DeclaredTypeScannerTests` + 2 new `CompositionPoint`
tests. The author's claim that "Every case, table and expectation is unchanged" in that class is therefore accurate for
this round.

**"The new tests hardcode diagnostic identifiers."** Refuted. The only `AG0` string in the three new files is inside a
comment (`DeclaredTypeScannerTests.cs:94`); the assertions use
`EngineInternalsOneDoorAnalyzer.DiagnosticId` (`:74`) and `ContractConcreteTypeMustNotBeReferencedAnalyzer.DiagnosticId`
(`:105`) through the shared `AssertAllReported`/`AssertReported` owners. The hardcoded-id finding (F6) is confined to
pre-existing code.

**"Acceptance 25's caller check re-implements identity matching in the test oracle."** Refuted.
`EngineToBoundariesOneDoorAnalyzerTests.cs:261` and `:278` call the real predicate,
`CompositionPoint.IsContainerFactoryMethod`. The decoy case asserts namespace, type name, method name and staticness
separately first (`:274-277`) to show the assembly is the only difference — assertions about the fixture, not a second
copy of the conjunction.

## One inaccuracy in the delivery report

`tdd-result.md` says "Five shared owners were added to `SharedAnalyzerSources.cs`" (Status/What was added) and "five
additions and nothing removed or altered" (Changed files), then lists eight names in the same sentence. Eight members
were added; the "nothing removed or altered" half is correct and I verified it. Miscount, not a misrepresentation, but
it should be corrected before the result is read as an inventory.

## Fix list, in order

1. Hoist `GuardAssembly` and `EngineInternalType` into `SharedAnalyzerSources` beside `EngineAssemblyName`, and
   reference them from both `DeclaredTypeScannerTests.cs` and `EngineInternalsOneDoorAnalyzerTests.cs`. Take
   `TestHelpersAssembly` and `TestsAssembly` with them and interpolate all three into `EngineAssemblySource`'s
   `InternalsVisibleTo` grants, the way `rule-phase-using-literal-correction.md` did it. (F1, F2 — blocking)
2. Route the six `"AgentGuard.Engine"` literals in `OneDoorIntoCrossPlatformAnalyzerTests.cs` and
   `OneDoorIntoPerOsAnalyzerTests.cs` to `SharedAnalyzerSources.EngineAssemblyName`. (F3 — blocking)
3. File an issue for the project-wide cleanups that must not ride along in a TDD round: the ~90 remaining
   `"AgentGuard.Engine"` spellings, the hardcoded AG identifiers in 46 test files (F6), and the AG0041 reach table
   against `TypeReferencePositions` (F4).
4. Put F5 in front of the test-quality reviewer; it is a duplication with a tautology attached, not only a DRY item.
5. Correct the "five owners" count in `tdd-result.md`.

Nothing in this review asks for a change to a decided design, and nothing here needs Tim's sign-off beyond the rail-1
process question above.
