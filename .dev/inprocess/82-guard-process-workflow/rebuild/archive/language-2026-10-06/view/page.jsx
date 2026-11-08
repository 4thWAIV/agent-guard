// The contract document, assembled as typed nodes and handed to the drawings.
//
// This file decides what the page is MADE OF and in what order. It decides nothing about
// what anything means — that is `type.mjs` — and nothing about how anything looks — that
// is `ui.jsx`. It contains no markup.
import React from 'react'
import { Draw } from './ui.jsx'
import { contract, architecture, language, setup, metaLanguage, vocab, payloadKinds } from './model.mjs'
import { decision, element, process, relation, reference, gate, constraint,
  mark, resetUndescribed, undescribedFound, resolveMarks, unmatchedMarks } from './type.mjs'
import { languageTypes } from '../language-types.mjs'
import { metaLanguageOf } from './meta.mjs'

export const contractDocument = () => {
  resetUndescribed()
  resolveMarks({ contract, architecture, setup, language })

  // Everything the architecture declares, generated types included, is a definition a
  // reference can resolve to.
  const elements = [...architecture.elements, ...languageTypes(language, setup)]
  const names = new Set(elements.map((e) => e.name))
  const defined = (n) => names.has(n)
  const declared = declaredIn(elements)

  const contractRecord = elements.find((e) => e.id === 'Contract')
  const states = elements.find((e) => e.id === 'DecisionState').members.map((m) => m.name)

  const decisions = (contract.decisions ?? []).map((d, i) =>
    decision(d, `contract.decisions[${i}]`, states, defined))

  // What the contract requires comes off the Contract record's own members.
  const fields = contractRecord.members
    .filter((f) => f.name !== 'decisions')
    .map((f) => ({ kind: 'field', name: f.name, value: contract[f.name] }))

  const groups = (contract.decision_groups ?? []).map((g) => ({
    kind: 'decisionGroup', id: g.id, name: g.name, question: g.question,
    decisions: decisions.filter((d) => d.group === g.id),
    extra: [],
  }))
  const orphans = decisions.filter((d) => !(contract.decision_groups ?? []).some((g) => g.id === d.group))

  // EVERY problem, as an item you can click, not four counts you have to go and find.
  // The old verdict said "7 of 9 phases not designed yet" and left the reader to scroll a
  // long page working out which seven.
  const problems = []
  const add = (group, label, anchor, tone = 'bad') => problems.push({ group, label, anchor, tone })

  for (const f of fields.filter((f) => !f.value)) {
    add('Required and not written', f.name, `field-${f.name}`)
  }
  for (const d of decisions.filter((d) => d.state === 'Proposed')) {
    add('Proposed, not agreed', d.slug, `dec-${d.slug}`, 'waiting')
  }
  for (const d of decisions.filter((d) => d.payload?.body?.kind === 'sentence')) {
    add('Words only', d.slug, `dec-${d.slug}`)
  }
  const run = decisions.find((d) => d.payload?.body?.kind === 'process')
  const phases = run?.payload?.body?.members ?? []
  for (const ph of phases.filter((p) => p.undesigned)) {
    add('Phases not designed', ph.name, `proc-${ph.name}`)
  }
  const seenResearch = new Set()
  const walkResearch = (p) => {
    for (const r of p.research ?? []) {
      if (!r.hasProcess && r.spec && !seenResearch.has(r.spec.id)) {
        seenResearch.add(r.spec.id)
        add('Research with no steps', r.spec.name, `res-${r.spec.id}`)
      }
    }
    for (const m of p.members ?? []) if (m.kind === 'process') walkResearch(m)
  }
  if (run) walkResearch(run.payload.body)
  // What the language says a contract must carry. A requirement naming a payload kind the
  // architecture does not declare is itself a problem — the contract could not carry one
  // even if someone wrote it.
  for (const req of language.contract_must_carry ?? []) {
    if (!payloadKinds.includes(req.payload)) {
      add('Required decision cannot exist yet', `${req.payload} is not a declared payload kind`, null)
    } else if (!decisions.some((d) => d.payload?.type === req.payload)) {
      add('Required decision not written', req.payload, null)
    }
  }
  // a part nobody has written at all, listed with everything else that is wrong
  const notStated = []
  const findNotStated = (n, where) => {
    if (Array.isArray(n)) return n.forEach((x) => findNotStated(x, where))
    if (!n || typeof n !== 'object') return
    if (n.kind === 'notStated') notStated.push(`${n.part} on a ${n.device}`)
    const here = n.kind === 'process' || n.kind === 'step' ? (n.name ?? where) : where
    for (const [k, v] of Object.entries(n)) if (k !== 'spec') findNotStated(v, here)
  }
  findNotStated(decisions, null)
  const byPart = {}
  for (const n of notStated) byPart[n] = (byPart[n] ?? 0) + 1
  for (const [label, n] of Object.entries(byPart)) {
    add('Never stated', n > 1 ? `${label} ×${n}` : label, null, 'never')
  }

  for (const u of unmatchedMarks()) {
    add('Highlight names nothing', u.path, null)
  }
  for (const u of undescribedFound()) {
    add('Could not be described', u.why, null)
  }

  const blocks = []
  const unwritten = fields.filter((f) => !f.value)
  if (unwritten.length) blocks.push(`${unwritten.length} required field${unwritten.length === 1 ? '' : 's'} not written`)
  const proposed = decisions.filter((d) => d.state === 'Proposed')
  if (proposed.length) blocks.push(`${proposed.length} decision${proposed.length === 1 ? '' : 's'} still proposed`)
  const wordsOnly = decisions.filter((d) => d.payload?.body?.kind === 'sentence')
  if (wordsOnly.length) blocks.push(`${wordsOnly.length} decision${wordsOnly.length === 1 ? '' : 's'} are words only`)
  const undone = phases.filter((p) => p.undesigned)
  if (undone.length) blocks.push(`${undone.length} of ${phases.length} phases not designed yet`)

  const body = [
    { kind: 'section', title: 'What this run covers',
      question: 'What is settled, and what has not been written?', body: fields },
    { kind: 'section', title: 'The decisions',
      question: 'What has the human agreed to, and what is still waiting?',
      body: [...groups, ...orphans] },
  ]

  const doc = {
    kind: 'document',
    eyebrow: `AgentGuard · issue ${contract.issue}`,
    title: contract.slug,
    footer: 'Generated from contract.yaml. The YAML is the source; never edit the HTML.',
    body,
  }

  // Every definition a reference can open, drawn once into a hidden block. A reference
  // that offers to open something must have something to open, so these are built from the
  // same elements the references resolve against.
  const definitions = [
    ...elements.map((e) => ({ name: e.name, node: element(e, `architecture.${e.id}`, defined, declared) })),
    ...(setup.research ?? []).filter((r) => r.process)
      .map((r) => ({ name: r.name, node: process(r.process, `setup.research.${r.id}`, defined) })),
  ]

  // Counted after the page is built, so it is the real number.
  const verdict = { kind: 'verdict', blocks, problems, undescribed: undescribedFound().length }
  doc.body = [verdict, ...body]
  return { doc: doc, definitions, undescribed: undescribedFound(), blocks }
}

