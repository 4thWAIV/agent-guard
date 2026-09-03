---
name: rails-run-a-workflow
description: The standard way substantive work runs — a worker executes against a written CONTRACT, independent adversaries REFUTE the result against that contract, and the gate accepts only unrefuted and proven results. Use for any mutating change, any claimed fix, migrations, validator or gate changes, or any reusable process change.
---

# Rails: run a workflow

The contract replaces the subjective question “would the human approve?” with checks an adversary can refute. `rails-write-a-contract` is the only definition of the contract contents and section order. `rails-decisions` owns the decision boundary and approval procedure.

Substantive work is any mutation other than a trivial documentation edit, including any claimed fix, migration, validator, gate, reusable process, state, behavior, or shipped-code change. “Already approved” covers only the exact item that got a yes, not an inflated reading of it.

## Cross-stage requirement rule

### Never weaken a requirement to fit the code

- **Practice:** Grow the code (or the tool) until it meets the requirement as stated.
- **Violation:** The change narrows, softens, or reinterprets a requirement so it matches what the code already does. This is a top-line finding. Any change that quietly makes a requirement match existing behavior is a top-line finding.
- **Fix:** Restore the full requirement and grow the code to meet it.

Every stage applies this rule. No role may reinterpret the contract to match existing behavior.

## Level selection

Work runs at one of three Levels. L1 is the full process. L3 is the lightest.

| Stage or role | L1 | L2 | L3 |
|---|---|---|---|
| Orchestrator | required | required | acts directly |
| GROUND | required | not present | not present |
| DESIGN | required | only when needed | not present |
| CONTRACT | required | required | no contract |
| RULE-PHASE | required unless the human grants an exception | not present | not present |
| ARCHITECTURE | required; records the approved no-signature result when the contract changes no signature | only when needed | not present |
| TDD | required | only when needed | not present |
| IMPLEMENT | required | required | orchestrator acts directly only for a trivial documentation edit |
| READINESS | required | required | not present |
| Prove-It | required | required | not present |
| SOLID adversary | required | not present | not present |
| DRY adversary | required | required | required |
| Laziness-auditor | required | only when needed | not present |
| Lie-catcher | required | required | only when the human requests it |

`ANY run IS DEFAULT to L1 UNless I approve L2 or L3`. L1 does not require explicit approval. L2 and L3 require Tim's explicit approval for that run.

**L1 — full process.** Use L1 for any mutation with structural or design consequences and for any claimed fix. Every applicable stage and all five REFUTE adversaries run. RULE-PHASE is omitted only when the human explicitly grants that exception. ARCHITECTURE records that it has nothing to write when no signature changes.

**L2 — delegated process.** Use L2 only when a SOLID violation is not possible. A contract, opening and closing build-and-test bracket, one fresh worker, Prove-It, DRY, and the Lie-catcher are required. DESIGN, ARCHITECTURE, TDD, and the Laziness-auditor run when the work needs them. If the work can touch structure or design, it is L1 rather than L2.

**L3 — direct work.** L3 covers reads, probes, and trivial documentation edits without a contract. The orchestrator acts directly, and DRY runs through REFUTE. The Lie-catcher also runs when Tim requests it. Every other mutation requires an L1 or L2 contract.

The human approves how much rigor the work needs. A Level never grants permission to expand the approved work.

## Stage sequence

```text
GROUND → DESIGN → CONTRACT → RULE-PHASE → ARCHITECTURE → TDD → IMPLEMENT → READINESS → REFUTE → GATE → REPORT
```

Stages that the chosen Level marks “not present” are omitted. Stage order never changes.

## Running the stages once

GROUND, DESIGN, CONTRACT, RULE-PHASE, ARCHITECTURE and TDD each run **once per contract**. A fix round loops IMPLEMENT and REFUTE only. Re-running any completed stage is the human's decision and needs their explicit yes — never the orchestrator's own call, and never as a way to refresh an input.

A completed stage's recorded result is background, not the spec; the contract is the spec. Fix rounds keep changing the tree, so a recorded fact the current code has since overtaken is expected and is never a finding. Each agent's own reading of the live code is the authority on what is true now.

