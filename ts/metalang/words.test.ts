// The vocabulary, and the reader that resolves one from a document.
//
// The reader is the reason this file matters. It is what makes the language file the only copy
// of the vocabulary, so every way it can be given something it cannot read is exercised here
// rather than trusted, and the live file is resolved and checked word for word.

import { readFileSync } from 'node:fs'

import * as yaml from 'js-yaml'

import { describe, expect, test } from 'bun:test'

import { LANGUAGE_FILE, WORDS } from './rulebook.ts'
import {
  BOOTSTRAP,
  declaredBodiesOf,
  isMapping,
  onlyWord,
  optionsInOrder,
  propertiesCarrying,
  propertiesOf,
  propertyCarrying,
  resolveVocabulary,
  ROLES,
  sameValue,
  SELF,
  valuesOf,
  type BodyOf,
  type Mapping,
} from './words.ts'

const resolve = (declare: unknown[]): ReturnType<typeof resolveVocabulary> =>
  resolveVocabulary({ declare })

const entity = (body: Record<string, unknown>): unknown => ({ entity: body })
const choice = (id: string, options: unknown): unknown => ({ choice: { id, options } })

/** The least a document can say and still name a kind that may be written plainly. */
const kinds = (...named: string[]): unknown[] => [
  ...named.map((id) => entity({ id, properties: { id: { is: 'name' } } })),
  choice(SELF.declaration, named),
]

describe('the vocabulary the live language file declares', () => {
  test('the top level is the properties of the entity the file is', () => {
    expect(WORDS.topLevel).toEqual([
      'scope',
      'import',
      'language',
      'title',
      'description',
      'declare',
    ])
    expect(WORDS.entities[SELF.document]).toEqual(WORDS.topLevel)
  })

  test('a kind is written plainly or inside external, and two choices say which', () => {
    expect(WORDS.places.declare).toEqual([
      'entity',
      'enum',
      'choice',
      'constant',
      'rule',
      'fact',
    ])
    expect(WORDS.places.external).toEqual(['cast'])
  })

  test('external is an option of a declaration, so it is written in the one stream', () => {
    expect(WORDS.choices[SELF.declaration]).toContain(BOOTSTRAP.external)
  })

  test('the kinds are every kind in both places, once each', () => {
    expect(WORDS.kinds).toEqual([
      'entity',
      'enum',
      'choice',
      'constant',
      'rule',
      'fact',
      'cast',
    ])
  })

  test('each kind carries the properties of the entity that declares it', () => {
    const prose = ['title', 'description']
    expect(WORDS.onKind).toEqual({
      entity: ['id', 'is', ...prose, 'abstract', 'primitive', 'constraints', 'properties'],
      enum: ['id', 'is', ...prose, 'values'],
      choice: ['id', ...prose, 'options'],
      constant: ['id', 'is', ...prose, 'value'],
      rule: ['id', ...prose],
      fact: ['id', ...prose],
      cast: ['id', ...prose, 'from', 'to', 'implicit'],
    })
  })

  test('a kind approved with its keywords undecided is not a kind carrying none', () => {
    const { words } = resolveVocabulary({
      declare: [
        ...kinds('entity'),
        entity({ id: 'cast' }),
        entity({ id: 'fact', properties: {} }),
        choice(SELF.external, ['cast', 'fact']),
      ],
    })
    expect(words.onKind.cast).toBeNull()
    expect(words.onKind.fact).toEqual([])
    expect('cast' in words.onKind).toBe(true)
  })

  test('a property carries its type, its prose, whether it may be left out, what it holds, the reserved states it takes, and what it is to a reader', () => {
    expect(WORDS.onProperty).toEqual([
      'is',
      'title',
      'description',
      'optional',
      'value',
      'accepts',
      'role',
    ])
    expect(WORDS.onValue).toEqual(['id', 'title', 'description', 'value'])
  })

  test('which kinds are types is stated, so a name under is can be judged', () => {
    expect(WORDS.types).toEqual(['entity', 'enum', 'choice'])
  })

  test('the four collection kinds take what they hold from the one they derive from', () => {
    expect(WORDS.entities.collection).toEqual(['of', 'ordered', 'unique'])
    for (const kind of ['list', 'array', 'set', 'bag']) {
      expect(WORDS.entities[kind]).toEqual(['of', 'ordered', 'unique'])
    }
  })

  test('the built-in types are entities, and carry nothing of their own', () => {
    for (const named of ['object', 'boolean', 'number', 'string', 'name']) {
      expect(WORDS.entities[named]).toBeNull()
    }
  })

  test('a type carrying constraints carries no properties for them', () => {
    for (const named of ['byte', 'uuid', 'expression']) {
      expect(named in WORDS.entities).toBe(true)
      expect(WORDS.entities[named]).toBeNull()
    }
  })

  test('each kind of constraint carries what it holds', () => {
    expect(WORDS.entities.constraint).toBeNull()
    expect(WORDS.entities.pattern).toEqual(['value'])
    expect(WORDS.entities.range).toEqual(['minimum', 'maximum'])
  })

  test('the language file is read as written, with nothing it could not read', () => {
    const doc = yaml.load(readFileSync(LANGUAGE_FILE, 'utf8'))
    expect(resolveVocabulary(doc).faults).toEqual([])
  })

  test('every property a kind carries is itself declared in the file', () => {
    const declared = new Set<string>()
    const doc = yaml.load(readFileSync(LANGUAGE_FILE, 'utf8'))
    for (const item of (isMapping(doc) && Array.isArray(doc.declare) ? doc.declare : []) as unknown[]) {
      const named = onlyWord(item)
      if (named !== null && isMapping(named[1]) && typeof named[1].id === 'string') {
        declared.add(named[1].id)
      }
    }
    for (const kind of WORDS.kinds) expect(declared.has(kind)).toBe(true)
    for (const named of Object.keys(WORDS.entities)) expect(declared.has(named)).toBe(true)
  })
})

