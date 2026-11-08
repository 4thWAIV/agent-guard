# Existing workflow inputs and outputs

Inventory of the eleven current .agents/workflows/*.js command entry points, read from the working tree on 2026-09-17. This describes existing interfaces, not a replacement design. IssueId is the name used here for the current issueNumber argument. Other argument spellings remain unchanged. Internal shared-runtime arguments are not additional public command inputs.

Commands are ordered by workflow position, with GROUND first. The prior-art ledger is part of GROUND and also has a standalone command. The hidden-decision scan is listed at the draft-contract checkpoint because it requires that document; the workflow skill also lists it under GROUND. CONTRACT, GATE, and REPORT have no dedicated stage script. Preflight is a contract-checking command, not a separately named stage in the sequence.

## GROUND — ground.js

Inputs:
- projectPath: repository path; required.
- IssueId (current argument issueNumber): GitHub issue number; required.
- inputFolderPath: work-item folder whose files agents read; required.
- issueScope: optional restriction to part of the issue.
- areas: nonempty list of { id, focus } investigation assignments.
- capabilities: list of { id, description }; defaults to an empty list.
- priorPanelResults, retryRoles, priorStageResult: optional resume inputs, defined below.

Content read: live issue, files in the input folder, working-tree code, exploration/decisions/DRY skills and best-practices guide. There is no dedicated contractPath argument.

Completed output: { projectPath, facts, ledger, panelComplete, failedRoles }.
- facts[]: { area, facts: [{ claim, evidence }], surfaces[], openQuestions[], autoResolved[] }.
- ledger: completed prior-art-ledger result below, or the predefined no-capability marker.
- autoResolved[]: { choice, resolution, principle, evidence }.

Interrupted output: the same top-level fields plus priorPanelResults, retryRoles, priorStageResult. During exploration ledger is null and priorStageResult is null. During ledger interruption the partial ledger is retained and priorStageResult contains { facts, ledgerStageResult }. The nested ledger also contains its own resume fields. Some data is therefore repeated in the result and checkpoint fields.

Downstream: DESIGN receives recorded facts and ledger by file path. CONTRACT uses the findings. REFUTE receives a recorded GROUND result path.

## GROUND reuse search — prior-art-ledger.js

GROUND executes an inlined copy; it does not need a second standalone invocation.

Inputs:
- projectPath: required.
- capabilities: nonempty list of { id, description }; required for standalone invocation.
- brief: optional caller-provided briefing text. GROUND supplies its generated issue/folder/scope instructions here.
- priorPanelResults, retryRoles, priorStageResult: optional resume inputs.

Search-agent output: { capability, candidates: [{ lens, file, line?, symbol, snippet }], lensesRun[], lensesEmpty[], lensErrors[] }.

Judge output: { capability, decision, owner?, copies[]?, evidence, confidence }; decision is reuse, extract, or new.

Completed command output: { projectPath, verdicts[], panelComplete, failedRoles }. The search-agent results are passed to the judges but are not included in this completed return.

Interrupted output additionally retains searchResults, priorPanelResults, retryRoles, and priorStageResult. If search is incomplete, verdicts is empty and priorStageResult is null. If judging is incomplete, priorStageResult contains { searchResults }.

Downstream: GROUND includes the ledger result; DESIGN reads it and derives reuse instructions.

## DESIGN — design.js

Inputs:
- projectPath, IssueId (issueNumber), inputFolderPath: required.
- issueScope: optional.
- factsPath, ledgerPath: optional file paths; GROUND results when that stage ran.
- angles: optional list of { id, focus, model? }; defaults to the script's three design angles.
- priorPanelResults, retryRoles, priorStageResult: optional resume inputs.

Proposal output: { angle, approach, keySteps[], rulesToAdd[], risks[], autoResolved[] }.
- rulesToAdd[]: { rule, failureModeStopped, analyzer? }.

Judge output: { winningAngle, why, synthesizedApproach, graftedFrom[], rulesToAdd[], risks[], reuseInstructions[], autoResolved[] }.

Completed command output: { proposals[], verdict, panelComplete, failedRoles }.

Interrupted output retains proposals, completion/failure fields, and the three resume fields. A judge interruption also returns verdict: null and priorStageResult: { proposals }; an interruption while proposing has priorStageResult: null.

Downstream: CONTRACT uses the selected design, rules and reuse instructions. REFUTE receives the recorded DESIGN result path.

## CONTRACT checkpoint — hidden-decision-scan.js

Inputs:
- projectPath, contractPath: required.
- priorPanelResults, retryRoles, priorStageResult: optional resume inputs.

Hunter output: { lensId, candidates: [{ choice, forcedBy, category, evidence }] }.

Filter output: { findings[], droppedAsNoise, autoResolved[] }.
- findings[]: { choice, whyForced, options[], howItBites, recommended, severity }.

Completed command output: { contractPath, findings[], autoResolved[], droppedAsNoise, panelComplete, failedRoles }. The hunters' candidate lists are not included in the completed return.

Interrupted output has empty findings/autoResolved, droppedAsNoise: null, completion/failure fields and the three resume fields. priorStageResult is null during hunting or { hunted } when the filter is incomplete.

Downstream: unresolved choices go to the human before the contract advances. REFUTE receives this result by file path.

## CONTRACT checkpoint — preflight-contract.js

Inputs:
- projectPath, contractPath: required.
- priorPanelResults, retryRoles: optional resume inputs.

Completed output: { contractPath, sound, defects[], checksRun[], panelComplete, failedRoles }.
- defects[]: { kind, contractSays, ruleSays, fix }.

Interrupted output: the shared-runtime panel object, not the completed output shape. Its fields are panelComplete, results, failedRoles, priorPanelResults, retryRoles, nextGroupInput.

Downstream: the coordinator uses the contract-check result before building against the contract. Later stages read contractPath, not this preflight result as a dedicated argument.

## CONTRACT — no dedicated script

Inputs defined by rails-run-a-workflow: requirement, grounded facts, surfaces, reuse ledger, selected design, proposed rules, approved decisions, unresolved hidden-decision findings.

Outputs: contract.md and the opening build/test evidence. These are workflow artifacts, not a JavaScript command's return object.

## RULE-PHASE — rule-phase.js

Inputs:
- projectPath, contractPath: required.
- rulesToAdd: required list of rule descriptions; may be empty only under the no-rule conditions.
- noRuleEvidence: conditional approved contract excerpt for an empty rule list.
- refutationPath: optional correction-instructions file.
- priorPanelResults, retryRoles, priorStageResult: optional resume inputs.

Coding-agent output: { ruleFiles[], buildProof, perRule[], notes? }.
- perRule[]: { rule, diagnosticId, violatingFixture, violatingProof, compliantFixture, compliantProof }.

Completed command output: coding-agent fields plus verdicts[], anyFail, panelComplete, failedRoles. Verdict structure is defined below.

Interrupted output retains coding-agent fields where available, verdicts, anyFail: true, completion/failure fields and the three resume fields. Before a valid coding result, those work fields are empty and priorStageResult is null; during review it contains the coding-agent result.

Physical outputs: analyzer/rule files and fixtures. buildProof and fixture proofs are strings in the agent result, not separate execution receipts enforced by this schema.

Downstream: ARCHITECTURE reads the live analyzer fence; IMPLEMENT receives rulePhaseResultPath.

## ARCHITECTURE — architecture.js

Inputs:
- projectPath, contractPath: required.
- ruleWarningIds: optional list, default empty.
- refutationPath: optional correction-instructions file.
- priorPanelResults, retryRoles, priorStageResult: optional resume inputs.

Coding-agent output: { skeletonFiles[], signatureDiff, buildProof, perType[], notes }.
- perType[]: { type, role, bodies }.

Completed result assembled by the script: coding-agent fields plus verdicts[], anyFail, panelComplete, failedRoles. Its final validator requires a passing aggregate; a completed panel with a FAIL therefore throws rather than returning that assembled result normally. Individual agent results have already been produced.

Interrupted output retains available coding-agent fields and verdicts, anyFail: true, completion/failure fields and the three resume fields. priorStageResult holds the coding-agent result during review or null before a valid coding result.

Physical outputs: approved application signatures/skeletons, or no files for a no-signature result. signatureDiff and buildProof are agent-returned strings.

Downstream: human signature approval and the live application surface used by TDD. TDD has no dedicated architecture-result path argument.

## TDD — tdd.js

Inputs:
- projectPath, contractPath: required.
- ruleWarningIds: optional list, default empty.
- noTestEvidence: conditional approved contract excerpt for the no-test path.
- refutationPath: optional correction-instructions file.
- priorPanelResults, retryRoles, priorStageResult: optional resume inputs.

Coding-agent output: { testFiles[], redProof, coverage[], notes }.
- coverage[]: { acceptanceItem, tests[], kind, red }.

Completed command output: coding-agent fields plus verdicts[], anyFail, panelComplete, failedRoles.

Interrupted output retains available coding-agent fields and verdicts, anyFail: true, completion/failure fields and the three resume fields. priorStageResult holds the coding-agent result during review or null before a valid coding result.

Physical outputs: test files. redProof is an agent-returned string containing test evidence, not a schema-enforced execution capture.

Downstream: IMPLEMENT receives tddResultPath and the live tests.

## IMPLEMENT — implement.js

Inputs:
- projectPath, contractPath: required.
- level: L1 or L2; defaults to L1.
- rulePhaseResultPath: required at L1 unless the recorded omission authorization is supplied; not accepted at L2.
- tddResultPath: required at L1; optional at L2.
- omittedStageAuthorizations: optional object whose only permitted member is rulePhase, containing authorization text.
- instruction: optional additional direction; text is used directly, other supplied values are JSON-serialized.
- priorPanelResults, retryRoles: optional resume inputs.

Completed command output: { changedFiles[], buildProof, testProof, acceptance[], walls[], complete }.
- acceptance[]: { item, met, evidence }.
- This successful return has no panelComplete or failedRoles wrapper.

Interrupted output: empty work arrays/strings, complete: false, panelComplete: false, failedRoles, priorPanelResults, retryRoles.

Physical outputs: implementation changes. buildProof and testProof contain agent-supplied command-output strings with exit codes as requested by the prompt.

Downstream: READINESS and REFUTE inspect the changed working tree and contract. Neither has a dedicated implement-result path argument.

## READINESS — refute-readiness.js

Inputs:
- projectPath, contractPath: required.
- decisionsGiven: optional list of approval text from conversation.
- panelInstruction: optional proposed adversary instructions.
- priorPanelResults, retryRoles: optional resume inputs.

Completed command output: { contractPath, ready, mismatches[], checksRun[], panelComplete, failedRoles }.
- mismatches[]: { kind, contractSays, treeSays, fix }.

Interrupted output: shared-runtime panel object with panelComplete, results, failedRoles, priorPanelResults, retryRoles, nextGroupInput.

Downstream: coordinator determines whether REFUTE may launch. REFUTE has no dedicated readiness-result argument.

## REFUTE — refute.js

Inputs:
- projectPath: required.
- level: L1, L2 or L3; defaults to L1.
- contractPath: required at L1/L2.
- humanRequest: required at L3, carrying the request and L3 approval.
- changedFiles: required list.
- stageOutputPaths: named file paths; permitted members are ground, design, hiddenDecisionScan. All three are required at L1; L2 supplies stages that ran; L3 supplies none.
- additionalAdversaries: optional list, default empty; only laziness and lie-catcher are recognized, with level-specific restrictions.
- priorPanelResults, retryRoles: optional resume inputs.

Completed command output: { contractPath, verdicts[], anyFail, failedLenses[], panelComplete, failedRoles }.

Interrupted output: the same fields plus priorPanelResults and retryRoles; anyFail is true and failedLenses also includes missing reviewer roles.

Downstream: GATE uses the original verdicts, contract, working tree and verification evidence.

## GATE and REPORT — no dedicated scripts

GATE inputs: adversary verdicts, contract, live working tree, rerunnable proof. Outputs: acceptance/rejection and closing build/test command output with exit codes.

REPORT inputs: contract success definition, gate result, verdicts, proof, run record. Output: the terminal report. Neither is a public JavaScript command interface in .agents/workflows.

## Shared result and resume shapes

- Reviewer verdict: { lens, verdict, findings: [{ summary, evidence, fix }], observations?: [{ summary, evidence, nonBlockingReason }], refutationAttempts[], proofChecked[] }. verdict is PASS or FAIL. The schema allows observations to be absent on retained older results; newly dispatched reviewers are instructed to return it.
- failedRoles[]: { role, attempts: [{ attempt, result, problem }] }. Failed result validation is different from a valid reviewer FAIL verdict.
- priorPanelResults: accepted results for agents not being rerun in the current group.
- retryRoles: roles being retried.
- priorStageResult: completed earlier groups within this same stage execution, not a result from a different workflow phase. Its shape varies by command as listed above.
- nextGroupInput: internal runtime forwarding object, normally empty or containing priorPanelResults and retryRoles for a later group. It leaks into interrupted preflight/readiness returns because those commands return the runtime object directly.

Scripts also emit progress log messages and invoke agents whose tool activity belongs to the execution runtime. Input/validation failures can throw. The command objects listed here do not themselves guarantee that logs, build/test captures, or agent results are written to named files. Ordinary agents read code, contract/brief files and the skills named by their prompts; these are content inputs even when not separate command arguments.

## Parameters common now, and applicability to a workflow interface

| Existing parameter | Where it appears | Applicability |
|---|---|---|
| projectPath | All eleven commands | Workflow-level; already one of Tim's four fields. |
| priorPanelResults, retryRoles | All eleven commands | Legacy resume arguments, replaced in the new design by Workflow's registered work and agent-run tracking. See conversation.md, "Attempts and resumption". |
| priorStageResult | GROUND, prior-art ledger, DESIGN, hidden-decision scan, RULE-PHASE, ARCHITECTURE, TDD | Same-stage checkpoint data; only multi-group commands need this current form. Not a base workflow field. |
| IssueId (issueNumber) | GROUND and DESIGN | Shared work identity even though later commands rely on the contract instead. Already one of Tim's four fields. |
| inputFolderPath | GROUND and DESIGN | Assigned to IWorkflow.WorkingFolder. Later commands currently receive individual file paths rather than this folder. |
| level | IMPLEMENT and REFUTE | Workflow-wide selection, now represented by IWorkflow.Level. Earlier scripts rely on the caller to dispatch stages appropriately. |
| contractPath | Hidden-decision scan, preflight, RULE-PHASE, ARCHITECTURE, TDD, IMPLEMENT, READINESS, REFUTE | Shared document reference once the contract exists; not available at initial intake. Consider with the document types later. |
| issueScope | GROUND and DESIGN | Belongs in the user-representing conversation/contract, as directed; not another base field. |
| refutationPath | RULE-PHASE, ARCHITECTURE, TDD | Correction-specific input. |
| ruleWarningIds | ARCHITECTURE and TDD | Intermediate-build permission for those stages, not global identity. |

All other parameters above are phase-specific inputs or evidence. Neither IssueText nor WorkflowId exists as a public argument in the eleven scripts. They are the new fields Tim explicitly requested, not additional fields inferred from this inventory.

## L1, L2 and L3 stage selection for the new workflow

| Stage | L1 | L2 | L3 |
|---|---|---|---|
| GROUND | Required | Required | Absent |
| DESIGN | Required | When needed | Absent |
| CONTRACT | Required | Required | Absent |
| RULE-PHASE | Required unless Tim approves omission | Absent | Absent |
| ARCHITECTURE | Required | When needed | Absent |
| TDD | Required | When needed | Absent |
| IMPLEMENT | Required | Required | Coordinator acts directly; edits limited to small changes as directed by human |
| READINESS | Required | Required | Absent |
| REFUTE | Required | Required | Required |
| GATE | Required | Required | Required |
| REPORT | Required | Required | Required |

GROUND is required at both L1 and L2, including prior-art search. This is the requested change from the current workflow, whose L2 definition omits GROUND.

## Existing L1, L2 and L3 reviewers within REFUTE

| Reviewer | L1 | L2 | L3 |
|---|---|---|---|
| Prove-It | Required | Required | Absent |
| SOLID | Required | Absent | Absent |
| DRY | Required | Required | Required |
| Laziness | Required | When needed/requested | Absent |
| Lie-catcher | Required | Required | When Tim requests it |

These reviewers belong to REFUTE. Earlier stages can have their own reviewers; they are not listed in this reviewer table. The reviewer table describes the existing process. The stage table retains the existing selections except for the requested addition of GROUND at L2. Sources for the existing selections: .agents/skills/rails-run-a-workflow/SKILL.md (Level selection and Stage sequence) and .agents/workflows/refute.js (panelIds).

## Current design definitions

[IWorkflow.cs](IWorkflow.cs) owns the workflow interface; its fields are not copied into this inventory. [IConversation.cs](IConversation.cs) owns the instruction structure. The approved [prior-art input ownership](conversation.md#prior-art-input-ownership--no-separate-input-or-assignment-file) distinguishes instructions from execution tracking and references, without separate prior-art input or assignment files.

The command inventory above describes the existing scripts, not the new file design. Legacy argument and output names remain here as evidence of those interfaces.
