# Reorganization preflight — approved decisions and checkpoint record

This file records the exact code gaps found at the pre-worker checkpoint and the approvals that resolved them. Every decision below is approved. Statements about what the code “currently” did describe that checkpoint, not the post-implementation working tree.

## Already decided: `default-level-selection`

This is a permanent workflow rule, not a one-run exception.

Tim's exact decision is recorded in `contract.md`:

> OKAY let me be very clear.  I DON"T approve L1 RUNS.  I approve L2 and L3 RUNS.  ANY run IS DEFAULT to L1 UNless I approve L2 or L3 so why are we hung up on a conversation about wether or not I approved L1?
>
> IF I do not specify a LEVEL it is L1 we need to put that INTO THIS contract so I don't have to tell you this in the future.

The same contract already directs the implementation:

> Apply the exact default-Level rule in `default-level-selection` to `rails-run-a-workflow`.

`rails-run-a-workflow` is the single owner because `rails-read-me` already says:

> `rails-run-a-workflow` owns level selection, stage execution, role authority, retries, the gate, provenance, and reporting.

At the pre-worker checkpoint, the source skill had not been edited for this decision. The approved implementation changes `.agents/skills/rails-run-a-workflow/SKILL.md` so an omitted Level means L1 and only L2 or L3 requires Tim's explicit approval for that run. `rails-read-me` continues to route readers to that owner instead of copying the Level rule into a second place.

No approval remains open for `default-level-selection`.

---

## Approved: `design-ledger-handoff`

### The requirement that exposes the gap

At the pre-worker checkpoint, `rails-run-a-workflow` said:

> **Inputs:** The goal and GROUND outputs.

It also says:

> The DRY pass is prior-art-ledger-driven: it finds what the drafted design would make a downstream agent re-implement and writes explicit “reuse X” instructions INTO the contract.

GROUND returns both grounded facts and a prior-art ledger. DESIGN must therefore be able to read the ledger and hand explicit reuse instructions to the contract writer.

### What `design.js` accepted at the pre-worker checkpoint

The current input extraction is:

```js
const projectPath = input && input.projectPath
const goal = input && input.goal
const facts = input && input.facts
const angles = (input && input.angles) || [/* defaults */]
```

The current validation describes the public input as:

```js
design requires args { projectPath, goal, facts?, angles?: [{id, focus}] }
```

There is no `ledger` input. The architect prompts receive `facts`; they never receive the prior-art ledger.

### What `design.js` returned at the pre-worker checkpoint

The judge's current structured verdict contains:

```js
{
  winningAngle,
  why,
  synthesizedApproach,
  graftedFrom,
  rulesToAdd,
  autoResolved,
}
```

There is no separate place for the required contract-ready “reuse X” instructions. A later caller would have to guess whether prose inside `synthesizedApproach` is a reuse instruction.

### Recommended minimal change

Add one optional input:

```js
ledger?: PriorArtLedger
```

The value is optional because L2 may run DESIGN without GROUND. When GROUND ran, the orchestrator passes the ledger that GROUND already produced. `design.js` includes that value in the architect and judge prompts.

Add one required field to the final judge verdict:

```js
reuseInstructions: string[]
```

Each string is one explicit instruction that can be copied into the contract, such as `Reuse the existing X owner; do not add a second path.` The judge returns `[]` when the ledger produces no reuse instruction.

### Why both pieces are needed

- `ledger` supplies the evidence DESIGN is required to use.
- `reuseInstructions` carries DESIGN's concrete result to the contract writer without making the contract writer reinterpret design prose.

Adding only `ledger` would let DESIGN read the evidence but would still leave no defined handoff. Adding only `reuseInstructions` would ask DESIGN to produce ledger-driven results without receiving the ledger.

### What this does not change

- It does not change `ground.js`; that script already produces the ledger.
- It does not add a new workflow stage or a new contract section.
- It does not add `reuseInstructions` to every architect proposal. Only the judge produces the final deduplicated instructions.
- It does not require callers to invent a ledger when GROUND did not run.
- It does not change `rulesToAdd`, `autoResolved`, or any existing design field.

### Approval record

Tim:

> I thought I approved this but just to record it again.   This is approved.

Tim reconfirmed:

> ## design-ledger-handoff
>
> Approved

---

## Approved: `rule-phase-proof-interface`

### The approved behavior that exposes the gap

The contract already says:

