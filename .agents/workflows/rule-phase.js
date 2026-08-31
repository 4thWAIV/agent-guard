export const meta = {
  name: 'rule-phase',
  description: 'RULE-PHASE fan-out: a rule-gen agent writes the DESIGN-determined guardrails into analyzers/ (RED against existing violations), then the INDEPENDENT refute panel refutes the rules themselves — never the author grading its own work. Produces the rule files, the RED proof, and the adversary verdicts. Separation of powers: this agent does NOT implement the framework and does NOT clean up the RED (that is the later IMPLEMENT worker).',
  phases: [
    { title: 'Write-rules', detail: 'rule-gen agent writes the guardrails into analyzers/, RED against violations' },
    { title: 'Refute-rules', detail: 'independent adversaries refute the rules against the contract and rails', model: 'sonnet' },
  ],
}

// What the rule-gen agent returns after writing the rules.
const RULEGEN_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    ruleFiles: { type: 'array', items: { type: 'string' } }, // every file created/edited under analyzers/
    redProof: { type: 'string' },  // build output showing each rule fires RED against its named violation
    perRule: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          rule: { type: 'string' },        // the guardrail, one line
          diagnosticId: { type: 'string' }, // e.g. AG0008, or "test" for a non-analyzer check
          firesAgainst: { type: 'string' }, // the file:line it goes RED against, or "preventive (none)"
        },
        required: ['rule', 'firesAgainst'],
      },
    },
    notes: { type: 'string' },
  },
  required: ['ruleFiles', 'redProof', 'perRule'],
}

// One rule-adversary's verdict on the rules themselves.
const VERDICT_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    lens: { type: 'string' },
    verdict: { type: 'string', enum: ['PASS', 'FAIL'] },
    findings: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          summary: { type: 'string' },
          evidence: { type: 'string' },
          fix: { type: 'string' },
        },
        required: ['summary', 'evidence'],
      },
    },
    refutationAttempts: { type: 'array', items: { type: 'string' } },
    proofChecked: { type: 'array', items: { type: 'string' } },
  },
  required: ['lens', 'verdict', 'refutationAttempts'],
}

