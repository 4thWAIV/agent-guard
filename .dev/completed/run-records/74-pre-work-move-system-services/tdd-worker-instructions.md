# TDD worker instructions — SystemServices relocation

## Dispatch and authority

Claude forwards this file unchanged to one fresh TDD author and to the independent test-quality, DRY and Lie-catcher reviewers. Claude is the dispatcher and monitor, not an additional author. This file is the complete supplemental task; Claude adds no replacement brief, inferred requirements or scope exceptions.

Use the existing L1 TDD workflow and its role boundaries. This authoring and review round ends with a report for Tim. A failed result is a failure, not authority for another fix round. Implementation remains a later stage.

The manager makes no repository edits, including edits to this file, the contract, test code or results. Delegate required evidence retention to the author or reviewer that produced the evidence. Monitor scope, required outputs and stop conditions. Report a deviation rather than silently repairing or approving it.

## Inputs and precedence

Read these files from this work folder:

- `contract.md`, including every Decision, Surface and Acceptance requirement.
- `architecture-result.md` and its actual referenced review records.
- `remaining-work.md`, as an index of unfinished tests, subordinate to the contract.
- The retained RULE-PHASE results and production-impact record, as evidence of existing work rather than replacement requirements.

Read the applicable repository rails, especially `rails-test-code`, `rails-dry-code`, `rails-solid-code`, `rails-decisions` and `rails-run-a-workflow`. Inspect the current tests and helpers before deciding that coverage is missing. Use current signatures, not interfaces proposed in historical DESIGN records.

The contract is the task authority. Older panel proposals do not reintroduce the removed analyzer-analysis framework. Copy a requirement verbatim when recording its acceptance mapping; do not substitute a summary for the requirement.

## Deliverable

Complete the remaining contract-required test coverage against the existing application surface and the real shared analyzer helpers. Reuse accepted RULE-PHASE tests as proof instead of rebuilding them.

Before editing, map the Acceptance section to existing tests, missing tests and checks assigned to IMPLEMENT or final verification. A later-stage assignment must identify the actual contract instruction assigning it there. Requirements governing a test written now apply now.

### Shared resolution

Add the missing focused tests in `analyzers/AgentGuard.Analyzers.Tests/SymbolResolutionTests.cs` using the real helper and existing compilation infrastructure. Cover the contract-required resolution behavior with meaningful expected symbols and types.

The contract states:

> The existing malformed-attribute tests prove that extracting resolution preserves recovery when symbol resolution fails but type information remains available; keep their inputs, assertions, and compiler-error expectations unchanged. Reuse these tests rather than introduce another malformed-fixture exception.

`remaining-work.md` mentions a malformed-attribute case. Fulfil that part with the existing tests named by the contract, not a new compiler-error allowance. Likewise, do not create an invalid unresolved-name fixture as a shortcut around the compiler-error requirement.

### Declaration scanning

Use the existing unchanged AG0006 tests for declaration behavior they already prove. Add only missing coverage for the real `DeclaredTypeScanner`, including the invoked-return-type behavior used by the access rules. Use the existing access analyzers and test runner to exercise registrations when appropriate; a fake scanner or a duplicate compilation framework proves nothing about the real scanner.

`DeclaredTypeScannerTests.cs` is a listed surface where separate tests are needed. The file's existence is not itself an acceptance requirement. If the real scanner's required behavior cannot be exercised within the approved test surfaces and interfaces, report the precise obstacle rather than adding a new test framework or editing analyzer signatures.

### Pointer traversal

Add `analyzers/AgentGuard.Analyzers.Tests/TypeTreeTests.cs` for the real `TypeTree.Any` traversal. Use compiler-created type symbols through the existing compilation helper. Check matching and nonmatching pointer targets and relevant nesting. Assert whether the intended leaf is reached; a non-null constructed pointer symbol is not proof of traversal.

The contract states:

> Direct tests of `TypeTree.Any` exercise pointer targets using compiler-created type symbols and the existing compilation helper. This proves traversal, not unsafe-source compilation. No runner change is needed.

### Caller identity and documentation

Add the direct wrong-assembly `CompositionPoint` check required by Acceptance 25 in a contract-listed test surface. Use the real composition predicate on the resolved method, paired with the correct-identity case. Reuse existing full-identity matching; do not reproduce it inside the test oracle.

