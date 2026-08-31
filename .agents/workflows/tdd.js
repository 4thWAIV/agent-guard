export const meta = {
  name: 'tdd',
  description: 'TDD stage: one test author writes the acceptance tests — unit AND per-OS pointed-integration — from the contract\'s Acceptance section against the ARCHITECTURE skeleton, RED because the skeleton throws NotImplementedException. Then the INDEPENDENT panel refutes the tests themselves against the rails-test-code standard (never the author grading its own tests). Produces the test files, the RED proof, per-criterion coverage, and the adversary verdicts. Separation of powers: this agent does NOT change an interface and does NOT implement behavior (later stages clean the RED to green).',
  phases: [
    { title: 'Write-tests', detail: 'test author writes the acceptance tests against the skeleton, RED because it throws' },
    { title: 'Refute-tests', detail: 'independent adversaries refute the tests against rails-test-code and the contract', model: 'sonnet' },
  ],
}

// What the test author returns after writing the tests.
const TESTGEN_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    testFiles: { type: 'array', items: { type: 'string' } }, // every test file created/edited under tests/
    redProof: { type: 'string' },  // dotnet test output showing the new tests FAIL RED against the throwing skeleton
    coverage: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          acceptanceItem: { type: 'string' }, // the contract Acceptance criterion
          tests: { type: 'array', items: { type: 'string' } }, // the test(s) that prove it
          kind: { type: 'string' }, // "unit" or "pointed-integration (per-OS)" or both
          red: { type: 'boolean' }, // does it fail now against the throwing skeleton
        },
        required: ['acceptanceItem', 'tests', 'red'],
      },
    },
    notes: { type: 'string' },
  },
  required: ['testFiles', 'redProof', 'coverage'],
}

