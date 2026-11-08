# TDD review — test quality (independent)

## Verdict

**PASS.** No violation of `rails-test-code` is confirmed. Every Acceptance claim in `tdd-result.md` that I could
check against the delivered tests is true against the contract's own words. The six mutation proofs reproduce
exactly in my own isolated copy, and four further mutations of my own choosing confirm the new tests and the
reused ones are live rather than green-by-construction.

Three findings are recorded below as OBSERVATIONS, not failures: each is bounded, none is an unmet Acceptance
criterion, and two of the three are pre-existing RULE-PHASE decisions that Tim already accepted.

## Snapshot reviewed, and isolation

Reviewed the working tree of `/Users/timothystockstill/code/macos/4thWAIV/agent-guard` at branch
`appd-1-process`, HEAD `c1c2a18`, with the uncommitted and untracked delivered files in place. I verified all ten
delivered files against `tdd-digests.txt` with `shasum -a 256` before and after my run — every digest matches, so
the snapshot I read is the snapshot the round produced, and the shared checkout was never mutated.

Destructive work ran only in an isolated rsync copy at
`/private/tmp/claude-501/-Users-timothystockstill-code-macos-4thWAIV-agent-guard/c96ea1f7-6b0a-4cef-b29f-2fca152f05fe/scratchpad/review-copy`
(`.git`, `bin`, `obj`, `.dev` excluded; `.codegraph/daemon.sock` skipped as a non-regular file). `diff -r` against
the delivered `analyzers/` tree was clean before the first mutation and clean again after the last.

Independent baseline in that copy, before any mutation:

    dotnet test analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj
    Passed!  - Failed: 0, Passed: 616, Skipped: 0, Total: 616

## Nothing stubbed, inverted, weakened, deleted or skipped — proved, not assumed

The RULE-PHASE result `rule-phase-fix5-output.json` records a per-class case table taken from its own run. Set
against the TDD round's per-case run (`tdd-affected-classes-detailed.txt`), it closes exactly:

| Class | RULE-PHASE | after TDD | delta |
| --- | --- | --- | --- |
| `EngineInternalsOneDoorAnalyzerTests` | 81 | 81 | 0 |
| `EngineToBoundariesOneDoorAnalyzerTests` | 13 | 15 | +2 (the two direct `CompositionPoint` tests) |
| `OneDoorIntoCrossPlatformAnalyzerTests` | 33 | 33 | 0 |
| `OneDoorIntoPerOsAnalyzerTests` | 34 | 34 | 0 |
| `WrittenNameScannerTests` | 5 | 5 | 0 |
| `TimeMustUseTimeProviderAnalyzerTests` | 12 | 12 | 0 |
| `ContractConcreteTypeMustNotBeReferencedAnalyzerTests` (AG0006) | 7 | 7 | 0 |

600 + 2 + `SymbolResolutionTests` 8 + `TypeTreeTests` 4 + `DeclaredTypeScannerTests` 2 = 616, which is the suite
total. No case was deleted, merged or renamed away to make room for a new one. `Skipped: 0` in the baseline and in
every run since; `grep` finds no `Skip =`, no `Assert.True(true)`, no empty body, no commented-out test anywhere in
`analyzers/AgentGuard.Analyzers.Tests`.

`AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs`: `git diff --stat HEAD` reports no change for either, and neither
appears in `git status --short`. Unchanged, as Acceptance 18 requires.

No new expected-compiler-error exception: `grep -n '"CS[0-9]'` over all ten touched files returns nothing, and
`AnalyzerRunner.CompileAsync`/`RunWithReferenceAsync` (`analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs:115`,
`:78`) throw on any unexpected compiler error with an empty default expectation — so every new fixture provably
compiles clean.

Nothing under `src/` or `tests/` was touched: `git status --short -- src tests` is empty. `git diff --check` exits 0.

## The six mutation proofs, re-run independently

