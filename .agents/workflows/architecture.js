export const meta = {
  name: 'architecture',
  description: 'ARCHITECTURE stage: one architecture author turns the contract\'s decided interface structure into COMPILING C# — interfaces, DTOs, enums, and concrete-type signatures with `throw new NotImplementedException()` bodies, shape and no behavior — born under the RULE-PHASE fence. Then the INDEPENDENT panel refutes the interface surface (never the author grading its own work). Produces the skeleton files, a signature diff for the human to sign off, the compile proof, and the adversary verdicts. Separation of powers: this agent does NOT write tests and does NOT implement behavior (later stages). The human signs off the interface surface before TDD begins — that gate is the orchestrator\'s, after this returns clean.',
  phases: [
    { title: 'Skeleton', detail: 'architecture author writes the compiling interface surface, bodies throw NotImplementedException' },
    { title: 'Refute-skeleton', detail: 'independent adversaries refute the interface surface against the contract and rails', model: 'sonnet' },
  ],
}

// What the architecture author returns after writing the skeleton.
const SKELETON_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    skeletonFiles: { type: 'array', items: { type: 'string' } }, // every file created/edited under src/
    signatureDiff: { type: 'string' },  // the interface surface as a signature-only diff, for the human sign-off gate
    buildProof: { type: 'string' },     // dotnet build output (0/0) proving the skeleton compiles under the fence
    perType: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          type: { type: 'string' },      // the interface / DTO / concrete type
          role: { type: 'string' },      // what the contract's decided structure says it is
          bodies: { type: 'string' },    // "interface (no bodies)" or "throws NotImplementedException"
        },
        required: ['type', 'role'],
      },
    },
    notes: { type: 'string' },
  },
  required: ['skeletonFiles', 'signatureDiff', 'buildProof', 'perType'],
}