// One test-adversary's verdict on the tests themselves.
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
    throw new Error('tdd: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath

if (!projectPath || !contractPath) {
  throw new Error(
    'tdd requires args { projectPath, contractPath } (got type: ' + typeof args + ')')
}

// Fix round: when refutation/approved fixes are passed, the author FIXES the existing tests rather than writing fresh.
const refutation = input && input.refutation
const fixModePreamble = refutation
  ? `THIS IS A FIX ROUND, NOT AN INITIAL WRITE. The tests already exist on disk from a prior tdd run. Do NOT rewrite them from scratch and do NOT recreate files. Apply EXACTLY these confirmed, human-approved fixes — nothing more — then re-prove the tests are RED against the throwing skeleton:

${typeof refutation === 'string' ? refutation : JSON.stringify(refutation, null, 2)}

The proof and return steps below apply unchanged.

`
  : ''

const authorPrompt = `${fixModePreamble}You are the TDD test author. You are a FRESH agent, different from whoever wrote the rules, the skeleton, or the implementation (separation of powers). You write the acceptance tests, and ONLY the tests. Do NOT commit, do NOT push.

PROJECT PATH: ${projectPath}
CONTRACT: ${contractPath} — Read it in FULL. Its Acceptance section is the source of the tests: every acceptance criterion gets at least one test that would prove it, derived from the criterion's own words.
STANDARD: Read .agents/skills/rails-test-code/SKILL.md — it is your checklist for what makes a test real, and it is exactly what the panel will refute your tests against.

WHAT YOU WRITE — TESTS AGAINST THE SKELETON, RED:
- The ARCHITECTURE surface is already on disk: the interfaces and concrete types exist. Write your tests against those real types, so they COMPILE — and they must FAIL (RED) now: for a NEW member because its body throws \`NotImplementedException\`; for an EXPANDED existing member because today's behavior returns the wrong result for the inputs the change requires (a plain expected-vs-actual failure). That RED is the forcing function for the IMPLEMENT worker, the same role the RED rules play in RULE-PHASE. For an expansion, a test that asserts the member's CURRENT behavior passes GREEN and proves nothing — assert the REQUIRED new behavior, which today's code fails.
- Cover EVERY acceptance criterion. Unit tests for logic; per-OS pointed-integration tests (real on-disk / real-OS behavior) for anything whose truth depends on the actual OS or boundary — the integration coverage the project chronically skips is your job to write, authored here and run on the per-OS CI legs.
- Each test is two-sided where the criterion has a pass and a fail case; none is trivially-passing or tautological; none tests a mock instead of the real behavior. Follow rails-test-code.

YOUR AUTHORITY IS LIMITED:
- You may NOT change an interface or any skeleton signature (that was the ARCHITECTURE stage, human-signed-off and frozen to you). If a test needs a seam the skeleton does not expose, you STOP and escalate — you never widen an interface to fit a test.
- You do NOT implement behavior and you do NOT fill a skeleton body. Your tests stay RED; the IMPLEMENT worker turns them green.

BLOCK THESE FAILURE CLASSES BY NAME:
- A test that is GREEN now (before the change) — it proves nothing about the change; every acceptance test MUST be RED against the pre-change state (a new body throwing, or existing behavior wrong for the required inputs).
- A single-sided test (only the happy path, or only the error path) where the criterion has both.
- A tautological / trivially-passing assertion, or one that asserts against a mock instead of the real type.
- Dropping an acceptance criterion (easy-half), or testing something the contract did not ask for (scope creep).

PROVE THE RED (paste verbatim, with exit code): run \`dotnet test\` and show your new tests FAILING against the throwing skeleton (a \`NotImplementedException\` per acceptance test). Do NOT make them green.

FINAL ANSWER: testFiles (every test file created/edited under tests/), redProof (the pasted failing-test output), coverage (each acceptance criterion mapped to its test(s), its kind unit/pointed-integration, and whether it is RED now), and notes.`

const testGen = await agent(authorPrompt, { label: 'test-author', phase: 'Write-tests', schema: TESTGEN_SCHEMA })

if (!testGen || !testGen.testFiles || !testGen.testFiles.length) {
  return { testFiles: [], error: 'test author produced no test files', testGen }
}

log(`test author wrote ${testGen.testFiles.length} files; handing the tests to the independent adversary panel`)

// The independent adversaries judge the TESTS themselves — separate agents, never the author. The tests are
// INTENTIONALLY RED (they fail against the throwing skeleton); RED is correct here and is never a reason to fail them.
const TEST_ADVERSARIES = [
  { id: 'test-quality', rail: 'rails-test-code', focus: 'the tests as tests — acceptance-derived, RED-first, both unit and per-OS pointed-integration, two-sided, no trivially-passing or mock-testing-a-mock fake, every acceptance criterion covered', model: 'sonnet' },
  { id: 'dry', rail: 'rails-dry-code', focus: 'duplication across the tests — shared fixtures/builders reused, no copy-pasted setup or assertion block', model: 'sonnet' },
  { id: 'lie-catcher', rail: 'rails-decisions', focus: 'honesty of the tests', model: 'opus' },
]

const testRefutePrompt = (adv) => `You are the ${adv.id} adversary judging the acceptance TESTS a test author just wrote — NOT a finished implementation. Do NOT make code changes. The tests are INTENTIONALLY RED right now: they fail against the pre-change state — a NEW body throwing \`NotImplementedException\`, or an EXPANDED member's current behavior being wrong for the inputs the change requires. RED is correct here; never fail the tests because they are red — a test that is GREEN before the change is the fault, not one that is red.

Read .agents/skills/${adv.rail}/SKILL.md — it is your PASS/FAIL checklist. Read the contract's Acceptance section (${contractPath}) and the test files (${JSON.stringify(testGen.testFiles)}). Your lens: ${adv.focus}.

${adv.id === 'lie-catcher'
  ? 'Give NO fix advice. Re-run dotnet test yourself. YELL any acceptance criterion with NO covering test (dropped), any test that is GREEN now instead of RED (it exercises nothing), any skipped/xfail/[Fact(Skip)]/commented-out or weakened test, any single-sided test where the criterion has both sides, and any assertion that passes trivially or asserts against a mock instead of the real type.'
  : 'Rule each Violation in your rail against the tests with exact file:line. Give the fix path. Remember: a test that FAILS against the throwing skeleton is correct, not a Violation.'}

Return lens="${adv.id}", verdict PASS or FAIL, findings (summary, evidence as file:line, and fix — leave fix empty for the lie-catcher), refutationAttempts (what you tried to break; none means rubber-stamping), and proofChecked.`

const verdicts = (await parallel(TEST_ADVERSARIES.map((adv) => () =>
  agent(testRefutePrompt(adv), { label: `refute-tests:${adv.id}`, phase: 'Refute-tests', model: adv.model, schema: VERDICT_SCHEMA }))
)).filter(Boolean)

const failed = verdicts.filter((v) => v.verdict === 'FAIL')
log(`${verdicts.length} test-adversaries ran; ${failed.length} FAIL (${failed.map((v) => v.lens).join(', ') || 'none'})`)

return { testFiles: testGen.testFiles, redProof: testGen.redProof, coverage: testGen.coverage, verdicts, anyFail: failed.length > 0 }
