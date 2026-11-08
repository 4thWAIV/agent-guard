// TYPING. Declared data in, typed nodes out. The only place that decides what a piece of
// the model IS.
//
// Two rules make this fail closed by construction rather than by discipline:
//
//   1. Every rule names the keys it consumes. Whatever is left over becomes an
//      `undescribed` node carrying the key and its value, so a field written in a model
//      file and rendered by nobody appears on the page instead of vanishing. The old
//      renderers simply never looked at a key they did not use.
//
//   2. A value that no rule matches becomes `undescribed` too. Nothing is skipped and
//      nothing is printed as bare text, because printing requires a component and the
//      component for data nothing describes is the error.
//
// No vocabulary is written here. Anything about what exists comes from `model.mjs`,
// which reads it from the language.
import { vocab, payloadKinds, fromSetup, language, highlight } from './model.mjs'

export const UNDESCRIBED = 'undescribed'

// ── MARKING ──────────────────────────────────────────────────────────────────────
// `highlight.yaml` names nodes to mark. A path is a list of segments, each the index in
// its list or the node's own name; which part of a device is its name is declared by
// `identity:` in the language, so nothing about naming is written here.
//
// The paths are resolved against the raw data ONCE, before anything is typed, so the only
// thing a typing rule does is ask whether its own path was named. No component implements
// marking and nothing carries a list of what may be marked.
// A path is a list of segments: a property name, or a position in a list. A position may
// be written as its index or as the node's own name, and the two may be mixed.
const segsOf = (p) => String(p).split(/\.|\[|\]/).map((x) => x.trim()).filter(Boolean)

// Which part of a device is its name, declared by `identity:` in the language. A node
// answers to that name as well as to its index.
const identityOf = (obj) => {
  if (!obj || typeof obj !== 'object' || Array.isArray(obj)) return null
  for (const dev of Object.values(language)) {
    if (!dev || typeof dev !== 'object' || !dev.identity) continue
    if (obj[dev.identity] === undefined) continue
    const parts = (dev.parts ?? []).map((x) => x.id)
    // it must look like THIS device: it carries its identity and at least one other part
    if (!parts.some((k) => k !== dev.identity && obj[k] !== undefined)) continue
    return String(obj[dev.identity])
  }
  return null
}

// Every node in the raw data, with each segment carrying both the position it sits at and
// the name it answers to.
const addressBook = (roots) => {
  const out = []
  const walk = (o, at, segs) => {
    if (Array.isArray(o)) {
      o.forEach((x, i) => walk(x, `${at}[${i}]`, [...segs, { index: String(i), name: identityOf(x) }]))
      return
    }
    if (!o || typeof o !== 'object') return
    out.push({ at, segs })
    for (const k of Object.keys(o)) walk(o[k], `${at}.${k}`, [...segs, { index: k, name: k }])
  }
  for (const [name, root] of Object.entries(roots)) walk(root, name, [{ index: name, name }])
  return out
}

let marks = new Map()
let unmatched = []

export const resolveMarks = (roots) => {
  marks = new Map()
  unmatched = []
  const book = addressBook(roots)
  for (const entry of highlight ?? []) {
    const want = segsOf(entry.path)
    const hit = book.find(({ segs }) => segs.length === want.length &&
      segs.every((s, i) => s.index === want[i] || s.name === want[i]))
    if (hit) marks.set(hit.at, entry)
    else unmatched.push(entry)
  }
}
export const unmatchedMarks = () => unmatched
export const marked = (at) => marks.get(at) ?? null

let found = []
export const undescribedFound = () => found
export const resetUndescribed = () => { found = [] }

// Report something the page cannot honour, from outside the typing rules.
export const mark = (why, at, value) => undescribed(why, at, value)

const undescribed = (why, at, value) => {
  const node = { kind: UNDESCRIBED, why, at, value }
  found.push(node)
  return node
}

const list = (xs, fn) => (xs ?? []).map(fn)

// A declared vocabulary entry drawn on the page is a node like any other, so it carries
// the kind that names its component.
const tag = (kind, spec) => (spec ? { kind, ...spec } : null)