## Launch preconditions

Check all three before launching any agent panel. Each one is a stop, not a warning.

**Nothing is open.** No stage runs while any item is open. An open item is anything approved but not finished, anything the orchestrator has flagged as awaiting the human's decision, anything it would describe to the human as "one more thing", and any part of approved work it cut on its own. Every one of these belongs in front of the human BEFORE the stage, never carried alongside it and never disclosed after. A stage that runs with an open item produces a verdict on work that does not exist yet, and the human finds out at the gate.

**The inputs are whole.** Never launch on an input already known to be abridged, stale, reshaped, or hand-composed. Every recorded artifact is handed over as a file path so there is one copy and nothing to reconcile; the exception is L3, where the work may exist only in the conversation and no file exists to point at. If an input is wrong, fix it or stop and ask — launching anyway spends the whole panel proving what was already known.

**Every decision the work rides on is recorded.** A decision the human gave in conversation is not in force until it is written into the contract with their verbatim words. No panel launches while work depends on an unrecorded decision: the adversaries are right to refuse it, and the round is wasted establishing that.

**No exclusion the orchestrator wrote.** Every exclusion, waiver, or "do not raise this" in an agent's instruction must quote a Decision from the contract. An exclusion that traces to nothing but the orchestrator is void, and writing one is how an orchestrator hides its own unfinished or unapproved work from the adversaries that exist to catch it. Instructions are typed into a tool call and touch no file, so nothing else can check them — this precondition is the only guard.

**The stage is the one that should run.** Confirm the stage about to run is the next one in sequence or an authorized fix round, not a completed stage being repeated.

### GROUND

- **Role:** One fresh explorer per named area. The orchestrator runs the hidden-decision scan before the contract locks.
- **Scripts:** `ground.js`, which runs the prior-art ledger itself from its inlined copied block when the change introduces a new capability, and `hidden-decision-scan.js`. Do not run `prior-art-ledger.js` separately as part of GROUND; doing so runs the ledger twice.
- **Inputs:** The repository path, named areas, new capabilities, the requirement, and the draft contract for the hidden-decision scan.
- **Outputs:** Grounded facts with evidence, every surface, open questions, structured `autoResolved` records, the reuse ledger, and unresolved forced decisions.
- **Revoked authority:** Explorers, reuse judges, and decision hunters do not write code, change the contract, or decide anything reserved for the human.

Run `rails-explorer` fresh before judging any subsystem whose current code-level behavior is not already grounded in this run. It owns the exploration method and evidence requirements. Documentation, comments, prior run records, and memory only point to places to inspect.

Run the prior-art ledger only for capabilities the work introduces. `rails-dry-code` owns its lenses and rulings. When the work introduces no new capability, GROUND accepts an empty capability list and the contract records `None — this change introduces no new capability` rather than inventing a capability.

The hidden-decision scan uses `rails-decisions` and the best-practices guide. A guide-covered choice is recorded in `autoResolved` with its choice, resolution, principle, and evidence. A forced choice that no approved principle answers stays unresolved for the human. No contract advances past CONTRACT with an unresolved forced choice.

### DESIGN

- **Role:** Independent architects propose approaches; one judge selects and merges the approach.
- **Script:** `design.js`.
- **Inputs:** The goal, grounded facts, and the optional prior-art ledger from GROUND. The ledger is absent only when the selected Level runs DESIGN without GROUND.
- **Outputs:** Candidate approaches, one selected approach, `Rules to add`, risks, required `autoResolved` records for every proposal and the final verdict, and `reuseInstructions: string[]`. The reuse list is empty when no instruction applies.
- **Revoked authority:** DESIGN does not write production code, tests, analyzers, interfaces, or approvals.

Every architect and the judge load `rails-solid-code`, `rails-dry-code`, `rails-real-work`, and the best-practices guide.