// ── shared pieces ────────────────────────────────────────────────────────────────
// Everything the architecture declares, generated types included.
const allElements = () => [...architecture.elements, ...languageTypes(language, setup)]
const definedIn = (els) => { const n = new Set(els.map((e) => e.name)); return (x) => n.has(x) }
// How many type parameters a named element declares, so a reference can be checked
// against it. Null for a name this architecture does not own.
const declaredIn = (els) => {
  const by = Object.fromEntries(els.map((e) => [e.name, (e.parameters ?? []).length]))
  return (x) => (x in by ? by[x] : null)
}

// Every definition a reference can open. A reference that offers to open something must
// have something to open.
const definitionsFrom = (els, defined, declared) => [
  ...els.map((e) => ({ name: e.name, node: element(e, `architecture.${e.id}`, defined, declared) })),
  ...(setup.research ?? []).filter((r) => r.process)
    .map((r) => ({ name: r.name, node: process(r.process, `setup.research.${r.id}`, defined) })),
]

const card = (x, opts = {}) => ({
  kind: 'vocabCard', name: x.name ?? x.id, description: x.description,
  example: x.example, mark: x.mark, tone: x.tone,
  tags: opts.tags ? opts.tags(x) : undefined,
  rows: opts.rows ? opts.rows(x) : undefined,
  extra: [],
})
const grid = (items) => ({ kind: 'vocabGrid', items })
// A declared device, shown as the parts it is made of, each saying whether it is required.
const parts = (device) => grid((device?.parts ?? []).map((p) => ({
  kind: 'vocabCard', name: p.id, description: p.description,
  tags: p.required === false ? ['optional'] : undefined, extra: [],
})))

