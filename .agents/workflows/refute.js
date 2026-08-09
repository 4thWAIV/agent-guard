export const meta = {
  name: 'refute',
  description: 'REFUTE stage fan-out: the independent adversaries (Prove-It, SOLID, DRY, laziness-auditor, Lie-catcher) each try to REFUTE the finished change against the contract and the rails, in parallel. Produces one verdict per lens; any FAIL blocks the gate.',
  phases: [
    { title: 'Refute', detail: 'each adversary refutes against the contract and its rail', model: 'sonnet' },
  ],
}

// One adversary's verdict.
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
          evidence: { type: 'string' }, // file:line or the command re-run
          fix: { type: 'string' },      // path to fix — left empty by the Lie-catcher
        },
        required: ['summary', 'evidence'],
      },
    },
    refutationAttempts: { type: 'array', items: { type: 'string' } }, // what it tried to break — none = rubber-stamp
    proofChecked: { type: 'array', items: { type: 'string' } },       // which contract commands it re-ran
  },
  required: ['lens', 'verdict', 'refutationAttempts'],
}

let input = args
let unwrapGuard = 0
while (typeof input === 'string' && unwrapGuard < 5) {
  try {
    input = JSON.parse(input)
  } catch (parseError) {
    throw new Error('refute: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath
const changedFiles = input && input.changedFiles

if (!projectPath || !contractPath) {
  throw new Error(
    'refute requires args { projectPath, contractPath, changedFiles?: [..] } (got type: ' + typeof args + ')')
}

// Each adversary is a distinct lens; SOLID/DRY/laziness/Lie-catcher each READ their owning rail as the checklist.
const ADVERSARIES = [
  {
    id: 'prove-it',
    charge: `You are the Prove-It / anti-review-failure adversary. Rule 1 FIRST: did any change narrow, soften, or reinterpret a contract requirement to fit what the code already does, instead of growing the code to meet it? If so, FAIL. Then, for each contract acceptance check: is it MET with proof pasted verbatim (flags AND bytes AND command output with exit codes), or merely asserted? Re-run the contract's spot checks yourself. Hunt what was left unimplemented, unmigrated, unverified, stale, or scoped-out; assumptions made without reading real code or output; records that could be lost or mismatched; provenance not preserved before overwrite; success claimed while a caveat is open; any red called "pre-existing" (banned).`,
  },
  {
    id: 'solid',
    charge: `You are the SOLID adversary — design and structure ONLY (duplication is the DRY adversary's job). Read .agents/skills/rails-solid-code/SKILL.md — it is your PRIMARY PASS/FAIL checklist; rule each Violation line against the change with exact file:line. Any one confirmed Violation is a FAIL.`,
  },
  {
    id: 'dry',
    charge: `You are the DRY adversary — duplication ONLY. Read .agents/skills/rails-dry-code/SKILL.md — it is your PRIMARY PASS/FAIL checklist. Re-run every discovery lens yourself (CodeGraph, lore, grep): FAIL any capability the contract's reuse ledger marked "new" that is not empty on every lens, and hunt every duplicated value, block, or whole function, including copies across modules the in-build analyzers cannot see. Any one confirmed Violation is a FAIL.`,
  },
  {
    id: 'laziness',
    charge: `You are the laziness-auditor — shortcut / low-quality work. Read .agents/skills/rails-real-work/SKILL.md — it is your PRIMARY PASS/FAIL checklist. Confirm each Violation against the finished work and its live ground truth: the easy-half shortcut, the hacked result, the one-case design, dropped purpose, noise over signal, stale or unused data. Any one confirmed Violation is a FAIL.`,
  },
  {
    id: 'lie-catcher',
    model: 'opus', // Tim relies on this reviewer more than any other; it runs on the strongest model
    charge: `You are the Lie-catcher. Give NO fix advice — leave every finding's fix empty. Read .agents/skills/rails-decisions/SKILL.md — it is your PRIMARY PASS/FAIL checklist for every decision-level item. FIRST rule the run against the contract's SUCCESS DEFINITION: any criterion unmet or any resulting system error is FAIL, no matter the progress. Then YELL, most-damaging first: every deviation, fake justification, unproven claim, and weakened / skipped / loosened test (diff the test files); every decision-level item lacking the human's cited verbatim approval, and every "approval" that is really a non-answer, a topic change, or a reword request treated as a yes; ANY suppression of ANY analyzer warning or error — not only the new rules — by any means (#pragma warning disable, [SuppressMessage], NoWarn, severity = none, an .editorconfig severity edit, or a dropped analyzer reference): the RED is cleared by doing the real work, never by silencing a warning. There is no automated guard blocking suppression yet, so YOU are the only thing catching it — treat any suppression anywhere in the change as an automatic FAIL. Audit the ORCHESTRATOR's steps too — no one is exempt.`,
  },
]

const refutePrompt = (adv) => `You are an adversary for a FINISHED change. Do NOT make code changes. Your job is to REFUTE that this change meets the CONTRACT and the rails — assume it does NOT until proven. Ground every judgment in the live code and re-run the contract's spot checks yourself; cropped, stale, or exit-code-missing output is an automatic FAIL.

${adv.charge}

CONTRACT FILE: ${contractPath} — Read it in full, especially the SUCCESS DEFINITION, Decisions, Surfaces, and reuse ledger.
PROJECT PATH: ${projectPath} — explore the live code (CodeGraph "select:mcp__codegraph__codegraph_explore" then codegraph_explore with projectPath="${projectPath}", lore search_code with project_dir="${projectPath}", Grep, Read) and re-run the contract's verification commands.
CHANGED FILES: ${JSON.stringify(changedFiles || [])}

Return lens="${adv.id}", verdict (PASS or FAIL), findings (each with summary, evidence as file:line or the command re-run, and the fix path — leave fix empty if you are the Lie-catcher), refutationAttempts (what you actually tried to break — an adversary that lists none is rubber-stamping), and proofChecked (which contract commands you re-ran).`

const verdicts = (await parallel(ADVERSARIES.map((adv) => () =>
  agent(refutePrompt(adv), { label: `refute:${adv.id}`, phase: 'Refute', model: adv.model || 'sonnet', schema: VERDICT_SCHEMA }))
)).filter(Boolean)

const failed = verdicts.filter((v) => v.verdict === 'FAIL')
log(`${verdicts.length} adversaries ran; ${failed.length} FAIL (${failed.map((v) => v.lens).join(', ') || 'none'})`)

return { contractPath, verdicts, anyFail: failed.length > 0, failedLenses: failed.map((v) => v.lens) }
