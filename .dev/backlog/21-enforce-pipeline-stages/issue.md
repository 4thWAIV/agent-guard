# 21 — Enforce the workflow pipeline — a stage cannot be skipped without the human's approval

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/21

---

Today the 8-stage process (GROUND → DESIGN → CONTRACT → RULE-PHASE → IMPLEMENT → REFUTE → GATE → REPORT) is enforced only by behavioral rules in the skills and memory. An agent can still collapse or skip a stage on its own judgment.

**Want:** a guardrail that makes skipping or lightening a stage require the human's explicit, by-name approval — the same way an analyzer rule makes a raw boundary call a build error. An agent that jumps GROUND → CONTRACT → IMPLEMENT, or decides a stage is 'empty' or 'N/A', is stopped unless the human waived that stage.

**Why:** skipping stages breaks the process and its separation of powers (e.g. the IMPLEMENT worker must not do rule-writing). Which stages run, and at what rigor, is the human's call — not the agent's.

**When:** deferred. This rides on the guard's process/plan-governance enforcement machinery, which is not yet in place (same dependency as the rule-token grant system). Build it once that system is sufficiently in place.

**Incident that prompted this:** the coverage-gate task skipped DESIGN and RULE-PHASE on the agent's own call and launched the IMPLEMENT worker; caught after the fact, not blocked.

Related: #5 (guard-enforced reuse ledger — deny a mutating run whose contract lacks a prior-art ledger) is the same class of plan-governance enforcement.