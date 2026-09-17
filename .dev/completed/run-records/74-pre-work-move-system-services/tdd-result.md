# TDD result — SystemServices relocation

This is the TDD stage result the IMPLEMENT stage consumes. It carries the fields `implement.js` names for a TDD result (`testFiles`, `redProof`, `coverage`, `notes`, `verdicts`, `anyFail`, `panelComplete`, `failedRoles`), the per-criterion coverage for all 29 Acceptance items, the actual outcomes, the evidence paths, the review verdicts, and the work assigned to IMPLEMENT.

Every path below is relative to the repository root and was checked to exist in the working tree.

## STOP CONDITION — read this before starting IMPLEMENT

**No coverage entry in this result is red. Every delivered acceptance test is green right now.** That is not an error in this document and it is not a concealed failure; it is what the contract approved for this round, and it collides head-on with the sentence the IMPLEMENT worker is handed about this file.

The two sides, verbatim:

`.agents/workflows/implement.js:1333`:

> `TDD RESULT — a file path; open and read it yourself: ${tddResultPath}\nEvery coverage entry must be red, and the exact no-test marker is valid only on an empty result with the contract evidence that authorizes no tests. STOP and escalate if it does not hold; do not start work on a prior stage's false result.`

The same requirement in executable form, `.agents/workflows/implement.js:1091-1097`, inside the `tdd-author` result contract:

> ```
>             // The no-test sentinel is valid ONLY on the empty result, and a real test result proves RED on every
>             // covered acceptance item.
>             whenArrayNonEmpty: [{
>               field: 'testFiles',
>               nonEmptyArrayFields: ['coverage'],
>               forbiddenFields: { redProof: NO_TEST_PROOF },
>               itemsAllTrue: [{ field: 'coverage', itemField: 'red' }],
>             }],
> ```

`contract.md:116`:

> `- Regression tests of helpers already implemented during RULE-PHASE may pass immediately. Prove their assertions exercise the required behavior.`

`contract.md:115` and `contract.md:117-118` set the surrounding terms of that approval:

> `- Direct dispatch replaces the file's requirement to use the TDD workflow script for this round only.`
> `- Tests for behavior still awaiting IMPLEMENT must expose that missing behavior. Never manufacture RED by breaking working code or weakening expectations.`
> `- This is a run-specific exception to the conflicting workflow and test-rail instructions. It changes neither the relocation scope nor its acceptance requirements.`

Nothing in this document resolves that conflict, and no workflow script was changed. The `red` value on every coverage row is the true value. IMPLEMENT must escalate this to Tim rather than start, unless he rules on the conflict first.

The reason every row is green rather than red: the analyzer rules and the shared helpers these tests exercise were built and accepted in RULE-PHASE, before TDD began. They are present at the base commit `a12e58b305c5315bc175332066e90a2d693b1769` with no analyzer source change in the working tree. Making them red would mean breaking working analyzer code, which `contract.md:117` and `.claude/skills/rails-test-code/SKILL.md` rail 8 both forbid. The substitute for RED is mutation: each mutation captured in `.dev/inprocess/74-pre-work-move-system-services/tdd-mutation-proofs.txt`, `.dev/inprocess/74-pre-work-move-system-services/tdd-acceptance25-mutation-proofs.txt` and `.dev/inprocess/74-pre-work-move-system-services/dry-fix-mutation-proofs.txt` breaks one analyzer or helper behavior and names the exact cases that go red.

## The production RED that IMPLEMENT does clear — a build failure, not a test failure

Keep this separate from the paragraph above. It is the RULE-PHASE production impact, recorded in `.dev/inprocess/74-pre-work-move-system-services/rule-phase-production-impact.md`, and it is real, current, and IMPLEMENT's to clear.

Without the contract's approved AG0015 warning override, the production build FAILS. `.dev/inprocess/74-pre-work-move-system-services/architecture-build-without-override.txt`:

```
/Users/timothystockstill/code/macos/4thWAIV/agent-guard/src/AgentGuard.Boundaries/SystemServices.cs(88,30): error AG0015: Clock access 'TimeProvider.System' bypasses the injected clock; read time off ISystemServices.Clock — a raw wall-clock read is banned everywhere and a direct TimeProvider acquisition only at the composition point [/Users/timothystockstill/code/macos/4thWAIV/agent-guard/src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj]
Build FAILED.
    1 Error(s)
EXIT=1
```

With the override — the only form in which any TDD or ARCHITECTURE intermediate build was run — the same diagnostic stands as a warning and the build succeeds. `.dev/inprocess/74-pre-work-move-system-services/tdd-acceptance25-intermediate-build.txt`, command `dotnet build -p:WarningsNotAsErrors=AG0015`:

```
Build succeeded.
/Users/timothystockstill/code/macos/4thWAIV/agent-guard/src/AgentGuard.Boundaries/SystemServices.cs(88,30): warning AG0015: Clock access 'TimeProvider.System' bypasses the injected clock; read time off ISystemServices.Clock — a raw wall-clock read is banned everywhere and a direct TimeProvider acquisition only at the composition point
    1 Warning(s)
    0 Error(s)
EXIT=0
```

That single diagnostic is the whole production impact. It clears when IMPLEMENT moves `src/AgentGuard.Boundaries/SystemServices.cs` to `src/AgentGuard.Engine/SystemServices.cs` in namespace `AgentGuard.Engine`, because `CompositionPoint`'s clock construction-site identity was retargeted to `AgentGuard.Engine` in RULE-PHASE while the file stayed in Boundaries. No suppression, `NoWarn`, severity change or production edit was used to hide it. The override applies to intermediate builds only; `contract.md:110` requires final verification to run without it.

## Two other things the next stage must not trip on

**The final delivered tree carries a DRY verdict only.** The three test-file edits of the DRY-fix round (`dry-fix-scope-and-digests.txt`, three files changed) were reviewed by DRY alone. The test-quality and Lie-catcher PASS verdicts in this result are against the Acceptance 25 delivery, which is the same test code minus those three extractions. They are recorded below as verdicts on that delivery and must not be read as reviews of the DRY-fix round.

