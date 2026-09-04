# 22 — Drive tests from scenario expectations, not from the coverage number

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/22

---

A coverage percentage is a lagging, gameable target: chasing it invites tests that execute lines without asserting behavior. REFUTE (the laziness auditor and the lie-catcher) catches that after the fact, but that is a backstop, not prevention.

**Want:** a process that forces test-writing to START from scenarios — the expected behaviors and edge cases the code must satisfy — and derive the tests from those. The scenario/expectation list is the driver and the reviewable artifact; the coverage number is a byproduct, never the goal. An agent that writes tests to hit a number instead of to cover named scenarios is stopped.

**Where it fits:** sharpens the 'proof' area of the best-practices guide (test-first becomes scenario-first / behavior-first), and it is enforced once the process/plan-governance machinery is in place.

**Related:** #21 (enforce the workflow pipeline), #12 (the coverage gate — the metric this is the antidote to).