let input = args
let unwrapGuard = 0
while (typeof input === 'string' && unwrapGuard < 5) {
  try {
    input = JSON.parse(input)
  } catch (parseError) {
    throw new Error('rule-phase: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath

if (!projectPath || !contractPath) {
  throw new Error(
    'rule-phase requires args { projectPath, contractPath } (got type: ' + typeof args + ')')
}

// Fix round: when refutation/approved fixes are passed, the rule-gen FIXES the existing rules rather than writing fresh.
const refutation = input && input.refutation
const fixModePreamble = refutation
  ? `THIS IS A FIX ROUND, NOT AN INITIAL WRITE. The rules already exist on disk from a prior rule-phase run. Do NOT rewrite them from scratch and do NOT recreate files. Apply EXACTLY these confirmed, human-approved fixes and add the tests they call for — nothing more — then re-prove RED and re-run the covering tests:

${typeof refutation === 'string' ? refutation : JSON.stringify(refutation, null, 2)}

The proof and return steps below apply unchanged.

`
  : ''

const ruleGenPrompt = `${fixModePreamble}You are the RULE-GEN agent for the rule phase. You hold rule-generation authority: you MAY create and edit files under \`analyzers/\`. You are the ONLY writer this phase. Do NOT commit.

PROJECT PATH: ${projectPath}
CONTRACT: ${contractPath} — Read it in full. Its \`rule-phase-ruleset\` decision lists the exact guardrails to create, each with what it forbids, where it goes RED, and its mechanism (Roslyn analyzer vs a test).

Write EVERY guardrail in \`rule-phase-ruleset\` into the codebase, following the repo's established pattern so nothing is one-off:
- Roslyn analyzers live in \`analyzers/AgentGuard.Analyzers/\`, one class per rule, following the existing AG0001–AG0007 analyzers exactly (a \`DiagnosticId\` const, a \`DiagnosticDescriptor\`, a \`DiagnosticAnalyzer\`). Use the next free ids (AG0008, AG0009, …). Register each new id in \`analyzers/AgentGuard.Analyzers/AnalyzerReleases.Unshipped.md\` (the build fails without it).
- Add a covering test for each analyzer in \`analyzers/AgentGuard.Analyzers.Tests/\`, following the existing analyzer tests.
- For any rule the contract marks as a test rather than an analyzer, write that test where the contract says.

CRITICAL — the rules go RED, and you do NOT clean up:
- Wire each rule in EVEN WHERE existing code violates it. It is SUPPOSED to fail the build (RED) against the current violations — that red is the forcing function for the later IMPLEMENT worker. NEVER suppress, exempt, \`NoWarn\`, lower severity, or edit the violating code to make it pass. Leave the RED standing.
- Do NOT build the cross-platform framework, do NOT delete or edit \`NativeInterop.cs\` or \`CreationHelper.cs\`, do NOT touch non-analyzer source except to register/wire the rules. That cleanup is a DIFFERENT agent's job (separation of powers).

PROVE the RED: run \`dotnet build\` and paste the output showing each new rule firing against its named violation (e.g. the interop rule error on \`NativeInterop.cs\`, the OS-branching rule error on \`CreationHelper.cs:46\`). Preventive rules that have no current violation compile clean — say so.

Return: ruleFiles (every file you created/edited under analyzers/), redProof (the pasted build output), perRule (each rule with its diagnostic id and the file:line it fires against, or "preventive (none)"), and notes.`

const ruleGen = await agent(ruleGenPrompt, { label: 'rule-gen', phase: 'Write-rules', schema: RULEGEN_SCHEMA })

if (!ruleGen || !ruleGen.ruleFiles || !ruleGen.ruleFiles.length) {
  return { ruleFiles: [], error: 'rule-gen produced no rule files', ruleGen }
}

log(`rule-gen wrote ${ruleGen.ruleFiles.length} files; handing the rules to the independent adversary panel`)

// The independent adversaries judge the RULES themselves — separate agents, never the author.
// This is NOT the implementation refute: the build is INTENTIONALLY RED here (the rules fire against the
// code the later IMPLEMENT worker will clean up), so RED is correct and is never a reason to fail the rules.
const RULE_ADVERSARIES = [
  { id: 'solid', rail: 'rails-solid-code', focus: "the analyzer code's design and structure", model: 'sonnet' },
  { id: 'dry', rail: 'rails-dry-code', focus: 'duplication across the analyzers — reuse the existing analyzer helpers/pattern, no copy-pasted rule logic', model: 'sonnet' },
  { id: 'lie-catcher', rail: 'rails-decisions', focus: 'honesty of the rules', model: 'opus' },
]

// Which adversaries gate this round is the human's call (the L-level / panel choice). Pass args.adversaries (a subset of
// the ids above) to run a lighter panel — e.g. ["dry","lie-catcher"] once SOLID's structural completeness pass is done;
// omit it to run the full three. The lie-catcher (decision/honesty) should stay in any panel a human trims.
const ACTIVE_ADVERSARIES = Array.isArray(input && input.adversaries) && input.adversaries.length
  ? RULE_ADVERSARIES.filter((adv) => input.adversaries.includes(adv.id))
  : RULE_ADVERSARIES

const ruleRefutePrompt = (adv) => `You are the ${adv.id} adversary judging the RULES a rule-gen agent just wrote — NOT a finished implementation. Do NOT make code changes. The build is INTENTIONALLY RED right now: the new rules fire against the code that the later IMPLEMENT worker will clean up. RED is correct here; never fail the rules because the build is red.

Read .agents/skills/${adv.rail}/SKILL.md — it is your PASS/FAIL checklist. Read the contract's rule-phase-ruleset (${contractPath}) and the rule files (${JSON.stringify(ruleGen.ruleFiles)}). Your lens: ${adv.focus}.

${adv.id === 'lie-catcher'
  ? 'Give NO fix advice. Re-run dotnet build yourself and confirm each rule fires RED against EXACTLY its intended violation and nothing spurious (not over-broad, not under-broad). YELL any suppression, NoWarn, lowered severity, or scaffolded/faked rule; any rule that does not match the signed-off rule-phase-ruleset; and any new analyzer missing from AnalyzerReleases.Unshipped.md.'
  : 'Rule each Violation in your rail against the analyzer code with exact file:line. Give the fix path.'}

Return lens="${adv.id}", verdict PASS or FAIL, findings (summary, evidence as file:line, and fix — leave fix empty for the lie-catcher), refutationAttempts (what you tried to break; none means rubber-stamping), and proofChecked.`

const verdicts = (await parallel(ACTIVE_ADVERSARIES.map((adv) => () =>
  agent(ruleRefutePrompt(adv), { label: `refute-rule:${adv.id}`, phase: 'Refute-rules', model: adv.model, schema: VERDICT_SCHEMA }))
)).filter(Boolean)

const failed = verdicts.filter((v) => v.verdict === 'FAIL')
log(`${verdicts.length} rule-adversaries ran; ${failed.length} FAIL (${failed.map((v) => v.lens).join(', ') || 'none'})`)

return { ruleFiles: ruleGen.ruleFiles, redProof: ruleGen.redProof, perRule: ruleGen.perRule, verdicts, anyFail: failed.length > 0 }