describe('the words the reader knows before it reads anything', () => {
  test('are the words the vocabulary itself is written in', () => {
    expect(Object.values(BOOTSTRAP)).toEqual([
      'declare',
      'external',
      'entity',
      'choice',
      'id',
      'is',
      'properties',
      'options',
    ])
  })

  test('every one of them is declared in the file as well, so nothing is known and unstated', () => {
    const stated = new Set([...Object.keys(WORDS.entities), ...Object.keys(WORDS.choices)])
    for (const carried of Object.values(WORDS.entities).flatMap((c) => c ?? [])) stated.add(carried)
    for (const known of Object.values(BOOTSTRAP)) expect(stated.has(known)).toBe(true)
  })

  test('the declarations it looks up by name each say something about the language', () => {
    expect(Object.values(SELF)).toEqual([
      'document',
      'declaration',
      'external',
      'type',
      'property',
      'value',
      'object',
      'collection',
      'link',
      'reserved',
    ])
    for (const named of [SELF.document, SELF.property, SELF.value, SELF.object, SELF.collection, SELF.link]) {
      expect(named in WORDS.entities).toBe(true)
    }
    for (const named of [SELF.declaration, SELF.external, SELF.type]) {
      expect(named in WORDS.choices).toBe(true)
    }
  })
})

describe('which kinds are types', () => {
  test('comes from the choice the document declares under that name', () => {
    const { words, faults } = resolve([
      ...kinds('entity', 'enum', 'constant'),
      choice(SELF.type, ['entity', 'enum']),
    ])
    expect(faults).toEqual([])
    expect(words.types).toEqual(['entity', 'enum'])
  })

  test('a document that does not declare it says nothing about which kinds are types', () => {
    expect(resolve(kinds('entity')).words.types).toEqual([])
  })

  test('a choice under any other name is not it', () => {
    const { words } = resolve([...kinds('entity'), choice('other', ['entity'])])
    expect(words.types).toEqual([])
  })

  test('listing something that is not a kind is a fault', () => {
    const { faults } = resolve([choice(SELF.type, ['entity'])])
    expect(faults).toEqual(['"type" lists "entity", which is not a kind'])
  })

  test('options that are not names is a fault', () => {
    const { faults } = resolve([choice(SELF.type, 'entity')])
    expect(faults[0]).toContain('lists its options as names')
  })
})