**Acceptance 23's single-owner fact holds, but not through the review the criterion names.** `contract.md:277` says the two lenses having exactly one owner is "established by reading every registration in `analyzers/AgentGuard.Analyzers` during the existing DRY review". No DRY review in this folder records that reading. The RULE-PHASE SOLID reviewer does, in `rule-phase-fix4-output.json` (`result.verdicts[0].refutationAttempts[5]`) and `rule-phase-fix5-output.json` (`result.verdicts[0].refutationAttempts[3]`). The fact itself is live and checkable: `grep -rn "WrittenNameScanner\.\|DeclaredTypeScanner\." analyzers/AgentGuard.Analyzers/*.cs` returns consumers only — `ContractConcreteTypeMustNotBeReferencedAnalyzer.cs:52`, `EngineInternalsOneDoorAnalyzer.cs:88-89`, `OneDoorRule.cs:123,128` — and no second implementation.

---

## Stage result fields

### testFiles

The ten files this TDD work delivered, across all four rounds. Six are uncommitted modifications on top of `a12e58b`; four were committed at `a12e58b` by the original round and are unchanged since.

```
analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs
analyzers/AgentGuard.Analyzers.Tests/DeclaredTypeScannerTests.cs
analyzers/AgentGuard.Analyzers.Tests/EngineToBoundariesOneDoorAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/OneDoorIntoCrossPlatformAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/OneDoorIntoPerOsAnalyzerTests.cs
analyzers/AgentGuard.Analyzers.Tests/SymbolResolutionTests.cs
analyzers/AgentGuard.Analyzers.Tests/TypeTreeTests.cs
analyzers/AgentGuard.Analyzers.Tests/WrittenNameScannerTests.cs
analyzers/AgentGuard.Analyzers.Tests/GuardedConstructionAnalyzerTests.cs
```

`analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs` and `analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunnerTests.cs` are unchanged, as `contract.md:249` and `contract.md:272` require. Proof: `.dev/inprocess/74-pre-work-move-system-services/tdd-acceptance25-scope-and-digests.txt`, `git diff --stat` over both paths, no output, EXIT=0.

### redProof

Not the no-test marker. This result has test files, so the `NO_TEST_PROOF` sentinel (`implement.js:585`, value `Not applicable — the approved contract authorizes no test files.`) does not apply and is not used.

**Every delivered acceptance test is GREEN. No test is red.** The full analyzer suite is 619 passed / 0 failed / 0 skipped, EXIT=0, over the delivered tree. Source: `.dev/inprocess/74-pre-work-move-system-services/dry-fix-full-analyzer-tests.txt` and `.dev/inprocess/74-pre-work-move-system-services/dry-fix-commands-and-exits.txt`.

```
$ dotnet test analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj
Passed!  - Failed:     0, Passed:   619, Skipped:     0, Total:   619, Duration: 6 s - AgentGuard.Analyzers.Tests.dll (net10.0)
EXIT=0
```

The suite grew 600 → 616 → 619 across the rounds, each step captured:

| capture | suite | source |
| --- | --- | --- |
| before the original TDD round | 600 passed / 600 total, EXIT=0 | `tdd-baseline-analyzer-tests.txt` |
| original round, delivered | 616 passed / 616 total, EXIT=0 | `tdd-final-analyzer-tests.txt` |
| L3 correction, after | 616 passed / 616 total, EXIT=0 | `tdd-correction-analyzer-tests.txt` |
| before the Acceptance 25 round | 616 passed / 616 total, EXIT=0 | `tdd-acceptance25-baseline-analyzer-tests.txt` |
| Acceptance 25 round, delivered | 619 passed / 619 total, EXIT=0 | `tdd-acceptance25-full-analyzer-tests.txt` |
| DRY-fix round, delivered (current) | 619 passed / 619 total, EXIT=0 | `dry-fix-full-analyzer-tests.txt` |

The DRY-fix round changed no test outcome: the 619 case names and their 619 `Passed` verdicts are byte-identical before and after the three extractions, `diff` EXIT=0, both normalised lists hashing to `5ab7e68feb41c3110a7d25466c02ac150017502f6095180b8625bd3263f85747` (`dry-fix-full-analyzer-tests.txt`).

The one red in the tree is the production AG0015 build error described above, not a test.

### coverage

All 29 Acceptance items from `contract.md:253-283`. `red` is the true value for every row and every row is `false` — see the stop condition at the top of this file.

`kind` is one of: `unit` (a direct test of a helper through the real production type), `analyzer-behavioral` (a fixture compiled and run through the real analyzer with `AnalyzerRunner`), `preserved` (an existing test whose unchanged pass is itself the criterion), or `implement-verification` (no test; the criterion is a check IMPLEMENT or final verification runs).

There are no pointed-integration tests in this round. Every criterion is about analyzer and helper behavior over in-memory Roslyn compilations, and the per-OS criteria (16, 17) are MSBuild project-selection checks assigned to IMPLEMENT.

