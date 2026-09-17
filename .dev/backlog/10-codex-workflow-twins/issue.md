# 10 — Build the Codex twins of the workflow (ground/design/refute + the two tools)

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/10

---

## What
Codex now has the personal skills (synced into `~/.codex/skills`), but it cannot run the workflow. The five workflow scripts — `ground`, `design`, `refute`, `prior-art-ledger`, `hidden-decision-scan` — are Claude Workflow-tool scripts in `.agents/workflows/`. Codex has no equivalent, so a Codex session can't run GROUND / DESIGN / REFUTE or the two tools.

## Scope
Per the cross-tool design (one shared step-spec, thin wiring per tool, no new standalone tool): implement the Codex-native equivalents that run the same fan-out steps through Codex's own subagents, driven by the same step-spec the Claude scripts use. A Codex session must be able to run GROUND, DESIGN, REFUTE, the prior-art ledger, and the hidden-decision scan and get the same structured outputs.

## Source
MANIFEST cross-tool row "Codex twins of the runner + both tools"; RESUME cross-tool note. Deferred behind the CrossPlatform build; filed so it is tracked, not lost.