// ── the language's own vocabularies, as nodes ────────────────────────────────────
// The parts a declared kind carries. A part is required unless it says otherwise — which
// is what the language could not say before, and why `only_when` had to be special-cased
// in code: putting it in the shape made it required on every permission.
// What a missing part means, from the `part_absence` vocabulary. A part that says nothing
// is an error, which is what every part used to be by default.
const partsOf = (spec) => (spec?.parts ?? []).map((p) => {
  const absence = p.absence ?? 'error'
  if (!vocab.partAbsence[absence]) {
    throw new Error(`"${absence}" is not a declared part_absence`)
  }
  return { id: p.id, absence, required: absence === 'error', description: p.description }
})

// THE ONLY WAY a rule may know what fields a thing has. It reads the device's declared
// parts, reports a required one that is absent and a field nobody declared, and hands back
// the values. A rule that wants a field the language does not declare cannot get one.
//
// Before this, ten devices had their field lists written out in this file and the language
// declared nothing about them, so a field could be added to a model and silently consumed,
// or made required by code alone — which is how `only_when` could not be optional.
export const take = (deviceId, obj, at, extraParts = []) => {
  const device = language[deviceId]
  if (!device) {
    return { v: {}, gaps: [undescribed(`the language declares no "${deviceId}"`, at, deviceId)] }
  }
  const parts = [...partsOf(device), ...extraParts]
  const gaps = []
  const v = {}
  for (const part of parts) {
    const got = obj?.[part.id]
    if (got === undefined) {
      if (part.absence === 'error') {
        gaps.push(undescribed(`a ${deviceId} must say "${part.id}"`, at, obj))
      } else if (part.absence === 'undesigned') {
        // Not malformed — nobody has stated it. Visible where it sits rather than silent.
        gaps.push({ kind: 'notStated', device: deviceId, part: part.id, what: part.description })
      }
      continue
    }
    v[part.id] = got
  }
  const known = new Set(parts.map((p) => p.id))
  for (const k of Object.keys(obj ?? {})) {
    if (!known.has(k)) gaps.push(undescribed(`the language does not declare "${k}" on a ${deviceId}`, at, obj[k]))
  }
  return { v, gaps, parts }
}

const inVocab = (table, id, what, at) =>
  vocab[table][id] ?? undescribed(`"${id}" is not a declared ${what}`, at, id)

// ── type references ──────────────────────────────────────────────────────────────
// A reference is never a string on a page. It is a node that knows what it names, so a
// name with no definition behind it is an error in place rather than ordinary text.
export const reference = (t, at, defined, params = [], declared = null) => {
  if (t == null) return undescribed('a reference with no name', at, t)
  const r = typeof t === 'string' ? { name: t } : t
  if (!r.name) return undescribed('a reference with no name', at, t)
  const args = list(r.arguments, (x, i) => reference(x, `${at}.arguments[${i}]`, defined, params, declared))
  // A type parameter is its own thing. It resolves only inside the element that declares
  // it, and it has no definition to open — a reference that offered to open one was a
  // button that did nothing.
  const what =
    params.includes(r.name) ? 'parameter'
      : defined(r.name) ? 'defined'
        : vocab.primitive[r.name] ? 'primitive'
          : vocab.external[r.name] ? 'external'
            : null
  if (!what) {
    return { kind: 'reference', name: r.name, resolves: null, arguments: args,
      gap: undescribed(`"${r.name}" names nothing`, at, r.name) }
  }
  // THE check the type reference exists for: one argument per parameter the named element
  // declares. Nothing could say this while a generic was a name with angle brackets in it.
  const want = declared?.(r.name)
  const wrongArity = want != null && want !== args.length
    ? undescribed(`${r.name} declares ${want} type parameter${want === 1 ? '' : 's'}, given ${args.length}`, at, r.name)
    : null
  return { kind: 'reference', name: r.name, resolves: what, arguments: args, wrongArity,
    extra: take('type_reference', r, at).gaps }
}

