export const meta = {
  name: 'prior-art-ledger',
  description: 'For each capability a job needs, search every lens (CodeGraph, lore semantic, grep), then a cheap model rules reuse / extract / new — producing the prior-art ledger that keeps duplicates out at design time.',
  phases: [
    { title: 'Search', detail: 'run all lenses per capability', model: 'sonnet' },
    { title: 'Judge', detail: 'cheap model rules reuse/extract/new', model: 'sonnet' },
  ],
}

// The pooled hits one search agent returns for one capability.
const CANDIDATES_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    capabilityId: { type: 'string' },
    candidates: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          lens: { type: 'string', enum: ['codegraph', 'lore', 'grep'] },
          file: { type: 'string' },
          line: { type: 'integer' },
          symbol: { type: 'string' },
          snippet: { type: 'string' },
        },
        required: ['lens', 'file', 'symbol'],
      },
    },
    lensesRun: { type: 'array', items: { type: 'string' } },
    lensesEmpty: { type: 'array', items: { type: 'string' } },
  },
  required: ['capabilityId', 'candidates', 'lensesRun'],
}

// The judge's ruling for one capability: one line of the ledger.
const VERDICT_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    capabilityId: { type: 'string' },
    decision: { type: 'string', enum: ['reuse', 'extract', 'new'] },
    owner: { type: 'string' },
    copies: { type: 'array', items: { type: 'string' } },
    evidence: { type: 'string' },
    confidence: { type: 'string', enum: ['high', 'medium', 'low'] },
    rationale: { type: 'string' },
  },
  required: ['capabilityId', 'decision', 'confidence', 'rationale'],
}

let input = args
let unwrapGuard = 0
while (typeof input === 'string' && unwrapGuard < 5) {
  try {
    input = JSON.parse(input)
  } catch (parseError) {
    throw new Error('prior-art-ledger: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const capabilities = input && input.capabilities

if (!projectPath || !capabilities || !capabilities.length) {
  throw new Error(
    'prior-art-ledger requires args { projectPath, capabilities: [{id, description}] } (got type: ' + typeof args + ')')
}

const gatherPrompt = (cap) => `You are a code-search agent. Find every existing place in this codebase that ALREADY provides the capability below. Do NOT write code. Never invent a hit — only report real results the tools return.

PROJECT PATH: ${projectPath}
CAPABILITY id="${cap.id}": ${cap.description}

Run ALL THREE search lenses and pool the hits. Load a tool first with ToolSearch if it is not already available.

1. CodeGraph (structural + call graph): ToolSearch "select:mcp__codegraph__codegraph_explore", then call codegraph_explore with projectPath="${projectPath}" and a query naming the key symbols/terms for this capability.
2. Lore (semantic, by meaning): ToolSearch "select:mcp__plugin_elves_lore__search_code", then call search_code with project_dir="${projectPath}", query = a plain-English phrase describing the capability, limit 8.
3. Grep (lexical): use the Grep tool (or Bash ripgrep) over ${projectPath}/src for the distinctive identifiers/tokens this capability would use.

For each hit record: which lens found it, file, line (if known), symbol name, and a one-line snippet.
Return: capabilityId="${cap.id}", the pooled candidates, lensesRun (every lens you actually ran), and lensesEmpty (lenses that returned nothing). If a lens errors, still list it in lensesRun and treat it as empty.`

const evaluatePrompt = (cap, found) => `You are a reuse judge. Rule whether the capability below ALREADY exists in the codebase, using ONLY the pooled search hits provided. Do NOT write code. Do NOT invent hits.

CAPABILITY id="${cap.id}": ${cap.description}

POOLED SEARCH HITS (JSON):
${JSON.stringify(found)}

Choose exactly one decision:
- "reuse": exactly one existing symbol already provides this capability. Set owner="file:line".
- "extract": the capability's logic already appears in TWO OR MORE places with no single shared owner (a duplication). Set copies=["file:line", ...] for EVERY copy. Correct action is to extract one shared owner and reuse it.
- "new": nothing provides this capability AND every lens was run and came back empty. Only valid when the search genuinely found nothing that does this; cite in evidence that all lenses ran and were empty.

Give evidence (the file:line facts you relied on), confidence (high/medium/low), and a one-sentence rationale. Return capabilityId="${cap.id}".`

const results = await pipeline(
  capabilities,
  (cap) => agent(gatherPrompt(cap), { label: `search:${cap.id}`, phase: 'Search', model: 'sonnet', schema: CANDIDATES_SCHEMA }),
  (found, cap) => agent(evaluatePrompt(cap, found), { label: `judge:${cap.id}`, phase: 'Judge', model: 'sonnet', effort: 'low', schema: VERDICT_SCHEMA }),
)

return { projectPath, verdicts: results.filter(Boolean) }
