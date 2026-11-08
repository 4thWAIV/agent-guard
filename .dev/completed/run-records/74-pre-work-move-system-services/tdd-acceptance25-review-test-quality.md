# Test-quality review — Acceptance 25 round (SystemServices relocation)

**Verdict: PASS.** No `rails-test-code` violation is confirmed. Every judgement below comes from the live working-tree files and from my own runs in an isolated copy; nothing is taken from the author's evidence files or from an earlier review.

Reviewed tree: HEAD `a12e58b305c5315bc175332066e90a2d693b1769`, plus the five uncommitted test files. I verified their digests against the shared checkout before and after every run, and the shared checkout is byte-identical to how I found it.

## Confirmed findings

None.

## Things the next stage must not trip on

1. **The new span assertion compares the TEXT at a diagnostic's span, not the span's position.** `SharedAnalyzerSources.AssertSpans` (`analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:975-982`) reads each diagnostic through `AnalyzerRunner.SpanText`, which returns `source.Substring(span.Start, span.Length)`, and compares the sorted multiset of those strings against the expected strings. For the three delivered fixtures this is equivalent to comparing positions, because each expected text occurs exactly once in its fixture — I rebuilt all three fixture sources from the live `EngineCompilation` template and counted: `EnvironmentAdapter.Create()` 1, `CrossPlatformAdapters` 1, `CrossPlatformAdapters.Create()` 1, `PlatformServices` 1, `PlatformServices.Create()` 1. A future fixture that repeats an expected text would make the assertion unable to tell two positions apart. The helper's own documentation says it asserts that diagnostics "point at EXACTLY the source text", which is accurate, but a reader could take it as a position check.