| # | acceptanceItem (contract.md line) | tests | kind | red | outcome |
| --- | --- | --- | --- | --- | --- |
| 1 | `make build` and `make test` pass before and after (`:255`) | `make build`; `make test` | implement-verification | false | BEFORE half done and green: `contract-opening-bracket.txt`, `BUILD_EXIT=0`, `TEST_EXIT=0`, 438 + 56 + 148 + 11 passed. AFTER half is IMPLEMENT's, and cannot pass until the AG0015 production error clears. |
| 2 | AG0040 accepts the four factories from `SystemServices.Create()` in Engine (`:256`) | `EngineToBoundariesOneDoorAnalyzerTests.PermittedFactory_FromContainerFactory_IsNotReported` (four theory rows: `EnvironmentAdapter.Create()`, `ConsoleAdapter.Create()`, `Ed25519SignatureService.Create()`, `BuildInfoReader.Create()`) | analyzer-behavioral | false | All four rows pass. Named individually in `tdd-acceptance25-affected-classes.txt`. |
| 3 | AG0040 reports the five wrong-caller and wrong-identity cases (`:257`) | `EngineToBoundariesOneDoorAnalyzerTests.PermittedFactory_FromAnotherContainerMethod_IsReported`; `…PermittedFactory_FromAnotherEngineClass_IsReported`; `…FifthFactoryReturningAServiceInterface_FromContainerFactory_IsReported`; `…SameNamedFactoryInAnotherNamespace_IsReported`; `…SameNamedFactoryInAnotherAssembly_IsReported` | analyzer-behavioral | false | All five pass. |
| 4 | AG0040 reports a non-factory Boundaries member from `Create()`, and reports nothing outside Engine (`:258`) | `EngineToBoundariesOneDoorAnalyzerTests.NonFactoryMemberOnAPermittedType_FromContainerFactory_IsReported`; `…AnyBoundariesCall_FromAnotherAssembly_IsNotReported` | analyzer-behavioral | false | Both pass. |
| 5 | AG0023 and AG0029 door accepted from `Create()`, reported from the wrong method, the wrong class and a same-named decoy; each message names every failed condition (`:259`) | In both `OneDoorIntoCrossPlatformAnalyzerTests` and `OneDoorIntoPerOsAnalyzerTests`: `DoorCall_FromContainerFactory_IsNotReported`; `DoorCall_FromAnotherContainerMethod_IsReported`; `DoorCall_FromAnotherEngineClass_IsReported`; `SameNamedDoorInAnotherNamespace_FromContainerFactory_IsReported`; `SameNamedDoorInAnotherAssembly_FromContainerFactory_IsReported`; `NonDoorCall_FromContainerFactory_IsReported`; plus `OneDoorIntoPerOsAnalyzerTests.SameNamedDoorTypeReferenceInAnotherAssembly_InsideContainerFactory_IsReported` | analyzer-behavioral | false | All pass. The paired message check runs through the one owner `SharedAnalyzerSources.AssertNamesExactly` (`SharedAnalyzerSources.cs:1030`), which asserts the named condition present and its pair absent, so no test asserts that a door call is not the door. |
| 6 | The five Boundaries-gate tests still pass with their current expectations (`:260`) | `OneDoorIntoCrossPlatformAnalyzerTests.CallIntoAdapterFactory_FromBoundaries_IsNotReported`; `…CallIntoOtherCrossPlatformType_FromBoundaries_IsReported`; `OneDoorIntoPerOsAnalyzerTests.CallOtherPerOsMember_FromBoundaries_IsReported`; `…CallPlatformServicesCreate_FromBoundaries_IsNotReported`; `…CallOldPlatformDoor_FromBoundaries_IsReported` | preserved | false | All five pass with their current expectations. Three of them (`CallIntoOtherCrossPlatformType_FromBoundaries_IsReported`, `CallOtherPerOsMember_FromBoundaries_IsReported`, `CallOldPlatformDoor_FromBoundaries_IsReported`) assert `Assert.Single(diagnostics).AssertReported(RuleId)` and make no message assertion, deliberately left as they were; the reasoning is recorded in `rule-phase-fix5-output.json`, `result.notes`, and this criterion is the one that pins them to their current expectations. |
| 7 | The two named tests keep their input byte for byte, are renamed, and assert the diagnostic; a new positive beside each (`:261`) | `OneDoorIntoCrossPlatformAnalyzerTests.CallIntoOtherCrossPlatformType_FromEngine_IsReported` (was `CallIntoOtherCrossPlatformType_FromNonBoundariesAssembly_IsNotReported`, `c1c2a18:analyzers/AgentGuard.Analyzers.Tests/BoundariesToCrossPlatformOneDoorAnalyzerTests.cs:68`); `OneDoorIntoPerOsAnalyzerTests.CallOtherPerOsMember_FromEngine_IsReported` (was `CallOtherPerOsMember_FromNonBoundariesAssembly_IsNotReported`, `c1c2a18:analyzers/AgentGuard.Analyzers.Tests/BoundariesToPerOsOneDoorAnalyzerTests.cs:68`); paired positives `OneDoorIntoCrossPlatformAnalyzerTests.DoorCall_FromContainerFactory_IsNotReported` and `OneDoorIntoPerOsAnalyzerTests.DoorCall_FromContainerFactory_IsNotReported` | analyzer-behavioral | false | All four pass. These are the only two reversed assertions in the delivery; `contract.md:251` permits no others. |
| 8 | AG0041 accepts `SystemServices.Create()` and the public Engine surface in both gated compilations, over a genuinely accessible fake Engine (`:262`) | `EngineInternalsOneDoorAnalyzerTests.ApprovedContainerFactoryCall_IsNotReported` (2 rows); `…FullyQualifiedApprovedContainerFactoryCall_IsNotReported` (2 rows); `…Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` over `PermittedPublicReaches` (12 rows: six positions × two consumers) | analyzer-behavioral | false | All pass. The fake Engine source carries `[assembly: InternalsVisibleTo("guard")]` and `[assembly: InternalsVisibleTo("AgentGuard.TestHelpers")]` in `SharedAnalyzerSources.cs`, so the accepted call turns on the analyzer and not on an inaccessibility error. |
| 9 | AG0041 reports every written-name and carried-type position in both compilations, and nothing in `AgentGuard.Tests` (`:263`) | `EngineInternalsOneDoorAnalyzerTests.Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` over `ProhibitedReaches` (14 positions × two consumers); `…EngineInternalInBaseList_FromGatedConsumer_IsReported`; `…EngineInternalThroughUsingAlias_FromGatedConsumer_IsReported`; `…EveryProhibitedReach_FromTestsAssembly_IsNotReported` | analyzer-behavioral | false | All pass. `ProhibitedReaches` covers `typeof`, `nameof`, cast, `is` pattern, generic type argument, constructed generic, member access, field, property, method return, parameter, local, inferred `var` local, and a type argument nested inside a generic array. |
| 10 | The `SystemServices` exception is no wider than the approved call (`:264`) | `EngineInternalsOneDoorAnalyzerTests.Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics` over `ProhibitedContainerReaches` (12 positions × two consumers); `…ContainerInBaseList_FromGatedConsumer_IsReported`; `…ContainerThroughUsingAlias_FromGatedConsumer_IsReported`; `…SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported` | analyzer-behavioral | false | All pass. `ProhibitedContainerReaches` includes `SystemServices.Other()` and the method group `SystemServices.Create`, so only the invocation receiver is accepted. |
| 11 | Every AG0041 test asserts by count and message; no `Assert.Single` on a dual-lens input (`:265`) | `EngineInternalsOneDoorAnalyzerTests.AssertReportsAsync` (the file's one assertion path) | analyzer-behavioral | false | Holds. `AssertReportsAsync` does `Assert.Equal(expectedSubjects.Length, diagnostics.Length)`, then `AssertReported(RuleId)` on each, then `SharedAnalyzerSources.AssertSameStrings` over the full expected messages. `grep -n "Assert.Single" analyzers/AgentGuard.Analyzers.Tests/EngineInternalsOneDoorAnalyzerTests.cs` returns nothing. |
| 12 | AG0040, AG0023 and AG0029 report their own door call written in a lambda and in a local function nested in `Create()` (`:266`) | `EngineToBoundariesOneDoorAnalyzerTests.PermittedFactory_FromLambdaInsideContainerFactory_IsReported`; `…PermittedFactory_FromLocalFunctionInsideContainerFactory_IsReported`; `OneDoorIntoCrossPlatformAnalyzerTests.DoorCall_FromLambdaInsideContainerFactory_IsReported`; `…DoorCall_FromLocalFunctionInsideContainerFactory_IsReported`; `OneDoorIntoPerOsAnalyzerTests.DoorCall_FromLambdaInsideContainerFactory_IsReported`; `…DoorCall_FromLocalFunctionInsideContainerFactory_IsReported` | analyzer-behavioral | false | All six pass. |
| 13 | AG0006 unchanged after the `DeclaredTypeScanner` extraction (`:267`) | `analyzers/AgentGuard.Analyzers.Tests/ContractConcreteTypeMustNotBeReferencedAnalyzerTests.cs`, every test and assertion unmodified | preserved | false | The file is untouched by every round — it does not appear in `git status --short` and is not in `tdd-digests.txt` or `dry-fix-scope-and-digests.txt`. Its tests pass inside the 619. |
| 14 | AG0015 accepts a direct `TimeProvider.System` read inside `SystemServices` in Engine and reports it elsewhere in Engine (`:268`) | `TimeMustUseTimeProviderAnalyzerTests.TimeProviderSystem_InSystemServicesCreate_IsNotReported`; `…TimeProviderSystem_OutsideComposition_IsReported`; `…TimeProviderSystem_InSystemServicesLeftBehindInBoundaries_IsReported`; `…TimeProviderSystem_InSameNamedContainerInAnotherAssembly_IsReported`; `…TimeProviderSystem_InSystemServicesBuilder_IsNotReported`; `…TimeProviderSystem_InProgramCaller_IsReported` | analyzer-behavioral | false | All pass; 12 cases in the class (`dry-fix-review-dry.md:151`). `TimeProviderSystem_InSystemServicesLeftBehindInBoundaries_IsReported` is the test that pins the production diagnostic IMPLEMENT clears. |
| 15 | The wiring tests and four boundary adapter test classes pass unchanged in behavior; `ISystemServices.cs` and the four adapters have no behavioral change, by `git diff` review (`:269`) | `tests/AgentGuard.Tests` wiring and adapter tests; `git diff` over `src/AgentGuard.Abstractions/Contracts/ISystemServices.cs` and the four adapters | implement-verification | false | Not yet performable. `contract.md:197-198` renames `BoundariesSystemServicesWiringTests.cs` and edits comments, and `contract.md:238` assigns that to IMPLEMENT. Nothing under `tests/` has changed in this round — `tdd-acceptance25-scope-and-digests.txt`, `git status --short -- src tests eng .github Makefile analyzers/AgentGuard.Analyzers`, no output, EXIT=0. |
| 16 | Per-rid `dotnet msbuild … -getItem:ProjectReference` selects exactly the matching per-OS project, for `osx-arm64`, `linux-x64`, `win-x64` (`:270`) | `dotnet msbuild src/AgentGuard.Engine/AgentGuard.Engine.csproj -getItem:ProjectReference -p:AgentGuardPlatformRid=<rid>` × 3 | implement-verification | false | Assigned to IMPLEMENT by `contract.md:238`: "IMPLEMENT: a separate worker moves `SystemServices.cs` to `src/AgentGuard.Engine/`, changes its namespace, and preserves its shape, service instances, and construction order. Apply the approved project references, platform-target import, internal-access grants, and caller changes." The `PlatformImplementation.targets` import this check reads moves in IMPLEMENT (`contract.md:180-181`). |
| 17 | `dotnet msbuild src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj -getItem:ProjectReference` returns `AgentGuard.Abstractions` and nothing else (`:271`) | `dotnet msbuild src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj -getItem:ProjectReference` | implement-verification | false | Assigned to IMPLEMENT by the same `contract.md:238` sentence; the Boundaries project references change there (`contract.md:180`). |
| 18 | Every analyzer test passes using the existing compiler-error validation; the runner and its tests unchanged; new fixtures compile under the default empty expectation (`:272`) | The full `analyzers/AgentGuard.Analyzers.Tests` suite | analyzer-behavioral | false | 619 passed / 0 failed / 0 skipped, EXIT=0 (`dry-fix-full-analyzer-tests.txt`). `AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs` unchanged (`tdd-acceptance25-scope-and-digests.txt`). No new expected-compiler-error exception was added; the two malformed-attribute exceptions that exist are the pre-existing ones named under item 26. |
| 19 | `git diff --check` passes; `git diff` and `git status --short` reviewed against every contract boundary; unavailable cross-OS execution reported explicitly (`:273`) | `git diff --check`; `git status --short` | analyzer-behavioral | false | `git diff --check` EXIT=0 in `tdd-acceptance25-scope-and-digests.txt`, `tdd-correction-diff-check.txt` and `dry-fix-scope-and-digests.txt`. Nothing under `src/`, `tests/`, `eng/`, `.github/`, `Makefile` or `analyzers/AgentGuard.Analyzers/` changed in this round. Cross-OS execution was not run and is not claimed; item 16 states the check explicitly. |
| 20 | AG0023 and AG0029 report a non-door guarded-type reference from anywhere in Engine, through both lenses (`:274`) | In both `OneDoorIntoCrossPlatformAnalyzerTests` and `OneDoorIntoPerOsAnalyzerTests`: `GuardedTypeReference_FromEngine_IsReported` (theory, one row per position); `GuardedTypeInBaseList_FromEngine_IsReported`; `GuardedTypeThroughUsingAlias_FromEngine_IsReported` | analyzer-behavioral | false | All pass. The theory rows named in `tdd-acceptance25-affected-classes.txt` cover `typeof`, `nameof`, cast, `is` pattern, generic argument, generic constraint, attribute argument, field, property, method return, parameter, local, inferred `var` local, and a nested type argument. |
| 21 | AG0023 accepts `CrossPlatformAdapters` written inside `SystemServices.Create()` including as a local's declared type and reports it elsewhere; AG0029 the same for `PlatformServices`; `IPlatformServices` never reported (`:275`) | `OneDoorIntoCrossPlatformAnalyzerTests.DoorLocalDeclaration_InsideContainerFactory_IsNotReported`; `…DoorLocalDeclaration_InAnotherEngineMethod_IsReported`; `OneDoorIntoPerOsAnalyzerTests.DoorTypeReference_InsideContainerFactory_IsNotReported`; `…DoorTypeReference_InAnotherEngineMethod_IsReported`; `AbstractionsTypeReference_FromEngine_IsNotReported` in both classes | analyzer-behavioral | false | All pass. `DoorLocalDeclaration_InsideContainerFactory_IsNotReported` uses the exact `CrossPlatformAdapters adapters = CrossPlatformAdapters.Create();` form `Create()` uses today. |
| 22 | The Boundaries gate is unchanged by the type-reference extension (`:276`) | `OneDoorIntoCrossPlatformAnalyzerTests.ReferenceToOtherCrossPlatformType_FromBoundaries_IsNotReported`; `OneDoorIntoPerOsAnalyzerTests.ReferenceToOtherPerOsType_FromBoundaries_IsNotReported` | analyzer-behavioral | false | Both pass. |
| 23 | The two lenses have exactly one owner, consumed by AG0041, AG0023 and AG0029; no second implementation (`:277`) | `rule-phase-fix4-output.json` `result.verdicts[0].refutationAttempts[5]`; `rule-phase-fix5-output.json` `result.verdicts[0].refutationAttempts[3]`; live `grep -rn "WrittenNameScanner\.\|DeclaredTypeScanner\." analyzers/AgentGuard.Analyzers/*.cs` | preserved | false | The fact holds. The establishing review is the RULE-PHASE SOLID reviewer, not a DRY review; see "Two other things the next stage must not trip on" above. |
| 24 | Documentation references to governed types and members are allowed while application-code references stay restricted; the fixture is asserted to contain a documentation-reference node (`:278`) | `EngineInternalsOneDoorAnalyzerTests.DocumentationReferenceToEngineInternal_IsNotReported` (2 rows); `OneDoorIntoCrossPlatformAnalyzerTests.DocumentationReferenceToGuardedType_FromEngine_IsNotReported`; `OneDoorIntoPerOsAnalyzerTests.DocumentationReferenceToGuardedType_FromEngine_IsNotReported` | analyzer-behavioral | false | All pass. Each calls `SharedAnalyzerSources.AssertHasDocumentationReference(source)` (`SharedAnalyzerSources.cs:927`) before asserting the empty result, so the test cannot pass by the fixture omitting documentation. The paired restriction on the same identifier in code is items 9, 10 and 20. |
| 25 | The privileged caller's full identity: wrong-namespace callers rejected by the access analyzers; direct tests of `CompositionPoint` reject a same-named method in the wrong assembly; non-Engine tests still prove the assembly gate (`:279`) | `EngineToBoundariesOneDoorAnalyzerTests.PermittedFactory_FromSameNamedContainerInAnotherNamespace_IsReported`; `…ContainerFactoryIdentity_IsTheStaticCreateInTheEngineAssembly`; `…ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly`; `…AnyBoundariesCall_FromAnotherAssembly_IsNotReported`; `OneDoorIntoCrossPlatformAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported`; `OneDoorIntoPerOsAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported`; `EngineInternalsOneDoorAnalyzerTests.SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported`; `…EveryProhibitedReach_FromTestsAssembly_IsNotReported`; `TimeMustUseTimeProviderAnalyzerTests.TimeProviderSystem_InSameNamedContainerInAnotherAssembly_IsReported` | unit + analyzer-behavioral | false | All pass. The three tests the Acceptance 25 round added are the two `…FromSameNamedContainerInAnotherNamespace_IsReported` cases and `PermittedFactory_FromSameNamedContainerInAnotherNamespace_IsReported`; the two `ContainerFactoryIdentity_…` cases are the direct `CompositionPoint` tests, driving the real predicate on a resolved method. Their discrimination is proved by mutation, not by passing: `tdd-acceptance25-mutation-proofs.txt`, M-A25-1 makes `CompositionPoint.IsContainerFactoryType` namespace-blind and the named cases go red. Multiple scanner reports are accommodated by asserting the full expected multiset (`SharedAnalyzerSources.AssertSameStrings`, `…AssertSpans`) rather than `Assert.Single`; no existing expectation was weakened (`tdd-acceptance25-review-dry.md:196`). |
| 26 | Generic guarded names and shared type resolution; the existing malformed-attribute tests keep their inputs, assertions and compiler-error expectations (`:280`) | `SymbolResolutionTests.Symbol_OnAWrittenTypeName_ResolvesThatType`; `…Symbol_OnANamespaceSegment_ResolvesTheNamespaceItself`; `…NamedType_OnANamespaceSegment_ResolvesNothing`; `…Symbol_OnAGenericName_ResolvesTheConstructedType`; `…Symbol_OnAMemberName_ResolvesThatMember`; `…NamedType_OnAWrittenTypeName_ResolvesThroughTheBoundTypeItself`; `…NamedType_OnAnAppliedAttribute_ResolvesTheAttributeTypeThroughItsConstructor`; `…NamedType_OnAnInvocation_StopsAtTheBoundMethodAndNeverReachesTheTypeInfoTier`; `DeclaredTypeScannerTests.TheGatedEntryPoint_ScansTheReturnTypeOfAnInvokedMember`; `…TheDeclarationEntryPoint_DoesNotScanTheReturnTypeOfAnInvokedMember`; `InteropOnlyInCrossPlatformLibrariesAnalyzerTests.MalformedSameNamedDllImportAttribute_WithRequiredArgOmitted_IsNotReported`; `NativeCallbackBodyMustBeGuardedAnalyzerTests.MalformedUserDefinedUnmanagedCallersOnly_WithRequiredArgOmitted_IsNotReported` | unit + preserved | false | All pass. The malformed-attribute recovery tier is proved by the two existing tests, reused unchanged, so no second malformed-fixture exception was introduced — stated in `SymbolResolutionTests.cs:19-22`. Both host files are untouched by every round. |
| 27 | Direct tests of `TypeTree.Any` exercise pointer targets using compiler-created type symbols and the existing compilation helper (`:281`) | `TypeTreeTests.APointerToTheSoughtType_ReachesItsTarget`; `…APointerToAnotherType_IsWalkedToItsTargetAndDoesNotMatch`; `…AnArrayOfPointers_ReachesTheTargetThroughItsElementType`; `…AGenericCarryingAPointerArray_ReachesTheTargetThroughItsTypeArgument` | unit | false | All four pass. They assert whether the intended leaf is reached, both matching and non-matching, so a non-null constructed pointer symbol is not the proof. No runner change; no new compiler-error exception. |
| 28 | A direct `SystemServices.Create()` call in an `AgentGuard.Tests` compilation reports AG0017, over a reachable `Create()` (`:282`) | `GuardedConstructionAnalyzerTests.CreateCall_FromTheMainTestAssembly_IsReported` (`GuardedConstructionAnalyzerTests.cs:393`) | analyzer-behavioral | false | Passes. The fixture declares `SystemServices` in namespace and assembly `AgentGuard.Engine` with a reachable `Create()`, so the assertion turns on the analyzer rather than on an inaccessibility error. |
| 29 | The documentation exclusion is a `CrefSyntax` ancestor check, true for a generic cref and an operator cref, false for code and for a preprocessor-directive name (`:283`) | `WrittenNameScannerTests.EveryNameInsideACref_IsADocumentationReference`; `…TheGenericAndOperatorCrefShapes_AreBothCovered`; `…TheSameNameWrittenInCode_IsNotADocumentationReference`; `…APreprocessorDirectiveName_IsNotADocumentationReference`; `…ACrefName_ResolvesToTheSameSymbolAsTheCodeName` | unit | false | All five pass. `TheGenericAndOperatorCrefShapes_AreBothCovered` asserts the fixture actually contains a name under a `TypeArgumentListSyntax` and one under a `CrefParameterListSyntax`; `APreprocessorDirectiveName_IsNotADocumentationReference` asserts `IsPartOfStructuredTrivia()` is true for `#if GUARDED` while the predicate is false, which is the rejected alternative the contract names. |

### notes

**Base and snapshot.** Base commit `a12e58b305c5315bc175332066e90a2d693b1769`, branch `appd-1-process`. The working tree carries the Acceptance 25 round and the DRY-fix round on top of it, uncommitted. Nothing was staged, committed, pushed or stashed. `git status --short` at the time of the DRY-fix capture is reproduced in `dry-fix-scope-and-digests.txt` and matches the tree.

Delivered-file digests, for the tree IMPLEMENT receives:

- Acceptance 25 round, five files: `tdd-acceptance25-scope-and-digests.txt`
- DRY-fix round, before → after for the three it changed and the four it did not: `dry-fix-scope-and-digests.txt`
- original round, all ten files: `tdd-digests.txt`
- L3 correction, all ten files: `tdd-correction-scope-and-digests.txt`

**Rounds, in order.** Four authoring rounds produced this delivery. Each is its own record and none of their verdicts transfers to another.

1. Original TDD round — wrote and edited the ten files listed under `testFiles`. Evidence: `tdd-baseline-analyzer-tests.txt`, `tdd-run1-analyzer-tests.txt`, `tdd-run2-analyzer-tests.txt`, `tdd-run3-after-dedup.txt`, `tdd-final-analyzer-tests.txt`, `tdd-digests.txt`, `tdd-intermediate-build.txt`, `tdd-mutation-proofs.txt`, `tdd-affected-classes-detailed.txt`, `tdd-diff-check-and-status.txt`.
2. L3 correction — answered the original DRY FAIL by routing three duplicated values to shared owners, changing five test files. Evidence: `tdd-correction-analyzer-tests.txt`, `tdd-correction-per-case.txt`, `tdd-correction-diff-check.txt`, `tdd-correction-scope-and-digests.txt`.
3. Acceptance 25 round — added the caller-identity coverage, changing five test files. Evidence: `tdd-acceptance25-baseline-analyzer-tests.txt`, `tdd-acceptance25-full-analyzer-tests.txt`, `tdd-acceptance25-affected-classes.txt`, `tdd-acceptance25-intermediate-build.txt`, `tdd-acceptance25-mutation-proofs.txt`, `tdd-acceptance25-scope-and-digests.txt`.
4. DRY-fix round — three ordered extractions answering the Acceptance 25 DRY FAIL, changing three test files. Evidence: `dry-fix-commands-and-exits.txt`, `dry-fix-full-analyzer-tests.txt`, `dry-fix-fixture-text-identity.txt`, `dry-fix-mutation-proofs.txt`, `dry-fix-scope-and-digests.txt`.

Two further review rounds (`tdd-correction-review-*`, `tdd-correction2-review-*`) reviewed an earlier narrative report document, not test code. Their verdicts appear below under that heading and are not verdicts on any test.

**Authority held.** This round wrote no production code, no analyzer, no project file, no contract text and no workflow script. `tdd-acceptance25-scope-and-digests.txt`: `git status --short -- src tests eng .github Makefile analyzers/AgentGuard.Analyzers` returns no output, EXIT=0. `dry-fix-scope-and-digests.txt` records the same for its round.

**Direct dispatch.** These rounds ran without `.agents/workflows/tdd.js`, under `contract.md:115`: "Direct dispatch replaces the file's requirement to use the TDD workflow script for this round only." That is why this result is a written file rather than a script return value, and why its fields are spelled out here rather than validated by `tdd.js`.

**Missing input.** No authorization record exists in this work folder for the correction, second correction, Acceptance 25 or DRY-fix rounds. `tdd-worker-instructions.md`, "Dispatch and authority", says "A failed result is a failure, not authority for another fix round", and `conversation.md`, "Current checkpoint", says "Retain stage review checkpoints and the separate authorization for fix rounds." The Acceptance 25 Lie-catcher raised this (`tdd-acceptance25-review-lie-catcher.md`) and it is still unwritten. Recorded as missing, not invented.

**Missing input.** No structured author result was retained for the Acceptance 25 round or the DRY-fix round; both left execution captures only. Recorded as missing.

### verdicts

Every verdict below is attributed to the review record that produced it and to the delivery it was passed on. An earlier verdict is a historical verdict on that earlier delivery, never a review of a later change.

**On the original TDD round's ten delivered files** (`tdd-digests.txt` snapshot, HEAD `c1c2a18`):

| lens | verdict | record |
| --- | --- | --- |
| test-quality | PASS | `tdd-review-test-quality.md` |
| lie-catcher | FAIL — six findings, all in the author's claims about its own sources, none in the tests | `tdd-review-lie-catcher.md` |
| dry | FAIL — three duplications in the round's changed files | `tdd-review-dry.md` |

**On the L3 correction to the narrative report** (five test files also changed, HEAD `c1c2a18`):

| lens | verdict | record |
| --- | --- | --- |
| lie-catcher | FAIL — six findings, all in the document's account of its own evidence, none in the tests | `tdd-correction-review-lie-catcher.md` |
| dry | PASS — the three duplications are gone and the correction introduced none | `tdd-correction-review-dry.md` |

**On the second correction to the narrative report** (report text only, HEAD `c1c2a18`):

| lens | verdict | record |
| --- | --- | --- |
| lie-catcher | FAIL — four findings, three in one paragraph of the duplication section, none in the tests | `tdd-correction2-review-lie-catcher.md` |
| dry | FAIL — two findings against the report's own provenance statements, none in the tests | `tdd-correction2-review-dry.md` |

Both of those rounds reviewed a narrative report document that has since been deleted. Their findings are against a document, not against the test code, and none of them names a test defect.

**On the Acceptance 25 delivery** (five test files, base `a12e58b`):

| lens | verdict | record |
| --- | --- | --- |
| test-quality | PASS — no `rails-test-code` violation confirmed | `tdd-acceptance25-review-test-quality.md` |
| lie-catcher | PASS on the delivered tests | `tdd-acceptance25-review-lie-catcher.md` |
| dry | FAIL — three duplications confirmed against the live files | `tdd-acceptance25-review-dry.md` |

**On the DRY-fix delivery — the tree IMPLEMENT receives** (three test files, base `a12e58b`):

| lens | verdict | record |
| --- | --- | --- |
| dry | PASS — all three ordered corrections delivered, wired and load-bearing, no new duplication | `dry-fix-review-dry.md` |
| test-quality | not run on this delivery | — |
| lie-catcher | not run on this delivery | — |

### anyFail

**true.** Five FAIL verdicts stand in the record: two against the original round (lie-catcher, dry), two against the deleted narrative report (correction2 lie-catcher, correction2 dry) plus one more against the first correction (lie-catcher), and one against the Acceptance 25 delivery (dry).

Against the **delivered test code as it now stands**, no FAIL is unanswered: the original DRY FAIL was answered by the L3 correction and cleared by `tdd-correction-review-dry.md` PASS; the Acceptance 25 DRY FAIL was answered by the DRY-fix round and cleared by `dry-fix-review-dry.md` PASS; every Lie-catcher FAIL is against a report document, and each of those reviews states in its own verdict that none of its findings is in the tests.

### panelComplete

**true**, in the sense `implement.js:534` defines — `panelComplete: failedRoles.length === 0` — every reviewer dispatched in every round returned a valid result.

It is **not** true in the sense `implement.js:1152-1157` checks for a `tdd-stage` result, which requires a valid verdict from each of `test-quality`, `dry` and `lie-catcher`. The DRY-fix delivery carries a DRY verdict only. See the second heading under "Two other things the next stage must not trip on".

### failedRoles

```
[]
```

No dispatched role failed to return a valid result, in any round.

---

## Work assigned to IMPLEMENT

Everything below is IMPLEMENT's, per `contract.md:238` and the Surfaces list at `contract.md:179-199`. None of it was done in TDD and none of it is claimed here.

1. Move `src/AgentGuard.Boundaries/SystemServices.cs` to `src/AgentGuard.Engine/SystemServices.cs`, change its namespace to `AgentGuard.Engine`, and preserve its shape, service instances and construction order. This is what clears the AG0015 production error.
2. `src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj` — drop the `PlatformImplementation.targets` import, replace the two internals grants with `AgentGuard.Engine`.
3. `src/AgentGuard.Engine/AgentGuard.Engine.csproj` — add the Boundaries project reference, add the `PlatformImplementation.targets` import, add internals grants to `guard` and `AgentGuard.TestHelpers`.
4. `src/AgentGuard.Cli/AgentGuard.Cli.csproj` — drop the direct Boundaries project reference.
5. `src/AgentGuard.CrossPlatform/AgentGuard.CrossPlatform.csproj` and the three `src/AgentGuard.CrossPlatform.{MacOS,Linux,Windows}/*.csproj` — retarget the `AgentGuard.Boundaries` internals grant to `AgentGuard.Engine`.
6. `tests/AgentGuard.TestHelpers/AgentGuard.TestHelpers.csproj` — retarget the project reference from Boundaries to Engine.
7. `src/AgentGuard.Cli/Program.cs` and `tests/AgentGuard.TestHelpers/SystemServicesBuilder.cs` — `using AgentGuard.Boundaries` becomes `using AgentGuard.Engine`.
8. `tests/AgentGuard.Tests/BoundariesSystemServicesWiringTests.cs` — rename to `EngineSystemServicesWiringTests.cs` with the class renamed to match, plus the comments naming the assembly the container lives in.
9. `tests/AgentGuard.Tests/BoundariesConsoleAdapterTests.cs`, `BoundariesEnvironmentAdapterTests.cs`, `BoundariesBuildInfoTests.cs`, `BoundariesEd25519SignatureServiceTests.cs` — comments only; the names keep the `Boundaries` prefix because the four adapters stay in `AgentGuard.Boundaries`.
10. Comments on changed ownership and access in the listed source and project files.
11. Acceptance items 1, 15, 16, 17 — run and record them. Acceptance 19's cross-OS note: if an OS leg cannot be executed, say so explicitly rather than claiming it ran.

IMPLEMENT's authority over this delivery, from `contract.md:238` and `contract.md:251`: it does not edit the analyzer rules or the acceptance tests, and it must not weaken or delete a test to reach green.

## Every file this result names

Checked to exist in the working tree. Contract and evidence files live under `.dev/inprocess/74-pre-work-move-system-services/`.

Three paths this result names are deliberately not working-tree files, and are listed here so nothing reads as a broken citation:

- `c1c2a18:analyzers/AgentGuard.Analyzers.Tests/BoundariesToCrossPlatformOneDoorAnalyzerTests.cs` and `c1c2a18:analyzers/AgentGuard.Analyzers.Tests/BoundariesToPerOsOneDoorAnalyzerTests.cs`, cited under Acceptance item 7. Both are git-revision references at commit `c1c2a18` and both objects resolve there (`git cat-file -e`). RULE-PHASE renamed the files, so neither exists in the working tree.
- `src/AgentGuard.Engine/SystemServices.cs`, cited under "Work assigned to IMPLEMENT". It does not exist yet; it is the destination of the move.

Contract and stage records: `contract.md`, `conversation.md`, `tdd-worker-instructions.md`, `remaining-work.md`, `architecture-result.md`, `rule-phase-production-impact.md`, `rule-phase-fix4-output.json`, `rule-phase-fix5-output.json`, `contract-opening-bracket.txt`, `architecture-build-with-override.txt`, `architecture-build-without-override.txt`.

Original round: `tdd-baseline-analyzer-tests.txt`, `tdd-run1-analyzer-tests.txt`, `tdd-run2-analyzer-tests.txt`, `tdd-run3-after-dedup.txt`, `tdd-final-analyzer-tests.txt`, `tdd-digests.txt`, `tdd-intermediate-build.txt`, `tdd-mutation-proofs.txt`, `tdd-affected-classes-detailed.txt`, `tdd-diff-check-and-status.txt`, `tdd-review-test-quality.md`, `tdd-review-lie-catcher.md`, `tdd-review-dry.md`.

Corrections: `tdd-correction-analyzer-tests.txt`, `tdd-correction-per-case.txt`, `tdd-correction-diff-check.txt`, `tdd-correction-scope-and-digests.txt`, `tdd-correction-review-lie-catcher.md`, `tdd-correction-review-dry.md`, `tdd-correction2-review-lie-catcher.md`, `tdd-correction2-review-dry.md`.

Acceptance 25 round: `tdd-acceptance25-baseline-analyzer-tests.txt`, `tdd-acceptance25-full-analyzer-tests.txt`, `tdd-acceptance25-affected-classes.txt`, `tdd-acceptance25-intermediate-build.txt`, `tdd-acceptance25-mutation-proofs.txt`, `tdd-acceptance25-scope-and-digests.txt`, `tdd-acceptance25-review-test-quality.md`, `tdd-acceptance25-review-lie-catcher.md`, `tdd-acceptance25-review-dry.md`.

DRY-fix round: `dry-fix-commands-and-exits.txt`, `dry-fix-full-analyzer-tests.txt`, `dry-fix-fixture-text-identity.txt`, `dry-fix-mutation-proofs.txt`, `dry-fix-scope-and-digests.txt`, `dry-fix-review-dry.md`.

Test sources, under `analyzers/AgentGuard.Analyzers.Tests/`: `SharedAnalyzerSources.cs`, `DeclaredTypeScannerTests.cs`, `EngineToBoundariesOneDoorAnalyzerTests.cs`, `EngineInternalsOneDoorAnalyzerTests.cs`, `OneDoorIntoCrossPlatformAnalyzerTests.cs`, `OneDoorIntoPerOsAnalyzerTests.cs`, `SymbolResolutionTests.cs`, `TypeTreeTests.cs`, `WrittenNameScannerTests.cs`, `GuardedConstructionAnalyzerTests.cs`, `TimeMustUseTimeProviderAnalyzerTests.cs`, `ContractConcreteTypeMustNotBeReferencedAnalyzerTests.cs`, `InteropOnlyInCrossPlatformLibrariesAnalyzerTests.cs`, `NativeCallbackBodyMustBeGuardedAnalyzerTests.cs`, `AnalyzerRunner.cs`, `AnalyzerRunnerTests.cs`.

Analyzer sources: `analyzers/AgentGuard.Analyzers/WrittenNameScanner.cs`, `analyzers/AgentGuard.Analyzers/DeclaredTypeScanner.cs`, `analyzers/AgentGuard.Analyzers/CompositionPoint.cs`, `analyzers/AgentGuard.Analyzers/OneDoorRule.cs`, `analyzers/AgentGuard.Analyzers/EngineInternalsOneDoorAnalyzer.cs`, `analyzers/AgentGuard.Analyzers/ContractConcreteTypeMustNotBeReferencedAnalyzer.cs`.

Production source: `src/AgentGuard.Boundaries/SystemServices.cs`.

Workflow: `.agents/workflows/implement.js`.
