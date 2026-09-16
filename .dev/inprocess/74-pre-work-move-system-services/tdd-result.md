# TDD result — remaining relocation acceptance tests

## Status

**One branch is uncovered and stays uncovered.** `SymbolResolution.Symbol`'s error-type guard has no direct
test. Every fixture that reaches it must fail to compile, which Acceptance 18 forbids ("no new
expected-compiler-error exception is introduced") and which this round's instruction forbids as a shortcut. It is
uncovered, not covered, and this document claims nothing else. Detail under "One gap, reported rather than papered
over".

**One placement decision needs Tim's sign-off** — where Acceptance 25's direct `CompositionPoint` tests live.
Detail under "One placement decision to sign off".

**One duplication needs Tim's yes or no** — whether `SharedAnalyzerSources.EngineAssemblySource` interpolates its
three `InternalsVisibleTo` grant names from their owners instead of spelling them, which changes how the shared
fixture is built. Detail under "Duplication left unresolved in the delivered files".

**One process question is unanswered** — whether a TDD round's new shared test helpers fall under `rails-dry-code`
rail 1, which requires a `prior-art-ledger` ruling carried in the contract's reuse ledger, when the same round's
instruction forbids the round editing the contract. Detail under "Duplication left unresolved in the delivered
files".

Authoring complete, then corrected twice after review. The L3 correction routed duplicated constants to shared
owners in five of the ten test files and corrected this document; the second correction changed this document
alone. The full analyzer suite is green at 616/616 over the delivered files (`tdd-correction-analyzer-tests.txt`),
up from the 600/600 baseline this round opened with (`tdd-baseline-analyzer-tests.txt`). No test is outstanding, no
test is failing, and nothing in `src/` or `tests/` was touched.

Review history. On the authored files: test-quality PASS (`tdd-review-test-quality.md`), DRY FAIL on three
duplication findings (`tdd-review-dry.md`), and Lie-catcher FAIL on six findings, all in this document's claims
about its own evidence rather than in the tests (`tdd-review-lie-catcher.md`). Tim then authorized the L3
correction, which drew DRY PASS (`tdd-correction-review-dry.md`) and Lie-catcher FAIL on six further findings,
again all in this document's account of its own evidence rather than in the tests
(`tdd-correction-review-lie-catcher.md`). This document has since been corrected against those six findings. That
second correction has not been reviewed.

## Dispatch

Dispatched directly rather than through the TDD workflow script, under the exception Tim approved and recorded in
`contract.md`, Decisions / Approved:

> Tim approved the direct-dispatch exception for the TDD round, presented as written:
>
> - Direct dispatch replaces the file's requirement to use the TDD workflow script for this round only.
> - Regression tests of helpers already implemented during RULE-PHASE may pass immediately. Prove their assertions
>   exercise the required behavior.
> - Tests for behavior still awaiting IMPLEMENT must expose that missing behavior. Never manufacture RED by breaking
>   working code or weakening expectations.
> - This is a run-specific exception to the conflicting workflow and test-rail instructions. It changes neither the
>   relocation scope nor its acceptance requirements.

Every test added this round covers a helper RULE-PHASE already implemented, so every one of them is green on its
first run. Nothing was stubbed, inverted, weakened, or broken to obtain RED. The requirement that came with that
allowance — "Prove their assertions exercise the required behavior" — is discharged by the mutation proofs below.

No test was written for behavior still awaiting IMPLEMENT, because no remaining Acceptance criterion assigns one:
the criteria that turn on the class move (15, 16, 17) are existing tests that must keep passing and two `dotnet
msbuild` project-reference checks, not new test code. The one thing still RED against IMPLEMENT is the production
AG0015 diagnostic recorded in `rule-phase-production-impact.md`, which is production impact and is kept separate
from the acceptance tests throughout this document.

## What was added

Three new direct helper test files, two new tests in an existing file, and one assertion strengthened in three
existing documentation tests. Ten shared owners were added to `SharedAnalyzerSources.cs` — eight during authoring
and two more during the L3 correction. Duplication that remains in the delivered files is listed under "Duplication
left unresolved in the delivered files"; this document makes no claim that the delivered files are free of it.

### New files

- `analyzers/AgentGuard.Analyzers.Tests/SymbolResolutionTests.cs` — 8 tests over the real `SymbolResolution`.
  A written type name, a namespace segment of the same qualified name (resolved by `Symbol`, and NOT resolved by
  `NamedType`, which is why every rule's leaf test may ignore namespaces), a generic name resolving to the
  constructed type with its argument, a member name, tier one on an applied attribute whose node binds the
  CONSTRUCTOR, tier two on a bound type, and the tier ORDER pinned where the tiers disagree: on an invocation,
  `NamedType` answers the bound method's containing type and never reaches the type-info tier, which would answer
  with the return type. The fixture compiles under the runner's default empty compiler-error expectation.
- `analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs` — 2 tests over the real `DeclaredTypeScanner`,
  both halves of the split the extraction had to preserve. The gated entry point DOES scan the return type of an
  invoked member: driven through AG0041 against the shared fake Engine assembly, the three diagnostics are
  identified by their own source spans (`EngineInternal`, `Declared`, `Declared()`), and only the invocation span
  can come from the invoked-return registration. The declaration entry point does NOT: driven through AG0006, the
  one rule that uses `RegisterDeclarations` alone, over a fixture whose invocation really does return the contract
  implementation — asserted from the semantic model, so the negative cannot pass by the call being absent. The L3
  correction deleted this file's private `GuardAssembly` and `EngineInternalType` constants and routed both sites
  to `SharedAnalyzerSources.GuardAssemblyName` (`:68`) and `SharedAnalyzerSources.EngineInternalTypeName` (`:82`);
  both cases still pass and their display names are unchanged.
- `analyzers/AgentGuard.Analyzers.Tests/TypeTreeTests.cs` — 4 tests over the real `TypeTree.Any`, driving pointer,
  array-of-pointer and generic-carrying-a-pointer-array shapes built with the compiler's own symbol factories
  (`CreatePointerTypeSymbol`, `CreateArrayTypeSymbol`, `Construct`). No unsafe source and no runner change. The leaf
  test RECORDS every named type the walk offers it, so each assertion names which types the walk actually reached
  and in what order — a matching pointer target, a NONMATCHING pointer target that is still walked to, and the
  generic-then-array-then-pointer descent.

### Changed files

- `analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs` — the two direct
  `CompositionPoint` caller-identity tests Acceptance 25 requires: the same container declaration compiled once as
  `AgentGuard.Engine` and once as `AgentGuard.Engine.Decoy`, with `CompositionPoint.IsContainerFactoryMethod` — the
  real predicate the gates are built from, not a re-spelling — asserted true and false. The decoy case first asserts
  the namespace, type name, method name and staticness all still match, so the only difference is the declaring
  assembly. Also reads `SharedAnalyzerSources.EngineAssemblyName` where the assembly name was a literal.
- `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs` — ten additions and nothing removed or altered
  (`git diff --numstat` against `HEAD` is `720 0` for this file). Eight during authoring: `EngineAssemblyName`,
  `EngineUsing`, `GatedConsumer` (moved verbatim from `EngineInternalsOneDoorAnalyzerTests`),
  `RunAgainstFakeEngineAsync`, `AssertHasDocumentationReference`, `SemanticModelAsync`, `TypeIn`, `OnlyNode`. Two
  more during the L3 correction: `GuardAssemblyName` (`:143`, the value `"guard"`) and `EngineInternalTypeName`
  (`:149`, composed as `EngineAssemblyName + ".EngineInternal"` rather than respelling the assembly name).
- `analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs` — routed through those shared
  owners; the documentation assertion strengthened. The L3 correction deleted its private `GuardAssembly` and
  `EngineInternalType` constants and routed their use sites to `SharedAnalyzerSources.GuardAssemblyName` (`:54`,
  `:288`) and `SharedAnalyzerSources.EngineInternalTypeName` (23 sites). Every case, table and expectation is
  unchanged: the 221 per-case display names are byte-identical before and after the correction, which is what
  proves the routed constants resolve to the same strings the deleted local constants held
  (`tdd-correction-review-dry.md`, "Independently reported cases — re-run, not trusted").
- `analyzers/AgentGuard.Analyzers.Tests/OneDoorIntoCrossPlatformAnalyzerTests.cs` and
  `analyzers/AgentGuard.Analyzers.Tests/OneDoorIntoPerOsAnalyzerTests.cs` — the documentation assertion
  strengthened during authoring. The L3 correction then routed six `"AgentGuard.Engine"` literals in these two
  files to `SharedAnalyzerSources.EngineAssemblyName`: `OneDoorIntoCrossPlatformAnalyzerTests.cs:150`, `:286`,
  `:306`, `:368` and `OneDoorIntoPerOsAnalyzerTests.cs:375`, `:383`. `grep -n '"AgentGuard\.Engine'` over both
  files now exits 1; the only surviving occurrences of that text are prose in comments
  (`OneDoorIntoCrossPlatformAnalyzerTests.cs:146`, `OneDoorIntoPerOsAnalyzerTests.cs:161`). No expectation changed:
  the per-class counts are 33 and 34 before and after.
- `analyzers/AgentGuard.Analyzers.Tests/GuardedConstructionAnalyzerTests.cs` — AG0017's post-relocation regression
  routed through the same shared runner, so the fake-Engine reference wiring has one owner rather than three copies.
- `analyzers/AgentGuard.Analyzers.Tests/WrittenNameScannerTests.cs` — one line: the third copy of the
  compile-then-model pair routed to the shared owner. All five documentation-context cases are untouched.

### The documentation assertion, and why it changed

Acceptance 24 requires, verbatim:

> Assert the fixture contains a documentation-reference node so the test cannot pass by omitting documentation
> parsing.

The three behavioral documentation tests asserted `Assert.Contains("cref", source, StringComparison.Ordinal)` — a
check on the fixture's TEXT, not on a parsed node, and it does not achieve what the criterion says it is for. A
fixture whose cref text survives but is never parsed into a `CrefSyntax` node passes that check, reaches nothing,
and reports nothing, so the test goes green while proving nothing about the exclusion. The assertion is now
`SharedAnalyzerSources.AssertHasDocumentationReference`, which parses the source exactly as `AnalyzerRunner` parses
it and asserts a `CrefSyntax` node is present. Mutation M5 below demonstrates the hole and that it is now closed.

## Evidence

Every command below was run from the repository root, with its complete output and its immediately captured exit
code retained in this folder.

The L3 correction changed five of the ten delivered files after the authoring runs were captured, so the two sets
of evidence are kept apart. The four correction captures describe the files as delivered. The authoring captures
describe the file state before the correction, and five of the ten files have left that state.

### The delivered files

Captured `2026-09-16T16:06:15Z` to `16:07:12Z`, at `HEAD` `c1c2a1875c14b1a2fb76bf1affc04b386769dccb`, after the
correction's last edit to a test file.

| File | Command | Result |
| --- | --- | --- |
| `tdd-correction-analyzer-tests.txt` | `dotnet test analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj` | `Passed! - Failed: 0, Passed: 616, Skipped: 0, Total: 616`, `EXIT=0` (`:13-14`) |
| `tdd-correction-per-case.txt` | the same with `--logger 'console;verbosity=detailed'` and a `--filter` over the eleven affected classes | `Total tests: 221`, `Passed: 221`, `EXIT=0`, every case named individually |
| `tdd-correction-diff-check.txt` | `git diff --check` | `EXIT=0` |
| `tdd-correction-scope-and-digests.txt` | `git status --short -- src tests`; `git status --short -- analyzers`; `shasum -a 256` over the ten delivered test files | the scoped status is empty at `EXIT=0` (`:5-6`); the analyzers status block is `:8-40`; the ten digests are `:43-52` |

`tdd-correction-scope-and-digests.txt:43-52` is the record of the tested snapshot. It lists all ten delivered files
and matches them byte for byte.

Two gaps in these captures, so a reviewer is not surprised by them. `tdd-correction-per-case.txt:5` and
`tdd-correction-scope-and-digests.txt:42` record their command lines with a placeholder for the argument list —
`--filter <eleven affected classes>` and `shasum -a 256 <the ten delivered test files>` — rather than the exact
command the round's instruction asks for; the output and the exit codes are complete.
`tdd-correction-diff-check.txt` is a single line with no `captured:` or `HEAD:` header, unlike its three siblings.

### Before the correction

These runs describe the ten files as first delivered. `SymbolResolutionTests.cs`, `TypeTreeTests.cs`,
`EngineToBoundariesOneDoorAnalyzerTests.cs`, `GuardedConstructionAnalyzerTests.cs` and `WrittenNameScannerTests.cs`
are byte-identical to the delivered files, so these runs still describe those five. `DeclaredTypeScannerTests.cs`,
`SharedAnalyzerSources.cs`, `EngineInternalsOneDoorAnalyzerTests.cs`, `OneDoorIntoCrossPlatformAnalyzerTests.cs` and
`OneDoorIntoPerOsAnalyzerTests.cs` were changed afterwards and these runs do not describe them.

| File | Command | Result |
| --- | --- | --- |
| `tdd-baseline-analyzer-tests.txt` | `dotnet test analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj` | 600 passed, 0 failed, `EXIT=0` — the baseline this round opened with |
| `tdd-run1-analyzer-tests.txt` | same | `EXIT=1` — the first compile of the new tests, rejected by four repository analyzer rules in six diagnostics (CA1849, S6966, MA0004 ×2, CA1859 ×2) in the new test code. No test ran. |
| `tdd-run2-analyzer-tests.txt` | same | 616 passed, 0 failed, `EXIT=0` |
| `tdd-run3-after-dedup.txt` | same | 616 passed, 0 failed, `EXIT=0` — after routing the duplicated readers to one owner |
| `tdd-final-analyzer-tests.txt` | same | 616 passed, `EXIT=0` — the full suite over the files as first delivered |
| `tdd-digests.txt` | `shasum -a 256` over the ten files as first delivered | the digests of that earlier state. Five of its ten rows no longer match the delivered files; the delivered digests are in `tdd-correction-scope-and-digests.txt:43-52`. |
| `tdd-affected-classes-detailed.txt` | same with `--filter` over the eleven affected classes and `--logger "console;verbosity=detailed"` | 221 passed, 0 failed, `EXIT=0`, every case named individually. Its 221 display names are byte-identical to the correction's per-case run. |
| `tdd-intermediate-build.txt` | `dotnet build -c Release -p:WarningsNotAsErrors=AG0015` | `Build succeeded`, `1 Warning(s)`, `0 Error(s)`, `EXIT=0` |
| `tdd-diff-check-and-status.txt` | `git diff --check`, then `git status --short` | `EXIT=0`, no `src/` or `tests/` path present |
| `tdd-mutation-proofs.txt` | six mutations, each run and reverted in an isolated copy | every mutation turns the intended assertions RED |

Two of these were not re-run after the correction and no post-correction capture of either exists. The intermediate
build is one: `tdd-intermediate-build.txt` was captured at `04:58:29Z`, before the correction. The mutation proofs
are the other: `tdd-mutation-proofs.txt` ran against the pre-correction tree. The correction changed no analyzer
source and no expectation, and its own DRY review re-ran the full suite and the per-case run over the corrected
files and mutated each of the three routed constants in an isolated copy, but neither of the two captures above was
repeated.

### The intermediate build

`tdd-intermediate-build.txt` records one diagnostic and one only: `warning AG0015` at
`src/AgentGuard.Boundaries/SystemServices.cs(88,30)`. That is the production impact
`rule-phase-production-impact.md` records, which IMPLEMENT clears by moving the file, and it is kept visible rather
than suppressed. The allowance is the one the contract grants:

> For ARCHITECTURE and TDD intermediate builds only, AG0015 may remain an enabled warning while `SystemServices`
> remains in Boundaries. Final verification must run without that override.

No other diagnostic exists anywhere in that log, so nothing else is masked by the override.

### Case accounting, read off the run and not off the totals

From `tdd-correction-per-case.txt`, the per-case run over the delivered files, per class: `SymbolResolutionTests` 8,
`TypeTreeTests` 4, `DeclaredTypeScannerTests` 2, `WrittenNameScannerTests` 5,
`EngineToBoundariesOneDoorAnalyzerTests` 15, `EngineInternalsOneDoorAnalyzerTests` 81,
`OneDoorIntoCrossPlatformAnalyzerTests` 33, `OneDoorIntoPerOsAnalyzerTests` 34,
`GuardedConstructionAnalyzerTests` 20, `ContractConcreteTypeMustNotBeReferencedAnalyzerTests` 7,
`TimeMustUseTimeProviderAnalyzerTests` 12. All passed; the file ends `Total tests: 221`, `Passed: 221`, `EXIT=0`.

The same eleven counts stand in the pre-correction run, `tdd-affected-classes-detailed.txt`. Sorted and stripped of
their timing suffix, the 221 display names in the two files are identical and all 221 are distinct, so every case
the pre-correction run reported is still reported under the same name.

AG0041's `Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` reports 64 individual cases in
`tdd-correction-per-case.txt`. Twelve of them carry `expectedSubjects: []` — the twelve separate permitted-public
cases — and the remaining 52 are the separate negative cases. Both counts are preserved exactly, and both were read
from the per-case lines of the actual run over the delivered files.

AG0006's `ContractConcreteTypeMustNotBeReferencedAnalyzerTests` reports its seven existing cases, and the file is
unmodified: `git status --short` lists it not at all.

### Proof that a green regression test is a real one

Two gaps first. None of the six mutations was re-run after the L3 correction, so no capture of any of them
describes the delivered files; the correction changed no analyzer source and no expectation, and
`tdd-correction-review-dry.md` mutated the three routed constants instead — `GuardAssemblyName`,
`EngineInternalTypeName` and `EngineAssemblyName` — recording 34, 31 and 141 failures respectively, which shows the
routed sites are load-bearing, but that is a different set of mutations from the six below. And the byte-identity
of the isolated copy before and after the mutations was asserted in `tdd-mutation-proofs.txt` without a captured
command; it is restated here as an assertion, not as captured evidence.

What each review agent re-ran. `tdd-review-test-quality.md` re-ran all six in its own isolated copy, applying and
reverting each, and its table records every one as matching `tdd-mutation-proofs.txt`, including each "and the
pre-existing tests still pass" half; it then ran four mutations of its own, M7, M9, M11 and M12.
`tdd-review-lie-catcher.md` re-ran four of the six — M3, M4, M5 and M6 — and reproduced each pass/fail split.
M1 and M2 were re-run by the test-quality agent alone. `tdd-review-dry.md` re-ran none: "The shared checkout was not
mutated; no destructive check was needed, so no isolated copy was written."

The test-quality and Lie-catcher agents each verified the ten files against `tdd-digests.txt` before and after
reviewing; the DRY agent verified them against it once, at the start. All three checks were made against the file
state before the L3 correction, and five of those ten files have since changed, so none of them is a present
assurance about the delivered files. The delivered digests are in `tdd-correction-scope-and-digests.txt:43-52`, and
`tdd-correction-review-dry.md` and `tdd-correction-review-lie-catcher.md` each verified all ten against that file.

The six mutations in `tdd-mutation-proofs.txt` ran in an isolated rsync copy of the working tree as first
delivered, including the uncommitted and untracked files. The copy was verified byte-identical to that analyzers
tree before the run and again after it, and the shared checkout was never mutated.

| Mutation | Effect on the tests |
| --- | --- |
| M1 — delete the `IPointerTypeSymbol` branch from `TypeTree.Any` | all 4 `TypeTreeTests` FAIL; the nonmatching-pointer case fails on its recorded walk being empty rather than on a bare false |
| M2 — disable the invoked-return registration in `DeclaredTypeScanner.RegisterForAssembly` | `TheGatedEntryPoint_ScansTheReturnTypeOfAnInvokedMember` FAILS: the `Declared()` span disappears |
| M3 — add the invoked-return registration to `RegisterDeclarations` | both `DeclaredTypeScannerTests` FAIL — and AG0006's seven existing tests all still PASS, so only the new test catches a change to AG0006's registration set |
| M4 — drop the assembly from `CompositionPoint`'s identity conjunction | `ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly` FAILS — and the other 14 AG0040 tests all still PASS, so only the direct test catches it |
| M5 — demote the documented fixture's `///` to `//`, so the text `cref` survives but no `CrefSyntax` node is parsed | all 4 behavioral documentation cases FAIL at the node assertion. The replaced `Assert.Contains("cref", source)` would have passed here, which is the hole the criterion names |
| M6 — delete tier one from `SymbolResolution.NamedType` | `NamedType_OnAnInvocation_StopsAtTheBoundMethodAndNeverReachesTheTypeInfoTier` FAILS (`Sample.Guarded` for `Sample.Holder`) — and the two existing malformed-attribute test classes all still PASS, so only the direct test catches it |

M3, M4 and M6 are the ones worth noting: in each, the pre-existing tests stay green under a real regression, which
is why `remaining-work.md` assigned the direct tests here rather than treating the indirect coverage as sufficient.

## Acceptance mapping

The contract's own words, one row per criterion, with what proves it and where that proof came from.

| # | Contract requirement (verbatim) | Status |
| --- | --- | --- |
| 1 | "`make build` and `make test` pass before and after the implementation; retain exact commands and output." | LATER — final verification, after IMPLEMENT. The opening bracket is `contract-opening-bracket.txt`. This round ran only the contract-authorized intermediate build. |
| 2 | "`analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs` proves AG0040 accepts each of `EnvironmentAdapter.Create()`, `ConsoleAdapter.Create()`, `Ed25519SignatureService.Create()`, and `BuildInfoReader.Create()` when called from `SystemServices.Create()` in an `AgentGuard.Engine` compilation." | EXISTING — `PermittedFactory_FromContainerFactory_IsNotReported`, four theory cases, one per identity. |
| 3 | "The same test file proves AG0040 reports each of these: one of the four factory calls made from another method on `SystemServices`; one of the four made from a different Engine class; a call to a fifth Boundaries factory that returns a service interface but is not one of the four named identities; a call to a `Create` method on a type with one of the four names declared in a different namespace; and a call to a `Create` method on a type with one of the four names declared in an assembly other than `AgentGuard.Boundaries`." | EXISTING — `PermittedFactory_FromAnotherContainerMethod_IsReported`, `PermittedFactory_FromAnotherEngineClass_IsReported`, `FifthFactoryReturningAServiceInterface_FromContainerFactory_IsReported`, `SameNamedFactoryInAnotherNamespace_IsReported`, `SameNamedFactoryInAnotherAssembly_IsReported`. Five separate tests, one per listed shape. |
| 4 | "The same test file proves AG0040 reports a non-factory Boundaries member called from `SystemServices.Create()`, and reports nothing at all for any of these calls made from a compilation that is not `AgentGuard.Engine`." | EXISTING — `NonFactoryMemberOnAPermittedType_FromContainerFactory_IsReported` and `AnyBoundariesCall_FromAnotherAssembly_IsNotReported`. |
| 5 | "The AG0023 and AG0029 tests prove their door is accepted from `SystemServices.Create()` in an `AgentGuard.Engine` compilation, and reported when called from another method on `SystemServices`, from another Engine class, and when the door type is a same-named decoy in another namespace or assembly. Each test asserts the reported message names every condition that failed: the call site alone when the call is the door but the site is wrong, the member alone when the site is right but the call is not the door, and both when both failed. No test asserts a message that says a call to the door is not the door." | EXISTING — in each of the two classes: `DoorCall_FromContainerFactory_IsNotReported`, `DoorCall_FromAnotherContainerMethod_IsReported`, `DoorCall_FromAnotherEngineClass_IsReported`, `SameNamedDoorInAnotherNamespace_FromContainerFactory_IsReported`, `SameNamedDoorInAnotherAssembly_FromContainerFactory_IsReported`, `NonDoorCall_FromContainerFactory_IsReported`. The message half is `SharedAnalyzerSources.AssertNamesExactly`, which checks the required condition is named AND that no unfailed condition is named. |
| 6 | "The three existing tests that exercise an `AgentGuard.Boundaries` compilation — `CallIntoAdapterFactory_FromBoundaries_IsNotReported`, `CallIntoOtherCrossPlatformType_FromBoundaries_IsReported`, and `CallOtherPerOsMember_FromBoundaries_IsReported`, together with `CallPlatformServicesCreate_FromBoundaries_IsNotReported` and `CallOldPlatformDoor_FromBoundaries_IsReported` — still pass with their current expectations, proving the restriction outside Engine was not loosened." | EXISTING — all five named cases appear individually in `tdd-affected-classes-detailed.txt` and passed. |
| 7 | "`CallIntoOtherCrossPlatformType_FromNonBoundariesAssembly_IsNotReported` and `CallOtherPerOsMember_FromNonBoundariesAssembly_IsNotReported` each build an `AgentGuard.Engine` compilation and today assert no diagnostic. Each keeps its prohibited-call input byte for byte, is renamed to describe the prohibited Engine call it makes, and asserts the diagnostic instead. Beside each, a new positive test makes the same rule's permitted door call from the exact `AgentGuard.Engine.SystemServices.Create()` method and asserts no diagnostic. These are the only two assertions either rule's tests may reverse." | EXISTING — `CallIntoOtherCrossPlatformType_FromEngine_IsReported` and `CallOtherPerOsMember_FromEngine_IsReported`, each beside `DoorCall_FromContainerFactory_IsNotReported`. This round reversed no assertion anywhere. |
| 8 | "`analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs` proves AG0041 accepts an invocation of `SystemServices.Create()` and accepts use of a public Engine type and a public Engine member, in both a `guard` and an `AgentGuard.TestHelpers` compilation. Its fake Engine source declares every type and member the rule governs as `internal` and carries `[assembly: InternalsVisibleTo("guard")]` and `[assembly: InternalsVisibleTo("AgentGuard.TestHelpers")]`, so the accepted call is genuinely accessible and the assertion turns on the analyzer rather than on an inaccessibility error." | EXISTING — `ApprovedContainerFactoryCall_IsNotReported` and `FullyQualifiedApprovedContainerFactoryCall_IsNotReported` (two cases each) plus the twelve permitted-public cases. The fake Engine source is unchanged by this round. |
| 9 | "The same test file proves AG0041 reports each of these in both compilations. Through the written-name lens: an internal Engine type in `typeof`, in `nameof`, in a cast, in an `is` pattern, as a generic type argument, in a base list, and through a using alias; and an internal Engine member named anywhere other than the approved call. Through the carried-type lens: an internal Engine type as a field type, a property type, a method return type, a method parameter type, a local variable type, the inferred type of a `var` local, and a type argument nested inside a generic or array type. It proves nothing is reported in an `AgentGuard.Tests` compilation, so that grant is unchanged." | EXISTING — the `ProhibitedReaches` table (28 cases), `EngineInternalInBaseList_FromGatedConsumer_IsReported`, `EngineInternalThroughUsingAlias_FromGatedConsumer_IsReported`, and `EveryProhibitedReach_FromTestsAssembly_IsNotReported`. |
| 10 | "The same test file proves the `SystemServices` exception is no wider than the approved call: `SystemServices` is reported in `typeof`, in `nameof`, as a declared type, in a cast, in a pattern, as a generic argument, in a base list, and through a using alias, and a member of `SystemServices` other than `Create` is reported. Only the invocation of `SystemServices.Create()` is accepted." | EXISTING — the `ProhibitedContainerReaches` table (24 cases), `ContainerInBaseList_FromGatedConsumer_IsReported`, `ContainerThroughUsingAlias_FromGatedConsumer_IsReported`. |
| 11 | "Every AG0041 test asserts the diagnostics it actually expects by count and message. No AG0041 test uses `Assert.Single` on an input that can trip both lenses." | EXISTING — `AssertReportsAsync` asserts the exact count and the exact message multiset; no `Assert.Single` appears in that class. |
| 12 | "AG0040, AG0023, and AG0029 each report their own permitted door call when it is written inside a lambda nested in `Create()`'s body, and again when it is written inside a local function nested in `Create()`'s body." | EXISTING — `*_FromLambdaInsideContainerFactory_IsReported` and `*_FromLocalFunctionInsideContainerFactory_IsReported` in all three test classes. |
| 13 | "AG0006's behavior is unchanged after the `DeclaredTypeScanner` extraction: `analyzers/AgentGuard.Analyzers.Tests/ContractConcreteTypeMustNotBeReferencedAnalyzerTests.cs` passes with every existing test and assertion unmodified." | EXISTING, file unmodified — seven cases, all passing. NEW, additionally: `DeclaredTypeScannerTests.TheDeclarationEntryPoint_DoesNotScanTheReturnTypeOfAnInvokedMember`, which catches a registration-set change that all seven existing tests survive (mutation M3). |
| 14 | "The AG0015 test proves a direct `TimeProvider.System` read is accepted inside `SystemServices` in an `AgentGuard.Engine` compilation and reported elsewhere in Engine." | EXISTING — `TimeProviderSystem_InSystemServicesCreate_IsNotReported` and `TimeProviderSystem_OutsideComposition_IsReported`, with `TimeProviderSystem_InSystemServicesLeftBehindInBoundaries_IsReported` and `TimeProviderSystem_InSameNamedContainerInAnotherAssembly_IsReported` proving the retarget from the other side. |
| 15 | "The existing wiring tests and the four boundary adapter test classes pass unchanged in behavior. `src/AgentGuard.Abstractions/Contracts/ISystemServices.cs` and all four boundary adapter implementations have no behavioral changes, established by `git diff` review." | LATER — IMPLEMENT and final verification. This round changed nothing under `tests/` or `src/`: `git status --short -- src tests` is empty, retained in `tdd-correction-scope-and-digests.txt`. (`tdd-diff-check-and-status.txt` retains `git diff --check` and an unscoped `git status --short`; the scoped command was captured during the L3 correction.) The rename of `BoundariesSystemServicesWiringTests` to `EngineSystemServicesWiringTests` is IMPLEMENT's, per the contract's own words — "This is a mechanical consequence of moving the container, not a separate architecture decision" — and per this round's instruction, "The class move, project-reference changes, internal-access grant changes and production callers belong to IMPLEMENT, not this author." |
| 16 | "For each of `osx-arm64`, `linux-x64`, and `win-x64`, run `dotnet msbuild src/AgentGuard.Engine/AgentGuard.Engine.csproj -getItem:ProjectReference -p:AgentGuardPlatformRid=<rid>`. Each result selects exactly the matching per-OS project and exactly one of them. This checks project selection, not native execution on another OS." | LATER — a final-verification command over project files IMPLEMENT has not yet changed. The project references it inspects are IMPLEMENT's to apply: contract step 6 assigns it: "IMPLEMENT: a separate worker moves `SystemServices.cs` to `src/AgentGuard.Engine/`, changes its namespace... Apply the approved project references, platform-target import, internal-access grants, and caller changes." |
| 17 | "`dotnet msbuild src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj -getItem:ProjectReference` returns `AgentGuard.Abstractions` and nothing else." | LATER — the same final-verification shape, over the Boundaries project file IMPLEMENT has not yet changed. contract step 6 assigns it: "IMPLEMENT: a separate worker moves `SystemServices.cs` to `src/AgentGuard.Engine/`, changes its namespace... Apply the approved project references, platform-target import, internal-access grants, and caller changes." |
| 18 | "Every analyzer test in `analyzers/AgentGuard.Analyzers.Tests` passes using the existing compiler-error validation. `AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs` are unchanged from this run's opening baseline. New relocation fixtures compile under the default empty compiler-error expectation; no new expected-compiler-error exception is introduced. Report any failure requiring work outside this relocation contract rather than starting another runner or unrelated-fixture repair." | MET — 616/616 pass over the delivered files, `tdd-correction-analyzer-tests.txt:13-14`. `git status --short` lists neither runner file, so both are unmodified. No file this round created or edited declares any expected compiler error: `grep -n '"CS[0-9]'` over all ten exits 1. No unrelated-fixture repair was needed or attempted. |
| 19 | "`git diff --check` passes. Review `git diff` and `git status --short` against every contract boundary. Report any unavailable cross-OS execution explicitly; do not claim it ran." | MET for this round — `git diff --check` exit 0 over the delivered files, `tdd-correction-diff-check.txt`, and again before the correction in `tdd-diff-check-and-status.txt`. `git status --short` is retained unscoped in `tdd-diff-check-and-status.txt` and scoped to `analyzers` in `tdd-correction-scope-and-digests.txt:8-40`. The full `git diff` review against every boundary is final verification's. No cross-OS execution was attempted or claimed. |
| 20 | "The AG0023 tests prove the Engine gate reports a reference to a CrossPlatform-core type that is not the door, made from anywhere in an `AgentGuard.Engine` compilation, through both lenses. Through the written-name lens: in `typeof`, in `nameof`, in a cast, in an `is` pattern, as a generic type argument, in a base list, and through a using alias. Through the carried-type lens: as a field type, a property type, a method return type, a method parameter type, a local variable type, the inferred type of a `var` local, and a type argument nested inside a generic or array type. The AG0029 tests prove the same for a per-OS type that is not the door." | EXISTING — `GuardedTypeReference_FromEngine_IsReported` driven by `SharedAnalyzerSources.TypeReferencePositions` (14 positions), plus `GuardedTypeInBaseList_FromEngine_IsReported` and `GuardedTypeThroughUsingAlias_FromEngine_IsReported`, in both classes. |
| 21 | "The AG0023 tests prove the Engine gate accepts a reference to `CrossPlatformAdapters` written inside `AgentGuard.Engine.SystemServices.Create()` — including as the declared type of a local, the form `Create()` uses today — and reports the same reference written in any other method of the Engine compilation. The AG0029 tests prove the same for `PlatformServices`. Both prove a reference to `IPlatformServices` is never reported, because it is declared in `AgentGuard.Abstractions` and is in neither rule's type set." | EXISTING — `DoorLocalDeclaration_InsideContainerFactory_IsNotReported` / `DoorTypeReference_InsideContainerFactory_IsNotReported`, `DoorLocalDeclaration_InAnotherEngineMethod_IsReported` / `DoorTypeReference_InAnotherEngineMethod_IsReported`, and `AbstractionsTypeReference_FromEngine_IsNotReported` in both classes. |
| 22 | "The AG0023 and AG0029 tests prove the Boundaries gate is unchanged by the type-reference extension: a reference to a guarded type that is not a member access, made from an `AgentGuard.Boundaries` compilation, is not reported by either rule." | EXISTING — `ReferenceToOtherCrossPlatformType_FromBoundaries_IsNotReported` and `ReferenceToOtherPerOsType_FromBoundaries_IsNotReported`. |
| 23 | "The two lenses have exactly one owner in the analyzer source, consumed by AG0041, AG0023, and AG0029. No second implementation of either lens exists, established by reading every registration in `analyzers/AgentGuard.Analyzers` during the existing DRY review, not by building an analyzer-analysis framework." | LATER — a DRY review finding, not a test, and it is REFUTE's. This round wrote no analyzer source at all, so it added no second implementation of either lens. |
| 24 | "Behavioral tests prove documentation references to governed types and members are allowed while the corresponding application-code references remain restricted. Assert the fixture contains a documentation-reference node so the test cannot pass by omitting documentation parsing. Existing production documentation links remain unchanged." | EXISTING for the behavioral half — `DocumentationReferenceToGuardedType_FromEngine_IsNotReported` in both one-door classes and `DocumentationReferenceToEngineInternal_IsNotReported` (two cases) for AG0041, each paired with the application-code reference tests of criteria 9, 10 and 20 that report the same type and member. NEW for the node half — `SharedAnalyzerSources.AssertHasDocumentationReference`, one owner, called by all three; mutation M5 shows the previous text assertion left the criterion unmet. No production documentation was touched. |
| 25 | "Prove the privileged caller's full identity: wrong-namespace callers are rejected by the access analyzers; direct tests of `CompositionPoint` reject a same-named method in the wrong assembly. Existing non-Engine tests continue to prove the assembly gate. Tests assert the intended diagnostics and source locations, accommodating multiple scanner reports without weakening an existing expectation." | EXISTING for the analyzer half — `SameNamedFactoryInAnotherNamespace_IsReported`, `SameNamedDoorInAnotherNamespace_FromContainerFactory_IsReported` (both classes), `SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported`, `AnyBoundariesCall_FromAnotherAssembly_IsNotReported`, `EveryProhibitedReach_FromTestsAssembly_IsNotReported`. NEW for the direct half — `ContainerFactoryIdentity_IsTheStaticCreateInTheEngineAssembly` and `ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly`. Source locations: `DeclaredTypeScannerTests` asserts the three diagnostics by their source spans; no existing expectation was weakened anywhere. |
| 26 | "Test generic guarded names and shared type resolution. The existing malformed-attribute tests prove that extracting resolution preserves recovery when symbol resolution fails but type information remains available; keep their inputs, assertions, and compiler-error expectations unchanged. Reuse these tests rather than introduce another malformed-fixture exception." | GENERIC GUARDED NAMES — EXISTING at the rule level (AG0041's `new EngineCache<int>()` row expecting `AgentGuard.Engine.EngineCache<int>`, the `List<EngineInternal>` and `List<EngineInternal[]>` rows, and the generic positions of `TypeReferencePositions` for AG0023/AG0029); NEW at the resolution level (`Symbol_OnAGenericName_ResolvesTheConstructedType`). SHARED TYPE RESOLUTION — NEW, `SymbolResolutionTests`, 8 tests. MALFORMED-ATTRIBUTE TESTS — reused, not rebuilt: `InteropOnlyInCrossPlatformLibrariesAnalyzerTests` and `NativeCallbackBodyMustBeGuardedAnalyzerTests` are both unmodified (absent from `git status --short`), their `CS7036` and `CS0246` expectations intact, and no new malformed fixture or compiler-error exception was introduced. |
| 27 | "Direct tests of `TypeTree.Any` exercise pointer targets using compiler-created type symbols and the existing compilation helper. This proves traversal, not unsafe-source compilation. No runner change is needed." | NEW — `TypeTreeTests`, 4 tests. Symbols built with `Compilation.CreatePointerTypeSymbol`, `CreateArrayTypeSymbol` and `INamedTypeSymbol.Construct` over a fixture compiled by the existing `AnalyzerRunner.CompileAsync`. No unsafe source, no runner change, and the recorded walk is what proves traversal rather than a non-null constructed symbol. |
| 28 | "A regression test proves that a direct `SystemServices.Create()` call written in an `AgentGuard.Tests` compilation reports AG0017. The fixture's `SystemServices` is declared in namespace and assembly `AgentGuard.Engine` and its `Create()` is reachable, so the assertion turns on the analyzer rather than on an inaccessibility error. This pins the one wall that remains against the main test assembly after the relocation gives it compiler visibility of the container." | EXISTING — `GuardedConstructionAnalyzerTests.CreateCall_FromTheMainTestAssembly_IsReported`, against the shared fake Engine assembly whose `InternalsVisibleTo("AgentGuard.Tests")` grant makes the call compile. Only its reference wiring was routed to the shared owner this round; the fixture, the assertion and the assembly name are unchanged. |
| 29 | "The documentation exclusion is a `CrefSyntax` ancestor check. A direct test proves it is true for a cref naming a guarded type, including a generic cref whose node sits under a type-argument list and an operator's cref parameter list, and false for the same identifier written in code and for a preprocessor-directive name." | EXISTING and complete — `WrittenNameScannerTests`: `EveryNameInsideACref_IsADocumentationReference`, `TheGenericAndOperatorCrefShapes_AreBothCovered`, `TheSameNameWrittenInCode_IsNotADocumentationReference`, `APreprocessorDirectiveName_IsNotADocumentationReference`, `ACrefName_ResolvesToTheSameSymbolAsTheCodeName`. Every clause of the criterion has its own case, so nothing was added. |

## One gap, reported rather than papered over

`SymbolResolution.Symbol` ends with a guard that treats a resolution yielding only an error type as no resolution:

    return bound is INamedTypeSymbol { TypeKind: TypeKind.Error } ? null : bound;

That guard has no direct test. Every way to make the semantic model hand back an error type requires a fixture the
compiler rejects, and this round's instruction forbids exactly that shortcut — "do not create an invalid
unresolved-name fixture as a shortcut around the compiler-error requirement" — as does Acceptance 18: "New
relocation fixtures compile under the default empty compiler-error expectation; no new expected-compiler-error
exception is introduced." The analogous case for `NamedType` — nothing binds at any tier, so the caller's
fail-closed fallback runs — is covered by the existing
`InteropOnlyInCrossPlatformLibrariesAnalyzerTests.UnresolvedInteropAttribute_IsReportedViaSyntacticFallback`, which
declares its two `CS0246` occurrences and is unmodified. This is stated as an uncovered branch, not claimed as
covered.

## One placement decision to sign off

Acceptance 25's direct `CompositionPoint` tests went into
`analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs`. The contract's Surfaces list names
no `CompositionPointTests.cs`, and this round's instruction requires the check to land "in a contract-listed test
surface". AG0040 is the rule the contract names for the method-level check — "The method-level check in
`CompositionPoint` identifies only the static `Create()` method on `AgentGuard.Engine.SystemServices` in the Engine
assembly, and is used for the factory-access rules" — and that class already owns the wrong-namespace and
wrong-assembly decoy proofs for the CALLED member, so the matching decoy proofs for the CALLER sit beside them. The
predicate is shared by AG0040, AG0023, AG0029 and AG0041, so if a reviewer prefers a different listed surface it is
a one-file move.

## Duplication left unresolved in the delivered files

The L3 correction closed the three duplications `tdd-review-dry.md` blocked on — the two private constants and the
six Engine assembly-name literals — and `tdd-correction-review-dry.md` records them as resolved and records that the
correction introduced no new duplication. What follows is the duplication those two reviews confirmed and left
standing in the delivered files. None of it was created by this round or by the correction.

**One item needs Tim's yes or no.** `"guard"` is still spelled inside the fixture in the file that now owns it:
`SharedAnalyzerSources.cs:72`, `[assembly: InternalsVisibleTo("guard")]`, 71 lines above the new owner at `:143`.
`"AgentGuard.TestHelpers"` (`:73`) and `"AgentGuard.Tests"` (`:74`) are the same shape. The prior review's fix asked
for these three grant names to be interpolated from their owners, which turns `EngineAssemblySource` into an
interpolated raw string — a change to how the shared fixture is built, and outside the correction's instruction.
`tdd-correction-review-dry.md` records it as "a live rail-5 violation and it needs a yes or a no from Tim, not an
agent's judgement."

**One process question is unanswered.** This round added its eight shared test helpers to `SharedAnalyzerSources.cs`
with no `prior-art-ledger` run and no reuse-ledger row, which `rails-dry-code` rail 1 calls a Violation. `tdd-review-dry.md`
did not rest its FAIL on it, because the ruling has to be carried in the contract and this round's instruction says
"The manager makes no repository edits, including edits to this file, the contract, test code or results." It asks
Tim to settle the conflict: "either TDD-stage helpers are inside rail 1 and the stage needs a way to record a
ruling, or they are outside it and the rail should say so." `tdd-correction-review-dry.md` records the conflict as
unchanged by the correction and still needing Tim.

The rest is for whoever schedules the cleanup. `tdd-review-dry.md` fix item 3 asks for one issue covering the
project-wide part of it.

- AG0041's `ProhibitedReaches` (`EngineInternalsOneDoorAnalyzerTests.cs:76`) and `ProhibitedContainerReaches`
  (`:116`) re-spell twelve of the fourteen rows of `SharedAnalyzerSources.TypeReferencePositions` (`:749`),
  verbatim but for the type name. The clearest instance is byte-identical: `SharedAnalyzerSources.cs:765` and
  `EngineInternalsOneDoorAnalyzerTests.cs:48` both spell
  `"        internal static object Inferred() { var value = Declared(); return value; }\n"`. The AG0041 tables carry
  per-row expected subjects the shared table does not, so collapsing them is not free. `tdd-review-dry.md` F4
  records it as "the largest fixture duplication left in the delivered tree".
- `EngineInternalsOneDoorAnalyzerTests.cs:22`, `:27` and `:30` still spell
  `AgentGuard.Engine.EngineInternal` as a literal, in the same file that reads
  `SharedAnalyzerSources.EngineInternalTypeName` 23 times. The file spells `AgentGuard.Engine` in ten more places:
  the constants at `:24`, `:32`, `:36`, `:38`, `:40`, `:42` and `:44`, and the inline fixtures and message at
  `:156`, `:253` and `:317`. All thirteen predate the correction. `tdd-correction-review-dry.md` R2.
- `"AgentGuard.CrossPlatform"` stands un-routed at `OneDoorIntoCrossPlatformAnalyzerTests.cs:117`, `:125`, `:139`,
  `:150` and `:368` while `SharedAnalyzerSources.CrossPlatformNamespace` (`:465`) owns it and `:101` already reads
  it. Two of those lines, `:150` and `:368`, are lines the correction rewrote: the Engine half was routed and the
  CrossPlatform half on the same line was not. `tdd-correction-review-dry.md` R3.
- `OneDoorIntoCrossPlatformAnalyzerTests.cs:148-150` is an inline copy of the file's own `RunFromEngineAsync`
  (`:365-369`), differing only in the source argument. `tdd-correction-review-dry.md` R4.
- `WrittenNameScannerTests.cs:119` partitions the fixture with the same expression it then asserts
  `IsDocumentationReference` against, which is `WrittenNameScanner.cs:86`. `tdd-review-dry.md` F5, raised there for
  the test-quality reviewer as well, because two of the cases are true by construction of their own input.
- `GuardedConstructionAnalyzerTests.cs:413-414` asserts `"AG0017"` as a literal where
  `GuardedConstructionAnalyzer.ContainerDiagnosticId` and `SharedAnalyzerSources.AssertReported` (`:855`) are the
  owners. The same file spells an AG identifier as a literal 11 times, and the pattern runs across 46 test files.
  `tdd-review-dry.md` F6.
- `"AgentGuard.CrossPlatform.Decoy"` and `"AgentGuard.CrossPlatform.MacOS"` are each spelled in both one-door
  classes with no owner. `tdd-review-dry.md` F7.
- `TypeTreeTests.cs` repeats the same three arrange-and-act lines in each of its four cases (`:39`, `:41`, `:43`;
  `:55`, `:57`, `:59`; `:69`, `:72`, `:74`; `:86`, `:90`, `:92`), and the "sort both sides ordinally, then
  `Assert.Equal`" shape is spelled in both `DeclaredTypeScannerTests.cs:75-78` and
  `EngineInternalsOneDoorAnalyzerTests.cs:305-309`. Both are in this round's new code. `tdd-review-dry.md` F8 and
  F9, reported there rather than blocked on.
- `"AgentGuard.Engine"` is spelled roughly 90 times across 25 test files, most of them untouched by this work
  (`tdd-review-dry.md` F3's scope note). `"guard"` is spelled at `GuardedConstructionAnalyzerTests.cs:421` and
  `:458`, `TimeMustUseTimeProviderAnalyzerTests.cs:208`, `NoServiceAsParameterAnalyzerTests.cs:75`, and
  `NoStaticServiceHolderAnalyzerTests.cs:75` and `:99` (`tdd-correction-review-dry.md` R1).

## Delivered files

New: `SymbolResolutionTests.cs`, `DeclaredTypeScannerTests.cs`, `TypeTreeTests.cs`.
Changed: `SharedAnalyzerSources.cs`, `EngineToBoundariesOneDoorAnalyzerTests.cs`,
`EngineInternalsOneDoorAnalyzerTests.cs`, `OneDoorIntoCrossPlatformAnalyzerTests.cs`,
`OneDoorIntoPerOsAnalyzerTests.cs`, `GuardedConstructionAnalyzerTests.cs`, `WrittenNameScannerTests.cs`.
All ten are under `analyzers/AgentGuard.Analyzers.Tests/`. Their SHA-256 digests as delivered are recorded in
`tdd-correction-scope-and-digests.txt:43-52`, so a reviewer can confirm the snapshot it reads is the snapshot this
round produced. `tdd-digests.txt` records the earlier state, before the L3 correction; five of its ten rows —
`DeclaredTypeScannerTests.cs`, `SharedAnalyzerSources.cs`, `EngineInternalsOneDoorAnalyzerTests.cs`,
`OneDoorIntoCrossPlatformAnalyzerTests.cs` and `OneDoorIntoPerOsAnalyzerTests.cs` — no longer match, and those five
are exactly the files the correction changed.

The tested snapshot is `HEAD` `c1c2a1875c14b1a2fb76bf1affc04b386769dccb` on branch `appd-1-process`, with the
uncommitted and untracked changes under `analyzers/` that `tdd-correction-scope-and-digests.txt:8-40` lists and
nothing under `src/` or `tests/` (`:5-6`). The only other uncommitted changes in the tree are this work folder's
own stage records.

Nothing else was touched. No production code, no analyzer source, no project file, no grant, no workflow script, no
contract. Nothing was staged, committed or pushed.

## Remaining IMPLEMENT work

Unchanged by this round, and none of it is test authoring:

1. Move `src/AgentGuard.Boundaries/SystemServices.cs` to `src/AgentGuard.Engine/SystemServices.cs` in namespace
   `AgentGuard.Engine`, preserving its shape, service instances and construction order. This is what clears the one
   AG0015 diagnostic the intermediate build still carries.
2. The project references, the `PlatformImplementation.targets` import move, the internal-access grants, and the
   CLI and test-builder `using` changes.
3. The mechanical rename of `BoundariesSystemServicesWiringTests` to `EngineSystemServicesWiringTests`, file and
   class, and the comments in the four boundary adapter test classes that name the assembly the container lives in.
4. Acceptance 1, 15, 16, 17 and the full Acceptance 19 review, which are final verification and run without the
   AG0015 override.

## Handoff

Nothing in this document is a review verdict. The independent verdicts on the authored files are in
`tdd-review-test-quality.md`, `tdd-review-dry.md` and `tdd-review-lie-catcher.md`, and on the L3 correction in
`tdd-correction-review-dry.md` and `tdd-correction-review-lie-catcher.md`. The second correction — this document
alone, against the six findings of `tdd-correction-review-lie-catcher.md` — has not been reviewed.
