# 68 — The proof commands a stage requires must be chosen per run, not hard-coded to dotnet

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/68

---

`architecture.js`, `tdd.js`, and `implement.js` all hard-code `dotnet build -c Release` and `dotnet test` as the commands a stage's worker must run and paste as proof. `architecture.js` and `tdd.js` derive theirs inside the shared `selected-rule-warnings` copied block; `implement.js` hard-codes both again in its worker prompt. Nothing accepts them as an input.

Every run therefore builds and tests the whole C# solution, including a run whose contract touches no C# at all. That costs minutes and exposes the run to failures unrelated to the change, such as the flaky test in #60.

The proof commands belong to the run, and the human decides them when the run is set up.

## Done when

The three stages take the proof commands as an input rather than deriving them; the human sets them for the run; and a contract that touches no C# does not run the solution build or test suite.
