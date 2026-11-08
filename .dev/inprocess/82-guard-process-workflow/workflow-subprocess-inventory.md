# Workflow subprocesses and GROUND file consumers

This is a review inventory, not a new execution specification. Labels identify work in this document; they do not introduce agent roles, runtime fields or additional output files. Existing structures and process definitions remain the sources linked below.

## Does GROUND need this many files?

The diagram lists more things than physical files. GROUND evidence is a collection, receipts are return values, and assignments live inside Workflow. None needs another file merely because it appears in the diagram.

The replacement design has four agent-result kinds: exploration, prior-art search, reuse evaluation and platform/API findings. Search and reuse evaluation are separate because they come from different agents and the evaluation consumes the original search result. Tool evidence is separate from the agent's interpretation. The number of files varies with assignments, agent runs and tool calls.

Two storage choices deserve examination rather than an automatic new file:

- `PriorArtLedger` contains only result references. DESIGN currently consumes it through `DesignAssignment.PriorArtLedger`, so it has a consumer. Whether that extra index earns its place alongside Workflow's result tracking remains a design question. Removing it would require changing that handoff, not merely deleting an unused file.
- Platform/API research retains the requested operation, actual backend request, response and capture separately. The distinction between those data is useful, but the actual backend request has no separately identified reader in the current process inventory. A separate physical file for that request has not demonstrated the same value as the original response. Prior-art already keeps its executed command inside its capture. This inventory does not merge or remove either design.

## What counts as a subprocess

A subprocess here is a separately identifiable execution responsibility: an agent task, a tool operation, or a coordinator/Guard operation. It does not mean an operating-system process.

Writing a result at the end of an agent task is not another agent. A retry repeats a subprocess; it does not add a subprocess type. Reviewers are counted separately from coding/planning agents. Human approval is not code execution and is not counted as a subprocess.

Current scripts and the replacement design are distinguished below. The draft Guard operations are not implemented by the current workflow scripts.

## GROUND: labeled execution flow

| Group | Subprocess | Executor | Reads | Produces |
|---|---|---|---|---|
| `GR-EX` Exploration | `GR-EX-EXPLORE` | Exploration agent, per investigation area | Conversation investigation/scope; existing source | Original exploration result |
| `GR-PA` Prior art | `GR-PA-SEARCH` | Code-search agent, per capability | Assigned Conversation version; tool captures | Search requests during execution; original candidate/search result at completion |
| `GR-PA` Prior art | `GR-PA-EXECUTE` | Guard search tool | Search request; Workflow assignment/project; scoped source | Saved request and execution capture; receipt returned to caller |
| `GR-PA` Prior art | `GR-PA-EVALUATE` | Reuse-judge agent, per capability | Original search result; Conversation; captured evidence | Original reuse evaluation |
| `GR-PA` Prior art | `GR-PA-INDEX` | Guard | Saved search/evaluation references | Prior-art ledger references |
| `GR-API` Platform/API research | `GR-API-RESEARCH` | Research agent | Conversation capability/platforms; original tool responses | Research requests during execution; original findings at completion |
| `GR-API` Platform/API research | `GR-API-DOCS` | Guard documentation search | Documentation query and domains | Research capture and backend response |
| `GR-API` Platform/API research | `GR-API-SDK` | Guard SDK search | Search root, pattern and match kind | Research capture and backend response |
| `GR-API` Platform/API research | `GR-API-CORPORA` | Guard reference-library listing | Registered assignment | Research capture and available-corpora response |
| `GR-API` Platform/API research | `GR-API-LIBRARY` | Guard reference-library search | Corpus ID and query | Research capture and search response |
| `GR-API` Platform/API research | `GR-API-READ` | Guard source reader | Web, local or reference-library source | Research capture and original source response |
| `GR-RUN` Execution tracking | `GR-RUN-TRACK` | Guard | Workflow assignments and existing agent runs | Updates to the same Workflow document |

Counts at these boundaries: **GR-EX: 1; GR-PA: 4; GR-API: 6; GR-RUN: 1.** These are twelve responsibility/operation types in the replacement design, not twelve agents or twelve files. `GR-PA-EXECUTE` accepts two request variants through one method; `GR-API` exposes five tool methods. Repeated calls do not increase these type counts.

Exploration evidence capture is not counted as another defined subprocess: its capturing operation and storage shape remain undefined. GROUND's exact exit-check implementation is also not defined; this inventory does not invent one.

