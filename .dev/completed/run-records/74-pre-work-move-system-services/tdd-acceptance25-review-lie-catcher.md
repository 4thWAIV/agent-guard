# Lie-catcher review — the Acceptance 25 caller-identity tests

## Verdict

**PASS on the delivered tests.** No `rails-decisions` Violation is confirmed and no `rails-real-work` Violation is confirmed in the test code. Every number, exit code, digest and mutation result in the six `tdd-acceptance25-*` evidence files reproduces on my own runs, in my own isolated copy, against the files as they stand in the shared checkout. The claimed no-RED exception is backed by an approved contract decision, and I could not find an analyzer defect the tests conceal.

Two items below are about the round's record, not its tests, and they need the orchestrator or Tim.

## The authorization for this fix round is not written down anywhere in the work folder

`conversation.md`, "Current checkpoint": "Retain stage review checkpoints and the separate authorization for fix rounds."

`tdd-worker-instructions.md`, "Dispatch and authority": "This authoring and review round ends with a report for Tim. A failed result is a failure, not authority for another fix round."

This is at least the fourth pass over the same TDD deliverable — the original authoring round, the correction, the second correction, and now this Acceptance 25 round. I searched every file in `.dev/inprocess/74-pre-work-move-system-services/` for a record of Tim authorizing any of them and found none. I am not asserting that he did not authorize it; I am asserting that the record both files above require does not exist, so nothing in the folder distinguishes an authorized round from an unauthorized one. Whoever closes this round should write the authorization down, or say plainly that it was not obtained.

## The round ran on a replacement brief, and no author result was retained

`tdd-worker-instructions.md`, "Dispatch and authority": "Claude forwards this file unchanged to one fresh TDD author and to the independent test-quality, DRY and Lie-catcher reviewers. ... This file is the complete supplemental task; Claude adds no replacement brief, inferred requirements or scope exceptions."

This round's author received a two-item brief instead of that file. The brief's content is faithful to Acceptance 25 — I checked it clause by clause and it narrows nothing — but it drops the file's "Author return" section, and the concrete effect is that this round has six execution captures and no author result, no per-case accounting and no acceptance mapping. The contract's approved direct-dispatch decision covers dispatching without the workflow script; it does not say the supplemental task file is replaced.

Two consequences a later reader will trip on:

- `tdd-result.md`, the previous round's 440-line author result committed at `a12e58b`, is deleted from the working tree. Six retained reviews cite it 68 times by line number: `tdd-correction2-review-lie-catcher.md` (25), `tdd-correction-review-lie-catcher.md` (20), `tdd-correction2-review-dry.md` (12), `tdd-review-lie-catcher.md` (6), `tdd-review-dry.md` (3), `tdd-review-test-quality.md` (2). Every one of those now resolves to nothing in the tree. The content is recoverable with `git show a12e58b:.dev/inprocess/74-pre-work-move-system-services/tdd-result.md`. I left the deletion in place as instructed.
- The deleted file is where Acceptance 25 was previously claimed satisfied, and the claim was wrong. `tdd-result.md:303` maps Acceptance 25 to "EXISTING for the analyzer half — `SameNamedFactoryInAnotherNamespace_IsReported`, `SameNamedDoorInAnotherNamespace_FromContainerFactory_IsReported` (both classes), `SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported`, `AnyBoundariesCall_FromAnotherAssembly_IsNotReported`, `EveryProhibitedReach_FromTestsAssembly_IsNotReported`." Every one of those is a wrong-namespace or wrong-assembly **callee**; none of them varies the **caller's** namespace, which is what Acceptance 25's first clause requires. This round is the correction of that gap. With `tdd-result.md` deleted, the record of the false claim is gone from the tree too, and the only place the correction is written down is the test code.

## Findings in the tests

None. Below is what I attacked and could not break.

---

OPTIONAL DETAILS (do not need to read)

## Reviewed snapshot

`HEAD` = `a12e58b305c5315bc175332066e90a2d693b1769`, branch `appd-1-process`. Nothing staged, no stash, no new commit. `git status --short -- src tests eng .github Makefile analyzers/AgentGuard.Analyzers` is empty: no production code, no analyzer source, no project or workflow file changed. The working tree carries the five changed test files, the deletion of `tdd-result.md`, and the six untracked `tdd-acceptance25-*` captures — nothing else. `git diff --check` exits 0.