Check existing `WrittenNameScannerTests.cs` against Acceptance 29. Reuse its direct documentation-context tests if complete; add only missing contract cases. Preserve independently reported generic-cref, operator-cref, application-code and preprocessor cases.

### Other acceptance requirements

Account for every remaining criterion, including wiring-test coverage, using the contract's exact requirements. Preserve accepted rule fixtures and the existing AG0006 assertions. The class move, project-reference changes, internal-access grant changes and production callers belong to IMPLEMENT, not this author.

## Test construction and preservation

- Use the existing `AnalyzerRunner`, fixture builders, diagnostic assertion helpers, constants and identity owners. The system under test is the real helper or analyzer.
- Preserve independently reported scenarios. Share setup and test data rather than bundle separate cases into one source fixture to avoid duplication.
- Keep positive and negative expectations explicit. Assert the required diagnostic identifier, count, source location and message content where the criterion requires them. Expectations come from the contract, not the implementation's own formatting method.
- Inspect all new and changed test code for repeated fixture construction, assertion logic and literals before submitting. Search beyond the first named occurrence and reuse the appropriate existing owner.
- Preserve the previously established twelve separate permitted-public cases and 52 separate negative cases. Read the actual test run for case accounting; suite totals alone are insufficient proof.

The contract states:

> New relocation fixtures compile under the default empty compiler-error expectation; no new expected-compiler-error exception is introduced.

`AnalyzerRunner.cs` and `AnalyzerRunnerTests.cs` remain unchanged. Changes to production code, analyzers, contracts, workflow scripts, grants and project configuration are outside this TDD author's task. Preserve unrelated working-tree changes. Staging, commits and pushes are not part of this round.

## Intermediate results and proof

Use the contract's recorded AG0015 warning allowance for the applicable TDD intermediate build. Keep the diagnostic visible. All other diagnostics retain normal enforcement. Final verification remains without the allowance.

Existing helpers were implemented during RULE-PHASE. Record their actual initial test results. Do not break working code, stub an existing body, invert an assertion, or manufacture compiler errors to obtain RED. If a reviewer considers a green regression test for that existing behavior incompatible with the TDD rail, report that exact conflict; neither the manager nor author may waive it or falsify RED evidence.

For tests of behavior still awaiting IMPLEMENT, record actual expected-versus-observed failure and its cause. Keep expected production AG0015 impact separate from acceptance-test failures. Tests deferred to later stages remain explicitly outstanding, never PASS.

Run the affected test classes and the full analyzer suite with actual output retained. Run the applicable TDD intermediate build and `git diff --check`. A test that discovers a defect in a helper reports the defect; the test author does not repair the helper or weaken the expected result.

Capture complete actual command output and immediately observed exit codes in this folder. Record the tested snapshot and changed paths. Save each result before referring to its file. Never reconstruct terminal transcripts, truncate reviewer results, or state a pending verdict as completed.

## Author return

Return changed files, actual commands and retained evidence paths, per-case results, and an acceptance mapping distinguishing existing proof, newly added proof, failure and explicitly assigned later-stage work. Map every compound requirement to all its required assertions. Avoid inventories of unchanged classes or claims that all files are complete.

Retain the complete structured author result using the existing workflow's format. Make no new reporting schema or extraction tool. If the aggregate workflow output fails, preserve the complete per-agent results that exist; check the journals before claiming any result is absent.

## Independent review and manager checkpoint

Reviewers receive this exact file, the contract, the author's actual diff and retained evidence. They independently check test quality, duplication, authority and completeness claims. They do not edit the shared checkout. Any destructive mutation runs only in an isolated copy containing the actual delivered uncommitted and untracked files.

Each reviewer saves its complete result before reporting its path. Reviewers report an unmet requirement as FAIL; a green suite or another reviewer's PASS does not supersede it.

Claude monitors that the author and reviewers follow their assigned roles, retain the required evidence, and keep the scope unchanged. On an unexpected scope need or contract conflict, stop and report the concrete issue. On completion, report failures first, then the TDD result and remaining IMPLEMENT work. Stop for Tim's review; do not start a correction loop or IMPLEMENT.