### What actually runs today

[`ground.js`](../../../.agents/workflows/ground.js) dispatches three agent-task types:

| Draft label | Current entry point | Result handling |
|---|---|---|
| `GR-EX-EXPLORE` | `explorePrompt(area)` | Explorer results enter the returned `facts` collection. |
| `GR-PA-SEARCH` | `gatherPrompt(cap)` inside `__priorArtLedger` | Search object is passed to the reuse judge. |
| `GR-PA-EVALUATE` | `evaluatePrompt(cap, found)` | Evaluation enters the returned ledger. |

The agents run their search tools today. The replacement Guard capture operations above are draft interfaces, not existing `ground.js` subprocesses. `ground.js` assembles returned objects; it does not itself write these proposed YAML files. Its completed aggregate retains facts and the ledger, but omits original search results. Incomplete/resume results retain search state. Platform/API research has no corresponding dedicated dispatch in this script.

## Every GROUND output: storage and consumers

Names below identify artifact roles or C# types, not newly assigned filenames. Exact filenames should come from the saved run/result references, not a second filename registry here.

| Output | Where it lives in the design | Producer | Reader and use | Separate file justified? |
|---|---|---|---|---|
| Exploration result | Original agent-result file, referenced by `DesignAssignment.ExplorationResults` | `GR-EX-EXPLORE` | DESIGN proposal agents use facts, affected surfaces and questions. | Yes as an original agent result; replacement result schema remains undefined. |
| Prior-art search request | Saved request file, located by `PriorArtSearchCapture.RequestPath` | `GR-PA-SEARCH`, persisted by `GR-PA-EXECUTE` | `GR-PA-EXECUTE` runs the request; capture inspection can recover what was requested. | Explicit in `PriorArt.cs`; not an extra copy of Conversation. |
| Prior-art capture | Tool-written capture file referenced by `SearchExecution.CapturePath` and `EvaluationEvidence.CapturePath` | `GR-PA-EXECUTE` | `GR-PA-SEARCH` interprets matches/failures; `GR-PA-EVALUATE` checks supporting evidence. | Yes: original tool evidence, not an agent account. Command, stdout, stderr and status are fields inside this file. |
| Prior-art search result | Original `PriorArtSearchResult` file | `GR-PA-SEARCH` | `GR-PA-EVALUATE` consumes candidates and evidence references; `GR-PA-INDEX` links it; DESIGN follows the ledger to it. | Yes: independent search output needed by the reuse judge. |
| Reuse evaluation | Original `PriorArtEvaluationResult` file | `GR-PA-EVALUATE` | `GR-PA-INDEX` links it; DESIGN uses candidate behavior, limitations and reuse determination. | Yes: independent judge output; do not rewrite it into a second summary. |
| Prior-art ledger | `PriorArtLedger` file containing reference pairs | `GR-PA-INDEX` | DESIGN locates exact search and evaluation results through `DesignAssignment.PriorArtLedger`. | Has a defined consumer; its separate-index value versus Workflow needs review before removal. |
| Platform/API request | File identified by `PlatformResearchCapture.Request` | `GR-API-RESEARCH`, persisted by Guard | Selected `GR-API-*` tool executes it; capture links the original request. | Explicit in `PlatformResearch.cs`. |
| Actual platform/API backend request | File identified by `PlatformResearchCapture.ExecutedRequest` | Selected `GR-API-*` tool | Capture references it. No separate reader is specified in the process inventory. | Separate-file value not yet established. A reference alone is not a demonstrated read. |
| Platform/API response | Original response file identified by `ResearchCompleted.Response` or `ResearchFailed.Response` | Selected `GR-API-*` tool | `GR-API-RESEARCH` reads source content to produce platform findings. | Yes: retain original external evidence separately from interpretation. A failure may have no response. |
| Platform/API capture | `PlatformResearchCapture` file | Selected `GR-API-*` tool | `GR-API-RESEARCH` reads outcome and follows the response reference. | Defined capture record; current draft stores links rather than repeating request/response bytes. |
| Platform/API findings | Original research-agent result file, referenced by `DesignAssignment.PlatformResearchResults` | `GR-API-RESEARCH` | DESIGN uses supported primitives, limitations and platform differences. | Yes as original agent output; result schema and exact evidence fields remain undefined. |
| Exploration evidence | Storage not yet defined | Capturing operation not yet defined | Intended to support exploration claims; exact reader binding is undefined. | Do not count it as an established additional file yet. |
| GROUND tool captures | Collection label covering the captures above | No independent producer | Readers consume the individual captures. | **No additional file.** |
| Search/research receipt | Method return value | Guard tool | Calling agent gets the saved capture reference. | **No additional file.** |
| Assignments and agent-run status | Existing Workflow document | `GR-RUN-TRACK` | Guard dispatches/resumes work; agents/tools resolve registered inputs; later stages locate results. | **No separate assignment or status file.** |

