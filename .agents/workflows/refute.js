export const meta = {
  name: 'refute',
  description: 'REFUTE stage fan-out: the Level selects the required independent adversaries. DRY runs at every Level; L1 runs all five, L2 runs Prove-It, DRY, and Lie-catcher with optional Laziness-auditor, and L3 runs DRY with an optional human-requested Lie-catcher. Produces one verdict per required lens; any FAIL blocks the gate.',
  phases: [
    { title: 'Refute', detail: 'each Level-selected adversary refutes against the governing requirement and its rail', model: 'sonnet' },
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
      goal: STRING,
      proposals: { type: 'array', minItems: 1, items: COMPLETE_DESIGN_APPROACH_SCHEMA },
      verdict: COMPLETE_DESIGN_VERDICT_SCHEMA,
      ...EMPTY_SUCCESS_METADATA,
    },
    required: ['goal', 'proposals', 'verdict', 'panelComplete', 'failedRoles'],
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
            nonEmptyStringFields: ['goal'],
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


const input = await __workflowInput({ caller: 'refute', value: args })

const projectPath = input && input.projectPath
const contractPath = input && input.contractPath
const humanRequest = input && input.humanRequest
const changedFiles = input && input.changedFiles
const stageOutputPaths = (input && input.stageOutputPaths) || {}
const level = (input && input.level) || 'L1'
const additionalAdversaries = input && input.additionalAdversaries !== undefined ? input.additionalAdversaries : []
const priorPanelResults = input && input.priorPanelResults !== undefined ? input.priorPanelResults : []
const retryRoles = input && input.retryRoles

if (!projectPath || !['L1', 'L2', 'L3'].includes(level)) {
  throw new Error(
    'refute requires args { projectPath, level?: "L1"|"L2"|"L3", ... } (got type: ' + typeof args + ')')
}

if (!Array.isArray(additionalAdversaries) || additionalAdversaries.some((id) => !['laziness', 'lie-catcher'].includes(id))) {
  throw new Error('refute: additionalAdversaries must contain only laziness or lie-catcher')
}

if ((level === 'L1' || level === 'L2') && !contractPath) {
  throw new Error(`refute: ${level} requires contractPath`)
}

if (level === 'L3' && (typeof humanRequest !== 'string' || !humanRequest.trim())) {
  throw new Error('refute: L3 requires humanRequest containing Tim\'s exact request and L3 approval')
}

if (!Array.isArray(changedFiles)) {
  throw new Error('refute: changedFiles must be an array at every Level')
}

if (!stageOutputPaths || typeof stageOutputPaths !== 'object' || Array.isArray(stageOutputPaths)) {
  throw new Error('refute: stageOutputPaths must be an object when provided')
}

// Prior-stage results are handed over as file paths, never as content. One copy exists — the file on
// disk — so nothing can disagree with anything and nothing can be altered on the way here. Scripts have
// no filesystem, so the path is checked here and every adversary opens the file itself.
const STAGE_OUTPUT_NAMES = ['ground', 'design', 'hiddenDecisionScan']

for (const [name, path] of Object.entries(stageOutputPaths)) {
  if (!STAGE_OUTPUT_NAMES.includes(name)) throw new Error(`refute: unknown stageOutputPaths field ${name}`)
  if (typeof path !== 'string' || !path.trim()) {
    throw new Error(`refute: stageOutputPaths.${name} must be a nonempty file path`)
  }
}

if (level === 'L1') {
  for (const name of STAGE_OUTPUT_NAMES) {
    if (typeof stageOutputPaths[name] !== 'string' || !stageOutputPaths[name].trim()) {
      throw new Error(`refute: L1 requires stageOutputPaths.${name}, the path to that stage's recorded result`)
    }
  }
}

// L3 has no contract and no prior stage — only the human's own words, which arrive in humanRequest.
if (level === 'L3' && Object.keys(stageOutputPaths).length) {
  throw new Error('refute: L3 supplies no stageOutputPaths')
}

// Each adversary is a distinct lens; SOLID/DRY/laziness/Lie-catcher each READ their owning rail as the checklist.
const ADVERSARIES = [
  {
    id: 'prove-it',
    charge: `You are the Prove-It / anti-review-failure adversary. Read .agents/skills/rails-run-a-workflow/SKILL.md; it owns the cross-stage requirement, proof, gate, and report criteria. Apply the REFUTE criteria from that owner.

Rule the cross-stage requirement first: did any change narrow, soften, or reinterpret a governing requirement to fit what the code already does instead of growing the code to meet it? If so, FAIL. For each contract acceptance check, determine whether it is met with proof pasted verbatim, including flags, bytes, command output, and exit codes, or merely asserted. Re-run every applicable contract spot check yourself. When the contract authorized tests, confirm the acceptance tests pass because the implementation grew to meet each criterion, never because a test was weakened, skipped, deleted, or re-pointed.

Hunt every intended item left unimplemented, unmigrated, unaudited, unverified, stale, or scoped out without an approved reason. Hunt assumptions made without reading the real code, data, or output; loss or mismatch of existing records, inputs, anchors, IDs, lock records, or generated outputs; provenance not preserved before overwrite; success claimed while any caveat remains open; and any RED called "pre-existing".

Answer these three checks exactly: Does every intended item have a change, or is every skipped item explicitly reported with an approved reason? Are locked regions/facts handled through the required lock/fact-owner mechanism? What would the human wrongly believe if they reviewed this as-is?`,
  },
  {
    id: 'solid',
    charge: `You are the SOLID adversary — design and structure ONLY. Read .agents/skills/rails-solid-code/SKILL.md and rule the finished change against that owner rail.`,
  },
  {
    id: 'dry',
    charge: `You are the DRY adversary — duplication ONLY. Read .agents/skills/rails-dry-code/SKILL.md and rule the finished change against that owner rail. Re-run its CodeGraph and grep lenses yourself.`,
  },
  {
    id: 'laziness',
    charge: `You are the laziness-auditor. Read .agents/skills/rails-real-work/SKILL.md and rule the finished change against that owner rail.`,
  },
  {
    id: 'lie-catcher',
    model: 'opus', // Tim relies on this reviewer more than any other; it runs on the strongest model
    charge: `You are the Lie-catcher. Give NO fix advice; leave every finding's fix empty. Every prior-stage result is given to you as a file path; open and read each one yourself. Those stages run once per contract while fix rounds keep changing the tree, so a recorded fact the current code has since overtaken is expected and is not a finding — your own reading of the live code is the authority on what is true now. Judge the solution against the contract, which is the spec; a prior stage's survey is background, never the spec. Read .agents/skills/rails-decisions/SKILL.md, .agents/skills/rails-run-a-workflow/SKILL.md, .agents/skills/rails-test-code/SKILL.md, and .dev/reference/best-practices-guide.md. Apply each owner's Lie-catcher duties.

FIRST rule the run against its complete success definition. Any criterion unmet or any resulting system error is FAIL, regardless of progress. Then report, most damaging first, every deviation, fake or hand-wavy justification, unproven claim asserted as true, and every test weakened, skipped, xfail'd, deleted, or loosened to pass. Diff the test files. Check every decision-level item for Tim's cited verbatim approval. Reject every approval claim based on a non-answer, topic change, clarification request, rewording request, or code that already exists.

Search for every suppression used to reach green instead of cleaning the RED, including #pragma warning disable, [SuppressMessage], NoWarn, severity = none, and a dropped analyzer reference. Only an exact suppression recorded in the contract's Decisions with Tim's verbatim approving words passes; anything absent from or beyond that granted set fails. Audit the orchestrator's steps too. No one is exempt.

Inspect the supplied GROUND, DESIGN, and hidden-decision stage outputs. Check every autoResolved entry against the cited approved principle and its evidence. FAIL when a cited principle does not resolve the recorded choice and resolution. FAIL when a guide-covered choice was escalated instead of auto-resolved. FAIL when a choice not covered by an approved principle was silently defaulted instead of surfaced. FAIL when a stage that ran omits its required autoResolved array.`,
  },
]

if (level === 'L3' && additionalAdversaries.includes('laziness')) {
  throw new Error('refute: L3 may add only the human-requested lie-catcher')
}

const panelIds = level === 'L1'
  ? ['prove-it', 'solid', 'dry', 'laziness', 'lie-catcher']
  : level === 'L2'
    ? ['prove-it', 'dry', 'lie-catcher', ...(additionalAdversaries.includes('laziness') ? ['laziness'] : [])]
    : ['dry', ...(additionalAdversaries.includes('lie-catcher') ? ['lie-catcher'] : [])]
const selectedAdversaries = panelIds.map((id) => ADVERSARIES.find((adversary) => adversary.id === id))
const governingContext = level === 'L3'
  ? `HUMAN REQUEST: ${humanRequest} — This is Tim's exact request defining the work and approving L3. Rule the finished change against it.`
  : `CONTRACT FILE: ${contractPath} — Read it in full and rule the finished change against the complete contract.`

const refutePrompt = (adv) => `You are an adversary for a FINISHED change. Do NOT make code changes. Your job is to REFUTE that this change meets its governing requirement and the rails — assume it does NOT until proven. Ground every judgment in the live code and re-run the applicable spot checks yourself; cropped, stale, or exit-code-missing output is an automatic FAIL.

${adv.charge}

LEVEL: ${level}
${governingContext}
PROJECT PATH: ${projectPath} — read .agents/skills/rails-explorer/SKILL.md and use CodeGraph plus grep across src, tests, analyzers, and eng, then read the relevant working-tree code and re-run the applicable verification commands from the governing context.
CHANGED FILES: ${JSON.stringify(changedFiles)}
RECORDED RESULTS OF THE STAGES THAT RAN, as file paths — open and read each one yourself: ${JSON.stringify(stageOutputPaths)}`

const adversaryRoles = selectedAdversaries.map((adv) => ({
  id: `refute:${adv.id}`,
  prompt: refutePrompt(adv),
  label: `refute:${adv.id}`,
  phase: 'Refute',
  model: adv.model || 'sonnet',
  resultKind: 'verdict',
  expectedLens: adv.id,
  lieCatcher: adv.id === 'lie-catcher',
}))
const panel = await __requiredAgentRuntime({
  caller: 'refute',
  roles: adversaryRoles,
  priorPanelResults,
  retryRoles,
})
const verdicts = panel.results

if (!panel.panelComplete) {
  return {
    contractPath,
    verdicts,
    anyFail: true,
    failedLenses: [
      ...verdicts.filter((verdict) => verdict.verdict === 'FAIL').map((verdict) => verdict.lens),
      ...panel.retryRoles.map((role) => role.replace(/^refute:/, '')),
    ],
    panelComplete: false,
    failedRoles: panel.failedRoles,
    priorPanelResults: panel.priorPanelResults,
    retryRoles: panel.retryRoles,
  }
}

const failed = verdicts.filter((v) => v.verdict === 'FAIL')
log(__adversarySummary(verdicts, 'adversaries'))

return {
  contractPath,
  verdicts,
  anyFail: failed.length > 0,
  failedLenses: failed.map((v) => v.lens),
  panelComplete: true,
  failedRoles: [],
}
