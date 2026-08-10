# Corrected workflow + the RDD fix (the record — work from this, not memory)

The process skill `rails-run-a-workflow` was missing two whole stages — DESIGN and the RULE-PHASE — so RDD, the founding idea of the project, was never wired into the operational process (it sat as abstract prose only in `rails-read-me`). This is the corrected design and the exact changes to make.

## The corrected pipeline (8 stages)

GROUND → DESIGN → CONTRACT → RULE-PHASE → IMPLEMENT → REFUTE → GATE → REPORT

- **GROUND** — explorer + `prior-art-ledger`; `hidden-decision-scan` runs later on the draft contract. Produces the facts, the reuse ledger, the open decisions.
- **DESIGN** — the architect picks the approach AND determines the rules: for this work, what guardrail can we develop to make a failure mode impossible, or at least greppable. This is where rules are DETERMINED. Guided by the code rails.
- **CONTRACT** — record the decisions, including the rules to add; Tim signs off (a new rule is a Tim-decision — it's a guardrail).
- **RULE-PHASE** — a rule-gen agent writes the determined rules into `analyzers/`. They go RED against existing violating code. Runs its own SOLID/DRY/Lie-catcher pass so the rules themselves are clean.
- **IMPLEMENT** — a fresh worker, rule-writing authority revoked (can't add, edit, or suppress an analyzer). Builds within the rules and cleans up the RED until the build is green under the new rule (the cleanup law).
- **REFUTE** — the adversaries (Prove-It, SOLID, DRY, laziness-auditor, Lie-catcher) try to break it against the contract and the rails.
- **GATE** — accept only when the build and tests are green under the new rule, every adversary PASS, and proof pasted.
- **REPORT** — the terminal outcome, one of two: SUCCESS (done and proven) or ESCALATION (refuted twice, a design fork, or stuck — the decision Tim must make). Written in Tim's terms (`how-to-communicate` + `rewrite-in-tim`), decision first. This replaces the old ESCALATE.

## The rule trigger (the founding point — do not narrow it)

A rule is developed WHENEVER a rule can be developed to mechanically stop a way the AI could cut a corner or hurt Tim — NOT only when architecture changes. DESIGN asks that question on all substantive work.

## Changes to `rails-run-a-workflow` (current file has only GROUND/CONTRACT/IMPLEMENT/REFUTE/GATE + ESCALATE)

1. Stages: add DESIGN and RULE-PHASE; change ESCALATE to REPORT (success or escalation); update the count and the diagram to the 8-stage sequence above.
2. New `### DESIGN` section (approach + determine the rules; the rule trigger above).
3. New `### RULE-PHASE` section (rule-gen agent writes rules into `analyzers/`, RED against violations, own adversary pass).
4. `### IMPLEMENT`: make it the implementation-phase — authority revoked, clean up the RED until green under the new rule.
5. `### GATE`: enforce the cleanup law — green under the new rule, no lingering RED, no suppression.
6. Roles: add the architect (DESIGN) and the rule-gen agent (RULE-PHASE); seven roles → nine; state the separation of powers (rule-writer ≠ implementer; the follower can't rewrite the rules).
7. CONTRACT: a new rule is a Tim-decision, signed off before RULE-PHASE writes it — link to `rails-decisions`.
8. Lie-catcher + failure-class pack: hunt suppression (`#pragma`, `[SuppressMessage]`, `NoWarn`, `severity = none`, a dropped analyzer reference) — the only way to break the fence; top-line finding.
9. Tiering: DESIGN and RULE-PHASE run whenever a guardrail can be developed for the work.

## Reconcile `rails-read-me`

- De-dup: once `rails-run-a-workflow` owns the operational RDD, trim `rails-read-me`'s Rule-Driven-Development section to a short pointer to the process rail.
- Fix its stage list / process description to the corrected 8-stage sequence.

The RDD source content to preserve lives in `rails-read-me`'s "Rule-Driven Development" section (rule-phase, implementation-phase, tokens revoked, separation of powers, the cleanup law).