DESIGN's explicit input relationships are defined by `DesignAssignment` in [StageExecution.cs](StageExecution.cs). Further CONTRACT/REFUTE evidence consumption appears in the process draft, but its exact replacement bindings remain unsettled; it is not used here to claim additional established consumers.

## Stage-prefixed subprocess inventory across the workflow

This table groups the existing process inventory without treating each input/output arrow as another execution. It does not authorize new work. Counts describe task types shown, not the number of agents launched. Reviewer fan-out varies with WorkflowLevel. A correction reuses the relevant work type with the findings as input.

| Stage | Labeled subprocesses | Count and definition boundary |
|---|---|---|
| CONVERSATION | `CV-INTAKE` Create initial Conversation and Workflow | 1 combined intake responsibility; not an existing stage script. Human decisions are not another code subprocess. |
| GROUND | `GR-EX`, `GR-PA`, `GR-API`, `GR-RUN` expanded above | 12 replacement responsibility/operation types; 3 agent-task types in current `ground.js`. |
| DESIGN | `DS-PROPOSE` Produce proposals; `DS-SELECT` select design; `DS-REVIEW` enabled independent reviews | 2 agent-task types in `design.js`, plus review fan-out required by the design process. New reviewer input bindings remain unsettled. |
| CONTRACT | `CT-FORM` form/update contract; `CT-HUNT` hidden-decision search; `CT-FILTER` evaluate candidates; `CT-CHECK` preflight; `CT-VERIFY` opening build/tests; `CT-EXIT` resolve exit | 6 responsibility types. Current executable scripts cover hunter/filter and preflight; contract writing, verification and exit are coordinator work. Replacement bindings remain partly undefined. |
| SPEC | `SP-PLAN` BDD planning | 1 agent-task type in the replacement design. Human guidance updates that plan; no separate current SPEC script. |
| ARCHITECTURE | `AR-WRITE` write surface and return determination; `AR-BUILD` compile; `AR-REVIEW` enabled reviewers; `AR-EXIT` consume reviews | 3 non-review responsibility types plus reviewer fan-out. Current `architecture.js` dispatches author and reviewers; new capture/exit bindings remain unsettled. |
| RULE_PLAN | `RP-PLAN` plan rules | 1 agent-task type in replacement design; no separate current script. |
| RULE | `RL-WRITE` write rules and return result; `RL-EXECUTE` run checks; `RL-EVALUATE` investigate Unknown/unexpected outcomes; `RL-REVIEW` enabled reviewers; `RL-EXIT` consume reviews | 4 non-review responsibility types plus reviewer fan-out. Independent outcome evaluation belongs to the replacement design, not an existing separate role in `rule-phase.js`. |
| TEST_PLAN | `TP-PLAN` detailed test planning | 1 agent-task type. BDD/detail-plan coordination is an unresolved responsibility, not a second invented agent. |
| TEST | `TS-SPEC` write spec tests; `TS-UNIT` write unit tests; `TS-EXECUTE` run checks; `TS-EVALUATE` investigate Unknown/unexpected outcomes; `TS-REVIEW` Test Reviewer and enabled reviewers; `TS-EXIT` consume reviews | 5 non-review responsibility types plus reviewer fan-out. Current `tdd.js` has one test author; the two coding tasks are the replacement design. |
| IMPLEMENT | `IM-WRITE` implement and return result; `IM-VERIFY` build/test/rule checks; `IM-EXIT` check completion | 3 responsibility types; verification and returning a result need not launch separate agents. Replacement result bindings remain unsettled. |
| READINESS | `RD-CHECK` reconcile inputs and work state; `RD-EXIT` consume readiness result | 2 responsibility types; current script dispatches one checking agent. |
| REFUTE | `RF-REVIEW` enabled independent reviewers; `RF-VERIFY` verification; `RF-ADJUDICATE` evaluate findings against source | Reviewer fan-out plus 2 responsibilities. A reviewer's verification is work within that review, not necessarily another launched agent. |
| GATE | `GT-VERIFY` closing verification when performed; `GT-DECIDE` acceptance decision | 2 responsibilities; no separate current workflow script. |
| REPORT | `RT-PRESENT` present outcome | 1 coordinator responsibility; no separate current workflow script. |
| REVIEW | No subprocess defined | Unknown, **not zero required work**. Only the terminal state is defined. |

