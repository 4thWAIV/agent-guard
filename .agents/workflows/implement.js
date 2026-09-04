export const meta = {
  name: 'implement',
  description: 'IMPLEMENT stage: launch the SINGLE fresh worker to implement the behavior against a contract, with rule-, interface-, and test-writing authority ALL revoked. The selected Level determines which prior-stage results exist. When RULE-PHASE added rules or TDD added tests, IMPLEMENT observes and clears their expected RED. An approved no-rule or no-test result creates no RED to invent. The worker fills any ARCHITECTURE skeleton bodies and STOPs at any wall instead of deviating. One worker, not a fan-out. REFUTE is the separate refute.js round the orchestrator runs and loops after this; this script does not self-review.',
  phases: [
    { title: 'Implement', detail: 'one fresh worker builds the contract and clears every real rule or test RED' },
  ],
}

// ##COPIED-MODULE-BEGIN## workflow-input
// This block is shared code, pasted into every workflow script that needs it. The Workflow
// runtime gives scripts no module import and allows only one level of workflow() nesting,
// so there is no way to call shared code from another file. Do not edit this copy alone:
// every copy of a block name must stay byte-identical, and eng/check-copied-modules.mjs
// fails the moment two copies differ.
async function __workflowInput(args) {
  if (!args || typeof args !== 'object' || Array.isArray(args) || typeof args.caller !== 'string' || !args.caller || !Object.prototype.hasOwnProperty.call(args, 'value')) {
    throw new Error('workflow-input requires args { caller, value }')
  }

  let input = args.value
  let unwrapGuard = 0
  while (typeof input === 'string' && unwrapGuard < 5) {
    try {
      input = JSON.parse(input)
    } catch (parseError) {
      throw new Error(`${args.caller}: args string did not parse as JSON: ${parseError.message}`)
    }
    unwrapGuard++
  }

  return input
}
// ##COPIED-MODULE-END## workflow-input

