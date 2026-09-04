export const meta = {
  name: 'preflight-contract',
  description: 'Contract preflight: one agent rules whether a contract is sound BEFORE any work starts. It reads the contract against the rails that govern it, against itself, and against the tree as it stands, and returns sound, or the exact defects. It exists because contract defects were repeatedly found by an adversary panel an hour after a worker had already built against them. Everything it checks is true or false without any work having happened; what the work did is refute-readiness.js, later.',
  phases: [
    { title: 'Preflight', detail: 'one agent rules the contract before any work starts', model: 'opus' },
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

const input = await __workflowInput({ caller: 'preflight-contract', value: args })

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath
const priorPanelResults = input && input.priorPanelResults !== undefined ? input.priorPanelResults : []
const retryRoles = input && input.retryRoles

if (!projectPath || !contractPath) {
  throw new Error(
    'preflight-contract requires args { projectPath, contractPath } (got type: ' + typeof args + ')')
}

const PREFLIGHT_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  properties: {
    sound: { type: 'boolean' },
    defects: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        properties: {
          kind: { type: 'string' },        // shape | decision-form | internal | ledger | tree-fact | file-pair
          contractSays: { type: 'string' },
          ruleSays: { type: 'string' },
          fix: { type: 'string' },
        },
        required: ['kind', 'contractSays', 'ruleSays', 'fix'],
      },
    },
    checksRun: { type: 'array', items: { type: 'string' } },
  },
  required: ['sound', 'defects', 'checksRun'],
}

const preflightPrompt = `You are the CONTRACT PREFLIGHT. Do NOT make code changes. Do NOT stage, commit, or push. Do NOT do any of the work the contract describes.

Your only job: rule whether this contract is sound before anyone builds against it, or name exactly what is wrong. You exist because contract defects were found by an adversary panel an hour after a worker had already built against them, and every one of those rounds was wasted. Everything you check is true or false right now, without any work having happened.

PROJECT PATH: ${projectPath}
CONTRACT: ${contractPath} — read it in full.

Read these first; they own the rules you rule against, and you read them live rather than relying on any list in this prompt:
- .agents/skills/rails-write-a-contract/SKILL.md — the sole definition of the contract's sections, their order, their contents, and the voice.
- .agents/skills/rails-decisions/SKILL.md — what a decision is, what counts as approval, and every decision Violation.
- .agents/skills/rails-dry-code/SKILL.md — the prior-art criteria the reuse ledger must satisfy.

Run all six checks and report every check you ran, including the ones that passed.

1. SHAPE. Compare the contract's sections against the section list in rails-write-a-contract: every mandated section present, named exactly as the rail names it, in the rail's order, with no section the rail does not define. Confirm the Scope section carries the exact rule the rail mandates, word for word. Confirm the Success definition carries the human's standing definition. Confirm the Level section names L1 or L2. A departure is a shape defect.

2. DECISION FORM. Rule every decision in the Decisions section against rails-decisions. A decision that reports what a file says rather than stating the wording the human approved is a defect — that is a paraphrase, and the rail forbids it. So is a decision that reads as an intention, a plan, an open question, or an item still to be settled. So is a decision out of numbering order. You cannot know what the human actually approved, so do not guess; rule on the FORM, and say plainly which decisions you could not rule on.

3. INTERNAL. Rule the contract against itself. Any count it states about its own contents must match — "the nine decisions" when the Decisions section holds fourteen is a defect. Any sentence describing progress — what is or is not yet applied, what is still to come — is a defect, because it goes stale the moment work lands and the contract is a spec, not a status. Every numbered work item must trace to a decision, and every decision must be reachable from a work item, an acceptance check, or an explicit statement that it is already in force. Any section that bounds the work by the Surfaces list is a defect: Surfaces documents where the work is and never limits it.

4. LEDGER. Rule the Reuse ledger against rails-dry-code. It must either record the exact no-capability line the rail specifies, or rule every capability reuse, extract or new with the evidence the rail demands, from every lens the rail demands, produced by the tool the rail names. A ledger that records no new capability while a work item builds a shared owner, a helper, a module, or a new script contradicts the work and is a defect.

5. TREE FACT. Every factual claim the contract makes about the tree as it stands right now must be true. Every path it names must exist. Every count it states about the tree must match what is actually there — count it yourself. Every code identifier an Acceptance check names must exist in the tree with that exact spelling, unless the same contract lists it as a deliverable. Verify each one; do not take the contract's word.

6. FILE PAIR. Where the contract governs a rule that is written in more than one place — a rail and the script that enforces it, two rails stating the same rule, a script stating a rule twice — read both and confirm they agree today. A pair that already disagrees before any work starts is a defect the work will inherit.

Search properly. A reference built at runtime from a variable — a template literal, a concatenation, a value from a table — is invisible to a literal text search, so a plain search returning nothing is NOT proof of absence. Read the file. Use CodeGraph and grep across src, tests, analyzers, and eng, then read the code that matters.

FINAL ANSWER: sound (true only when defects is empty), defects (each with kind = shape, decision-form, internal, ledger, tree-fact, or file-pair; what the contract says; what the governing rule or the tree says; and the fix), and checksRun naming all six checks and what each one covered. Returning sound with a defect present is itself the failure this stage exists to prevent.`

const panel = await __requiredAgentRuntime({
  caller: 'preflight-contract',
  roles: [{
    id: 'preflight',
    prompt: preflightPrompt,
    label: 'preflight',
    phase: 'Preflight',
    model: 'opus',
    schema: PREFLIGHT_SCHEMA,
    validation: {
      itemNonEmptyStringFields: { defects: ['kind', 'contractSays', 'ruleSays', 'fix'] },
      nonEmptyArrayFields: ['checksRun'],
      whenArrayNonEmpty: [{ field: 'defects', forbiddenFields: { sound: true } }],
    },
  }],
  priorPanelResults,
  retryRoles,
})

if (panel.panelComplete === false) return panel

const result = panel.results[0]
log(result.sound
  ? `SOUND — ${result.checksRun.length} checks run, no defect`
  : `NOT SOUND — ${result.defects.length} defect(s): ${result.defects.map((d) => d.kind).join(', ')}`)

return { contractPath, ...result, panelComplete: true, failedRoles: [] }