> RULE-PHASE installs every approved rule before ARCHITECTURE and TDD. It proves each rule with a violating fixture that produces the diagnostic and a compliant fixture that does not. It does not require production code to violate a new rule or require the production build to be RED.

It also said:

> If an approved rule diagnoses existing production code, RULE-PHASE stops and reports the exact conflict. Only Tim may approve selected diagnostic IDs as warnings for the exact intermediate builds he names. GATE never receives that override unless Tim explicitly approves it.

That sentence is superseded by the approved `rule-phase-red-handoff` recorded below.

The behavior is already approved. The remaining decision is the structured result that records its proof.

### What `rule-phase.js` recorded at the pre-worker checkpoint

The pre-worker rule-generation result was:

```js
{
  ruleFiles,
  redProof,
  perRule: [{
    rule,
    diagnosticId,
    firesAgainst,
  }],
  notes,
}
```

The pre-worker prompt defined those fields this way:

```text
redProof (the pasted build output), perRule (each rule with its diagnostic id and the file:line it fires against, or "preventive (none)")
```

That shape was built for the old instruction that made production code go RED. It records one production location in `firesAgainst`. It has no field for the violating fixture, no field for the compliant fixture, and no result for either fixture run.

The pre-worker no-rule branch also treated an empty rule list as an error:

```js
if (!ruleGen || !ruleGen.ruleFiles || !ruleGen.ruleFiles.length) {
  return { ruleFiles: [], error: 'rule-gen produced no rule files', ruleGen }
}
```

This run's contract says:

> None. The approved work reorganizes documentation and synchronizes existing workflow prompts. Adding an analyzer would exceed the approved scope.

Therefore, an empty rule list is the correct result for this run, not an error and not permission to skip RULE-PHASE.

### Recommended replacement result

```js
{
  ruleFiles: string[],
  buildProof: string,
  perRule: [{
    rule: string,
    diagnosticId: string,
    violatingFixture: string,
    violatingProof: string,
    compliantFixture: string,
    compliantProof: string,
  }],
  notes?: string,
}
```

### What each field means and why it is present

- `ruleFiles` is the existing list of analyzer or test-rule files written during RULE-PHASE. The adversaries need it to inspect the actual rule implementation.
- `buildProof` replaces `redProof`. It records the production build output after the rules are installed. Production RED is recorded as the rule's effect on existing code; it is not proof and does not fail RULE-PHASE.
- `rule` is the existing human-readable identity of the approved rule.
- `diagnosticId` is the existing machine-readable identity. Making it required connects the fixture proof and any exact selected-warning approval to one diagnostic. A rule implemented as a non-analyzer test uses the existing `"test"` identity.
- `violatingFixture` identifies the fixture that deliberately violates the rule.
- `violatingProof` records the command output showing that the violating fixture produced the expected diagnostic.
- `compliantFixture` identifies the fixture that complies with the rule.
- `compliantProof` records the command output showing that the compliant fixture did not produce the diagnostic. This is the proof that the rule is not over-broad.
- `notes` remains the existing optional explanation field. It is not a second proof channel.

The four fixture fields are separate because a file path is not proof that the fixture ran, and command output is not enough to identify which fixture produced it.

### The defined no-rule result

When the contract's `Rules to add` section says there are no rules, the same interface returns:

```js
{
  ruleFiles: [],
  buildProof: 'Not applicable — the approved contract adds no rules.',
  perRule: [],
  notes: '<the exact contract sentence establishing that no rules were approved>',
}
```

The SOLID, DRY, and Lie-catcher RULE-PHASE adversaries still run. They verify that the contract actually says no rules and that no rule work was silently omitted.

### What this does not change

- It does not add a new rule.
- It does not allow RULE-PHASE to be skipped.
- It does not use production RED as proof that a rule works.
- It does not make every compiler or build error eligible for a warning override.
- It does not add another warning-selection mechanism; `ruleWarningIds` remains the single approved intermediate-build mechanism for ARCHITECTURE and TDD.
- It does not change the three RULE-PHASE adversaries.

### Approval record

Tim:

> I can now approve this.  I approve this.  this is about settign up the rules, and the phase is allowed to go RED without it being a blocker (the rules are finding prior violations) but it does not need to GO RED to prove anythign that's what FIXTURE tests are for, that show the rule catching the violations.

---

## Approved: `rule-phase-red-handoff`

### Executive summary