describe('resolving a vocabulary from a document', () => {
  test('a document that declares nothing resolves to a vocabulary holding nothing', () => {
    const { words, faults } = resolveVocabulary({})
    expect(faults).toEqual([])
    expect(words.kinds).toEqual([])
    expect(words.topLevel).toEqual([])
    expect(words.entities).toEqual({})
    expect(words.choices).toEqual({})
  })

  test('something other than a mapping resolves to nothing rather than throwing', () => {
    expect(resolveVocabulary(null).words.kinds).toEqual([])
    expect(resolveVocabulary([1]).words.kinds).toEqual([])
  })

  test('a declare that is not a list resolves to nothing, and says so', () => {
    expect(resolveVocabulary({ declare: 'entity' }).words.kinds).toEqual([])
  })

  test('a declaration that is neither an entity nor a choice is kept and not read further', () => {
    const { words, faults } = resolve([{ constant: { id: 'Pi' } }])
    expect(faults).toEqual([])
    expect(words.entities).toEqual({})
    expect(words.declared.Pi?.kind).toBe('constant')
  })

  test('a declaration that cannot be kept is a fault whatever its kind, never dropped', () => {
    const { faults } = resolve(['entity', {}, { a: {}, b: {} }, { external: 7 }, { constant: 7 }])
    expect(faults).toHaveLength(5)
    expect(faults[0]).toContain('exactly one kind')
    expect(faults[3]).toContain('external')
    expect(faults[4]).toContain('a constant is a mapping')
  })

  test('a constant with no id or a repeated id is a fault', () => {
    const { faults } = resolve([{ constant: {} }, { constant: { id: 'P' } }, { constant: { id: 'P' } }])
    expect(faults[0]).toContain('declares its name as its id')
    expect(faults[1]).toContain('"P" is declared twice')
  })

  test('what an entity derives from or a scope names that is not text is a fault', () => {
    expect(resolveVocabulary({ scope: ['a', 7], declare: [] }).faults[0]).toContain('scope')
    expect(resolve([{ entity: { id: 'x', is: [7] } }]).faults[0]).toContain('derives from')
  })

  test('declarations written as something other than a list are a fault', () => {
    expect(resolveVocabulary({ declare: 'entity' }).faults[0]).toContain('a list')
  })

  test('a declaration written inside external is read, because external says where it is built', () => {
    const { words } = resolve([
      { external: { entity: { id: 'cast' } } },
      choice(SELF.external, ['cast']),
    ])
    expect(words.places.external).toEqual(['cast'])
    expect(words.entities.cast).toBeNull()
  })

  test('an entity with no properties is undecided, not carrying nothing', () => {
    expect(resolve([entity({ id: 'cast' })]).words.entities.cast).toBeNull()
  })

  test('an entity writing an empty properties carries nothing, which is not undecided', () => {
    expect(resolve([entity({ id: 'k', properties: {} })]).words.entities.k).toEqual([])
  })

  test('what an entity derives from is folded in, and its own properties come last', () => {
    const { words } = resolve([
      entity({ id: 'base', properties: { of: {} } }),
      entity({ id: 'mine', is: 'base', properties: { extra: {} } }),
    ])
    expect(words.entities.base).toEqual(['of'])
    expect(words.entities.mine).toEqual(['of', 'extra'])
  })

  test('deriving from several types folds in each of them, once each', () => {
    const { words } = resolve([
      entity({ id: 'one', properties: { a: {} } }),
      entity({ id: 'two', properties: { a: {}, b: {} } }),
      entity({ id: 'both', is: ['one', 'two'] }),
    ])
    expect(words.entities.both).toEqual(['a', 'b'])
  })

  test('deriving from something undecided leaves the deriving one undecided too', () => {
    const { words } = resolve([entity({ id: 'base' }), entity({ id: 'mine', is: 'base' })])
    expect(words.entities.mine).toBeNull()
  })

  test('deriving from something declared nowhere is left to the checker, not read as a fault', () => {
    const { words, faults } = resolve([entity({ id: 'mine', is: 'absent', properties: { a: {} } })])
    expect(faults).toEqual([])
    expect(words.entities.mine).toEqual(['a'])
  })

  test('a type that derives from itself is read rather than followed forever', () => {
    // The walk stops where it comes back round, so the file is still read. Which type derives from
    // itself is the checker's to report, not the reader's.
    const { words, faults } = resolve([
      entity({ id: 'a', is: 'b', properties: { x: {} } }),
      entity({ id: 'b', is: 'a', properties: { y: {} } }),
    ])
    expect(faults).toEqual([])
    expect(words.entities.a).toContain('x')
    expect(words.entities.b).toContain('y')
  })

  test('a kind written in both places is still one kind', () => {
    const { words } = resolve([
      entity({ id: 'rule', properties: { id: {} } }),
      choice(SELF.declaration, ['rule']),
      choice(SELF.external, ['rule']),
    ])
    expect(words.places.declare).toEqual(['rule'])
    expect(words.places.external).toEqual(['rule'])
    expect(words.kinds).toEqual(['rule'])
  })

  test('the order the top-level keys are written in is the order they come out in', () => {
    const { words } = resolve([entity({ id: SELF.document, properties: { b: {}, a: {} } })])
    expect(words.topLevel).toEqual(['b', 'a'])
  })
})