// ##COPIED-MODULE-BEGIN## required-agent-runtime
// This block is shared code, pasted into every workflow script that needs it. The Workflow
// runtime gives scripts no module import and allows only one level of workflow() nesting,
// so there is no way to call shared code from another file. Do not edit this copy alone:
// every copy of a block name must stay byte-identical, and eng/check-copied-modules.mjs
// fails the moment two copies differ.
async function __requiredAgentRuntime(args) {
  const AUTO_RESOLVED_ITEM_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      choice: { type: 'string' },
      resolution: { type: 'string' },
      principle: { type: 'string' },
      evidence: { type: 'string' },
    },
    required: ['choice', 'resolution', 'principle', 'evidence'],
  }

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
          required: ['summary', 'evidence', 'fix'],
        },
      },
      refutationAttempts: { type: 'array', items: { type: 'string' } },
      proofChecked: { type: 'array', items: { type: 'string' } },
    },
    required: ['lens', 'verdict', 'findings', 'refutationAttempts', 'proofChecked'],
  }

  const input = await __workflowInput({ caller: 'required-agent-runtime', value: args })

  const caller = input && input.caller
  const roles = (input && input.roles) || []
  const priorPanelResults = input && input.priorPanelResults !== undefined ? input.priorPanelResults : []
  const retryRoles = input && input.retryRoles
  const roleGroups = input && input.roleGroups
  const roleGroup = input && input.roleGroup
  const priorStageResults = input && input.priorStageResults
  const successfulVerdicts = input && input.successfulVerdicts
  const validationResultProvided = input && Object.prototype.hasOwnProperty.call(input, 'validationResult')
  const validationResult = input && input.validationResult

  if (!caller || !Array.isArray(roles) || !roles.length) {
    throw new Error('required-agent-runtime requires args { caller, roles: [{ id, prompt, label, phase, schema?, resultKind?, validation? }] }')
  }
  if (successfulVerdicts !== undefined && !roles.length) throw new Error(`${caller}: successfulVerdicts requires verdict roles`)
  if (!Array.isArray(priorPanelResults)) {
    throw new Error(`${caller}: priorPanelResults must be an array when provided`)
  }
  if (retryRoles !== undefined && !Array.isArray(retryRoles)) {
    throw new Error(`${caller}: retryRoles must be an array when provided`)
  }
  if (retryRoles === undefined && priorPanelResults.length) {
    throw new Error(`${caller}: priorPanelResults requires retryRoles`)
  }

  const roleById = new Map()
  for (const role of roles) {
    if (!role || typeof role !== 'object' || typeof role.id !== 'string' || !role.id) {
      throw new Error(`${caller}: every required role must contain a nonempty id string`)
    }
    if (successfulVerdicts === undefined && !validationResultProvided && (typeof role.prompt !== 'string' || !role.prompt)) {
      throw new Error(`${caller}: every executable required role must contain a nonempty prompt string`)
    }
    if (roleById.has(role.id)) throw new Error(`${caller}: duplicate required role ${role.id}`)
    if (role.resultKind !== 'verdict' && (!role.schema || typeof role.schema !== 'object' || Array.isArray(role.schema))) {
      throw new Error(`${caller}: required role ${role.id} must supply its result schema`)
    }
    roleById.set(role.id, role)
  }

  const schemaFor = (role) => {
    const base = role.resultKind === 'verdict' ? VERDICT_SCHEMA : role.schema
    if (!role.autoResolved) return base
    return {
      ...base,
      properties: {
        ...(base.properties || {}),
        autoResolved: { type: 'array', items: AUTO_RESOLVED_ITEM_SCHEMA },
      },
      required: [...new Set([...(base.required || []), 'autoResolved'])],
    }
  }

  const autoResolvedProblem = (result) => {
    for (const item of result.autoResolved) {
      for (const field of AUTO_RESOLVED_ITEM_SCHEMA.required) {
        if (!item[field].trim()) return `every autoResolved entry must contain nonempty string ${field}`
      }
    }
    return null
  }

  const declaredSchemaProblem = (schema, value, path) => {
    if (schema.oneOf) {
      const matching = schema.oneOf.filter((candidate) => declaredSchemaProblem(candidate, value, path) === null)
      if (matching.length !== 1) return `${path} must match exactly one allowed schema`
      return null
    }
    if (schema.enum && !schema.enum.includes(value)) return `${path} must be one of ${JSON.stringify(schema.enum)}`

    if (schema.type === 'object') {
      if (!value || typeof value !== 'object' || Array.isArray(value)) return `${path} must be an object`
      for (const field of schema.required || []) {
        if (!Object.prototype.hasOwnProperty.call(value, field)) return `${path}.${field} is required`
      }
      if (schema.additionalProperties === false) {
        const unexpected = Object.keys(value).filter((field) => !Object.prototype.hasOwnProperty.call(schema.properties || {}, field))
        if (unexpected.length) return `${path} contains unexpected field ${unexpected[0]}`
      }
      for (const [field, fieldSchema] of Object.entries(schema.properties || {})) {
        if (!Object.prototype.hasOwnProperty.call(value, field)) continue
        const problem = declaredSchemaProblem(fieldSchema, value[field], `${path}.${field}`)
        if (problem) return problem
      }
      return null
    }

    if (schema.type === 'array') {
      if (!Array.isArray(value)) return `${path} must be an array`
      if (schema.minItems !== undefined && value.length < schema.minItems) return `${path} must contain at least ${schema.minItems} items`
      if (schema.maxItems !== undefined && value.length > schema.maxItems) return `${path} must contain at most ${schema.maxItems} items`
      if (schema.items) {
        for (let index = 0; index < value.length; index++) {
          const problem = declaredSchemaProblem(schema.items, value[index], `${path}[${index}]`)
          if (problem) return problem
        }
      }
      return null
    }

    if (schema.type === 'string' && typeof value !== 'string') return `${path} must be a string`
    if (schema.type === 'boolean' && typeof value !== 'boolean') return `${path} must be a boolean`
    if (schema.type === 'integer' && !Number.isInteger(value)) return `${path} must be an integer`
    if (schema.minimum !== undefined && value < schema.minimum) return `${path} must be at least ${schema.minimum}`
    return null
  }

  const verdictProblem = (role, result) => {
    if (result.lens !== role.expectedLens) return `expected lens ${String(role.expectedLens)}, received ${String(result.lens)}`
    if (!result.refutationAttempts.length || result.refutationAttempts.some((attempt) => !attempt.trim())) {
      return 'refutationAttempts must contain only actual nonblank attempts'
    }
    if (result.verdict === 'PASS' && result.findings.length) return 'a PASS result cannot contain findings'
    if (result.verdict === 'FAIL' && !result.findings.length) return 'a FAIL result must contain an actionable finding'
    if (result.verdict === 'FAIL') {
      for (const finding of result.findings) {
        if (!finding.summary.trim() || !finding.evidence.trim()) {
          return 'every FAIL finding must contain nonempty summary and evidence strings plus a fix string'
        }
        if (role.lieCatcher && finding.fix !== '') return 'every Lie-catcher FAIL finding must contain an empty fix'
        if (!role.lieCatcher && !finding.fix.trim()) return 'every non-Lie-catcher FAIL finding must contain a nonempty fix'
      }
    }
    return null
  }

  const configuredProblem = (role, result) => {
    const validation = role.validation || {}
    for (const [field, expected] of Object.entries(validation.expectedFields || {})) {
      if (result[field] !== expected) return `expected ${field} ${String(expected)}, received ${String(result[field])}`
    }
    for (const field of validation.nonEmptyArrayFields || []) {
      if (!Array.isArray(result[field]) || !result[field].length) return `${field} must contain at least one item`
    }
    for (const field of validation.emptyArrayFields || []) {
      if (!Array.isArray(result[field]) || result[field].length !== 0) return `${field} must be an empty array`
    }
    for (const [field, expectedItems] of Object.entries(validation.arrayIncludes || {})) {
      if (!Array.isArray(result[field])) return `${field} must be an array`
      if (!Array.isArray(expectedItems)) return `${field} required items must be an array`
      const missingItems = expectedItems.filter((item) => !result[field].includes(item))
      if (missingItems.length) return `${field} is missing ${JSON.stringify(missingItems)}`
    }
    for (const field of validation.nonEmptyStringFields || []) {
      if (typeof result[field] !== 'string' || !result[field].trim()) return `${field} must be a nonempty string`
    }
    for (const [field, values] of Object.entries(validation.enumFields || {})) {
      if (!Array.isArray(values) || !values.includes(result[field])) return `${field} must be one of ${JSON.stringify(values || [])}`
    }
    for (const [field, forbidden] of Object.entries(validation.forbiddenFields || {})) {
      if (result[field] === forbidden) return `${field} must not equal ${JSON.stringify(forbidden)}`
    }
    for (const [field, required] of Object.entries(validation.containsFields || {})) {
      if (typeof required !== 'string' || !required.trim()) return `${field} required evidence must be a nonempty string`
      if (typeof result[field] !== 'string' || !result[field].includes(required)) {
        return `${field} must contain the exact required evidence ${JSON.stringify(required)}`
      }
    }
    for (const field of validation.stringArrayFields || []) {
      if (!Array.isArray(result[field])) return `${field} must be an array`
      const invalidIndex = result[field].findIndex((item) => typeof item !== 'string' || !item.trim())
      if (invalidIndex >= 0) return `${field}[${invalidIndex}] must be a nonempty string`
    }
    for (const [arrayField, fields] of Object.entries(validation.itemNonEmptyStringFields || {})) {
      if (!Array.isArray(result[arrayField])) return `${arrayField} must be an array`
      for (let index = 0; index < result[arrayField].length; index++) {
        for (const field of fields) {
          if (typeof result[arrayField][index][field] !== 'string' || !result[arrayField][index][field].trim()) {
            return `${arrayField}[${index}].${field} must be a nonempty string`
          }
        }
      }
    }
    for (const [arrayField, fields] of Object.entries(validation.itemNonEmptyArrayFields || {})) {
      if (!Array.isArray(result[arrayField])) return `${arrayField} must be an array`
      for (let index = 0; index < result[arrayField].length; index++) {
        for (const field of fields) {
          if (!Array.isArray(result[arrayField][index][field]) || !result[arrayField][index][field].length) {
            return `${arrayField}[${index}].${field} must contain at least one item`
          }
        }
      }
    }
    for (const [arrayField, fields] of Object.entries(validation.itemStringArrayFields || {})) {
      if (!Array.isArray(result[arrayField])) return `${arrayField} must be an array`
      for (let index = 0; index < result[arrayField].length; index++) {
        for (const field of fields) {
          if (!Array.isArray(result[arrayField][index][field])) return `${arrayField}[${index}].${field} must be an array`
          const invalidIndex = result[arrayField][index][field].findIndex((item) => typeof item !== 'string' || !item.trim())
          if (invalidIndex >= 0) return `${arrayField}[${index}].${field}[${invalidIndex}] must be a nonempty string`
        }
      }
    }
    for (const exactSet of validation.exactItemSets || []) {
      const actualItems = result[exactSet.field]
      const expectedItems = exactSet.expectedItems
      if (!Array.isArray(actualItems)) return `${exactSet.field} must be an array`
      if (!Array.isArray(expectedItems)) return `${exactSet.field} expected items must be an array`
      const actualNames = actualItems.map((item) => item && item[exactSet.itemField])
      const duplicate = actualNames.find((item, index) => actualNames.indexOf(item) !== index)
      if (duplicate !== undefined) return `${exactSet.field} contains duplicate ${exactSet.itemField} ${JSON.stringify(duplicate)}`
      const expectedNames = [...new Set(expectedItems)]
      const missing = expectedNames.filter((item) => !actualNames.includes(item))
      if (missing.length) return `${exactSet.field} is missing ${JSON.stringify(missing)}`
      const extra = actualNames.filter((item) => !expectedNames.includes(item))
      if (extra.length) return `${exactSet.field} contains extra ${JSON.stringify(extra)}`
    }
    for (const condition of validation.whenArrayEmpty || []) {
      if (!Array.isArray(result[condition.field]) || result[condition.field].length !== 0) continue
      for (const [field, expected] of Object.entries(condition.exactFields || {})) {
        if (result[field] !== expected) return `${field} must equal ${JSON.stringify(expected)} when ${condition.field} is empty`
      }
      for (const field of condition.emptyArrayFields || []) {
        if (!Array.isArray(result[field]) || result[field].length !== 0) return `${field} must be empty when ${condition.field} is empty`
      }
      for (const field of condition.nonEmptyStringFields || []) {
        if (typeof result[field] !== 'string' || !result[field].trim()) return `${field} must be nonempty when ${condition.field} is empty`
      }
      for (const [field, required] of Object.entries(condition.containsFields || {})) {
        if (typeof required !== 'string' || !required.trim()) {
          return `${field} required evidence must be supplied as a nonempty string when ${condition.field} is empty`
        }
        if (typeof result[field] !== 'string' || !result[field].includes(required)) {
          return `${field} must contain the exact required evidence ${JSON.stringify(required)} when ${condition.field} is empty`
        }
      }
    }
    for (const condition of validation.whenArrayNonEmpty || []) {
      if (!Array.isArray(result[condition.field]) || result[condition.field].length === 0) continue
      for (const field of condition.nonEmptyArrayFields || []) {
        if (!Array.isArray(result[field]) || !result[field].length) return `${field} must contain at least one item when ${condition.field} is not empty`
      }
      for (const [field, forbidden] of Object.entries(condition.forbiddenFields || {})) {
        if (result[field] === forbidden) return `${field} must not equal ${JSON.stringify(forbidden)} when ${condition.field} is not empty`
      }
      for (const itemsTrue of condition.itemsAllTrue || []) {
        if (!Array.isArray(result[itemsTrue.field])) return `${itemsTrue.field} must be an array`
        const invalidIndex = result[itemsTrue.field].findIndex((item) => !item || item[itemsTrue.itemField] !== true)
        if (invalidIndex >= 0) {
          return `${itemsTrue.field}[${invalidIndex}].${itemsTrue.itemField} must be exactly true when ${condition.field} is not empty`
        }
      }
    }
    for (const condition of validation.whenFieldEquals || []) {
      if (result[condition.field] !== condition.equals) continue
      for (const field of condition.nonEmptyStringFields || []) {
        if (typeof result[field] !== 'string' || !result[field].trim()) return `${field} must be nonempty when ${condition.field} equals ${JSON.stringify(condition.equals)}`
      }
      for (const field of condition.nonEmptyArrayFields || []) {
        if (!Array.isArray(result[field]) || !result[field].length) return `${field} must contain at least one item when ${condition.field} equals ${JSON.stringify(condition.equals)}`
      }
      for (const field of condition.nonEmptyStringArrayFields || []) {
        if (!Array.isArray(result[field]) || result[field].some((item) => typeof item !== 'string' || !item.trim())) {
          return `${field} must contain only nonempty strings when ${condition.field} equals ${JSON.stringify(condition.equals)}`
        }
      }
      for (const contextArray of condition.contextArrays || []) {
        if (!Array.isArray(contextArray.value)) return `${contextArray.name} must be an array`
        if (contextArray.empty === true && contextArray.value.length) return `${contextArray.name} must be empty`
        if (contextArray.nonEmpty === true && !contextArray.value.length) return `${contextArray.name} must contain at least one item`
        const missingItems = (contextArray.includes || []).filter((item) => !contextArray.value.includes(item))
        if (missingItems.length) return `${contextArray.name} is missing ${JSON.stringify(missingItems)}`
      }
    }
    if (validation.completeWhenTrue && result[validation.completeWhenTrue.field] === true) {
      const condition = validation.completeWhenTrue
      if (result[condition.itemsField].some((item) => item[condition.itemBooleanField] !== true)) {
        return `every ${condition.itemsField} item must have ${condition.itemBooleanField}=true when ${condition.field} is true`
      }
      if (!Array.isArray(result[condition.emptyArrayField]) || result[condition.emptyArrayField].length) {
        return `${condition.emptyArrayField} must be empty when ${condition.field} is true`
      }
    }
    return null
  }

  const resultProblem = (role, result) => {
    if (!result || typeof result !== 'object' || Array.isArray(result)) return 'no structured result was returned'
    const schemaProblem = declaredSchemaProblem(schemaFor(role), result, 'result')
    if (schemaProblem) return schemaProblem
    if (role.resultKind === 'verdict') {
      const problem = verdictProblem(role, result)
      if (problem) return problem
    }
    if (role.autoResolved) {
      const problem = autoResolvedProblem(result)
      if (problem) return problem
    }
    return configuredProblem(role, result)
  }

  if (validationResultProvided) {
    if (roles.length !== 1 || successfulVerdicts !== undefined || priorPanelResults.length || retryRoles !== undefined) {
      throw new Error(`${caller}: validationResult requires exactly one role and no execution, verdict, or retry inputs`)
    }
    const problem = resultProblem(roles[0], validationResult)
    if (problem) throw new Error(`${caller}: ${problem}`)
    return { valid: true }
  }

  const matchResults = (expectedRoles, source, sourceName) => {
    if (!Array.isArray(source) || source.length !== expectedRoles.length) {
      throw new Error(`${caller}: ${sourceName} must contain exactly one result per required role`)
    }
    const used = new Set()
    const matched = expectedRoles.map((role) => {
      const matches = source
        .map((result, index) => ({ result, index }))
        .filter((candidate) => !used.has(candidate.index) && resultProblem(role, candidate.result) === null)
      if (matches.length !== 1) {
        throw new Error(`${caller}: ${sourceName} must contain exactly one valid result for required role ${role.id}`)
      }
      used.add(matches[0].index)
      return matches[0].result
    })
    if (used.size !== source.length) throw new Error(`${caller}: ${sourceName} contains an invalid or unmatched result`)
    return matched
  }

  if (successfulVerdicts !== undefined) {
    if (roles.some((role) => role.resultKind !== 'verdict')) {
      throw new Error(`${caller}: successfulVerdicts can validate only verdict roles`)
    }
    const validatedVerdicts = matchResults(roles, successfulVerdicts, 'successfulVerdicts')
    if (validatedVerdicts.some((verdict) => verdict.verdict !== 'PASS')) {
      throw new Error(`${caller}: successfulVerdicts must contain only PASS results`)
    }
    return { verdicts: validatedVerdicts }
  }

  const normalizedRoleGroups = roleGroups === undefined
    ? [{ id: 'required-roles', roleIds: roles.map((role) => role.id) }]
    : roleGroups
  if (!Array.isArray(normalizedRoleGroups) || !normalizedRoleGroups.length) {
    throw new Error(`${caller}: roleGroups must be a nonempty array when provided`)
  }
  const groupIndexByRole = new Map()
  let currentGroupIndex = -1
  normalizedRoleGroups.forEach((group, index) => {
    if (!group || typeof group.id !== 'string' || !group.id || !Array.isArray(group.roleIds) || !group.roleIds.length) {
      throw new Error(`${caller}: every role group must contain nonempty id and roleIds`)
    }
    if (normalizedRoleGroups.some((candidate, candidateIndex) => candidateIndex !== index && candidate.id === group.id)) {
      throw new Error(`${caller}: duplicate role group ${group.id}`)
    }
    for (const id of group.roleIds) {
      if (typeof id !== 'string' || !id || groupIndexByRole.has(id)) throw new Error(`${caller}: every grouped role id must be nonempty and unique`)
      groupIndexByRole.set(id, index)
    }
    if (group.id === (roleGroup || 'required-roles')) currentGroupIndex = index
  })
  if (currentGroupIndex < 0) throw new Error(`${caller}: roleGroup ${String(roleGroup)} is not declared`)
  const currentRoleIds = normalizedRoleGroups[currentGroupIndex].roleIds
  if (currentRoleIds.length !== roles.length || roles.some((role) => !currentRoleIds.includes(role.id))) {
    throw new Error(`${caller}: roles must exactly match the current roleGroup`)
  }

  let targetGroupIndex = currentGroupIndex
  if (retryRoles !== undefined) {
    if (!retryRoles.length) throw new Error(`${caller}: retryRoles must name at least one failed role`)
    const targetGroups = new Set(retryRoles.map((id) => groupIndexByRole.get(id)))
    if (targetGroups.has(undefined)) {
      const unknown = retryRoles.find((id) => !groupIndexByRole.has(id))
      throw new Error(`${caller}: retryRoles contains unknown role ${String(unknown)}`)
    }
    if (targetGroups.size !== 1) throw new Error(`${caller}: retryRoles must belong to exactly one role group`)
    targetGroupIndex = [...targetGroups][0]
    if (targetGroupIndex < currentGroupIndex) throw new Error(`${caller}: retryRoles target an already completed role group`)
  }

  if (targetGroupIndex > currentGroupIndex) {
    const preservedStageResults = matchResults(roles, priorStageResults, 'priorStageResults')
    return {
      panelComplete: true,
      results: preservedStageResults,
      failedRoles: [],
      priorPanelResults: preservedStageResults,
      retryRoles: [],
      nextGroupInput: { priorPanelResults, retryRoles },
    }
  }

  const requestedIds = retryRoles === undefined ? roles.map((role) => role.id) : [...new Set(retryRoles)]
  for (const id of requestedIds) {
    if (!roleById.has(id)) throw new Error(`${caller}: retryRoles contains unknown role ${String(id)}`)
  }

  const requested = new Set(requestedIds)
  const preservedRoles = roles.filter((role) => !requested.has(role.id))
  const preserved = preservedRoles.length
    ? matchResults(preservedRoles, priorPanelResults, 'priorPanelResults')
    : []

  const targets = requestedIds.map((id) => roleById.get(id))
  const invokeRole = async (role, firstFailure) => {
    const retryBlock = firstFailure
      ? `\n\nRETRY: Your first attempt did not return a valid result for this role. Return the complete result now using the same instructions and context. First attempt: ${JSON.stringify(firstFailure)}`
      : ''
    const options = {
      label: role.label || role.id,
      phase: role.phase || 'Run-required-roles',
      schema: schemaFor(role),
    }
    if (role.model) options.model = role.model
    if (role.effort) options.effort = role.effort
    const verdictOutputInstruction = role.resultKind === 'verdict'
      ? `\n\nCOMMON VERDICT OUTPUT: Return lens="${role.expectedLens}" and verdict PASS or FAIL. Return findings as an empty array for PASS. Return one or more findings for FAIL; every finding contains a nonempty summary, nonempty evidence, and ${role.lieCatcher ? 'an empty fix because the Lie-catcher gives no fix advice' : 'a nonempty fix'}. Return refutationAttempts with at least one actual attempt. Return proofChecked as an array.`
      : ''
    try {
      return await agent(role.prompt + verdictOutputInstruction + retryBlock, options)
    } catch (error) {
      return { executionError: error instanceof Error ? error.message : String(error) }
    }
  }

  const firstResults = await parallel(targets.map((role) => () => invokeRole(role)))
  const firstAttempts = targets.map((role, index) => ({
    role,
    result: firstResults[index],
    problem: resultProblem(role, firstResults[index]),
  }))
  const missing = firstAttempts.filter((attempt) => attempt.problem)
  const secondResults = missing.length
    ? await parallel(missing.map((attempt) => () => invokeRole(attempt.role, {
      result: attempt.result === undefined ? null : attempt.result,
      problem: attempt.problem,
    })))
    : []

  const accepted = firstAttempts.filter((attempt) => !attempt.problem).map((attempt) => attempt.result)
  const failedRoles = []
  missing.forEach((attempt, index) => {
    const secondResult = secondResults[index]
    const secondProblem = resultProblem(attempt.role, secondResult)
    if (!secondProblem) {
      accepted.push(secondResult)
      return
    }
    failedRoles.push({
      role: attempt.role.id,
      attempts: [
        { attempt: 1, result: attempt.result === undefined ? null : attempt.result, problem: attempt.problem },
        { attempt: 2, result: secondResult === undefined ? null : secondResult, problem: secondProblem },
      ],
    })
  })

  const merged = roles.flatMap((role) => {
    const result = [...preserved, ...accepted].find((candidate) => resultProblem(role, candidate) === null)
    return result ? [result] : []
  })

  return {
    panelComplete: failedRoles.length === 0,
    results: merged,
    failedRoles,
    priorPanelResults: merged,
    retryRoles: failedRoles.map((failure) => failure.role),
    nextGroupInput: {},
  }
}