// ── the architecture document ────────────────────────────────────────────────────
export const architectureDocument = () => {
  resetUndescribed()
  resolveMarks({ contract, architecture, setup, language })
  const els = allElements()
  const defined = definedIn(els)
  const declared = declaredIn(els)
  const byId = Object.fromEntries(els.map((e) => [e.id, e]))

  const designed = els.filter((e) => !e.generated)
  const generated = els.filter((e) => e.generated)
  const rels = (architecture.relations ?? []).map((r, i) =>
    relation(r, `architecture.relations[${i}]`, defined, byId))

  const body = [
    { kind: 'section', id: 'proposed', title: 'What this proposes',
      question: 'What does this proposal put in the code?',
      body: designed.map((e) => element(e, `architecture.${e.id}`, defined, declared)) },
    { kind: 'section', id: 'generated', title: 'Generated from the language',
      question: 'What is derived rather than designed?',
      body: generated.map((e) => element(e, `architecture.${e.id}`, defined, declared)) },
    { kind: 'section', id: 'relations', title: 'How it connects',
      question: 'What links one element to another?', body: rels },
  ]
  const doc = {
    kind: 'document',
    eyebrow: `AgentGuard · issue ${architecture.proposal.issue}`,
    title: architecture.proposal.slug,
    sub: architecture.proposal.statement,
    footer: 'Generated from architecture.yaml. The YAML is the source; never edit the HTML.',
    body: [{ kind: 'verdict', undescribed: undescribedFound().length }, ...body],
  }
  return { doc, definitions: definitionsFrom(els, defined, declared) }
}

// ── the language document ────────────────────────────────────────────────────────
export const languageDocument = () => {
  resetUndescribed()
  resolveMarks({ contract, architecture, setup, language })
  const els = allElements()
  const defined = definedIn(els)
  const declared = declaredIn(els)
  const m = language

  const permissions = {
    kind: 'permissions',
    verbs: m.permission_verbs,
    actors: m.actors,
    cells: Object.fromEntries((setup.constraints ?? [])
      .filter((c) => c.kind === 'permission')
      .map((c) => [`${c.actor}|${c.verb}`, c])),
  }

  const body = [
    { kind: 'section', id: 'core', title: 'Core',
      question: 'What can a structural statement be about at all?',
      body: [grid(m.core.map((c) => card(c)))] },
    { kind: 'section', id: 'levels', title: 'Levels',
      question: 'Where does a thing live, and what can live inside it?',
      body: [{ kind: 'nest', items: m.levels }] },
    { kind: 'section', id: 'forms', title: 'Forms',
      question: 'What sort of construct is this, and how is it drawn?',
      body: [grid(m.forms.map((f) => card(f)))] },
    { kind: 'section', id: 'members', title: 'Members',
      question: 'What does a type declare?',
      body: [grid(m.members.map((x) => card(x)))] },
    { kind: 'section', id: 'visibility', title: 'Visibility',
      question: 'How far out can this be seen?',
      body: [{ kind: 'rings', items: m.visibility },
        grid(m.visibility.map((v) => card(v)))] },
    { kind: 'section', id: 'alteration', title: 'Alteration',
      question: 'What is this proposal doing to the code as it stands?',
      body: [grid(m.alteration.map((a) => card(a)))] },
    { kind: 'section', id: 'naming', title: 'Naming a type',
      question: 'How does one element name another?',
      body: [parts(m.type_reference),
        { kind: 'figure', caption: 'Each case, drawn',
          body: [
            reference({ name: 'Contract' }, 'figure', defined),
            reference({ name: 'string' }, 'figure', defined),
            reference({ name: 'IReadOnlyList', arguments: [{ name: 'Decision', arguments: [{ name: 'Payload' }] }] }, 'figure', defined),
            reference({ name: 'Acceptance' }, 'figure', defined),
          ] }] },
    { kind: 'section', id: 'relations', title: 'Relations',
      question: 'What links one element to another?',
      body: [grid(m.relations.map((r) => card(r)))] },
    { kind: 'section', id: 'dimensions', title: 'Dimensions',
      question: 'What other ways can these same elements be grouped?',
      body: [grid(m.dimensions.map((d) => card(d, { tags: (x) => [x.ordered ? 'ordered' : 'unordered'] })))] },
    { kind: 'section', id: 'storage', title: 'Storage and precision',
      question: 'Where is a record kept, and how exactly does it pin its types?',
      body: [grid(m.storage.map((x) => card(x))), grid(m.precision.map((x) => card(x)))] },
    { kind: 'section', id: 'actors', title: 'Actors',
      question: 'Who can run a step, and what can each of them actually do?',
      body: [grid(m.actors.map((a) => card(a)))] },
    { kind: 'section', id: 'research', title: 'Research, as this project stands it up',
      question: 'Which kinds does this project have, and which have steps of their own?',
      body: [grid(setup.research.map((r) => card(r, {
        tags: (x) => [x.where, ...(x.must_run ?? []).map((l) => vocab.lens[l]?.name ?? l),
          ...(x.process ? [] : ['not designed yet'])].filter(Boolean),
      }))), grid(m.lenses.map((l) => card(l)))] },
    { kind: 'section', id: 'process', title: 'Processes',
      question: 'What happens, in what order, and who does it?',
      body: [parts(m.process), parts(m.step)] },
    { kind: 'section', id: 'gate', title: 'Gates',
      question: 'What must hold before a process starts, and before it finishes?',
      body: [parts(m.gate), grid(m.gate_kinds.map((k) => card(k))),
        grid(m.gate_condition_kinds.map((k) => card(k, {
          tags: (x) => (x.parts ?? []).map((p) => p.id + (p.required === false ? ' (optional)' : '')) }))),
        { kind: 'figure', caption: 'One condition of every kind, drawn',
          body: [gate({ kind: 'exit', conditions: m.gate_condition_kinds.map((k) => ({
            kind: k.id, over: 'what this condition is over',
            ...((k.parts ?? []).some((p) => p.id === 'by') ? { by: 'what satisfies it' } : {}),
          })) }, 'figure')] }] },
    { kind: 'section', id: 'command_set', title: 'Command sets',
      question: 'What does a human or an agent type to change a record?',
      body: [parts(m.command_set)] },
    { kind: 'section', id: 'constraints', title: 'Constraints',
      question: 'What kinds of constraint exist, and which does this project stand up?',
      body: [grid(m.constraint_kinds.map((k) => card(k, {
        tags: (x) => (x.parts ?? []).map((p) => p.id + (p.required === false ? ' (optional)' : '')) }))),
        permissions,
        ...(setup.constraints ?? []).filter((c) => c.kind !== 'permission')
          .map((c, i) => constraint(c, `setup.constraints[${i}]`))] },
    { kind: 'section', id: 'prose', title: 'Prose',
      question: 'How much text may sit in each place, and why does it exist?',
      body: [grid(m.prose.map((x) => card({ ...x, name: x.id, description: x.purpose },
        { tags: (y) => [`${y.words} words`],
          rows: (y) => [{ kind: 'pair', key: 'holds', value: y.holds },
            { kind: 'pair', key: 'never', value: y.never }] })))] },
  ]

  const doc = {
    kind: 'document', eyebrow: 'AgentGuard', title: 'The architecture language',
    sub: 'A project describes its structure in this language so the human can read, accept and sign it.',
    footer: 'Generated from language.yaml. The YAML is the source; never edit the HTML.',
    body: [{ kind: 'verdict', undescribed: undescribedFound().length }, ...body],
  }
  return { doc: doc, definitions: definitionsFrom(els, defined, declared) }
}