// ── process, step, gate ──────────────────────────────────────────────────────────
export const gateCondition = (c, at) => {
  const spec = vocab.gateCondition[c?.kind]
  if (!spec) return undescribed(`"${c?.kind}" is not a declared gate condition kind`, at, c?.kind)
  const kindParts = partsOf(spec)
  const gaps = []
  for (const p of kindParts) {
    if (p.required && c[p.id] === undefined) {
      gaps.push(undescribed(`a ${spec.id} condition must say "${p.id}"`, at, c))
    }
  }
  const known = new Set(['kind', ...kindParts.map((p) => p.id)])
  for (const k of Object.keys(c ?? {})) {
    if (!known.has(k)) gaps.push(undescribed(`the language does not declare "${k}" on a ${spec.id} condition`, at, c[k]))
  }
  const category = vocab.gateConditionCategory[spec.category]
  if (!category) gaps.push(undescribed(`"${spec.category}" is not a declared gate condition category`, at, spec.category))
  return {
    highlight: marked(at), kind: 'gateCondition', spec, category,
    dependsOn: spec.depends_on ?? [],
    fields: kindParts.filter((p) => c[p.id] !== undefined).map((p) => ({ ...p, value: c[p.id] })),
    over: c.over, by: c.by,
    missing: [], extra: gaps,
  }
}

// A loop's end is a list of conditions, drawn by the same rules a gate's are.
export const repeat = (r, at) => {
  const { v, gaps } = take('repeat', r, at)
  const until = list(v.until, (c, i) => gateCondition(c, `${at}.until[${i}]`))
  return {
    kind: 'repeat', until, atMost: v.at_most,
    empty: until.length ? null : undescribed('a repeat with no condition never ends', at, r),
    extra: gaps,
  }
}

export const gate = (g, at) => {
  const { v, gaps } = take('gate', g, at)
  const spec = vocab.gateKind[v.kind]
  if (!spec) return undescribed(`"${v.kind}" is not a declared gate kind`, at, v.kind)
  const conditions = list(v.conditions, (c, i) => gateCondition(c, `${at}.conditions[${i}]`))
  // A condition that cannot be signalled until another holds needs that other one HERE.
  // Signing what was never locked is the case this exists for.
  const present = new Set(conditions.map((c) => c.spec?.id).filter(Boolean))
  const unmet = []
  for (const c of conditions) {
    for (const need of c.dependsOn ?? []) {
      if (!present.has(need)) {
        unmet.push(undescribed(`${c.spec.id} cannot be signalled until ${need} holds, and this gate does not check ${need}`, at, c.spec.id))
      }
    }
  }
  return {
    highlight: marked(at), kind: 'gate', spec, conditions, unmet,
    empty: conditions.length ? null : undescribed('a gate with no conditions checks nothing', at, g),
    extra: gaps,
  }
}

export const step = (s, at, defined) => {
  const { v, gaps } = take('step', s, at)
  // A step whose actor is wrong is still that step. Replacing the whole row with an error
  // threw away the name and left the reader counting ordinals to find out which one broke.
  const actor = vocab.actor[v.actor]
  return {
    highlight: marked(at), kind: 'step', actor: actor ?? null, name: v.name, note: v.note, fansOut: v.fans_out,
    repeats: v.repeats === undefined ? null : repeat(v.repeats, `${at}.repeats`),
    badActor: actor ? null : undescribed(`"${v.actor}" is not a declared actor`, at, v.actor),
    takes: list(v.takes, (t, i) => reference(t, `${at}.takes[${i}]`, defined)),
    produces: list(v.produces, (t, i) => reference(t, `${at}.produces[${i}]`, defined)),
    unnamed: null,
    extra: gaps,
  }
}

export const researchNeed = (n, at) => {
  const { v, gaps } = take('research_need', n, at)
  const spec = vocab.research[v.kind]
  if (!spec) return undescribed(`"${v.kind}" is not a declared research kind`, at, v.kind)
  return {
    highlight: marked(at), kind: 'researchNeed', spec, required: Boolean(v.required),
    hasProcess: Boolean(spec.process),
    extra: gaps,
  }
}