Each applied in my isolated copy, run, and reverted. All six reproduce the author's recorded outcome, including
every "…and the pre-existing tests still pass" claim, which is the load-bearing half.

| Mutation | My result | Matches `tdd-mutation-proofs.txt` |
| --- | --- | --- |
| M1 — delete the `IPointerTypeSymbol` branch from `TypeTree.Any` | 4 of 4 `TypeTreeTests` FAIL; the nonmatching case fails on `Expected ["Sample.Other"], Actual []` — the recorded walk, not a bare false | yes |
| M2 — disable the invoked-return registration in `DeclaredTypeScanner.RegisterForAssembly` | `TheGatedEntryPoint_ScansTheReturnTypeOfAnInvokedMember` FAILS; the `Declared()` span disappears | yes |
| M3 — add the invoked-return registration to `RegisterDeclarations` | both `DeclaredTypeScannerTests` FAIL **and all 7 AG0006 tests still PASS** | yes |
| M4 — drop the assembly from `CompositionPoint`'s identity conjunction | `ContainerFactoryIdentity_RejectsTheSameNamedMethodInAnotherAssembly` FAILS; **the other 14 AG0040 tests still PASS** | yes |
| M5 — demote the documented fixture's `///` to `//` | all 4 behavioral documentation cases FAIL at the node assertion | yes |
| M6 — delete tier one from `SymbolResolution.NamedType` | `NamedType_OnAnInvocation_…` FAILS (`Sample.Guarded` for `Sample.Holder`) **and all 40 others, including both malformed-attribute classes, still PASS** | yes |

M2 needed `-p:TreatWarningsAsErrors=false` on my run because removing the only call site leaves
`AnalyzerInvocationReturn` unused (S1144). That is a property of my edit, not a discrepancy in the author's result.

Each mutation breaks only what the author says it breaks: M3 breaks 2 and leaves AG0006's 7 green; M4 breaks 1 and
leaves 14 green; M6 breaks 1 and leaves 40 green.

## Four further mutations of my own

| Mutation | Result | What it establishes |
| --- | --- | --- |
| M7 — make `CompositionPoint.IsContainerFactoryType` namespace-blind (name + assembly only) | `SameNamedContainerInTheWrongNamespace_FromGatedConsumer_IsReported` FAILS on both gated consumers (expected 2, actual 0); 173 others pass | the namespace conjunct of the privileged identity **is** pinned by an access analyzer — Acceptance 25's first clause has real force |
| M9 — `WrittenNameScanner.IsDocumentationReference` always returns `false` | all 4 behavioral documentation cases FAIL on `Assert.Empty`, plus `WrittenNameScannerTests.EveryNameInsideACref_IsADocumentationReference` | the documentation tests are not vacuous: they fail when the exclusion stops working, as well as when the fixture stops carrying a cref |
| M11 — `RegisterForAssembly` stops registering the four declaration positions | 25 AG0041/AG0023/AG0029 cases FAIL on exact counts (e.g. expected 5 actual 3, expected 2 actual 1) | the carried-type lens coverage in the AG0041 tables is pinned by count, not by non-emptiness |
| M12 — delete the type-info recovery tier from `SymbolResolution.NamedType` | `InteropOnlyInCrossPlatformLibrariesAnalyzerTests.MalformedSameNamedDllImportAttribute_…` and `NativeCallbackBodyMustBeGuardedAnalyzerTests.MalformedUserDefinedUnmanagedCallersOnly_…` both FAIL | Acceptance 26's reuse claim is true: the existing malformed-attribute tests really do prove that the extraction preserves recovery, so reusing them instead of writing a new malformed fixture is proof, not a dodge |

## Acceptance 24 — the strengthened documentation assertion

**The author's claim is true, the strengthening is correct, and it is within the author's authority.**

Acceptance 24 requires, verbatim: "Assert the fixture contains a documentation-reference node so the test cannot
pass by omitting documentation parsing."

