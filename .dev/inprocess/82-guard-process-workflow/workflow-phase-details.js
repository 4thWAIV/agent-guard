// Presentation data for the design discussion, not executable workflow policy.
(() => {
  // Names/icons and stage descriptions. Process I/O is owned by workflow-artifact-usage.js.
  const artifacts = {
  "request": [
    "GitHub Issue",
    "external",
    "source",
    [
      "workflow"
    ]
  ],
  "project": [
    "Source Code",
    "external",
    "source",
    [
      "production",
      "test",
      "rule"
    ]
  ],
  "workflow": [
    "Workflow",
    "external",
    "file",
    [
      "workflow"
    ]
  ],
  "rails": [
    "Governing instructions",
    "external",
    "source",
    [
      "workflow"
    ]
  ],
  "conversation": [
    "Conversation",
    "conversation",
    "file",
    [
      "workflow"
    ]
  ],
  "exploration": [
    "Exploration results",
    "ground",
    "file",
    [
      "output"
    ]
  ],
  "search": [
    "Prior-art search results",
    "ground",
    "file",
    [
      "output"
    ]
  ],
  "evaluation": [
    "Reuse evaluations",
    "ground",
    "file",
    [
      "output"
    ]
  ],
  "ledger": [
    "Prior-art ledger",
    "ground",
    "file",
    [
      "output"
    ]
  ],
  "research": [
    "Platform/API research results",
    "ground",
    "file",
    [
      "output"
    ]
  ],
  "groundEvidence": [
    "GROUND tool captures",
    "ground",
    "file",
    [
      "evidence"
    ]
  ],
  "proposals": [
    "Design proposals",
    "design",
    "file",
    [
      "output"
    ]
  ],
  "design": [
    "Selected design result",
    "design",
    "file",
    [
      "output"
    ]
  ],
  "designReviews": [
    "DESIGN review results",
    "design",
    "file",
    [
      "output"
    ]
  ],
  "contract": [
    "Contract",
    "contract",
    "file",
    [
      "workflow"
    ]
  ],
  "hidden": [
    "Hidden-decision results",
    "contract",
    "file",
    [
      "output"
    ]
  ],
  "contractCheck": [
    "Contract-check results",
    "contract",
    "file",
    [
      "output"
    ]
  ],
  "baseline": [
    "Opening verification captures",
    "contract",
    "file",
    [
      "evidence"
    ]
  ],
  "specPlan": [
    "Spec Plan",
    "spec",
    "file",
    [
      "workflow"
    ]
  ],
  "architecture": [
    "Architecture result",
    "architecture",
    "file",
    [
      "output"
    ]
  ],
  "signatures": [
    "Application signature files",
    "architecture",
    "code",
    [
      "production"
    ]
  ],
  "architectureReviews": [
    "ARCHITECTURE review results",
    "architecture",
    "file",
    [
      "output"
    ]
  ],
  "architectureEvidence": [
    "Architecture build captures",
    "architecture",
    "file",
    [
      "evidence"
    ]
  ],
  "rulePlan": [
    "Rule Plan",
    "rule-plan",
    "file",
    [
      "workflow"
    ]
  ],
  "rules": [
    "Rule files and fixtures",
    "rule",
    "code",
    [
      "rule",
      "test"
    ]
  ],
  "ruleResults": [
    "Rule execution results",
    "rule",
    "file",
    [
      "output"
    ]
  ],
  "ruleEvidence": [
    "Rule execution captures",
    "rule",
    "file",
    [
      "evidence"
    ]
  ],
  "ruleReviews": [
    "RULE review results",
    "rule",
    "file",
    [
      "output"
    ]
  ],
  "testPlan": [
    "Test Plan",
    "test-plan",
    "file",
    [
      "workflow"
    ]
  ],
  "tests": [
    "Spec and unit-test files",
    "test",
    "code",
    [
      "test"
    ]
  ],
  "testResults": [
    "Test execution results",
    "test",
    "file",
    [
      "output"
    ]
  ],
  "testEvidence": [
    "Test execution captures",
    "test",
    "file",
    [
      "evidence"
    ]
  ],
  "testReviews": [
    "TEST review results",
    "test",
    "file",
    [
      "output"
    ]
  ],
  "implementation": [
    "Implementation files",
    "implement",
    "code",
    [
      "production"
    ]
  ],
  "implResult": [
    "Implementation result",
    "implement",
    "file",
    [
      "output"
    ]
  ],
  "implEvidence": [
    "Implementation verification captures",
    "implement",
    "file",
    [
      "evidence"
    ]
  ],
  "readiness": [
    "READINESS result",
    "readiness",
    "file",
    [
      "output"
    ]
  ],
  "verdicts": [
    "REFUTE reviewer results",
    "refute",
    "file",
    [
      "output"
    ]
  ],
  "refuteEvidence": [
    "REFUTE verification captures",
    "refute",
    "file",
    [
      "evidence"
    ]
  ],
  "gate": [
    "GATE decision",
    "gate",
    "file",
    [
      "output"
    ]
  ],
  "finalEvidence": [
    "Final verification captures",
    "gate",
    "file",
    [
      "evidence"
    ]
  ],
  "report": [
    "Terminal report",
    "report",
    "file",
    [
      "output"
    ]
  ]
};
  const stages = [
  {
    "id": "conversation",
    "name": "Conversation",
    "purpose": "Describe the work and resolve the initial scope.",
    "status": "Conversation type drafted.",
    "issue": "Unclear requirement, scope, target platforms or approval choice.",
    "steps": [
      "Agent identifies the exact missing decision and its effect on the requested work.",
      "Research factual questions before presenting a human choice.",
      "Human settles the requirement; Guard updates the owning Conversation section."
    ],
    "close": "The intended work and required decisions are explicit. A missing answer is not an agent-selected default.",
    "diagram": {
      "work": [
        "Describe the intended work, scope and selected platforms.",
        "Identify the information needed to assign GROUND work."
      ],
      "exit": "GROUND assignments have the instructions they need.",
      "blocks": [
        "Missing capability, scope or platform selection → dependent GROUND assignment.",
        "Resolve in Conversation; reference the selected version."
      ],
      "basis": "Conversation inputs are defined; the exact exit predicate is proposed."
    }
  },
  {
    "id": "ground",
    "name": "GROUND",
    "purpose": "Explore the project, find reusable code and research platform primitives.",
    "status": "Prior-art types and search captures drafted; exploration and research findings still open.",
    "issue": "Missing API facts, conflicting evidence, incomplete searches or unresolved reuse.",
    "steps": [
      "Exploration → its saved exploration result. Prior-art search → saved search results and captures.",
      "Reuse judge reads those original search results and captures → a separate reuse evaluation. Guard links both in the ledger.",
      "Platform/API research reads Conversation capabilities and TargetPlatforms; documentation, SDK and RefLib operations produce captures. Findings reference those captures.",
      "Assign targeted follow-up for a factual gap. An approved probe may establish behavior when documentation cannot.",
      "A question that needs a human choice goes to Conversation. A design question goes to DESIGN with its evidence, not a fabricated research answer."
    ],
    "close": "Required research is evidenced. Any remaining item has an explicit dependent task; that dependency cannot be silently treated as satisfied.",
    "diagram": {
      "work": [
        "Explore assigned areas; retain facts and open questions.",
        "Search our code → reuse judge evaluates original results.",
        "Research required APIs/SDKs for each selected platform."
      ],
      "exit": "Required investigations are usable; no dependent design choice relies on an unanswered factual question.",
      "blocks": [
        "Search error or missing search → reuse determination.",
        "Unresolved reuse ruling → dependent DESIGN choice.",
        "Missing platform fact → dependent platform design.",
        "Research the specific gap; preserve successful results."
      ],
      "basis": "UnresolvedReuseDecision exists. Question-to-blocked-work links remain undefined."
    }
  },
  {
    "id": "design",
    "name": "DESIGN",
    "purpose": "Choose a design using the original GROUND findings.",
    "status": "DesignAssignment defined. Ledger references supply search results and reuse evaluations.",
    "issue": "Competing approaches, insufficient research, design defects or an unapproved trade-off.",
    "steps": [
      "Architects read saved evidence and return their own proposals.",
      "The design judge evaluates the original proposals and identifies the selected approach. Enabled stage reviewers assess the design.",
      "Missing facts return to the specific GROUND assignment; revise only affected proposals.",
      "Reserved choices go to the human with alternatives, recommendation and consequences."
    ],
    "close": "The selected approach answers the requirements, with applicable decisions resolved. Rewriting a proposal summary is not a design correction.",
    "diagram": {
      "work": [
        "Design agents produce proposals from original GROUND evidence.",
        "Design judge selects an approach; enabled reviewers examine it.",
        "Resolve risks that prevent the approach meeting requirements."
      ],
      "exit": "The selected approach is supported; required design choices are settled.",
      "blocks": [
        "Unsupported assumption → affected proposal.",
        "Unresolved design risk → affected approach selection.",
        "Missing fact → targeted GROUND work.",
        "Design choice → human where required."
      ],
      "basis": "Existing proposals carry risks; explicit resolution links are not defined."
    }
  },
  {
    "id": "contract",
    "name": "CONTRACT",
    "purpose": "Establish the approved instructions and acceptance requirements.",
    "status": "Structured Contract schema and extraction operations still undefined.",
    "issue": "Forced choices, conflicting requirements or missing authority.",
    "steps": [
      "Extract the relevant approved content into Contract through Guard; retain source references.",
      "Hidden-decision review identifies forced choices. Contract checks identify contradictions affecting execution.",
      "Human resolves choices reserved for them. Correct the smallest affected contract section.",
      "Capture the opening verification before mutating stages."
    ],
    "close": "Stages and judges can apply the requirements without inventing decisions. Contract corrections do not trigger unrelated completed work.",
    "diagram": {
      "work": [
        "Form Contract from approved work and design.",
        "Run hidden-decision scan and contract checks.",
        "Resolve forced choices and execution contradictions."
      ],
      "exit": "No unresolved forced choice; decisions are recorded and requirements can be applied.",
      "blocks": [
        "Forced choice → CONTRACT exit.",
        "Contradictory acceptance requirement → dependent work.",
        "Record approved decision in Contract; correct affected requirement.",
        "Check the affected result without restarting unrelated work."
      ],
      "basis": "Hidden decisions cannot leave CONTRACT unresolved."
    }
  },
  {
    "id": "spec",
    "name": "SPEC",
    "purpose": "Plan observable BDD scenarios; do not implement tests.",
    "status": "IPlan / PlannedCase drafted; stage result binding still undefined.",
    "issue": "Ambiguous behavior or a planned Unknown outcome.",
    "steps": [
      "Map required behavior to spec cases with expected Pass, Fail or Unknown and a reason.",
      "Present Unknown cases to the human during planning; retain any HumanInput with the case.",
      "A requirement ambiguity returns to CONTRACT. A defective scenario returns only to SPEC.",
      "The later TEST evaluator resolves Unknown against the observed pre-implementation code."
    ],
    "close": "The plan is executable and the expected result is explicit. Unknown is carried to its evaluator; it is not permission to accept either outcome.",
    "diagram": {
      "work": [
        "Plan observable BDD scenarios; write no tests.",
        "Give Pass, Fail or Unknown expectations and reasons.",
        "Surface Unknown cases to the human during planning."
      ],
      "exit": "Scenarios are executable; ambiguity about required behavior is resolved.",
      "blocks": [
        "Unclear required behavior → affected spec case; resolve in CONTRACT.",
        "Unknown current-code outcome → later TEST evaluation, not a guessed answer.",
        "Retain the case and any HumanInput for the evaluator."
      ],
      "basis": "Unknown may reach TEST. It is not permission to accept either outcome."
    }
  },
  {
    "id": "architecture",
    "name": "ARCHITECTURE",
    "purpose": "Define the approved application signatures, or establish that none change.",
    "status": "File roles mapped from the existing stage; replacement result schema still undefined.",
    "issue": "A signature cannot express the requirement or the stage review rejects it.",
    "steps": [
      "Define only the required signatures; preserve working bodies.",
      "If no signature changes are needed, retain that result without inventing skeletons.",
      "Fix the affected signature when the enabled stage reviewers identify a defect.",
      "An approach change returns to DESIGN; a requirement change returns to CONTRACT for approval."
    ],
    "close": "The approved surface supports the work, and required signature approval is obtained.",
    "diagram": {
      "work": [
        "Define required signatures, or establish no signature change.",
        "Preserve existing working bodies.",
        "Enabled reviewers inspect the application surface."
      ],
      "exit": "Required signatures support the work and have the required approval.",
      "blocks": [
        "Missing or defective signature → RULE_PLAN / TEST work using it.",
        "Unsupported architecture → DESIGN.",
        "Correct affected signatures; keep unrelated accepted work."
      ],
      "basis": "No-signature is valid when justified; it is not a missing result."
    }
  },
  {
    "id": "rule-plan",
    "name": "RULE_PLAN",
    "purpose": "Plan what the rules must enforce against the architecture.",
    "status": "Planning stage approved; full output schema still undefined.",
    "issue": "Unclear enforcement, untestable rule claims or Unknown planned outcomes.",
    "steps": [
      "Plan compliant and violating cases against the approved architecture.",
      "Give planned outcomes and reasons; surface Unknown cases during planning.",
      "A plan defect returns to RULE_PLAN. Missing architectural information returns to ARCHITECTURE.",
      "New restrictions or policy changes require the human decision; the plan does not create an exception."
    ],
    "close": "The rule-writing task and its expected evidence are explicit.",
    "diagram": {
      "work": [
        "Plan the rule boundary using the architecture.",
        "Plan compliant and violating cases with expectations.",
        "Surface Unknown cases and preserve human input."
      ],
      "exit": "The rule boundary and cases are explicit; no undecided policy is delegated to RULE.",
      "blocks": [
        "Unclear enforcement policy → RULE writing; resolve in CONTRACT.",
        "Missing signature → ARCHITECTURE.",
        "Unknown observed outcome → RULE evaluator."
      ],
      "basis": "Plans exist as a shared draft type; evaluator result types remain open."
    }
  },
  {
    "id": "rule",
    "name": "RULE",
    "purpose": "Write and exercise the planned rules.",
    "status": "Replacement schemas must retain separate rule-agent, reviewer and tool outputs.",
    "issue": "Rule behavior disagrees with its plan, an Unknown needs evaluation, or reviewers find a defect.",
    "steps": [
      "Rule-writing agent implements the approved cases; Guard captures actual execution.",
      "Evaluate unexpected and Unknown results against the plan and current code. Do not manufacture RED.",
      "An implementation defect returns to RULE; a defective plan returns to RULE_PLAN.",
      "A missing signature returns to ARCHITECTURE. A required policy change goes to the human."
    ],
    "close": "The rule enforces the required boundary and the evidence demonstrates its cases. Production impact remains explicit, not suppressed.",
    "diagram": {
      "work": [
        "Implement planned rules and exercise their fixtures.",
        "Evaluate Unknown and unexpected outcomes independently.",
        "Enabled reviewers inspect enforcement and reuse."
      ],
      "exit": "Rule behavior is proven; no rule defect or unevaluated outcome remains.",
      "blocks": [
        "Wrong diagnostic or missing enforcement → RULE correction.",
        "Defective plan → RULE_PLAN.",
        "Expected production violation → IMPLEMENT work; retain evidence.",
        "Missing execution/evaluation result → RULE exit."
      ],
      "basis": "Expected production red is not a reason to weaken rules or manufacture failures."
    }
  },
  {
    "id": "test-plan",
    "name": "TEST_PLAN",
    "purpose": "Plan detailed testing against the available signatures and rules.",
    "status": "IPlan / PlannedCase drafted; complete TEST_PLAN output still undefined.",
    "issue": "Missing coverage, unclear expected behavior or Unknown outcomes.",
    "steps": [
      "Plan detailed test cases without duplicating the Spec Plan content; reference the relevant scenarios.",
      "Specify expected Pass, Fail or Unknown and the reason for the current code.",
      "Present Unknowns during planning and retain any certified HumanInput for evaluation.",
      "Correct the affected plan, signature or requirement at its owner."
    ],
    "close": "The test-writing tasks cover the required behavior and every planned case has an explicit expected outcome.",
    "diagram": {
      "work": [
        "Plan detailed tests using Spec Plan, signatures and rules.",
        "Reference spec cases rather than rewrite them.",
        "Set expectations and surface Unknown cases."
      ],
      "exit": "Required testing is planned; expected behavior is clear and cases can be implemented.",
      "blocks": [
        "Missing case → TEST_PLAN correction.",
        "Requirement ambiguity → CONTRACT.",
        "Missing surface → ARCHITECTURE.",
        "Unknown observed outcome → TEST evaluator."
      ],
      "basis": "A planned Unknown is different from an undecided requirement."
    }
  },
  {
    "id": "test",
    "name": "TEST",
    "purpose": "Write spec tests and unit tests, then evaluate observed results.",
    "status": "Shared plan shape exists; typed execution and evaluation results remain to be defined.",
    "issue": "Unexpected outcomes, Unknown results, inadequate tests or a required architecture change.",
    "steps": [
      "Spec-test and unit-test work fans out from the plans; both preserve independently reported scenarios.",
      "Guard captures actual runs. An independent evaluator examines Unknowns and mismatches.",
      "Test Reviewer is required; other stage reviewers follow workflow selection.",
      "Defective test → TEST. Defective plan → its planning stage. Required signature change → ARCHITECTURE.",
      "Green regression tests remain green when justified; do not change code or expectations to manufacture RED."
    ],
    "close": "Tests prove the required behavior. Unknowns are evaluated, and actual remaining implementation failures are identified.",
    "diagram": {
      "work": [
        "Fan out spec-test and unit-test writing.",
        "Capture runs; independently evaluate Unknowns and mismatches.",
        "Test Reviewer is required; other enabled reviewers inspect."
      ],
      "exit": "Tests meet requirements; Unknowns and mismatches have evaluations.",
      "blocks": [
        "Weak or incorrect assertion → TEST correction.",
        "Missing coverage → TEST / relevant plan.",
        "Required signature change → ARCHITECTURE.",
        "Expected failure from missing behavior → IMPLEMENT.",
        "Unevaluated Unknown → TEST exit."
      ],
      "basis": "A valid test may fail before IMPLEMENT. A green regression test need not be broken."
    }
  },
  {
    "id": "implement",
    "name": "IMPLEMENT",
    "purpose": "Implement the approved behavior against the rules, signatures and tests.",
    "status": "Replacement result schema and tool-capture bindings still undefined.",
    "issue": "Failing behavior, a missing dependency or a genuine design/requirement conflict.",
    "steps": [
      "Implement against the approved inputs and capture actual build/test results.",
      "Fix implementation defects in IMPLEMENT; do not weaken tests or rules.",
      "A demonstrated test, rule or signature defect returns to its owning stage under the required approval.",
      "A new decision goes to the human with evidence, options and a recommendation."
    ],
    "close": "The implementation satisfies the requirement and relevant verification. Missing evidence or an unmet requirement is not hidden by a completion claim.",
    "diagram": {
      "work": [
        "Implement approved behavior against signatures, rules and tests.",
        "Run required verification and capture actual results.",
        "Identify any requirement still unmet or implementation wall."
      ],
      "exit": "Required behavior is implemented; no unresolved wall or applicable failing verification remains.",
      "blocks": [
        "Implementation defect / wall → IMPLEMENT exit.",
        "Actual test or rule defect → its owning stage.",
        "New requirement choice → CONTRACT / human.",
        "Correct the cause; do not weaken the test or rule."
      ],
      "basis": "Existing output has walls and acceptance entries; a complete flag is not evidence."
    }
  },
  {
    "id": "readiness",
    "name": "READINESS",
    "purpose": "Check that the review can use the correct inputs and work state.",
    "status": "Replacement readiness criteria and schema still need approval.",
    "issue": "Wrong input version, missing required evidence or an authority conflict.",
    "steps": [
      "Check the actual artifacts and their authority; do not reconstruct prior results.",
      "Fix a wrong reference or obtain genuinely missing required evidence.",
      "A conflict requiring a human decision is presented before REFUTE starts.",
      "Administrative observations that do not leave requirements, proof or authority unmet do not create correction loops."
    ],
    "close": "REFUTE can inspect the right work against the right requirements. A stage-summary narrative is not a substitute input.",
    "diagram": {
      "work": [
        "Check the delivered state against approved scope and inputs.",
        "Check required evidence and approval references.",
        "Determine whether REFUTE has what it needs."
      ],
      "exit": "REFUTE can inspect the correct work using the correct requirements and evidence.",
      "blocks": [
        "Wrong artifact version → affected reviewer input.",
        "Missing required approval → affected execution.",
        "Missing necessary proof → dependent review.",
        "Repair the specific dependency, not unrelated paperwork."
      ],
      "basis": "Readiness mismatch must identify what review cannot proceed and why."
    }
  },
  {
    "id": "refute",
    "name": "REFUTE",
    "purpose": "Independent reviewers challenge the code, tests, rules and supporting evidence.",
    "status": "Inputs beyond the recorded legacy paths shown here are proposed; exact new bindings remain open.",
    "issue": "Confirmed code, test or rule defects; inadequate proof; or approval violations.",
    "steps": [
      "Each enabled reviewer reads the original inputs and returns its own structured verdict.",
      "Implementation defect → IMPLEMENT. Test defect → TEST. Rule defect → RULE.",
      "Design or requirement conflicts return to the responsible earlier owner with evidence.",
      "Keep historical verdicts unchanged. Review corrected work and reuse results unaffected by the correction where valid.",
      "An unresolved finding remains tracked; a missing ledger entry alone does not prove duplicated code."
    ],
    "close": "No blocking finding remains unsupported or unanswered. Corrections change the defective work, not the evidence to disguise it.",
    "diagram": {
      "work": [
        "Enabled independent reviewers inspect delivered code and evidence.",
        "Save each original verdict and supporting findings.",
        "Route confirmed defects to the stage responsible."
      ],
      "exit": "Required reviews are present; confirmed blocking findings are resolved.",
      "blocks": [
        "Implementation defect → IMPLEMENT.",
        "Test defect / missing coverage → TEST.",
        "Rule defect → RULE.",
        "Missing reviewer result → that reviewer run.",
        "Re-review affected corrections; retain original findings."
      ],
      "basis": "A summary, reassignment or rewritten verdict does not resolve a finding."
    }
  },
  {
    "id": "gate",
    "name": "GATE",
    "purpose": "Decide acceptance from the requirements, reviewer outputs and verification.",
    "status": "Acceptance result and evidence-reuse rules need typed definitions.",
    "issue": "Blocking findings, missing required review results or verification that does not cover delivered code.",
    "steps": [
      "Read each original verdict and required verification record.",
      "Use evidence valid for the delivered state; obtain required proof that is genuinely missing.",
      "Unresolved work returns to its responsible assignment. Do not edit verdicts to obtain acceptance."
    ],
    "close": "The governing requirements and required evidence support acceptance. Any human-required approval remains explicit.",
    "diagram": {
      "work": [
        "Read original verdicts and verification for the delivered state.",
        "Determine acceptance against the approved requirements.",
        "Obtain genuinely missing required proof."
      ],
      "exit": "Acceptance is supported by applicable proof and resolved blocking findings.",
      "blocks": [
        "Unanswered blocking finding → acceptance.",
        "Evidence for an obsolete code state → dependent acceptance check.",
        "Return only the affected work or obtain missing proof."
      ],
      "basis": "Do not repeat valid verification merely to create a new report."
    }
  },
  {
    "id": "report",
    "name": "REPORT",
    "purpose": "Present the accepted outcome and any action still required.",
    "status": "The report is a presentation output; it does not replace source results or execution captures.",
    "issue": "An inaccurate presentation or a genuinely unmet completion requirement.",
    "steps": [
      "Present the outcome from saved results and evidence references.",
      "Correct a report error in REPORT; do not restart code work merely to reword a description.",
      "If the evidence exposes unfinished required work, route that work to its owner."
    ],
    "close": "The human sees the actual outcome without fabricated transcripts or rewritten reviewer findings.",
    "diagram": {
      "work": [
        "Present the outcome using original accepted results.",
        "Link the evidence rather than reconstruct transcripts.",
        "Separate remaining work from completed work."
      ],
      "exit": "The communicated outcome agrees with the authoritative results.",
      "blocks": [
        "False statement → correct the presentation.",
        "Missing proof of behavior → responsible work, not a prose repair.",
        "A wording correction does not restart completed code stages."
      ],
      "basis": "REPORT does not become a replacement source of stage evidence."
    }
  },
  {
    "id": "review",
    "name": "REVIEW",
    "purpose": "Recorded terminal state; responsibilities and output are not yet defined.",
    "status": "Inputs shown are proposed. REVIEW behavior and output need definition.",
    "issue": "The purpose and closing action of this terminal stage remain an open design question.",
    "steps": [
      "Define what REVIEW adds beyond GATE acceptance and REPORT presentation.",
      "Only then define its actor, required input references and output.",
      "Do not invent a final approval or review artifact merely to fill this box."
    ],
    "close": "Not yet defined; the diagram makes this gap visible rather than declaring the workflow complete.",
    "diagram": {
      "work": [
        "Terminal state in the recorded sequence.",
        "Its purpose beyond GATE and REPORT has not been defined."
      ],
      "exit": "NOT DEFINED — do not invent a terminal acceptance condition.",
      "blocks": [
        "Undefined responsibility → final workflow specification.",
        "Settle what REVIEW does before defining its required work or exit."
      ],
      "basis": "Open design question, not an execution-time choice for an agent."
    }
  }
];
  const stageSymbols={conversation:'CV',ground:'GR',design:'DS',contract:'CT',spec:'SP',architecture:'AR','rule-plan':'RP',rule:'RL','test-plan':'TP',test:'TS',implement:'IM',readiness:'RD',refute:'RF',gate:'GT',report:'RT',review:'RV',external:'EX'};
  const stageName=id=>id==='external'?'External / existing':stages.find(stage=>stage.id===id).name;
  // Drill-down diagrams use the same audited process inventory as the usage page.
  const maps = Object.fromEntries(stages.map(stage=>[stage.id,{title:stage.name}]));

  const escape = value => String(value).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
  const wrap=(value,max)=>{const lines=[];let line='';for(const word of value.split(/\s+/)){if(line&&line.length+word.length+1>max){lines.push(line);line=word;}else line+=(line?' ':'')+word;}if(line)lines.push(line);return lines;};
  const label=(x,y,value,size,max,weight=400)=>wrap(value,max).map((line,i)=>'<text x="'+x+'" y="'+(y+i*23)+'" font-size="'+size+'" font-weight="'+weight+'">'+escape(line)+'</text>').join('');
  const MUST_PASS='Must pass',MAY_FAIL='May fail';
  const verificationAreas=[['rule','Rule code'],['test','Test code'],['production','Production code']];
  const verificationKinds=[['build','Build'],['rules','Rules'],['tests','Test']];
  const initialStates={
    rule:{build:MUST_PASS,rules:MUST_PASS,tests:MUST_PASS},
    test:{build:MUST_PASS,rules:MUST_PASS},
    production:{build:MUST_PASS,rules:MUST_PASS,tests:MUST_PASS}
  };
  // Stage-exit requirements, not observed results or per-case expectations.
  // Only changes are stored; the renderer expands every applicable cell.
  const verification={
    architecture:{
      production:{rules:MAY_FAIL,tests:MAY_FAIL}
    },
    implement:initialStates
  };
  function verificationStates(id){
    let current=Object.fromEntries(verificationAreas.map(([area])=>[area,{...initialStates[area]}]));
    for(const stage of stages){
      for(const [area,changes] of Object.entries(verification[stage.id]||{}))current[area]={...current[area],...changes};
      if(stage.id===id)return current;
    }
    throw new Error('Unknown stage: '+id);
  }
  function startSvg(height,title){return '<svg xmlns="http://www.w3.org/2000/svg" width="1500" height="'+height+'" viewBox="0 0 1500 '+height+'" role="img" aria-label="'+escape(title)+'"><defs><marker id="tip" markerWidth="9" markerHeight="9" refX="7" refY="4" orient="auto"><path d="M1 1 L8 4 L1 7" fill="none" stroke="#637d92" stroke-width="1.4"/></marker></defs><style>text{font-family:Arial,sans-serif;fill:#20364b}.edge{fill:none;stroke:#637d92;stroke-width:1.6;marker-end:url(#tip)}</style>'+modificationIconDefs+'<rect width="1500" height="'+height+'" fill="#fbfcfe"/>';}
  const rect=(x,y,w,h,fill,stroke,rx=5)=>'<rect x="'+x+'" y="'+y+'" width="'+w+'" height="'+h+'" rx="'+rx+'" fill="'+fill+'" stroke="'+stroke+'"/>';
  const arrow=(x,y,tx,ty)=>'<path class="edge" d="M'+x+' '+y+' C'+x+' '+(y+20)+' '+tx+' '+(ty-20)+' '+tx+' '+ty+'"/>';
  function fileHeight(title,width){return 68+wrap(title,Math.floor((width-34)/10)).length*23;}
  function stageBadge(id,x,y){
    if(!stageSymbols[id])throw new Error('Unknown producer: '+id);
    return '<g data-stage-symbol="'+stageSymbols[id]+'"><title>'+escape(stageName(id))+'</title>'+rect(x,y,34,24,'#244b6b','#244b6b',4)+'<text x="'+(x+17)+'" y="'+(y+17)+'" text-anchor="middle" style="fill:#fff" font-size="13" font-weight="700">'+stageSymbols[id]+'</text></g>';
  }
  function stageLedger(x,y,width){
    const cols=width>1000?4:3,cellWidth=width/cols;
    let svg=label(x,y+22,'STAGE SYMBOL LEDGER — files retain their producer’s symbol',18,95,700);
    Object.keys(stageSymbols).forEach((id,i)=>{const bx=x+(i%cols)*cellWidth,by=y+38+Math.floor(i/cols)*34;svg+=stageBadge(id,bx,by)+label(bx+44,by+18,stageName(id),16,30);});
    return svg;
  }
  function fileCard(x,y,w,title,meta,direction,categories,producers){
    const h=fileHeight(title,w),input=direction==='input',stroke=input?'#427eaf':'#518268',fill=input?'#edf5ff':'#edf7ef';
    return '<g data-artifact="'+escape(title)+'" data-direction="'+direction+'"><path d="M'+x+' '+y+' H'+(x+w-23)+' L'+(x+w)+' '+(y+23)+' V'+(y+h)+' H'+x+' Z" fill="'+fill+'" stroke="'+stroke+'" stroke-width="2"/><path d="M'+(x+w-23)+' '+y+' V'+(y+23)+' H'+(x+w)+'" fill="none" stroke="'+stroke+'"/>'
      +label(x+16,y+23,input?'→ IN':'OUT →',15,30,700)
      +categories.map((category,i)=>'<use data-file-category="'+category+'" href="#file-'+category+'" x="'+(x+w-30-categories.length*35+i*35)+'" y="'+(y+5)+'" width="29" height="29"/>').join('')
      +label(x+16,y+57,title,19,Math.floor((w-34)/10),700)
      +producers.map((producer,i)=>stageBadge(producer,x+16+i*40,y+h-30)).join('')
      +label(x+22+producers.length*40,y+h-13,meta,13,Math.floor((w-44-producers.length*40)/7))+'</g>';
  }
  function grid(items,x,y,width){
    if(!items.length)return {svg:label(x,y+25,'No artifact specified here.',18,80),height:50};
    const cols=Math.min(3,items.length,Math.max(1,Math.floor((width+16)/280))),w=(width-(cols-1)*16)/cols;let svg='',cursor=y;
    for(let i=0;i<items.length;i+=cols){const row=items.slice(i,i+cols),h=Math.max(...row.map(item=>fileHeight(item.title,w)));row.forEach((item,j)=>svg+=fileCard(x+j*(w+16),cursor,w,item.title,item.meta,item.direction,item.categories,item.producers));cursor+=h+14;}
    return {svg,height:cursor-y-14};
  }
  function artifactItems(keys,direction){return keys.map(key=>{const [title,producer,,categories,id]=artifacts[key];return {title,meta:id||stageName(producer),direction,categories,producers:[producer]};});}
  // Shared symbol source; the taxonomy SVG retains these as an exported snapshot.
  const modificationIconDefs="<defs><symbol id=\"edit-signature\" viewBox=\"0 0 76 64\"><rect x=\"1\" y=\"1\" width=\"74\" height=\"62\" rx=\"10\" fill=\"#e4edf8\" stroke=\"#6b89a6\"/><text x=\"38\" y=\"43\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"29\" font-weight=\"700\" fill=\"#244b6b\">ƒ↔</text></symbol><symbol id=\"edit-stub\" viewBox=\"0 0 76 64\"><rect x=\"1\" y=\"1\" width=\"74\" height=\"62\" rx=\"10\" fill=\"#e4edf8\" stroke=\"#6b89a6\"/><text x=\"38\" y=\"43\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"29\" font-weight=\"700\" fill=\"#244b6b\">ƒ+!</text></symbol><symbol id=\"edit-entry\" viewBox=\"0 0 76 64\"><rect x=\"1\" y=\"1\" width=\"74\" height=\"62\" rx=\"10\" fill=\"#e4edf8\" stroke=\"#6b89a6\"/><text x=\"38\" y=\"43\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"29\" font-weight=\"700\" fill=\"#244b6b\">!→ƒ</text></symbol><symbol id=\"edit-behavior\" viewBox=\"0 0 76 64\"><rect x=\"1\" y=\"1\" width=\"74\" height=\"62\" rx=\"10\" fill=\"#e4edf8\" stroke=\"#6b89a6\"/><text x=\"38\" y=\"43\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"29\" font-weight=\"700\" fill=\"#244b6b\">{…}</text></symbol><symbol id=\"edit-settings\" viewBox=\"0 0 76 64\"><rect x=\"1\" y=\"1\" width=\"74\" height=\"62\" rx=\"10\" fill=\"#e4edf8\" stroke=\"#6b89a6\"/><text x=\"38\" y=\"43\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"29\" font-weight=\"700\" fill=\"#244b6b\">⚙</text></symbol><symbol id=\"edit-document\" viewBox=\"0 0 76 64\"><rect x=\"1\" y=\"1\" width=\"74\" height=\"62\" rx=\"10\" fill=\"#e4edf8\" stroke=\"#6b89a6\"/><text x=\"38\" y=\"43\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"29\" font-weight=\"700\" fill=\"#244b6b\">¶</text></symbol><symbol id=\"file-production\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"19\" font-weight=\"700\" fill=\"#244b6b\">{ }</text></symbol><symbol id=\"file-test\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"27\" font-weight=\"700\" fill=\"#244b6b\">✓</text></symbol><symbol id=\"file-rule\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"27\" font-weight=\"700\" fill=\"#244b6b\">§</text></symbol><symbol id=\"file-config\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"27\" font-weight=\"700\" fill=\"#244b6b\">⚙</text></symbol><symbol id=\"file-docs\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"27\" font-weight=\"700\" fill=\"#244b6b\">¶</text></symbol><symbol id=\"file-workflow\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"27\" font-weight=\"700\" fill=\"#244b6b\">W→</text></symbol><symbol id=\"file-output\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"19\" font-weight=\"700\" fill=\"#244b6b\">OUT</text></symbol><symbol id=\"file-evidence\" viewBox=\"0 0 64 64\"><rect x=\"1\" y=\"1\" width=\"62\" height=\"62\" rx=\"6\" fill=\"#e7f2fa\" stroke=\"#4e80a6\"/><text x=\"32\" y=\"41\" text-anchor=\"middle\" style=\"font-family:Arial,sans-serif\" font-size=\"27\" font-weight=\"700\" fill=\"#244b6b\">E≡</text></symbol></defs>";
  const editScopes={
    conversation:[['workflow','Conversation','document','Describe the work']],
    ground:[],
    design:[],
    contract:[['workflow','Contract','document','Define approved requirements']],
    spec:[['workflow','Spec plan','document','Plan spec cases']],
    architecture:[['production','Production code','signature','Change planned signatures'],['production','Production code','stub','Add planned throwing stubs'],['production','Production code','entry','Entry throw only when planned']],
    'rule-plan':[['workflow','Rule plan','document','Plan enforcement']],
    rule:[['rule','Rule code','behavior','Implement planned rules'],['test','Rule-verification tests','behavior','Write rule tests and fixtures']],
    'test-plan':[['workflow','Test plan','document','Plan detailed tests']],
    test:[['test','Production-behavior tests','behavior','Write tests, fixtures and helpers']],
    implement:[['production','Production code','behavior','Implement planned behavior']],
    readiness:[],refute:[],gate:[],report:[],review:null
  };
  function editScopeBox(id,x,y,width){
    const scope=editScopes[id],stage=stages.find(s=>s.id===id);
    const icon=(prefix,name,ix,iy)=>'<use href="#'+prefix+'-'+name+'" x="'+ix+'" y="'+iy+'" width="42" height="38"/>';
    let contents='',cursor=y+48;
    if(scope===null){
      contents=label(x+18,cursor+23,'REVIEW edit scope is not defined.',18,80,700);cursor+=48;
    }else{
      for(const [category,name,change,intent] of scope){
        contents+=icon('file',category,x+18,cursor)+label(x+72,cursor+24,name,17,31,700)
          +label(x+345,cursor+24,'→',22,4)+icon('edit',change,x+388,cursor)
          +label(x+444,cursor+24,intent,17,43,700);cursor+=48;
      }
      if(!scope.some(row=>['production','test','rule'].includes(row[0]))){
        contents+=label(x+18,cursor+23,'SOURCE CODE LOCKED',17,80,700);cursor+=40;
      }
      if(stage.outputs.length){
        contents+=icon('file','output',x+18,cursor)+label(x+72,cursor+24,'Own stage outputs',17,31,700)
          +label(x+345,cursor+24,'→',22,4)+icon('edit','document',x+388,cursor)
          +label(x+444,cursor+24,'Only assigned output artifacts below',17,43);cursor+=48;
      }
      if(stage.outputs.some(key=>/capture/i.test(artifacts[key][0]))){
        contents+=icon('file','evidence',x+18,cursor)+label(x+72,cursor+24,'Captured evidence',17,31,700)
          +label(x+388,cursor+24,'Written by tools, not rewritten by agents',17,49);cursor+=48;
      }
    }
    contents+=label(x+18,cursor+20,'Guard updates Workflow execution state; agents edit only assigned outputs.',16,100,700);cursor+=32;
    contents+=label(x+18,cursor+20,'Other files locked • Prior-stage results remain unchanged',16,100,700);cursor+=32;
    return {svg:rect(x,y,width,cursor-y+12,'#f2f7fb','#7891ac')+label(x+18,y+28,'EDIT SCOPE — FILES + NAMED CHANGES',17,85,700)+contents,height:cursor-y+12};
  }
  function verificationBox(id,x,y,width){
    const groups=verificationStates(id),columnWidth=(width-210)/3;let shapes='';
    verificationKinds.forEach(([,name],column)=>shapes+=label(x+210+column*columnWidth,y+57,name,17,20,700));
    verificationAreas.forEach(([area,name],row)=>{
      const by=y+72+row*44;shapes+=label(x+18,by+22,name,17,22,700);
      verificationKinds.forEach(([kind],column)=>{
        const value=groups[area][kind],bx=x+210+column*columnWidth;
        if(value===undefined){shapes+=label(bx+58,by+22,'—',17,5);return;}
        const green=value===MUST_PASS;
        shapes+='<g aria-label="'+escape(name+' · '+kind+' · '+value)+'" data-verification-area="'+area+'" data-verification-kind="'+kind+'" data-verification-state="'+value+'">'
          +rect(bx,by,145,32,green?'#e6f4eb':'#fdebea',green?'#38704b':'#a94742',16)
          +'<circle cx="'+(bx+16)+'" cy="'+(by+16)+'" r="6" fill="'+(green?'#248044':'#bd3d36')+'"/>'
          +label(bx+31,by+22,value,17,20,700)+'</g>';
      });
    });
    const h=214;
    return {svg:rect(x,y,width,h,'#f1f5fa','#7891ac')+label(x+18,y+28,'REQUIRED STATE AT EXIT',17,85,700)+shapes,height:h};
  }
  function renderOverview(){
    let parts='',cursor=475;const layout={};
    for(const s of stages){
      const y=cursor,c=s.diagram;parts+=stageBadge(s.id,40,y+9)+label(86,y+30,s.name,28,90,700);let v=y+55;
      parts+=label(40,v+18,'ENTERING THIS STAGE — from outside or an earlier stage',17,100,700);v+=40;
      const input=grid(artifactItems(s.inputs,'input'),40,v,910);parts+=input.svg;v+=input.height+20;
      if(s.unresolvedInputs?.length){const pending='PROPOSED / UNSETTLED INPUTS: '+s.unresolvedInputs.map(k=>artifacts[k][4]).join(', ');parts+=label(40,v+20,pending,16,103);v+=wrap(pending,103).length*23+20;}
      v+=20;
      const workY=v,workH=55+c.work.length*29;
      parts+=arrow(495,v-30,495,v)+rect(40,v,910,workH,'#edf3fb','#7d9ab9',15)+label(60,v+28,stageSymbols[s.id]+' · WITHIN THIS STAGE',17,95,700);
      c.work.forEach((line,i)=>parts+=label(60,v+60+i*29,line,19,96));v+=workH+14;
      if(s.internalInputs?.length){
        const flow=window.WorkflowPhaseDetails.artifactUsage.stageFlow(s.id);
        parts+=label(60,v+23,'INTERNAL HANDOFFS — produced here, then read here',17,94,700);v+=50;
        for(const key of s.internalInputs){
          const links=flow.internal.filter(link=>link.edge.key===key);
          const readers=[...new Set(links.map(link=>link.reader.name))];
          const line=artifacts[key][4]+' → '+readers.join('; ');
          parts+=label(60,v,line,16,99);v+=wrap(line,99).length*23+10;
        }
        v+=15;
      }
      if(s.correctionInputs?.length){const line='CORRECTION ONLY — previous-attempt findings: '+s.correctionInputs.map(key=>artifacts[key][4]).join(', ');parts+=label(60,v+20,line,16,99);v+=wrap(line,99).length*23+35;}
      const edits=editScopeBox(s.id,40,v,910);parts+=edits.svg;v+=edits.height+40;parts+=arrow(495,v-40,495,v);
      parts+=label(40,v+18,'PRODUCED / UPDATED HERE — retained outputs',17,100,700);v+=40;
      const output=grid(artifactItems(s.outputs,'output'),40,v,910);parts+=output.svg;v+=output.height+40;
      if(s.unresolvedOutputs?.length){const pending='PROPOSED / UNSETTLED OUTPUTS: '+s.unresolvedOutputs.map(k=>artifacts[k][4]).join(', ');parts+=label(40,v+20,pending,16,103);v+=wrap(pending,103).length*23+40;}
      const exitH=50+wrap(c.exit,91).length*23;
      parts+=arrow(495,v-40,495,v)+rect(40,v,910,exitH,'#fff6d9','#9e8336')+label(60,v+27,'EXIT CHECK',18,95,700)+label(60,v+57,c.exit,19,91);v+=exitH+14;
      const state=verificationBox(s.id,40,v,910);parts+=state.svg;v+=state.height;
      let by=workY+35;parts+=label(990,by,'UNFINISHED → BLOCKED WORK',17,47,700);by+=35;
      for(const b of c.blocks){parts+=label(990,by,b,19,47);by+=wrap(b,47).length*23+19;}
      parts+=label(990,by+15,c.basis,17,49);by+=wrap(c.basis,49).length*23+40;
      const end=Math.max(v,by)+65;if(s.id!=='review')parts+=arrow(495,v+8,495,end-8)+label(520,v+35,'Exit satisfied + transition approved',16,46);
      layout[s.id]={top:y,workY,workH};cursor=end+20;
    }
    const height=cursor+30,svg=startSvg(height,'Full workflow: inputs, outputs and expected verification states')
      +label(40,46,'Full workflow — artifacts, work and expected results',31,100,700)
      +label(40,85,'Above the stage → incoming files    •    Inside → substep handoffs    •    Below → produced / updated files.',19,145)
      +label(40,121,'Every read still counts in the usage matrix. Internal results are not prerequisites for starting their own stage.',19,145)
      +label(40,157,'Expected states are requirements, not observed results. No automatic rerun or warning waiver is introduced.',18,145)
      +label(40,189,'Proposed exit details remain review material. Stage selection and transition approvals still apply.',18,145)+stageLedger(40,210,1410)+parts+'</svg>';
    return {svg,height,layout};
  }
  function render(id){
    return window.WorkflowPhaseDetails.artifactUsage.renderStage(id);
  }
  window.WorkflowPhaseDetails={maps,render,artifacts,stages,verification,verificationStates,renderOverview,editScopes,modificationIconDefs,stageSymbols};
})();