export const question = (q, at) => {
  const { v, gaps } = take('question', q, at)
  return {
    highlight: marked(at), kind: 'question', id: v.id, text: v.text,
    answeredBy: list(v.answered_by, (k, i) =>
      vocab.research[k] ?? undescribed(`"${k}" is not a declared research kind`, `${at}.answered_by[${i}]`, k)),
    unanswerable: (v.answered_by ?? []).length ? null
      : undescribed('a question with no research behind it cannot be answered', at, q),
    extra: gaps,
  }
}

// A member of a process is a step when one actor does it, and a process when it has
// members of its own. One rule, so depth costs nothing.
export const process = (p, at, defined) => {
  const { v, gaps } = take('process', p, at)
  const members = list(v.members, (x, i) =>
    x?.actor ? step(x, `${at}.members[${i}]`, defined) : process(x, `${at}.members[${i}]`, defined))
  const research = list(v.research, (r, i) => researchNeed(r, `${at}.research[${i}]`))
  const questions = list(v.questions, (q, i) => question(q, `${at}.questions[${i}]`))
  const gates = list(v.gates, (g, i) => gate(g, `${at}.gates[${i}]`))
  return {
    highlight: marked(at), kind: 'process', name: v.name, note: v.note,
    takes: list(v.takes, (t, i) => reference(t, `${at}.takes[${i}]`, defined)),
    produces: list(v.produces, (t, i) => reference(t, `${at}.produces[${i}]`, defined)),
    research, questions, gates, members,
    repeats: v.repeats === undefined ? null : repeat(v.repeats, `${at}.repeats`),
    undesigned: (members.length || research.length || questions.length || gates.length)
      ? null : { kind: 'undesigned', name: v.name },
    extra: gaps,
  }
}

// ── elements and members ─────────────────────────────────────────────────────────
export const typeMember = (x, at, defined, params = [], declared = null) => {
  const { v, gaps } = take('member', x, at)
  const mk = vocab.member[v.kind]
  const vis = vocab.visibility[v.visibility]
  const alt = v.alteration === undefined ? null : vocab.alteration[v.alteration]
  const bad = [...gaps]
  if (v.kind !== undefined && !mk) bad.push(undescribed(`"${v.kind}" is not a declared member kind`, at, v.kind))
  if (v.visibility !== undefined && !vis) bad.push(undescribed(`"${v.visibility}" is not a declared visibility`, at, v.visibility))
  if (v.alteration !== undefined && !alt) bad.push(undescribed(`"${v.alteration}" is not a declared alteration`, at, v.alteration))
  return {
    highlight: marked(at), kind: 'member', name: v.name, memberKind: mk,
    visibility: tag('visibility', vis), alteration: tag('alteration', alt),
    note: v.note,
    type: v.type === undefined ? null : reference(v.type, `${at}.type`, defined, params, declared),
    bad, extra: [],
  }
}

export const element = (e, at, defined, declared = null) => {
  const { v, gaps } = take('element', e, at)
  const form = vocab.form[v.form]
  const vis = vocab.visibility[v.visibility]
  const alt = vocab.alteration[v.alteration]
  const bad = [...gaps]
  if (v.form !== undefined && !form) bad.push(undescribed(`"${v.form}" is not a declared form`, at, v.form))
  if (v.visibility !== undefined && !vis) bad.push(undescribed(`"${v.visibility}" is not a declared visibility`, at, v.visibility))
  if (v.alteration !== undefined && !alt) bad.push(undescribed(`"${v.alteration}" is not a declared alteration`, at, v.alteration))
  if (v.storage !== undefined && !vocab.storage[v.storage]) bad.push(undescribed(`"${v.storage}" is not a declared storage family`, at, v.storage))
  if (v.precision !== undefined && !vocab.precision[v.precision]) bad.push(undescribed(`"${v.precision}" is not a declared precision`, at, v.precision))
  // THE rule precision exists for. An exact record pins every field to a determined size,
  // so a member of one may only name a primitive.
  if (v.precision === 'exact') {
    for (const x of v.members ?? []) {
      const nm = typeof x.type === 'string' ? x.type : x.type?.name
      if (!vocab.primitive[nm]) {
        bad.push(undescribed(`${v.id} is exact, so ${v.id}.${x.name} must name a primitive, not "${nm}"`, at, nm))
      }
    }
  }
  return {
    highlight: marked(at), kind: 'element', id: v.id, name: v.name, form: tag('form', form),
    visibility: tag('visibility', vis), alteration: tag('alteration', alt),
    parameters: v.parameters ?? [], purpose: v.purpose, generated: Boolean(v.generated),
    storage: vocab.storage[v.storage], precision: vocab.precision[v.precision],
    members: list(v.members, (x, i) => typeMember(x, `${at}.members[${i}]`, defined, v.parameters ?? [], declared)),
    bad, extra: [],
  }
}