// One skeleton-adversary's verdict on the interface surface itself.
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
    throw new Error('architecture: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath

if (!projectPath || !contractPath) {
  throw new Error(
    'architecture requires args { projectPath, contractPath } (got type: ' + typeof args + ')')
}

// Fix round: when refutation/approved fixes are passed, the author FIXES the existing skeleton rather than writing fresh.
const refutation = input && input.refutation
const fixModePreamble = refutation
  ? `THIS IS A FIX ROUND, NOT AN INITIAL WRITE. The skeleton already exists on disk from a prior architecture run. Do NOT rewrite it from scratch and do NOT recreate files. Apply EXACTLY these confirmed, human-approved fixes — nothing more — then re-prove it compiles 0/0:

${typeof refutation === 'string' ? refutation : JSON.stringify(refutation, null, 2)}

The proof and return steps below apply unchanged.

`
  : ''

const authorPrompt = `${fixModePreamble}You are the ARCHITECTURE author. You are a FRESH agent, different from whoever wrote the rules and whoever will write the tests or the implementation (separation of powers). You turn the contract's DECIDED interface structure into COMPILING C#. Do NOT commit, do NOT push.

PROJECT PATH: ${projectPath}
CONTRACT: ${contractPath} — Read it in FULL. Its Decisions and its interface structure are what you build; build ONLY the shape the contract decided, nothing it did not.

WHAT YOU WRITE — SHAPE ONLY, NO BEHAVIOR:
- The interfaces the contract decided, in the assemblies the contract names.
- The DTOs, records, and enums those signatures need.
- The concrete production types the contract names, with their real SIGNATURES — for a NEW type or member, the body is an unimplemented \`throw new NotImplementedException()\` (no logic, no branching, no real behavior). Interfaces have no bodies.
- When the contract EXPANDS existing functionality, leave every existing body EXACTLY as it is — never replace working code with a throw. Add or change ONLY the signatures the expansion needs. If the expansion changes no signature at all, you have nothing to write: say so in notes, and the run proceeds to TDD against the existing types.
- Nothing else. No tests (that is the TDD stage). No implementation (that is IMPLEMENT). No helper you were not asked for.

HONOR THE CONTRACT, BUT START NO IMPLEMENTATION — PREPARE THE GROUND SO THE TESTS CAN BE WRITTEN (this is the line):
- Build the FULL surface the contract decided — the new interfaces, types, and enums AND every signature change its decisions require, INCLUDING reshaping an existing method or rippling that change through its callers when the contract calls for it (for example making the approval gate and the commands that call it async, or replacing a now-callerless method). Honor the contract's decided shape completely; do not trim it, and do not invent shape it did not decide.
- But write NO implementation. Every body you create or reshape is \`throw new NotImplementedException()\` — no real logic, no behavior, no branch that does actual work: not the presence check, not the timeout race, not the reason mapping, not a real return value. An existing body you are NOT reshaping stays exactly as it is.
- The ONE test of "enough" is the tests: produce only what the TDD test author needs to WRITE and COMPILE the acceptance tests against this surface and watch them fail RED. If a type, signature, or member must exist for the tests to be written, declare it (throwing). If it is behavior the tests will drive, leave it unimplemented. Nothing beyond what the tests need in order to exist and compile.

BORN UNDER THE FENCE:
- The analyzer rules from RULE-PHASE are already live. Your skeleton must be born COMPLIANT — 0 warnings, 0 errors — not RED. If a rule blocks the shape the contract decided, you STOP and escalate; you never suppress, \`NoWarn\`, lower a severity, or edit a rule to fit. The rules are frozen to you.

BLOCK THESE FAILURE CLASSES BY NAME:
- Adding behavior "just to compile" into a NEW body — a real return value, a cached field, a branch. A new body is \`throw new NotImplementedException()\` and nothing else; that is what makes the tests go RED next.
- Stubbing EXISTING working code back to a throw — leave every existing body as it is; only new members throw.
- Starting implementation — writing any real behavior into a body instead of \`throw new NotImplementedException()\`: the actual presence check, the timeout race, the reason mapping, a real return value, or any branch that does work. Reshaping a signature the contract decides is correct (stub the body throwing); implementing what that signature DOES is IMPLEMENT's job, driven by the tests.
- Inventing an interface, member, or type the contract did not decide (scope creep), or dropping one it did (easy-half).
- A weakened signature that quietly matches something easier to implement than the contract requires.

PROVE (paste verbatim, with exit codes): \`dotnet build -c Release\` is 0 warnings / 0 errors — the skeleton compiles clean UNDER the live rules.

FINAL ANSWER: skeletonFiles (every file created/edited under src/), signatureDiff (the interface surface as a signature-only view — this is what the human reviews and signs off, so make it a clean read of every interface, type, and member with its signature), buildProof (pasted with exit code), perType (each type with its role from the contract and whether its bodies are interface-none or throw), and notes.`

const author = await agent(authorPrompt, { label: 'architecture', phase: 'Skeleton', schema: SKELETON_SCHEMA })

if (!author || !author.skeletonFiles || !author.skeletonFiles.length) {
  return { skeletonFiles: [], error: 'architecture author produced no skeleton files', author }
}

log(`architecture author wrote ${author.skeletonFiles.length} files; handing the interface surface to the independent adversary panel`)

// The independent adversaries judge the INTERFACE SURFACE itself — separate agents, never the author. The skeleton
// COMPILES (unlike the RED rules), so a failing build here IS a real fault, not the expected RED of the rule phase.
const SKELETON_ADVERSARIES = [
  { id: 'solid', rail: 'rails-solid-code', focus: "the interface surface's design and structure — right seams, single responsibility, no god-interface, no leaked concrete type where an interface belongs, faithful to the contract's decided structure", model: 'sonnet' },
  { id: 'dry', rail: 'rails-dry-code', focus: 'duplication across the interfaces and types — no member/DTO/enum spelled twice, reuse the existing abstractions the contract names', model: 'sonnet' },
  { id: 'lie-catcher', rail: 'rails-decisions', focus: 'honesty of the skeleton — every interface, type, and member traces to the contract\'s decided structure with the human\'s sign-off; no smuggled shape, no real behavior hidden in a "skeleton" body, no signature weakened from what the contract requires', model: 'opus' },
]

const skeletonRefutePrompt = (adv) => `You are the ${adv.id} adversary judging the INTERFACE SURFACE an architecture author just wrote — the compiling skeleton, NOT a finished implementation. Do NOT make code changes. Every method body is \`throw new NotImplementedException()\` ON PURPOSE (the tests go RED against it next); an unimplemented body is CORRECT here and is never a finding. Judge the SHAPE — the interfaces, types, members, and signatures — not the missing behavior.

Read .agents/skills/${adv.rail}/SKILL.md — it is your PASS/FAIL checklist. Read the contract's Decisions and interface structure (${contractPath}) and the skeleton files (${JSON.stringify(author.skeletonFiles)}). Your lens: ${adv.focus}.

${adv.id === 'lie-catcher'
  ? 'Give NO fix advice. Confirm the skeleton compiles 0/0 under the live rules yourself. YELL any interface/type/member that does NOT trace to the contract\'s decided structure and the human\'s sign-off; any real behavior smuggled into a body instead of NotImplementedException; any signature weakened from what the contract requires; and any suppression used to make the skeleton compile.'
  : 'Rule each Violation in your rail against the interface surface with exact file:line. Give the fix path. Remember: a thrown-not-implemented body is not a Violation.'}

Return lens="${adv.id}", verdict PASS or FAIL, findings (summary, evidence as file:line, and fix — leave fix empty for the lie-catcher), refutationAttempts (what you tried to break; none means rubber-stamping), and proofChecked.`

const verdicts = (await parallel(SKELETON_ADVERSARIES.map((adv) => () =>
  agent(skeletonRefutePrompt(adv), { label: `refute-skeleton:${adv.id}`, phase: 'Refute-skeleton', model: adv.model, schema: VERDICT_SCHEMA }))
)).filter(Boolean)

const failed = verdicts.filter((v) => v.verdict === 'FAIL')
log(`${verdicts.length} skeleton-adversaries ran; ${failed.length} FAIL (${failed.map((v) => v.lens).join(', ') || 'none'})`)

// The human sign-off on signatureDiff is the orchestrator's gate AFTER this returns clean — it is not run here.
return { skeletonFiles: author.skeletonFiles, signatureDiff: author.signatureDiff, buildProof: author.buildProof, perType: author.perType, verdicts, anyFail: failed.length > 0 }