// Every recorded artifact a stage hands another stage is a file path, never content, so one copy exists
// and nothing can be altered in transit. Callers share this check instead of each spelling it out.
function __optionalArtifactPath(caller, field, value) {
  if (value === undefined) return undefined
  if (typeof value !== 'string' || !value.trim()) {
    throw new Error(`${caller}: ${field} must be a nonempty file path when provided`)
  }
  return value
}

// The one line every stage that runs an adversary panel writes when the panel finishes.
function __adversarySummary(verdicts, label) {
  const failed = verdicts.filter((v) => v.verdict === 'FAIL')
  return `${verdicts.length} ${label} ran; ${failed.length} FAIL (${failed.map((v) => v.lens).join(', ') || 'none'})`
}
// ##COPIED-MODULE-END## required-agent-runtime

// ##COPIED-MODULE-BEGIN## stage-result-contracts
// This block is shared code, pasted into every workflow script that needs it. The Workflow
// runtime gives scripts no module import and allows only one level of workflow() nesting,
// so there is no way to call shared code from another file. Do not edit this copy alone:
// every copy of a block name must stay byte-identical, and eng/check-copied-modules.mjs
// fails the moment two copies differ.
async function __stageResultContracts(args) {
  const input = await __workflowInput({ caller: 'stage-result-contracts', value: args })
  const name = input && input.name
  const operation = (input && input.operation) || 'definition'
  const context = (input && input.context) || {}

  if (typeof name !== 'string' || !name || !['definition', 'validate'].includes(operation)) {
    throw new Error('stage-result-contracts requires args { name, operation?: "definition"|"validate", result?, context? }')
  }

  const STRING = { type: 'string' }
  const BOOLEAN = { type: 'boolean' }
  const STRING_ARRAY = { type: 'array', items: STRING }
  const EMPTY_SUCCESS_METADATA = {
    panelComplete: BOOLEAN,
    failedRoles: { type: 'array' },
  }
  const NO_CAPABILITY_LEDGER = 'None — this change introduces no new capability'
  const NO_RULE_PROOF = 'Not applicable — the approved contract adds no rules.'
  const NO_TEST_PROOF = 'Not applicable — the approved contract authorizes no test files.'

  const RULE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      rule: STRING,
      failureModeStopped: STRING,
      analyzer: STRING,
    },
    required: ['rule', 'failureModeStopped'],
  }

  const GROUND_FACT_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      claim: STRING,
      evidence: STRING,
    },
    required: ['claim', 'evidence'],
  }

  const GROUND_FACTS_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      area: STRING,
      facts: { type: 'array', items: GROUND_FACT_SCHEMA },
      surfaces: STRING_ARRAY,
      openQuestions: STRING_ARRAY,
    },
    required: ['area', 'facts', 'surfaces', 'openQuestions'],
  }

  const PRIOR_ART_CANDIDATE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      lens: { type: 'string', enum: ['codegraph', 'grep'] },
      file: STRING,
      line: { type: 'integer' },
      symbol: STRING,
      snippet: STRING,
    },
    required: ['lens', 'file', 'symbol', 'snippet'],
  }

  const PRIOR_ART_CANDIDATES_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      capability: STRING,
      candidates: { type: 'array', items: PRIOR_ART_CANDIDATE_SCHEMA },
      lensesRun: STRING_ARRAY,
      lensesEmpty: STRING_ARRAY,
      lensErrors: STRING_ARRAY,
    },
    required: ['capability', 'candidates', 'lensesRun', 'lensesEmpty', 'lensErrors'],
  }

  const PRIOR_ART_VERDICT_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      capability: STRING,
      decision: { type: 'string', enum: ['reuse', 'extract', 'new'] },
      owner: STRING,
      copies: STRING_ARRAY,
      evidence: STRING,
      confidence: { type: 'string', enum: ['high', 'medium', 'low'] },
    },
    required: ['capability', 'decision', 'evidence', 'confidence'],
  }

  const PRIOR_ART_STAGE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      projectPath: STRING,
      verdicts: { type: 'array', minItems: 1, items: PRIOR_ART_VERDICT_SCHEMA },
      ...EMPTY_SUCCESS_METADATA,
    },
    required: ['projectPath', 'verdicts', 'panelComplete', 'failedRoles'],
  }

  const COMPLETE_GROUND_FACTS_SCHEMA = {
    ...GROUND_FACTS_SCHEMA,
    properties: {
      ...GROUND_FACTS_SCHEMA.properties,
      autoResolved: { type: 'array' },
    },
    required: [...GROUND_FACTS_SCHEMA.required, 'autoResolved'],
  }

  const GROUND_STAGE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      projectPath: STRING,
      facts: { type: 'array', minItems: 1, items: COMPLETE_GROUND_FACTS_SCHEMA },
      ledger: {
        oneOf: [
          { type: 'string', enum: [NO_CAPABILITY_LEDGER] },
          PRIOR_ART_STAGE_SCHEMA,
        ],
      },
      ...EMPTY_SUCCESS_METADATA,
    },
    required: ['projectPath', 'facts', 'ledger', 'panelComplete', 'failedRoles'],
  }

  const DESIGN_APPROACH_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      angle: STRING,
      approach: STRING,
      keySteps: STRING_ARRAY,
      rulesToAdd: { type: 'array', items: RULE_SCHEMA },
      risks: STRING_ARRAY,
    },
    required: ['angle', 'approach', 'keySteps', 'rulesToAdd', 'risks'],
  }

  const DESIGN_VERDICT_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      winningAngle: STRING,
      why: STRING,
      synthesizedApproach: STRING,
      graftedFrom: STRING_ARRAY,
      rulesToAdd: { type: 'array', items: RULE_SCHEMA },
      risks: STRING_ARRAY,
      reuseInstructions: STRING_ARRAY,
    },
    required: ['winningAngle', 'why', 'synthesizedApproach', 'graftedFrom', 'rulesToAdd', 'risks', 'reuseInstructions'],
  }

  const COMPLETE_DESIGN_APPROACH_SCHEMA = {
    ...DESIGN_APPROACH_SCHEMA,
    properties: {
      ...DESIGN_APPROACH_SCHEMA.properties,
      autoResolved: { type: 'array' },
    },
    required: [...DESIGN_APPROACH_SCHEMA.required, 'autoResolved'],
  }

  const COMPLETE_DESIGN_VERDICT_SCHEMA = {
    ...DESIGN_VERDICT_SCHEMA,
    properties: {
      ...DESIGN_VERDICT_SCHEMA.properties,
      autoResolved: { type: 'array' },
    },
    required: [...DESIGN_VERDICT_SCHEMA.required, 'autoResolved'],
  }

  const DESIGN_STAGE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      proposals: { type: 'array', minItems: 1, items: COMPLETE_DESIGN_APPROACH_SCHEMA },
      verdict: COMPLETE_DESIGN_VERDICT_SCHEMA,
      ...EMPTY_SUCCESS_METADATA,
    },
    required: ['proposals', 'verdict', 'panelComplete', 'failedRoles'],
  }

  const HIDDEN_CANDIDATE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      choice: STRING,
      forcedBy: STRING,
      category: STRING,
      evidence: STRING,
    },
    required: ['choice', 'forcedBy', 'category', 'evidence'],
  }

  const HIDDEN_CANDIDATES_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      lensId: STRING,
      candidates: { type: 'array', items: HIDDEN_CANDIDATE_SCHEMA },
    },
    required: ['lensId', 'candidates'],
  }

  const HIDDEN_FINDING_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      choice: STRING,
      whyForced: STRING,
      options: { type: 'array', minItems: 2, maxItems: 4, items: STRING },
      howItBites: STRING,
      recommended: STRING,
      severity: { type: 'string', enum: ['high', 'medium', 'low'] },
    },
    required: ['choice', 'whyForced', 'options', 'howItBites', 'recommended', 'severity'],
  }

  const HIDDEN_FINDINGS_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      findings: { type: 'array', items: HIDDEN_FINDING_SCHEMA },
      droppedAsNoise: { type: 'integer', minimum: 0 },
    },
    required: ['findings', 'droppedAsNoise'],
  }

  const COMPLETE_HIDDEN_FINDINGS_SCHEMA = {
    ...HIDDEN_FINDINGS_SCHEMA,
    properties: {
      ...HIDDEN_FINDINGS_SCHEMA.properties,
      autoResolved: { type: 'array' },
    },
    required: [...HIDDEN_FINDINGS_SCHEMA.required, 'autoResolved'],
  }

  const HIDDEN_STAGE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      contractPath: STRING,
      ...COMPLETE_HIDDEN_FINDINGS_SCHEMA.properties,
      ...EMPTY_SUCCESS_METADATA,
    },
    required: ['contractPath', ...COMPLETE_HIDDEN_FINDINGS_SCHEMA.required, 'panelComplete', 'failedRoles'],
  }

  const RULE_PROOF_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      rule: STRING,
      diagnosticId: STRING,
      violatingFixture: STRING,
      violatingProof: STRING,
      compliantFixture: STRING,
      compliantProof: STRING,
    },
    required: ['rule', 'diagnosticId', 'violatingFixture', 'violatingProof', 'compliantFixture', 'compliantProof'],
  }

  const RULE_PHASE_AUTHOR_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      ruleFiles: STRING_ARRAY,
      buildProof: STRING,
      perRule: { type: 'array', items: RULE_PROOF_SCHEMA },
      notes: STRING,
    },
    required: ['ruleFiles', 'buildProof', 'perRule'],
  }

  const RULE_PHASE_STAGE_SCHEMA = {
    ...RULE_PHASE_AUTHOR_SCHEMA,
    properties: {
      ...RULE_PHASE_AUTHOR_SCHEMA.properties,
      verdicts: { type: 'array' },
      anyFail: BOOLEAN,
      ...EMPTY_SUCCESS_METADATA,
    },
    required: [...RULE_PHASE_AUTHOR_SCHEMA.required, 'verdicts', 'anyFail', 'panelComplete', 'failedRoles'],
  }

  const TDD_COVERAGE_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      acceptanceItem: STRING,
      tests: STRING_ARRAY,
      kind: STRING,
      red: BOOLEAN,
    },
    required: ['acceptanceItem', 'tests', 'kind', 'red'],
  }

  const ARCHITECTURE_AUTHOR_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      skeletonFiles: STRING_ARRAY,
      signatureDiff: STRING,
      buildProof: STRING,
      perType: {
        type: 'array',
        items: {
          type: 'object',
          additionalProperties: false,
          properties: { type: STRING, role: STRING, bodies: STRING },
          required: ['type', 'role', 'bodies'],
        },
      },
      notes: STRING,
    },
    required: ['skeletonFiles', 'signatureDiff', 'buildProof', 'perType', 'notes'],
  }

  const ARCHITECTURE_STAGE_SCHEMA = {
    ...ARCHITECTURE_AUTHOR_SCHEMA,
    properties: {
      ...ARCHITECTURE_AUTHOR_SCHEMA.properties,
      verdicts: { type: 'array' },
      anyFail: BOOLEAN,
      ...EMPTY_SUCCESS_METADATA,
    },
    required: [...ARCHITECTURE_AUTHOR_SCHEMA.required, 'verdicts', 'anyFail', 'panelComplete', 'failedRoles'],
  }

  const TDD_AUTHOR_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    properties: {
      testFiles: STRING_ARRAY,
      redProof: STRING,
      coverage: { type: 'array', items: TDD_COVERAGE_SCHEMA },
      notes: STRING,
    },
    required: ['testFiles', 'redProof', 'coverage', 'notes'],
  }

  const TDD_STAGE_SCHEMA = {
    ...TDD_AUTHOR_SCHEMA,
    properties: {
      ...TDD_AUTHOR_SCHEMA.properties,
      verdicts: { type: 'array' },
      anyFail: BOOLEAN,
      ...EMPTY_SUCCESS_METADATA,
    },
    required: [...TDD_AUTHOR_SCHEMA.required, 'verdicts', 'anyFail', 'panelComplete', 'failedRoles'],
  }

  const definitionFor = (contractName, contractContext = {}) => {
    switch (contractName) {
      case 'ground-facts':
        return {
          schema: GROUND_FACTS_SCHEMA,
          autoResolved: true,
          validation: {
            ...(contractContext.expectedArea === undefined ? {} : { expectedFields: { area: contractContext.expectedArea } }),
            nonEmptyStringFields: ['area'],
            itemNonEmptyStringFields: { facts: ['claim', 'evidence'] },
            stringArrayFields: ['surfaces', 'openQuestions'],
          },
          values: { noCapabilityLedger: NO_CAPABILITY_LEDGER },
        }
      case 'prior-art-candidates':
        return {
          schema: PRIOR_ART_CANDIDATES_SCHEMA,
          validation: {
            ...(contractContext.expectedCapability === undefined ? {} : { expectedFields: { capability: contractContext.expectedCapability } }),
            emptyArrayFields: ['lensErrors'],
            arrayIncludes: { lensesRun: ['codegraph', 'grep'] },
            itemNonEmptyStringFields: { candidates: ['lens', 'file', 'symbol', 'snippet'] },
            stringArrayFields: ['lensesRun', 'lensesEmpty', 'lensErrors'],
          },
        }
      case 'prior-art-verdict':
        return {
          schema: PRIOR_ART_VERDICT_SCHEMA,
          validation: {
            ...(contractContext.expectedCapability === undefined ? {} : { expectedFields: { capability: contractContext.expectedCapability } }),
            nonEmptyStringFields: ['capability', 'evidence'],
            whenFieldEquals: [
              { field: 'decision', equals: 'reuse', nonEmptyStringFields: ['owner'] },
              { field: 'decision', equals: 'extract', nonEmptyArrayFields: ['copies'], nonEmptyStringArrayFields: ['copies'] },
              ...(contractContext.searchResult === undefined
                ? []
                : [
                    { field: 'decision', equals: 'reuse', contextArrays: [{ name: 'reuse search candidates', value: contractContext.searchResult.candidates, nonEmpty: true }] },
                    { field: 'decision', equals: 'extract', contextArrays: [{ name: 'extract search candidates', value: contractContext.searchResult.candidates, nonEmpty: true }] },
                    {
                      field: 'decision',
                      equals: 'new',
                      contextArrays: [
                        { name: 'new-ruling search candidates', value: contractContext.searchResult.candidates, empty: true },
                        { name: 'new-ruling empty lenses', value: contractContext.searchResult.lensesEmpty, includes: ['codegraph', 'grep'] },
                      ],
                    },
                  ]),
            ],
          },
        }
      case 'design-proposal':
        return {
          schema: DESIGN_APPROACH_SCHEMA,
          autoResolved: true,
          validation: {
            ...(contractContext.expectedAngle === undefined ? {} : { expectedFields: { angle: contractContext.expectedAngle } }),
            nonEmptyArrayFields: ['keySteps'],
            nonEmptyStringFields: ['angle', 'approach'],
            stringArrayFields: ['keySteps', 'risks'],
            itemNonEmptyStringFields: { rulesToAdd: ['rule', 'failureModeStopped'] },
          },
        }
      case 'design-verdict':
        return {
          schema: DESIGN_VERDICT_SCHEMA,
          autoResolved: true,
          validation: {
            nonEmptyStringFields: ['winningAngle', 'why', 'synthesizedApproach'],
            ...(contractContext.allowedAngles === undefined ? {} : { enumFields: { winningAngle: contractContext.allowedAngles } }),
            stringArrayFields: ['graftedFrom', 'risks', 'reuseInstructions'],
            itemNonEmptyStringFields: { rulesToAdd: ['rule', 'failureModeStopped'] },
          },
        }
      case 'hidden-candidates':
        return {
          schema: HIDDEN_CANDIDATES_SCHEMA,
          validation: {
            ...(contractContext.expectedLens === undefined ? {} : { expectedFields: { lensId: contractContext.expectedLens } }),
            nonEmptyStringFields: ['lensId'],
            itemNonEmptyStringFields: { candidates: ['choice', 'forcedBy', 'category', 'evidence'] },
          },
        }
      case 'hidden-findings':
        return {
          schema: HIDDEN_FINDINGS_SCHEMA,
          autoResolved: true,
          validation: {
            itemNonEmptyStringFields: { findings: ['choice', 'whyForced', 'howItBites', 'recommended'] },
            itemNonEmptyArrayFields: { findings: ['options'] },
            itemStringArrayFields: { findings: ['options'] },
          },
        }
      case 'rule-phase-author': {
        const approvedRuleNames = contractContext.approvedRuleNames
        const noRules = Array.isArray(approvedRuleNames) && approvedRuleNames.length === 0
        if (noRules && (typeof contractContext.noRuleEvidence !== 'string' || !contractContext.noRuleEvidence.trim())) {
          throw new Error('stage-result-contracts: rule-phase-author requires context.noRuleEvidence, the exact contract sentence establishing that no rules were approved')
        }
        return {
          schema: RULE_PHASE_AUTHOR_SCHEMA,
          validation: {
            itemNonEmptyStringFields: {
              perRule: ['rule', 'diagnosticId', 'violatingFixture', 'violatingProof', 'compliantFixture', 'compliantProof'],
            },
            stringArrayFields: ['ruleFiles'],
            // The no-rule sentinel is valid ONLY on the empty result. A nonempty ruleFiles or perRule means rules were
            // installed, so the sentinel would be a forged no-op proof.
            whenArrayNonEmpty: [
              { field: 'ruleFiles', forbiddenFields: { buildProof: NO_RULE_PROOF } },
              { field: 'perRule', forbiddenFields: { buildProof: NO_RULE_PROOF } },
            ],
            ...(noRules
              ? {
                  expectedFields: { buildProof: NO_RULE_PROOF },
                  emptyArrayFields: ['ruleFiles', 'perRule'],
                  nonEmptyStringFields: ['notes'],
                  containsFields: { notes: contractContext.noRuleEvidence },
                }
              : {
                  nonEmptyArrayFields: ['ruleFiles', 'perRule'],
                  nonEmptyStringFields: ['buildProof'],
                  ...(Array.isArray(approvedRuleNames)
                    ? { exactItemSets: [{ field: 'perRule', itemField: 'rule', expectedItems: approvedRuleNames }] }
                    : {}),
                }),
          },
          values: { noRuleProof: NO_RULE_PROOF },
        }
      }
      case 'architecture-author':
        return {
          schema: ARCHITECTURE_AUTHOR_SCHEMA,
          validation: {
            nonEmptyStringFields: ['signatureDiff', 'buildProof', 'notes'],
            itemNonEmptyStringFields: { perType: ['type', 'role', 'bodies'] },
            // The approved no-signature result: when the contract changes no signature there are no skeleton
            // files, so there is no per-type row either. Both arrays empty together, never one without the other.
            whenArrayEmpty: [{ field: 'skeletonFiles', emptyArrayFields: ['perType'] }],
          },
        }
      case 'architecture-stage':
        return {
          schema: ARCHITECTURE_STAGE_SCHEMA,
          validation: {
            expectedFields: { anyFail: false, panelComplete: true },
            emptyArrayFields: ['failedRoles'],
          },
        }
      case 'tdd-author': {
        const noTestEvidence = contractContext.noTestEvidence
        return {
          schema: TDD_AUTHOR_SCHEMA,
          validation: {
            nonEmptyStringFields: ['redProof', 'notes'],
            stringArrayFields: ['testFiles'],
            itemNonEmptyStringFields: { coverage: ['acceptanceItem', 'kind'] },
            itemNonEmptyArrayFields: { coverage: ['tests'] },
            itemStringArrayFields: { coverage: ['tests'] },
            whenArrayEmpty: [{
              field: 'testFiles',
              exactFields: { redProof: NO_TEST_PROOF },
              emptyArrayFields: ['coverage'],
              // The approved no-test result is contract-backed: notes must carry the exact contract evidence, not any
              // nonblank text. The caller supplies the excerpt; a missing excerpt fails the empty result here, and the
              // check never runs when testFiles is nonempty.
              containsFields: { notes: noTestEvidence },
            }],
            // The no-test sentinel is valid ONLY on the empty result, and a real test result proves RED on every
            // covered acceptance item.
            whenArrayNonEmpty: [{
              field: 'testFiles',
              nonEmptyArrayFields: ['coverage'],
              forbiddenFields: { redProof: NO_TEST_PROOF },
              itemsAllTrue: [{ field: 'coverage', itemField: 'red' }],
            }],
          },
          values: { noTestProof: NO_TEST_PROOF },
        }
      }
      case 'ground-stage':
        return {
          schema: GROUND_STAGE_SCHEMA,
          validation: {
            expectedFields: { panelComplete: true },
            emptyArrayFields: ['failedRoles'],
            nonEmptyArrayFields: ['facts'],
            nonEmptyStringFields: ['projectPath'],
          },
        }
      case 'prior-art-stage':
        return {
          schema: PRIOR_ART_STAGE_SCHEMA,
          validation: {
            expectedFields: { panelComplete: true },
            emptyArrayFields: ['failedRoles'],
            nonEmptyArrayFields: ['verdicts'],
            nonEmptyStringFields: ['projectPath'],
          },
        }
      case 'design-stage':
        return {
          schema: DESIGN_STAGE_SCHEMA,
          validation: {
            expectedFields: { panelComplete: true },
            emptyArrayFields: ['failedRoles'],
            nonEmptyArrayFields: ['proposals'],
          },
        }
      case 'hidden-decision-stage':
        return {
          schema: HIDDEN_STAGE_SCHEMA,
          validation: {
            expectedFields: { panelComplete: true },
            emptyArrayFields: ['failedRoles'],
            nonEmptyStringFields: ['contractPath'],
          },
        }
      case 'rule-phase-stage':
        return {
          schema: RULE_PHASE_STAGE_SCHEMA,
          validation: {
            expectedFields: { anyFail: false, panelComplete: true },
            emptyArrayFields: ['failedRoles'],
          },
        }
      case 'tdd-stage':
        return {
          schema: TDD_STAGE_SCHEMA,
          validation: {
            expectedFields: { anyFail: false, panelComplete: true },
            emptyArrayFields: ['failedRoles'],
          },
        }
      default:
        throw new Error(`stage-result-contracts: unknown result contract ${name}`)
    }
  }

  const validateWith = async (contractName, result, contractContext = {}) => {
    const definition = definitionFor(contractName, contractContext)
    await __requiredAgentRuntime({
      caller: `stage-result-contracts:${contractName}`,
      roles: [{ id: contractName, ...definition }],
      validationResult: result,
    })
  }

  const validateSuccessfulVerdicts = async (contractName, verdicts, lenses) => {
    await __requiredAgentRuntime({
      caller: `stage-result-contracts:${contractName}:verdicts`,
      roles: lenses.map((lens) => ({
        id: `${contractName}:${lens}`,
        resultKind: 'verdict',
        expectedLens: lens,
        lieCatcher: lens === 'lie-catcher',
      })),
      successfulVerdicts: verdicts,
    })
  }

  const validateCompleteStage = async (contractName, result, contractContext) => {
    await validateWith(contractName, result, contractContext)

    if (contractName === 'ground-stage') {
      for (const fact of result.facts) await validateWith('ground-facts', fact)
      if (typeof result.ledger !== 'string') {
        await validateWith('prior-art-stage', result.ledger)
        for (const verdict of result.ledger.verdicts) await validateWith('prior-art-verdict', verdict)
      }
    }

    if (contractName === 'prior-art-stage') {
      for (const verdict of result.verdicts) await validateWith('prior-art-verdict', verdict)
    }

    if (contractName === 'design-stage') {
      for (const proposal of result.proposals) await validateWith('design-proposal', proposal)
      await validateWith('design-verdict', result.verdict, { allowedAngles: result.proposals.map((proposal) => proposal.angle) })
    }

    if (contractName === 'hidden-decision-stage') {
      const findingsResult = {
        findings: result.findings,
        droppedAsNoise: result.droppedAsNoise,
        autoResolved: result.autoResolved,
      }
      await validateWith('hidden-findings', findingsResult)
    }

    if (contractName === 'rule-phase-stage') {
      // The complete approved rule-name set is what makes the exact-set check possible. Without it an incomplete or
      // extra rule proof validates, so every downstream validator must supply it rather than fall back to a guess.
      if (!Array.isArray(contractContext.approvedRuleNames)) {
        throw new Error('stage-result-contracts: rule-phase-stage requires context.approvedRuleNames, the complete approved rule-name set from the contract')
      }
      const authorResult = {
        ruleFiles: result.ruleFiles,
        buildProof: result.buildProof,
        perRule: result.perRule,
        ...(result.notes === undefined ? {} : { notes: result.notes }),
      }
      await validateWith('rule-phase-author', authorResult, contractContext)
      await validateSuccessfulVerdicts(contractName, result.verdicts, ['solid', 'dry', 'lie-catcher'])
    }

    if (contractName === 'tdd-stage') {
      await validateWith('tdd-author', {
        testFiles: result.testFiles,
        redProof: result.redProof,
        coverage: result.coverage,
        notes: result.notes,
      }, contractContext)
      await validateSuccessfulVerdicts(contractName, result.verdicts, ['test-quality', 'dry', 'lie-catcher'])
    }
  }

  if (operation === 'definition') return definitionFor(name, context)
  if (!Object.prototype.hasOwnProperty.call(input, 'result')) throw new Error('stage-result-contracts: validate requires result')

  await validateCompleteStage(name, input.result, context)
  return { valid: true }
}
// ##COPIED-MODULE-END## stage-result-contracts