What stood before this round was `Assert.Contains("cref", source, StringComparison.Ordinal)` — a check on the
fixture's TEXT. RULE-PHASE's own record claims it discharged the criterion: `rule-phase-fix5-output.json` says
"`Assert.Contains(\"cref\", source, StringComparison.Ordinal)` proving the fixture really contains a
documentation-reference node". It does not.

I proved the hole directly rather than taking either party's word. In the isolated copy I applied M5 (so the text
`cref` survives but no `CrefSyntax` node is parsed) **and** reverted the assertion in
`OneDoorIntoCrossPlatformAnalyzerTests` to the old text form:

    Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1

The old assertion passes over a fixture that carries no documentation reference at all, and `Assert.Empty` then
proves nothing about the exclusion. With the delivered
`SharedAnalyzerSources.AssertHasDocumentationReference` (`analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs:871`,
which parses the source the way `AnalyzerRunner` does and asserts a `CrefSyntax` node is present) the same mutation
fails all four cases. The criterion was unmet before this round and is met now.

Authority: this is a strengthening, not a reversal and not a weakening. The node assertion strictly implies the
text assertion it replaced (a parsed `CrefSyntax` node cannot exist without the text), so nothing was lost. The
round's instruction forbids reversing assertions other than the two in Acceptance 7 and forbids weakening or
deleting tests; it does not forbid completing an unmet criterion, and "Account for every remaining criterion …
using the contract's exact requirements" requires exactly this. All three call sites carry it
(`OneDoorIntoCrossPlatformAnalyzerTests.cs:361`, `OneDoorIntoPerOsAnalyzerTests.cs:368`,
`EngineInternalsOneDoorAnalyzerTests.cs:245`); no text-only `Contains("cref"` remains anywhere.

## The one uncovered branch — ruled TRUE, not an excuse

`SymbolResolution.Symbol` (`analyzers/AgentGuard.Analyzers/SymbolResolution.cs:36`) ends with
`return bound is INamedTypeSymbol { TypeKind: TypeKind.Error } ? null : bound;`. The author reports it as
uncovered and says covering it needs a fixture the instruction and Acceptance 18 forbid.