### Shared execution tracking

`<stage>-RUN` denotes the same Guard responsibility in GROUND, DESIGN, CONTRACT, SPEC, ARCHITECTURE, RULE_PLAN, RULE, TEST_PLAN, TEST, IMPLEMENT, READINESS and REFUTE. It reads registered work and records attempts, agent runs and result references in Workflow. It is counted in GROUND's detailed table; add one tracking responsibility to each other listed stage, not another tracking implementation or file per stage.

Workflow updates for GATE, REPORT and REVIEW are not mapped yet. This inventory does not interpret that missing mapping as permission to omit state updates.

## GROUND design proposal — for review, not adopted

**Revision:** the separate Exploration Item recommended below is withdrawn from the latest proposal. The [full GROUND flow](ground-workflow.html) places code behavior, dependencies and change-impact investigation inside Prior Art, retains the reuse judge within Prior Art, and shows all Guard persistence/control operations. That page owns the revised Step flow. Earlier sections below retain the reasoning being reconsidered; they do not define the latest proposed grouping. C# types, runtime scripts and approval rules are unchanged. The search-result and DESIGN handoff types still require explicit revision before that consolidation could be implemented.

This section proposes the GROUND design after evaluating the inventory above. It does not replace the current C# drafts, diagram definitions, approved transition permissions or workflow scripts. The earlier sections remain an inventory of those sources, not a claim that this proposal has been adopted.

### Stage purpose and output boundary

GROUND supplies DESIGN with the existing-code facts, reuse determinations and platform primitives needed to choose an implementation approach. It does not choose the application architecture, implement code or rewrite human requirements.

| Item | Key output | What DESIGN uses it for |
|---|---|---|
| `GR-EX` Exploration | Original exploration results, one per assigned area | Understand existing behavior, dependencies and constraints affecting the change. |
| `GR-PA` Prior Art | Prior-art ledger linking each original search result and reuse evaluation | Identify existing owners and justified reuse, extension, extraction or new implementation. |
| `GR-API` Platform/API Research | Original platform findings, one per assigned capability covering the selected platforms | Identify available primitives, limitations and unsupported behavior before choosing the common abstraction. |
| `GR-STATE` Execution state and handoff | Updated Workflow | Locate the exact results, unfinished assignments and next-stage input references. This is shared Guard machinery, not a fourth investigation or another report. |

The ledger remains in this proposal. Its specific job is pairing the selected search and evaluation for each capability, while Workflow retains execution attempts. It carries references only. Removing it is not necessary to simplify the agent work, and would change the existing `DesignAssignment` handoff.

### GR-STATE — shared operations, explicitly present

These operations surround the Item work rather than running only after the investigations. Each operation has one implementation; its call sites remain visible below.

| Step | Executor and action | Inputs | Output or update | Consumer |
|---|---|---|---|---|
| `GR-STATE-REGISTER` | Guard registers required work without rewriting its instructions. | Workflow; referenced Conversation investigation areas, capabilities and platforms. | Assignments inside the current Workflow attempt. | Dispatch and the assigned agents/tools. |
| `GR-STATE-DISPATCH` | Execution mechanism starts an authorized assignment, or resumes missing work. | Registered assignment; its bound input references; required human authorization. | Agent invocation; AgentRun start/status in Workflow. | The relevant Item Step; completion tracking. |
| `GR-STATE-CAPTURE` | Guard executes an allowed typed tool request and retains the actual response. | Request from an Item Step; assignment; referenced scope; selected source/backend. | Saved request, capture and, for platform research, original response. Receipt returned only after saving. | The requesting agent and subsequent evidence readers. |
| `GR-STATE-SAVE` | Guard validates the submitted result's structure and references, then stores the exact result. | Agent's structured output; assignment; referenced captures/results. | Original result file; Workflow result reference and agent-run status/end time. Invalid submissions are not marked completed. | Reuse judge, ledger generation or DESIGN; dispatch when a run needs resumption. |
| `GR-STATE-EXIT` | Guard checks registered work and unresolved GROUND obligations; human approves transition. | Workflow assignments/results; original unresolved determinations; recorded resolutions and approval. | Workflow transition state, or the specific assignment/result preventing transition. No new narrative report. | Handoff or the authorized correction/resumption. |
| `GR-STATE-HANDOFF` | Guard registers DESIGN's inputs without rewriting findings. | Selected original exploration/research results; prior-art ledger; approved transition. | `DesignAssignment` references inside Workflow. | DESIGN agents. |

