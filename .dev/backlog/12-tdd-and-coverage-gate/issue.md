# 12 — Force TDD, make code coverage a zero-effort standard, and gate at ≥75%

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/12

---

## Why
The config-protection core has four classes with zero unit tests — `SnapshotDiffer`, `RegionDiffer`, `RegionVerifier`, `RegionRestore` — and their drift behavior is proven only through the full-pipeline acceptance tests, never in isolation. Code coverage is not measured at all: `coverlet.collector` is referenced in `tests/AgentGuard.Tests/AgentGuard.Tests.csproj` but nothing runs it (no CI step, no `.runsettings`, no threshold, no script). That discipline gap is what let those classes ship untested.

## What to do

### A. Force TDD in the process
Update `rails-run-a-workflow` so development is test-first: the failing unit test that defines a component's spec is written before the component, the same rules-first discipline RDD already applies to analyzers. No component with real behavior advances without its own isolated spec test, and a component that has none is a gate failure, not just a smell.

### B. Code coverage as a zero-effort standard build option
Wire coverage so a normal `dotnet test` produces it with no extra flags — a checked-in `.runsettings` enabling the already-referenced coverlet collector, plus a one-command coverage report. Every test run measures coverage automatically; nobody has to remember a flag.

### C. Gate coverage at ≥75%
Enforce a minimum line coverage of 75%: the build/CI fails below it. Existing gaps (the four classes above, and anything else uncovered) are burned down to reach it.