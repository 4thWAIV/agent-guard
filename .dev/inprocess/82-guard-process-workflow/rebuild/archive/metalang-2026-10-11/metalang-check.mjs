// Checks a document at any of the three layers, with the same rule each time: a document is
// checked against the one below it.
//
//   metalang.yaml                      the metalanguage, against ITSELF
//   a language   ($language absent)    against metalang.yaml
//   an instance  ($language present)   against the language it names
//
// Usage:  node metalang-check.mjs [file]
import * as yaml from 'js-yaml'
import { readFileSync } from 'node:fs'
import { dirname, resolve as resolvePath } from 'node:path'

const VOCABULARY = 'metalang.yaml'
const BOOTSTRAP = ['$id', '$anchor', '$language', 'is', 'title', 'description']
const REFERENCE = /^[A-Za-z0-9._\/-]*#[A-Za-z_][A-Za-z0-9._-]*$/
const OUTSIDE_SUBSET = [
  [/\\A/, '\\A'], [/\\[zZ]/, '\\z or \\Z'], [/\\[pP]\{/, '\\p{...}'],
  [/\[[^\]]*-\[/, 'class subtraction'], [/\(\?<-/, 'balancing group'],
  [/\(\?>/, 'atomic group'], [/[*+?}]\+/, 'possessive quantifier'],
]

const problems = []
const fail = (where, what) => problems.push({ where, what })

const load = path => yaml.load(readFileSync(path, 'utf8'))
const topLevel = d => Object.fromEntries(Object.entries(d).filter(([k]) => !k.startsWith('$') && d[k] && typeof d[k] === 'object'))

// Every definition reachable by name, including the ones nested inside another.
const nest = (into, name, def) => {
  into[name] = def
  for (const key of ['properties', 'options', 'defines'])
    for (const [childName, child] of Object.entries(def[key] ?? {}))
      if (child && typeof child === 'object') nest(into, child.$anchor ?? `${name}.${childName}`, child)
}
const indexOf = doc => {
  const defs = {}
  for (const [n, d] of Object.entries(topLevel(doc))) nest(defs, n, d)
  const anchors = new Map()
  for (const [n, d] of Object.entries(defs)) if (d.$anchor && !anchors.has(d.$anchor)) anchors.set(d.$anchor, n)
  return { defs, anchors }
}

const file = process.argv[2] ?? VOCABULARY
const doc = load(file)
const here = dirname(resolvePath(file))

// ── which layer is this, and what is it checked against ──────────────────────
const isMeta = !doc.$language && file.endsWith(VOCABULARY)
const againstPath = doc.$language
  ? resolvePath(here, doc.$language)
  : (isMeta ? resolvePath(file) : resolvePath(here, VOCABULARY))
const against = isMeta ? doc : load(againstPath)
const lower = indexOf(against)
const own = topLevel(doc)
const mine = indexOf(doc)
const lookup = name => lower.defs[lower.anchors.get(name) ?? name]

const resolveRef = (ref, index) => {
  if (typeof ref !== 'string') return null
  if (!REFERENCE.test(ref)) return { bad: 'not a plain-name fragment' }
  const [docPart, frag] = ref.split('#')
  if (docPart) return { crossDocument: docPart }
  const owner = index.anchors.get(frag)
  return owner ? { name: owner, def: index.defs[owner] } : { bad: `nothing anchors "${frag}"` }
}

// One value against the requirements that govern it. A requirement may sit on the TYPE,
// where it holds for every use, or on the PART, where it holds for that one use. Both apply,
// and both layers below call this, so the rules cannot drift apart.
const checkRequirements = (where, value, sources) => {
  for (const src of sources) {
    if (!src) continue
    if (typeof src.pattern === 'string' && typeof value === 'string' && !new RegExp(src.pattern).test(value))
      fail(where, `"${value}" does not match ${src.pattern}`)
    if (Array.isArray(src.one_of) && !src.one_of.includes(value))
      fail(where, `"${value}" is not one of ${src.one_of.join(', ')}`)
    if (typeof src.minimum === 'number' && value < src.minimum) fail(where, `${value} is below the minimum ${src.minimum}`)
    if (typeof src.maximum === 'number' && value > src.maximum) fail(where, `${value} is above the maximum ${src.maximum}`)
    if (typeof src.min_length === 'number' && String(value).length < src.min_length)
      fail(where, `"${value}" is shorter than ${src.min_length}`)
    if (typeof src.max_length === 'number' && String(value).length > src.max_length)
      fail(where, `"${value}" is longer than ${src.max_length}`)
  }
}

// ════════════════════════════════════════════════════════════════════════════
// LAYER 1 and 2 — a language, checked against the metalanguage
// ════════════════════════════════════════════════════════════════════════════
const permitted = (name, seen = new Set()) => {
  if (seen.has(name)) return new Set()
  seen.add(name)
  const mc = lower.defs[name]
  if (!mc) return new Set()
  const inherited = (mc.extends ?? []).flatMap(r => {
    const t = resolveRef(r, lower)
    return t?.name ? [...permitted(t.name, seen)] : []
  })
  return new Set([...BOOTSTRAP, ...Object.keys(mc.properties ?? {}), ...inherited])
}
const requiredOf = (name, seen = new Set()) => {
  if (seen.has(name)) return []
  seen.add(name)
  const mc = lower.defs[name]
  if (!mc) return []
  const inherited = (mc.extends ?? []).flatMap(r => {
    const t = resolveRef(r, lower)
    return t?.name ? requiredOf(t.name, seen) : []
  })
  return [...Object.entries(mc.properties ?? {}).filter(([, p]) => p.required === 'yes').map(([k]) => k), ...inherited]
}

const checkDefinition = (where, node) => {
  if (!node || typeof node !== 'object' || Array.isArray(node)) return
  if (node.is === undefined) { fail(where, 'no `is`, so its metaclass is unknown'); return }
  const mcName = lower.anchors.get(node.is) ?? (lower.defs[node.is] ? node.is : null)
  if (!mcName) { fail(where, `is: ${node.is} — nothing anchors that metaclass`); return }
  const mc = lower.defs[mcName]
  if (mc.abstract === 'yes') fail(where, `is: ${node.is} — that metaclass is abstract`)

  const allowed = permitted(mcName)
  for (const key of Object.keys(node))
    if (!allowed.has(key)) fail(where, `keyword "${key}" is not in the vocabulary of ${node.is}`)
  for (const key of requiredOf(mcName))
    if (node[key] === undefined) fail(where, `"${key}" is required by ${node.is} and is absent`)
  for (const key of ['owns', 'opposite'])
    if (node[key] !== undefined && node.is !== 'link') fail(where, `"${key}" is only meaningful on a link, not on ${node.is}`)

  for (const key of ['target', 'type', 'opposite', 'identified_by']) {
    const v = node[key]
    if (typeof v !== 'string' || !v.includes('#')) continue
    const r = resolveRef(v, mine.anchors.has(v.split('#')[1]) ? mine : lower)
    if (r?.bad) fail(where, `${key}: ${v} — ${r.bad}`)
  }
  for (const r of node.extends ?? []) {
    const t = resolveRef(r, mine.anchors.has(String(r).split('#')[1]) ? mine : lower)
    if (t?.bad) fail(where, `extends: ${r} — ${t.bad}`)
  }
  if (node.is === 'field' && typeof node.type === 'string') {
    const index = mine.anchors.has(node.type.split('#')[1]) ? mine : lower
    const t = resolveRef(node.type.includes('#') ? node.type : '#' + node.type, index)
    if (t?.def && !['scalar', 'choice'].includes(t.def.is))
      fail(where, `type: ${node.type} — a field holds a value, so its type must be a scalar or a choice, not ${t.def.is === 'entity' ? 'an entity' : 'a ' + t.def.is}`)
  }
  if (typeof node.pattern === 'string')
    for (const [re, shown] of OUTSIDE_SUBSET)
      if (re.test(node.pattern)) fail(where, `pattern uses ${shown}, which is outside the approved subset`)

  // A keyword's own value requirements apply here too, so an unknown primitive or an unknown
  // format is refused where the language is written and not only where an instance is.
  for (const [key, part] of Object.entries(partsOf(mcName))) {
    if (part.is !== 'field' || node[key] === undefined) continue
    const ref = part.type ? String(part.type) : null
    const t = ref ? resolveRef(ref.includes('#') ? ref : '#' + ref, lower) : null
    for (const v of Array.isArray(node[key]) ? node[key] : [node[key]])
      checkRequirements(`${where}.${key}`, v, [t?.def, part])
  }

  for (const key of ['properties', 'options', 'defines'])
    for (const [childName, child] of Object.entries(node[key] ?? {}))
      checkDefinition(`${where}.${childName}`, child)
}

// ════════════════════════════════════════════════════════════════════════════
// LAYER 3 — an instance, checked against the language it names
// ════════════════════════════════════════════════════════════════════════════
const PRIMITIVE = {
  string: v => typeof v === 'string',
  number: v => typeof v === 'number',
  boolean: v => typeof v === 'boolean',
  collection: v => Array.isArray(v),
  object: v => v !== null && typeof v === 'object' && !Array.isArray(v),
  null: v => v === null,
}

// Is `name` the wanted entity, or anything that extends it? A link that takes a term must
// accept a variable, because a variable IS a term. Exact-name matching was the bug.
const isKindOf = (name, wanted, seen = new Set()) => {
  if (name === wanted) return true
  if (seen.has(name)) return false
  seen.add(name)
  const def = lower.defs[name]
  return (def?.extends ?? []).some(r => {
    const t = resolveRef(r, lower)
    return t?.name ? isKindOf(t.name, wanted, seen) : false
  })
}

// What a link may legally point at: the entity it names, plus everything extending it.
const acceptedBy = wanted =>
  Object.keys(lower.defs).filter(n => lower.defs[n].is === 'entity'
    && lower.defs[n].abstract !== 'yes' && isKindOf(n, wanted))

// Every part an entity offers, its own and everything it extends.
const partsOf = (name, seen = new Set()) => {
  if (seen.has(name)) return {}
  seen.add(name)
  const def = lower.defs[name]
  if (!def) return {}
  const inherited = (def.extends ?? []).reduce((acc, r) => {
    const t = resolveRef(r, lower)
    return t?.name ? { ...partsOf(t.name, seen), ...acc } : acc
  }, {})
  return { ...inherited, ...(def.properties ?? {}) }
}

// One value against the scalar or choice its part names.
const checkValue = (where, value, typeRef, part) => {
  const t = resolveRef(typeRef.includes('#') ? typeRef : '#' + typeRef, lower)
  if (!t || t.bad) { fail(where, `type: ${typeRef} — ${t?.bad ?? 'unresolvable'}`); return }
  const type = t.def

  if (type.is === 'choice') {
    const names = Object.keys(type.options ?? {})
    if (!names.includes(value))
      fail(where, `"${value}" is not one of ${t.name} — ${names.join(', ')}`)
    return
  }
  const test = PRIMITIVE[type.of]
  if (test && !test(value)) { fail(where, `"${value}" is not ${type.of}, which ${t.name} holds`); return }

  checkRequirements(where, value, [type, part])
}

const identities = new Map()

const checkInstance = (where, node) => {
  if (!node || typeof node !== 'object' || Array.isArray(node)) { fail(where, 'not an instance'); return }
  if (node.is === undefined) { fail(where, 'no `is`, so what it is an instance of is unknown'); return }
  const ref = resolveRef(String(node.is), lower)
  if (!ref || ref.bad) { fail(where, `is: ${node.is} — ${ref?.bad ?? 'unresolvable'}`); return }
  const def = ref.def
  if (def.is !== 'entity') { fail(where, `is: ${node.is} — only an entity can have instances, and ${ref.name} is a ${def.is}`); return }
  if (def.abstract === 'yes') { fail(where, `is: ${node.is} — ${ref.name} is abstract`); return }

  const parts = partsOf(ref.name)

  // The closed vocabulary again, one layer down.
  for (const key of Object.keys(node))
    if (!BOOTSTRAP.includes(key) && !(key in parts))
      fail(where, `"${key}" is not a part of ${ref.name}`)

  for (const [partName, part] of Object.entries(parts)) {
    const value = node[partName]
    if (value === undefined) {
      if (part.required === 'yes') fail(where, `"${partName}" is required by ${ref.name} and is absent`)
      continue
    }
    const many = Array.isArray(value)
    const members = many ? value : [value]

    if (typeof part.min_items === 'number' && members.length < part.min_items)
      fail(`${where}.${partName}`, `has ${members.length} members, fewer than the ${part.min_items} required`)
    if (typeof part.max_items === 'number' && members.length > part.max_items)
      fail(`${where}.${partName}`, `has ${members.length} members, more than the ${part.max_items} allowed`)
    if (part.unique === 'yes') {
      const key = m => (part.unique_by && m && typeof m === 'object') ? m[part.unique_by] : m
      const seen = new Set()
      for (const m of members) {
        const k = JSON.stringify(key(m))
        if (seen.has(k)) fail(`${where}.${partName}`, `"${key(m)}" appears more than once, and members must be unique`)
        seen.add(k)
      }
    }

    members.forEach((m, i) => {
      const at = many ? `${where}.${partName}[${i}]` : `${where}.${partName}`
      if (part.is === 'field') checkValue(at, m, String(part.type), part)
      else {
        const target = resolveRef(String(m), mine)
        if (!target || target.bad) { fail(at, `${m} — ${target?.bad ?? 'is not a reference'}`); return }
        const wanted = resolveRef(String(part.target), lower)
        if (wanted?.name && target.def?.is !== undefined) {
          const actual = resolveRef(String(target.def.is), lower)
          if (actual?.name && !isKindOf(actual.name, wanted.name)) {
            const ok = acceptedBy(wanted.name)
            fail(at, `points at ${actual.name}, but ${partName} takes ${wanted.name}`
              + (ok.length && ok.join() !== wanted.name ? ` — which here means ${ok.join(', ')}` : ''))
          }
        }
      }
    })
  }

  // identified_by: the named part must be present and unique across instances.
  if (def.identified_by) {
    const idPart = resolveRef(String(def.identified_by), lower)
    const partName = idPart?.name?.split('.').pop()
    if (partName) {
      const v = JSON.stringify(node[partName])
      const bucket = ref.name + '::' + v
      if (node[partName] === undefined) fail(where, `"${partName}" identifies ${/^[aeiou]/i.test(ref.name) ? "an" : "a"} ${ref.name} and is absent`)
      else if (identities.has(bucket)) fail(where, `"${node[partName]}" already identifies ${identities.get(bucket)}`)
      else identities.set(bucket, where)
    }
  }
}

// ── run ──────────────────────────────────────────────────────────────────────
const layer = doc.$language ? 'instance' : isMeta ? 'metalanguage' : 'language'
for (const [name, def] of Object.entries(own)) {
  if (!def.$anchor) fail(name, 'no $anchor, so nothing can reference it')
  if (layer === 'instance') checkInstance(name, def)
  else checkDefinition(name, def)
}

const againstName = isMeta ? 'itself' : (doc.$language ?? VOCABULARY)
console.log(`${file} — ${Object.keys(own).length} ${layer === 'instance' ? 'instances' : 'definitions'}, checked against ${againstName}`)
console.log()
if (!problems.length) {
  console.log(layer === 'instance' ? 'Every instance is legal.' : isMeta ? 'The file validates against itself.' : 'Every definition is legal MetaLang.')
} else {
  console.log(`${problems.length} problem${problems.length === 1 ? '' : 's'}:`)
  for (const p of problems) console.log(`  ${p.where}`.padEnd(40) + p.what)
}
process.exit(problems.length ? 1 : 0)