export const relation = (r, at, defined, byId) => {
  const { v, gaps } = take('relation', r, at)
  const spec = vocab.relation[v.kind]
  if (!spec) return undescribed(`"${v.kind}" is not a declared relation`, at, v.kind)
  const end = (side) => {
    const [head, member] = String(v[side] ?? '').split('.')
    const el = byId[head]
    if (!el) return undescribed(`relation ${side} "${v[side]}" is not an element here`, at, v[side])
    if (member && !(el.members ?? []).some((x) => x.name === member)) {
      return undescribed(`"${v[side]}" is not a member of ${head}`, at, v[side])
    }
    return { kind: 'relationEnd', element: head, member: member ?? null }
  }
  return {
    highlight: marked(at), kind: 'relation', spec, from: end('from'), to: end('to'),
    alteration: tag('alteration', vocab.alteration[v.alteration]), note: v.note,
    // The arguments a relation passes are written in the FROM element's scope.
    toArguments: list(v.to_arguments, (t, i) =>
      reference(t, `${at}.to_arguments[${i}]`, defined,
        byId[String(v.from ?? '').split('.')[0]]?.parameters ?? [])),
    extra: gaps,
  }
}

// ── constraints ──────────────────────────────────────────────────────────────────
export const constraint = (c, at) => {
  const spec = vocab.constraintKind[c?.kind]
  if (!spec) return undescribed(`"${c?.kind}" is not a declared constraint kind`, at, c?.kind)
  // A constraint's own parts, plus the ones its KIND declares. Both come from the
  // language; neither is written here.
  const { v, gaps, parts } = take('constraint', c, at, partsOf(spec))
  const kindParts = partsOf(spec)
  return {
    highlight: marked(at), kind: 'constraint', spec, id: v.id,
    fields: kindParts.filter((p) => v[p.id] !== undefined).map((p) => ({ ...p, value: v[p.id] })),
    text: v.text, source: v.source, over: v.over,
    verb: vocab.permissionVerb[v.verb], actor: vocab.actor[v.actor],
    missing: [], extra: gaps,
  }
}