// ── the metalanguage document ────────────────────────────────────────────────────
// The devices are grouped by WHAT THEY ARE, read off each declaration rather than listed
// here: a leaf carries a value, a closed vocabulary carries its instances, a concept
// carries parts. A device that is none of those three lands in the last group, which is
// why that group exists rather than being left out as impossible.
export const metaLanguageDocument = () => {
  resetUndescribed()
  const m = metaLanguageOf(metaLanguage)

  // Every device, in the order the file declares them. The grouping that was here —
  // concepts, closed vocabularies, leaves — was a reading of the file rather than the
  // file, and its four headings and four questions were words nobody had written down.
  const body = [
    ...(m.pinned.length || m.rootGaps.length || m.unreachable.length
      ? [{ kind: 'section', id: 'selfcheck', title: 'The self-check',
          question: 'Is this file a valid instance of itself?',
          body: [...m.pinned, ...m.rootGaps, ...m.unreachable] }]
      : []),
    { kind: 'metaIndex', devices: m.devices },
    ...m.legend,
    ...m.devices,
  ]

  const doc = {
    kind: 'document', eyebrow: 'AgentGuard', title: 'The metalanguage',
    sub: metaLanguage.description,
    footer: 'Generated from meta-language.yaml. The YAML is the source; never edit the HTML.',
    body: [{ kind: 'verdict', undescribed: undescribedFound().length }, ...body],
  }
  return { doc, definitions: [] }
}

export const renderContract = () => {
  const { doc, definitions, undescribed, blocks } = contractDocument()
  return {
    element: (
      <>
        <Draw node={doc} />
        <div id="defs">
          {definitions.map((d) => (
            <div data-defname={d.name} key={d.name}><Draw node={d.node} /></div>
          ))}
        </div>
      </>
    ),
    undescribed, blocks,
  }
}

export const DOCUMENTS = {
  contract: contractDocument,
  architecture: architectureDocument,
  language: languageDocument,
  metaLanguage: metaLanguageDocument,
}