// ##COPIED-MODULE-BEGIN## scope-boundary
// This block is shared code, pasted into every workflow script that needs it. The Workflow
// runtime gives scripts no module import and allows only one level of workflow() nesting,
// so there is no way to call shared code from another file. Do not edit this copy alone:
// every copy of a block name must stay byte-identical, and eng/check-copied-modules.mjs
// fails the moment two copies differ.
//
// The one owner of the scope-boundary sentence every fix-round agent is handed. Scope changes
// only when the human edits the contract, so every stage states that boundary in the same words.
const __SCOPE_BOUNDARY = 'A fix that changes the contract or an approved design is allowed only after the human edits the contract. There is no second path.'
// ##COPIED-MODULE-END## scope-boundary


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
        required: ['item', 'met', 'evidence'],
      },
    },
    walls: { type: 'array', items: { type: 'string' } }, // anything it stopped on instead of deviating
    complete: { type: 'boolean' },                        // every acceptance item met, build+test green
  },
  required: ['changedFiles', 'buildProof', 'testProof', 'acceptance', 'walls', 'complete'],
}

const input = await __workflowInput({ caller: 'implement', value: args })

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath
const level = (input && input.level) || 'L1'
// The RULE-PHASE and TDD results are handed over as file paths, never as content, like every other
// artifact one stage hands another. The worker opens each file itself.
const rulePhaseResultPath = __optionalArtifactPath('implement', 'rulePhaseResultPath', input && input.rulePhaseResultPath)
const tddResultPath = __optionalArtifactPath('implement', 'tddResultPath', input && input.tddResultPath)
const omittedStageAuthorizations = (input && input.omittedStageAuthorizations) || {}
const instruction = input && input.instruction
const priorPanelResults = input && input.priorPanelResults !== undefined ? input.priorPanelResults : []
const retryRoles = input && input.retryRoles

