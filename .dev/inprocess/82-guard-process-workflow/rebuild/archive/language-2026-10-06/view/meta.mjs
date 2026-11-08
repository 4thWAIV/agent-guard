// TYPING, ONE LAYER UP. A document against a metalanguage.
//
// `type.mjs` has one rule per device because it types a MODEL, where a process and a gate
// are drawn differently on purpose. A layer up there is nothing to special-case: a
// metalanguage declares devices and parts, and an instance either writes what its device
// declares or it does not. So this is ONE walk, and it works at any layer.
//
// Nothing about the metalanguage is written here. Which words exist, what a part may hold,
// where a reference resolves and what an absence means all come out of the file being
// checked, which is what makes the self-check mean anything.
import { mark } from './type.mjs'

// The words this walk reads out of a metalanguage, by name. They are its INTERPRETATION of
// the metalanguage, not the metalanguage's declaration, so a rename in the YAML would
// silently stop being honoured — a renderer knowing a field list by heart, one layer up.
// Pinned here, and checked before anything else. THIS IS THE WHOLE BOOTSTRAP.
const CONSUMES = {
  device: ['id', 'identity', 'value', 'parts', 'instances', 'one_of'],
  part: ['id', 'holds', 'by', 'within', 'many', 'absence'],
}

const DEFAULT_ABSENCE = 'error'

