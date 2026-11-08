# L1 REFUTE — verdicts and the adjudication of every finding

Panel run against the corrected contract, the live working tree, and the retained execution evidence. Complete output: `refute-output.json`. No fix was applied; Tim stopped the run after the review results.

| lens | verdict | findings |
| --- | --- | --- |
| Prove-It | FAIL | 2 |
| SOLID | PASS | 0 |
| DRY | FAIL | 2 |
| Laziness-auditor | FAIL | 1 |
| Lie-catcher | PASS | 0 |

`anyFail: true`, `panelComplete: true`, `failedRoles: []`. Every dispatched reviewer returned a valid result.

Each finding below was checked against the live tree by the orchestrator before being carried here. Prove-It's second finding and DRY's second finding are the same defect reported by two lenses, and are adjudicated once.

## Confirmed — three shared test helpers have no prior-art-ledger row

Raised by Prove-It and by DRY. `rails-dry-code` rail 1 requires every new capability, a one-off helper included, to carry a prior-art-ledger ruling in the contract's Reuse ledger.

`grep -n 'AssertSameStrings\|AssertSpans\|WrongContainerNamespace' contract.md` returns nothing, so none of the three has a row. All three are real shared owners with consumers in other files:

- `SharedAnalyzerSources.AssertSameStrings` — defined at `SharedAnalyzerSources.cs:998`, called from `SharedAnalyzerSources.AssertSpans` at `:978` and from `EngineInternalsOneDoorAnalyzerTests.cs:306`.
- `SharedAnalyzerSources.AssertSpans` — defined at `SharedAnalyzerSources.cs:975`, called from `DeclaredTypeScannerTests.cs:74`, `EngineToBoundariesOneDoorAnalyzerTests.cs:147`, `OneDoorIntoCrossPlatformAnalyzerTests.cs:278` and `OneDoorIntoPerOsAnalyzerTests.cs:287`.
- `SharedAnalyzerSources.WrongContainerNamespace` — defined at `SharedAnalyzerSources.cs:146`, consumed at `SharedAnalyzerSources.cs:115`, `:583` and `EngineInternalsOneDoorAnalyzerTests.cs:44`.

`dry-fix-review-dry.md` raised `AssertSameStrings` and recorded that "The same conflict was raised by the two earlier DRY reviews of this work, referred to Tim as a process question, and is still unanswered." This is the third round it has stood open. The two analogous omissions readiness found — `TypeTree.cs` and `CrossPlatformBoundary.cs` — were closed by adding Surfaces entries and ledger rows; these three were not.

## Confirmed — three of the four permitted factory calls are re-spelled instead of routed to a constant

Raised by DRY. `EngineToBoundariesOneDoorAnalyzerTests.cs:91` holds `PermittedFactoryCall = "EnvironmentAdapter.Create()"` as the one owner of that call, with the comment "Held once because the compliant theory, the wrong-site test, the wrong-namespace caller test and the wrong-namespace factory test all make this same call and must make the same one." Both sibling files hold the same pattern as a triple — `OneDoorIntoCrossPlatformAnalyzerTests.cs:86-90` and `OneDoorIntoPerOsAnalyzerTests.cs:60-64` each declare `DoorType`, then `DoorMember = DoorType + ".Create"`, then `DoorCall = DoorMember + "()"`.

The other three factory calls have no owner and are spelled as raw literals: `"ConsoleAdapter.Create()"` at lines 105, 158, 245; `"Ed25519SignatureService.Create()"` at 106 and 186; `"BuildInfoReader.Create()"` at 107 and 172; and `"ConsoleAdapter.Create"` at 253.

Fix named by the reviewer: give each of the three the same type / member / call constant triple `EnvironmentAdapter` already has, and route those call sites through them — the call constants at the `Inside…` sites and the member constants at the `CalledMemberAccusation` sites.

## Confirmed — `tdd-result.md` cites a file as proof of a check that file never ran

Raised by the Laziness-auditor. `tdd-result.md:153`, the Acceptance 19 coverage row, reads: "`git diff --check` EXIT=0 in `tdd-acceptance25-scope-and-digests.txt`, `tdd-correction-diff-check.txt` and `dry-fix-scope-and-digests.txt`."

`grep -c 'diff --check' dry-fix-scope-and-digests.txt` returns 0 and `grep -c 'EXIT=' dry-fix-scope-and-digests.txt` returns 0. That file contains no `git diff --check` invocation and no exit code at all, so one of the three cited corroborations does not exist.

`git diff --check` does pass on the current tree, EXIT=0, so the claim itself is true; the citation backing it is not.

## Confirmed as fact, adjudicated as a provenance gap — Acceptance 17's edit has no record in Decisions

Raised by Prove-It. `grep -n 'Acceptance 17\|getItem\|ProjectReference' contract.md` matches only the Acceptance 16 and 17 lines themselves; no Decisions entry records the rewrite, while roughly twenty other substantive changes in that section each carry one.

The reviewer confirmed the new wording is true and stronger than the old: it re-ran `dotnet msbuild src/AgentGuard.Boundaries/AgentGuard.Boundaries.csproj -getItem:ProjectReference` itself and got exactly the two entries the new text names, and observed that the previous "and nothing else" wording was never satisfiable, because `Directory.Build.props` injects the analyzer reference into every project in the repo.

Orchestrator's adjudication: the change is not a weakened requirement and not an agent's edit. Tim rewrote the acceptance check himself, which `rails-run-a-workflow` names as the only path by which scope changes — "Scope changes ONLY by the human editing the contract. There is no second path." What the contract lacks is a line saying so, in the same form as the "Approval provenance" note already in the Recorded conversation authorizations section. Recorded here rather than treated as settled; the wording is Tim's to give.