describe('what stops a vocabulary being read as written', () => {
  test('an entity that is not a mapping, and a choice that is not one', () => {
    expect(resolve([{ entity: 'id' }]).faults).toEqual(['declare[0]: an entity is a mapping'])
    expect(resolve([{ choice: 'id' }]).faults).toEqual(['declare[0]: a choice is a mapping'])
  })

  test('a declaration that declares no name, and one whose name is not text', () => {
    expect(resolve([entity({})]).faults[0]).toContain('declares its name as its id')
    expect(resolve([entity({ id: 7 })]).faults[0]).toContain('declares its name as its id')
    expect(resolve([entity({ id: '' })]).faults[0]).toContain('declares its name as its id')
  })

  test('one name declared twice, and the second one is not read', () => {
    const { words, faults } = resolve([
      entity({ id: 'a', properties: { x: {} } }),
      entity({ id: 'a', properties: { y: {} } }),
    ])
    expect(faults).toEqual(['declare[1]: "a" is declared twice'])
    expect(words.entities.a).toEqual(['x'])
  })

  test('properties written as anything but a mapping', () => {
    expect(resolve([entity({ id: 'a', properties: ['id'] })]).faults[0]).toContain(
      'writes its properties as a mapping',
    )
  })

  test('every fault is reported, not only the first', () => {
    const { faults } = resolve([entity({}), { choice: { id: 'a', options: 'x' } }])
    expect(faults).toHaveLength(2)
  })
})