The five delivered files' SHA-256 digests match `tdd-acceptance25-scope-and-digests.txt` exactly, at the start and at the end of this review:

```
db6c6995a18d64a6fc54e759483e9b806c534356e07572b49f3fc428ebfd97e4  SharedAnalyzerSources.cs
1942cc6cfa58ae99297f5a4b9102b3a14cb6e2d12adcccc55b2187d421ab6e3d  DeclaredTypeScannerTests.cs
e20d7570be1b80a13956ff3c2850e62b8361ab12da1c2390d333c38937955a04  EngineToBoundariesOneDoorAnalyzerTests.cs
6ec75338a3eb73c3eaae59842c99170c3b3e3fb751f23ce07e2a555cf96d0c26  OneDoorIntoCrossPlatformAnalyzerTests.cs
7139aa1d44db1d5e1ec4486218b4ece897b888911da76fcb880f7224acb81a02  OneDoorIntoPerOsAnalyzerTests.cs
```

All my runs ran in `…/scratchpad/lc-copy` and `…/scratchpad/lc-baseline`, rsync copies excluding `.git`, `bin`, `obj` and `.codegraph`. Build output under `analyzers/` was deleted before every run. After all six mutations, `diff -r --exclude=bin --exclude=obj` between the shared checkout's `analyzers` tree and the copy's produced no output.

## Every captured claim, re-derived

| Evidence file | Claim | My run |
|---|---|---|
| `tdd-acceptance25-baseline-analyzer-tests.txt` | full suite before the change: Passed 616, Total 616, `EXIT=0` | reconstructed the five files from `a12e58b` in a separate copy: Passed 616, Total 616, exit 0 |
| `tdd-acceptance25-full-analyzer-tests.txt` | full suite after: Passed 619, Total 619, `EXIT=0` | Passed 619, Total 619, exit 0 |
| `tdd-acceptance25-affected-classes.txt` | the four-class filter: Total 87, Passed 87, `EXIT=0` | same filter string: Passed 87, Total 87, exit 0 |
| `tdd-acceptance25-intermediate-build.txt` | `dotnet build -p:WarningsNotAsErrors=AG0015`: build succeeded, 1 warning, 0 errors, `EXIT=0` | build succeeded, 1 warning (the same AG0015 at `src/AgentGuard.Boundaries/SystemServices.cs(88,30)`), 0 errors, exit 0 |
| `tdd-acceptance25-intermediate-build.txt:5` | quotes `contract.md:110` for the AG0015 allowance | `contract.md:110` is that sentence, word for word |
| `tdd-acceptance25-scope-and-digests.txt` | `git diff --check` exit 0; scoped status empty; runner and its tests unchanged; five digests | all reproduce |
| `tdd-acceptance25-mutation-proofs.txt` M-A25-1 | Failed 5, Passed 614, Total 619, `EXIT=1` | applied the same edit to `CompositionPoint.IsContainerFactoryType`: Failed 5, Passed 614, Total 619, exit 1, and the same five test names |
| `tdd-acceptance25-mutation-proofs.txt` M-A25-2 | Failed 3, Passed 616, Total 619, `EXIT=1` | applied the same edit to `OneDoorRule.InspectCall`: Failed 3, Passed 616, Total 619, exit 1, and the same three test names |

The 616→619 delta is exactly three added tests and nothing removed. I listed every test name in both trees with `dotnet test --list-tests` and diffed them: nothing appears only in the baseline, and the three that appear only in the delivered tree are `EngineToBoundariesOneDoorAnalyzerTests.PermittedFactory_FromSameNamedContainerInAnotherNamespace_IsReported`, `OneDoorIntoCrossPlatformAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported` and `OneDoorIntoPerOsAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported`.

## The four mutations I added

Each ran in the isolated copy with all `analyzers/` build output deleted first, and each was reverted to a file byte-identical to the shared checkout's before the next.