Run a SOLID and a DRY review at DESIGN, over the drafted approach against the EXISTING code, BEFORE the contract locks. The DRY pass is prior-art-ledger-driven: it finds what the drafted design would make a downstream agent re-implement and writes explicit “reuse X” instructions INTO the contract. The SOLID pass catches mislaid responsibility and design-level duplication before any code exists. This ADDS the check in front; it does not move it — the RULE-PHASE and REFUTE adversaries remain the backstop. The DRY standard is “every piece of knowledge has one authoritative representation,” so a diverged semantic duplicate is a violation even when the text differs, and a duplicate that currently AGREES is the worst case because it will drift.

When GROUND ran, `design.js` receives its complete prior-art ledger. The judge returns the explicit, deduplicated reuse instructions the contract must carry; no later stage reinterprets design prose to reconstruct them.

The rule trigger applies to all substantive work: whenever a rule can mechanically stop a way the AI could cut a corner or hurt the human, DESIGN determines that rule. Every determined rule enters the contract for the human’s approval before RULE-PHASE writes it.

### CONTRACT

- **Role:** The orchestrator writes and locks the contract.
- **Script:** No workflow script. Follow `rails-write-a-contract`.
- **Inputs:** The requirement, grounded facts, surfaces, reuse ledger, selected design, proposed rules, resolved decisions with approving words, and unresolved findings from the hidden-decision scan.
- **Outputs:** `.dev/inprocess/<date>-<slug>/contract.md` and a clean opening `make build` plus `make test` proof before the first mutating stage.
- **Revoked authority:** No mutating stage starts before the contract is written, every forced decision is resolved, and the opening bracket is green.

Acceptance checks are derived verbatim from the requirement sentences and carry exact rerunnable commands. A clean `make build` and `make test` opening bracket runs once per contract after the contract is written and before RULE-PHASE or any other mutating stage. It establishes the pre-change state before any approved rule, interface, or test changes are written.

Scope changes ONLY by the human editing the contract — or, when the human is unavailable and has given explicit prior authorization for exactly this extension, by recording that authorization verbatim as the change’s ruling provenance and top-lining it.

### RULE-PHASE

- **Role:** One rule-generation agent writes the approved rules; independent SOLID, DRY, and Lie-catcher adversaries refute them.
- **Script:** `rule-phase.js`.
- **Input:** The contract’s `Rules to add` section.
- **Outputs:** Analyzer or test rule files, production build impact, per-rule violating-fixture and compliant-fixture proof, and one PASS or FAIL verdict from each adversary. When the contract adds no rules, the successful result records `ruleFiles: []`, `buildProof: 'Not applicable — the approved contract adds no rules.'`, `perRule: []`, and the exact contract evidence in `notes`.
- **Revoked authority:** The rule-generation agent does not implement the feature or clean the violations. The adversaries do not edit the rules. No participant suppresses, exempts, hides, or lowers a rule to reach green.

Each approved rule is proven by one violating fixture that produces the diagnostic and one compliant fixture that does not. Production RED is recorded as the rule's impact on existing code; it is not proof and does not fail RULE-PHASE. When production is RED, the result records every responsible diagnostic ID and production location. The independent panel runs even when the approved rule list is empty.

Before ARCHITECTURE begins, Tim approves the exact diagnostic IDs and exact intermediate builds where those diagnostics may remain warnings. The orchestrator passes that same approved `ruleWarningIds` list to `architecture.js` and `tdd.js`. Both scripts hold the `selected-rule-warnings` copied block, which translates only those IDs into `WarningsNotAsErrors`; the selected diagnostics remain enabled and visible. GATE never receives this override.

### ARCHITECTURE

- **Role:** One fresh architecture author writes the decided surface; independent SOLID, DRY, and Lie-catcher adversaries refute it.
- **Script:** `architecture.js`.
- **Inputs:** The contract’s decided interface structure, the live analyzer fence, and the exact `ruleWarningIds` Tim approved for the ARCHITECTURE build.
- **Outputs:** Compiling interfaces, DTOs, enums, concrete signatures, a signature-only diff, build proof, and PASS or FAIL verdicts. When the contract changes no signature, the result records empty `skeletonFiles` and `perType`, the no-signature explanation and exact contract evidence, then runs the same adversary panel.
- **Revoked authority:** The author writes no tests or behavior and cannot edit or suppress an analyzer. The adversaries make no code changes.