// Every property a type has, its own and the ones it takes from above. The checker reads a value
// of a type with properties by this list, and the page draws the same list, so it is one walk.
describe('every property a type has', () => {
  const given = (declared: Record<string, Mapping>): BodyOf => (id) => declared[id]
  const names = (lookUp: BodyOf, id: string): string[] => [...propertiesOf(lookUp, id).keys()]

  test('a type declaring nothing has none', () => {
    expect(names(given({ bare: { id: 'bare' } }), 'bare')).toEqual([])
  })

  test('a type declared nowhere has none, rather than throwing', () => {
    expect(names(given({}), 'absent')).toEqual([])
  })

  test('its own properties come out in the order they are written', () => {
    const lookUp = given({ form: { properties: { mark: { is: 'string' }, name: { is: 'string' } } } })
    expect(names(lookUp, 'form')).toEqual(['mark', 'name'])
  })

  test('the properties of the type it derives from come first', () => {
    const lookUp = given({
      thing: { properties: { id: { is: 'name' } } },
      form: { is: 'thing', properties: { mark: { is: 'string' } } },
    })
    expect(names(lookUp, 'form')).toEqual(['id', 'mark'])
  })

  test('deriving from several types folds in each of them, once each', () => {
    const lookUp = given({
      left: { properties: { a: { is: 'string' } } },
      right: { properties: { b: { is: 'string' } } },
      both: { is: ['left', 'right'], properties: { c: { is: 'string' } } },
    })
    expect(names(lookUp, 'both')).toEqual(['a', 'b', 'c'])
  })

  test('a property written again takes everything it does not write from above', () => {
    const lookUp = given({
      thing: { properties: { mark: { is: 'string', title: 'The glyph.', optional: true } } },
      form: { is: 'thing', properties: { mark: { title: 'The form glyph.' } } },
    })
    expect(propertiesOf(lookUp, 'form').get('mark')).toEqual({
      is: 'string',
      title: 'The form glyph.',
      optional: true,
    })
  })

  test('a type that derives from itself is read rather than followed forever', () => {
    const lookUp = given({ loop: { is: 'loop', properties: { a: { is: 'string' } } } })
    expect(names(lookUp, 'loop')).toEqual(['a'])
  })

  test('a ring of two types is read rather than followed forever', () => {
    const lookUp = given({
      one: { is: 'two', properties: { a: { is: 'string' } } },
      two: { is: 'one', properties: { b: { is: 'string' } } },
    })
    expect(names(lookUp, 'one')).toEqual(['b', 'a'])
  })

  test('something written under is that is not a name is passed over', () => {
    const lookUp = given({ form: { is: [7, 'thing'], properties: { a: { is: 'string' } } } })
    expect(names(lookUp, 'form')).toEqual(['a'])
  })

  test('a properties that is not a mapping carries none', () => {
    expect(names(given({ form: { properties: ['mark'] } }), 'form')).toEqual([])
  })

  test('a property that is not a mapping is passed over', () => {
    const lookUp = given({ form: { properties: { mark: 'string', name: { is: 'string' } } } })
    expect(names(lookUp, 'form')).toEqual(['name'])
  })

  test('the live language file reads its own kinds this way', () => {
    const lookUp: BodyOf = (id) => WORDS.declared[id]?.body
    expect(names(lookUp, 'constant')).toEqual(['id', 'is', 'title', 'description', 'value'])
  })
})

// Which property of a type carries a role. It is asked here and nowhere else, so no renderer
// names a property of a language it draws, and a language spelling its words differently is
// drawn by the same code.
describe('which property of a type carries a role', () => {
  const given = (declared: Record<string, Mapping>): BodyOf => (id) => declared[id]

  const element = given({
    element: {
      id: 'element',
      properties: {
        form: { is: 'forms', role: 'kind' },
        does: { is: 'string', role: 'prose' },
        members: { is: 'string' },
      },
    },
    part: { id: 'part', is: 'element', properties: { file: { is: 'string' } } },
  })

  test('is the one the type gives it', () => {
    expect(propertyCarrying(element, 'element', ROLES.kind)).toBe('form')
    expect(propertyCarrying(element, 'element', ROLES.prose)).toBe('does')
  })

  test('is nothing where the type gives it to none, and the thing draws as it always did', () => {
    expect(propertyCarrying(element, 'element', ROLES.name)).toBeUndefined()
  })

  test('is taken from the type above, so a type deriving from one answers the same', () => {
    expect(propertyCarrying(element, 'part', ROLES.kind)).toBe('form')
  })

  test('a type declared nowhere carries none, rather than throwing', () => {
    expect(propertyCarrying(given({}), 'absent', ROLES.kind)).toBeUndefined()
  })

  // A role answers one question about a type once. Two properties carrying it is not an answer,
  // so nothing comes back and whatever asked says which two rather than picking one of them.
  test('two properties carrying one role is no answer at all', () => {
    const two = given({
      A: { id: 'A', properties: { one: { role: 'kind' }, two: { role: 'kind' } } },
    })
    expect(propertiesCarrying(two, 'A', ROLES.kind)).toEqual(['one', 'two'])
    expect(propertyCarrying(two, 'A', ROLES.kind)).toBeUndefined()
  })

  test('the live architecture language answers each of the three it uses', () => {
    const arch = yaml.load(
      readFileSync(LANGUAGE_FILE.replace('metalang.yaml', 'arch.yaml'), 'utf8'),
    )
    const bodies = declaredBodiesOf(arch)
    const bodyOf: BodyOf = (id) => bodies.get(id)
    expect(propertyCarrying(bodyOf, 'element', ROLES.kind)).toBe('form')
    expect(propertyCarrying(bodyOf, 'element', ROLES.prose)).toBe('does')
    expect(propertyCarrying(bodyOf, 'member', ROLES.kind)).toBe('kind')
    expect(propertyCarrying(bodyOf, 'form', ROLES.name)).toBe('name')
  })
})

