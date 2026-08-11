export const meta = {
  name: 'hidden-decision-scan',
  description: 'GROUND-phase adversary: scans a contract plus the grounded code for forced, lasting, costly-to-reverse choices that are NOT in the contract Decisions section and that the human would care about — so no key decision is silently defaulted and bites later.',
  phases: [
    { title: 'Hunt', detail: 'one hunter per lens finds forced, undecided choices', model: 'sonnet' },
    { title: 'Filter', detail: 'apply forced+lasting+bites, drop noise, rank by bite', model: 'opus' },
  ],
}

// One hunter's raw candidates for its lens.
const CANDIDATES_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    lensId: { type: 'string' },
    candidates: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          choice: { type: 'string' },   // the decision that must be made, one line
          forcedBy: { type: 'string' }, // what default gets silently taken if no one decides
          category: { type: 'string' },
          evidence: { type: 'string' }, // contract section or file:line
        },
        required: ['choice', 'forcedBy'],
      },
    },
  },
  required: ['lensId', 'candidates'],
}

// The filtered, ranked list the human must act on.
const FINDINGS_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          choice: { type: 'string' },
          whyForced: { type: 'string' },
          options: { type: 'array', items: { type: 'string' } },
          howItBites: { type: 'string' },
          recommended: { type: 'string' },
          severity: { type: 'string', enum: ['high', 'medium', 'low'] },
        },
        required: ['choice', 'whyForced', 'howItBites', 'severity'],
      },
    },
    droppedAsNoise: { type: 'integer' },
  },
  required: ['findings'],
}

let input = args
let unwrapGuard = 0
while (typeof input === 'string' && unwrapGuard < 5) {
  try {
    input = JSON.parse(input)
  } catch (parseError) {
    throw new Error('hidden-decision-scan: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const contractPath = input && input.contractPath
const projectPath = input && input.projectPath

if (!contractPath || !projectPath) {
  throw new Error(
    'hidden-decision-scan requires args { contractPath, projectPath } (got type: ' + typeof args + ')')
}

// The lenses a single agent would miss on its own — each hunts one family of forced, undecided choices.
const LENSES = [
  {
    id: 'tool-defaults',
    focus: 'Tool, SDK, library, or framework DEFAULTS the contract silently accepts where more than one reasonable setting exists — an auto-append/auto-generate behavior, a timestamp or encoding choice, a fail-fast default, a compression/trim/target default, a default URL or endpoint. Every unstated default is a decision a tool already made for the human.',
  },
  {
    id: 'user-dev-facing',
    focus: 'Anything a USER or DEVELOPER sees or depends on that is left unpinned: names of files, artifacts, commands, flags, environment variables; output formats and labels; version strings; release names; URLs; tags; identities. Once shipped, changing these breaks people.',
  },
  {
    id: 'committed-shipped',
    focus: 'Anything COMMITTED to the repo or SHIPPED whose FORM is a choice: key/cert formats and file locations, config-file schema, on-disk layout, wire/serialization formats, package metadata. Baked into git history or releases the moment it lands.',
  },
  {
    id: 'trust-identity',
    focus: 'TRUST and IDENTITY boundaries: what is trusted vs untrusted, what is signed and by whom, what is committed vs a secret, who may produce an official artifact, how a consumer verifies it. A wrong default here is a security or provenance bite.',
  },
  {
    id: 'invariants-forks',
    focus: 'Cross-cutting INVARIANTS and unmade FORKS: encodings, sort order, ranges/limits/overflow, time bases, monotonicity, precedence — and any "pick one of several" the contract leaves to the implementer.',
  },
]

const CRITERIA = `A choice qualifies ONLY if ALL THREE hold:
1. FORCED — the work cannot be built without choosing; if no one decides, a person or a tool picks a default anyway.
2. LASTING — it fixes something that outlives its function: a shipped or committed thing, a name/format/identity a user or developer sees, a public interface or command, a dependency, a trust boundary, an encoding/limit/invariant, a versioning or release scheme. NOT internal code, a private helper, an algorithm choice, or a name only the code sees.
3. Would BITE the human later — once shipped, committed, or depended on, changing it is expensive, OR it silently does the wrong thing.

EXCLUDE (this is the noise the human does not want): pure implementation with no lasting external effect; anything already stated in the contract's Decisions section; cheaply-reversible internal choices.`

const huntPrompt = (lens) => `You are a hidden-decision hunter for ONE lens. Read the CONTRACT (all of it, especially the Decisions section), explore the grounded code, and list every choice IN YOUR LENS that the work FORCES but that is NOT already in the contract's Decisions section. Do NOT propose implementations. Do NOT list pure implementation detail or anything already decided.

CONTRACT FILE: ${contractPath}  — Read it in full first, including its Decisions section, so you know what is already decided.
PROJECT PATH: ${projectPath}  — explore with CodeGraph (ToolSearch "select:mcp__codegraph__codegraph_explore" then codegraph_explore with projectPath="${projectPath}"), lore (search_code with project_dir="${projectPath}"), Grep, and Read as needed to see what the work actually touches and what defaults would apply.

YOUR LENS — ${lens.id}: ${lens.focus}

${CRITERIA}

For each qualifying choice record: choice (one line — what must be decided), forcedBy (the default that gets silently taken if no one decides), category, and evidence (the contract section or file:line that makes it forced). Return lensId="${lens.id}" and the candidates array (empty if your lens is genuinely clean — do NOT pad).`

const filterPrompt = (pooled) => `You are the care-filter judge for a hidden-decision scan. You are given candidate undecided choices pooled from several hunters. Produce the FINAL ranked list of decisions the human must make, and RUTHLESSLY drop the noise — surfacing implementation trivia is itself the failure this scan exists to avoid.

POOLED CANDIDATES (JSON):
${JSON.stringify(pooled)}

${CRITERIA}

DROP a candidate if: it is pure implementation with no lasting external effect; it is already decided in the contract; it is cheaply reversible; or it duplicates another candidate (merge duplicates into one).

For each SURVIVING decision output: choice (one line); whyForced (the default that gets taken if it stays unaddressed); options (the realistic choices, 2–4); howItBites (the concrete lock-in / user-visible / trust consequence if the wrong default is silently taken); recommended (your recommended pick, one line); severity (high/medium/low by how badly it bites). RANK worst-bite first. Also return droppedAsNoise = the count of candidates you dropped.`

const hunted = (await parallel(LENSES.map((lens) => () =>
  agent(huntPrompt(lens), { label: `hunt:${lens.id}`, phase: 'Hunt', model: 'sonnet', schema: CANDIDATES_SCHEMA }))
)).filter(Boolean)

const pooled = hunted.flatMap((h) => (h.candidates || []).map((c) => ({ ...c, lensId: h.lensId })))

log(`hunted ${pooled.length} candidate undecided choices across ${hunted.length} lenses; filtering to what the human would care about`)

const result = await agent(filterPrompt(pooled), { label: 'filter', phase: 'Filter', model: 'opus', schema: FINDINGS_SCHEMA })

return { contractPath, findings: (result && result.findings) || [], droppedAsNoise: result && result.droppedAsNoise }
