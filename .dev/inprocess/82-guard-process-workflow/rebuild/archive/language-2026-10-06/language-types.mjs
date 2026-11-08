// The payload types, GENERATED from language.yaml.
//
// An Architecture holds Elements, Relations and Constraints; a Process holds Steps,
// research and questions; a CommandSet holds Verbs. Those are the language's own
// vocabulary reflected as data. Hand-writing them would put the same list in two files
// that can drift: the language would say five research kinds and the enum would say
// five, with nothing checking they agree. They are derived here, so there is one source.
//
// Nothing in this file names a form, an actor, a research kind or a part. If
// language.yaml gains one, the type gains it and every document redraws. A part with no
// type here fails the render rather than generating a record that quietly omits it.
// The language is passed in, so this file depends on the DATA and not on any
// particular renderer. Both the old renderers and the new view hand it the same model.

const enumOf = (id, purpose, items) => ({
  id,
  name: id,
  form: 'enum',
  visibility: 'public',
  alteration: 'inserted',
  generated: true,
  purpose,
  members: items.map((x) => ({
    name: x.id,
    kind: 'value',
    visibility: 'public',
    note: x.name ?? '',
    alteration: 'inserted',
  })),
})

const field = (name, type) => ({
  name, kind: 'field', visibility: 'public', type, alteration: 'inserted',
})

const recordOf = (id, purpose, members) => ({
  id, name: id, form: 'record', visibility: 'public',
  alteration: 'inserted', generated: true, purpose, members,
})

const list = (of) => ({ name: 'IReadOnlyList', arguments: [{ name: of }] })

// The fields of a record generated from a declared device: one per part the language
// declares, named and typed by `types`. A part the language declares and this table does
// not type is an error, so adding a part to the YAML cannot silently drop it here.
const partsOf = (device, label, types, extra = []) => {
  const parts = device.parts ?? []
  const out = []
  for (const p of parts) {
    const spec = types[p.id]
    if (!spec) throw new Error(`language-types: ${label} part "${p.id}" has no type`)
    out.push(field(spec.name ?? p.id, spec.type))
  }
  return [...extra, ...out]
}