These describe responsibilities, not new command signatures. Mechanical validation establishes shape, identity and presence, not the truth of an agent's claim. The current `AgentRun` structure covers agent timing/status; stage-transition and resolution links still need their C# definition. No extra status-report file is proposed.

The existing recorded approval default remains unchanged: “Human approval is required for every transition by default, including forward transitions, self-loops, and returns to earlier stages.” This proposal grants no automatic-transition exception. The exact distinction between a tool operation within an assignment and a workflow transition must be expressed in the execution definition, not improvised by an agent.

### GR-EX — exploration Steps

| Step | Executor and action | Inputs | Output | Next consumer |
|---|---|---|---|---|
| `GR-EX-INSPECT` | Exploration agent investigates the assigned area; `GR-STATE-CAPTURE` performs requested source reads/searches/probes. | Registered area; pinned Conversation scope/exclusions; existing source and configuration. | Tool request/capture files. | Same exploration agent. |
| `GR-EX-RESULT` | Same agent produces area findings; `GR-STATE-SAVE` persists them. | Area instructions and original captures. | Original exploration result linking claims to evidence. | DESIGN via `GR-STATE-HANDOFF`; `GR-STATE-EXIT` for unresolved GROUND work. |

Boundary: exploration explains existing behavior and constraints. It does not independently conduct another capability-wide reuse determination. If it encounters reusable code, it references that evidence for the relevant prior-art assignment rather than creating a competing ledger. The reference binding between these assignments requires definition before implementation.

No dedicated exploration review agent is proposed. This saves a review round but does not certify exploration facts as true: DESIGN must inspect the source/evidence supporting the assumptions its design depends on. Disputed facts return to the relevant investigation, not every GROUND agent.

#### Exploration output necessity — proposed consumer contract

This evaluates downstream needs, not the behavior of the old scripts. The bindings below are proposals; they are not already implemented assignment fields. These are parts of one exploration result, not additional files.

| Result content | Process it supports or unblocks | Why that process needs it | Limit |
|---|---|---|---|
| Existing behavior, call/data flow and dependencies relevant to the requested change | `DS-PROPOSE`: develop an approach that fits the existing system | Establish where the change connects and what it would affect, instead of designing against an imagined system. | Only investigate facts that affect an actual design choice. Not a general codebase tour or second capability search. |
| Existing constraints, with evidence of their source | `DS-PROPOSE`: evaluate feasible approaches | Identify compatibility, dependency, lifecycle or platform constraints that make an approach valid or invalid. | Observed implementation is not automatically an approved requirement. Preserve that distinction. |
| Relevant affected files, stores and callers, tied to the behavior above | `CT-FORM`: establish the chosen design's edit scope and acceptance checks | Avoid omitting a caller, configuration store or dependency from the work. | Exploration identifies candidates; the selected design determines which must change. Not a second independently maintained file inventory. |
| Original evidence references supporting each claim | `DS-PROPOSE` and `CT-FORM`, when they rely on that claim | Let a consumer inspect the actual source/probe result instead of trusting an agent summary. | Evidence supports the claim in the same result. No separate evidence-summary agent or document. |
| A factual question the assigned investigation could not answer, identifying the dependent design/contract work | `GR-EX-INSPECT` follow-up and `GR-STATE-EXIT` | Identify exactly what research remains and what cannot proceed without it. | Not a loose list of interesting questions. A question that changes no downstream action does not block the stage. Resolution references still require definition. |
| A choice actually resolved by an existing approved principle, with the principle and evidence | `CT-HUNT` / `CT-FILTER`: distinguish an already-governed choice from one needing human decision | Avoid silently inventing a decision or asking the human to decide one already covered. | Only when such a choice occurred; do not manufacture a principle ruling for each factual observation. It is not evidence of new human approval. |
| Assigned-area identity | `GR-STATE-SAVE` and `GR-STATE-EXIT` | Associate the result with registered work and detect a missing result. | Administrative identity, not a separate investigation output or file. Use the assignment reference rather than copying its instructions. |

