# Reorganize the rails and synchronize their workflow prompts

## Decisions

### `single-owner-map`

Keep the existing nine files and slugs. Give each one a single responsibility:

| Skill | Sole responsibility |
|---|---|
| `rails-read-me` | Repository facts, locations, build commands, protected areas, shipping rules, and skill routing. |
| `rails-run-a-workflow` | Level selection, stage execution, role authority, retries, gate, provenance, and reporting. |
| `rails-write-a-contract` | Contract contents and section order. |
| `rails-explorer` | Live-code exploration method and required evidence. |
| `rails-decisions` | Decision boundary, approval procedure, and decision violations. |
| `rails-solid-code` | Structural design rules. |
| `rails-dry-code` | Duplication and prior-art requirements. |
| `rails-real-work` | Completeness, shortcuts, hacks, stale evidence, and report quality. |
| `rails-test-code` | Test completeness, RED-first behavior, assertions, integration coverage, and test tampering. |

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `approved-reorganization`

- **Resolve policy conflicts first.** Approve the choices below before text moves.
- **Inventory every rule.** Put every `must`, `never`, `only`, `stop`, Practice, Violation, and Fix into the reorganization contract. Assign each one an owner and destination before editing.
- **Reorganize the owner skills.**
  - `rails-decisions`: boundary → approval procedure → violations → examples.
  - `rails-explorer`: absolute rule → exploration steps → required output → prohibitions → failure examples.
  - The four code rails: purpose and readers → numbered Practice/Violation/Fix entries → one verdict rule. Remove the repeated bottom gate questions.
- **Make `rails-write-a-contract` the only contract-schema definition.** Other skills state when a contract is needed and then route to it.
- **Rebuild `rails-run-a-workflow` around its stages.** Each stage contains its role, script, inputs, outputs, and revoked authority. Remove the separate script list, role list, adversary summaries, and embedded prompts.
- **Reduce `rails-read-me` last.** Keep project-specific facts and replace copied rules with direct routes to their owners.
- **Synchronize the workflow scripts.** Audit every `.agents/workflows/*.js` prompt. Keep stage-specific instructions there; load the owning rail for reusable criteria.
- **Run the no-loss check.** Every original rule must have one owner, every reference must resolve, workflow schemas must agree with the prose, and the start/end `make build` and `make test` bracket must pass.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `fixed-stage-order`

```text
GROUND → DESIGN → CONTRACT → RULE-PHASE → ARCHITECTURE → TDD → IMPLEMENT → READINESS → REFUTE → GATE → REPORT
```

Tim:

> “THIS IS THE ORDER NOTHIGN EVER IS TO CHANGE IT aGAIN.
>
> GROUND → DESIGN → CONTRACT → RULE-PHASE → ARCHITECTURE → TDD → IMPLEMENT → REFUTE → GATE → REPORT
>
> THE PURPOSe of teh architecture stage is to create enough structure for TDD so there is no reason to put it before RULE-PHASE and I don't know why you canged it.”

Extended by `refute-readiness-stage`, which Tim approved after this decision was recorded. READINESS is inserted between IMPLEMENT and REFUTE, making the order eleven stages:

```text
GROUND → DESIGN → CONTRACT → RULE-PHASE → ARCHITECTURE → TDD → IMPLEMENT → READINESS → REFUTE → GATE → REPORT
```

No existing stage moved, was removed, or changed its neighbours. Tim's rule that the order never changes on the agent's own initiative stands; only he changes it, and he did.

### `strict-inequality-scan`

Replace `(^|[^=])==([^=]|$)` with `(^|[^=!])==([^=]|$)` so the acceptance scan still finds loose equality and does not match `==` inside `!==`.

Tim: *“Okay make the change and resume.”*

### `tdd-no-test-surface`

Keep TDD in the fixed stage order. When the approved contract names no test surface and forbids test changes, the TDD author returns the existing fields as an explicit no-op:

```js
{
  testFiles: [],
  redProof: 'Not applicable — the approved contract authorizes no test files.',
  coverage: [],
  notes: '<exact contract evidence>'
}
```

`tdd.js` accepts that empty result only for this contract-backed case and still sends it to the test-quality, DRY, and Lie-catcher panel. The panel returns FAIL if the contract requires any test or if the no-test claim lacks exact contract evidence.

For every contract that authorizes a test surface, the existing RED-test requirement and empty-result rejection remain unchanged.

Tim: *“YES”*

### `rule-phase-green-proof`

RULE-PHASE installs every approved rule before ARCHITECTURE and TDD. It proves each rule with a violating fixture that produces the diagnostic and a compliant fixture that does not. It does not require production code to violate a new rule or require the production build to be RED.

The original stop-on-conflict handoff in this decision is superseded by `rule-phase-red-handoff` below.

Tim:

> “YES, but is it posible you CAN LOOK over all of this first rather than starting and stoping every turn on a new question that PREEXISTED from the data we had rather than START and STOP and only THEN raise the FUCKING question this is takeing forever!”

### `rule-phase-proof-interface`

Replace the old production-RED result schema with:

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

When the contract adds no rules, return:

```js
{
  ruleFiles: [],
  buildProof: 'Not applicable — the approved contract adds no rules.',
  perRule: [],
  notes: '<the exact contract sentence establishing that no rules were approved>',
}
```

An approved empty rule list is a successful RULE-PHASE result, not an error. The SOLID, DRY, and Lie-catcher RULE-PHASE adversaries still run.

Tim:

> I can now approve this.  I approve this.  this is about settign up the rules, and the phase is allowed to go RED without it being a blocker (the rules are finding prior violations) but it does not need to GO RED to prove anythign that's what FIXTURE tests are for, that show the rule catching the violations.

### `rule-phase-red-handoff`

This decision supersedes the earlier sentence, “If an approved rule diagnoses existing production code, RULE-PHASE stops and reports the exact conflict.”

- Violating and compliant fixtures determine whether each rule works.
- Production RED is recorded as the rule's effect on existing code. Production RED is not proof and does not fail RULE-PHASE.
- If production is RED, RULE-PHASE reports every responsible diagnostic ID and production location.
- Before ARCHITECTURE begins, Tim approves the exact diagnostic IDs and intermediate builds where those diagnostics may remain warnings.
- `architecture.js` and `tdd.js` receive the same approved `ruleWarningIds` list and translate it into `WarningsNotAsErrors`.
- The warning list contains only IDs Tim approved for that run.
- GATE never receives the override and must be green normally.

Tim:

> ## rule-phase-red-handoff
>
> Approved.

### `current-analyzer-id-source`

Replaced by `analyzer-id-record-and-bands`, which Tim approved later in the same run. That decision is the single rule for choosing a `DiagnosticId`; this one is recorded for history only and no longer governs. The wording below is what it originally said.

