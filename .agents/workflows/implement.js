export const meta = {
  name: 'implement',
  description: 'IMPLEMENT stage: launch the SINGLE fresh worker to implement the behavior against a contract, with rule-, interface-, and test-writing authority ALL revoked. It opens on the build+test bracket, fills the ARCHITECTURE skeleton\'s NotImplementedException bodies and clears any rule RED until the build is green under the rules AND the RED TDD tests pass, and STOPs at any wall instead of deviating. One worker, not a fan-out (nothing to parallelize). REFUTE is the separate refute.js round the orchestrator runs and loops after this — this script does not self-review.',
  phases: [
    { title: 'Implement', detail: 'one fresh worker builds the contract within the rules, RED -> green' },
  ],
}

// The worker's proof-carrying result.
const RESULT_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    changedFiles: { type: 'array', items: { type: 'string' } }, // every file created/edited/deleted
    buildProof: { type: 'string' }, // dotnet build -c Release output with its exit code
    testProof: { type: 'string' },  // dotnet test -c Release output with its exit code
    acceptance: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          item: { type: 'string' },      // the contract acceptance item
          met: { type: 'boolean' },
          evidence: { type: 'string' },   // the command output / file:line proving it
        },
        required: ['item', 'met'],
      },
    },
    walls: { type: 'array', items: { type: 'string' } }, // anything it stopped on instead of deviating
    complete: { type: 'boolean' },                        // every acceptance item met, build+test green
  },
  required: ['changedFiles', 'buildProof', 'testProof', 'acceptance', 'complete'],
}

let input = args
let unwrapGuard = 0
while (typeof input === 'string' && unwrapGuard < 5) {
  try {
    input = JSON.parse(input)
  } catch (parseError) {
    throw new Error('implement: args string did not parse as JSON: ' + parseError.message)
  }
  unwrapGuard++
}

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath
const instruction = input && input.instruction

if (!projectPath || !contractPath) {
  throw new Error(
    'implement requires args { projectPath, contractPath } (got type: ' + typeof args + ')')
}

const instructionBlock = instruction
  ? `ADDITIONAL DIRECTION FROM THE ORCHESTRATOR (follow it exactly, on top of the contract — it relaxes no rule and no acceptance check):\n${typeof instruction === 'string' ? instruction : JSON.stringify(instruction, null, 2)}\n\n`
  : ''

const workerPrompt = `${instructionBlock}You are the IMPLEMENT worker. You are a FRESH agent, different from whoever wrote the rules, the skeleton (ARCHITECTURE), and the tests (TDD) — separation of powers. You implement the behavior, and only the behavior. Do NOT commit, do NOT push, do NOT migrate or prepare any target.

PROJECT PATH: ${projectPath}
CONTRACT: ${contractPath} — Read it in FULL. Build its "What to do", stay inside its "Surfaces" (the only files you may touch), obey every "What the agent MUST NOT do", and prove every item in its "Acceptance".

WHAT YOU DO — WRITE THE BEHAVIOR, TURN RED TO GREEN:
- The ARCHITECTURE stage wrote the interfaces and any new concrete types (a new body is an unimplemented \`throw new NotImplementedException()\`; existing bodies it left as they are). The TDD stage wrote the acceptance tests, and they FAIL (RED) now — for NEW code because the body throws, and for an EXPANSION of existing code because today's behavior returns the wrong result for the inputs the change requires. Your job is to make every RED test GREEN by writing the new behavior into the throwing bodies AND correcting the existing behavior the expansion changes, and to clear any rule RED the contract names — WITHOUT touching the rules, the interfaces, or the tests. Run the build+test bracket first to see the starting RED state, so you know exactly what the rules and tests demand. Never call a red "pre-existing" and never footnote it; it is your work to clear.
- The working tree already contains the rule-phase analyzers, the ARCHITECTURE surface, and the TDD tests. Do not revert them or any other unrelated work.

YOUR AUTHORITY IS BOXED IN ON EVERY SIDE:
- Rule-writing authority REVOKED: you may NOT add, edit, or suppress any analyzer; \`analyzers/**\` is off-limits, and so is anything the contract marks frozen. If a rule blocks you, STOP and escalate — never suppress it, \`NoWarn\` it, lower its severity, or edit its definition.
- Interface authority REVOKED: you may NOT change any interface or skeleton signature the ARCHITECTURE stage set (human-signed-off and frozen). If you need a seam the skeleton does not expose, STOP and escalate — never widen an interface to make your code fit.
- Test authority REVOKED: you may NOT weaken, delete, skip, xfail, or re-point any test the TDD stage wrote. A red test is made green by growing the code, never by softening the test.
- Do ONLY this contract's unit of work. Do not add, ship, or decide anything beyond it (tier permits more only with explicit human approval).

BLOCK THESE FAILURE CLASSES BY NAME:
- Silent success (exit 0 with "skipped"; a status print over real state). Demand positive proof — flags AND bytes AND command output with exit codes.
- Second-store miss (the same value in two stores, only one verified). Enumerate every surface first.
- Weakening a requirement to fit the code, or editing a test/oracle to pass. Grow the code to meet the requirement.
- Scaffolded or gamed metric (a check hard-wired to pass). The check must measure reality.
- Suppressing a rule to reach green (\`#pragma warning disable\`, \`[SuppressMessage]\`, \`NoWarn\`, \`severity = none\`, a dropped analyzer reference). Forbidden; STOP instead.

PROVE (paste verbatim, with exit codes): \`dotnet build -c Release\` is 0 warnings / 0 errors, and \`dotnet test -c Release\` is 0 failed — the TDD acceptance tests now PASS against your implementation, not one skipped or weakened — plus each of the contract's own acceptance spot checks (its grep checks, file-existence checks, etc.). Cropped or exit-code-missing output does not count.

FINAL ANSWER: changedFiles (every file created/edited/deleted), buildProof and testProof (pasted with exit codes), acceptance (each contract acceptance item with met true/false and its evidence), walls (anything you stopped on), and complete (true only if every acceptance item is met and the build and tests are green under the rules).`

const result = await agent(workerPrompt, { label: 'implement', phase: 'Implement', schema: RESULT_SCHEMA })

return result