| Mutation | What it changes | Result |
|---|---|---|
| M3 | `OneDoorRule.InspectWrittenName` reports at `(context.Node.Parent ?? context.Node).GetLocation()` — written-name lens moved off the identifier onto the member-access node, identifier and message untouched | Failed 2, Passed 617, exit 1. Both failures are the new AG0023 and AG0029 tests. Nothing else in the suite catches it. |
| M4 | `DeclaredTypeScanner.AnalyzeInvocationReturn` reports at `invocation.Syntax.GetFirstToken().GetLocation()` — carried-type lens moved off the invocation onto its first token | Failed 2, Passed 617, exit 1: the new AG0023 test and `DeclaredTypeScannerTests.TheGatedEntryPoint_ScansTheReturnTypeOfAnInvokedMember`. |
| M5 | `IsProgramInCli`, `IsBuilderInTestHelpers` and `IsWrapperFactoryType` all made namespace-blind | Failed 1, Passed 618, exit 1: `GuardedConstructionAnalyzerTests.CreateCall_InProgramNamedTypeInDifferentNamespaceOfGuardAssembly_IsReported`. See the residual note below. |
| M6 | `OneDoorRule.RegisterBoundariesAndEngineGates` registers the written-name lens twice, so each written-name report is duplicated at the same location with the same identifier and the same message | Failed 2, Passed 617, exit 1. Both failures are the new AG0023 and AG0029 tests. Only a count assertion can catch this, and only these two catch it. |

M2 plus M3 plus M4 cover all three lenses AG0023 and AG0029 register on Engine. Each lens's report location is now pinned, and M6 shows the count is pinned in the other direction too.

## The no-RED exception is backed

`contract.md`, "Tim approved the direct-dispatch exception for the TDD round, presented as written": "Regression tests of helpers already implemented during RULE-PHASE may pass immediately. Prove their assertions exercise the required behavior."

The three new tests pass on first run because `CompositionPoint.IsContainerFactoryMethod` already conjoins the namespace. That is the case the decision above covers, and the second sentence's obligation — prove the assertions exercise the behavior — is discharged by M-A25-1 (the tests go red when the namespace leg is removed) and by M-A25-2, M3, M4 and M6 (the location and count assertions go red when a report moves or doubles).

## The tests test what they claim

**The caller's namespace is the only leg that varies.** `SharedAnalyzerSources.InsideContainerFactory` and `InsideContainerFactoryInWrongNamespace` both route through the private `ContainerFactoryIn`, which routes through the one `EngineCompilation(containerNamespace, …)` template. The only difference between the compliant and the wrong-namespace fixture is `AgentGuard.Engine` against `AgentGuard.Engine.Decoy`. The compilation is still named `AgentGuard.Engine` in all three tests, the class is still `SystemServices`, and the method is still a static ordinary `Create`. M-A25-1 proves this independently: with the namespace check removed the predicate accepts the caller and all three tests report nothing, which can only happen if every other leg already matched.

**The call is the genuine door, not a fake.** AG0040 calls `EnvironmentAdapter.Create()` against the real `BoundariesSource` compiled as `AgentGuard.Boundaries`; AG0023 calls `CrossPlatformAdapters.Create()` against `CrossPlatformSource` compiled as `AgentGuard.CrossPlatform`; AG0029 calls `PlatformServices.Create()` against `PerOsSource` compiled as `AgentGuard.CrossPlatform.MacOS`, a genuine per-OS assembly name.

**The wrong namespace is the tightest miss available.** `WellKnownType.Is` compares `type.ContainingNamespace?.ToDisplayString()` by ordinal equality, so `AgentGuard.Engine.Decoy` fails against `AgentGuard.Engine`. A prefix or `StartsWith` match would accept the fixture. The doc comment on `WrongContainerNamespace` claims exactly that, and it is true.

**The expected spans come from the fixture, not from the diagnostic.** Each test passes its own constants — `PermittedFactoryCall`, `DoorCall`, `DoorType` — which are the same constants the fixture builder is handed. Nothing is read back off the reported diagnostic.

**The expected counts match the registrations.** AG0040 goes through `OneDoorRule.RegisterEngineCallGate`, one lens, one diagnostic. AG0023 and AG0029 go through `RegisterBoundariesAndEngineGates`, which registers three Engine-facing lenses. AG0023's fake `CrossPlatformAdapters.Create()` returns `CrossPlatformAdapters`, a guarded type, so the carried-type lens fires and three diagnostics are expected. AG0029's fake `PlatformServices.Create()` returns `object`, so it does not and two are expected. Both in-code comments state this and both are accurate against the analyzer source.