if (!projectPath || !contractPath || !['L1', 'L2'].includes(level)) {
  throw new Error(
    'implement requires args { projectPath, contractPath, level?: "L1"|"L2", rulePhaseResultPath?, tddResultPath?, omittedStageAuthorizations?: { rulePhase } } (got type: ' + typeof args + ')')
}
if (!omittedStageAuthorizations || typeof omittedStageAuthorizations !== 'object' || Array.isArray(omittedStageAuthorizations)) {
  throw new Error('implement: omittedStageAuthorizations must be an object when provided')
}
const unknownOmissions = Object.keys(omittedStageAuthorizations).filter((stage) => stage !== 'rulePhase')
if (unknownOmissions.length) throw new Error(`implement: unknown omitted-stage authorization ${unknownOmissions[0]}`)
for (const [stage, authorization] of Object.entries(omittedStageAuthorizations)) {
  if (typeof authorization !== 'string' || !authorization.trim()) throw new Error(`implement: omittedStageAuthorizations.${stage} must contain Tim's nonempty recorded authorization`)
}

const hasRulePhaseResult = rulePhaseResultPath !== undefined
const hasTddResult = tddResultPath !== undefined
if (level === 'L2' && hasRulePhaseResult) throw new Error('implement: RULE-PHASE is not present at L2')
if (level === 'L1' && !hasRulePhaseResult && (typeof omittedStageAuthorizations.rulePhase !== 'string' || !omittedStageAuthorizations.rulePhase.trim())) {
  throw new Error('implement: L1 requires rulePhaseResultPath or Tim\'s recorded RULE-PHASE omission authorization')
}
if (level === 'L1' && !hasTddResult) {
  throw new Error('implement: L1 requires tddResultPath, including the approved no-test TDD result when the contract authorizes no test files')
}

