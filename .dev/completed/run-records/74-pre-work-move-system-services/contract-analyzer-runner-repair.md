# Repair the analyzer test fixtures and pin their compiler errors

## Decisions

### Approved

Tim approved this repair task, presented as written:

- Execute the analyzer-runner and fixture repairs as a bounded L2 task, with independent Lie-Catcher, Prove-It, and DRY reviews. This approval includes the expected-compiler-error interface and shared test-infrastructure changes described below.
- Record the repair scope in a short contract in the current work folder before implementation. Keep it separate from executing the SystemServices relocation.
- Preserve and review the existing uncommitted runner changes and new runner tests. Capture the starting working-tree status and diff so unrelated work remains distinguishable.
- Repair the 14 accidentally invalid fixtures using the proposed repairs. Preserve each test's intended analyzer scenario and existing analyzer assertions.
- Add explicit expected compiler errors for the three deliberately invalid attribute fixtures. Default to no compiler errors. Compare actual and expected error IDs with occurrence counts; fail on missing or additional errors. Continue checking analyzer diagnostics independently. Do not introduce a skip-validation path.
- Add runner regression tests covering matching expectations, missing expected errors, unexpected errors, and unexpected duplicate errors. Preserve validation of referenced compilations.
- Correct the runner comments and failure wording: compiler errors do not universally prevent analyzer diagnostics, and deliberately invalid fixtures can produce meaningful analyzer results.
- Record the existing failing baseline rather than disabling the compiler check to manufacture a green opening run. Treat those documented failures as the explicit exception for this repair task. Stop on unrelated failures.
- Run the analyzer suite, then `make build` and `make test`. Retain the exact commands and results. Run independent Lie-Catcher, Prove-It, and DRY reviews against the repair contract and final working tree. Resolve findings within scope and rerun the affected checks.
- Do not start the SystemServices relocation, implement its new rules, modify unrelated files or memory, stage, commit, push, or open a PR. Ask before exceeding the approved scope or beginning work estimated over ten minutes.
- Return the changed-file list, test results, review findings and resolutions, and any remaining failures. Leave the changes uncommitted for Codex's review.

Tim approved the SourceLink change, presented as written:

- Retain SourceLink and replace the explicit `Microsoft.SourceLink.GitHub` 8.0.0 package reference with the implementation bundled in our pinned .NET SDK.
- Before changing the references, confirm that SDK 10.0.400 contains a SourceLink implementation patched for the reported vulnerability. Report its actual version and supporting evidence. If it is affected, stop and propose the required SDK change.
- Remove only the explicit SourceLink `PackageReference` and its central `PackageVersion`. Keep the SDK pin and roll-forward policy unchanged. Do not suppress the advisory or disable NuGet auditing.
- Verify that restore, `make build`, and `make test` pass with auditing enabled, and that the generated debugging symbols still contain the expected SourceLink mapping.
- Then complete the pending analyzer-repair L2 reviews: Lie-Catcher, Prove-It, and DRY. Leave all changes uncommitted for Codex's review. Do not begin the SystemServices relocation.

Tim approved the final cleanup and the review conditions, presented as written:

- A delegated worker may replace the repeated message-prefix expression in `AnalyzerRunner.Describe` with one local variable, preserving the resulting messages exactly. No other code cleanup is authorized.
- `refute.js` is unchanged. The persistent workflow change is recorded separately in the isolated-mutation issue. Do not modify workflow files. Give this run's reviewers the isolation instructions directly.
- Run restore, build, and tests with auditing enabled. If anything fails, report the failure and stop. Do not expand the repairs.
- Run exactly one REFUTE round with independent Prove-It, Lie-Catcher, and DRY reviewers against the final tree. Keep the shared checkout unchanged throughout review. Each reviewer must identify the snapshot reviewed. Mutation tests must run in separate isolated copies containing that snapshot, including the uncommitted and untracked changes.
- Stop after REFUTE regardless of verdict. No automatic IMPLEMENT, additional fixes, retries to seek a passing verdict, or further review rounds. Report any finding and proposed correction for approval.
- Save command output, reviewer results, snapshot identities, and the final changed-file list in the current work folder.

