// The data. Loaded, never interpreted here.
//
// This file knows how to READ the three model files and nothing else. It holds no rule
// about what anything means and draws nothing. The language's own declarations are the
// only source for what kinds exist, what parts a device has, and what the vocabularies
// are — so a list of forms or lenses or gate kinds is never written in code again.
// The YAML is read once at build time and handed over as data. Nothing here touches a
// filesystem, so every one of these modules runs in a browser unchanged.
export { metaLanguage, language, architecture, contract, setup, highlight } from './.data.mjs'
import { language, architecture, setup } from './.data.mjs'

// Every declared vocabulary, by id, so nothing looks one up by writing its values out.
const index = (list) => Object.fromEntries((list ?? []).map((x) => [x.id, x]))

export const vocab = {
  form: index(language.forms),
  member: index(language.members),
  visibility: index(language.visibility),
  alteration: index(language.alteration),
  relation: index(language.relations),
  storage: index(language.storage),
  precision: index(language.precision),
  primitive: index(language.primitives),
  external: index(language.external),
  actor: index(language.actors),
  lens: index(language.lenses),
  research: index(setup.research),
  gateKind: index(language.gate_kinds),
  gateCondition: index(language.gate_condition_kinds),
  constraintKind: index(language.constraint_kinds),
  permissionVerb: index(language.permission_verbs),
  prose: index(language.prose),
  level: index(language.levels),
  tone: index(language.tones),
  view: index(language.views),
  dimension: index(language.dimensions),
  device: index(language.device),
  partAbsence: index(language.part_absence),
  gateConditionCategory: index(language.gate_condition_categories),
}

// THE look of a declared thing, from what IT declares. A vocabulary entry names a tone; the
// palette says what that tone is. Nothing decides a colour from an id, and an entry naming a
// tone the palette does not have is an error rather than a silent default.
export const toneOf = (entry) => {
  if (!entry?.tone) return null
  const t = index(language.tones)[entry.tone]
  if (!t) return { miss: `"${entry.tone}" is not a declared tone` }
  return { style: { '--t-ink': t.ink, '--t-tint': t.tint } }
}

// A path into the setup, as `from:` writes it. Resolves to the thing itself, or to
// nothing — which is an error, the same as any other name that resolves to nothing.
export const fromSetup = (path) => {
  const [head, id] = String(path ?? '').split('.')
  const list = setup[head]
  if (!Array.isArray(list)) return { miss: `the setup has no "${head}"` }
  if (id === undefined) return { value: list, what: head }
  const found = list.find((x) => x.id === id)
  if (!found) return { miss: `the setup's ${head} has no "${id}"` }
  return { value: found, what: head }
}

// The parts a declared device may carry, straight from the language.
export const partsOf = (device) => (language[device]?.parts ?? []).map((p) => p.id)

// What a Decision may carry: whatever the architecture declares as implementing Payload.
// Not a list anyone maintains.
export const payloadKinds = architecture.relations
  .filter((r) => r.kind === 'implements' && r.to === 'Payload')
  .map((r) => r.from)