**The fixtures compile clean.** All three tests call `AnalyzerRunner.RunWithReferenceAsync` with no `expectedCompilerErrors` argument, so `ThrowOnUnexpectedCompilerErrors` runs against an empty expectation. Acceptance 18's "no new expected-compiler-error exception is introduced" holds.

**Nothing existing was weakened.** The only change to an existing assertion is in `DeclaredTypeScannerTests.cs:71-78`, where the inline `Assert.Equal(new[] { … }.OrderBy(…), diagnostics.Select(…).OrderBy(…))` was replaced by `diagnostics.AssertSpans(source, "EngineInternal", DeclaringMethod, DeclaringMethod + "()")`. Same three expected texts, same ordinal sort on both sides, same `Assert.Equal` on the sorted sequences. M4 confirms that test still bites. `EngineToBoundariesOneDoorAnalyzerTests` extracted `"EnvironmentAdapter.Create()"` into `PermittedFactoryCall` and uses it in the theory data and in `PermittedFactory_FromAnotherContainerMethod_IsReported`; the captured per-case output still shows the theory case as `call: "EnvironmentAdapter.Create()"`.

**The wrong-assembly direct test remains.** `EngineToBoundariesOneDoorAnalyzerTests.ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly` is untouched by this diff and still passes, as is its positive counterpart `ContainerFactoryIdentity_IsTheStaticCreateInTheEngineAssembly`. The existing non-Engine tests Acceptance 6 names are untouched and pass.

## Authority and scope

Nothing outside `analyzers/AgentGuard.Analyzers.Tests` changed. `AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs` are byte-identical to `a12e58b`. No diagnostic was suppressed, no test deleted, no assertion reversed. Nothing was staged, committed, pushed or stashed. The new `AssertSpans` helper, the `WrongContainerNamespace` constant, the `EngineCompilation` namespace overload and the `InsideContainerFactoryInWrongNamespace` entry point are all internal test infrastructure in `SharedAnalyzerSources.cs`, the file the contract already approved as the shared fixture owner; under `rails-decisions` these are implementation, not decision-level items, so none of them needed Tim.

## Residual notes

- `AssertSpans` compares the source **text** a diagnostic spans, not its offset, because `AnalyzerRunner.SpanText` returns `source.Substring(span.Start, span.Length)`. A report displaced to a different position carrying identical text would pass. In all three new fixtures each expected text occurs exactly once, so the assertion is unambiguous there, and M2, M3, M4 and M6 all landed. The mechanism is the pre-existing one `DeclaredTypeScannerTests` already used; this is a property of it, not something this round introduced.
- M5 found that two of `CompositionPoint`'s identity legs have no test isolating the namespace axis: `SystemServicesBuilder` in `AgentGuard.TestHelpers`, and `FileInfoFactory` in `AgentGuard.CrossPlatform` (AG0033). Neither is this relocation's privileged caller and the contract forbids expanding into unrelated fixtures, so neither is a gap against Acceptance 25. `Program` in `AgentGuard.Cli` is already covered, by `GuardedConstructionAnalyzerTests.CreateCall_InProgramNamedTypeInDifferentNamespaceOfGuardAssembly_IsReported`.
- AG0015's construction-site exemption runs through the same `IsContainerFactoryType` predicate, and `TimeMustUseTimeProviderAnalyzerTests` isolates the assembly axis (`TimeProviderSystem_InSameNamedContainerInAnotherAssembly_IsReported`) but not the namespace axis. No AG0015 test failed under M-A25-1. Acceptance 14 does not ask for it and the contract treats AG0015 as the clock rule rather than one of the factory-access rules, so this is not a gap against Acceptance 25 either — and after this round the predicate itself is pinned on the namespace axis by five tests, so it cannot regress silently.
- `tdd-acceptance25-mutation-proofs.txt:5` states "Before this run the copy's analyzers tree was diffed against the delivered one: identical, no output" as prose with no captured command; the matching check after the run is captured, but written as `$ diff -r --exclude=bin --exclude=obj $REPO/analyzers $COPY/analyzers` with the variables unexpanded rather than as the literal command line. This is the same class of thing `tdd-correction2-review-dry.md` flagged in the previous round's mutation file. It changes nothing here — I built my own copy of the same delivered files and reproduced both mutation results exactly — but the before-run check is still an assertion rather than a capture.