RULE-PHASE may pass when new rules make the production build RED. Fixtures—not production RED—prove the rules. When production is RED, RULE-PHASE reports the responsible diagnostic IDs and production locations. Before ARCHITECTURE begins, Tim approves the exact diagnostic IDs and intermediate builds where those diagnostics may remain warnings. `architecture.js` and `tdd.js` apply that same approved list through `WarningsNotAsErrors`. GATE receives no override.

### Approval record

Tim:

> ## rule-phase-red-handoff
>
> Approved.

---

## Approved: `dry-every-level-refute-interface`

### Executive summary

Run DRY validation through `refute.js` at every Level: all five adversaries at L1, Prove-It plus DRY plus Lie-catcher with optional Laziness-auditor at L2, and DRY with an optional human-requested Lie-catcher at L3. An omitted Level means L1.

### Exact input

```js
{
  level?: 'L1' | 'L2' | 'L3',
  additionalAdversaries?: Array<'laziness' | 'lie-catcher'>,
}
```

### Approval record

Tim:

> Approved ...
>
> AND GIVE ME A LIST OF ANYTHIGN ELSE BLOCKING FOWRAD MOTION WITHOUT me having to ask EVERY TURN

---

## Approved: `refute-stage-output-input`

### Executive summary

Pass the complete existing GROUND, DESIGN, and hidden-decision work products into REFUTE so the Lie-catcher can verify them. L1 requires all three, L2 supplies outputs only for stages that ran, and L3 supplies none.

### Exact input

```js
stageOutputs?: {
  ground?: GroundResult,
  design?: DesignResult,
  hiddenDecisionScan?: HiddenDecisionScanResult,
}
```

### Approval record

Tim:

> Okay you have my permision to add the extra prior stage work product to the what was it refut stage?

---

## Approved: `l3-refute-context`

### Executive summary

L1 and L2 supply `contractPath`. L3 has no contract, so L3 supplies `humanRequest` containing Tim's exact request defining the work and approving L3.

### Exact input

```js
humanRequest?: string
```

### Approval record

Tim:

> Aproved.

---

## Approved: `reorganization-opening-proof-recovery`

### Why recovery was required

This decision applies only to the current `2026-09-01-reorganize-rails` run. It is not a reusable workflow rule or a new JS interface.

`rails-run-a-workflow` requires:

> A clean `make build` and `make test` opening bracket runs once per contract after the contract is written and before RULE-PHASE or any other mutating stage.

At the recovery decision checkpoint, the run did not have a saved opening build/test record, GROUND output, DESIGN output, or hidden-decision output. Those recovered records now exist. The reorganization edits were already made and staged at Tim's direction. The later selected-warning L2 change is unstaged. The staged snapshot remains untouched as the requested revert point.

Running `make build` and `make test` at that point could prove the checkpoint was green. It could not truthfully prove what the repository looked like before the already-staged edits. Recreating read-only GROUND, DESIGN, and hidden-decision records had the same timing limitation.

### Approved one-run recovery

> For the 2026-09-01 reorganize-rails run only, use the staged reorganization snapshot plus the selected-warning delta as the pre-worker checkpoint. Save the selected-warning Lie-catcher verdict. Then recreate the missing read-only GROUND, DESIGN, and hidden-decision records against that checkpoint and stop if any record surfaces a new conflict. Run and save `make build` and `make test` before the implementation worker starts. This waives only the fact that the read-only records and opening proof were saved after the checkpoint edits already existed. It does not change the fixed stage order, omit a remaining stage or adversary, or create a precedent for another run.

### What each recovery action accomplished

- Saving the selected-warning Lie-catcher verdict completed the already-approved L2 check for the separate warning mechanism. It did not reopen or expand that change.
- Recreating GROUND records established the checkpoint code facts supplied to the implementation worker.
- Recreating DESIGN records checked the approved implementation plan against the checkpoint code and the owner rails.
- Recreating the hidden-decision record checked that no unapproved choice remained concealed in the contract.
- Running `make build` and `make test` established that the exact checkpoint handed to the implementation worker was green.

### What the waiver does not permit

- It does not pretend the recovered records predate the staged edits.
- It does not alter, restage, commit, or discard the staged revert point.
- It does not authorize product code, tests, analyzers, build, CI, release, or memory-store changes.
- It does not skip RULE-PHASE, ARCHITECTURE, TDD, REFUTE, GATE, or REPORT.
- It does not authorize implementation before a recovered record is reviewed.
- It does not apply to a future run.

### Approval record

Tim:

> YES, we are not working on the product code so all of those tests are of no value right now anyway.  YOU DID nothing to break it and GIT can prove thiat.