The confirmation the change was gated on is recorded here: advisory GHSA-23fw-v26w-5fgq (CVE-2026-62900, moderate) lists the affected `Microsoft.Build.Tasks.Git` ranges as `= 8.0.0`, `>= 10.0.102, <= 10.0.110`, `>= 10.0.200, <= 10.0.204`, and `>= 10.0.300, <= 10.0.301`. SDK 10.0.400 bundles `Microsoft.Build.Tasks.Git` 10.0.400, established from `/usr/local/share/dotnet/sdk/10.0.400/Sdks/Microsoft.Build.Tasks.Git/tools/net/Microsoft.Build.Tasks.Git.deps.json`, whose `libraries` map holds two entries, `Microsoft.Build.Tasks.Git/10.0.400` and its dependency `System.IO.Hashing/10.0.11`. The SourceLink version that matters is therefore 10.0.400, which falls in none of the affected ranges, so the SDK implementation is not affected and no SDK change is required.

## Rules to add

None. This task adds no analyzer rule.

## The standard / what we're building

`AnalyzerRunner` validates every compilation it builds against the compiler errors that compilation is expected to produce. The expectation defaults to none. A call site that needs a deliberately invalid fixture declares the exact error identifiers and how many of each it expects; the runner fails when an expected error is missing, when an unexpected error appears, and when an expected error appears a different number of times. There is no path that skips validation. Analyzer diagnostics are still collected and returned for every call, including one whose fixture carries expected compiler errors.

Every analyzer test in `analyzers/AgentGuard.Analyzers.Tests` passes. The fourteen fixtures that failed only because they were incomplete compile, and each keeps the analyzer scenario and the assertions it had. The three fixtures that are invalid on purpose stay invalid and declare what the compiler must say about them.

## Success definition

ALL criteria met AND no errors in the system as a result of the change.

The analyzer suite is green, `make build` and `make test` are green, no analyzer assertion is weakened or removed, and no fixture is exempted from compiler-error validation.

## Surfaces

- `analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs` — the expected-compiler-error parameter on every public entry point, including `CompileAsync`, the comparison by identifier and occurrence count, and the corrected comments and failure wording.
- `analyzers/AgentGuard.Analyzers.Tests/SharedAnalyzerSources.cs` — the documentation comment on `PresenceContract`, which states that the AG0107 fixtures omit `Check` and that the incomplete implementation is irrelevant. The approved decision to correct the comments and failure wording reaches it; the earlier Surfaces list missed it.
- `analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunnerTests.cs` — regression tests for a matching expectation, a missing expected error, an unexpected error, an unexpected duplicate, and a referenced compilation that does not compile.
- `analyzers/AgentGuard.Analyzers.Tests/ContainerInterfaceSingleOwnerAnalyzerTests.cs` — four fixtures, CS0535.
- `analyzers/AgentGuard.Analyzers.Tests/NoTimeoutInPresenceImplAnalyzerTests.cs` — five fixtures, CS0535, and the class comment that states those fixtures omit `Check`.
- `analyzers/AgentGuard.Analyzers.Tests/LeafPlatformSingleImplementerAnalyzerTests.cs` — one fixture, CS0535.
- `analyzers/AgentGuard.Analyzers.Tests/BuilderCompletenessAnalyzerTests.cs` — one fixture, CS0246.
- `analyzers/AgentGuard.Analyzers.Tests/InteropOnlyInCrossPlatformLibrariesAnalyzerTests.cs` — two fixtures, CS8795; one deliberately invalid fixture, CS0246; one deliberately invalid fixture, CS7036.
- `analyzers/AgentGuard.Analyzers.Tests/NoControlFlowInNativeOpsAnalyzerTests.cs` — one fixture, CS8518.
- `analyzers/AgentGuard.Analyzers.Tests/NativeCallbackBodyMustBeGuardedAnalyzerTests.cs` — one deliberately invalid fixture, CS7036.
- `Directory.Build.props` — remove the `Microsoft.SourceLink.GitHub` `PackageReference` and the now-empty `Source link` item group.
- `Directory.Packages.props` — remove the `Microsoft.SourceLink.GitHub` `PackageVersion`.
- `.dev/inprocess/74-pre-work-move-system-services/repair-baseline-status.txt`, `repair-baseline-diff.patch`, and `repair-baseline-analyzer-tests.txt` — the recorded starting state.