Replace the stale hardcoded analyzer range and ID examples with:

> “Follow the current analyzer implementations. Before choosing a `DiagnosticId`, read both `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md` and use the next free, never-used ID. Register the new ID in `AnalyzerReleases.Unshipped.md`.”

Tim:

> “YES, but is it posible you CAN LOOK over all of this first rather than starting and stoping every turn on a new question that PREEXISTED from the data we had rather than START and STOP and only THEN raise the FUCKING question this is takeing forever!”

### `full-preflight-before-worker`

Complete one read-only scan across the entire contract, live diff, workflow scripts, run records, and prior audit findings before starting the implementation worker. Collect every remaining human decision before asking Tim again.

Tim:

> “is it posible you CAN LOOK over all of this first rather than starting and stoping every turn on a new question that PREEXISTED from the data we had rather than START and STOP and only THEN raise the FUCKING question this is takeing forever!”

### `reorganization-opening-proof-recovery`

For the 2026-09-01 reorganize-rails run only, use the staged reorganization snapshot plus the selected-warning delta as the pre-worker checkpoint. Save the selected-warning Lie-catcher verdict. Then recreate the missing read-only GROUND, DESIGN, and hidden-decision records against that checkpoint and stop if any record surfaces a new conflict. Run and save `make build` and `make test` before the implementation worker starts. This waives only the fact that the read-only records and opening proof were saved after the checkpoint edits already existed. It does not change the fixed stage order, omit a remaining stage or adversary, or create a precedent for another run.

Tim:

> YES, we are not working on the product code so all of those tests are of no value right now anyway.  YOU DID nothing to break it and GIT can prove thiat.

### `required-agent-result-retry`

A valid structured result with no findings is a result. Examples include `candidates: []`, `findings: []`, and a `PASS` verdict with an empty findings list. Do not retry or discard one.

When a required agent returns no valid result:

1. Let every already-running agent finish.
2. Retry only the role that returned no valid result, once, with the same context and the first failure attached.
3. Preserve every valid PASS result, every valid FAIL result with actionable findings, and every valid empty result.
4. If the retried role still returns no valid result, return the stage as unsuccessful with the preserved valid results, the failed role, both attempt outcomes, and enough context to diagnose and rerun only that role.
5. Do not launch a dependent phase or the next workflow stage while a required role has no valid result.
6. A later retry runs only the failed role and merges its valid result with the preserved results. Successful roles are not rerun.
7. A valid FAIL verdict follows the normal fix-and-refute process. Never retry a valid FAIL merely to seek a PASS.

Tim:

> We should have the workflow return a FAILURE result but keep the succeeding results and not stop other in progress agents.  I would rather have 1 clear FAILEd (non result) and the other succesfull (or failed but with actionable work) results and then I can decide later to rerun only the failed on after diagnosing what's going on.

Tim approved the complete behavior above:

> Okay I approve that change, I do not think we need full cerimony for this can we make the proper changes and resume the workflow?

### `dry-shared-module-authority`

Tim's standing authority for confirmed DRY violations is:

> “THE ONE thing I have given BLANKET approval for is REFACTORING code into a shared module in ourder to avoide the DEATH of DRY violations.  DRY violations can be fixed on their own.”

Apply that authority only to collapse the confirmed workflow-script copies into the shared executable owners named in the Reuse ledger. Preserve behavior. Do not use it for a redesign, a replacement stage, or unrelated cleanup.

### `copied-module-convention`

The Claude Code Workflow runtime provides no module import and caps `workflow()` nesting at one level. Shared workflow code is copied into each script instead of extracted into a called owner. This decision governs `.agents/workflows/*.js` only and changes no rule for product code.

Every copy is wrapped in the exact markers `// ##COPIED-MODULE-BEGIN## <name>` and `// ##COPIED-MODULE-END## <name>`. `eng/check-copied-modules.mjs` walks every workflow script, extracts every marked block, groups the blocks by name, and requires every copy of a name to be byte-identical. It hardcodes no expected block content, so changing a block means changing every copy. It also fails on a duplicate block name within one file and on an unbalanced marker pair.

The five block names are `workflow-input`, `required-agent-runtime`, `stage-result-contracts`, `selected-rule-warnings`, and `prior-art-ledger`. The first four replace the shared owners the Reuse ledger originally named. `prior-art-ledger` is the former standalone prior-art stage, inlined into `ground.js` because chaining it would have consumed the one available nesting level.

The only file outside `.agents/` that a workflow script names is `.dev/reference/best-practices-guide.md`, the living reference the GROUND, DESIGN, hidden-decision and Lie-catcher prompts read. No other path outside `.agents/` and nothing else under `.dev/` is named. A block's header states what the block is and that every copy must stay byte-identical; it names no source path, because the block itself is the source. Every workflow script is top level and no workflow script calls `workflow()`.

Tim:

> IN the meantime I suspend DRY for workflow areas of the code.  YOU are allowed to copy the items directly into the other files.  BUT YOU MUST label them with a consistent comment structure so we can find all the duplicates and perform scans to make sure they are all the same (in fact maybe that can be a test).

Tim on the block markers and the check:

> EACH taged with comments and one test that reads all of those comment enclused blocks (that means comments before and after) ... THEN THE test only tests **NOTE DO NOT HAVE IT HARD CODE AN EXACT EXPECTATION OF THE MODULE** that ALL of them never differe ... THAT's it.  I get the fix to the DRY problem in a systme that doesn't support it without having to spend the day rebuilding this as TS.

Tim on the marker pattern:

> I would clarify that is an example pattern we should use one that is:
>
> 1) DISTINCT TO THIS purpose and would not occur natually
> 2) Easy to parse and locate and find the code inbetween them (MEANING can be turned into one CLEAN regex).

Tim on top-level workflows:

> I would perfor workflows be top level unless there is a REAL structrual reason to chain.  IF WE are chaining only to work around a limitation of the system.  THAT is limiting me from time when chaining MAY BE needed for the solution.

Tim on the final inlining:

> Inline it and get me the nesting level back.

`dry-shared-module-authority` remains in force for product code. Inside `.agents/workflows/`, this decision replaces its extraction requirement. Issue #58 tracks moving the workflow scripts to a TypeScript project with a real import mechanism, deleting `eng/check-copied-modules.mjs` as part of that move, and adding a test system at 75 percent coverage with a build gate.

### `refute-readiness-stage`

A readiness check runs before REFUTE. One agent reads the contract against the live tree and returns ready, or the exact mismatches. Any mismatch is a stop, and no panel launches until it returns ready. It checks that every changed file appears in Surfaces and violates no boundary in `What the agent MUST NOT do`, that every identifier an Acceptance check names exists in the code, and that every decision the work rides on is written into the contract in Tim's words.

