export const meta = {
  name: 'ground',
  description: 'GROUND stage fan-out: parallel explorers derive the subsystem ground truth from the live code, then the prior-art-ledger rules reuse/extract/new per capability. Produces the grounded facts and the reuse ledger a contract must carry.',
  phases: [
    { title: 'Explore', detail: 'one explorer per area derives ground truth from live code', model: 'sonnet' },
    { title: 'Ledger', detail: 'prior-art-ledger rules reuse/extract/new per capability', model: 'sonnet' },
  ],
}

// One explorer's proven, code-level read of its area.
const FACTS_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    area: { type: 'string' },
    facts: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          claim: { type: 'string' },    // a proven, code-level fact
          evidence: { type: 'string' }, // the file:line the claim rests on
        },
        required: ['claim', 'evidence'],
      },
    },
    surfaces: { type: 'array', items: { type: 'string' } }, // every store/file where a touched value lives
    openQuestions: { type: 'array', items: { type: 'string' } },
  },
  required: ['area', 'facts'],
}

let input = args
let unwrapGuard = 0
while (typeof input === 'string' && unwrapGuard < 5) {
  try {
    input = JSON.parse(input)
  } catch (parseError) {
    throw new Error('ground: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const areas = input && input.areas                 // [{ id, focus }]
const capabilities = input && input.capabilities   // [{ id, description }] — MANDATORY; the ledger runs every time

if (!projectPath || !areas || !areas.length || !capabilities || !capabilities.length) {
  throw new Error(
    'ground requires args { projectPath, areas: [{id, focus}], capabilities: [{id, description}] }. capabilities is MANDATORY and non-empty — the prior-art ledger runs on EVERY ground, never optional; a run that declares no capability cannot ground (got type: ' + typeof args + ')')
}

const explorePrompt = (area) => `You are an explorer deriving GROUND TRUTH for one area of a subsystem, from the LIVE code as it exists right now. Docs, comments, maps, and memory are hints about where to look — never answers. Do NOT write code. Never state a fact you did not read; every claim carries a file:line.

PROJECT PATH: ${projectPath}
YOUR AREA — ${area.id}: ${area.focus}

Explore with CodeGraph (ToolSearch "select:mcp__codegraph__codegraph_explore", then codegraph_explore with projectPath="${projectPath}" and a query naming the area's key symbols), lore (search_code with project_dir="${projectPath}"), Grep, and Read. Follow the call paths that matter and read the real bodies.

Return: area="${area.id}"; facts (each a proven code-level claim with its file:line evidence); surfaces (EVERY store or file where a value this area touches lives, so nothing gets verified off one store later); openQuestions (anything the code cannot answer). If your area is genuinely thin, return few facts — do NOT pad or invent.`

const facts = (await parallel(areas.map((area) => () =>
  agent(explorePrompt(area), { label: `explore:${area.id}`, phase: 'Explore', model: 'sonnet', schema: FACTS_SCHEMA }))
)).filter(Boolean)

log(`grounded ${facts.reduce((n, f) => n + (f.facts ? f.facts.length : 0), 0)} facts across ${facts.length} areas`)

// The prior-art ledger runs on EVERY ground — never optional, no skip path. Enforced above by the mandatory
// non-empty capabilities arg: there is no way to reach here without capabilities to run the ledger over.
const ledger = await workflow('prior-art-ledger', { projectPath, capabilities })

return { projectPath, facts, ledger }