That is correct. `GetSymbolInfo(...).Symbol` yields an error-typed named symbol only where binding failed, and
binding fails only in a compilation carrying compiler errors. `AnalyzerRunner.CompileAsync` and
`RunWithReferenceAsync` reject any compilation whose errors are not declared up front, so reaching the branch means
declaring a compiler error on a new fixture — precisely what Acceptance 18 forbids ("New relocation fixtures compile
under the default empty compiler-error expectation; no new expected-compiler-error exception is introduced") and
what the round's instruction forbids ("do not create an invalid unresolved-name fixture as a shortcut around the
compiler-error requirement"). The only other route is a second compilation framework, which the contract's Surfaces
rule out ("use the existing runner, without a second compilation framework").

The author states it as an uncovered branch and never claims it covered, and the analogous `NamedType` fail-closed
path is genuinely covered by the existing, unmodified
`InteropOnlyInCrossPlatformLibrariesAnalyzerTests.UnresolvedInteropAttribute_IsReportedViaSyntacticFallback`. No
Acceptance criterion requires branch coverage of that guard. Honest report, not an excuse.

## The 12 permitted-public and 52 negative cases — read off the per-case output

From `tdd-affected-classes-detailed.txt`, not from the summary:

    grep -c "Reach_FromGatedConsumer_ProducesExactlyItsExpectedDiagnostics"        -> 64
    ... | grep -c "expectedSubjects: \[\]"                                         -> 12
    ... | grep -vc "expectedSubjects: \[\]"                                        -> 52

All 12 empty-expectation cases name `EnginePublic` — the six permitted public forms (`typeof`, a public member
call, a field, a return type, a parameter, a generic argument) crossed with the `guard` and
`AgentGuard.TestHelpers` compilations. The 52 remaining are the prohibited rows: 14 Engine-internal reaches and 12
container reaches, each against both consumers. Both counts preserved exactly; the run's own footer reads
`Total tests: 221 / Passed: 221`. Separately confirmed from the per-case lines: the four permitted factory
identities run as four separate AG0040 cases, and the AG0023 and AG0029 type-reference theories run 14 cases each.

## Acceptance adjudication

Every row of the author's mapping, checked against the tests and the contract's words.

| # | Author's claim | My ruling |
| --- | --- | --- |
| 1 | LATER (final verification) | TRUE — `make build`/`make test` bracket the implementation; the opening half is `contract-opening-bracket.txt`, the closing half is post-IMPLEMENT |
| 2 | EXISTING — `PermittedFactory_FromContainerFactory_IsNotReported`, 4 cases | TRUE — one case per named identity, all four in the run |
| 3 | EXISTING — five separate tests | TRUE — `EngineToBoundariesOneDoorAnalyzerTests.cs:114,129,170,199,214`, one per listed shape, incl. the fifth factory returning a service interface and both decoys |
| 4 | EXISTING — two tests | TRUE — `:185` and `:232` |
| 5 | EXISTING — six tests per class, message half via `AssertNamesExactly` | TRUE — `AssertNamesExactly` (`SharedAnalyzerSources.cs:928`) checks the required condition IS named and that no unfailed condition is named, so no test can assert that a call to the door is not the door |
| 6 | EXISTING — all five named cases pass | TRUE — each appears once in the per-case run and passed |
| 7 | EXISTING — the two renamed tests, each beside a positive | TRUE — the prohibited-call input is byte-identical to HEAD's (`CrossPlatformUsing` expands to the same `using AgentGuard.CrossPlatform;`), the assertion is reversed as the criterion directs, and no other assertion was reversed this round |
| 8 | EXISTING | TRUE — the fake Engine source carries both `InternalsVisibleTo` grants, so the accepted call compiles and the assertion turns on the analyzer |
| 9 | EXISTING — 28-case table + base list + alias + tests-assembly | TRUE — every named written-name and carried-type position has its own row |
| 10 | EXISTING — 24-case table + base list + alias | TRUE — container reported in every other position; only the `Create()` invocation accepted, incl. the method-group case |
| 11 | EXISTING | TRUE — `AssertReportsAsync` pins exact count and exact message multiset; `grep` finds no `Assert.Single` in that class |
| 12 | EXISTING | TRUE — lambda and local-function cases in all three one-door classes |
| 13 | EXISTING (file unmodified) + NEW | TRUE — AG0006's file is untouched and its 7 cases pass; M3 shows only the new test catches a change to its registration set |
| 14 | EXISTING — four tests | TRUE |
| 15 | LATER | TRUE — nothing under `src/` or `tests/` changed; the wiring-test rename is IMPLEMENT's under the contract's own words |
| 16, 17 | LATER | TRUE — both are msbuild reads of project files IMPLEMENT has not yet changed; running them now would assert the pre-relocation answer |
| 18 | MET | TRUE — reproduced 616/616 independently; both runner files unchanged; no new compiler-error exception |
| 19 | MET for this round | TRUE — `git diff --check` exit 0 (I re-ran it); no cross-OS execution claimed |
| 20 | EXISTING — 14 positions + base list + alias, both rules | TRUE |
| 21 | EXISTING | TRUE — incl. the local-declaration form `Create()` uses today, and `IPlatformServices` never reported in either class |
| 22 | EXISTING | TRUE — the Boundaries gate stays member-access only in both rules |
| 23 | LATER (DRY review) | TRUE — the criterion assigns itself to the DRY review; this round wrote no analyzer source |
| 24 | EXISTING behavioral half + NEW node half | TRUE — see the Acceptance 24 section; M9 shows the behavioral half is live and M5 shows the node half closes a real hole |
| 25 | EXISTING analyzer half + NEW direct half | TRUE — see OBSERVATION 1 for the bounded gap |
| 26 | Generic names EXISTING + resolution NEW + malformed reused | TRUE — and M12 proves the reused tests really carry the recovery proof the criterion assigns them |
| 27 | NEW — `TypeTreeTests` | TRUE — compiler-created pointer/array/generic symbols over `AnalyzerRunner.CompileAsync`, no unsafe source, no runner change; the recorded walk is what proves traversal, and M1 confirms it |
| 28 | EXISTING | TRUE — `CreateCall_FromTheMainTestAssembly_IsReported` reaches the fake Engine's `Create()` from an `AgentGuard.Tests` compilation that the fixture's `InternalsVisibleTo("AgentGuard.Tests")` makes legal, and asserts AG0017 |
| 29 | EXISTING and complete | TRUE — five cases, one per clause: plain cref, generic cref under a type-argument list, operator cref under a cref parameter list, the same name in code, and a preprocessor-directive name |

## Rails checklist

| Rail | Ruling |
| --- | --- |
| 1 — every criterion covered | PASS — each criterion maps to a test that actually proves it, or to a later stage with the contract instruction that assigns it there |
| 2 — RED before green | PASS under Tim's recorded exception. No test was written for behavior still awaiting IMPLEMENT because no remaining criterion assigns one — 15, 16 and 17 are existing tests plus two `dotnet msbuild` reads, not new test code. The one real RED against IMPLEMENT (the production AG0015 warning at `src/AgentGuard.Boundaries/SystemServices.cs(88,30)`) is kept visible in `tdd-intermediate-build.txt` and reported as production impact, never as an acceptance-test failure. The exception's condition — "Prove their assertions exercise the required behavior" — is discharged by ten mutations, six theirs and four mine |
| 3 — real type, not a fake | PASS — the systems under test are the real `SymbolResolution`, `TypeTree`, `DeclaredTypeScanner`, `WrittenNameScanner`, `CompositionPoint` and the real analyzers. The fake Engine and Boundaries sources are fixture INPUT compiled into real second assemblies, never a stand-in for the thing being proven |
| 4 — two-sided | PASS — every rule has its permitted case beside its reported case; the doc exclusion has the cref case beside the code case and the preprocessor case |
| 5 — pointed-integration | N/A this round — nothing here turns on real OS or filesystem behavior; the two cross-OS-ish criteria (16, 17) are msbuild project-selection reads assigned to final verification |
| 6 — every assertion can fail | PASS — demonstrated by mutation for every new test file and for the reused documentation and malformed-attribute tests |
| 7 — derived from the criterion | PASS — message expectations are spelled in the test layer (`SharedAnalyzerSources.CallSiteFailureFragment`, `ReferencedTypeFragment`, `NotTheDoorFragment`, `EngineInternalsOneDoorAnalyzerTests.ExpectedMessage`), never read back off the analyzer |
| 8 — never weaken, skip, delete | PASS — proved by the per-class case table above, `Skipped: 0`, and the absence of any skip/disable construct |
| 9 — one behavior per test | PASS |

## Observations (non-blocking)

1. **Acceptance 25's wrong-namespace proof sits in the callee position, not the call-site position.** No fixture in
   AG0040/AG0023/AG0029 puts the call site inside a same-named `SystemServices.Create()` declared in another
   namespace of the Engine compilation; the "another Engine class" fixtures vary the type NAME, not the namespace.
   I ruled the criterion met because the namespace conjunct lives in one predicate
   (`CompositionPoint.IsContainerFactoryType`) shared by the caller test in `OneDoorRule` and AG0041's exception,
   and M7 proves an access analyzer catches a namespace-blind version of it. If Tim wants the literal call-site
   decoy, it is one fixture in `SharedAnalyzerSources` plus one test per one-door class.
2. **Source-location assertions are thin outside `DeclaredTypeScannerTests`.** Acceptance 25 says "Tests assert the
   intended diagnostics and source locations". Only the new `DeclaredTypeScannerTests` asserts spans; the AG0041
   and one-door tests assert exact counts and exact message text, which is what Acceptance 11 names for AG0041.
   Residual risk, stated precisely: a row expecting two identical messages (a field, a property, a return, a
   parameter, a local) cannot tell which lens produced each. The realistic single-fault regression — a lens going
   silent — is caught by count, which M11 demonstrates on 25 cases.
3. **Rule identifiers are read from the analyzer, not pinned as literals.** `RuleId =
   OneDoorIntoCrossPlatformAnalyzer.DiagnosticId` and its siblings mean a renamed diagnostic ID would not fail
   these tests, where HEAD's deleted `BoundariesToCrossPlatformOneDoorAnalyzerTests` asserted the literal
   `"AG0023"`. This is RULE-PHASE's deliberate DRY decision, recorded in `SharedAnalyzerSources`, and Tim accepted
   RULE-PHASE; the AG0040 and AG0041 rows in `AnalyzerReleases.Unshipped.md` still pin the identifiers at build
   time. Not this round's doing and not an Acceptance breach — recorded so it is a decision on the record rather
   than an accident.
4. **One count error in the author's own prose.** `tdd-result.md`, "Changed files": "`SharedAnalyzerSources.cs` —
   five additions and nothing removed or altered" is followed by eight names. The substance (nothing removed or
   altered) holds; the number does not. Reporting accuracy, not test quality.