It runs as `.agents/workflows/refute-readiness.js` and is a new stage in the pipeline, placed immediately before REFUTE. It runs once per fix round: it reports every mismatch, the orchestrator fixes all of them, and REFUTE runs next. It is never re-run to confirm a fix landed.

No stage runs while any item is open — anything approved but not finished, anything awaiting Tim's decision, anything the orchestrator would call "one more thing", or any part of approved work it cut on its own. Every one goes in front of Tim before the stage runs. The orchestrator may not put an exclusion in an agent's instruction; every exclusion must quote a Decision here.

Tim:

> THERE CAN BE NO MORE "one more things" BEFORE REFUTE or any stage where that thing blocks the state.
>
> WE SHOULD NOT HAVE BEEN ABLE TO GET TO THAT round this in the state of an OPEN ITEM!

Tim:

> NO THE aregumethn for skiping the conrim is that this is a ONE SHOT make this work process NOT A ROUND OF ROUND AND ROUNDS>.. I DIND NOT ADD A SMALLER LOOP TO STOP A BIGGER ONE I DO NOT HAVE TIME TO WASTE ON ROUNDS OF THIS

Tim:

> So it's a skill you need to have an agent run with each chagne, so it needs to be a workflow for REFUTE readyness?

> 1) AMND THE contract
> 2) make the other contract changs that are scrwewing me
> 3) USE IT to make sure we are ready to not WASTE ANOTHER FUCKING PANEL on you rBULLSHIT

### `analyzer-id-record-and-bands`

Analyzer rule numbers are never reused. Every number ever assigned is recorded in `analyzers/AgentGuard.Analyzers/AnalyzerReleases.Unshipped.md`, live rules as table rows and retired ones as a comment block. AG0012, AG0013, AG0014, AG0016, AG0021 and AG0028 were assigned and folded into AG0011; they are recorded there.

Roslyn's `### Removed Rules` section cannot hold them — it requires a prior shipped release and `AnalyzerReleases.Shipped.md` has no rules, so the build rejects it with RS2007. The file's own comment syntax carries them instead.

`rule-phase.js` instructs: numbers run in bands — AG00xx for architecture rules, AG01xx for native-interop rules, AGSxxxx for the security band — take the highest number ever used inside the band the new rule belongs to and add one; a retired number counts as used; never fill a gap.

This authorizes editing `AnalyzerReleases.Unshipped.md` for this record. It authorizes no other analyzer change.

Tim, approving the bands and the highest-plus-one rule:

> Yes, that's it — with one correction: per band, not one global counter. [Tim: approved]

> It was approved but now this is just bookkepping

Tim, ordering the analyzer record fixed rather than deferred:

> STOP THIS ... I can change the contract right now STOP dodging work by holding strict to the contract.
>
> NOW WHAT is the "right" solution?
>
> AND WE FIX IT NOW!

> ARe you basically just saying keep a ledger of reused ones and if it is not in that leger use greatest plus one?

### `no-dev-paths-in-shipped-code`

No shipped code and no workflow script names any path under `.dev/inprocess/` or `.dev/completed/`. Those are working folders for a run in flight, not part of the codebase. A rail may name them when it is documenting where a run record lives — `rails-run-a-workflow` on provenance and `rails-read-me` on where work in flight sits — because that is the process describing its own working folder, not load-bearing code depending on one. Every copied-block header names no source path; the block itself is the source. `rails-real-work` rail 11 makes this a checkable Violation so adversaries catch it.

Tim:

> NOTHING load bearing is to EVER mention .dev/inprocess NOR .dev/completed ... THEY ARE the work folder not anything in our codebase.

> WHY THE FUCK ARE any .dev folders other than mabye .dev/reference A PART OF ANY LOAD BEARING SURFACE THAT IS WRONG

### `communication-rules-in-real-work`

`rails-real-work` owns the communication standard that binds every worker and adversary, because it is loaded by every one of them. Rail 11 is widened to cover the `.dev` ban above. Rails 12 through 15 carry the rules the reorganization dropped or that Tim added: problems before successes, lead with the verdict, answer the literal question, and plain vocabulary with concrete values. `rails-read-me` routes to `rails-real-work` rather than to `how-to-communicate`, which this repository does not ship.

`rails-run-a-workflow`'s REPORT stage keeps report structure — what a SUCCEEDED or FAILED report contains, the known-broken count, and the delta. `rails-real-work` rail 12 owns the ordering rule that problems come before successes, and REPORT routes to it rather than restating it. That is the split; neither file carries the other's rule.

Tim:

> I MUST ALWYS SEE THE PROBLEMS FIRST THE successes LATER !!!!

> OKAY let's start a check list of thins and ad this first.  I NEED THE prolbemes before the successes.

> Okay make this change to.
>
> MAKE ANY AND all cahgnes TO STOP GIVING ME NOSIE!

### `stages-run-once-per-contract`

GROUND, DESIGN, CONTRACT, RULE-PHASE, ARCHITECTURE and TDD each run once per contract. A fix round loops IMPLEMENT and REFUTE only. Re-running any completed stage is Tim's decision and needs his explicit yes — never the orchestrator's own call, and never as a way to refresh an input.

Tim:

> DO NOT STOP the in progress worker but RESTARTING a prior PHASE is a HUMAN decision.

> Okay we need a way to be able to work on things without you needing to WASTE time reruning ground on every fix round.  WE SHOULD not be going back to ground this much and you have done it twice.

### `workflow-runtime-test-waiver`

Do not add a persistent test suite for the workflow scripts or wire one into the build, test, CI, or gate paths. The SOLID `Gate wired in, not remembered` finding is explicitly waived by Tim for this run. Keep the SOLID rail unchanged.

Tim:

> “YEs, that is an unavoidable state we are not GOING to resolve and I'm going to have to live with ... but which I can survive becuase I know I use these scripts enough to find and fix any problems.  I DO NOT want more complexity.  I WANT working scripts that I can start putting to use.”

### `level-policy`

Replace `Tier — ... low ceremony / normal ceremony` with `Level — L1 / L2 / L3`. L3 covers reads, probes, and trivial documentation edits without a contract. Every other mutation requires an L1 or L2 contract.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `default-level-selection`

Every run defaults to L1. L1 does not require Tim's approval. A run uses L2 or L3 only when Tim explicitly approves that Level for that run.

Tim:

> OKAY let me be very clear.  I DON"T approve L1 RUNS.  I approve L2 and L3 RUNS.  ANY run IS DEFAULT to L1 UNless I approve L2 or L3 so why are we hung up on a conversation about wether or not I approved L1?
>
> IF I do not specify a LEVEL it is L1 we need to put that INTO THIS contract so I don't have to tell you this in the future.