For a new type or member, each new body is `throw new NotImplementedException()`. For an expansion, existing bodies remain exactly as they are unless the approved signature must change; ARCHITECTURE never stubs unchanged working code back to a throw. If no signature changes, the stage records that it has nothing to write and proceeds only after its SOLID, DRY, and Lie-catcher panel reviews that contract-backed result.

The human signs off the signature diff in their own words before TDD begins. `rails-decisions` owns that approval rule.

### TDD

- **Role:** One fresh test author writes the acceptance tests; independent test-quality, DRY, and Lie-catcher adversaries refute them.
- **Script:** `tdd.js`.
- **Inputs:** The contract’s `Acceptance` section, the ARCHITECTURE surface, and the same exact `ruleWarningIds` Tim approved for the TDD build.
- **Outputs:** Compiling RED tests, per-criterion coverage, RED proof, and one PASS or FAIL verdict from each adversary. When the contract names no test surface and forbids test changes, the result records `testFiles: []`, `redProof: 'Not applicable — the approved contract authorizes no test files.'`, `coverage: []`, and exact contract evidence in `notes`, then runs the same adversary panel.
- **Revoked authority:** The test author does not change an interface, fill a production body, or judge the tests. The adversaries make no code changes.

`rails-test-code` owns test completeness, RED-first behavior, assertions, unit-test fakes, pointed-integration coverage, and test tampering. New behavior is RED through `NotImplementedException`; expanded behavior is RED through an expected-versus-actual failure against the required behavior.

The no-test result is valid only when the approved contract names no test surface and forbids test changes. The test-quality, DRY, and Lie-catcher panel returns FAIL when the contract requires a test or the no-test claim lacks exact contract evidence. Every contract that authorizes tests retains the normal RED-first requirement and empty-result rejection.

#### Selected RULE warnings during ARCHITECTURE and TDD

The orchestrator may pass an optional `ruleWarningIds` list to `architecture.js` and `tdd.js` only for diagnostic IDs that Tim approved verbatim in the contract to remain warnings in that exact intermediate build. Both stages receive the same approved list. The list contains whatever diagnostic IDs the human approved for that exact case and is not limited by a predefined prefix or set. A missing or empty list uses the existing plain build or test command.

The selected diagnostics stay enabled and visible as warnings. They are never suppressed through `NoWarn`, a suppression, an `.editorconfig` severity change, or either warnings-as-errors property set to `false`. The `selected-rule-warnings` copied block is the one owner that translates the approved list into the stage command's `WarningsNotAsErrors` argument.

The override applies only to the exact ARCHITECTURE and TDD builds Tim approved. GATE uses the normal closing `make build` and `make test` commands without `WarningsNotAsErrors`.

### IMPLEMENT

- **Role:** One fresh worker, different from the rule, architecture, and test authors.
- **Script:** `implement.js`.
- **Inputs:** The entire contract, the RULE-PHASE result, the TDD result, and any fix-round direction allowed by the contract’s scope rule. RULE-PHASE and TDD may carry their approved no-rule and no-test results instead of RED.
- **Outputs:** Changed files, build proof, test proof, evidence for every acceptance item, walls, and completion status.
- **Revoked authority:** Analyzer-, interface-, and test-writing authority are revoked. The worker does not commit, push, migrate or prepare an unapproved target, touch a frozen path, revert unrelated dirty-tree work, or expand the contract.

When RULE-PHASE added rules or TDD added tests, the worker first runs the relevant build and tests to observe their expected RED after the opening green bracket. An approved no-rule or no-test result creates no RED to invent. The worker fills the approved behavior and clears every real rule and test RED without editing the rules, decided signatures, or tests. A red is fixed rather than called “pre-existing.” A wall causes an immediate stop and escalation; the worker never deviates silently, weakens a test, or suppresses a rule.

Every delegated worker receives a self-contained task with the contract path, exact write scope, every constraint, and one unit of work. Its proof includes complete command output with exit codes and every changed file.