// ── payloads and decisions ───────────────────────────────────────────────────────
// A payload's type decides which component draws it. The set comes from the architecture,
// so a payload nothing declares is an error rather than a silent miss.
export const payload = (p, at, defined) => {
  if (!p?.type) return undescribed('a payload with no type', at, p)
  if (!payloadKinds.includes(p.type)) {
    return undescribed(`"${p.type}" is not declared as a Payload in the architecture`, at, p.type)
  }
  // `from:` names something in the setup. It resolves, or it is an error — unlike the
  // `in: <file>` it replaces, which was a hyperlink the page took on trust: nothing checked
  // that the file existed, that it held anything of that type, or that it held this.
  if (p.from) {
    const r = fromSetup(p.from)
    if (r.miss) return undescribed(r.miss, at, p.from)
    // What the setup holds is drawn as whatever the payload says it is.
    const DEVICE = { Constraint: 'constraint', Rule: 'rule', Spec: 'spec', UnitTest: 'unit_test' }
    const one = (x, i) => {
      if (p.type === 'Constraint') return constraint(x, `setup.${p.from}[${i}]`)
      const device = DEVICE[p.type]
      if (!device) return undescribed(`no rule types a ${p.type} held in the setup`, at, p.type)
      const got = take('payload', { type: p.type, ...x }, `setup.${p.from}[${i}]`, partsOf(language[device]))
      return { kind: device === 'unit_test' ? 'unitTest' : device, ...got.v, extra: got.gaps }
    }
    const body = p.type === 'Process' && r.value.process
      ? process(r.value.process, `setup.${p.from}`, defined)
      : Array.isArray(r.value)
        ? r.value.map(one)
        : undescribed(`the setup's ${p.from} holds nothing a ${p.type} payload can draw`, at, p.from)
    return { kind: 'payloadFrom', type: p.type, from: p.from, what: r.what, body,
      extra: take('payload', p, at).gaps }
  }
  if (p.in) {
    return undescribed('`in:` points at a file and is never checked — use `from:` into the setup', at, p.in)
  }
  if (p.type === 'Process') {
    // `type` is the payload's own key, consumed here, so the process rule does not meet it.
    const { type, ...body } = p
    return { kind: 'payload', type: p.type, body: process(body, at, defined) }
  }
  if (p.type === 'Sentence') {
    const { v, gaps } = take('payload', p, at, [
      { id: 'text', required: true, description: 'the statement' },
      { id: 'needs', required: true, description: 'the structure that would replace it' },
    ])
    const n = take('needs', v.needs, `${at}.needs`)
    return {
      highlight: marked(at), kind: 'payload', type: p.type,
      body: {
        highlight: marked(at), kind: 'sentence', text: v.text,
        needs: v.needs === undefined
          ? undescribed('a sentence must say what would replace it', at, p)
          : { kind: 'needs', ...n.v, missing: [], extra: n.gaps },
        extra: gaps,
      },
    }
  }
  if (p.type === 'CommandSet') {
    const { v, gaps } = take('payload', p, at, partsOf(language.command_set)
      .map((x) => (x.id === 'verb' ? { ...x, id: 'verbs' } : x)))
    return {
      highlight: marked(at), kind: 'payload', type: p.type,
      body: {
        highlight: marked(at), kind: 'commandSet', prefix: v.prefix,
        over: reference(v.over, `${at}.over`, defined),
        verbs: list(v.verbs, (x, i) => {
          const r = take('verb', x, `${at}.verbs[${i}]`)
          return { kind: 'verb', ...r.v, extra: r.gaps }
        }),
        extra: gaps,
      },
    }
  }
  // Each of these is one declared device, typed by its own parts. A judge reads a
  // constraint and is handed a rule's results; they are not the same thing.
  for (const [type, device] of [['Rule', 'rule'], ['Spec', 'spec'], ['UnitTest', 'unit_test']]) {
    if (p.type !== type) continue
    const { v, gaps } = take('payload', p, at, partsOf(language[device]))
    return {
      highlight: marked(at), kind: 'payload', type,
      body: { kind: device === 'unit_test' ? 'unitTest' : device, ...v,
        over: v.over === undefined ? null : reference(v.over, `${at}.over`, defined),
        extra: gaps },
    }
  }

  // A run has exactly one architecture. It needs no path: there is nothing to point at.
  if (p.type === 'Architecture') {
    return { kind: 'payloadArchitecture', type: p.type, extra: take('payload', p, at).gaps }
  }
  if (p.type === 'Constraint') {
    const { type, ...c } = p
    return { kind: 'payload', type: p.type, body: constraint(c, at) }
  }
  return undescribed(`no rule types a ${p.type} payload`, at, p)
}

export const decision = (d, at, states, defined) => {
  const { v, gaps } = take('decision', d, at)
  const stateOk = states.includes(v.state)
  return {
    highlight: marked(at), kind: 'decision', slug: v.slug, group: v.group, state: v.state,
    badState: stateOk ? null : undescribed(`"${v.state}" is not a declared decision state`, at, v.state),
    payload: payload(v.payload, `${at}.payload`, defined),
    extra: gaps,
  }
}