const omittedStagePrompt = (stage, authorization) => `${stage} OMITTED UNDER TIM'S RECORDED AUTHORIZATION: ${authorization}\nBefore proceeding, verify that this exact authorization appears word for word in the contract. Do not invent a stage result or RED.`
const rulePhaseContext = hasRulePhaseResult
  ? `RULE-PHASE RESULT — a file path; open and read it yourself: ${rulePhaseResultPath}\nIt must prove every rule the contract approved, with no rule missing and none added, and its buildProof must be real. The exact no-rule marker is valid only on an empty result. STOP and escalate if it does not hold; do not start work on a prior stage's false result.`
  : level === 'L1'
    ? omittedStagePrompt('RULE-PHASE', omittedStageAuthorizations.rulePhase)
    : 'RULE-PHASE: Not present at L2. Do not invent a RULE-PHASE result or analyzer RED.'
const tddContext = hasTddResult
  ? `TDD RESULT — a file path; open and read it yourself: ${tddResultPath}\nEvery coverage entry must be red, and the exact no-test marker is valid only on an empty result with the contract evidence that authorizes no tests. STOP and escalate if it does not hold; do not start work on a prior stage's false result.`
  : 'TDD: This L2 run did not need TDD. Do not invent a TDD result or test RED.'

const instructionBlock = instruction
  ? `ADDITIONAL DIRECTION FROM THE ORCHESTRATOR:\n${typeof instruction === 'string' ? instruction : JSON.stringify(instruction, null, 2)}\nThe direction may clarify work already inside the contract; it relaxes no rule or acceptance check. ${__SCOPE_BOUNDARY} STOP if the direction expands scope without that edit.\n\n`
  : ''