## What I tried to refute, and how

- **That a green suite hides a deleted or weakened case.** Refuted by reconstructing RULE-PHASE's per-class counts
  from `rule-phase-fix5-output.json` and closing the arithmetic to 616 exactly, plus `Skipped: 0` and a grep sweep
  for skip/disable constructs.
- **That the new tests pass because they assert nothing.** Refuted by re-running all six author mutations and
  adding four of my own (M7, M9, M11, M12), each targeted at a different claim.
- **That the Acceptance 24 strengthening was gratuitous, or a disguised weakening.** Refuted both ways: I ran the
  OLD assertion against the M5 fixture and watched it pass while proving nothing, and I confirmed the new node
  assertion strictly implies the old text assertion.
- **That the uncovered `SymbolResolution.Symbol` guard is an excuse.** Refuted by reading
  `AnalyzerRunner.CompileAsync`/`RunWithReferenceAsync`: an error type requires a compilation error, and declaring
  one is exactly the exception Acceptance 18 and the round's instruction forbid.
- **That Acceptance 25's namespace clause is unproven.** Refuted by M7, which shows an access analyzer test fails
  when the identity goes namespace-blind.
- **That the reused malformed-attribute tests are a bookkeeping dodge.** Refuted by M12, which shows they fail when
  the recovery tier is removed.
- **That the runner or an unrelated fixture was edited to make something pass.** Refuted by `git diff --stat HEAD`
  on both runner files, `git status --short -- src tests`, and the digest check on all ten delivered files.

## Commands I ran

    shasum -a 256 <the ten delivered files>                       # before and after; matches tdd-digests.txt
    git status --short ; git status --short -- src tests ; git diff --check
    git diff --stat HEAD -- analyzers/.../AnalyzerRunner.cs analyzers/.../AnalyzerRunnerTests.cs
    git show HEAD:analyzers/.../BoundariesToCrossPlatformOneDoorAnalyzerTests.cs   # criterion 7 byte-for-byte check
    rsync -a --no-specials --no-devices --exclude .git --exclude bin --exclude obj --exclude .dev <repo> <review-copy>
    diff -r --brief <repo>/analyzers <review-copy>/analyzers -x bin -x obj        # before and after mutations
    dotnet test analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj          # 616/616 in the copy
    <ten mutations, each applied in the copy, run with --filter, and reverted>