2. **AG0015's construction-site gate is still blind to a wrong-namespace container, and no test covers it.** `CompositionPoint.EnclosesConstructionSite` runs through the same `IsContainerFactoryType` predicate the one-door caller test uses. With that predicate made namespace-blind, the whole 619-case suite fails only the three new tests and AG0041's two `SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported` rows — `TimeMustUseTimeProviderAnalyzerTests` passes throughout. This sits under Acceptance 14, not 25 (contract.md:51 and :159 explicitly separate AG0015's type-level check from the factory-access rules), so it is not a defect in this round's scope. It is the one remaining place where the namespace conjunct carries no test.

3. **The one-door message assertions pin the failure kind, not the required site the message names.** `AssertNamesExactly` matches the fragment `"the call site is not"` (`SharedAnalyzerSources.cs:442`), which stops before the site description. I changed `CompositionPoint.ContainerFactoryDescription` to name `AgentGuard.Boundaries.SystemServices.Create()` instead of the Engine one, leaving the identifier, the location and the failure kind intact: 62 cases fail, all of them in `EngineInternalsOneDoorAnalyzerTests`, and every AG0040/AG0023/AG0029 test — the three new ones included — still passes. The wrong value is caught by the suite, through AG0041, because `ContainerFactoryDescription` has a single owner. It is not caught by the tests Acceptance 25 governs. This is inherited from the pre-existing helper the instructions told the author to reuse, not something this round introduced.

## The no-RED claim

All three new tests passed on first run. That is legitimate here, and I am recording it as legitimate rather than waiving it.

The behavior they pin — the namespace conjunct in `CompositionPoint.IsContainerFactoryType` — was built and accepted in RULE-PHASE, before this round began. It is present at HEAD: the 616-case baseline is green at HEAD with no analyzer change in the working tree (`git status` shows no file under `analyzers/AgentGuard.Analyzers/` modified). The only way to make these tests RED would be to break working analyzer code, which `rails-test-code` rail 8 forbids and which `tdd-worker-instructions.md` forbids in the same words: "Do not break working code, stub an existing body, invert an assertion, or manufacture compiler errors to obtain RED."

The substitute for RED is the mutation demonstration, and it does discriminate. I ran my own mutations rather than reading the author's:

| Mutation (mine, in an isolated copy) | Result |
| --- | --- |
| R1 — `CompositionPoint.IsContainerFactoryType` matches name + assembly only, ignoring the namespace | 5 of 619 fail: the three new tests, plus AG0041's two wrong-namespace rows. `Expected: ["EnvironmentAdapter.Create()"] / Actual: []` and the same shape for the other two. |
| R1 applied to the HEAD test files instead (616 cases) | 2 of 616 fail — AG0041's two rows only. The three one-door caller gates were blind to namespace-blindness before this round. |
| R2 — the written-name lens reports at the parent node; identifier and message untouched | 2 of 619 fail, and only the two new tests that assert a written-name span: `Expected: ["CrossPlatformAdapters", …] / Actual: ["CrossPlatformAdapters.Create", …]`. Nothing else in the suite pins that location. |
| R3 — the carried-type lens registration is removed from the Engine gate in `OneDoorRule.RegisterBoundariesAndEngineGates` | 1 of 619 fails: the new `OneDoorIntoCrossPlatformAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported`, on count (3 expected, 2 actual). The same mutation against the HEAD test files passes all 616. |

R1 is the mutation the instructions required ("Making caller recognition ignore the namespace causes the new caller tests to fail") and it lands on exactly the three new tests. R2 is the required location mutation ("Moving a reported diagnostic to the wrong source location, while preserving its identifier and message, causes the location assertions to fail") and it lands on exactly the two tests that assert a written-name span. R3 is mine, and it shows the count half of `AssertSpans` carries force that nothing else in the suite carried.

## Acceptance 25, clause by clause

The criterion, copied from `contract.md:279`:

> Prove the privileged caller's full identity: wrong-namespace callers are rejected by the access analyzers; direct tests of `CompositionPoint` reject a same-named method in the wrong assembly. Existing non-Engine tests continue to prove the assembly gate. Tests assert the intended diagnostics and source locations, accommodating multiple scanner reports without weakening an existing expectation.

**"wrong-namespace callers are rejected by the access analyzers."** Met. One test per access analyzer:

- `EngineToBoundariesOneDoorAnalyzerTests.PermittedFactory_FromSameNamedContainerInAnotherNamespace_IsReported` (AG0040)
- `OneDoorIntoCrossPlatformAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported` (AG0023)
- `OneDoorIntoPerOsAnalyzerTests.DoorCall_FromSameNamedContainerInAnotherNamespace_IsReported` (AG0029)

Those three are the analyzers whose gate consults the caller identity: `OneDoorRule.InspectCall:154` and `OneDoorRule.TypeReferenceDiagnostic:219` are the only call sites of `CompositionPoint.IsContainerFactoryMethod` that test a CALLER. AG0041 uses the same predicate on the CALLEE and gates its callers by assembly name alone (`EngineInternalsOneDoorAnalyzer.GatedAssemblies`), so there is no caller namespace for it to reject; its wrong-namespace coverage is the callee-side test that already exists.

Each fixture varies the namespace and nothing else, which is what the instructions demanded ("These tests must vary the caller's namespace — not substitute a fake factory being called"). The container class is still named `SystemServices`, its `Create()` is still static, it is still compiled into the real `AgentGuard.Engine` assembly, and the call is the genuine permitted target in every case — `EnvironmentAdapter.Create()` is one of the four named identities, `CrossPlatformAdapters.Create()` is AG0023's door, and `PlatformServices.Create()` is AG0029's door reached in a genuine per-OS assembly (`AgentGuard.CrossPlatform.MacOS`). Both the correct-namespace and the wrong-namespace fixtures are built by the same private `ContainerFactoryIn` (`SharedAnalyzerSources.cs:1045-1053`), so the namespace is provably the only difference and the rejection cannot be attributed to anything else.

The namespace chosen, `AgentGuard.Engine.Decoy`, is a child of the real one. A prefix match would wrongly accept it, so the fixture is the nearest miss rather than an easy one. `WellKnownType.Is` compares `ContainingNamespace.ToDisplayString()` with `string.Equals`, so the real predicate rejects it.

The correct-namespace counterparts are preserved and unchanged in all three classes: `PermittedFactory_FromContainerFactory_IsNotReported` (a four-row theory), `DoorCall_FromContainerFactory_IsNotReported` in each of the other two. All assert `Assert.Empty`.

**"direct tests of `CompositionPoint` reject a same-named method in the wrong assembly."** Met by the pre-existing `EngineToBoundariesOneDoorAnalyzerTests.ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly`, which is present and unmodified — it drives the real predicate on a resolved `IMethodSymbol` and asserts the four matching legs before asserting the predicate is false. The instruction "The existing wrong-assembly direct test remains" holds.

**"Existing non-Engine tests continue to prove the assembly gate."** Met. `AnyBoundariesCall_FromAnotherAssembly_IsNotReported`, `CallIntoAdapterFactory_FromBoundaries_IsNotReported`, `CallIntoOtherCrossPlatformType_FromBoundaries_IsReported`, `ReferenceToOtherCrossPlatformType_FromBoundaries_IsNotReported`, `CallPlatformServicesCreate_FromBoundaries_IsNotReported`, `CallOtherPerOsMember_FromBoundaries_IsReported` and `CallOldPlatformDoor_FromBoundaries_IsReported` are all present and untouched.

**"Tests assert the intended diagnostics and source locations, accommodating multiple scanner reports without weakening an existing expectation."** Met for the cases the criterion governs. Each new test asserts the identifier and severity (through `AssertNamesExactly` → `AssertAllReported` → `AssertReported`), the exact count and the exact span of every report (through `AssertSpans`), the failure condition the message must name, and that no message names the condition that did not fail. The multi-scanner case is accommodated rather than dodged: the CrossPlatform test writes the repeated span out twice and requires three diagnostics, and the PerOs test requires two.

The expected spans are spelled from each test's own fixture constants — `DoorCall`, `DoorType`, `PermittedFactoryCall` — the same constants that build the fixture, never read back off the diagnostic. I checked the multiset sizes against the fixtures rather than against the analyzer: AG0040 registers only the call lens (`OneDoorRule.RegisterEngineCallGate`), so one report; AG0023's door `Create()` returns the door type in `CrossPlatformSource`, so the carried-type lens fires and there are three; AG0029's door `Create()` returns `object` in `PerOsSource`, so it does not and there are two. Each of those is a fact about the fixture, not a transcription of what the analyzer happened to emit.

## Nothing was weakened, renamed or lost

I compared the full set of executed case names, not the totals. Baseline: the five test files restored from HEAD into an isolated copy, 616 cases. Delivered: the working-tree files, 619 cases. Both runs used a TRX logger and I diffed the sorted `testName` sets.

- In baseline but not in delivered: none.
- In delivered but not in baseline: exactly the three new tests named above.

The two edits to existing assertions are value-preserving:

- `EngineToBoundariesOneDoorAnalyzerTests.cs` — the literal `"EnvironmentAdapter.Create()"` in the theory data and in `PermittedFactory_FromAnotherContainerMethod_IsReported` became the constant `PermittedFactoryCall`, whose value is that same literal. `Assert.Single(diagnostics)` in that test is still there.
- `DeclaredTypeScannerTests.cs:74` — the inline `Assert.Equal(new[] { "EngineInternal", DeclaringMethod, DeclaringMethod + "()" }.OrderBy(…), diagnostics.Select(…).OrderBy(…))` became `diagnostics.AssertSpans(source, "EngineInternal", DeclaringMethod, DeclaringMethod + "()")`. `AssertSpans` performs the identical sorted-multiset comparison over the identical inputs. The expected values are unchanged and the surrounding `AssertAllReported` and per-message `Assert.Contains` assertions are untouched.

No `Skip`, no commented-out test, no loosened expectation, no `#pragma` appears anywhere in the diff.

## Rail-by-rail

| Rail | Verdict |
| --- | --- |
| 1 — every acceptance criterion covered | PASS — all four clauses of Acceptance 25, mapped above |
| 2 — RED before green | PASS under the exception recorded in `tdd-worker-instructions.md`; substantiated by R1/R2/R3 rather than asserted |
| 3 — assert the real type, not a fake | PASS — the system under test is the real analyzer driven through the real `AnalyzerRunner`; the fakes are the referenced guarded assemblies, which are the inputs |
| 4 — two-sided | PASS — each new rejection test has its unchanged correct-namespace counterpart asserting `Assert.Empty` |
| 5 — pointed-integration for real OS behavior | Not applicable — these are Roslyn analyzers with no OS or filesystem dependency |
| 6 — every assertion can fail | PASS — R1 fails all three, R2 fails the two span-asserting ones, R3 fails the count-asserting one |
| 7 — derived from the criterion | PASS — spans from the fixture constants, message fragments from test-layer literals (`SharedAnalyzerSources.cs:442-466`), never read off the analyzer |
| 8 — never weaken, skip or delete | PASS — proved by the case-name set comparison, `Skipped: 0`, and reading both edits to existing assertions |
| 9 — one behavior per test | PASS — one rejection scenario per test |

## What I attacked and could not break

- **That the new tests restate what the analyzer does rather than pinning required behavior.** R1 refutes it: with the namespace conjunct removed the analyzer reports nothing and all three fail with an empty actual collection.
- **That they add nothing the existing 616 already proved.** Refuted by running R1 against the HEAD test files: only AG0041's two rows fail there. The caller-side namespace leg was genuinely unproven before this round.
- **That the location assertions are decoration.** R2 refutes it: displacing only the written-name lens's location, with the identifier and the message byte-identical, fails exactly the two tests that assert that span and nothing else in the suite.
- **That the count is not really pinned.** R3 refutes it: deleting the carried-type lens registration from the Engine gate passes all 616 at HEAD and fails exactly one of the 619 now.
- **That the span assertion could pass at the wrong position because it compares text.** Could not break it for the delivered fixtures — I rebuilt all three from the live template and every expected text occurs exactly once. Recorded above as a caveat on the helper, not as a defect in these tests.
- **That a case was silently dropped or renamed behind a green total.** Refuted by the full case-name set comparison: baseline minus delivered is empty.
- **That the author's evidence was reconstructed.** The recorded outputs reproduce. My independent baseline is 616 passed, my independent delivered run is 619 passed, and my R1 reproduces the author's M-A25-1 failure set exactly (5 failures, the same five names).
- **That the criterion's "access analyzers" is wider than the three classes touched.** Checked every consumer of the `CompositionPoint` predicates. The caller-side use is confined to `OneDoorRule`, which backs AG0040, AG0023 and AG0029. AG0041 gates callers by assembly name only. AG0015's use is type-level and the contract separates it at lines 51 and 159 — recorded above as an observation, not a breach of this criterion.

## Commands

Run in an isolated rsync copy under the session scratchpad, with `analyzers/*/bin` and `analyzers/*/obj` deleted before every run so no run could read a stale analyzer.

```
rsync -a --delete --exclude .git --exclude bin --exclude obj --exclude .dev --exclude .codegraph <repo>/ <scratch>/rev/
diff -r --exclude=bin --exclude=obj <repo>/analyzers <scratch>/rev/analyzers      # identical, before and after
dotnet test analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj --logger "trx;LogFileName=delivered.trx"
                                                                                  # Passed 619, Total 619, EXIT=0
git show HEAD:analyzers/AgentGuard.Analyzers.Tests/<each of the five> > <scratch>/base/…
dotnet test … --logger "trx;LogFileName=baseline.trx"                             # Passed 616, Total 616, EXIT=0
comm -23 baseline-cases.txt delivered-cases.txt                                   # empty
comm -13 baseline-cases.txt delivered-cases.txt                                   # the three new tests
# R1 on delivered: Failed 5, Passed 614      # R1 on baseline: Failed 2, Passed 614
# R2 on delivered: Failed 2, Passed 617      # R3 on delivered: Failed 1, Passed 618
# R3 on baseline:  Failed 0, Passed 616      # R4 on delivered: Failed 62, Passed 557 (all AG0041)
shasum -a 256 <the five delivered files>                                          # matched before and after every run
git -C <repo> status --porcelain                                                  # unchanged throughout
```
