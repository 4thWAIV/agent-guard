export const meta = {
  name: 'implement',
  description: 'IMPLEMENT stage: launch the SINGLE fresh worker to build a contract exactly, with rule-writing authority REVOKED. It opens on the build+test bracket, works within the rules, cleans up the RED the rule phase left until the build is green under the rules, and STOPs at any wall instead of deviating. One worker, not a fan-out (nothing to parallelize). REFUTE is the separate refute.js round the orchestrator runs and loops after this — this script does not self-review.',
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

if (!projectPath || !contractPath) {
  throw new Error(
    'implement requires args { projectPath, contractPath } (got type: ' + typeof args + ')')
}

const workerPrompt = `You are the IMPLEMENT worker. You are a FRESH agent, different from whoever wrote the rules (separation of powers). Build the contract EXACTLY. Do NOT commit, do NOT push, do NOT migrate or prepare any target.

PROJECT PATH: ${projectPath}
CONTRACT: ${contractPath} — Read it in FULL. Build its "What to do", stay inside its "Surfaces" (the only files you may touch), obey every "What the agent MUST NOT do", and prove every item in its "Acceptance".

YOUR AUTHORITY IS LIMITED:
- Rule-writing authority is REVOKED. You may NOT add, edit, or suppress any analyzer; \`analyzers/**\` is off-limits to you, and so is anything the contract marks frozen (e.g. \`Abstractions/**\`). If a rule blocks you, you STOP and escalate — you never suppress it, \`NoWarn\` it, lower its severity, edit its definition, or edit a test to pass.
- Do ONLY this contract's unit of work. Do not add, ship, or decide anything beyond it (tier permits more only with explicit human approval).

THE BUILD STARTS RED, AND THAT IS THE POINT:
- The rule phase already wired the guardrails in, so \`dotnet build\` is RED right now against the code this contract exists to change. Run the build+test bracket first to see that starting state, so you know exactly what the rules demand. Your job is to make it GREEN by doing the contract's implementation — deleting/moving the violating code the contract names — NOT by touching the rules. Never call a red "pre-existing" and never footnote it; it is your work to clear.
- The working tree already contains the rule phase's new analyzer files. Do not revert them or any other unrelated work.

BLOCK THESE FAILURE CLASSES BY NAME:
- Silent success (exit 0 with "skipped"; a status print over real state). Demand positive proof — flags AND bytes AND command output with exit codes.
- Second-store miss (the same value in two stores, only one verified). Enumerate every surface first.
- Weakening a requirement to fit the code, or editing a test/oracle to pass. Grow the code to meet the requirement.
- Scaffolded or gamed metric (a check hard-wired to pass). The check must measure reality.
- Suppressing a rule to reach green (\`#pragma warning disable\`, \`[SuppressMessage]\`, \`NoWarn\`, \`severity = none\`, a dropped analyzer reference). Forbidden; STOP instead.

PROVE (paste verbatim, with exit codes): \`dotnet build -c Release\` is 0 warnings / 0 errors, and \`dotnet test -c Release\` is 0 failed, plus each of the contract's own acceptance spot checks (its grep checks, file-existence checks, etc.). Cropped or exit-code-missing output does not count.

FINAL ANSWER: changedFiles (every file created/edited/deleted), buildProof and testProof (pasted with exit codes), acceptance (each contract acceptance item with met true/false and its evidence), walls (anything you stopped on), and complete (true only if every acceptance item is met and the build and tests are green under the rules).`

const result = await agent(workerPrompt, { label: 'implement', phase: 'Implement', schema: RESULT_SCHEMA })

return result
