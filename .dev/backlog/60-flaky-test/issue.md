# 60 — Flaky test in AgentGuard.Tests — one failure, not reproducible

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/60

---

## What happened

During the reorganize-rails GATE run, `make test` reported:

```
Failed!  - Failed:     1, Passed:   147, Skipped:     0, Total:   148, Duration: 391 ms - AgentGuard.Tests.dll (net10.0)
```

Every other assembly passed. The run did not name the failing test — `dotnet test` at default verbosity prints only the summary line, so there is no test name, no assertion message, and no stack trace to go on.

## Reproduction attempts — all clean

- `dotnet test tests/AgentGuard.Tests/AgentGuard.Tests.csproj` alone, 9 consecutive runs: 148/148 every time.
- Full `make test` across all four assemblies, 3 consecutive runs: 642 passed, 0 failed every time.

Eleven clean runs after the single failure. Not reproducible so far.

## Why it matters

A test that fails roughly one run in twelve will eventually fail a CI leg and block the gate for a reason nobody can diagnose from the log. The reorganize-rails change touched no product code or product tests — `git status --porcelain src/ tests/` is empty for that run — so this is pre-existing, not caused by that work.

## What to do

Raise test verbosity so a failure names itself. `dotnet test --logger "console;verbosity=detailed"` or a TRX logger in `make test` would have captured the test name and assertion, which is the whole difficulty here. Then run the suite in a loop until it reproduces and fix the actual cause.

Worth checking first: tests that depend on wall-clock timing, on filesystem temp paths, or on ordering between tests that share state — those are the usual sources of a one-in-twelve failure in a 148-test assembly that otherwise runs in 400ms.