## Reuse ledger

The expected-compiler-error comparison is one new capability: comparing an actual diagnostic set to a declared set by identifier and occurrence count.

- `reuse analyzers/AgentGuard.Analyzers.Tests/AnalyzerRunner.cs` — the existing `ThrowOnCompilerErrors` is the one place compiler errors are inspected. Extend it rather than adding a second inspection point.
- `new` — no comparison of a diagnostic set to a declared expectation exists; `grep -rn "expectedCompilerErrors\|ExpectedDiagnostics\|GetDiagnostics()" analyzers/AgentGuard.Analyzers.Tests/*.cs` returns only the runner's own call.

## What to do

Steps 1 to 5 are ALREADY DONE and present as uncommitted changes in the working tree. The orchestrator made them directly before Tim ruled that the remainder runs as a delegated L2 task. Do not redo them and do not revert them. Read them against this contract, and report any place they fall short instead of silently rewriting them.

1. DONE — the starting working-tree status, the starting diff, and the baseline analyzer-suite result are recorded in this folder as `repair-baseline-status.txt`, `repair-baseline-diff.patch`, and `repair-baseline-analyzer-tests.txt`.
2. DONE — every `AnalyzerRunner` entry point takes an optional expected-compiler-error argument defaulting to none, and actual errors are compared to it by identifier and occurrence count.
3. DONE — the runner's comments and failure text state that an unexpected compiler error makes a fixture's analyzer result untrustworthy, without claiming compiler errors prevent analyzer diagnostics.
4. DONE — the fourteen incomplete fixtures are repaired: interface implementations completed for CS0535, missing interfaces declared for CS0246, the source-generated implementation half supplied for CS8795, and the `and` pattern given operands that can match for CS8518. No analyzer assertion changed.
5. DONE — the three deliberately invalid fixtures declare their expected compiler errors. No analyzer assertion changed.
6. REMAINING — add the runner regression tests for a matching expectation, a missing expected error, an unexpected error, an unexpected duplicate, and a referenced compilation that does not compile. `AnalyzerRunnerTests.cs` already covers a referenced compilation that does not compile and several clean cases; add what is missing rather than duplicating what is there.
7. REMAINING — run the analyzer suite, then `make build` and `make test`, retaining exact commands and output.
8. REMAINING — remove the `Microsoft.SourceLink.GitHub` `PackageReference` from `Directory.Build.props` and its `PackageVersion` from `Directory.Packages.props`, changing nothing else about the SDK pin, the roll-forward policy, or the audit settings.
9. REMAINING — verify `dotnet restore`, `make build`, and `make test` all pass with NuGet auditing enabled and no command-line override, and that the built debugging symbols still carry the SourceLink source mapping.
10. ORCHESTRATOR — independent Lie-Catcher, Prove-It, and DRY reviews run against this contract and the final working tree after the work above is complete. The implementing worker does not run them and does not review its own result.

## What the agent MAY do

Change the analyzer test files listed in Surfaces. Add fixture members and type declarations needed to make an incomplete fixture compile.

## What the agent MUST NOT do

