# RULE-PHASE fix direction — round five

Tim authorized one correction-and-review round. RULE-PHASE remains rejected until the message assertions satisfy the contract.

## The defect

Acceptance 5 requires each one-door test to assert that the message names **every condition that failed** — and only those. The check is a pair: the fragment that must appear, and the fragment that must not. Several sites assert only the present half, so they pass whether or not the message also names a condition that did not fail. The pair is hand-copied at eight sites across three test files with no shared owner, and the copies drifted.

Confirmed one-sided sites: `EngineToBoundariesOneDoorAnalyzerTests.cs` at 120, 131 and 142; `OneDoorIntoPerOsAnalyzerTests.cs` at 209, 239, 267 and 284. Complete pairs already exist two lines away at `EngineToBoundariesOneDoorAnalyzerTests.cs:108-109` and `:154-155`, and at `OneDoorIntoPerOsAnalyzerTests.cs:192-196`. The previous round's self-audit found one of these and reported the work complete.

## Fix the complete assertion pattern

In Tim's wording.

- Inspect all message assertions in the AG0040, AG0023 and AG0029 test classes — not only the reported locations.
- For each case, prove the diagnostic describes exactly the failed conditions:
  - Wrong caller, permitted factory: caller failure present; factory failure absent.
  - Permitted caller, wrong factory: factory failure present; caller failure absent.
  - Both wrong: both failures present.
- Use one shared assertion owner, following the existing test-helper structure and referencing the existing message-fragment constants. Route every applicable assertion through it. Preserve diagnostic-ID and count checks.
- Prove the helper rejects messages containing an incorrect extra accusation as well as messages missing a required explanation. Run any mutation proof in an isolated copy.

`SharedAnalyzerSources` already holds `CallSiteFailureFragment` and its siblings, and `EngineToBoundariesOneDoorAnalyzerTests.cs:108-109` already writes the complete paired form using them. Build the owner from those, not from new constants.

The helper's own proof matters as much as the routing: a helper that only checks presence would let every one of these sites stay half-asserted while looking routed. Prove BOTH directions — a message missing a required explanation is rejected, AND a message carrying an accusation that did not fail is rejected.

## Remove the remaining duplicated constant

Give `using AgentGuard.CrossPlatform;` one shared owner and update both consumers.

## Verify the whole correction

In Tim's wording.

- Review the complete diff for duplicated or incomplete assertion logic.
- Preserve all independently reported test cases, the working wrong-assembly rejection, the compilable fixtures and existing Boundaries behavior.
- Run the affected tests and full analyzer suite. Map the message requirements to their actual assertions before reporting completion.
- **A passing suite or reviewer verdict does not override an unmet requirement.**
- Retain the expected AG0015 production failure as intermediate impact; the class move remains IMPLEMENT work.

Report the per-case counts from the actual test run, so the independently reported cases are observed rather than asserted. The round-opening baseline is 600 passing in the analyzer suite, 81 of them in the AG0041 class.

## Preserve what already works

AG0029 rejects a wrong-assembly imitation of its own `PlatformServices` door through `IsPerOsTypeFromEngine` and `CrossPlatformBoundary.IsSharedNamespaceDecoy`, and its test asserts AG0029's own rule id. Overlapping diagnostics between AG0023 and AG0029 are permitted. The six permitted-public reaches are twelve independently reported cases across the two consumer assemblies, through one shared helper where positive rows carry an empty expected-message collection. The 52 negative cases report independently. The base-list fixture compiles with no expected compiler error. The AG0041 message assertions are in place. Do not undo any of it, and do not merge independently reported scenarios to reach a lower duplication count.

## Out of scope

Do not move `SystemServices`. The one expected AG0015 production diagnostic is recorded in `rule-phase-production-impact.md`. Do not clean it, suppress it, exempt it, lower a severity, or edit production code to hide it. Report any NEW production diagnostic as a finding.

Do not write the direct helper tests, the pointer-traversal test, or the direct caller-identity test — those are assigned to TDD in `remaining-work.md`.

Do not change `AnalyzerRunner.cs` or `AnalyzerRunnerTests.cs`. Do not change AG0006's diagnostics or its tests. Do not stage, commit, push, or open a pull request. Run any mutation test in an isolated copy, never in the shared working tree.

## Report order

Report any remaining unmet requirement first, naming the requirement and the observed result.