export const languageTypes = (m, setup) => [
  enumOf('Form', 'What a thing is.', m.forms),
  enumOf('Visibility', 'How far out a thing can be seen.', m.visibility),
  enumOf('Alteration', 'What a proposal does to a thing.', m.alteration),
  enumOf('ConstraintKind', 'Which shape a constraint has.', m.constraint_kinds),
  enumOf('PermissionVerb', 'What an actor may do.', m.permission_verbs),
  enumOf('Actor', 'Who can run a step.', m.actors),
  enumOf('ResearchKind', 'What a process can be required to find out.', setup.research),
  enumOf('Lens', 'How a research kind looks for something.', m.lenses),
  enumOf('GateKind', 'When a gate is checked.', m.gate_kinds),
  enumOf('GateConditionKind', 'What a gate condition checks.', m.gate_condition_kinds),

  recordOf('TypeReference', 'How one element names another, anywhere a type appears.',
    partsOf(m.type_reference, 'type_reference', {
      name: { type: 'string' },
      arguments: { type: list('TypeReference') },
    })),

  recordOf('ResearchNeed', 'One kind of research a process needs, and whether it may be skipped.', [
    field('kind', { name: 'ResearchKind' }),
    field('required', 'bool'),
  ]),

  recordOf('Question', 'Something a process must answer, and the research that answers it.', [
    field('id', 'string'),
    field('text', 'string'),
    field('answeredBy', list('ResearchKind')),
  ]),

  {
    id: 'ProcessMember', name: 'ProcessMember', form: 'interface', visibility: 'public',
    alteration: 'inserted', generated: true,
    purpose: 'A member of a process. A Step when one actor does it, a Process when it has members of its own.',
    members: [],
  },

  recordOf('GateCondition', 'One thing a gate checks, and what satisfies it.', [
    field('kind', { name: 'GateConditionKind' }),
    field('over', 'string'),
    field('by', 'string'),
  ]),

  recordOf('Repeat', 'What makes a step or a process run again, and what stops it.',
    partsOf(m.repeat, 'repeat', {
      until: { type: list('GateCondition') },
      at_most: { name: 'atMost', type: 'number' },
    })),

  recordOf('Gate', 'What must hold before a process may start, or before it may finish.',
    partsOf(m.gate, 'gate', {
      kind: { type: { name: 'GateKind' } },
      conditions: { type: list('GateCondition') },
    })),

  recordOf('Step', 'One unit of work inside a process, run by exactly one actor.',
    partsOf(m.step, 'step', {
      name: { type: 'string' },
      note: { type: 'string' },
      actor: { type: { name: 'Actor' } },
      takes: { type: list('TypeReference') },
      produces: { type: list('TypeReference') },
      fans_out: { name: 'fansOut', type: 'string' },
      repeats: { type: { name: 'Repeat' } },
    })),

  recordOf('Process', 'A named, ordered sequence whose members are steps or processes.',
    partsOf(m.process, 'process', {
      name: { type: 'string' },
      note: { type: 'string' },
      members: { type: list('ProcessMember') },
      takes: { type: list('TypeReference') },
      produces: { type: list('TypeReference') },
      research: { type: list('ResearchNeed') },
      questions: { type: list('Question') },
      gates: { type: list('Gate') },
      repeats: { type: { name: 'Repeat' } },
    })),

  recordOf('Verb', 'One operation in a command set, with what it takes and returns.', [
    field('id', 'string'),
    field('takes', 'string'),
    field('returns', 'string'),
  ]),

  recordOf('CommandSet', 'The commands that read and change one part of a record.',
    partsOf(m.command_set, 'command_set', {
      over: { type: { name: 'TypeReference' } },
      prefix: { type: 'string' },
      verb: { name: 'verbs', type: list('Verb') },
    })),

  recordOf('DecisionGroup', 'An ordered band of decisions, named by the question it answers.',
    partsOf(m.decision_group, 'decision_group', {
      id: { type: 'string' },
      name: { type: 'string' },
      question: { type: 'string' },
    })),

  recordOf('Element', 'One named thing in an architecture, with where it lives and what it is.', [
    field('id', 'string'), field('name', 'string'),
    field('form', { name: 'Form' }), field('visibility', { name: 'Visibility' }),
    field('unit', 'string'), field('scope', 'string'),
    field('alteration', { name: 'Alteration' }),
  ]),

  recordOf('Relation', 'A directed link between two elements, or between their members.', [
    field('kind', 'string'), field('from', 'string'), field('to', 'string'),
    field('alteration', { name: 'Alteration' }),
  ]),

  recordOf('Constraint', 'Something a judge must watch for, because no rule enforces it.', [
    field('kind', { name: 'ConstraintKind' }), field('text', 'markdown'),
  ]),

  recordOf('Rule', 'Built code that refuses something, with a test proving it fires.',
    partsOf(m.rule, 'rule', {
      id: { type: 'string' },
      forbids: { type: 'markdown' },
      built_as: { name: 'builtAs', type: 'string' },
      proven_by: { name: 'provenBy', type: 'string' },
      mechanises: { type: 'string' },
    })),

  recordOf('Spec', 'What a behaviour-level test must prove.',
    partsOf(m.spec, 'spec', {
      id: { type: 'string' },
      proves: { type: 'markdown' },
      over: { type: { name: 'TypeReference' } },
      when: { type: 'string' },
    })),

  recordOf('UnitTest', 'What one test asserts about one piece of the architecture.',
    partsOf(m.unit_test, 'unit_test', {
      id: { type: 'string' },
      proves: { type: 'markdown' },
      over: { type: { name: 'TypeReference' } },
      from_spec: { name: 'fromSpec', type: 'string' },
    })),
]
