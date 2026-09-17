# 59 — Replace grep as the default discovery lens in the rails

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/59

---

## Why

Grep repeatedly produced false "not found" conclusions that cost whole review rounds.

The concrete failure: a sweep for which rails each workflow script loads searched for the literal string `rails-decisions/SKILL.md`. Three scripts — `tdd.js`, `rule-phase.js`, `architecture.js` — build that path from a variable as `.agents/skills/${adv.rail}/SKILL.md`, so the literal never appears. All three were reported as missing the rail when all three load it correctly. Three findings were raised that did not exist, and a round was spent on them.

This is not a one-off. Any reference assembled at runtime — a template literal, a concatenation, a value from a table — is invisible to a literal search, and the search returns clean.

## What to decide

The rails currently name CodeGraph and grep as the discovery lenses. Grep as a *primary* lens for "does anything reference X" is the problem. Options include making CodeGraph the required first lens for reference questions, requiring a second lens before any "absent" conclusion, or reading the candidate files directly when the set is small enough to read.

Tim: "HOW about instead YOU don't rely on GREP for these tasks and WE REMOVE THAT as the CONSTANT FUCK ME TOOL you turn it into? WE need a better way for you to find things. but we'll get to that."

## Scope

Affects the discovery instructions in `rails-explorer`, `rails-dry-code`, and the prompts in `ground.js`, `refute.js`, `hidden-decision-scan.js`, and `prior-art-ledger.js`, which all currently say "CodeGraph and grep".

Deferred by Tim: "but we'll get to that."