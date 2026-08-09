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

const ruleGenPrompt = `You are the RULE-GEN agent for the rule phase. You hold rule-generation authority: you MAY create and edit files under \`analyzers/\`. You are the ONLY writer this phase. Do NOT commit.

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

log(`rule-gen wrote ${ruleGen.ruleFiles.length} files; handing the rules to the independent refute panel`)

// The independent adversary panel refutes the RULES themselves — separate agents, never the author.
// Reuse the refute workflow, pointing it at the new rule files as the changed set.
const refute = await workflow('refute', {
  projectPath,
  contractPath,
  changedFiles: ruleGen.ruleFiles,
})

return { ruleFiles: ruleGen.ruleFiles, redProof: ruleGen.redProof, perRule: ruleGen.perRule, refute }