Do not start the SystemServices relocation or implement its rules. Do not change any file outside Surfaces, any production source, any analyzer implementation, or any memory. Do not weaken, delete, or skip an analyzer assertion. Do not add a path that skips compiler-error validation, and do not exempt a fixture from it. Do not disable the compiler check to produce a green opening run. Do not suppress advisory GHSA-23fw-v26w-5fgq, disable NuGet auditing, change the SDK pin or its roll-forward policy, or pass a `NuGetAudit` override on any command whose output is offered as acceptance evidence. Do not stage, commit, push, or open a PR. Stop and report any failure unrelated to the recorded baseline.

## Acceptance

1. The starting working-tree status, diff, and baseline analyzer-suite output are recorded in this folder, and the baseline records exactly the seventeen failures this task is authorized to fix.
2. `dotnet test analyzers/AgentGuard.Analyzers.Tests/AgentGuard.Analyzers.Tests.csproj` passes with zero failures, and the total test count is no lower than the baseline's 434.
3. `make build` and `make test` pass. Retain exact commands and output.
4a. Deleting `ThrowOnUnexpectedCompilerErrors(referenceCompilation, ...)` from `RunWithReferenceAsync` makes at least one test fail. The test that covers a referenced compilation which does not compile asserts something only the reference check can produce, not text the subject check also emits.
4b. No comment anywhere in `analyzers/AgentGuard.Analyzers.Tests` states that a fixture omits a required interface member, or that the runner returns only analyzer diagnostics and tolerates a compile error. `grep -rn "compile error is irrelevant\|without supplying" analyzers/AgentGuard.Analyzers.Tests/*.cs` returns nothing.
4. The runner's regression tests prove all five cases: an expectation that matches the actual errors passes; a declared error the compilation does not produce fails; an error the compilation produces that was not declared fails; an expected error produced more times than declared fails; and a referenced compilation that does not compile fails.
5. Exactly three analyzer-fixture tests declare expected compiler errors, and they are `UnresolvedInteropAttribute_IsReportedViaSyntacticFallback`, `MalformedSameNamedDllImportAttribute_WithRequiredArgOmitted_IsNotReported`, and `MalformedUserDefinedUnmanagedCallersOnly_WithRequiredArgOmitted_IsNotReported`. The argument is declared `params string[]`, so a call site passes the identifiers positionally and never spells the parameter name; establish the count from the error identifiers the call sites pass. Run `grep -rnE '"CS[0-9]{4}"' analyzers/AgentGuard.Analyzers.Tests/*.cs`: outside `AnalyzerRunner.cs` it returns those three call sites and, additionally, only `AnalyzerRunnerTests.cs`, where the runner's own behavior is what is under test.
6. `git diff` over the fourteen repaired fixtures shows no changed `Assert` line and no removed test.
7. `git status --short` differs from the recorded baseline only by the files listed in Surfaces.
8a. `AnalyzerRunner.Describe` builds its shared message prefix once into a local, and the three message texts it returns are byte-identical to what the three-branch version returned.
8. `dotnet restore` exits zero with NuGet auditing enabled and no `NuGetAudit` override on the command line, and reports no NU1902.
9. `grep -rn "Microsoft.SourceLink" Directory.Build.props Directory.Packages.props` returns nothing, and `git diff Directory.Build.props Directory.Packages.props` shows only those two removals.
10. `global.json` is unchanged, and `git diff` shows no change to any `NuGetAudit`, `NuGetAuditMode`, `NuGetAuditSuppress`, or `TreatWarningsAsErrors` setting.
11. The debugging symbols for a built assembly still carry the SourceLink source mapping. Establish it from the built PDB rather than from the build log.
12. Independent Lie-Catcher, Prove-It, and DRY reviews return their verdicts against this contract and the final tree, and every accepted finding is resolved with its affected check rerun.

## Level

L2, as Tim approved for this bounded repair, with the three independent reviews retained.

## Scope

Scope changes ONLY by the human editing the contract. There is no second path.
