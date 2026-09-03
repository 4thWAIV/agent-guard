---
name: rails-test-code
description: The TEST guardrail and the TDD phase's PRIMARY PASS/FAIL checklist — what makes an acceptance test real, complete, and RED-first. Load it when writing the acceptance tests, when implementing against them, and when reviewing the tests.
---

# Rails: test code

A rail against tests that look like coverage while proving nothing — a criterion left untested, a test green before any code exists, an assertion that cannot fail, a mock checked instead of the real thing. Test QUALITY and completeness only; duplication across tests is the DRY rail's job, not this one. One source, three readers:

- **Test author** (TDD stage) — follow every **Practice** while writing the acceptance tests; create no **Violation**.
- **Implementer** (IMPLEMENT stage) — turn every RED test green by GROWING THE CODE; never weaken, skip, or re-point a test to reach green.
- **Test-quality adversary** — use every **Violation** as the PRIMARY checklist for the test files and confirm each by re-running `dotnet test`.

The tests are written against the ARCHITECTURE surface. Every acceptance test MUST be RED right now — it FAILS, either because a NEW body throws `NotImplementedException`, or because EXISTING behavior being expanded returns the wrong result for the inputs the change requires (a plain expected-vs-actual failure).

## The rails

### 1. Every acceptance criterion is covered
- **Practice:** Every criterion in the contract's Acceptance section has at least one test that would prove it. Map each test back to the criterion it covers.
- **Violation:** An acceptance criterion has no covering test — the easy-half shortcut, coverage that stops at the criteria that were simple to test.
- **Fix:** Write the missing test for the uncovered criterion, derived from the criterion's own words.

### 2. RED before green
- **Practice:** Every acceptance test fails now, for the reason the criterion describes — for new code because the body throws `NotImplementedException`, for an expansion because today's behavior is wrong for the required inputs.
- **Violation:** A test passes GREEN before the change is implemented — it exercises nothing new (it asserts a constant, a mock, the skeleton's own thrown exception as if that were success, or, for an expansion, the CURRENT wrong behavior), so it can never catch whether the change was made.
- **Fix:** Rewrite it to assert the REQUIRED behavior the criterion names, so it fails today and passes only once the behavior is built or corrected.

### 3. Assert the real type, not a fake of it
- **Practice:** Unit tests use fakes for outside-world dependencies. The system under test is the real production type. Fakes stand in only for its outside-world dependencies, never for the thing being proven.
- **Violation:** The test configures a fake and then asserts that same fake — it verifies the test's own setup, not the code. Or it substitutes a fake for the production type whose behavior the criterion is about.
- **Fix:** Drive the real type; assert its observable result. Fake only its outside-world dependencies.

### 4. Two-sided where the criterion has two sides
- **Practice:** When a criterion has a pass case and a fail/deny/error case, test BOTH.
- **Violation:** Only the happy path is tested (or only the error path).
- **Fix:** Add the missing side — the rejection for a rule, the failure for a guard, the boundary for a range.

### 5. Real OS or boundary behavior gets a pointed-integration test
- **Practice:** Pointed-integration tests exercise the real OS adapter. Anything whose truth depends on the actual OS, filesystem, or native boundary is proven by an on-disk / real-OS pointed-integration test, run on the per-OS CI legs — not only a unit test over a fake.
- **Violation:** A criterion about real per-OS behavior (a native call's result, a real file operation, an actual presence check) is "covered" only by a unit test against a fake, so the real OS adapter is never exercised.
- **Fix:** Add the pointed-integration test that exercises the real OS adapter on disk / on the OS; keep the unit test with fakes for the logic around it.

### 6. Every assertion can fail
- **Practice:** Each test has an assertion that is false when the behavior is wrong.
- **Violation:** A tautological or trivially-passing assertion — `Assert.True(true)`, asserting a literal you just wrote, `Assert.NotNull` on a freshly-constructed object, or an empty test body — that passes regardless of the code.
- **Fix:** Assert the specific observable the criterion names, with the exact expected value.

### 7. Derived from the criterion, not from the implementation
- **Practice:** The test encodes what the contract REQUIRES. Write it from the criterion's words, before/independent of how the skeleton happens to be shaped.
- **Violation:** The test is written to match a convenient implementation or the skeleton's current internals, so it will pass whatever the code does rather than pinning the required behavior.
- **Fix:** Rewrite the expectation to the criterion as stated; grow the code to meet it, never the reverse.

### 8. Never weaken, skip, or delete a test to pass
- **Practice:** A red test is made green by growing the code. A pinned oracle — a deliberately-fixed baseline value (a golden output, a snapshot, a recorded expected value), not an ordinary assertion — changes only when its correct value genuinely changed and the human gives explicit sign-off. A pinned test that fails has exactly two legal moves: fix the code, or repin the oracle WITH ruling provenance.
- **Violation:** A test is skipped (`[Fact(Skip)]`), `xfail`'d, commented out, deleted, or its assertion loosened/re-pointed to make a red go green. This is a top-line finding, ranked with a smuggled decision. Silent weakening is the Lie-catcher's #1 hunt — it diffs test files specifically.
- **Fix:** Restore the full test and fix the code. Repin an oracle only with the human's explicit yes.

### 9. One behavior per test
- **Practice:** Each test pins one behavior, so a failure names exactly what broke.
- **Violation:** A grab-bag test asserts many unrelated behaviors.
- **Fix:** Split it into focused tests, one behavior each.

## Verdict

Return PASS only when no Violation is confirmed. Return FAIL when any Violation is confirmed.