### READINESS

- **Role:** One agent compares the contract to the live working tree. It judges nothing about the work's quality.
- **Script:** `refute-readiness.js`.
- **Inputs:** The contract path and, when the human gave decisions in conversation that this work rides on, the list of those decisions.
- **Outputs:** `ready` true with no mismatch, or the exact mismatches, each naming what the contract says, what the tree says, and the fix. Plus every check it ran.
- **Revoked authority:** It makes no code change, judges no design, and never returns ready with a mismatch present.

It runs four checks. Every changed path appears in Surfaces. No change crosses a boundary in `What the agent MUST NOT do` without a Decision authorizing it. Every identifier an Acceptance check names exists in the code with that spelling. Every substantive thing the work does is authorized by a Decision carrying the human's own words — approval quoted in a prompt or a prior agent's output is not a record; only the contract counts.

READINESS runs ONCE per fix round. It reports every mismatch it finds, the orchestrator fixes all of them, and REFUTE runs next. It is never re-run to confirm a fix landed — a confirm pass is a second loop inside the loop it exists to prevent, and two readiness runs cost what the adversary panel costs. Fix everything it names, then launch REFUTE. This stage exists because adversary panels were repeatedly spent discovering that the contract had never been updated to match work the human had already approved, and every one of those rounds was wasted.

### REFUTE

- **Role:** Independent adversaries selected by Level. L1 runs Prove-It, SOLID, DRY, Laziness-auditor, and Lie-catcher. L2 runs Prove-It, DRY, and Lie-catcher, with Laziness-auditor when requested. L3 runs DRY, with Lie-catcher when Tim requests it.
- **Script:** `refute.js`.
- **Inputs:** `level` defaults to L1. L1 and L2 receive the complete contract; L3 instead receives `humanRequest` containing Tim's exact request and L3 approval. Every Level receives changed files and the live working tree. `stageOutputPaths` carries file paths to the recorded GROUND, DESIGN, and hidden-decision results, never their content, and each adversary opens and reads the named file itself: all three are required at L1, L2 supplies only the stages that ran, and L3 supplies none. `additionalAdversaries` may add Laziness-auditor at L2 or a human-requested Lie-catcher at L3.
- **Outputs:** One `PASS` or `FAIL` verdict per lens, findings, refutation attempts, proof checked, and a fix path from every adversary except the Lie-catcher.
- **Revoked authority:** Adversaries do not change code. The Lie-catcher gives no fix advice. No adversary approves its own work.

DRY is required at every Level. The SOLID, DRY, Laziness, and Lie-catcher adversaries load `rails-solid-code`, `rails-dry-code`, `rails-real-work`, and `rails-decisions` as their reusable checklists. Prove-It uses the contract. The Lie-catcher also verifies every supplied `autoResolved` record and the corresponding decision filtering, and audits the orchestrator’s own steps.

Adjudicate every finding against the live code before acting. Reject a false positive only with evidence. A finding that changes an approved design returns to the human before a fix is built. Any condition attached to an accepted finding travels verbatim into the fix direction.

### GATE

- **Role:** The orchestrator reconciles adversary verdicts and runs the closing proof; adversaries rerun the contract’s checks.
- **Script:** No separate script. REFUTE verdicts and the contract drive the gate.
- **Inputs:** All adversary verdicts, the contract, the live working tree, and complete rerunnable proof.
- **Outputs:** Accepted or rejected, plus the closing `make build` and `make test` output with exit codes.
- **Revoked authority:** The gate does not waive a finding, accept cropped or stale output, call a red “pre-existing,” or accept a suppressed rule. Only an explicit human waiver recorded under the scope rule can waive the cleanup law.

A result counts only when no adversary refutes and every verification includes the flags, bytes, command output, and exit code needed to rerun it. The closing build and tests are green under the analyzer fence. `#pragma warning disable`, `[SuppressMessage]`, `NoWarn`, `severity = none`, or a dropped analyzer reference used to reach green is an automatic refute. A status message or exit code without positive state proof is not proof.

