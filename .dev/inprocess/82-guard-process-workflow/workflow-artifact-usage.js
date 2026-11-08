// Source audit of current scripts and the separately recorded replacement design.
// Every recorded read is an input, including reads within a stage and through references.
// Names/icons stay in workflow-phase-details.js. This file owns process I/O and its sources.
(() => {
  const api = window.WorkflowPhaseDetails;
  const {artifacts, stages, stageSymbols} = api;
  const sources = {
    tracking: ['conversation.md', 'Registered work is the execution source; Attempts and resumption'],
    workflow: ['IWorkflow.cs', 'IssueId / IssueText; AgentGuard creates the issue when IssueText is supplied'],
    assignments: ['StageExecution.cs', 'StageAttempt, DesignAssignment, AssignmentReference'],
    prior: ['PriorArt.cs', 'PriorArtSearchAssignment, PriorArtEvaluationAssignment, SearchExecution, PriorArtLedgerEntry'],
    handoff: ['conversation.md', 'GROUND to DESIGN handoff'],
    research: ['PlatformResearch.cs', 'PlatformResearchAssignment, PlatformResearchCapture, ResearchCompleted'],
    planning: ['conversation.md', 'Rule and test planning'],
    plans: ['IPlan.cs', 'PlannedCase.Expectation, Reason, HumanInput'],
    design: ['../../../.agents/workflows/design.js', 'proposePrompt (1346), judgePrompt (1363), final result (1441)'],
    ground: ['../../../.agents/workflows/ground.js', 'gatherPrompt, evaluatePrompt, explorePrompt'],
    hidden: ['../../../.agents/workflows/hidden-decision-scan.js', 'huntPrompt, filterPrompt'],
    preflight: ['../../../.agents/workflows/preflight-contract.js', 'contract checking agent prompt'],
    architecture: ['../../../.agents/workflows/architecture.js', 'authorPrompt, skeletonRefutePrompt, SKELETON_ADVERSARIES, result'],
    rule: ['../../../.agents/workflows/rule-phase.js', 'ruleGenPrompt and rule adversary prompt'],
    test: ['../../../.agents/workflows/tdd.js', 'test author and testRefutePrompt'],
    implement: ['../../../.agents/workflows/implement.js', 'workerPrompt; rulePhaseResultPath and tddResultPath'],
    readiness: ['../../../.agents/workflows/refute-readiness.js', 'readinessPrompt'],
    refute: ['../../../.agents/workflows/refute.js', 'refutePrompt, stageOutputPaths, per-lens charges'],
    rails: ['../../../.agents/skills/rails-run-a-workflow/SKILL.md', 'Stage inputs/outputs; GATE; REPORT; retries'],
    map: ['workflow-phase-details.js', 'Stage descriptions and internal process maps; review draft, not runtime policy']
  };
  Object.assign(sources, {
    brief: ['../../../.agents/workflows/ground.js', '__workflowBrief, lines 42–69: live issue plus every input-folder file'],
    runtime: ['../../../.agents/workflows/ground.js', '__requiredAgentRuntime, lines 414–601: validate objects, invoke agent, preserve results; no file writer'],
    contractGuide: ['../../../.agents/skills/rails-write-a-contract/SKILL.md', 'The sections, in order; Owner references'],
    build: ['../../../Makefile', 'build: restore → dotnet build; test: build → dotnet test'],
    buildConfig: ['../../../Directory.Build.props', 'ProjectReference, RunSettingsFilePath, analyzer configuration and imported props'],
    conversationType: ['IConversation.cs', 'Description, Scope, Exclusions, InvestigationAreas, Capabilities, TargetPlatforms'],
    timing: ['AgentRun.cs', 'AssignmentId, Status, ResultPath, StartedAt, EndedAt'],
    capture: ['conversation.md', 'Prior-art search and capture — Agent requests, Execution and receipt, Captured evidence'],
    reviewers: ['conversation.md', 'Discussion so far; Workflow state diagram: enabled stage reviewers and loops'],
    documentFlow: ['conversation.md', 'Intended document flow; Guard-managed document edits'],
    stages: ['conversation.md', 'Workflow state diagram; Rule and test planning'],
    outputs: ['../../../.agents/workflows/ground.js', '__stageResultContracts: agent and stage result schemas, lines 652–978']
  });
  // These roles already occur in the types or internal maps but were absent from the overview.
  const extra = {
    searchRequest: ['Prior-art search requests', 'ground', 'file', ['workflow']],
    searchCapture: ['Prior-art search / inspection captures', 'ground', 'file', ['evidence']],
    researchRequest: ['Platform/API research requests', 'ground', 'file', ['workflow']],
    researchCapture: ['Platform/API capture records', 'ground', 'file', ['evidence']],
    researchExecuted: ['Executed platform/API requests', 'ground', 'file', ['evidence']],
    researchResponse: ['Platform/API responses', 'ground', 'file', ['evidence']],
    explorationCapture: ['Exploration evidence', 'ground', 'file', ['evidence']],
    externalApi: ['API documentation / SDK / reference-library sources', 'external', 'source', ['docs']],
    projectConfig: ['Project / build / test configuration', 'external', 'source', ['config']],
    hiddenCandidates: ['Hidden-decision hunter results', 'contract', 'file', ['output']],
    ruleEvaluation: ['RULE outcome evaluations', 'rule', 'file', ['output']],
    testEvaluation: ['TEST outcome evaluations', 'test', 'file', ['output']],
    workFolder: ['Existing input-folder files (runtime-dependent set)', 'external', 'source', ['workflow']]
  };
  Object.assign(artifacts, extra);
  // Created at Conversation intake; subsequent stages update the same document.
  artifacts.workflow[1]='conversation';
  // Stable readable identifiers for artifact roles. Not new runtime fields or filenames.
  const ids = {
    request:'EX-issue', project:'EX-code', projectConfig:'EX-config', workflow:'CV-workflow', rails:'EX-instructions', externalApi:'EX-api-sources', workFolder:'EX-input-folder',
    conversation:'CV-conversation', exploration:'GR-exploration', search:'GR-search', evaluation:'GR-reuse', ledger:'GR-ledger', research:'GR-research',
    groundEvidence:'GR-evidence-view', searchRequest:'GR-search-request', searchCapture:'GR-search-capture', explorationCapture:'GR-explore-evidence',
    researchRequest:'GR-api-request', researchCapture:'GR-api-capture', researchExecuted:'GR-api-executed', researchResponse:'GR-api-response',
    proposals:'DS-proposals', design:'DS-selection', designReviews:'DS-reviews', contract:'CT-contract', hiddenCandidates:'CT-candidates', hidden:'CT-decisions',
    contractCheck:'CT-check', baseline:'CT-baseline', specPlan:'SP-plan', architecture:'AR-result', signatures:'AR-signatures',
    architectureReviews:'AR-reviews', architectureEvidence:'AR-build', rulePlan:'RP-plan', rules:'RL-code', ruleResults:'RL-results',
    ruleEvidence:'RL-captures', ruleEvaluation:'RL-evaluation', ruleReviews:'RL-reviews', testPlan:'TP-plan', tests:'TS-code',
    testResults:'TS-results', testEvidence:'TS-captures', testEvaluation:'TS-evaluation', testReviews:'TS-reviews', implementation:'IM-code',
    implResult:'IM-result', implEvidence:'IM-captures', readiness:'RD-result', verdicts:'RF-verdicts', refuteEvidence:'RF-captures',
    gate:'GT-decision', finalEvidence:'GT-captures', report:'RT-report'
  };
  const notes = {
    project: 'A set of source files, not a workflow document. AR-signatures, RL-code, TS-code and IM-code select files/revisions in this set; do not add them to get a physical-file count.',
    projectConfig: 'Existing project files, build scripts and runner configuration read by tools. Exact project-specific paths are not defined by this workflow draft.',
    workflow: 'One live document. It is an output, not an input, of Conversation. Subsequent stages use its assignments, attempts and agent runs. This is not an agent-authored stage summary.',
    rails: 'Existing instruction files. The applicable subset varies by role; this row is not one additional combined document.',
    request: 'External issue record, supplied by ID or created from Workflow.IssueText. New design instructions belong in Conversation; live issue precedence in old scripts is not copied into the new design.',
    ledger: 'Reference-only directory of search/evaluation pairs. DESIGN follows these references. Workflow also tracks the results; whether this separate directory adds enough value is a review question, not a removal decision.',
    groundEvidence: 'An umbrella label, NOT another saved file. Expanded into the search, exploration and platform/API evidence rows below.',
    explorationCapture: 'Evidence is required; its separate storage shape is not defined. Do not create a new evidence-summary document merely to fill this row.',
    research: 'Consumer purpose is defined; result schema and exact evidence-reference fields remain undefined.',
    designReviews: 'Separate enabled reviews appear in the working design. The current design.js returns proposals plus one judge verdict, not these separate review files. Reviewer input binding remains unresolved.',
    architecture: 'Original author result, including signature/no-signature determination. Current script embeds signatureDiff/buildProof; replacement schema must distinguish references from copied source/evidence.',
    signatures: 'A changed-source subset of EX-code, not an extra signature-document copy. The author result signatureDiff is a separate field used for human review in the existing script.',
    architectureEvidence: 'Working design calls for tool captures. Current script has author.buildProof, not a guaranteed separate tool-written file. Review use exists; separate-file binding is unfinished.',
    rules: 'Rule source plus rule-verification fixtures inside EX-code. Not an additional copy of that source.',
    tests: 'Spec/unit-test source inside EX-code. Not an additional copy of that source.',
    implementation: 'Changed production source inside EX-code, potentially the same paths as AR-signatures at a later revision.',
    ruleResults: 'Case/outcome handoff; must not become a second transcript. Replacement schema is undefined.',
    testResults: 'Case/coverage/outcome handoff; must not become a second transcript. Replacement schema is undefined.',
    ruleEvaluation: 'Independent explanation of Unknown/mismatched outcomes. Already in the internal process map; omitted from the old overview. Schema not defined.',
    testEvaluation: 'Independent explanation of Unknown/mismatched outcomes. Already in the internal process map; omitted from the old overview. Schema not defined.',
    implResult: 'Acceptance/wall/completion result. Distinct from command captures; new typed schema is not defined.',
    baseline: 'Consumed at CONTRACT exit to establish the opening state. Later comparison/evidence-validity bindings are unresolved.',
    finalEvidence: 'New capture only if verification is run. GATE may use valid existing captures; this is not a required duplicate of IM/RF captures.',
    report: 'Human-facing presentation. REVIEW is the proposed consumer, but that stage has no defined responsibility yet. It is not an authority for earlier facts.'
  };
  const processes = [];
  // Each edge owns the selected information and the reason this reader needs it.
  // "value" means a returned object embedded in a prompt, not an invented file.
  const io = (key, information, purpose, form='file') => ({key, information, purpose, form});
  const previousAttempt = edge => ({...edge, origin:'previous-attempt'});
  function process(profile, stage, name, source, inputs, outputs, note='', basis='defined', runtime=[]) {
    const entry = {
      id: stageSymbols[stage]+'-'+profile+'-'+(processes.filter(p=>p.stage===stage&&p.profile===profile).length+1),
      profile, stage, name, source, inputs, outputs, note, basis, runtime,
      reads: [...new Set(inputs.map(e=>e.key))], writes: [...new Set(outputs.map(e=>e.key))],
      condition: note || 'When this step runs'
    };
    processes.push(entry); return entry;
  }
  const c=(...args)=>process('current',...args);
  const n=(...args)=>process('design',...args);
  const proposal=(stage,name,source,inputs,outputs,note)=>n(stage,name,source,inputs,outputs,note,'unresolved');
  const sourceCode=(purpose)=>io('project','Relevant working-tree declarations, bodies, call sites and tests',purpose,'file set');
  const config=(purpose)=>io('projectConfig','Solution/project files, imported props/targets, runner settings and analyzer configuration',purpose,'file set');
  const instructions=(information,purpose)=>io('rails',information,purpose,'file set');
  const contractRead=(information,purpose)=>io('contract',information,purpose);
  const oldBrief=()=>[
    io('request','Live issue body from gh issue view','Establish the task and resolve conflict with folder material','external record'),
    io('workFolder','Every file under inputFolderPath','Read the run background; exact files depend on the folder contents','file set')
  ];
  const verdictOut=(key)=>io(key,'lens, verdict, findings, observations, refutationAttempts, proofChecked','Return the original reviewer result','value');
  const toolProof=(key,information)=>io(key,information,'Provide observed output to the agent; current scripts do not save a capture file','tool response');
  const definedToolReads=(...producedCode)=>[sourceCode('Compile or execute the selected projects'),config('Resolve projects, commands, dependencies and analysis/test settings'),...producedCode.map(key=>io(key,'Files from this output included in the selected build/test projects','Compile/analyze/execute the delivered source; these files are also part of Source Code','file set'))];

  // CURRENT: actual prompt/runtime transfers. No file access by workflow JavaScript is implied.
  c('conversation','Coordinator prepares the work folder','rails',
    [io('request','Issue requirements','Start the work from the issue','external record')],
    [io('conversation','Human-approved conversation summary','Brief the GROUND agents')],
    'No Conversation-stage script. The workflow document is not implemented in the existing scripts.');
  c('ground','Exploration agents — explorePrompt(area)','ground',
    [...oldBrief(),sourceCode('Derive facts and affected stores for area.focus'),
      instructions('rails-explorer, rails-decisions, best-practices-guide','Apply exploration and decision criteria')],
    [io('exploration','area, facts[{claim,evidence}], surfaces, openQuestions, autoResolved','Return findings for DESIGN and CONTRACT','value')],
    'One agent per areas entry. Facts are retained in the returned GROUND object, not written to a file by ground.js.',
    'defined',['projectPath; areas[].id/focus; issueScope are caller arguments.']);
  c('ground','Code-search agents — gatherPrompt(cap)','ground',
    [...oldBrief(),sourceCode('Find existing owners with CodeGraph and grep'),
      instructions('rails-dry-code','Choose discovery lenses and prior-art criteria')],
    [io('search','capability, candidates, lensesRun, lensesEmpty, lensErrors','Supply the reuse judge with the pooled search hits','value')],
    'The search agent runs the tools. No structured request/capture files are written by this script.',
    'defined',['capabilities[].id/description supplied by caller; CodeGraph and grep responses consumed by agent.']);
  c('ground','Reuse judges — evaluatePrompt(cap, found)','ground',
    [...oldBrief(),io('search','Complete search result for the matching capability','Decide reuse/extract/new using the provided hits','value'),
      instructions('rails-dry-code','Apply reuse criteria')],
    [io('evaluation','capability, decision, owner, copies, evidence, confidence','Return the reuse determination','value')],
    'The script embeds JSON.stringify(found). Its instruction is “using ONLY the pooled search hits provided”; no new source inspection is explicitly assigned here.');
  c('ground','GROUND completion — __priorArtLedger and final result','ground',
    [io('exploration','All valid area results','Retain facts in final result','value'),
      io('evaluation','All capability verdicts','Assemble returned ledger.verdicts','value')],
    [io('ledger','projectPath, verdicts, panelComplete, failedRoles','Provide the completed prior-art result to the caller','value')],
    'Completed GROUND returns {projectPath,facts,ledger,panelComplete,failedRoles}. Successful search results are omitted from this completed aggregate. Incomplete paths retain them for resume. Empty capabilities use the no-capability marker.');
  c('design','Design proposal agents — proposePrompt(angle)','design',
    [...oldBrief(),io('exploration','factsPath contents','Ground each design in the original facts'),
      io('ledger','ledgerPath contents when provided','Reuse existing owners'),
      sourceCode('Check design assumptions against current code'),
      instructions('rails-solid-code, rails-dry-code, rails-real-work, rails-decisions, best-practices-guide','Apply design and decision criteria')],
    [io('proposals','angle, approach, keySteps, rulesToAdd, risks, autoResolved','Supply candidates to the design judge','value')],
    'factsPath and ledgerPath are optional path arguments; agents open them. proposals are not separate files written by design.js.',
    'defined',['angles[].id/focus/model; projectPath; issueScope are caller arguments.']);
  c('design','Design judge — judgePrompt(proposals)','design',
    [...oldBrief(),io('proposals','Complete proposals array','Select and synthesize the winning approach','value'),
      io('ledger','ledgerPath contents when provided','Produce contract-ready reuseInstructions'),
      sourceCode('Apply the loaded SOLID/DRY criteria to existing ownership'),
      instructions('rails-solid-code, rails-dry-code, rails-real-work, rails-decisions, best-practices-guide','Judge approach and resolve covered choices')],
    [io('design','winningAngle, why, synthesizedApproach, graftedFrom, rulesToAdd, risks, autoResolved, reuseInstructions','Supply the chosen design to CONTRACT','value')],
    'No factsPath is explicitly forwarded to this prompt. The all-folder brief may expose that file if it lives there; that is a conditional folder read, not a guaranteed parameter. Returned stage object contains both proposals and verdict.');
  c('design','SOLID and DRY design reviews required by the skill','rails',
    [io('design','Drafted approach','Check design before CONTRACT'),
      io('ledger','Prior-art rulings','Check proposed duplication'),sourceCode('Check the design against existing owners'),
      instructions('rails-solid-code and rails-dry-code','Apply the named review criteria')],
    [io('designReviews','Review results','Expose design defects before the contract locks')],
    'Required in the DESIGN section of rails-run-a-workflow; design.js does not dispatch these separate review agents or define their persistence.');
  c('contract','Coordinator writes contract','rails',
    [io('request','Requirement','Derive acceptance criteria','external record'),
      io('conversation','Approved decisions and scoped work','Record the approved instructions'),
      io('exploration','Facts and surfaces','Name affected stores and feasible checks'),
      io('ledger','Reuse rulings','Populate Reuse ledger'),
      io('design','Selected design, rulesToAdd, reuseInstructions','Record the design and rules to build'),
      instructions('rails-write-a-contract and rails-decisions','Apply contract structure and approval rules')],
    [io('contract','Required sections including Decisions, Rules to add, Surfaces, Acceptance','Become the spec for subsequent work')],
    'No contract-writing script. Result paths are supplied by the coordinator; no extraction code exists here.');
  c('contract','Hidden-decision hunters — huntPrompt(lens)','hidden',
    [contractRead('Full contract, especially Decisions','Find forced choices not yet decided'),
      sourceCode('Establish whether a choice is actually forced'),
      instructions('rails-decisions and rails-explorer','Apply decision boundary and evidence method')],
    [io('hiddenCandidates','lensId, candidates[{choice,forcedBy,category,evidence}]','Feed the care-filter judge','value')],
    'One agent per lens; script pools the original candidates and adds lensId.');
  c('contract','Hidden-decision care-filter — filterPrompt(pooled)','hidden',
    [io('hiddenCandidates','Pooled candidates including lensId','Merge candidates and distinguish human decisions from noise','value'),
      instructions('rails-decisions and best-practices-guide','Resolve choices covered by approved principles')],
    [io('hidden','findings, autoResolved, droppedAsNoise','Expose decisions to resolve before CONTRACT exit','value')],
    'The filter prompt does not contain contractPath. It receives candidate evidence, not a mandatory direct read of the contract. Completed output omits the hunter results.');
  c('contract','Contract preflight agent — preflightPrompt','preflight',
    [contractRead('All sections, paths, identifiers and factual claims','Check shape and internal consistency'),
      sourceCode('Check paths, identifiers, counts and referenced file pairs'),
      instructions('rails-write-a-contract, rails-decisions, rails-dry-code and referenced rule/script pairs','Check governing requirements and paired definitions')],
    [io('contractCheck','sound, defects[{kind,contractSays,ruleSays,fix}], checksRun','Identify contract defects before writing code','value')],
    'Checks contract form, not the truth of unseen human approval. Files named by the contract are conditional additional reads.');
  c('contract','Opening build and test commands','build',definedToolReads(),
    [toolProof('baseline','make build / make test output and exit codes')],
    'Coordinator runs these under the CONTRACT section of rails-run-a-workflow. The script layer does not create a baseline file.');
  c('contract','Coordinator resolves contract exit','rails',
    [io('contract','Approved decisions and acceptance','Check readiness for mutating stages'),
      io('hidden','Unresolved findings and applied principles','Resolve forced decisions'),
      io('contractCheck','Defects and checksRun','Correct contract defects'),
      io('baseline','Opening build/test result','Require opening green')],
    [io('contract','Human-approved corrections','Finalize the contract')],
    'Human approval is an external action, not evidence synthesized from a result.');
  c('architecture','Architecture coding agent — authorPrompt','architecture',
    [contractRead('Full contract; decided interface structure and selected-warning approval','Write only decided signatures'),
      sourceCode('Existing types, bodies, callers and analyzer fence','file set'),
      instructions('rails-run-a-workflow, rails-solid-code, rails-dry-code, rails-decisions','Apply architecture authority')],
    [io('signatures','Interfaces, DTOs, enums and concrete signatures','Expose the surface tests compile against','file set')],
    'Current order is RULE then ARCHITECTURE. This prompt has no DESIGN-result or SPEC-plan path. Build proof is an agent-returned string.',
    'defined',['ruleWarningIds selects the command; refutationPath adds a findings file only in a correction round.']);
  c('architecture','Compiler launched by architecture agent','architecture',definedToolReads('signatures','rules'),
    [toolProof('architectureEvidence','dotnet build -c Release output and exit code')],
    'The selected-rule-warnings block constructs the approved WarningsNotAsErrors argument; the agent runs the command.');
  c('architecture','Architecture coding agent — return result after build','architecture',
    [io('signatures','Delivered declarations','Identify the surface written','file set'),
      io('architectureEvidence','Build response','Populate buildProof','tool response')],
    [io('architecture','skeletonFiles, signatureDiff, buildProof, perType, notes','Supply the architecture review and human signature review','value')],
    'Continuation of the same architecture agent, not another agent. The no-signature path returns empty source lists.');
  c('architecture','Architecture SOLID / DRY / Lie-catcher agents — skeletonRefutePrompt','architecture',
    [contractRead('Decisions and interface structure','Judge the authored surface'),
      io('architecture','Complete author object','Check signatures, no-signature claim and buildProof','value'),
      io('signatures','author.skeletonFiles on disk','Inspect actual declarations and bodies','file set'),
      sourceCode('Compare the surface with existing owners under SOLID/DRY'),
      instructions('Each reviewer’s rail','Apply its review criteria')],
    [verdictOut('architectureReviews')],
    'Author JSON is embedded in every prompt. No architecture-result file is read by these reviewers. Lie-catcher is instructed to confirm the build.');
  c('architecture','Coordinator and human consume architecture result','architecture',
    [io('architecture','signatureDiff / no-signature determination','Human sign-off before testing','value'),
      io('architectureReviews','Original verdicts','Require reviewed surface','value')],
    [],'architecture.js explicitly leaves human sign-off to the coordinator after returning.');
  c('rule','Rule-writing agent — ruleGenPrompt','rule',
    [contractRead('Full contract, exact Rules to add','Implement approved enforcement'),
      sourceCode('Existing analyzers and rule-test patterns','Reuse owners and fixture infrastructure'),
      instructions('rails-run-a-workflow, rails-solid-code, rails-dry-code, rails-decisions','Apply rule authority'),
      io('projectConfig','AnalyzerReleases.Shipped.md and AnalyzerReleases.Unshipped.md','Allocate and register the next unused diagnostic ID','file set')],
    [io('rules','Analyzer/test-rule files and their covering fixtures','Enforce the approved rules','file set'),
      io('projectConfig','AnalyzerReleases.Unshipped.md row','Register each new diagnostic ID','file set')],
    'rulesToAdd is also embedded from a caller argument, duplicating the contract list. No separate Rule Plan input exists.',
    'defined',['rulesToAdd; noRuleEvidence; optional refutationPath.']);
  c('rule','Rule fixture and production build commands','rule',definedToolReads('rules'),
    [toolProof('ruleEvidence','Violating/compliant fixture output and production build output with exits')],
    'The agent runs both fixture cases and the production build; source references depend on the projects selected.');
  c('rule','Rule-writing agent — return result after checks','rule',
    [io('rules','Delivered rule files and fixtures','Identify what each check exercises','file set'),
      io('ruleEvidence','Build and fixture responses','Return per-rule proof and production impact','tool response')],
    [io('ruleResults','ruleFiles, buildProof, perRule, notes','Supply reviewers and IMPLEMENT','value')],
    'Continuation of the rule-writing agent. The approved no-rule path returns its marker instead of manufacturing execution evidence.');
  c('rule','Rule SOLID / DRY / Lie-catcher agents — ruleRefutePrompt','rule',
    [contractRead('Rules to add','Check exact approved guardrails'),
      io('ruleResults','Complete rule-generation object','Check per-rule coverage and claimed evidence','value'),
      io('rules','ruleGen.ruleFiles and fixtures','Inspect rule behavior','file set'),
      sourceCode('Search existing implementations and consumers under DRY'),
      io('projectConfig','Both analyzer release-tracking files','Lie-catcher checks diagnostic identity','file set'),
      instructions('Each reviewer’s rail','Apply its review criteria')],
    [verdictOut('ruleReviews')],
    'JSON.stringify(ruleGen) is the handoff. Lie-catcher re-runs fixtures. Stage returns author fields plus verdicts; no script-written result file.');
  c('test','TDD coding agent — authorPrompt','test',
    [contractRead('Full contract, especially Acceptance','Derive tests from required behavior'),
      io('signatures','Live architecture declarations','Compile tests against real types','file set'),
      sourceCode('Production behavior and existing test infrastructure','Choose cases and reuse fixtures'),
      instructions('rails-run-a-workflow, rails-test-code, rails-dry-code','Apply test authority and criteria')],
    [io('tests','Test source and fixtures','Prove acceptance behavior','file set')],
    'No architecture-result file or test-plan file parameter. Existing prompt requires RED; the replacement design does not.',
    'defined',['ruleWarningIds; noTestEvidence; optional refutationPath.']);
  c('test','Build and test commands launched by TDD agent','test',definedToolReads('signatures','rules','tests'),
    [toolProof('testEvidence','dotnet test output and exit code')],
    'Selected warning IDs may affect this intermediate command. Test output is not saved by tdd.js.');
  c('test','TDD coding agent — return result after tests','test',
    [io('tests','Delivered cases and assertions','Identify acceptance coverage','file set'),
      io('testEvidence','dotnet test response','Return redProof and coverage','tool response')],
    [io('testResults','testFiles, redProof, coverage, notes','Supply reviewers and IMPLEMENT','value')],
    'Continuation of the test-writing agent. The approved no-test path returns its marker without inventing a test run.');
  c('test','Test-quality / DRY / Lie-catcher agents — testRefutePrompt','test',
    [contractRead('Full contract','Check tests against required behavior'),
      io('testResults','Complete test-author object','Check coverage, notes and proof','value'),
      io('tests','testGen.testFiles','Inspect test assertions and fixtures','file set'),
      sourceCode('Inspect production paths exercised by tests and existing fixture owners'),
      instructions('Each reviewer’s rail','Apply test-quality, duplication or approval criteria')],
    [verdictOut('testReviews')],
    'JSON.stringify(testGen) is embedded in the prompt. Stage result merges author fields and verdicts.');
  c('implement','Implementation coding agent — workerPrompt','implement',
    [contractRead('Full contract','Implement required behavior within approved authority'),
      io('ruleResults','rulePhaseResultPath, when stage ran','Read complete rule coverage, fixture proof and impact'),
      io('testResults','tddResultPath, when stage ran','Read test coverage and proof'),
      io('ruleReviews','verdicts inside the same saved RULE aggregate','Read the stage result without dropping its reviews','file field'),
      io('testReviews','verdicts inside the same saved TDD aggregate','Read the stage result without dropping its reviews','file field'),
      io('signatures','Existing declared surface','Implement without changing frozen signatures','file set'),
      io('rules','Existing rules','Understand and satisfy the rule fence','file set'),
      io('tests','Existing acceptance tests','Implement the required behavior','file set'),
      sourceCode('Current implementations and dependencies','Make the authorized change'),
      instructions('run-a-workflow, solid, dry, real-work, test, decisions, explorer rails','Apply implementation and proof criteria')],
    [io('implementation','Authorized production changes','Deliver the required behavior','file set')],
    'No architectureResultPath. Result paths are checked as nonempty strings, not opened or validated by JavaScript. workerPrompt requires every TDD coverage row red.',
    'defined',['instruction text or object is embedded in prompt; level; omittedStageAuthorizations.']);
  c('implement','Build, test and acceptance commands','implement',
    [...definedToolReads('implementation','signatures','rules','tests'),contractRead('Acceptance commands','Run the contract-specific checks')],
    [toolProof('implEvidence','dotnet build -c Release, dotnet test -c Release, acceptance output and exits')],
    'Commands and their selected projects determine actual source/config files read.');
  c('implement','Implementation coding agent — return result after verification','implement',
    [io('implementation','Delivered changes','Identify the implemented work','file set'),
      io('implEvidence','Build/test/spot-check responses','Return observed acceptance results','tool response')],
    [io('implResult','changedFiles, buildProof, testProof, acceptance, walls, complete','Return completion evidence','value')],
    'Continuation of the implementation agent, not a separate reporting agent.');
  c('readiness','Readiness agent — readinessPrompt','readiness',
    [contractRead('Surfaces, boundaries, Acceptance, Decisions','Compare contract with the delivered tree'),
      sourceCode('Changed files and named identifiers','Check actual paths, boundaries and completed work'),
      instructions('rails-decisions and rails-real-work','Apply authority and live-evidence criteria')],
    [io('readiness','ready, mismatches, checksRun','Identify mismatches before REFUTE','value')],
    'No IMPLEMENT-result, review-result or build-log path parameter. Reads git status and relevant diffs directly.',
    'defined',['panelInstruction string; decisionsGiven[] strings are caller-supplied, not original document references.']);
  c('readiness','Coordinator consumes readiness result','rails',
    [io('readiness','ready and mismatches','Resolve mismatches before launching REFUTE','value')],[],
    'This is the consumer of the readiness result. refute.js does not itself read it.');
  c('implement','Coordinator consumes implementation result','rails',
    [io('implResult','complete, acceptance and walls','Determine whether implementation finished or needs a human decision','value')],[],
    'Worker results are consumed before proceeding; READINESS does not take the result as a parameter.');
  c('refute','Each selected adversary — refutePrompt(adv)','refute',
    [contractRead('Full contract at L1/L2','Judge the finished work'),
      io('exploration','stageOutputPaths.ground: facts and autoResolved','Audit grounding and decision filtering'),
      io('ledger','stageOutputPaths.ground: ledger','Audit reuse claims in original GROUND aggregate'),
      io('proposals','stageOutputPaths.design: proposals','Inspect original candidate records and autoResolved'),
      io('design','stageOutputPaths.design: verdict','Audit selected design and applied principles'),
      io('hidden','stageOutputPaths.hiddenDecisionScan','Audit findings and autoResolved'),
      sourceCode('Relevant current code and diffs','Judge the delivered implementation rather than stale background'),
      io('refuteEvidence','Responses from the reviewer’s own checks','Return observed proofChecked and findings','tool response'),
      instructions('rails-explorer, each lens rail; Lie-catcher also workflow/test/decisions/guide','Apply the selected lens')],
    [verdictOut('verdicts')],
    'Each supplied stage-output file is opened. L1 requires GROUND, DESIGN and hidden-decision files. These are aggregate files: proposals and verdict share one DESIGN file. No explicit Architecture, TDD, IMPLEMENT or READINESS result path exists here.',
    'defined',['changedFiles[]; level; additionalAdversaries; L3 humanRequest replaces contract and has no stageOutputPaths.']);
  c('refute','Independent acceptance-check commands','refute',
    [...definedToolReads('implementation','signatures','rules','tests'),contractRead('Applicable spot-check commands','Re-run acceptance independently')],
    [toolProof('refuteEvidence','Command output, exit codes and observed state')],
    'Reviewer proofChecked is an agent-returned string array; no separate capture artifact is enforced by refute.js.');
  c('gate','Coordinator reconciles findings','rails',
    [io('verdicts','All original adversary verdicts','Check required reviewers and unresolved blocking findings'),
      contractRead('Acceptance and success definition','Apply the acceptance standard'),
      sourceCode('Current affected code','Adjudicate findings against live code'),
      io('implEvidence','Complete implementation proof when used','Establish acceptance from rerunnable evidence'),
      io('refuteEvidence','Independent verification when used','Establish acceptance from rerunnable evidence'),
      io('finalEvidence','Closing make build / make test responses','Require the closing green result','tool response')],
    [io('gate','Accepted or rejected','Drive the terminal report')],
    'No GATE script or structured Gate type. Skills specify the coordinator’s work; exact evidence filenames are run-specific.');
  c('gate','Closing build and test commands','build',definedToolReads('implementation','signatures','rules','tests'),
    [toolProof('finalEvidence','make build and make test output with exit codes')],
    'Required by the existing GATE skill; no selected-rule-warning override.');
  c('report','Coordinator writes terminal report','rails',
    [contractRead('Success definition','Choose SUCCEEDED or FAILED'),
      io('gate','Acceptance result','State outcome'),
      io('verdicts','Each lens verdict','Report review outcome'),
      io('finalEvidence','Final build/test proof','Report counts and proof paths'),
      io('workFolder','Run record','Locate original evidence and shipping record','file set')],
    [io('report','Outcome, verdicts, proof references and next action','Inform the human')],
    'No REPORT script. No REVIEW stage is defined in the existing workflow skill.');

  // REPLACEMENT DESIGN: exact definitions where present; proposals stay explicitly unresolved.
  n('conversation','Human and Guard create the initial documents','documentFlow',
    [io('request','Issue requirements, or supplied issue text','Describe the work','external record')],
    [io('conversation','IConversation fields','Own scope, investigations, capabilities, platforms and decisions'),
      io('workflow','IWorkflow identity, paths, level and stage collection','Own execution tracking')],
    'Initial creation only. Neither document is an input to this stage. Any later document reads belong to the stage performing that read; no self-input is inferred.');
  n('ground','Exploration agent','conversationType',
    [io('conversation','InvestigationAreas[].Focus; Scope; Exclusions','Investigate the selected area'),
      sourceCode('Derive facts, affected stores and open questions')],
    [io('exploration','Area, code facts/evidence, surfaces, open questions, autoResolved','Support design and contract work')],
    'Output responsibilities are recorded in the GROUND checklist. Exploration result type and source-version binding are not yet defined.');
  n('ground','Prior-art search agent — formulate search','prior',
    [io('workflow','StageAttempt.Assignments[assignmentId]: PriorArtSearchAssignment','Locate the pinned Conversation and selected capability'),
      io('conversation','Capabilities[CapabilityId].Description; Scope; Exclusions','Understand exactly what existing behavior to find')],
    [io('searchRequest','StructuralSearchRequest.Query or TextSearchRequest.Pattern/MatchKind; AssignmentReference','Ask Guard to run the selected search')],
    'A request is saved once by Guard. There is no standalone assignment/input file.');
  n('ground','Guard — IPriorArtSearch.SearchAsync','capture',
    [io('searchRequest','Assignment, query/pattern and match kind','Select and execute the backend'),
      io('workflow','ProjectPath and referenced assignment','Resolve project and registered work'),
      io('conversation','Pinned capability, scope and exclusions','Bound the request without copied instructions'),
      sourceCode('CodeGraph / ripgrep search within scope')],
    [io('searchCapture','RequestPath, ExecutedCommand, SearchExecutionResult','Retain actual execution, output, error and status')],
    'SearchReceipt returns CapturePath. It is a return value, not an additional saved file.');
  n('ground','Prior-art search agent — inspect result','prior',
    [io('searchCapture','Command/result and RequestPath','Distinguish matches, completed-empty and failed searches')],
    [io('search','Assignment, Searches[{Id,CapturePath}], Candidates[{Id,File,Symbol,FoundBySearchIds}]','Retain candidate identities tied to the original evidence')],
    'Search details remain in the capture. The result does not repeat the query, output or status.');
  n('ground','Reuse judge','prior',
    [io('workflow','PriorArtEvaluationAssignment.SearchResult; original search assignment','Resolve the exact search result and original instructions'),
      io('search','Candidates, Searches and Assignment','Identify the code being evaluated'),
      io('conversation','Selected capability, scope and exclusions','Determine whether existing behavior satisfies the requirement'),
      io('searchCapture','Captured source/search/inspection evidence','Support candidate behavior and limitations')],
    [io('evaluation','Evaluations and one ReuseDecision: Reuse / Extend / Extract / New / Unresolved','Determine how the existing code can serve the work')],
    'EvaluationEvidence refers to captured evidence. The operation that captures additional source inspection remains unspecified; it must not be silently treated as implemented.');
  n('ground','Guard — save prior-art ledger references','prior',
    [io('search','Saved file identity and Git blob ID','Link the original search result'),
      io('evaluation','Saved file identity and Git blob ID','Link the original determination')],
    [io('ledger','Entries[{SearchResult,EvaluationResult}]','Group the two original artifacts per capability')],
    'Guard writes references. The workflow script does not read files or reconstruct the determinations.');
  n('ground','Platform/API research agent — formulate request','research',
    [io('workflow','PlatformResearchAssignment.Conversation and CapabilityId','Locate the assigned capability'),
      io('conversation','Capability description, TargetPlatforms, Scope, Exclusions','Select platform-specific research questions')],
    [io('researchRequest','Documentation, SDK, RefLib or source-read request plus AssignmentReference','Request external evidence through Guard')],
    'Each platform uses the same intended findings structure; that findings type remains undefined.');
  n('ground','Guard — IPlatformResearch operations','research',
    [io('researchRequest','Query/domains, search root/pattern, corpus/section or source URI','Execute the requested operation'),
      io('workflow','Assignment and project context','Resolve registered research'),
      io('conversation','Pinned capability, platforms, scope and exclusions','Apply the registered research scope'),
      io('externalApi','Website content, local SDK/library source, RefLib response','Obtain source evidence','external/file set')],
    [io('researchExecuted','Actual backend request','Retain what was sent'),
      io('researchResponse','Returned content, including partial failure response when present','Retain original source bytes'),
      io('researchCapture','Request, Backend, ExecutedRequest, Outcome references','Link request and response with outcome')],
    'ResearchReceipt returns a versioned capture reference. It is not another saved file.');
  n('ground','Platform/API research agent — produce findings','research',
    [io('researchCapture','Outcome and response reference','Identify completed or failed research'),
      io('researchResponse','Original source content','Support platform capability/limitation findings'),
      io('conversation','Selected capability and TargetPlatforms','Cover every selected platform')],
    [io('research','Findings for each selected platform; unsupported behavior and differences','Support shared-interface and platform implementation design')],
    'Findings contents are required by the recorded flow; result and evidence-link fields remain undefined. Merely referencing ExecutedRequest does not establish that this agent reads that file.');
  proposal('ground','Exploration evidence capture','map',
    [sourceCode('Support exploration claims')],
    [io('explorationCapture','Original inspection/probe output','Support exploration findings')],
    'A separate capture format, capturing operation and reader binding have not been defined. Not counted as an established file.');
  n('design','Design agents — read registered GROUND results','assignments',
    [io('workflow','DesignAssignment.ExplorationResults, PriorArtLedger, PlatformResearchResults','Select original input versions'),
      io('exploration','Grounded facts, affected surfaces and open questions','Ground proposed design'),
      io('ledger','SearchResult and EvaluationResult references','Locate prior-art evidence'),
      io('search','Candidates and search references','Know what existing code was found'),
      io('evaluation','ProvidedBehavior, MissingBehavior, Limitations, Decision, RequiredChanges','Choose reuse, extension or extraction'),
      io('research','Per-platform findings and unsupported behavior','Design the shared interface and platform-specific implementations')],
    [io('proposals','Candidate design approaches','Supply the design-selection work')],
    'DesignAssignment pins these result files. It does not yet define a Conversation input for the task requirements or a separate assignment shape for the design judge.');
  proposal('design','Design selection and enabled design reviews','design',
    [io('proposals','Original candidate approaches, rules, risks and reuse choices','Select the approach and review its consequences'),
      io('conversation','Scope and accepted decisions','Keep design within the intended work'),
      sourceCode('Check proposed ownership and feasibility'),
      instructions('Enabled design-review criteria','Judge the proposed design')],
    [io('design','Selected approach, rules and reuse instructions','Supply CONTRACT'),
      io('designReviews','Original enabled reviewer findings','Identify design corrections')],
    'Purpose exists in the diagram and current process. Exact new judge/reviewer assignments, fields and result references are not settled. The selected design must not replace original proposals.');
  n('contract','Guard — form Contract from approved Conversation','documentFlow',
    [io('conversation','Relevant approved instructions and decisions','Create the governing contract')],
    [io('contract','Extracted approved requirements','Govern subsequent stages')],
    'Extraction point, field mapping and source-reference format remain undefined. This edge does not authorize copying all Conversation content.');
  proposal('contract','Incorporate design and check forced decisions','rails',
    [io('design','Selected design, proposed rules and reuse instructions','Record the approved approach'),
      io('exploration','Affected surfaces and facts','Form checkable work and acceptance'),
      io('ledger','Prior-art references','Keep reuse evidence linked'),
      contractRead('Draft requirements and Decisions','Find missing forced decisions'),
      sourceCode('Check feasibility and forced choices'),
      instructions('Contract, decision and prior-art criteria','Check the contract')],
    [io('contract','Approved instructions and acceptance checks','Supply downstream work'),
      io('hiddenCandidates','Original hunter findings','Supply the hidden-decision filter')],
    'These are existing process responsibilities. New Contract and hunter assignment/result bindings are not defined; neither old inline payloads nor proposed file splits are assumed final.');
  proposal('contract','Filter hidden decisions','hidden',
    [io('hiddenCandidates','Original choices and supporting evidence','Identify choices requiring human decisions'),
      instructions('Decision boundary and approved principles','Separate resolved principles from open choices')],
    [io('hidden','Unresolved choices and applied-principle evidence','Require decisions before leaving CONTRACT')],
    'Do not add a Contract read merely because the filter is in CONTRACT: the existing filter has none. Whether the new filter must read it is an explicit input-design gap.');
  proposal('contract','Preflight and opening verification','preflight',
    [contractRead('Required sections, paths, checks and Decisions','Check executable requirements'),
      ...definedToolReads(),instructions('Contract and approval criteria','Check requirements')],
    [io('contractCheck','Defects and checks performed','Identify requirements that cannot be applied'),
      io('baseline','Opening build/test observations','Establish the pre-change state')],
    'Preflight/build responsibilities exist. Guard capture types and exact new preflight inputs are not defined.');
  proposal('contract','Resolve CONTRACT exit','map',
    [io('hidden','Unresolved forced decisions','Close the decisions needed by downstream stages'),
      io('contractCheck','Contract defects','Correct requirements that would invalidate downstream work'),
      io('baseline','Opening verification','Require the opening state'),
      contractRead('Recorded decisions','Check closure')],
    [io('contract','Approved corrections only','Supply settled instructions')],
    'No unresolved forced choice may leave CONTRACT. New closure references and exit predicate are still to be specified.');
  n('spec','Spec planning agent','stages',
    [contractRead('Required observable behavior and acceptance','Plan BDD scenarios')],
    [io('specPlan','PlannedCase.Id, Description, Expectation, Reason, HumanInput','Tell TEST which spec scenarios to implement')],
    'SPEC follows CONTRACT and writes only a plan. The precise assignment type is not defined.');
  n('architecture','Architecture coding agent','stages',
    [contractRead('Decided interfaces and signatures','Write the approved surface'),
      sourceCode('Existing declarations and callers','Extend signatures without inventing behavior')],
    [io('signatures','Approved new/changed declarations or no source changes','Expose the surface to RULE_PLAN and TEST_PLAN')],
    'ARCHITECTURE precedes RULE in the new sequence. No separate selected-design read is counted: approved shape is carried by Contract.');
  proposal('architecture','Compile the architecture surface','architecture',definedToolReads('signatures'),
    [io('architectureEvidence','Build output and exit','Support build state')],
    'Separate tool capture binding remains undefined. Production build still must pass under the diagram’s existing required states.');
  proposal('architecture','Architecture agent returns the surface determination','architecture',
    [contractRead('Approved shape','Identify the surface required'),io('signatures','Actual declarations','Identify the delivered changes','file set'),
      io('architectureEvidence','Original build response','Reference the observed build state')],
    [io('architecture','Authored surface/no-signature determination','Identify what human sign-off covers')],
    'Current signatureDiff/buildProof strings need a new reference-based binding; the draft has not selected replacement fields.');
  proposal('architecture','Enabled architecture review agents','reviewers',
    [contractRead('Approved shape','Judge the surface'),io('signatures','Actual declarations and permitted stubs','Review shape','file set'),
      io('architecture','Surface/no-signature determination','Judge the original architecture result'),
      io('architectureEvidence','Build response','Check the claimed build state'),sourceCode('Check existing ownership'),
      instructions('Enabled SOLID / DRY / Lie-catcher criteria','Review the surface')],
    [io('architectureReviews','Original enabled reviewer verdicts','Identify required corrections')],
    'Reviewer selection is recorded. New assignment/result references remain undefined.');
  proposal('architecture','Human reviews the architecture surface','architecture',
    [io('architecture','Surface/no-signature determination','Review what the architecture stage delivered'),
      io('signatures','Changed declarations','Inspect the actual shape','file set'),
      io('architectureReviews','Original verdicts','See unresolved architecture findings')],[],
    'Human sign-off exists in the current process. The new certified approval/reference structure is not yet selected. This read is not erased merely because the consumer is human.');
  n('rule-plan','Rule planning agent','stages',
    [contractRead('Approved Rules to add','Plan the enforcement cases'),
      io('signatures','Architecture declarations','Identify what the rules must enforce','file set')],
    [io('rulePlan','PlannedCase entries for enforcement','Direct RULE writing and expected-outcome evaluation')],
    'Recorded sequence explicitly uses architecture. No extra Architecture-result file read is assumed.');
  n('rule','Rule-writing agent','planning',
    [io('rulePlan','Planned cases and expectations','Implement the rule plan'),
      contractRead('Approved rule requirements','Preserve the authorized enforcement'),
      sourceCode('Existing rule helpers and test infrastructure','Implement enforcement through existing owners')],
    [io('rules','Rule implementation and verification fixtures','Enforce the approved requirements','file set')],
    'Plan execution is separate from planning. Exact author assignment and structured result remain undefined.');
  n('test-plan','Detailed test planning agent','stages',
    [contractRead('Acceptance behavior','Plan tests that prove the requirements'),
      io('signatures','Architecture declarations','Plan tests against the decided surface','file set')],
    [io('testPlan','Detailed PlannedCase entries','Direct TEST writing and expected-outcome evaluation')],
    'TEST_PLAN follows RULE and uses architecture. Mere sequence does not establish a read of the Rule Plan, rule results or Architecture-result document.');
  proposal('test-plan','BDD/detail-plan coordination','map',
    [io('specPlan','Existing BDD scenario cases','Avoid duplicating or contradicting the detailed test plan')],
    [],'This is a candidate input, not a settled one. The relationship between SPEC cases and detailed test cases must be defined before counting this read.');
  n('test','Spec-test coding agent','stages',
    [io('specPlan','BDD cases','Write the planned spec tests'),
      io('signatures','Actual declarations','Compile against the approved surface','file set')],
    [io('tests','Spec-test source','Prove observable behavior','file set')],
    'Spec and unit work fan out. Their assignment/result schemas are not defined.');
  n('test','Unit-test coding agent','stages',
    [io('testPlan','Detailed cases','Write the planned unit tests'),
      io('signatures','Actual declarations','Compile against the approved surface','file set')],
    [io('tests','Unit-test source','Prove detailed behavior','file set')],
    'Original plans are inputs, not rewritten test-agent instructions.');
  for(const [stage,plan] of [['spec','specPlan'],['rule-plan','rulePlan'],['test-plan','testPlan']]) {
    n(stage,'Human guidance on Unknown planned cases','plans',
      [io(plan,'Cases with Expectation=Unknown and their Reason','Give engineering guidance on unresolved outcomes')],
      [io(plan,'Only the relevant case’s certified HumanInput','Carry guidance to the independent evaluator')],
      'No separate guidance document. HumanInput is optional; Unknown still requires independent result evaluation.');
  }
  for(const [stage,code,result,evidence,evaluation,plans,review] of [
    ['rule','rules','ruleResults','ruleEvidence','ruleEvaluation',['rulePlan'],'ruleReviews'],
    ['test','tests','testResults','testEvidence','testEvaluation',['specPlan','testPlan'],'testReviews']
  ]) {
    proposal(stage,'Execute planned checks and retain actual results','planning',
      [io(code,'Created rule/test source','Run the actual checks','file set'),...definedToolReads(...(stage==='rule'?['signatures']:['signatures','rules']))],
      [io(evidence,'Original command output, exit and observed state','Keep execution evidence separate from agent interpretation'),
        io(result,'Case-to-observed-outcome relationship','Compare execution with the plan')],
      'Execution is required. The result/capture types and exact case-link fields are not yet defined. No mandatory RED is inferred.');
    n(stage,'Independent evaluator — Unknown or unexpected outcome','planning',
      [...plans.map(k=>io(k,'Case Description, Expectation, Reason, HumanInput','Understand expected behavior and human guidance')),
        io(evidence,'Original observed outcome','Evaluate what actually happened'),
        io(result,'Case associated with that outcome','Identify which expectation needs evaluation'),
        sourceCode('Pre-implementation behavior relevant to the case','Determine whether the outcome is correct at this stage')],
      [io(evaluation,'Evaluation of the Unknown/mismatched outcome','Resolve the specific result question')],
      'Only Unknown or mismatched outcomes. Output schema and result-to-capture binding remain undefined; no invented fields are asserted here.');
    proposal(stage,stage==='test'?'Test Reviewer + enabled DRY / Lie-catcher reviewers':'Enabled SOLID / DRY / Lie-catcher reviewers','reviewers',
      [contractRead('Governing requirements','Review code against the spec'),
        io(code,'Authored rule/test code','Inspect actual behavior and duplication','file set'),
        io(result,'Coverage and observed result links','Inspect completeness'),
        io(evidence,'Original execution proof','Check assertions independently'),
        io(evaluation,'Applicable outcome determinations','Review resolved Unknown/mismatch cases'),
        instructions('Enabled reviewer criteria','Apply only the required reviewers')],
      [io(review,'Original per-reviewer findings','Identify needed code corrections')],
      'Reviewer selection is settled. Exact new input fields and which supporting evidence each reviewer reads remain to be defined; Test Reviewer is always present when TEST runs.');
  }
  proposal('implement','Implementation coding agent','implement',
    [contractRead('Required implementation and acceptance','Implement approved behavior'),
      io('signatures','Decided source declarations','Preserve architecture','file set'),
      io('rules','Live enforcement','Satisfy rules without altering them','file set'),
      io('tests','Spec/unit test assertions','Satisfy the required behavior','file set'),
      io('ruleResults','Original rule outcomes/impact','Understand work still assigned to implementation'),
      io('testResults','Original test coverage and outcomes','Understand tests already passing and those still requiring implementation'),
      sourceCode('Existing behavior and owners','Make the change')],
    [io('implementation','Authorized production changes','Deliver the requirement','file set')],
    'Current worker uses Contract + rule/test results + live code. New schemas must replace the old every-row-red check; an extra Architecture-result read is not established.');
  proposal('implement','Implementation verification','implement',
    [...definedToolReads('implementation','signatures','rules','tests'),contractRead('Acceptance commands','Run the required checks')],
    [io('implEvidence','Build, rule, test and acceptance responses','Establish delivered behavior')],
    'Keep tool evidence separate from the coding agent’s explanation. Capture/reference schema remains open.');
  proposal('implement','Implementation coding agent — return result after verification','implement',
    [io('implementation','Delivered changes','Identify implemented work','file set'),
      io('implEvidence','Observed acceptance results','Reference the actual execution')],
    [io('implResult','Acceptance results and unresolved implementation blockers','Supply completion evidence')],
    'Continuation of the same agent. Result/capture reference fields remain undefined.');
  proposal('readiness','Readiness reconciliation','readiness',
    [contractRead('Scope, decisions, surfaces and acceptance','Determine whether REFUTE can apply the requirements'),
      sourceCode('Delivered changes and identifiers','Check contract applicability')],
    [io('readiness','Readiness result and unresolved blocking mismatches','Control entry to REFUTE')],
    'No blanket reads of every old report/review. New replacements for panelInstruction and decisionsGiven are undefined, so this input list is not complete.');
  proposal('readiness','Check readiness before REFUTE','map',
    [io('readiness','Unresolved blocking mismatches','Resolve blockers before the requested transition')],[],
    'Consumer responsibility is the transition check, not an invented read by every REFUTE reviewer. Exit/reference fields remain undefined.');
  proposal('implement','Check implementation completion','map',
    [io('implResult','Completion, acceptance evidence and implementation blockers','Determine whether implementation is ready for the next stage'),
      io('implEvidence','Relevant observed results','Support the completion decision')],[],
    'No rewritten completion report. The new completion predicate and evidence-reference fields remain undefined.');
  proposal('refute','Enabled independent adversaries','refute',
    [contractRead('Complete success definition and acceptance','Judge the delivered work'),
      sourceCode('Implementation, rule and test code plus diffs','Find defects in actual behavior and ownership'),
      io('exploration','Original claims and applied principles','Audit grounding'),
      io('ledger','Prior-art result references','Locate reuse evidence'),
      io('search','Original candidates','Inspect ownership evidence'),
      io('evaluation','Original reuse decisions','Check actual reuse'),
      io('proposals','Original design proposals','Audit choices and applied principles'),
      io('design','Selected design','Audit approved design choices'),
      io('hidden','Forced decisions and resolutions','Check no decision was silently defaulted'),
      instructions('Enabled reviewer criteria','Apply the selected lenses')],
    [io('verdicts','Original findings, observations, attempts and checked proof','Drive correction or acceptance')],
    'Existing script consumes three aggregate stage files. Mapping them to the new original result files is not settled. Other stage-result/capture reads must be explicitly assigned, not inferred from reachable links.');
  proposal('refute','Independent verification and finding adjudication','refute',
    [...definedToolReads('implementation','signatures','rules','tests'),contractRead('Required checks','Execute acceptance independently'),
      io('verdicts','Original findings','Determine required corrections')],
    [io('refuteEvidence','Original independent check responses','Support verdicts and acceptance')],
    'New capture references are undefined. Adjudication must not overwrite the original verdict.');
  proposal('gate','Coordinator accepts or rejects','rails',
    [contractRead('Success definition','Apply the acceptance standard'),
      io('verdicts','Required review results and unresolved findings','Determine whether review permits acceptance'),
      io('implEvidence','Relevant valid implementation proof','Establish acceptance'),
      io('refuteEvidence','Relevant valid independent proof','Establish acceptance'),
      io('finalEvidence','Closing verification if performed','Use the closing result when it is the selected proof'),
      sourceCode('Delivered code if needed for adjudication','Check the finding against the tree')],
    [io('gate','Accepted/rejected outcome','Supply REPORT')],
    'No Gate schema or evidence-selection binding is defined. The old skill’s closing verification is shown separately, not assumed to require a copied proof file.');
  proposal('gate','Closing verification when performed','rails',definedToolReads('implementation','signatures','rules','tests'),
    [io('finalEvidence','New closing build/test responses','Supply acceptance and report proof')],
    'Whether existing valid captures satisfy the new closing check must be settled; this is not an extra duplicate of IM/RF evidence.');
  proposal('report','Coordinator presents the outcome','rails',
    [contractRead('Success definition','State the outcome against the requirement'),
      io('gate','Acceptance outcome','Report accepted/rejected'),
      io('verdicts','Original verdicts','Report review outcome'),
      io('implEvidence','Proof when this is the selected acceptance evidence','Link the evidence, not re-create it'),
      io('refuteEvidence','Proof when this is the selected acceptance evidence','Link the evidence, not re-create it'),
      io('finalEvidence','Proof when a new closing check ran','Link the evidence, not re-create it')],
    [io('report','Human-facing result and next action','Inform the human')],
    'Report input references are not yet typed. Conditional evidence alternatives do not require reading all three sets.');
  proposal('review','Terminal REVIEW','stages',[],[],
    'Only the terminal state is defined. No agent, document reader, output or additional review duty has been specified.');

  // Tracking is one shared responsibility, applied only to stages with registered agent work.
  // No fictitious agent run is added for a terminal state or a human-only operation.
  const trackedStages=['ground','design','contract','spec','architecture','rule-plan','rule','test-plan','test','implement','readiness','refute'];
  for(const stage of trackedStages) n(stage,'Guard — register, dispatch and retain agent-run results','tracking',
    [io('workflow','Stages → Attempts → Assignments / AgentRuns; Level; ProjectPath; WorkingFolder','Launch registered work, preserve successful runs, resume missing work')],
    [io('workflow','Assignment inputs; AgentRun.Status / ResultPath / StartedAt / EndedAt','Track the actual execution without copied findings')],
    'Applies when this stage has agent assignments. Agent-specific assignment types exist only for prior-art, platform research and DESIGN. No current-stage/status fields are invented.');
  for(const [stage,key,source] of [['architecture','architectureReviews','architecture'],['rule','ruleReviews','rule'],['test','testReviews','test']]) {
    c(stage,'Correction agent reads refutationPath',''+source,
      [previousAttempt(io(key,'Confirmed findings in the supplied file','Apply the specifically authorized corrections'))],[],
      'Conditional: refutationPath may instead name final REFUTE findings. It is not an additional summary required by the script.');
  }
  for(const [stage,key] of [['design','designReviews'],['architecture','architectureReviews'],['rule','ruleReviews'],['test','testReviews'],['implement','verdicts']]) {
    proposal(stage,'Correction attempt reads original findings','tracking',
      [previousAttempt(io(key,'Findings relevant to this authorized correction','Correct affected work without restarting successful work'))],[],
      'Reuse original results by reference. Exact correction-assignment types are not defined.');
  }
  for(const [stage,key] of [['design','designReviews'],['architecture','architectureReviews'],['rule','ruleReviews'],['test','testReviews']]) {
    proposal(stage,'Consume stage reviews before transition','reviewers',
      [io(key,'Required original verdicts and unresolved findings','Determine which corrections block leaving this stage')],[],
      'A review is consumed inside its producing stage even when no later stage opens it. New exit-check field bindings remain undefined.');
  }

  const umbrella={groundEvidence:['searchCapture','explorationCapture','researchCapture','researchExecuted','researchResponse']};
  const stageFindings={
    conversation:'Initial Conversation creates the Workflow and Conversation documents. It does not read them as pre-existing inputs. Human messages are external input; they are not a second document invented for the chart.',
    ground:'Current ground.js keeps facts and reuse verdicts in its completed result but omits successful search results. Platform/API research has a draft Guard interface, not a current GROUND agent. The new prior-art search and reuse-judge file handoff is defined separately.',
    design:'Current DESIGN proposals read factsPath; judgePrompt does not forward that path. It forwards proposals as JSON and ledgerPath as a file. New DesignAssignment pins GROUND results but lacks a pinned task-requirements reference. Do not invent that field in the chart.',
    contract:'Current hidden-decision hunters read Contract. The filter receives their pooled candidate objects and governing instructions, not contractPath. The new Contract extraction and result-reference schema are not defined.',
    spec:'SPEC is a new planning stage, with no existing workflow script. IPlan defines cases, expectations, reasons and HumanInput, but not the planning-agent assignment or a complete stage result.',
    architecture:'Current architecture reviewers consume author JSON plus live skeleton files. signatureDiff and buildProof are strings in that JSON. TEST uses the actual source declarations; that does not imply a read of an Architecture-result document.',
    'rule-plan':'RULE_PLAN is new. Its recorded purpose consumes the architecture to plan enforcement. A separate read of the Architecture result, selected-design result or Spec Plan has not been specified.',
    rule:'Current ruleGenPrompt receives Rules to add from both Contract and caller-supplied rulesToAdd. ruleRefutePrompt receives the returned author JSON, not a saved rule-result file. The new Rule Plan must not silently become another rewritten rule list.',
    'test-plan':'TEST_PLAN is new and explicitly uses architecture. Following RULE in sequence does not establish that it reads Rule Plan or RULE results. The relationship between the BDD plan and detailed test plan still needs definition.',
    test:'Current tdd.js has one test author and no plan-file parameter. Reviewers receive author JSON. The new design splits spec-test and unit-test work and evaluates Unknown/mismatched outcomes; those assignment/result types remain undefined.',
    implement:'Current workerPrompt opens rulePhaseResultPath and tddResultPath plus Contract, then reads live source/tests/rules. It has no architectureResultPath. Path checks in JavaScript do not validate the file contents.',
    readiness:'Current readinessPrompt reads Contract, code and git status. panelInstruction and decisionsGiven arrive as caller-written values. No IMPLEMENT-result or previous reviewer-result path is supplied. The new replacement for those caller-written values is not defined.',
    refute:'Current refutePrompt opens the GROUND, DESIGN and hidden-decision aggregate files. DESIGN proposals are read inside that aggregate: they are not unused. It has no explicit RULE, TEST, IMPLEMENT, ARCHITECTURE or READINESS result input. New original-file bindings must be settled rather than inferred from links.',
    gate:'GATE is coordinator work, not a JavaScript command. Its existing skill consumes Contract, verdicts, live code and rerunnable proof, and requires closing build/test output. The new capture-selection and Gate-result structures remain open.',
    report:'REPORT informs the human. A human reader is a consumer even if no later stage reads the report. It must reference original proof rather than supply another authoritative account of it.',
    review:'REVIEW is only a named terminal state in the new design. No reader, output or extra judging task is defined. The map does not manufacture one.'
  };
  const commonRuntime={
    source:'runtime',
    inputs:'roles with prompt/schema/validation; roleGroups; priorPanelResults; retryRoles; priorStageResults. __workflowInput unwraps argument JSON. __requiredAgentRuntime validates supplied and returned objects and dispatches only the requested roles.',
    outputs:'results, panelComplete, failedRoles, priorPanelResults, retryRoles and nextGroupInput. Stage bodies return their declared aggregate or incomplete result; progress is emitted through log().',
    persistence:'These are runtime values. The scripts call agent(), parallel() and log(); they do not write result files. Downstream file paths rely on caller/runtime persistence outside these command bodies. The new Guard-managed persistence replaces that dependency; it is not implemented by the drafts.',
    referenceReads:'A governing skill can require another file. That file belongs to the Governing instructions set. A source/build import belongs to its source/configuration set. Work-folder contents and project-specific transitive reads cannot be enumerated as fixed filenames without a particular run. They are not counted as zero or as a new summary document.'
  };
  Object.assign(notes,{
    workFolder:'Current __workflowBrief requires every input-folder file. This is a dynamic set, not an additional document. A report placed there can become input on a later run. The new explicit assignments must not inherit this wildcard by accident.',
    design:'Current design.js returns this as verdict inside the same object that owns proposals. Downstream stageOutputPaths.design reads both. Separate new files require defined references; they are not current behavior.',
    proposals:'Current proposals are values forwarded to the design judge and members of the aggregate opened by REFUTE. New Design proposal artifacts are intended to retain original outputs; their schema/storage binding is unfinished.',
    architecture:'Current author object is embedded in architecture review prompts. The same stage aggregate contains verdicts. No dedicated Architecture-result path is accepted by current TEST or IMPLEMENT.',
    architectureEvidence:'Current buildProof is a string returned by the architecture agent, not a guaranteed tool-written file. The new separate-capture binding remains undefined.',
    ruleResults:'Current author fields and reviewer verdicts share the returned RULE aggregate; IMPLEMENT later opens the saved aggregate. The schema embeds proof strings. New results should identify cases and reference evidence; exact fields remain undefined.',
    testResults:'Current author fields and reviewer verdicts share the returned TDD aggregate; IMPLEMENT later opens the saved aggregate. New result/coverage/capture fields remain undefined.',
    finalEvidence:'Existing GATE skill requires closing command output. The replacement design has not defined whether/how valid earlier captures can satisfy it. Do not turn a proposed reuse option into current behavior.',
    workflow:'Approved replacement execution owner. Conversation initially creates it; Guard reads/updates registered assignments and agent runs in executing stages. The old scripts use runtime arguments instead. No invented agent run is assigned to REVIEW.',
    researchExecuted:'Produced by Guard according to PlatformResearchCapture.ExecutedRequest. No specific downstream reader of this separate file has been assigned yet. A reference alone does not prove a read.',
    report:'Human-facing output has value without a later automated consumer. REVIEW remains only a named terminal state.'
  });
  const index=key=>stages.findIndex(s=>s.id===artifacts[key][1]);
  const keys=Object.keys(artifacts).filter(k=>!umbrella[k]).sort((a,b)=>index(a)-index(b));
  let profile='design';
  const activeProcesses=()=>processes.filter(p=>p.profile===profile);
  function usages(key) { return activeProcesses().filter(p=>p.reads.includes(key)||p.writes.includes(key)); }
  function count(key) {
    const uses=usages(key),known=uses.filter(p=>p.basis==='defined'),pending=uses.filter(p=>p.basis==='unresolved'&&p.reads.includes(key));
    const readStages=[...new Set(known.filter(p=>p.reads.includes(key)).map(p=>p.stage))];
    const unresolvedStages=[...new Set(pending.map(p=>p.stage))];
    const additionalReaderStages=unresolvedStages.filter(s=>!readStages.includes(s));
    const order=stageOrder(),producer=order.indexOf(artifacts[key][1]);
    return {readStages,laterStages:readStages.filter(s=>order.indexOf(s)>producer),
      writerStages:[...new Set(known.filter(p=>p.writes.includes(key)).map(p=>p.stage))],
      unresolvedStages,additionalReaderStages,additionalLaterStages:additionalReaderStages.filter(s=>order.indexOf(s)>producer)};
  }
  function cell(key,stage) {
    const local=usages(key).filter(p=>p.stage===stage);
    const read=local.some(p=>p.basis==='defined'&&p.reads.includes(key));
    const write=local.some(p=>p.basis==='defined'&&p.writes.includes(key));
    const pending=local.some(p=>p.basis==='unresolved');
    return {read,write,pending,label:(read?'R':'')+(write?'W':'')+(pending?'?':'')+(!read&&!write&&!pending?'—':'')};
  }
  function stageOrder() {
    return profile==='current'
      ? ['conversation','ground','design','contract','rule','architecture','test','implement','readiness','refute','gate','report']
      : stages.map(s=>s.id);
  }
  // This is diagram provenance, not a new workflow field or an agent assignment.
  // A read of an output produced inside this stage is still a usage, but is not
  // an incoming stage dependency. Existing documents can be both read and updated.
  function inputSource(p,edge) {
    if(edge.origin==='previous-attempt')return {kind:'previous-attempt',producers:[]};
    const owner=artifacts[edge.key][1];
    if(owner!==p.stage)return {kind:'incoming',stage:owner,producers:[]};
    const local=processes.filter(q=>q.profile===p.profile&&q.stage===p.stage);
    const writers=local.filter(q=>q!==p&&q.writes.includes(edge.key));
    const earlier=writers.filter(q=>local.indexOf(q)<local.indexOf(p));
    // For repeated writes (a draft followed by a correction), use the latest
    // earlier output. Parallel source writers remain a set, not invented copies.
    const producers=earlier.length?earlier.filter(q=>q===earlier.at(-1)||edge.form==='file set'):writers;
    return {kind:producers.length?'internal':'unresolved',stage:owner,producers};
  }
  function sourceLabel(p,edge) {
    const source=inputSource(p,edge);
    if(source.kind==='previous-attempt')return 'Previous attempt → this correction only';
    if(source.kind==='internal')return 'Inside '+stageSymbols[p.stage]+': '+source.producers.map(q=>q.id).join(' / ')+' → this step';
    if(source.kind==='unresolved')return 'Producer not defined in this view';
    return source.stage==='external'?'Existing / external → this stage':stageSymbols[source.stage]+' output → this stage';
  }
  function stageFlow(stageId) {
    const local=activeProcesses().filter(p=>p.stage===stageId),incoming=[],internal=[],corrections=[],unresolved=[];
    for(const p of local)for(const edge of p.inputs){
      const source=inputSource(p,edge),link={reader:p,edge,...source};
      ({incoming,internal,'previous-attempt':corrections,unresolved})[source.kind].push(link);
    }
    const unique=links=>[...new Set(links.map(link=>link.edge.key))];
    const known=incoming.filter(link=>link.reader.basis==='defined');
    const pending=incoming.filter(link=>link.reader.basis==='unresolved');
    return {local,incoming,internal,corrections,unresolved,
      inputs:unique(known),unresolvedInputs:unique(pending).filter(key=>!known.some(link=>link.edge.key===key)),
      outputs:[...new Set(local.filter(p=>p.basis==='defined').flatMap(p=>p.writes))],
      unresolvedOutputs:[...new Set(local.filter(p=>p.basis==='unresolved').flatMap(p=>p.writes))]
    };
  }
  function orderedSteps(flow) {
    const remaining=new Set(flow.local),done=new Set(),ordered=[];
    while(remaining.size){
      const next=[...remaining].find(p=>flow.internal.filter(link=>link.reader===p).every(link=>link.producers.every(q=>done.has(q))));
      if(!next)throw new Error('Circular substep dependencies in '+flow.local[0].stage);
      ordered.push(next);done.add(next);remaining.delete(next);
    }
    return ordered;
  }
  function syncStages() {
    for(const stage of stages) {
      const flow=stageFlow(stage.id);
      orderedSteps(flow); // Reject a circular internal handoff before rendering it.
      if(flow.unresolved.length)throw new Error('Missing internal producer in '+stage.id);
      const order=stageOrder();
      for(const link of flow.incoming){
        if(order.indexOf(link.stage)>order.indexOf(stage.id))throw new Error('Future-stage input without a correction origin: '+link.reader.id+' / '+link.edge.key);
      }
      stage.inputs=flow.inputs;
      stage.outputs=flow.outputs;
      stage.unresolvedInputs=flow.unresolvedInputs;
      stage.unresolvedOutputs=flow.unresolvedOutputs.filter(key=>!flow.outputs.includes(key));
      stage.internalInputs=[...new Set(flow.internal.map(link=>link.edge.key))];
      stage.correctionInputs=[...new Set(flow.corrections.map(link=>link.edge.key))];
    }
  }
  const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  function renderMatrix() {
    const order=stageOrder(),rowHeight=61,top=240,width=1720,height=top+keys.length*rowHeight+95;
    const text=(x,y,s,size=16,color='#20364b',weight=400)=>'<text x="'+x+'" y="'+y+'" font-size="'+size+'" fill="'+color+'" font-weight="'+weight+'">'+esc(s)+'</text>';
    let svg='<svg xmlns="http://www.w3.org/2000/svg" width="'+width+'" height="'+height+'" viewBox="0 0 '+width+' '+height+'" role="img" aria-label="Stage artifact consumers"><style>text{font-family:Arial,sans-serif}</style><rect width="100%" height="100%" fill="#fbfcfe"/>';
    svg+=text(24,40,'Artifact use — '+(profile==='current'?'existing scripts / required coordinator work':'replacement design: specified vs unresolved'),28,'#20364b',700);
    svg+=text(24,76,'R = consumed    W = produced/updated    ? = proposed or unspecified binding    — = no established use',18);
    svg+=text(24,108,'Counts are consuming stages, not physical files. Current-script results can be values, not saved files.',17);
    svg+=text(24,136,'Each connection’s actor, fields, purpose and transfer form are in the companion stage-by-stage page.',17);
    order.forEach((id,i)=>svg+=text(24+(i%8)*211,168+Math.floor(i/8)*25,stageSymbols[id]+' '+stages.find(s=>s.id===id).name,15));
    svg+=text(24,226,'Artifact ID / name',17,'#20364b',700);
    order.forEach((id,i)=>svg+=text(640+i*55,226,stageSymbols[id],16,'#20364b',700));
    svg+=text(1540,226,'Readers',17,'#20364b',700)+text(1630,226,'Later',17,'#20364b',700);
    keys.forEach((key,i)=>{
      const y=top+i*rowHeight,c=count(key);
      if(i%2===0)svg+='<rect x="15" y="'+y+'" width="1690" height="'+rowHeight+'" fill="#eef3f7"/>';
      svg+=text(24,y+23,ids[key],15,'#20364b',700)+text(24,y+47,artifacts[key][0],18);
      order.forEach((id,j)=>{const v=cell(key,id);svg+=text(640+j*55,y+36,v.label,17,v.pending?'#713e8e':v.read?'#235f91':v.write?'#225d3c':'#617381',v.label==='—'?400:700);});
      svg+=text(1550,y+36,c.readStages.length+(c.additionalReaderStages.length?' + ?':''),17);
      svg+=text(1640,y+36,c.laterStages.length+(c.additionalLaterStages.length?' + ?':''),17);
    });
    svg+=text(24,height-45,'Zero established reads with ? is a definition gap—not proof that an artifact is unused.',18);
    return svg+'</svg>';
  }
  function setProfile(next) {
    if(!['current','design'].includes(next))throw new Error('Unknown profile '+next);
    profile=next;syncStages();
  }
  function renderStage(stageId){
    const wrap=(s,max)=>{const lines=[];let line='';for(const word of String(s).split(/\s+/)){if(line&&line.length+word.length+1>max){lines.push(line);line=word;}else line+=(line?' ':'')+word;}if(line)lines.push(line);return lines;};
    const label=(x,y,s,max=125,size=18,weight=400,color='#20364b')=>wrap(s,max).map((line,i)=>'<text x="'+x+'" y="'+(y+i*24)+'" font-size="'+size+'" font-weight="'+weight+'" fill="'+color+'">'+esc(line)+'</text>').join('');
    const name=stages.find(s=>s.id===stageId).name;
    let y=140,body=label(35,42,stageSymbols[stageId]+' · '+name+' — inputs, work and outputs',90,28,700);
    body+=label(35,78,profile==='current'?'Existing prompts / code. Values are not claimed to be saved files.':'Replacement design. ? marks a proposed input/output binding.',125,18);
    const finding=stageFindings[stageId];body+=label(35,y,finding,139,18);y+=wrap(finding,139).length*24+40;
    const flow=stageFlow(stageId);
    for(const [heading,list] of [['ENTERING THIS STAGE',flow.inputs],['PROPOSED INCOMING BINDINGS',flow.unresolvedInputs]]){
      if(!list.length)continue;
      body+=label(35,y,heading,120,18,700);y+=30;
      const text=list.map(key=>ids[key]+' · '+artifacts[key][0]).join('  |  ');
      body+=label(35,y,text,139,18);y+=wrap(text,139).length*24+35;
    }
    for(const p of orderedSteps(flow)){
      body+='<path d="M35 '+y+' H1465" stroke="#acbecd"/>';y+=38;
      const title=p.id+' · '+p.name+(p.basis==='unresolved'?' — ? binding not settled':'');
      body+=label(35,y,title,112,22,700);y+=wrap(title,112).length*24+17;
      const [file,where]=sources[p.source];body+=label(35,y,file+' · '+where,155,15);y+=wrap(file+' · '+where,155).length*24+12;
      for(const direction of ['inputs','outputs']){
        if(direction==='outputs'){
          body+='<path d="M750 '+y+' v28" stroke="#637d92" marker-end="url(#audit-arrow)"/>';y+=62;
          const h=wrap(p.name,108).length*24+35;
          body+='<rect x="120" y="'+(y-26)+'" width="1260" height="'+h+'" rx="8" fill="#e8eff8"/>'+label(145,y,p.name,108,20,700);y+=h+12;
          body+='<path d="M750 '+y+' v25" stroke="#637d92" marker-end="url(#audit-arrow)"/>';y+=50;
        }
        const output=direction==='outputs';
        body+=label(35,y,output?'OUTPUTS ↓':'INPUTS ↓',80,17,700,output?'#225d3c':'#235f91')+label(460,y,output?'INFORMATION PRODUCED':'INFORMATION USED',48,16,700)+label(965,y,'PURPOSE',48,16,700);y+=28;
        if(!p[direction].length){body+=label(35,y,output?'No separate artifact specified.':'No document input specified.',110,18);y+=34;}
        for(const e of p[direction]){
          const artifact=artifacts[e.key],name=ids[e.key]+' · '+artifact[0];
          const provenance=output?'Produced by '+p.id:sourceLabel(p,e);
          const lines=Math.max(wrap(name,32).length+1,wrap(e.information,48).length,wrap(e.purpose,48).length);
          const h=lines*24+30+wrap(provenance,135).length*22;
          body+='<rect x="35" y="'+(y-20)+'" width="1430" height="'+h+'" fill="'+(output?'#edf7ef':'#edf5ff')+'"/>';
          body+='<use href="#file-'+artifact[3][0]+'" x="45" y="'+(y-13)+'" width="28" height="28"/>';
          body+=label(85,y,name,32,17,700)+label(85,y+wrap(name,32).length*24,e.form,32,15);
          body+=label(460,y,e.information,48,18)+label(965,y,e.purpose,48,18);
          body+=label(85,y+lines*24+6,provenance,135,16,700,output?'#225d3c':'#235f91');y+=h+8;
        }
      }
      if(p.note){body+=label(35,y+10,p.note,143,17);y+=wrap(p.note,143).length*24+28;}
      if(p.runtime.length){const line='Non-file arguments: '+p.runtime.join(' ');body+=label(35,y,line,143,17);y+=wrap(line,143).length*24+22;}
      y+=28;
    }
    body+=label(35,y,'Internal input labels identify the producing step. Previous-attempt inputs apply only on correction; they are not first-run prerequisites.',145,17);y+=70;
    return '<svg xmlns="http://www.w3.org/2000/svg" width="1500" height="'+y+'" viewBox="0 0 1500 '+y+'" role="img" aria-label="'+esc(name)+' stage input output audit"><style>text{font-family:Arial,sans-serif}</style><defs><marker id="audit-arrow" markerWidth="8" markerHeight="8" refX="6" refY="4" orient="auto"><path d="M0 0 L7 4 L0 8" fill="#637d92"/></marker></defs>'+api.modificationIconDefs+'<rect width="100%" height="100%" fill="#fbfcfe"/>'+body+'</svg>';
  }
  for(const key of Object.keys(artifacts)){
    if(!ids[key])throw new Error('Missing artifact identifier '+key);
    artifacts[key][4]=ids[key];
  }
  if(new Set(Object.values(ids)).size!==Object.keys(ids).length)throw new Error('Duplicate artifact ID');
  for(const p of processes){
    if(!sources[p.source])throw new Error('Missing source '+p.id);
    for(const e of [...p.inputs,...p.outputs]){
      if(!artifacts[e.key]||!e.information||!e.purpose||!e.form)throw new Error('Incomplete edge '+p.id);
    }
    for(const direction of ['inputs','outputs']){
      const keys=p[direction].map(e=>e.key);
      if(new Set(keys).size!==keys.length)throw new Error('Duplicate edge '+p.id+' '+direction);
    }
  }
  syncStages();
  api.artifactUsage={ids,notes,sources,processes,umbrella,keys,count,usages,cell,renderMatrix,renderStage,activeProcesses,stageOrder,setProfile,stageFlow,orderedSteps,inputSource,sourceLabel,stageFindings,commonRuntime,get profile(){return profile;}};
})();