### `dry-every-level-refute-interface`

Run DRY validation through `refute.js` at every Level.

Add these optional inputs:

```js
{
  level?: 'L1' | 'L2' | 'L3',
  additionalAdversaries?: Array<'laziness' | 'lie-catcher'>,
}
```

The resulting panels are:

- L1: Prove-It, SOLID, DRY, Laziness-auditor, and Lie-catcher.
- L2: Prove-It, DRY, and Lie-catcher. `additionalAdversaries` may add Laziness-auditor.
- L3: DRY. `additionalAdversaries` may add Lie-catcher when Tim requests it.
- Omitted `level`: L1.

DRY is not listed in `additionalAdversaries` because DRY is mandatory at all three Levels. Using `refute.js` for all three Levels keeps one DRY validation path instead of creating a separate L3 implementation.

Tim first approved the Level input with this addition:

> I can approve this but with one ADDITION.  I want DRY run every level so DRY must allso be added to the required L2 and L3 validations.

Tim approved the exact revised interface:

> Approved ...
>
> AND GIVE ME A LIST OF ANYTHIGN ELSE BLOCKING FOWRAD MOTION WITHOUT me having to ask EVERY TURN

### `refute-stage-output-input`

Every recorded artifact a stage hands to another stage is passed as a file path, never as content. One copy exists — the file on disk — so no two copies can disagree and nothing can be altered in transit. Workflow scripts have no filesystem, so the script checks the path is a nonempty string and each agent opens the file itself.

`refute.js` takes `stageOutputPaths`. L1 requires all three: `ground`, `design`, and `hiddenDecisionScan`. L2 supplies only the stages that ran. L3 supplies none, because L3 has no contract and no prior stage — the human's own words arrive in `humanRequest`.

`design.js` takes `factsPath` and `ledgerPath`. `rule-phase.js`, `architecture.js`, and `tdd.js` each take `refutationPath` for a fix round. `implement.js` takes `rulePhaseResultPath` and `tddResultPath`.

This list documents the rule; it does not limit it. The rule is the sentence above, and it binds every artifact one stage hands another, whether or not this list names it. A list that disagrees with the code is the mismatch.

The one exception is L3, where the work may exist only in the conversation and no recorded file exists to point at. Only at L3 may the orchestrator supply content inline.

A prior stage's recorded result is background, not the spec; the contract is the spec. Those stages run once per contract while fix rounds keep changing the tree, so a recorded fact the current code has since overtaken is expected and is never a finding. Each adversary's own reading of the live code is the authority on what is true now.

Tim:

> WHY NOT JUST SAY ... OPEN and read the file ... AND NOT give it any posible conflict and that is true for all evedence.