export const metaLanguageOf = (meta) => {
  const devices = meta.devices ?? []
  const byId = Object.fromEntries(devices.map((d) => [d.id, d]))
  const holdings = Object.fromEntries((byId.holding?.instances ?? []).map((x) => [x.id, x]))
  const absences = Object.fromEntries((byId.part_absence?.instances ?? []).map((x) => [x.id, x]))
  const scopes = Object.fromEntries((byId.scope?.instances ?? []).map((x) => [x.id, x]))
  // A tone carries its own colour, so no colour is ever keyed off an id in a stylesheet.
  // An undeclared one is already caught where it sits: `tone` is a reference like any other.
  const tones = Object.fromEntries((byId.tone?.instances ?? []).map((x) => [x.id, x]))
  const lit = (x) => (x ? { ...x, look: tones[x.tone] ?? null } : x)

  const partsOf = (d) => d?.parts ?? []
  const isUnion = (d) => Array.isArray(d?.one_of)
  const isLeaf = (d) => Boolean(d?.value) && partsOf(d).length === 0 && !isUnion(d)
  const altsOf = (d) => (isUnion(d) ? d.one_of.map((a) => byId[a]).filter(Boolean) : [d])
  const requiredOf = (d) => partsOf(d).filter((p) => (p.absence ?? DEFAULT_ABSENCE) === 'error').map((p) => p.id)

  const pinned = []
  for (const [deviceId, words] of Object.entries(CONSUMES)) {
    const d = byId[deviceId]
    if (!d) {
      pinned.push(mark(`the checker reads a "${deviceId}" and this declares none`, 'meta', deviceId))
      continue
    }
    const has = new Set(partsOf(d).map((p) => p.id))
    for (const w of words) {
      if (!has.has(w)) {
        pinned.push(mark(`the checker reads "${deviceId}.${w}" and this declares no such part`, 'meta', w))
      }
    }
  }

  // ── WHERE AN ID CAN BE FOUND ───────────────────────────────────────────────────
  // Two sets per device. DECLARED is what the device writes on itself, which is how a
  // closed vocabulary is named. THIS DOCUMENT is every instance of it anywhere in the
  // file, reached by following containment — which is what a message declaring its own
  // vocabulary and then using it needs. A part says which it means; nothing guesses.
  const docIndex = new Map()
  const addDoc = (deviceId, id) => {
    if (!docIndex.has(deviceId)) docIndex.set(deviceId, new Set())
    docIndex.get(deviceId).add(String(id))
  }
  const declaredIds = (d) => new Set((d?.instances ?? []).map((x) => String(x[d.identity])))

  // Which alternative of a union an instance IS: the one whose required parts it carries.
  const matchingAlts = (union, obj) => {
    if (!obj || typeof obj !== 'object') return []
    return altsOf(union).filter((a) => requiredOf(a).every((id) => obj[id] !== undefined))
  }
  const pickAlt = (union, obj) => {
    const m = matchingAlts(union, obj)
    return m.length === 1 ? m[0] : null
  }

  const indexInstance = (obj, d) => {
    if (!obj || typeof obj !== 'object' || Array.isArray(obj) || !d) return
    if (d.identity && obj[d.identity] !== undefined) addDoc(d.id, obj[d.identity])
    for (const p of partsOf(d)) {
      if (p.by !== 'containment' || obj[p.id] === undefined) continue
      const held = p.holds === 'self' ? d : byId[p.holds]
      const vals = p.many ? (obj[p.id] ?? []) : [obj[p.id]]
      for (const v of vals) indexInstance(v, held && isUnion(held) ? pickAlt(held, v) : held)
    }
  }
  if (byId.language) indexInstance(meta, byId.language)

  // ── RESOLVING A NAMED ID ───────────────────────────────────────────────────────
  // The id in the slot, looked up in the scope the part declares. A union resolves across
  // its alternatives in that same scope, so `holds: holdable` finds a device or a
  // primitive without the checker knowing that is what `holds` means.
  const resolveRef = (value, heldId, scopeId, at) => {
    const held = byId[heldId]
    if (!held) return { gap: mark(`"${heldId}" names no device`, at, heldId) }
    if (scopeId === undefined) return { gap: mark('a reference must say where it resolves', at, value) }
    const scope = scopes[scopeId]
    if (!scope) return { gap: mark(`"${scopeId}" is not a declared scope`, at, scopeId) }
    if (scopeId === 'elsewhere') {
      return { scope, gap: mark(`"${value}" is looked up in another document, and nothing resolves one yet`, at, value) }
    }
    const setOf = (a) => (scopeId === 'declared' ? declaredIds(a) : (docIndex.get(a.id) ?? new Set()))
    const hits = altsOf(held).filter((a) => setOf(a).has(String(value)))
    if (hits.length === 1) return { scope, found: hits[0] }
    if (hits.length > 1) {
      return { scope, gap: mark(`"${value}" is both a ${hits.map((h) => h.id).join(' and a ')}`, at, value) }
    }
    const have = altsOf(held).flatMap((a) => [...setOf(a)])
    const what = altsOf(held).map((a) => a.id).join(' or a ')
    return { scope, gap: mark(
      `"${value}" is not a ${what} ${scope.name.toLowerCase()} — there is ${have.join(', ') || 'nothing'}`, at, value) }
  }

  // ── A PART ─────────────────────────────────────────────────────────────────────
  const part = (p, d, at) => {
    const gaps = []
    const declared = new Set(partsOf(byId.part).map((x) => x.id))
    for (const k of Object.keys(p ?? {})) {
      if (!declared.has(k)) gaps.push(mark(`a part declares no "${k}"`, at, p[k]))
    }

    // Every id written on a part is resolved THROUGH that part's own declaration on
    // `part` — its `holds` and its `within` — so the scope the file declares is the scope
    // that is used. Looking any of them up directly would make the scope decorative.
    const viaDecl = (partId, value, where) => {
      const decl = partsOf(byId.part).find((x) => x.id === partId)
      if (!decl) return gaps.push(mark(`the checker reads "part.${partId}" and this declares no such part`, where, partId))
      const r = resolveRef(value, decl.holds, decl.within, where)
      if (r.gap) gaps.push(r.gap)
    }

    const absenceId = p.absence ?? DEFAULT_ABSENCE
    const absence = absences[absenceId]
    if (p.absence !== undefined) viaDecl('absence', p.absence, at)

    const by = holdings[p?.by]
    if (p?.by === undefined) gaps.push(mark('a part must say how it holds', at, p))
    else viaDecl('by', p.by, at)

    // `holds` is itself a declared reference on `part` — read that declaration and resolve
    // through it, so a primitive is found because `holdable` says it may be, not because
    // this file knows that is what `holds` means.
    const holdsDecl = partsOf(byId.part).find((x) => x.id === 'holds')
    let held = null
    if (p?.holds === undefined) gaps.push(mark('a part must say what it holds', at, p))
    else if (p.holds === 'self') held = d
    else {
      const r = resolveRef(p.holds, holdsDecl?.holds, holdsDecl?.within, at)
      if (r.gap) gaps.push(r.gap)
      held = r.found && !isUnion(r.found) && r.found.id !== 'primitive' ? byId[p.holds] ?? null : byId[p.holds] ?? null
    }

    // A scope belongs to a reference and to nothing else.
    if (p?.by === 'reference') {
      if (p.within === undefined) gaps.push(mark(`"${p.id}" names what it holds, so it must say where that is looked up`, at, p))
      else viaDecl('within', p.within, at)
    } else if (p?.within !== undefined) {
      gaps.push(mark(`"${p.id}" ${by?.reads ?? p.by} what it holds, so it looks nothing up`, at, p.within))
    }

    // `by` is written rather than derived, so it can disagree with what the slot holds.
    if (held && p?.by && by) {
      const direct = isLeaf(held) || (byId.primitive && declaredIds(byId.primitive).has(p.holds))
      if (p.by === 'value' && !direct) {
        gaps.push(mark(`"${p.id}" holds a ${p.holds}, which has no way of being written directly`, at, p.by))
      }
      if (p.by !== 'value' && direct) {
        gaps.push(mark(`"${p.id}" holds a ${p.holds}, which is written directly and cannot be ${p.by}`, at, p.by))
      }
    }

    return {
      kind: 'metaPart', id: p?.id, description: p?.description,
      holds: {
        name: p?.holds ?? null, target: held ?? null,
        union: held && isUnion(held) ? held.one_of : null,
        leaf: held ? isLeaf(held) : false,
      },
      by: lit(by) ?? null, scope: p?.within === undefined ? null : (lit(scopes[p.within]) ?? null),
      absence: lit(absence) ?? null, absenceId,
      many: Boolean(p?.many), owner: d.id, extra: gaps,
    }
  }

  // ── AN INSTANCE ────────────────────────────────────────────────────────────────
  const instance = (obj, d, at) => {
    const gaps = []
    if (obj == null || typeof obj !== 'object' || Array.isArray(obj)) {
      return { kind: 'metaInstance', owner: d?.id, rows: [],
        extra: [mark(`a ${d?.id} must be written as a block`, at, obj)] }
    }
    const declared = new Map(partsOf(d).map((p) => [p.id, p]))
    for (const p of partsOf(d)) {
      if (obj[p.id] === undefined) {
        if ((p.absence ?? DEFAULT_ABSENCE) === 'error') gaps.push(mark(`a ${d.id} must say "${p.id}"`, `${at}.${p.id}`, obj))
        continue
      }
      if (p.by !== 'reference') continue
      for (const v of (p.many ? obj[p.id] ?? [] : [obj[p.id]])) {
        const r = resolveRef(v, p.holds === 'self' ? d.id : p.holds, p.within, `${at}.${p.id}`)
        if (r.gap) gaps.push(r.gap)
      }
    }
    for (const k of Object.keys(obj)) {
      if (!declared.has(k)) gaps.push(mark(`a ${d.id} declares no part "${k}"`, `${at}.${k}`, obj[k]))
    }
    return {
      kind: 'metaInstance', owner: d.id,
      name: d.identity ? obj[d.identity] : null,
      rows: partsOf(d).filter((p) => obj[p.id] !== undefined && p.id !== d.identity)
        .map((p) => ({ kind: 'pair', key: p.id, value: String(obj[p.id]) })),
      extra: gaps,
    }
  }

  // ── A DEVICE ───────────────────────────────────────────────────────────────────
  const device = (d, at) => {
    const self = byId.device ? instance(d, byId.device, at) : null
    const gaps = [...(self?.extra ?? [])]

    if (d.value && partsOf(d).length) {
      gaps.push(mark(`"${d.id}" has parts, so it is not a leaf and may not carry a value`, at, d.value))
    }
    if (d.identity && !partsOf(d).some((p) => p.id === d.identity)) {
      gaps.push(mark(`"${d.id}" names "${d.identity}" as its identity and declares no such part`, at, d.identity))
    }
    if (isUnion(d)) {
      if (partsOf(d).length) gaps.push(mark(`"${d.id}" names a choice, so it declares no parts of its own`, at, d.id))
      if (d.instances) gaps.push(mark(`"${d.id}" names a choice, so it has no instances of its own`, at, d.id))
      if (d.value) gaps.push(mark(`"${d.id}" names a choice, so it is not written down directly`, at, d.value))
      if (d.one_of.length < 2) {
        gaps.push(mark(`"${d.id}" is a choice between ${d.one_of.length}, which is not a choice`, at, d.id))
      }
      // Two alternatives requiring the same parts would both match, and nothing could say
      // which. That is a defect in the union, not in anything written against it.
      const alts = altsOf(d)
      for (let i = 0; i < alts.length; i++) {
        for (let j = i + 1; j < alts.length; j++) {
          const a = alts[i]; const b = alts[j]
          if (a.instances || b.instances) continue
          if (requiredOf(a).sort().join(',') === requiredOf(b).sort().join(',')) {
            gaps.push(mark(`"${a.id}" and "${b.id}" require the same parts, so nothing can tell them apart`, at, d.id))
          }
        }
      }
    }

    const OWN_BLOCK = new Set(['id', 'description', 'parts', 'instances', 'one_of', 'value'])
    const rows = partsOf(byId.device ?? {})
      .filter((p) => !OWN_BLOCK.has(p.id) && d[p.id] !== undefined)
      .map((p) => ({ kind: 'pair', key: p.id, value: String(d[p.id]) }))

    return {
      kind: 'metaDevice', id: d.id, description: d.description, rows, value: d.value ?? null,
      leaf: isLeaf(d), closed: d.instances !== undefined, union: isUnion(d),
      alternatives: isUnion(d) ? (() => {
        const sigs = d.one_of.map((a) => (byId[a] ? requiredOf(byId[a]).sort().join(',') : ''))
        const distinguishing = new Set(sigs).size > 1
        return d.one_of.map((a, i) => ({ id: a, known: Boolean(byId[a]),
          requires: distinguishing && byId[a] ? requiredOf(byId[a]) : [] }))
      })() : [],
      parts: partsOf(d).map((p, i) => part(p, d, `${at}.parts[${i}]`)),
      instances: (d.instances ?? []).map((x, i) => instance(x, d, `${at}.instances[${i}]`)),
      extra: gaps,
    }
  }

  const root = byId.language
  const rootGaps = root
    ? instance(meta, root, 'meta-language.yaml').extra
    : [mark('this declares no `language` device, so a language file has no root to be an instance of', 'meta-language.yaml', null)]

  // A device nothing holds cannot be written anywhere. The ROOT is exempt because a file
  // is its instance; a LEAF is exempt because it is a type offered to the layer below.
  const held = new Set()
  for (const d of devices) {
    for (const p of partsOf(d)) if (p.holds) held.add(p.holds)
    for (const a of d.one_of ?? []) held.add(a)
  }
  const unreachable = devices
    .filter((d) => d.id !== 'language' && !isLeaf(d) && !held.has(d.id))
    .map((d) => mark(`nothing holds a "${d.id}", so nothing can ever write one`, 'meta', d.id))

  // Which vocabularies teach a reader how to read the page: the closed ones whose
  // instances carry a glyph. Nothing lists them; they say so themselves.
  const legend = devices
    .filter((d) => (d.instances ?? []).length && d.instances.every((x) => x.glyph !== undefined))
    .map((d) => ({ kind: 'metaKey', id: d.id, description: d.description,
      entries: d.instances.map((x) => ({ ...x, look: tones[x.tone] ?? null })) }))

  return { pinned, unreachable, rootGaps, legend, devices: devices.map((d, i) => device(d, `meta-language.yaml.devices[${i}]`)) }
}
