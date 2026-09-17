export const meta = {
  name: 'ground',
  description: 'GROUND stage fan-out: parallel explorers derive the subsystem ground truth from the live working tree. When the work introduces new capabilities, the prior-art-ledger rules reuse/extract/new for each capability. Produces grounded facts, applied-principle evidence, and either the reuse ledger or the exact no-new-capability record the contract must carry.',
  phases: [
    { title: 'Explore', detail: 'one explorer per area derives ground truth from live code', model: 'sonnet' },
    { title: 'Ledger', detail: 'when capabilities are present, prior-art-ledger rules reuse/extract/new per capability', model: 'sonnet' },
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

// ##COPIED-MODULE-BEGIN## workflow-brief
// This block is shared code, pasted into every workflow script that needs it. The Workflow
// runtime gives scripts no module import and allows only one level of workflow() nesting,
// so there is no way to call shared code from another file. Do not edit this copy alone:
// every copy of a block name must stay byte-identical, and eng/check-copied-modules.mjs
// fails the moment two copies differ.
function __workflowBrief(args) {
  const caller = args && args.caller
  const issueNumber = args && args.issueNumber
  const inputFolderPath = args && args.inputFolderPath
  const issueScope = args && args.issueScope

  if (typeof caller !== 'string' || !caller) {
    throw new Error('workflow-brief requires args { caller, issueNumber, inputFolderPath, issueScope? }')
  }

  const issue = typeof issueNumber === 'number' && Number.isFinite(issueNumber)
    ? String(issueNumber)
    : (typeof issueNumber === 'string' ? issueNumber.trim() : '')
  const folder = typeof inputFolderPath === 'string' ? inputFolderPath.trim() : ''
  if (!issue || !folder) {
    throw new Error(`${caller} requires issueNumber and inputFolderPath: the GitHub issue number this run works from, and the path of that issue's input folder`)
  }
  if (issueScope !== undefined && issueScope !== null && (typeof issueScope !== 'string' || !issueScope.trim())) {
    throw new Error(`${caller}: issueScope is optional, but when it is given it must be a nonempty string naming the part of the issue this run covers`)
  }

  const scope = typeof issueScope === 'string' ? issueScope.trim() : ''
  const scopeSentence = scope ? ` This run covers only part of that issue — ${scope} — so work to that part and nothing beyond it.` : ''

  return `THE BRIEF — read it before you do anything else. This run works from GitHub issue #${issue}. Read the live issue yourself with \`gh issue view ${issue}\`, and read every file in that issue's input folder ${folder}. The live issue is the authority: wherever the live issue and anything in the input folder disagree, the issue wins.${scopeSentence}`
}
// ##COPIED-MODULE-END## workflow-brief

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

// ##COPIED-MODULE-BEGIN## prior-art-ledger
// Generated copy of the prior-art-ledger stage body. It is inlined rather than chained so the
// Workflow runtime's single nesting level stays free for a case that genuinely needs it. This block
// uses the shared module copies already present in the host file. Every copy must stay byte-identical;
// the drift check in eng/check-copied-modules.mjs enforces it.
async function __priorArtLedger(args) {
  const input = await __workflowInput({ caller: 'prior-art-ledger', value: args })

  const projectPath = input && input.projectPath
  const capabilities = input && input.capabilities
  const brief = input && input.brief   // optional; the calling stage's run brief, absent when this stage is invoked on its own
  const priorPanelResults = input && input.priorPanelResults !== undefined ? input.priorPanelResults : []
  const retryRoles = input && input.retryRoles
  const priorStageResult = input && input.priorStageResult

  if (!projectPath || !capabilities || !capabilities.length) {
    throw new Error(
      'prior-art-ledger requires args { projectPath, capabilities: [{id, description}] } (got type: ' + typeof args + ')')
  }
  if (brief !== undefined && brief !== null && (typeof brief !== 'string' || !brief.trim())) {
    throw new Error('prior-art-ledger: brief is optional, but when it is given it must be a nonempty string carrying the run brief every agent reads')
  }

  // A stage that has a run brief hands it down, and every agent this block launches leads with it.
  // Invoked on its own there is no brief, and the prompts below are exactly what they were without one.
  const briefPreamble = brief ? `${brief.trim()}\n\n` : ''

  const searchContracts = []
  for (const capability of capabilities) {
    searchContracts.push(await __stageResultContracts({
      name: 'prior-art-candidates',
      context: { expectedCapability: capability.id },
    }))
  }

  const gatherPrompt = (cap) => `${briefPreamble}You are a code-search agent. Find every existing place in this codebase that ALREADY provides the capability below. Do NOT write code. Never invent a hit — only report real results the tools return. Read .agents/skills/rails-dry-code/SKILL.md first; it owns the discovery lenses and prior-art criteria.

  PROJECT PATH: ${projectPath}
  CAPABILITY id="${cap.id}": ${cap.description}

  Run BOTH search lenses and pool the hits. Load a tool first with ToolSearch if it is not already available.

  1. CodeGraph (structural + call graph): ToolSearch "select:mcp__codegraph__codegraph_explore", then call codegraph_explore with projectPath="${projectPath}" and a query naming the key symbols/terms for this capability.
  2. Grep (lexical): use the Grep tool (or Bash ripgrep) for the distinctive identifiers/tokens this capability would use. Search ALL FOUR code trees, not just one — ${projectPath}/src, ${projectPath}/tests, ${projectPath}/analyzers, and ${projectPath}/eng. A capability already implemented in the analyzers, in the test helpers, or as a build script in eng is exactly the duplicate this ledger exists to catch, and grepping src alone will miss it. Exclude build output (bin, obj) and ${projectPath}/docs.

  For each hit record: which lens found it, file, line (if known), symbol name, and a one-line snippet.
  Return: capability="${cap.id}", the pooled candidates, lensesRun (every lens you actually ran), lensesEmpty (only lenses that completed and returned nothing), and lensErrors (every lens that failed, or []). A failed lens is not empty and prevents a reuse/extract/new ruling.`

  const evaluatePrompt = (cap, found) => `${briefPreamble}You are a reuse judge. Rule whether the capability below ALREADY exists in the codebase, using ONLY the pooled search hits provided. Do NOT write code. Do NOT invent hits. Read .agents/skills/rails-dry-code/SKILL.md first; it owns the reuse/extract/new criteria.

  CAPABILITY id="${cap.id}": ${cap.description}

  POOLED SEARCH HITS (JSON):
  ${JSON.stringify(found)}

  Apply the rail's reuse/extract/new ruling. Set owner="file:line" for reuse. Set copies=["file:line", ...] for every copy when the ruling is extract. A new ruling is valid only when both required lenses completed and appear in lensesEmpty. Give evidence (the file:line facts or completed-empty lenses you relied on) and confidence (high/medium/low). Return capability="${cap.id}".`

  const searchRoles = capabilities.map((cap, index) => ({
    id: `search:${cap.id}`,
    prompt: gatherPrompt(cap),
    label: `search:${cap.id}`,
    phase: 'Search',
    model: 'sonnet',
    schema: searchContracts[index].schema,
    validation: searchContracts[index].validation,
  }))
  const roleGroups = [
    { id: 'search', roleIds: searchRoles.map((role) => role.id) },
    { id: 'judge', roleIds: capabilities.map((capability) => `judge:${capability.id}`) },
  ]
  const searchPanel = await __requiredAgentRuntime({
    caller: 'prior-art-ledger:search',
    roles: searchRoles,
    roleGroups,
    roleGroup: 'search',
    priorStageResults: priorStageResult && priorStageResult.searchResults,
    priorPanelResults,
    retryRoles,
  })

  if (!searchPanel.panelComplete) {
    return {
      projectPath,
      verdicts: [],
      searchResults: searchPanel.results,
      panelComplete: false,
      failedRoles: searchPanel.failedRoles,
      priorPanelResults: searchPanel.priorPanelResults,
      retryRoles: searchPanel.retryRoles,
      priorStageResult: null,
    }
  }

  const searchResults = searchPanel.results

  const judgeRoles = []
  for (const cap of capabilities) {
    const found = searchResults.find((result) => result.capability === cap.id)
    const verdictContract = await __stageResultContracts({
      name: 'prior-art-verdict',
      context: { expectedCapability: cap.id, searchResult: found },
    })
    judgeRoles.push({
      id: `judge:${cap.id}`,
      prompt: evaluatePrompt(cap, found),
      label: `judge:${cap.id}`,
      phase: 'Judge',
      model: 'sonnet',
      effort: 'low',
      schema: verdictContract.schema,
      validation: verdictContract.validation,
    })
  }
  const judgePanel = await __requiredAgentRuntime({
    caller: 'prior-art-ledger:judge',
    roles: judgeRoles,
    roleGroups,
    roleGroup: 'judge',
    ...searchPanel.nextGroupInput,
  })

  if (!judgePanel.panelComplete) {
    return {
      projectPath,
      verdicts: judgePanel.results,
      searchResults,
      panelComplete: false,
      failedRoles: judgePanel.failedRoles,
      priorPanelResults: judgePanel.priorPanelResults,
      retryRoles: judgePanel.retryRoles,
      priorStageResult: { searchResults },
    }
  }

  const result = { projectPath, verdicts: judgePanel.results, panelComplete: true, failedRoles: [] }
  await __stageResultContracts({ name: 'prior-art-stage', operation: 'validate', result })
  return result
}
// ##COPIED-MODULE-END## prior-art-ledger


const input = await __workflowInput({ caller: 'ground', value: args })

const projectPath = input && input.projectPath
const issueNumber = input && input.issueNumber           // the GitHub issue this run works from
const inputFolderPath = input && input.inputFolderPath   // that issue's input folder
const issueScope = input && input.issueScope             // optional; the part of the issue this run covers
const areas = input && input.areas                 // [{ id, focus }]
const capabilities = (input && input.capabilities) || [] // [{ id, description }]; empty means no new capability
const priorPanelResults = input && input.priorPanelResults !== undefined ? input.priorPanelResults : []
const retryRoles = input && input.retryRoles
const priorStageResult = input && input.priorStageResult

if (!projectPath || !areas || !areas.length || !Array.isArray(capabilities)) {
  throw new Error(
    'ground requires args { projectPath, issueNumber, inputFolderPath, issueScope?, areas: [{id, focus}], capabilities?: [{id, description}] }. issueNumber and inputFolderPath are both required. Pass capabilities: [] when the work introduces no new capability (got type: ' + typeof args + ')')
}

const brief = __workflowBrief({ caller: 'ground', issueNumber, inputFolderPath, issueScope })

const factsContracts = []
for (const area of areas) {
  factsContracts.push(await __stageResultContracts({
    name: 'ground-facts',
    context: { expectedArea: area.id },
  }))
}
const noCapabilityLedger = factsContracts[0].values.noCapabilityLedger

const explorePrompt = (area) => `${brief}

You are an explorer deriving GROUND TRUTH for one area of a subsystem from the live working tree. Do NOT write code.

Read .agents/skills/rails-explorer/SKILL.md in full and follow its exploration method and evidence requirements. Read .agents/skills/rails-decisions/SKILL.md and .dev/reference/best-practices-guide.md before classifying any choice. Use CodeGraph and grep across src, tests, analyzers, and eng; do not use a separate discovery lens.

PROJECT PATH: ${projectPath}
YOUR AREA — ${area.id}: ${area.focus}

Return: area="${area.id}"; facts (each a proven code-level claim with its evidence); surfaces (every store or file where a value this area touches lives); openQuestions (anything the working tree and approved principles cannot answer); and the autoResolved applied-principle evidence required by rails-decisions. If your area is genuinely thin, return few facts and do not pad or invent.`

const explorerRoles = areas.map((area, index) => ({
  id: `explore:${area.id}`,
  prompt: explorePrompt(area),
  label: `explore:${area.id}`,
  phase: 'Explore',
  model: 'sonnet',
  schema: factsContracts[index].schema,
  autoResolved: factsContracts[index].autoResolved,
  validation: factsContracts[index].validation,
}))
const roleGroups = [
  { id: 'explore', roleIds: explorerRoles.map((role) => role.id) },
  ...(capabilities.length ? [
    { id: 'prior-art-search', roleIds: capabilities.map((capability) => `search:${capability.id}`) },
    { id: 'prior-art-judge', roleIds: capabilities.map((capability) => `judge:${capability.id}`) },
  ] : []),
]
const panel = await __requiredAgentRuntime({
  caller: 'ground',
  roles: explorerRoles,
  roleGroups,
  roleGroup: 'explore',
  priorStageResults: priorStageResult && priorStageResult.facts,
  priorPanelResults,
  retryRoles,
})
const facts = panel.results

if (!panel.panelComplete) {
  return {
    projectPath,
    facts,
    ledger: null,
    panelComplete: false,
    failedRoles: panel.failedRoles,
    priorPanelResults: panel.priorPanelResults,
    retryRoles: panel.retryRoles,
    priorStageResult: null,
  }
}

log(`grounded ${facts.reduce((n, f) => n + (f.facts ? f.facts.length : 0), 0)} facts across ${facts.length} areas`)

const ledger = capabilities.length
  ? await __priorArtLedger({
    projectPath,
    capabilities,
    brief,
    priorStageResult: priorStageResult && priorStageResult.ledgerStageResult,
    ...panel.nextGroupInput,
  })
  : noCapabilityLedger

if (capabilities.length && (!ledger || ledger.panelComplete === false)) {
  return {
    projectPath,
    facts,
    ledger,
    panelComplete: false,
    failedRoles: (ledger && ledger.failedRoles) || [{ role: 'prior-art-ledger', attempts: [] }],
    priorPanelResults: (ledger && ledger.priorPanelResults) || [],
    retryRoles: (ledger && ledger.retryRoles) || ['prior-art-ledger'],
    priorStageResult: {
      facts,
      ledgerStageResult: ledger && ledger.priorStageResult,
    },
  }
}

const result = { projectPath, facts, ledger, panelComplete: true, failedRoles: [] }
await __stageResultContracts({ name: 'ground-stage', operation: 'validate', result })
return result