const workerPrompt = `${instructionBlock}You are the IMPLEMENT worker. You are a FRESH agent, different from whoever wrote the rules, the skeleton (ARCHITECTURE), and the tests (TDD) — separation of powers. You implement the behavior, and only the behavior. Do NOT commit, do NOT push, do NOT migrate or prepare any target.

Read .agents/skills/rails-run-a-workflow/SKILL.md for IMPLEMENT authority, scope, proof, and stop rules. Read .agents/skills/rails-solid-code/SKILL.md, .agents/skills/rails-dry-code/SKILL.md, .agents/skills/rails-real-work/SKILL.md, and .agents/skills/rails-test-code/SKILL.md; those files own the reusable implementation and test criteria. Read .agents/skills/rails-decisions/SKILL.md — it owns the decision boundary, so you know which choices are never yours to make and must be escalated. Read .agents/skills/rails-explorer/SKILL.md — it owns deriving truth from the live working tree and enumerating every store a value lives in.

PROJECT PATH: ${projectPath}
CONTRACT: ${contractPath} — Read it in FULL. Implement exactly the authorized work, keep every write inside the authorized surfaces, obey every boundary, and prove every acceptance criterion.
LEVEL: ${level}
${rulePhaseContext}
${tddContext}

WHAT YOU DO — WRITE THE BEHAVIOR, TURN RED TO GREEN:
- The opening green build+test bracket ran before any mutating stage changed the working tree. Do not call the current run a baseline. Read the complete contract and the supplied results from stages that ran before deciding which RED exists. When RULE-PHASE added rules, run the relevant build to observe and clear their production impact. When the supplied RULE-PHASE result contains no rules, do not claim or invent analyzer RED. When TDD added acceptance tests, run them to observe and clear their expected RED. When the supplied TDD result contains no tests, do not claim or invent test RED. When the selected Level omitted a stage, do not manufacture that stage's result or RED.
- The ARCHITECTURE stage may have written interfaces and new concrete types whose new bodies throw \`NotImplementedException()\`, or it may have returned the approved no-signature result. Fill only the approved behavior that exists. Clear every real rule and test failure the contract assigns to IMPLEMENT without touching the rules, interfaces, or tests. Never call a red "pre-existing" and never footnote it.
- The working tree contains the outputs from the stages that ran, including explicit empty outputs where approved. Do not revert them or any unrelated work.

YOUR AUTHORITY IS BOXED IN ON EVERY SIDE:
- Rule-writing authority REVOKED: you may NOT add, edit, or suppress any analyzer; \`analyzers/**\` is off-limits, and so is anything the contract marks frozen. If a rule blocks you, STOP and escalate — never suppress it, \`NoWarn\` it, lower its severity, or edit its definition.
- Interface authority REVOKED: you may NOT change any interface or skeleton signature the ARCHITECTURE stage set (human-signed-off and frozen). If you need a seam the skeleton does not expose, STOP and escalate — never widen an interface to make your code fit.
- Test authority REVOKED: you may NOT weaken, delete, skip, xfail, or re-point any test the TDD stage wrote. A red test is made green by growing the code, never by softening the test.
- Do ONLY this contract's unit of work. The selected Level controls which stages run; the selected Level never expands the contract's scope.

PROVE (paste verbatim, with exit codes): \`dotnet build -c Release\` is 0 warnings / 0 errors, and \`dotnet test -c Release\` is 0 failed. When TDD added acceptance tests, show that they pass against the implementation with none skipped or weakened. When TDD returned the approved no-test result, do not claim new test coverage. Also run each of the contract's own acceptance spot checks. Cropped or exit-code-missing output does not count.

FINAL ANSWER: changedFiles (every file created/edited/deleted), buildProof and testProof (pasted with exit codes), acceptance (each contract acceptance item with met true/false and its evidence), walls (anything you stopped on), and complete (true only if every acceptance item is met and the build and tests are green under the rules).`

const panel = await __requiredAgentRuntime({
  caller: 'implement',
  roles: [{
    id: 'implement',
    prompt: workerPrompt,
    label: 'implement',
    phase: 'Implement',
    schema: RESULT_SCHEMA,
    validation: {
      nonEmptyArrayFields: ['acceptance'],
      nonEmptyStringFields: ['buildProof', 'testProof'],
      itemNonEmptyStringFields: { acceptance: ['item', 'evidence'] },
      completeWhenTrue: {
        field: 'complete',
        itemsField: 'acceptance',
        itemBooleanField: 'met',
        emptyArrayField: 'walls',
      },
    },
  }],
  priorPanelResults,
  retryRoles,
})

if (!panel.panelComplete) {
  return {
    changedFiles: [],
    buildProof: '',
    testProof: '',
    acceptance: [],
    walls: [],
    complete: false,
    panelComplete: false,
    failedRoles: panel.failedRoles,
    priorPanelResults: panel.priorPanelResults,
    retryRoles: panel.retryRoles,
  }
}

return panel.results[0]