Refuted work returns to the worker with one reconciled directive. SOLID wins when SOLID and DRY conflict. The orchestrator never loosens a rule to reconcile findings.

### REPORT

- **Role:** The orchestrator writes one terminal report.
- **Script:** No workflow script.
- **Inputs:** The contract success definition, gate result, adversary verdicts, proof, and run-record.
- **Output:** One report whose first word is `SUCCEEDED` or `FAILED` by the contract success definition.
- **Revoked authority:** REPORT does not hide a failure, report incomplete work as success, include internal paperwork in the report body, or keep iterating past a genuine human decision.

Reports are self-contained — no reference to internal context the reader hasn't seen. A status report leads with pass or fail against the success definition, then the known-broken count and the delta since the last report, and says “not complete” before describing any successful part. `rails-real-work` rail 12 owns the ordering rule that problems come before successes; this stage does not restate it.

A `SUCCEEDED` report gives the gate result, each adversary verdict, proof counts and paths, and the next action. A `FAILED` report gives the decision the human must make when the same point is refuted twice, a genuine design fork is unresolved, or the run is stuck. Root cause precedes item lists. Paperwork and process notes stay in the run record.

## Retries and stuck work

A valid structured result with no findings is a result. Examples include `candidates: []`, `findings: []`, and a `PASS` verdict with an empty findings list. Do not retry or discard one.

The `required-agent-runtime` copied block is the one executable owner of the following behavior and of common adversary verdict validation. Every required workflow role runs through it.

When a required agent returns no valid result:

1. Let every already-running agent finish.
2. Retry only the role that returned no valid result, once, with the same context and the first failure attached.
3. Preserve every valid PASS result, every valid FAIL result with actionable findings, and every valid empty result.
4. If the retried role still returns no valid result, return the stage as unsuccessful with the preserved valid results, the failed role, both attempt outcomes, and enough context to diagnose and rerun only that role.
5. Do not launch a dependent phase or the next workflow stage while a required role has no valid result.
6. A later retry runs only the failed role and merges its valid result with the preserved results. Successful roles are not rerun.
7. A valid FAIL verdict follows the normal fix-and-refute process. Never retry a valid FAIL merely to seek a PASS.

The unsuccessful result supplies `priorPanelResults` and `retryRoles`. A stage with author work also supplies `priorStageResult`, so the later panel retry does not rerun that author.

Reuse the implementer’s context across retries and each adversary’s context across refute rounds. Spawn a fresh replacement only after the same approach is rejected twice, two rounds produce no reduction in findings, or the agent starts rationalizing a deviation. Give the replacement the prior refutation history.

Each adversary records what it tried to refute and how. An adversary with no attempts is rubber-stamping. Zero findings twice on changed work triggers a fresh adversary; repeatedly spawning reviewers to find an approval is itself a Lie-catcher finding.

If work is stuck or overwhelmed, say `STUCK`, name the exact wall, reduce the next action to one concrete step, delegate any independent piece, and park only the genuinely human-gated item. Do not wait silently.

When a problem is too large to hold, put a guardrail around it — a lint, validator, or test — that exposes a metric to drive to zero. `rails-real-work` forbids scaffolding or gaming the metric. `rails-dry-code` requires extending the right existing owner before adding another guardrail.

## Copied modules

The Workflow runtime provides no module import and caps `workflow()` nesting at one level, so every workflow script is top level and shared code is copied into each one. Each copy sits between `// ##COPIED-MODULE-BEGIN## <name>` and `// ##COPIED-MODULE-END## <name>`, and `eng/check-copied-modules.mjs` requires every copy of a name to be byte-identical while hardcoding no expected content. Editing a block means editing every copy of it and rerunning that check.

## Provenance

Every run keeps its contract, agent outputs, adversary verdicts, and final proof under `.dev/inprocess/<date>-<slug>/` while the work is active.

Move the run record to `.dev/completed/run-records/` inside the same shipping pull request, before merge. REPORT sweeps the whole `.dev/inprocess/` tree and files every run whose work has already shipped. Nothing in `.dev/inprocess/` may describe work already merged to `dev` or otherwise shipped.