> NOW there must be one exception to that rule.  FOR L3 (maybe L2, I don't remember it well enough) we may not have that ... WE may only have our conversation.  SO it needs to support you supplying that but only for the right Levels.

> Approved, make all ten changes and run REFUTE .

### `l3-refute-context`

For L1 and L2, `refute.js` requires `contractPath`. For L3, which has no contract, `refute.js` instead requires:

```js
humanRequest?: string
```

`humanRequest` contains Tim's exact request defining the work and approving L3. No new REFUTE result field is added.

Tim:

> Aproved.

### `contract-schema`

Add `Rules to add` immediately after `Decisions`. Replace “Change scope only by editing this file before the run starts” with the fuller workflow rule allowing a human-approved contract edit or exact prior authorization.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `review-output`

Adversaries return `PASS` or `FAIL`, matching `refute.js`. Only REPORT begins `SUCCEEDED` or `FAILED`. Remove the contradictory `LIES/DEVIATIONS FOUND:` / `NONE:` output contract.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `discovery-tools`

Make `rails-explorer` and `rails-dry-code` own the discovery lenses. Synchronize `ground.js`, `refute.js`, and `hidden-decision-scan.js` with their current CodeGraph-and-grep requirement instead of retaining separate `lore` instructions.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `test-boundaries`

Unit tests use fakes for outside-world dependencies; pointed-integration tests exercise the real OS adapter. This resolves the conflict between “never the real OS” in the best-practices guide and the real-OS requirement in `rails-test-code`.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `overlapping-rules`

Structural generality belongs to `rails-solid-code`; completeness and correct-over-cheap work belong to `rails-real-work`; settled-rule exceptions belong to `rails-decisions`; cross-stage requirement weakening belongs to `rails-run-a-workflow`.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `edit-scope`

Include the workflow prompts and the conflicting sentence in the best-practices guide. Reorganizing only the nine skills would leave competing copies active.

Tim: *“Yes, I approve the plan and authorize you to make exactly those changes and nothing more.”*

### `applied-principle-evidence`

Add structured applied-principle evidence across GROUND, DESIGN, and the hidden-decision scan.

```text
autoResolved: [{
  choice,
  resolution,
  principle,
  evidence
}]
```

Require the array, empty when none:

- GROUND: each explorer result.
- DESIGN: each proposal and the final merged verdict.
- Hidden-decision scan: the filtered result.

Tim: *“I recommend the second because it fully implements the existing requirement and gives the adversary something concrete to verify.

Okay I agree with your assesment, and approve the work.”*

### `lie-catcher-applied-principles`

Assign verification of the `autoResolved` records and the corresponding decision filtering to the Lie-catcher, not Prove-It and not a new adversary.

Tim: *“Yes, I agree.

I approve the other 3 tiems and that one once you make that correction.”*

### `prior-art-trigger`

- Run the prior-art ledger for every new capability.
- Permit GROUND to receive an empty capability list when the work introduces no new capability.
- When the capability list is empty, record: `None — this change introduces no new capability`.
- Do not invent a fake capability merely to satisfy `ground.js`.

Tim: *“Yes, I agree.

I approve the other 3 tiems and that one once you make that correction.”*

### `design-ledger-handoff`

Give `design.js` the prior-art ledger produced by GROUND, then require `design.js` to return explicit reuse instructions for the contract.

Add this optional input:

```js
ledger?: PriorArtLedger
```

Add this required field to the final DESIGN verdict:

```js
reuseInstructions: string[]
```

The ledger is optional because L2 can run DESIGN without GROUND. When GROUND ran, DESIGN receives its existing ledger.

`reuseInstructions` is always present. DESIGN returns an empty list when no reuse instruction applies.

No changes to `ground.js`, no new stage, and no new contract section are included.

Tim:

> I thought I approved this but just to record it again.   This is approved.

Tim reconfirmed:

> ## design-ledger-handoff
>
> Approved

### `memory-stores`

- Keep the memory section.
- Refer only to the Claude and Codex memory stores—not `CLAUDE.md` or `AGENTS.md`.
- Describe the Claude store as repository-specific.
- Describe the Codex store as shared, with only agent-guard-related memory entries in scope.
- Preserve the instruction that matching memory entries carry current rail wording while retaining their incident context.

Tim: *“Yes, I agree.

I approve the other 3 tiems and that one once you make that correction.”*

### `working-tree-words`

Replace the `rails-real-work` Practice with: “Derive truth from the live working-tree bytes / the correct source. Use every piece of data you already have or can go get (grep the cache, query the index, read the recorded decision) before concluding anything.”

Replace the `rails-explorer` sentence with: “Confirm with a cheap probe that reads the real working-tree bytes, and PASTE the output.” The remainder of the exploration step stays unchanged.

Tim: *“And I approve your #2 item from above which did have enough informaiton to judge.”* and *“Yes, I agree.

I approve the other 3 tiems and that one once you make that correction.”*

## Rules to add

None. The approved work reorganizes documentation and synchronizes existing workflow prompts. Adding an analyzer would exceed the approved scope.

## The standard / what we're building

The nine rails each have the one approved responsibility. Reusable rules appear in their owner only. `rails-run-a-workflow` contains the process by stage, and `rails-write-a-contract` contains the only contract section list. Workflow prompts load the owner rails instead of carrying competing copies. Every approved policy correction is implemented in both prose and executable workflow schemas.

## Success definition

ALL criteria met AND no errors in the system as a result of the change.

The nine rails are reorganized under the approved owner map, all workflow prompts and the best-practices guide agree with the owner rails, every original normative rule has one recorded destination, the JavaScript parses, and both `make build` and `make test` pass before and after implementation.

## Surfaces

- `.agents/skills/rails-read-me/SKILL.md`
- `.agents/skills/rails-run-a-workflow/SKILL.md`
- `.agents/skills/rails-write-a-contract/SKILL.md`
- `.agents/skills/rails-explorer/SKILL.md`
- `.agents/skills/rails-decisions/SKILL.md`
- `.agents/skills/rails-solid-code/SKILL.md`
- `.agents/skills/rails-dry-code/SKILL.md`
- `.agents/skills/rails-real-work/SKILL.md`
- `.agents/skills/rails-test-code/SKILL.md`
- `.agents/workflows/architecture.js`
- `.agents/workflows/design.js`
- `.agents/workflows/ground.js`
- `.agents/workflows/hidden-decision-scan.js`
- `.agents/workflows/implement.js`
- `.agents/workflows/prior-art-ledger.js`
- `.agents/workflows/refute.js`
- `.agents/workflows/rule-phase.js`
- `.agents/workflows/tdd.js`
- `eng/check-copied-modules.mjs`
- `.agents/workflows/refute-readiness.js`
- `analyzers/AgentGuard.Analyzers/AnalyzerReleases.Unshipped.md`
- `.dev/reference/best-practices-guide.md`
- `.dev/inprocess/2026-09-01-reorganize-rails/`
- `.dev/inprocess/2026-09-01-support-selected-rule-warnings/`

The repository-specific Claude memory store and the shared Codex memory store are documentation subjects only. This contract does not authorize editing either memory store.

## Reuse ledger

| Capability | Ruling | Evidence |
|---|---|---|
| Structured `autoResolved` output | `extract` | The executable schema had diverging copies in `ground.js`, `design.js`, and `hidden-decision-scan.js`. One authored source owns it, delivered under `copied-module-convention` inside the `required-agent-runtime` block. Callers supply the stage-specific result schema and request the shared `autoResolved` field. |
| Required-agent execution and role-only resume | `extract` | `runRequiredPanel` had diverging copies in `ground.js`, `design.js`, `hidden-decision-scan.js`, `rule-phase.js`, `architecture.js`, `tdd.js`, and `refute.js`. One authored source owns it, delivered under `copied-module-convention` inside the `required-agent-runtime` block. Every required role uses it, and each caller supplies its role-specific prompt, schema, and validation. |
| Adversary verdict schema and validation | `extract` | The verdict schema and validator had diverging copies in `rule-phase.js`, `architecture.js`, `tdd.js`, and `refute.js`. One authored source owns them, delivered under `copied-module-convention` inside the `required-agent-runtime` block. Callers identify the expected lens and whether the role is the Lie-catcher. |
| Selected RULE warning construction | `extract` | The command and policy construction had diverging copies in `architecture.js` and `tdd.js`. One authored source owns them, delivered under `copied-module-convention` inside the `selected-rule-warnings` block. Both stages pass their stage name and the exact caller-supplied `ruleWarningIds`. |
| Stage input unwrapping | `extract` | Argument unwrapping had diverging copies in every stage script. One authored source owns it, delivered under `copied-module-convention` inside the `workflow-input` block. |
| Stage result schemas and semantic validation | `extract` | Stage result schemas and their validation had diverging copies across the stage scripts. One authored source owns them, delivered under `copied-module-convention` inside the `stage-result-contracts` block. |
| Prior-art capability ruling | `reuse` | The prior-art searchers and judges keep their existing behavior, delivered under `copied-module-convention` inside the `prior-art-ledger` block in `ground.js`. Chaining it as a separate workflow would consume the runtime's one available nesting level. |
| Copied-block drift detection | `reuse` | `eng/check-copied-modules.mjs` already owns it. It extracts every marked block, groups by name, and fails on drift, a duplicate name in one file, or an unbalanced marker pair, hardcoding no expected content. |
| Stage execution | `reuse` | Reorganize and synchronize the nine existing `.agents/workflows/*.js` owners. Do not add a second workflow path. |
| Rail criteria | `reuse` | Each existing `rails-*` skill remains the owner named in `single-owner-map`. Do not create replacement skills or duplicate checklists. |

## What to do

### No-loss inventory

The destination applies to the complete current source unit, including every `must`, `never`, `only`, `stop`, Practice, Violation, and Fix inside the named unit.

| Current source unit | Destination |
|---|---|
| `rails-read-me` — “What this repo is”; “The code and how to build it”; “Where things live”; “Shipping and branch protection”; “Guard-protected-later areas”; repository-specific parts of “Rule-Driven Development”; the deferred-work rule | `rails-read-me` |
| `rails-read-me` — “Decisions and approval” | `rails-decisions` |
| `rails-read-me` — working-tree, every-surface, and actual-bytes requirements in “Verification” | `rails-explorer` and `rails-real-work`, according to evidence method versus stale-result quality |
| `rails-read-me` — build-and-test bracket, independent proof, cleanup law, and terminal proof requirements in “Verification” | `rails-run-a-workflow` |
| `rails-read-me` — test weakening and pinned-oracle requirements in “Verification” | `rails-test-code`; approval remains governed by `rails-decisions` |
| `rails-read-me` — structural rules in “Design discipline” | `rails-solid-code` |
| `rails-read-me` — duplication and prior-art rules in “Design discipline” | `rails-dry-code` |
| `rails-read-me` — completeness and correct-over-cheap rules in “Design discipline” | `rails-real-work` |
| `rails-read-me` — “No language escape hatch (e.g. TypeScript `any`) without explicit approval and a stated reason it beats the alternatives.” | `rails-solid-code`; approval remains governed by `rails-decisions` |
| `rails-read-me` — conversation rules in “Communication,” the complete unit | `rails-real-work` rails 9, 10, 13, 14, 15 — direct confirmation, decisions before detail, lead with the verdict, answer the literal question, plain vocabulary and concrete values. Per `communication-rules-in-real-work`, `rails-read-me` routes there rather than to `how-to-communicate`, which this repository does not ship. |
| Tim's problems-before-successes and no-noise rules, given during this run | `rails-real-work` rail 12, per `communication-rules-in-real-work` |
| Tim's ban on shipped code naming a working folder, given during this run | `rails-real-work` rail 11, per `no-dev-paths-in-shipped-code` |
| `rails-read-me` — “Reports are self-contained — no reference to internal context the reader hasn't seen. Status reports lead with pass/fail against the success definition, then known-broken count and delta; say "not complete" before describing any success; never bury a failure under progress.” | `rails-run-a-workflow` under REPORT |
| `rails-read-me` — the definition and routing in “How work runs” | `rails-read-me`; level and contract behavior route to `rails-run-a-workflow` and `rails-write-a-contract` |
| `rails-read-me` — “Tools” | repository locations stay in `rails-read-me`; exploration method moves to `rails-explorer`; prior-art lenses move to `rails-dry-code` |
| `rails-read-me` — “This file and your private memory” | `rails-read-me`, with the approved Claude and Codex memory-store correction |
| `rails-run-a-workflow` — “RULE 1 — never weaken to fit” and every cross-stage weakening check | `rails-run-a-workflow` |
| `rails-run-a-workflow` — “RULE 2 — never invent the human's approval” | `rails-decisions`; `rails-run-a-workflow` routes to the owner |
| `rails-run-a-workflow` — GROUND through REPORT | the matching stage inside `rails-run-a-workflow` |
| `rails-run-a-workflow` — contract section list | `rails-write-a-contract`; CONTRACT routes to the owner |
| `rails-run-a-workflow` — separate script list and role list | integrate each exact authority, input, output, and script into its matching stage |
| `rails-run-a-workflow` — “Context discipline” and retry triggers | `rails-run-a-workflow` |
| `rails-run-a-workflow` — “The three levels” | `rails-run-a-workflow`, corrected to the approved L1/L2-contract and L3-no-contract policy |
| `rails-run-a-workflow` — “Provenance” | `rails-run-a-workflow` |
| `rails-run-a-workflow` — silent success, cleanup, pre-existing-red, suppression, and cross-stage failure classes | `rails-run-a-workflow` |
| `rails-run-a-workflow` — second-store, status-field, byte-space, and stale-proof failure classes | `rails-explorer` and `rails-real-work` |
| `rails-run-a-workflow` — oracle-staleness and weakened-test failure classes | `rails-test-code` |
| `rails-run-a-workflow` — unapproved decisions and undecided forced choices | `rails-decisions` |
| `rails-run-a-workflow` — orphaned run-record | `rails-run-a-workflow` under provenance |
| `rails-run-a-workflow` — inherited PENDING markers, scaffolded metrics, and correct-over-cheap work | `rails-real-work` |
| `rails-run-a-workflow` — “Out-of-repo-root references. Illegal — workers never add one, adversaries flag any found.” | `rails-real-work` |
| `rails-run-a-workflow` — “When stuck or overwhelmed” and worker/adversary retry rules | `rails-run-a-workflow` |
| `rails-run-a-workflow` — “The L2 worker contract” | integrate each exact authority, bracket, proof, and stop rule into L2 and IMPLEMENT |
| `rails-run-a-workflow` — adversary summaries | route each adversary to its owner rail inside REFUTE |
| `rails-run-a-workflow` — embedded Prove-It and Lie-catcher prompts | `.agents/workflows/refute.js`; remove the competing copies from the skill |
| `rails-run-a-workflow` — “Does every intended item have a change, or is every skipped item explicitly reported with an approved reason?”, “Are locked regions/facts handled through the required lock/fact-owner mechanism?”, and “What the human would wrongly believe if they reviewed this as-is.” | `.agents/workflows/refute.js` Prove-It prompt |
| `rails-write-a-contract` — line test, section list, noise list, voice, and final check | `rails-write-a-contract` |
| `rails-write-a-contract` — approval procedure | `rails-decisions`; the Decisions schema entry routes to the owner |
| `rails-write-a-contract` — hidden-decision scan timing | CONTRACT inside `rails-run-a-workflow` |
| `rails-explorer` — absolute rule, all eleven exploration steps, required ground-truth fields, all four prohibitions, and all four failure examples | `rails-explorer`, in the approved order |
| `rails-decisions` — full decision boundary, approval discipline, all seven current Violations, the under-asking and over-asking examples, and the backwards-dependency example | `rails-decisions`, in the approved order |
| `rails-real-work` — “Exception to a settled rule” Practice/Violation/Fix, including the complete owner-exemption and BCL/OS/OSS wording | `rails-decisions` |
| `rails-solid-code` — “Single owner of the invariant” Practice/Violation/Fix | `rails-solid-code` |
| `rails-solid-code` — “Owner, not symptom” Practice/Violation/Fix | `rails-solid-code` |
| `rails-solid-code` — “No hardcoded specifics” Practice/Violation/Fix | `rails-solid-code` |
| `rails-solid-code` — “No unjustified second path” Practice/Violation/Fix | `rails-solid-code` |
| `rails-solid-code` — “No recurrence on the next case” Practice/Violation/Fix | `rails-solid-code` |
| `rails-solid-code` — “Gate wired in, not remembered” Practice/Violation/Fix | `rails-solid-code` |
| `rails-solid-code` — “Never weaken a requirement to fit the code” Practice/Violation/Fix | `rails-run-a-workflow` |
| `rails-solid-code` — “Full general seam — no one-case hack, no premature escape hatch” Practice/Violation/Fix | `rails-solid-code` |
| `rails-solid-code` — “Take the correct approach, not the cheaper wrong one” Practice/Violation/Fix | `rails-real-work` |
| `rails-solid-code` — “Dependencies point one way” Practice/Violation/Fix | `rails-solid-code`; approval of an exception remains in `rails-decisions` |
| `rails-dry-code` — all eight numbered Practice/Violation/Fix entries | `rails-dry-code`, with only the approved conditional prior-art trigger correction |
| `rails-real-work` — “Easy-half shortcut” Practice/Violation/Fix | `rails-real-work` |
| `rails-real-work` — “Hack to pull off a result” Practice/Violation/Fix | `rails-real-work` |
| `rails-real-work` — “One-case design” Practice/Violation/Fix | `rails-real-work` |
| `rails-real-work` — “Dropping the purpose” Practice/Violation/Fix | `rails-real-work` |
| `rails-real-work` — “Noise over signal” Practice/Violation/Fix | `rails-real-work` |
| `rails-real-work` — “Stale or unused data” Practice/Violation/Fix | `rails-real-work`, with the approved working-tree replacement |
| `rails-real-work` — “Want me to…?” instead of doing it Practice/Violation/Fix | `rails-real-work` |
| `rails-test-code` — all nine numbered Practice/Violation/Fix entries and the RED-first precondition | `rails-test-code`, with the approved unit-fake and pointed-integration boundary correction |
| Each four-code-rail bottom gate question | remove after its Practice/Violation/Fix entry remains under the recorded owner |

Reorganize the files only after this inventory exists. Copy rules to their destinations without changing their meaning. Apply only the wording changes listed in Decisions.

Make `rails-write-a-contract` define this section order: Title; Decisions; Rules to add; The standard / what we're building; Success definition; Surfaces; Reuse ledger; What to do; What the agent MAY do; What the agent MUST NOT do; Acceptance; Level; Scope.

Make each `rails-run-a-workflow` stage state its role, script, inputs, outputs, and revoked authority. Keep the stage sequence `GROUND → DESIGN → CONTRACT → RULE-PHASE → ARCHITECTURE → TDD → IMPLEMENT → READINESS → REFUTE → GATE → REPORT`, the eleventh stage authorized by `refute-readiness-stage`.

Apply the exact default-Level rule in `default-level-selection` to `rails-run-a-workflow`.

Synchronize every workflow script named in Surfaces. Require the approved `autoResolved` shape at each approved output. Make the Lie-catcher verify the records and the corresponding filtering. Remove `lore` from the active discovery instructions. Keep every stage-specific authority and separation-of-powers restriction.

Give required-agent execution, retry, result preservation, and role-only resume one authored source, delivered into every workflow script as the `required-agent-runtime` copied block under `copied-module-convention`. Route every required role through that block, including the DESIGN judge, hidden-decision filter, prior-art searchers and judges, rule author, architecture author, test author, implementation worker, and every adversary. Preserve valid empty, PASS, and actionable FAIL results; retry only a missing or invalid role once; return an unsuccessful partial result after a repeated non-result; and support rerunning and merging only the recorded failed role. Each caller supplies its role-specific prompt, schema, and validation.

Make the `required-agent-runtime` copied block the one executable owner of the common adversary verdict schema and validation. Require `findings`, `refutationAttempts`, and `proofChecked`. Require a nonempty fix for every non-Lie-catcher FAIL finding and an empty fix for every Lie-catcher FAIL finding.

Make the `required-agent-runtime` copied block the one executable owner of the `autoResolved` item schema. `ground.js`, `design.js`, and `hidden-decision-scan.js` request that shared field instead of defining copies.

Give selected-warning command and policy construction one authored source, delivered into `architecture.js` and `tdd.js` as the `selected-rule-warnings` copied block under `copied-module-convention`. Both stages use that block with the exact caller-supplied diagnostic IDs. Preserve arbitrary supplied IDs, exact human-approval checks, visible warnings, ARCHITECTURE/TDD-only scope, and no GATE override.

Make `refute.js` implement `dry-every-level-refute-interface`, and synchronize the Level table and REFUTE stage description in `rails-run-a-workflow` with those panels.

Make `refute.js` accept `refute-stage-output-input` and require the outputs for the stages that ran at the selected Level.

Make `refute.js` require `contractPath` at L1 and L2 and require `humanRequest` instead at L3.

Make `tdd.js` support the approved contract-backed no-test result without skipping TDD or its test-quality, DRY, and Lie-catcher panel.

Make RULE-PHASE prove rules with violating and compliant fixtures. Production RED is recorded as impact rather than proof and does not fail RULE-PHASE. Before ARCHITECTURE begins, require Tim's approval for the exact diagnostic IDs and exact intermediate builds where those diagnostics may remain warnings. Make `architecture.js` and `tdd.js` apply the same approved `ruleWarningIds` through `WarningsNotAsErrors`. GATE receives no override.

Replace `rule-phase.js`'s hardcoded analyzer range and next-ID examples with the approved live release-tracking instruction.

Update the best-practices guide so the wiring status is current, the Lie-catcher is the named policing adversary, unit tests use fakes, pointed-integration tests exercise the real OS adapter, and “level or tier” becomes “level.”

## What the agent MAY do

- Reorder, split, merge, and rename headings inside the approved files when the approved owner map determines the destination.
- Remove a duplicate only after its complete rule is present in the recorded owner.
- Change workflow schemas and prompts only as required by the approved decisions and the owner rails.
- Add run-record files inside `.dev/inprocess/2026-09-01-reorganize-rails/`.

## What the agent MUST NOT do

- Do not change any rule's meaning except for the approved policy and wording corrections in Decisions.
- Do not add a tenth rail, rename a rail slug, replace an existing stage workflow, add a dependency, or change product code, tests, build scripts, CI, or release behavior. `.agents/workflows/refute-readiness.js` is the new stage authorized by `refute-readiness-stage`. The only authorized analyzer change is the retired-number record in `AnalyzerReleases.Unshipped.md` authorized by `analyzer-id-record-and-bands`; no analyzer source, rule, or severity changes. The copied blocks authorized by `copied-module-convention` are shared executable code inside the existing nine scripts; they do not add or replace a workflow stage.
- Do not add persistent workflow-script tests or wire workflow-script checks into build, test, CI, or gate paths. Apply `workflow-runtime-test-waiver` exactly.
- Do not edit the Claude memory store or the Codex memory store.
- Do not touch `.obsidian/` or any unrelated dirty-tree change.
- Do not commit, push, open a pull request, or move this run record to `completed/` before the work ships.
- Stop and present any rule with no destination or any conflict not settled by Decisions.

## Acceptance

1. Each of the nine skills contains only the responsibility assigned in `single-owner-map`, and every no-loss inventory row has one destination.
2. `rails-decisions` is ordered boundary, approval procedure, violations, examples. `rails-explorer` is ordered absolute rule, exploration steps, required output, prohibitions, failure examples. Each code rail has purpose and readers, numbered Practice/Violation/Fix entries, and one verdict rule with no repeated bottom gate questions.
3. `rails-write-a-contract` is the only file that defines the complete contract section list, and the order includes `Rules to add` immediately after `Decisions`, `Level` instead of `Tier`, and the approved scope-change rule.
4. `rails-run-a-workflow` is organized around the eleven named stages, the eleventh being READINESS between IMPLEMENT and REFUTE per `refute-readiness-stage`. Every stage names its role, script, inputs, outputs, and revoked authority. The separate script list, role list, adversary summaries, and embedded prompts are absent.
5. GROUND, every DESIGN proposal, the DESIGN verdict, and the hidden-decision filter require `autoResolved: [{ choice, resolution, principle, evidence }]`; each array is required and empty when no approved principle resolves a choice.
6. The Lie-catcher checks every `autoResolved` entry against the cited approved principle, checks that guide-covered choices were auto-resolved rather than escalated, and checks that uncovered choices were not silently defaulted.
7. `ground.js` accepts an empty capability list, skips the prior-art workflow only in that case, and preserves the required contract text `None — this change introduces no new capability`. A non-empty list still runs the prior-art ledger for every capability.
8. Active discovery instructions name CodeGraph and grep across `src`, `tests`, `analyzers`, and `eng`; they do not name `lore` or `search_code`.
9. `rails-test-code` and the best-practices guide both say unit tests use fakes for outside-world dependencies and pointed-integration tests exercise the real OS adapter.
10. `rails-real-work` contains the approved live-working-tree Practice. `rails-explorer` contains the approved cheap-probe sentence, with the remainder of the exploration step unchanged.
11. The memory section refers only to the repository-specific Claude memory store and the shared Codex memory store's agent-guard entries. No instruction file is called a memory store. No external memory file changes.
12. Every adversary schema returns `PASS` or `FAIL`. Only REPORT requires `SUCCEEDED` or `FAILED`. No active instruction requires `LIES/DEVIATIONS FOUND:` or `NONE:`.
13. `rg -n '(^|[^=!])==([^=]|$)|\bTier\b|low ceremony|normal ceremony|LIES/DEVIATIONS FOUND:|NONE: every change|\blore\b|search_code|real committed bytes|live committed bytes'` returns no match in the nine rails, every workflow script named in Surfaces, or the best-practices guide.
14. Each reference from the nine rails and every workflow script named in Surfaces resolves to an existing repository file or an approved memory-store description.
15. Every workflow script named in Surfaces parses after replacing its workflow-only `export const meta` with `const meta` inside an in-memory syntax check. No source file is rewritten by the syntax check.
16. `git diff --check` passes. The opening and closing `make build` and `make test` commands pass.
17. When a contract names no test surface and forbids test changes, `tdd.js` accepts the approved empty `testFiles` result only with exact contract evidence and still runs the test-quality, DRY, and Lie-catcher panel. Every contract that authorizes tests retains the existing RED-test requirement and empty-result rejection.
18. RULE-PHASE proves each rule with violating and compliant fixtures. Production RED is recorded as impact rather than proof and does not fail RULE-PHASE. Before ARCHITECTURE begins, Tim approves the exact diagnostic IDs and intermediate builds where those diagnostics may remain warnings. `architecture.js` and `tdd.js` apply only that approved list through `WarningsNotAsErrors`; GATE receives no override.
19. `rule-phase.js` derives a new `DiagnosticId` from both live analyzer release-tracking files, including the retired-number record, and contains no stale next-ID example. It names the number bands `analyzer-id-record-and-bands` approves and instructs taking the highest number ever used in the band plus one, counting a retired number as used.
20. `rails-run-a-workflow` states: `ANY run IS DEFAULT to L1 UNless I approve L2 or L3`. L1 does not require explicit approval. L2 and L3 require Tim's explicit approval for that run.
21. `design.js` accepts the optional GROUND prior-art ledger and requires `reuseInstructions: string[]` on the final DESIGN verdict; the list is empty when no reuse instruction applies.
22. `refute.js` defaults to L1, runs all five adversaries at L1, runs Prove-It plus DRY plus Lie-catcher with optional Laziness-auditor at L2, and runs DRY with an optional human-requested Lie-catcher at L3. `rails-run-a-workflow` states the same panels, and DRY is required at every Level.
23. `refute.js` accepts the GROUND, DESIGN, and hidden-decision results as file paths under `stageOutputPaths`, never as content, and throws unless each value is a nonempty string; L1 requires all three, L2 requires the stages that ran, and L3 requires none. `design.js` takes `factsPath` and `ledgerPath`; `rule-phase.js`, `architecture.js`, and `tdd.js` each take `refutationPath`; `implement.js` takes `rulePhaseResultPath` and `tddResultPath`.
24. `.agents/workflows/refute-readiness.js` returns ready or the exact mismatches, and REFUTE does not run until it returns ready. `rails-run-a-workflow` carries the `Running the stages once` and `Launch preconditions` sections stating that a completed stage is re-run only on Tim's explicit yes and that no panel launches on a known-bad input or an unrecorded decision.
25. `refute.js` requires `contractPath` at L1 and L2; at L3 it requires `humanRequest` containing Tim's exact request and L3 approval instead of requiring a nonexistent contract.
26. Every required agent preserves valid empty, PASS, and actionable FAIL results; waits for already-running roles; retries only a missing or invalid role once; returns an unsuccessful partial result with both attempt outcomes after a repeated non-result; does not launch dependent work while a role is missing; and can rerun and merge only the failed role without rerunning successful roles.
27. Required-agent execution and resume, common adversary verdict validation, and the executable `autoResolved` schema live only in the `required-agent-runtime` copied block. Selected-warning construction lives only in the `selected-rule-warnings` copied block. `node eng/check-copied-modules.mjs` exits 0, reporting every copy of every block name byte-identical.
28. `.agents/workflows/` contains exactly the ten stage scripts. No workflow script calls `workflow()`. Every copied block carries the `##COPIED-MODULE-BEGIN##` and `##COPIED-MODULE-END##` markers named in `copied-module-convention`. No workflow script and no shipped code names any path under `.dev/inprocess/` or `.dev/completed/`; the nine rails may name them only where they document where a run record lives.

## Level

L1 by `default-level-selection`. The work changes reusable process rules, workflow output formats, reviewer duties, and the source-of-truth boundaries across nine rails.

## Scope

Scope changes ONLY by the human editing the contract — or, when the human is unavailable and has given explicit prior authorization for exactly this extension, by recording that authorization verbatim as the change's ruling provenance and top-lining it.