describe('the order the options of a choice are asked in', () => {
  const given = (declared: Record<string, Mapping>): BodyOf => (id) => declared[id]
  const named = (lookUp: BodyOf, options: unknown[]): (string | null)[] =>
    optionsInOrder(lookUp, options).map((one) => one.named)

  // `colour` is an enum based on `text`, so every colour it lists is a text as well. Asking the
  // enum first is what makes a value it lists the enum and everything else the text underneath.
  const shades = given({
    text: { id: 'text', primitive: true },
    colour: { id: 'colour', is: 'text', values: [{ id: 'red', value: 'red' }] },
    deeper: { id: 'deeper', is: 'colour' },
    number: { id: 'number', primitive: true },
  })

  test('an option deriving from another is asked before it, however they were written', () => {
    expect(named(shades, ['text', 'colour'])).toEqual(['colour', 'text'])
    expect(named(shades, ['colour', 'text'])).toEqual(['colour', 'text'])
  })

  test('a whole chain comes out narrowest first', () => {
    expect(named(shades, ['text', 'deeper', 'colour'])).toEqual(['deeper', 'colour', 'text'])
  })

  test('options with nothing between them keep the order they were written', () => {
    expect(named(shades, ['text', 'number'])).toEqual(['text', 'number'])
    expect(named(shades, ['number', 'text'])).toEqual(['number', 'text'])
  })

  test('an option that is not a name is kept, and named nothing', () => {
    expect(named(shades, [7, 'text'])).toEqual([null, 'text'])
  })

  test('no options come out as no options', () => {
    expect(optionsInOrder(shades, [])).toEqual([])
  })
})

describe('the values an enum lists', () => {
  const given = (declared: Record<string, Mapping>): BodyOf => (id) => declared[id]

  test('each value comes out with the id naming it', () => {
    const lookUp = given({ c: { id: 'c', values: [{ id: 'red', value: 'r' }] } })
    expect(valuesOf(lookUp, 'c')).toEqual([{ id: 'red', value: 'r' }])
  })

  test('a declaration listing no values is not an enum', () => {
    expect(valuesOf(given({ c: { id: 'c' } }), 'c')).toBeNull()
  })

  test('a listed value with no id of its own is passed over', () => {
    const lookUp = given({ c: { id: 'c', values: ['red', { value: 'r' }, { id: 'blue' }] } })
    expect(valuesOf(lookUp, 'c')).toEqual([{ id: 'blue', value: undefined }])
  })
})

describe('whether two values written in a document are the same value', () => {
  test('the same text, number, true or false', () => {
    expect(sameValue('a', 'a')).toBe(true)
    expect(sameValue(7, 7)).toBe(true)
    expect(sameValue(true, true)).toBe(true)
    expect(sameValue('a', 'b')).toBe(false)
    expect(sameValue(7, '7')).toBe(false)
  })

  test('the same parts written in another order are the same value', () => {
    expect(sameValue({ a: 1, b: 2 }, { b: 2, a: 1 })).toBe(true)
  })

  test('a part the other does not carry makes them different', () => {
    expect(sameValue({ a: 1, b: 2 }, { a: 1 })).toBe(false)
    expect(sameValue({ a: 1 }, { b: 1 })).toBe(false)
  })

  test('a list is the same only in the order it is written, because a list is ordered', () => {
    expect(sameValue([1, 2], [1, 2])).toBe(true)
    expect(sameValue([1, 2], [2, 1])).toBe(false)
    expect(sameValue([1, 2], [1])).toBe(false)
  })

  test('parts inside parts are compared the same way all the way down', () => {
    expect(sameValue({ a: { b: [1, { c: 2 }] } }, { a: { b: [1, { c: 2 }] } })).toBe(true)
    expect(sameValue({ a: { b: [1, { c: 2 }] } }, { a: { b: [1, { c: 3 }] } })).toBe(false)
  })

  test('a mapping and a list are never the same value', () => {
    expect(sameValue({ 0: 'a' }, ['a'])).toBe(false)
  })
})