Recommendation: retain Exploration for change-relevant system behavior and constraints, with their affected surfaces and original evidence. Do not commission an area unless its answer is needed to develop the design or define executable work. If prior-art evidence already answers the question, reference that evidence rather than researching and describing it again. How assignments select those shared references remains to be defined.

For the earlier container relocation, the useful exploration question was which assembly dependencies, internal-access grants and consumers the move would affect. The separate prior-art question was which existing identity/resolution/scanning helpers should be reused. Those are different questions; producing two inventories of the same helpers would not justify two assignments.

Exploration does not decide desired behavior. SPEC and TEST_PLAN derive expected behavior from the approved requirements/plans, not from the assumption that current behavior is correct. They may need original exploration evidence for a particular baseline expectation, but this proposal does not add a blanket new handoff to every later stage.

### GR-PA — prior-art Steps

| Step | Executor and action | Inputs | Output | Next consumer |
|---|---|---|---|---|
| `GR-PA-SEARCH` | Search agent follows source relationships and text matches. `GR-STATE-CAPTURE` executes each structural/text request. | Registered capability and Conversation version; scoped source; relevant existing exploration evidence when explicitly bound. | Search requests and original captures. | Same search agent. |
| `GR-PA-RESULT` | Same search agent produces the candidate result; `GR-STATE-SAVE` persists it. | Original captures and assigned capability. | `PriorArtSearchResult`, including searches with no candidates or failed execution through their capture references. | Reuse judge. |
| `GR-PA-EVALUATE` | Independent reuse judge evaluates candidates and whether the searches support the determination. Additional source inspection uses `GR-STATE-CAPTURE`; `GR-STATE-SAVE` persists the evaluation. | Exact search result; original instructions and captures; relevant candidate implementation. | `PriorArtEvaluationResult`. | Ledger generation; exit/correction handling. |
| `GR-PA-INDEX` | Guard constructs reference pairs, checking that each evaluation belongs to its supplied search result. | Saved/versioned search and evaluation references selected for this attempt. | `PriorArtLedger`. | DESIGN through `GR-STATE-HANDOFF`; `GR-STATE-EXIT`. |

Search is one agent assignment per capability, not an agent per query. Evaluation is a separate assignment because the search agent must not certify its own reuse conclusion. Generating the ledger is Guard work, not another agent or review.

The reuse judge examines substantive sufficiency: correct owner, relevant callers/implementations and missing behavior. A completed tool call or an empty response alone cannot establish an adequate search. When evidence cannot support a category, the judge returns the existing `UnresolvedReuseDecision`; it does not guess `New`. Completed search work remains available for targeted continuation.

Proposal for zero capabilities: register no search/evaluation assignments and generate an empty ledger from that empty registered set. Do not invent a capability or launch agents merely to produce empty outputs. This settles a currently open replacement-design case only if approved.

### GR-API — platform/API Steps

| Step | Executor and action | Inputs | Output | Next consumer |
|---|---|---|---|---|
| `GR-API-RESEARCH` | Research agent investigates the capability across the selected platforms. `GR-STATE-CAPTURE` runs documentation search, SDK search, corpus listing, reference-library search or source reading as requested. | Registered capability; pinned Conversation platforms/scope; external documentation and SDK/library sources. | Saved requests, captures and original responses. | Same research agent. |
| `GR-API-RESULT` | Same agent produces findings using the same result shape for each selected platform; `GR-STATE-SAVE` persists them. | Capability/platform requirements; captured source material. | Original platform findings with API/primitive candidates, applicable versions, behavior, constraints, evidence and unresolved factual questions. | DESIGN via `GR-STATE-HANDOFF`; `GR-STATE-EXIT`. |

Keep one research assignment per capability covering the selected platforms, as in `PlatformResearchAssignment`. Tool channels are not separate agent tasks. The agent reports candidate primitives and constraints; DESIGN chooses their architectural use. No separate platform research judge or blanket GROUND review panel is proposed. DESIGN inspects evidence for the platform assumptions it uses; Guard checks that each required platform has an explicit finding, not that the finding is true.

### File granularity

