export const meta = {
  name: 'design',
  description: 'DESIGN stage fan-out: independent architects each propose an approach from a distinct angle AND the guardrail rules it would add, then a judge scores them against the code rails and synthesizes the winner. Produces the chosen approach and the rules the contract must carry.',
  phases: [
    { title: 'Propose', detail: 'one architect per angle: approach + the rules it would add', model: 'sonnet' },
    { title: 'Judge', detail: 'score against SOLID/DRY/real-work, synthesize the winner' },
  ],
}

// The rule trigger: a guardrail an approach would add.
const RULE_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    rule: { type: 'string' },              // what it forbids or requires, one line
    failureModeStopped: { type: 'string' }, // the corner-cut it makes impossible or greppable
    analyzer: { type: 'string' },           // the analyzer id/kind, if known
  },
  required: ['rule', 'failureModeStopped'],
}

// One architect's proposal for its angle.
const APPROACH_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    angle: { type: 'string' },
    approach: { type: 'string' },       // the design, prose
    keySteps: { type: 'array', items: { type: 'string' } },
    rulesToAdd: { type: 'array', items: RULE_SCHEMA },
    risks: { type: 'array', items: { type: 'string' } },
  },
  required: ['angle', 'approach', 'rulesToAdd'],
}

// The judge's synthesized winner.
const VERDICT_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    winningAngle: { type: 'string' },
    why: { type: 'string' },
    synthesizedApproach: { type: 'string' }, // winner + best ideas grafted from the runners-up
    graftedFrom: { type: 'array', items: { type: 'string' } },
    rulesToAdd: { type: 'array', items: RULE_SCHEMA },
  },
  required: ['winningAngle', 'synthesizedApproach', 'rulesToAdd'],
}

let input = args
let unwrapGuard = 0
while (typeof input === 'string' && unwrapGuard < 5) {
  try {
    input = JSON.parse(input)
  } catch (parseError) {
    throw new Error('design: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const goal = input && input.goal
const facts = input && input.facts   // grounded facts from GROUND (optional but recommended)
const angles = (input && input.angles) || [
  { id: 'simplest', focus: 'the simplest correct design — fewest moving parts, least new surface; bias hard to reusing an existing owner over adding one.' },
  { id: 'most-general', focus: 'the fullest general seam for the KNOWN need — no one-case hack, no deferred abstraction; the seam built right the first time.' },
  { id: 'risk-first', focus: 'the design that most reduces the chance the AI cuts a corner here — maximize what a guardrail rule can mechanically stop or make greppable.' },
]

if (!projectPath || !goal) {
  throw new Error(
    'design requires args { projectPath, goal, facts?, angles?: [{id, focus}] } (got type: ' + typeof args + ')')
}

const proposePrompt = (angle) => `You are a software architect proposing ONE approach to the goal below, from a specific angle, grounded in the given facts and the LIVE code. Do NOT write production code. Design under the code rails: read .agents/skills/rails-solid-code/SKILL.md, .agents/skills/rails-dry-code/SKILL.md, and .agents/skills/rails-real-work/SKILL.md first, and design to pass them.

PROJECT PATH: ${projectPath}
GOAL: ${goal}
YOUR ANGLE — ${angle.id}: ${angle.focus}
GROUNDED FACTS (JSON, from GROUND): ${JSON.stringify(facts || [])}

TWO OUTPUTS, both required:
1. The approach — the design for this angle with its key steps. Verify every assumption against the live code (CodeGraph/lore/grep/Read); never assume.
2. The rules to add — apply the rule trigger: for THIS work, what guardrail (analyzer rule) can be developed to make a way the AI could cut a corner or hurt the human impossible, or at least greppable? A rule is developed WHENEVER one can be, not only when architecture changes. List each rule, the failure mode it stops, and the analyzer kind if you know it. If none applies, return an empty rulesToAdd and say why in the approach.

Return angle="${angle.id}", the approach, keySteps, rulesToAdd, and risks.`

const judgePrompt = (proposals) => `You are the DESIGN judge. Score the candidate approaches below against the code rails and synthesize ONE winner. Read .agents/skills/rails-solid-code/SKILL.md, .agents/skills/rails-dry-code/SKILL.md, and .agents/skills/rails-real-work/SKILL.md first — they are your scoring checklist (SOLID beats DRY when they conflict).

GOAL: ${goal}
CANDIDATE APPROACHES (JSON): ${JSON.stringify(proposals)}

Pick the winning angle, say why, then synthesize the approach to ADOPT: the winner plus the best ideas grafted from the runners-up (name where each graft came from). Merge and dedupe the rulesToAdd across all candidates into the final set the contract will carry — each a real guardrail with the failure mode it stops. Do NOT invent a rule that stops no named failure mode.

Return winningAngle, why, synthesizedApproach, graftedFrom, and the merged rulesToAdd.`

const proposals = (await parallel(angles.map((angle) => () =>
  agent(proposePrompt(angle), { label: `propose:${angle.id}`, phase: 'Propose', model: 'sonnet', schema: APPROACH_SCHEMA }))
)).filter(Boolean)

log(`${proposals.length} approaches proposed; judging against the code rails`)

const verdict = await agent(judgePrompt(proposals), { label: 'judge', phase: 'Judge', schema: VERDICT_SCHEMA })

return { goal, proposals, verdict }