| Data | Proposed storage | Reason |
|---|---|---|
| Instructions and selected scope/platforms | Conversation only; assignments reference its version. | No rewritten input briefs. |
| Assignments, agent executions and selected input/result references | Workflow only. | Dispatch and tracking use the same registered work. |
| Each agent's result | One original result file per agent run. | Preserve independent outputs and reuse successful work. Intermediate agent narration is not another result. |
| Prior-art tool request and execution | Retain the current saved request plus capture. Command, stdout, stderr and outcome remain fields of the capture. | Already supplies the original execution evidence without extra files per field. |
| Platform/API tool request and execution | **Proposed simplification:** saved request + capture + original response. Store the actual backend request inside the capture rather than in another file. | Preserve requested-versus-executed evidence while removing one file and lookup per call. Do not copy the response into the findings. This changes `PlatformResearchCapture.ExecutedRequest` only after approval. |
| Prior-art ledger | One reference-only ledger for the selected attempt results. | Pair the exact search and evaluation consumed by DESIGN without copying either. |
| Stage summary, evidence index, completion report or handoff wrapper | None added. | Existing Workflow tracking, ledger and `DesignAssignment` already carry their respective references. |

Source-inspection/probe requests still need a defined capture operation. Reuse the capture/persistence machinery, but do not pretend the current search interfaces already cover arbitrary source reads or probes. No arbitrary shell-command interface is approved by this proposal.

### Repeats and exit

| Observed condition | Proposed next work | What remains reusable |
|---|---|---|
| Agent stopped without a usable result. | Resume that assignment under the recorded attempt rules and human-approval default. | Other successful results and saved tool captures. |
| A required search/read failed. | Retry or repair that evidence-gathering operation; do not classify it as an empty result. | Successful evidence and unaffected assignments. |
| Reuse determination is `Unresolved`. | Identify the missing evidence or human choice in that original determination; perform only the required follow-up, then reevaluate the affected capability. | Search/evaluation history and unaffected capabilities. |
| Exploration/platform evidence is insufficient for an assigned question. | Correct that investigation, not the entire stage. | Unaffected findings and captures. |
| Existing code contains a defect. | Record the observed behavior for DESIGN; do not repair production code in GROUND. | The evidence may be a successful finding, not failed investigation. |
| An upstream input changes. | Check which dependent findings are invalidated; reuse only applicable results. Do not silently bind an old result to changed instructions. | Results whose inputs and supporting evidence still apply. |

Proposed exit criterion: every registered investigation has its required original result; every required capability has a resolved reuse determination; every selected platform has a supported finding or an evidence-backed unsupported finding; no unanswered GROUND factual obligation blocks DESIGN; referenced artifacts match the versions used; and transition approval is recorded. An unanswered factual question is not converted into an architectural choice merely to pass this gate. A choice that genuinely belongs to DESIGN remains DESIGN work and must have its supporting facts available.

An `Unresolved` result remains valid saved work, but is not a completed reuse determination. Guard can check its category and required references; deciding whether evidence answers the engineering question remains agent/human work. Missing resolution-reference structures must be defined before this exit check can be implemented.

### What this proposal changes and what remains to define

Recommended changes for approval: use the three investigation Items plus shared execution-state operations shown here; retain the independent reuse judge without adding blanket GROUND reviewers; reduce platform capture storage by placing the executed request inside the capture; use the proposed zero-capability handling and exit criterion. These are proposals, not recorded approvals.

The remaining implementation definitions are specific: exploration assignment/result types; per-platform findings/result types; typed source-inspection/probe capture operations; bindings for shared evidence; remaining path/version references; resolution and transition fields in Workflow; and CLI/MCP operations that enforce those structures. Their absence does not justify inventing extra summary files, result-rewriting agents or review stages.

## Sources

- [Process I/O inventory](workflow-artifact-usage.js): separate current-script and replacement-design entries, with source pointers and unresolved bindings.
- [Stage names, icons and descriptions](workflow-phase-details.js).
- [Prior-art types](PriorArt.cs), [platform/API types](PlatformResearch.cs), [assignment/reference types](StageExecution.cs), [Workflow](IWorkflow.cs), [agent-run tracking](AgentRun.cs).
- [Conversation](conversation.md): input ownership, captures, execution hierarchy and GROUND-to-DESIGN handoff.
- [Current GROUND script](../../../.agents/workflows/ground.js): `explorePrompt`, `__priorArtLedger`, `gatherPrompt`, `evaluatePrompt` and stage return.

No workflow script, C# type, existing diagram or storage layout was changed to produce this inventory.
