// The checker.
//
// Two lists come out of it and they mean different things. A PROBLEM is wrong with the
// document. A BLOCKER is something the checker cannot verify because the language has no word
// for it yet. Confusing the two would report a document as checked when it was not, so every
// test here asserts which list a thing lands in, not merely that something was said.

import { readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'

import * as yaml from 'js-yaml'

import { describe, expect, test } from 'bun:test'

import { check, countDeclarations, reportLines, run, type CheckResult } from './check.ts'
import { LANGUAGE_FILE, rulebookFor, WORDS } from './rulebook.ts'
import { resolveVocabulary, type Vocabulary } from './words.ts'

const problemsAt = (r: CheckResult, where: string): string[] =>
  r.problems.filter((p) => p.where === where).map((p) => p.what)

const blockersAt = (r: CheckResult, where: string): string[] =>
  r.blockers.filter((b) => b.where === where).map((b) => b.what)

const only = (doc: unknown): CheckResult => check(doc, 'f.yaml')

// A vocabulary that says nothing about which kinds are types, which is what the live one did
// before the language could state it. A name written under `is` cannot be judged against it.
const UNTYPED: Vocabulary = { ...WORDS, types: [] }
const untyped = (doc: unknown): CheckResult => check(doc, 'f.yaml', UNTYPED)

/** A whole document with the parts every valid file carries. */
const file = (extra: Record<string, unknown>): Record<string, unknown> => ({
  scope: ['metalang'],
  title: 'A file',
  ...extra,
})

describe('a document that cannot be walked', () => {
  test('an empty file is a problem and stops the rest', () => {
    const r = only(null)
    expect(problemsAt(r, 'f.yaml')).toEqual(['the file is empty'])
    expect(r.blockers).toEqual([])
  })

  test('undefined is the same as empty', () => {
    expect(problemsAt(only(undefined), 'f.yaml')).toEqual(['the file is empty'])
  })

  test('a list at the top level is not a mapping', () => {
    expect(problemsAt(only([1, 2]), 'f.yaml')).toEqual(['the file is not a mapping'])
  })

  test('text at the top level is not a mapping', () => {
    expect(problemsAt(only('declare'), 'f.yaml')).toEqual(['the file is not a mapping'])
  })
})

describe('the top level', () => {
  test('a key that is not in the vocabulary is a problem that names the ones that are', () => {
    const r = only(file({ notes: 'hello' }))
    expect(problemsAt(r, 'f.yaml')[0]).toContain('"notes" is not a top-level key')
    expect(problemsAt(r, 'f.yaml')[0]).toContain(
      'scope, import, language, against, title, description',
    )
  })

  test('a title written as a list is a problem', () => {
    expect(problemsAt(only(file({ title: ['a'] })), 'f.yaml.title')).toEqual([
      'must be written as text',
    ])
  })

  test('a declare section written as text is a problem', () => {
    expect(problemsAt(only(file({ declare: 'entity' })), 'f.yaml.declare')).toEqual([
      'must be written as a list',
    ])
  })

  test('a scope segment that is not text is a problem naming its position', () => {
    const r = only({ scope: ['metalang', 7, ''], title: 'A file' })
    expect(problemsAt(r, 'f.yaml.scope[1]')).toHaveLength(1)
    expect(problemsAt(r, 'f.yaml.scope[2]')).toHaveLength(1)
    expect(problemsAt(r, 'f.yaml.scope[0]')).toEqual([])
  })

  test('naming another language blocks, because a type is not yet made of anything', () => {
    expect(blockersAt(only(file({ language: 'other' })), 'f.yaml.language')).toHaveLength(1)
  })

  test('a top-level keyword with no approved shape blocks only when the document carries it', () => {
    const words: Vocabulary = { ...WORDS, topLevel: [...WORDS.topLevel, 'rules'] }
    expect(check(file({}), 'f.yaml', words).blockers.some((b) => b.where === 'f.yaml.rules')).toBe(
      false,
    )
    const carried = check(file({ rules: [] }), 'f.yaml', words)
    expect(blockersAt(carried, 'f.yaml.rules')[0]).toContain('has no approved shape')
  })

  test('a whole valid file with nothing declared has nothing wrong with it', () => {
    const r = only(file({ declare: [] }))
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })
})

describe('a declaration', () => {
  test('must be a mapping naming one kind', () => {
    expect(problemsAt(only(file({ declare: ['entity'] })), 'declare[0]')).toEqual([
      'must be a mapping naming one kind',
    ])
  })

  test('naming no kind is a problem', () => {
    expect(problemsAt(only(file({ declare: [{}] })), 'declare[0]')).toEqual(['names no kind'])
  })

  test('naming two kinds is a problem that counts them', () => {
    const r = only(file({ declare: [{ entity: {}, choice: {} }] }))
    expect(problemsAt(r, 'declare[0]')[0]).toContain('names 2 kinds, entity, choice')
  })

  test('a word that is not a kind is a problem naming the kinds', () => {
    const r = only(file({ declare: [{ thing: {} }] }))
    expect(problemsAt(r, 'declare[0]')[0]).toContain('"thing" is not a kind')
  })

  test('a kind that is only ever external is a problem when written plainly', () => {
    const r = only(file({ declare: [{ cast: {} }] }))
    expect(problemsAt(r, 'declare[0]')[0]).toBe('"cast" can only be written inside external')
  })

  test('a kind that is never external is a problem when written inside external', () => {
    const r = only(file({ declare: [{ external: { enum: { id: 'A' } } }] }))
    expect(problemsAt(r, 'declare[0].external')[0]).toBe('"enum" cannot be written inside external')
  })

  test('a type may be written inside external, which is how a borrowed type is named', () => {
    const r = only(
      file({ declare: [{ external: { entity: { id: 'Task', is: 'object', title: 'Work.' } } }] }),
    )
    expect(problemsAt(r, 'declare[0].external')).toEqual([])
    expect(problemsAt(r, 'declare[0]')).toEqual([])
  })

  test('external naming no kind, or more than one, is a problem at the wrapper', () => {
    expect(problemsAt(only(file({ declare: [{ external: {} }] })), 'declare[0].external')).toEqual([
      'names no kind',
    ])
    const two = only(file({ declare: [{ external: { cast: {}, entity: {} } }] }))
    expect(problemsAt(two, 'declare[0].external')[0]).toContain('names 2 kinds, cast, entity')
  })

  test('external carrying something that is not a mapping is a problem', () => {
    const r = only(file({ declare: [{ external: 'cast' }] }))
    expect(problemsAt(r, 'declare[0].external')).toEqual(['must be a mapping naming one kind'])
  })

  test('a kind may be written as often as a document needs, because declare is a list', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'A' } },
          { entity: { id: 'B' } },
          { entity: { id: 'C' } },
        ],
      }),
    )
    expect(r.problems).toEqual([])
  })

  test('a body that is not a mapping is a problem', () => {
    expect(problemsAt(only(file({ declare: [{ entity: 'x' }] })), 'declare[0].entity')).toEqual([
      'must be a mapping',
    ])
  })

  test('a kind whose keywords are undecided blocks, and nothing else is said about it', () => {
    // Every kind the language has carries its keywords today, so the vocabulary here is one where
    // a kind is approved and what it carries is not decided, which is what `null` means.
    const words: Vocabulary = { ...WORDS, onKind: { ...WORDS.onKind, cast: null } }
    const r = check(
      file({ declare: [{ external: { cast: { from: 'string', to: 'number' } } }] }),
      'f.yaml',
      words,
    )
    expect(blockersAt(r, 'declare[0].external.cast')[0]).toContain('have not been decided')
    expect(r.problems).toEqual([])
    // Nothing about from or to, because claiming either would claim an unapproved keyword.
    expect(r.blockers).toHaveLength(1)
  })

  test('a keyword the kind does not carry is a problem naming the ones it does', () => {
    const r = only(file({ declare: [{ choice: { id: 'C', values: ['a'] } }] }))
    expect(problemsAt(r, 'declare[0].choice')[0]).toBe(
      '"values" cannot be written on a choice — the only ones are id, title, description, standing, origin, replaces, options',
    )
  })

  test('no id is a problem', () => {
    expect(problemsAt(only(file({ declare: [{ entity: {} }] })), 'declare[0].entity')).toEqual([
      'has no id',
    ])
  })

  test('an id that is not text is a problem', () => {
    const r = only(file({ declare: [{ entity: { id: 7 } }] }))
    expect(problemsAt(r, 'declare[0].entity.id')).toEqual(['must be written as text'])
  })

  test('an empty id is a problem', () => {
    const r = only(file({ declare: [{ entity: { id: '' } }] }))
    expect(problemsAt(r, 'declare[0].entity.id')).toEqual(['is empty'])
  })

  test('two declarations with one id is a problem naming where the first was', () => {
    const r = only(file({ declare: [{ entity: { id: 'A' } }, { choice: { id: 'A' } }] }))
    expect(problemsAt(r, 'declare[1].choice.id')[0]).toBe(
      '"A" is already the id of declare[0].entity',
    )
  })

  test('an id is checked as a name, against what the language says a name may be', () => {
    const r = only(file({ declare: [{ entity: { id: 'A' } }] }))
    expect(problemsAt(r, 'declare[0].entity.id')).toEqual([])
    expect(blockersAt(r, 'declare[0].entity.id')).toEqual([])
  })

  test('an id written outside the characters a name may contain is a problem', () => {
    const r = only(file({ declare: [{ entity: { id: '2nd go' } }] }))
    expect(problemsAt(r, 'declare[0].entity.id')[0]).toContain('"2nd go" does not match')
    expect(problemsAt(r, 'declare[0].entity.id')[0]).toContain('"name" requires')
  })

  test('an id is checked against the language, whatever the document declares for itself', () => {
    const r = only(file({ declare: [{ entity: { id: 'A' } }, { enum: { id: 'B!', is: 'A' } }] }))
    expect(problemsAt(r, 'declare[1].entity.id')).toEqual([])
    expect(problemsAt(r, 'declare[1].enum.id')[0]).toContain('does not match')
  })

  test('nothing says what type an id has when the kind does not declare one', () => {
    const words: Vocabulary = { ...WORDS, declared: {}, onKind: { entity: ['id'] } }
    const r = check(file({ declare: [{ entity: { id: 'A' } }] }), 'f.yaml', words)
    expect(blockersAt(r, 'declare[0].entity.id')[0]).toContain('nothing says what type an id has')
  })

  test('an id an enum lists is a name as well, and is checked the same way', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'boolean', primitive: true } },
          { enum: { id: 'A', is: 'boolean', values: [{ id: 'not a name', value: true }] } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[1].enum.values[0].id')[0]).toContain('does not match')
  })

  test('an id is unique whether or not the declaration is external', () => {
    const words: Vocabulary = {
      ...WORDS,
      places: { declare: ['entity'], external: ['entity'] },
      onKind: { entity: ['id'] },
    }
    const r = check(
      file({ declare: [{ entity: { id: 'A' } }, { external: { entity: { id: 'A' } } }] }),
      'f.yaml',
      words,
    )
    expect(problemsAt(r, 'declare[1].external.entity.id')[0]).toContain('is already the id of')
  })
})

describe('what a declaration derives from', () => {
  test('a named type that this file declares is neither a problem nor a blocker', () => {
    const r = only(
      file({ declare: [{ entity: { id: 'Base' } }, { entity: { id: 'Kid', is: 'Base' } }] }),
    )
    expect(r.problems).toEqual([])
    expect(blockersAt(r, 'declare[1].entity.is')).toEqual([])
  })

  test('naming something that is not a type is a problem saying what it is instead', () => {
    const r = only(
      file({ declare: [{ constant: { id: 'Pi' } }, { entity: { id: 'Thing', is: 'Pi' } }] }),
    )
    expect(problemsAt(r, 'declare[1].entity.is')[0]).toBe(
      '"Pi" is a constant, and only a type can be named here — the kinds that are types are' +
        ' entity, enum, choice',
    )
  })

  test('a property cannot take its type from a constant either', () => {
    const r = only(
      file({
        declare: [
          { constant: { id: 'Pi' } },
          { entity: { id: 'Thing', properties: { a: { is: 'Pi' } } } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[1].entity.properties.a.is')[0]).toContain('is a constant')
  })

  test('a vocabulary that does not say which kinds are types blocks rather than guessing', () => {
    const r = untyped(
      file({ declare: [{ entity: { id: 'Base' } }, { entity: { id: 'Kid', is: 'Base' } }] }),
    )
    expect(r.problems).toEqual([])
    expect(blockersAt(r, 'declare[1].entity.is')[0]).toBe(
      'which kinds are types has not been stated, so what this names cannot be judged',
    )
  })

  test('a type this document neither declares nor brings in blocks, and says where it is', () => {
    const r = only(file({ declare: [{ entity: { id: 'A', is: 'string' } }] }))
    expect(blockersAt(r, 'declare[0].entity.is')[0]).toBe(
      '"string" is declared in "metalang", which this document does not bring in',
    )
  })

  test('deriving from several types names each position', () => {
    const r = only(file({ declare: [{ entity: { id: 'A', is: ['x', 'y'] } }] }))
    expect(blockersAt(r, 'declare[0].entity.is[0]')).toHaveLength(1)
    expect(blockersAt(r, 'declare[0].entity.is[1]')).toHaveLength(1)
  })

  test('something that is not a type name is a problem', () => {
    const r = only(file({ declare: [{ entity: { id: 'A', is: ['', 3] } }] }))
    expect(problemsAt(r, 'declare[0].entity.is[0]')).toEqual(['must name a type'])
    expect(problemsAt(r, 'declare[0].entity.is[1]')).toEqual(['must name a type'])
  })

  test('a type deriving from itself is a problem showing the path', () => {
    const r = only(file({ declare: [{ entity: { id: 'A', is: 'A' } }] }))
    expect(problemsAt(r, 'declare[0].entity.is')[0]).toBe('"A" derives from itself: A -> A')
  })

  test('a cycle through another type is a problem showing the whole path', () => {
    const r = only(
      file({ declare: [{ entity: { id: 'A', is: 'B' } }, { entity: { id: 'B', is: 'A' } }] }),
    )
    expect(problemsAt(r, 'declare[0].entity.is')[0]).toBe('"A" derives from itself: A -> B -> A')
  })

  test('a diamond is not a cycle', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'Top' } },
          { entity: { id: 'L', is: 'Top' } },
          { entity: { id: 'R', is: 'Top' } },
          { entity: { id: 'Bottom', is: ['L', 'R'] } },
        ],
      }),
    )
    expect(r.problems).toEqual([])
  })
})

describe('the two closed kinds — an enum lists values, a choice lists types', () => {
  test('a choice names the types it is one of, and one declared here resolves', () => {
    const r = only(
      file({ declare: [{ entity: { id: 'A' } }, { choice: { id: 'C', options: ['A'] } }] }),
    )
    expect(r.problems).toEqual([])
    expect(blockersAt(r, 'declare[1].choice.options[0]')).toEqual([])
  })

  test('an option declared nowhere blocks at its own position', () => {
    const r = only(file({ declare: [{ choice: { id: 'C', options: ['A', 'string'] } }] }))
    expect(blockersAt(r, 'declare[0].choice.options[0]')[0]).toContain('declared nowhere')
    expect(blockersAt(r, 'declare[0].choice.options[1]')[0]).toContain('does not bring in')
  })

  test('an option that does not name a type is a problem', () => {
    const r = only(file({ declare: [{ choice: { id: 'C', options: ['', 7] } }] }))
    expect(problemsAt(r, 'declare[0].choice.options[0]')).toEqual(['must name a type'])
    expect(problemsAt(r, 'declare[0].choice.options[1]')).toEqual(['must name a type'])
  })

  test('options written as anything but a list is a problem, and nothing is read from it', () => {
    const r = only(file({ declare: [{ choice: { id: 'C', options: 'A' } }] }))
    expect(problemsAt(r, 'declare[0].choice.options')).toEqual(['must be written as a list'])
    expect(r.blockers.filter((b) => b.where.startsWith('declare[0].choice.options['))).toEqual([])
  })

  test('an enum based on one type, listing values that each carry an id, is wholly right', () => {
    const r = only(
      file({ declare: [{ enum: { id: 'E', is: 'number', values: [{ id: 'NOT_FOUND', value: 404 }] } }] }),
    )
    expect(r.problems).toEqual([])
  })

  test('an enum with no is is a problem, because nothing says what its values are', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', values: [{ id: 'A', value: 1 }] } }] }))
    expect(problemsAt(r, 'declare[0].enum')).toEqual(['has no is'])
  })

  test('an enum based on several types is a problem, and nothing is read from them', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', is: ['number', 'string'], values: [] } }] }))
    expect(problemsAt(r, 'declare[0].enum.is')).toEqual([
      'must name exactly one type, because every value listed is a value of it',
    ])
    expect(r.blockers.filter((b) => b.where.startsWith('declare[0].enum.is'))).toEqual([])
  })

  test('a declaration that lists no values may still derive from several types', () => {
    const r = only(file({ declare: [{ entity: { id: 'A', is: ['x', 'y'] } }] }))
    expect(r.problems).toEqual([])
  })

  test('a value that is not a mapping is a problem', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', is: 'number', values: ['red'] } }] }))
    expect(problemsAt(r, 'declare[0].enum.values[0]')).toEqual([
      'must be a mapping carrying an id and a value',
    ])
  })

  test('a value with no id, and a value with no value, are each a problem', () => {
    const r = only(
      file({ declare: [{ enum: { id: 'E', is: 'number', values: [{ value: 1 }, { id: 'A' }] } }] }),
    )
    expect(problemsAt(r, 'declare[0].enum.values[0]')).toEqual(['has no id'])
    expect(problemsAt(r, 'declare[0].enum.values[1]')).toEqual(['has no value'])
  })

  test('an id that is not text is a problem at its own position', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', is: 'number', values: [{ id: 7, value: 1 }] } }] }))
    expect(problemsAt(r, 'declare[0].enum.values[0].id')).toEqual(['must be written as text'])
  })

  test('two values sharing one id is a problem naming where the first was', () => {
    const r = only(
      file({
        declare: [
          {
            enum: {
              id: 'E',
              is: 'number',
              values: [
                { id: 'A', value: 1 },
                { id: 'A', value: 2 },
              ],
            },
          },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[0].enum.values[1].id')[0]).toBe(
      '"A" is already the id of declare[0].enum.values[0]',
    )
  })

  test("a value's id names it inside its enum alone, so the file does not declare it", () => {
    const r = only(
      file({
        declare: [
          { enum: { id: 'E', is: 'number', values: [{ id: 'NOT_FOUND', value: 404 }] } },
          { entity: { id: 'X', is: 'NOT_FOUND' } },
        ],
      }),
    )
    expect(blockersAt(r, 'declare[1].entity.is')[0]).toContain('declared nowhere')
  })

  test('a keyword a value does not carry is a problem naming the ones it does', () => {
    const r = only(
      file({
        declare: [{ enum: { id: 'E', is: 'number', values: [{ id: 'A', value: 1, note: 'x' }] } }],
      }),
    )
    expect(problemsAt(r, 'declare[0].enum.values[0]')[0]).toBe(
      '"note" cannot be written on a value — the only ones are id, title, description, value',
    )
  })

  test('every value is checked against the primitive the enum is based on', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'number', primitive: true } },
          { enum: { id: 'E', is: 'number', values: [{ id: 'A', value: 1 }, { id: 'B', value: 'x' }] } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[1].enum.values[0].value')).toEqual([])
    expect(problemsAt(r, 'declare[1].enum.values[1].value')[0]).toBe('is not written as a number')
  })

  test('an enum based on a type that derives down to no primitive cannot hold a value', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'Thing' } },
          { enum: { id: 'E', is: 'Thing', values: [{ id: 'A', value: 1 }] } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[1].enum.values[0].value')[0]).toBe(
      '"Thing" derives down to no primitive, so nothing can be written as a value of it',
    )
  })

  test('a primitive the concrete syntax says nothing about blocks rather than passing', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'colour', primitive: true } },
          { enum: { id: 'E', is: 'colour', values: [{ id: 'A', value: 1 }] } },
        ],
      }),
    )
    expect(blockersAt(r, 'declare[1].enum.values[0].value')[0]).toBe(
      'nothing says what one value of a colour looks like once a file is read',
    )
  })

  test('an enum listing nothing does not block, because there is no value to check', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', is: 'number', values: [] } }] }))
    expect(blockersAt(r, 'declare[0].enum.values')).toEqual([])
  })

  test('values written as anything but a list is a problem rather than a blocker', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', is: 'number', values: 'a' } }] }))
    expect(problemsAt(r, 'declare[0].enum.values')).toEqual(['must be written as a list'])
    expect(blockersAt(r, 'declare[0].enum.values')).toEqual([])
  })

  test('the two words do not cross: options cannot be written on an enum', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', is: 'number', options: ['A'] } }] }))
    expect(problemsAt(r, 'declare[0].enum')[0]).toBe(
      '"options" cannot be written on an enum — the only ones are id, title, description, standing, origin, replaces, is, values',
    )
  })
})

// A constant names one fixed value, says what type it is of, and holds it. The value is read by
// the same rule a value an enum lists is read by, so the two stay one behaviour and not two.
describe('a declaration holding one value of its own', () => {
  /** A second document, which reaches `object` and the leaf types by importing the language. */
  const holder = (body: Record<string, unknown>): Record<string, unknown> => ({
    scope: ['arch'],
    import: [['metalang']],
    title: 'The architecture language',
    declare: [
      { entity: { id: 'A', is: 'object', title: 'A thing.', properties: { does: { is: 'string' } } } },
      { constant: { id: 'C', ...body } },
    ],
  })

  test('a constant saying what its value is of, and holding it, is wholly right', () => {
    const r = only(holder({ is: 'A', value: { does: 'Something.' } }))
    expect(r).toEqual({ problems: [], blockers: [], pending: [] })
  })

  test('a constant with no is is a problem, because nothing says what its value is', () => {
    expect(problemsAt(only(holder({ value: { does: 'Something.' } })), 'declare[1].constant')).toEqual([
      'has no is',
    ])
  })

  test('a constant with no value is a problem, because the name stands for nothing', () => {
    expect(problemsAt(only(holder({ is: 'A' })), 'declare[1].constant')).toEqual(['has no value'])
  })

  test('a word the type does not have is a problem at the word itself', () => {
    const r = only(holder({ is: 'A', value: { does: 'Something.', returns: 'B' } }))
    expect(problemsAt(r, 'declare[1].constant.value.returns')).toEqual([
      '"A" has no property called "returns"',
    ])
  })

  test('a value written as the wrong thing is reported against the property it is written on', () => {
    const r = only(holder({ is: 'A', value: { does: 7 } }))
    expect(problemsAt(r, 'declare[1].constant.value.does')).toEqual(['is not written as a string'])
  })

  test('a value whose type is declared nowhere blocks rather than passing', () => {
    const r = only(holder({ is: 'nothing', value: { does: 'Something.' } }))
    expect(blockersAt(r, 'declare[1].constant.value')[0]).toBe(
      '"nothing" is not declared where this value is read, so it cannot be checked',
    )
  })

  test('the two words do not cross: values cannot be written on a constant', () => {
    const r = only(holder({ is: 'A', values: [{ id: 'x', value: { does: 'Something.' } }] }))
    expect(problemsAt(r, 'declare[1].constant')[0]).toBe(
      '"values" cannot be written on a constant — the only ones are id, title, description, standing, origin, replaces, is, value',
    )
  })
})

describe('properties', () => {
  const entity = (body: Record<string, unknown>): Record<string, unknown> =>
    file({ declare: [{ entity: { id: 'A', ...body } }] })

  test('properties written as a list is a problem', () => {
    const r = only(entity({ properties: ['a'] }))
    expect(problemsAt(r, 'declare[0].entity.properties')).toEqual([
      'must be a mapping of name to property',
    ])
  })

  test('a property with a type declared in the file is clean', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'T' } },
          { entity: { id: 'A', properties: { p: { is: 'T' } } } },
        ],
      }),
    )
    expect(r.problems).toEqual([])
  })

  test('a property that is not a mapping is a reference, and references are unsettled', () => {
    const r = only(entity({ properties: { p: 'Other.q' } }))
    expect(blockersAt(r, 'declare[0].entity.properties.p')[0]).toContain(
      '"Other.q" is a reference to a property declared elsewhere',
    )
  })

  test('a keyword a property does not carry is a problem', () => {
    const r = only(entity({ properties: { p: { is: 'T', default: 1 } } }))
    expect(problemsAt(r, 'declare[0].entity.properties.p')[0]).toBe(
      '"default" cannot be written on a property — the only ones are is, title, description,' +
        ' optional, value, accepts, role',
    )
  })

  // What a property is to a reader, beyond the value it holds. It is a member of an enum the
  // language declares, so a word that is not one of the three is refused with the three named.
  test('a role the language lists is written on a property and nothing is wrong', () => {
    for (const role of ['kind', 'prose', 'name']) {
      expect(only(entity({ properties: { p: { is: 'string', role } } })).problems).toEqual([])
    }
  })

  test('a role the language does not list is a problem that names the ones it does', () => {
    const r = only(entity({ properties: { p: { is: 'string', role: 'heading' } } }))
    expect(problemsAt(r, 'declare[0].entity.properties.p.role')).toEqual([
      '"heading" is not a member of "role" — the members are kind, prose, name, mark',
    ])
  })

  test('a property with no is is a problem, because nothing says what it holds', () => {
    const r = only(entity({ properties: { p: {} } }))
    expect(problemsAt(r, 'declare[0].entity.properties.p')).toEqual([
      'has no is, so its type is unknown',
    ])
  })

  test('a property naming several types is a problem, because it holds one', () => {
    const r = only(entity({ properties: { p: { is: ['a', 'b'] } } }))
    expect(problemsAt(r, 'declare[0].entity.properties.p.is')).toEqual([
      'must name exactly one type, and a property holds one',
    ])
  })

  test('a property type that is not a name is a problem', () => {
    const r = only(entity({ properties: { p: { is: '' } } }))
    expect(problemsAt(r, 'declare[0].entity.properties.p.is')).toEqual(['must name a type'])
  })

  test('a property type this document does not reach blocks', () => {
    const r = only(entity({ properties: { p: { is: 'string' } } }))
    expect(blockersAt(r, 'declare[0].entity.properties.p.is')[0]).toContain(
      'does not bring in',
    )
  })
})

// What the language says has to be written, and what it says may be left out. No keyword is
// named in the checker any more, so each of these is the vocabulary being obeyed rather than a
// rule of this file's own.
describe('whether a keyword has to be written', () => {
  test('a declaration missing one the language requires is a problem naming it', () => {
    const r = only(file({ declare: [{ external: { cast: { id: 'c', from: 'string' } } }] }))
    expect(problemsAt(r, 'declare[0].external.cast')).toEqual(['has no to'])
  })

  test('one the language says may be left out is not reported missing', () => {
    const r = only(file({ declare: [{ entity: { id: 'A' } }] }))
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('an enum carries its is, because every value it lists is a value of that type', () => {
    const r = only(file({ declare: [{ enum: { id: 'E' } }] }))
    expect(problemsAt(r, 'declare[0].enum')).toEqual(['has no is', 'has no values'])
  })

  test('one of an enum values carries both its id and what that id stands for', () => {
    const r = only(file({ declare: [{ enum: { id: 'E', is: 'string', values: [{}] } }] }))
    expect(problemsAt(r, 'declare[0].enum.values[0]')).toEqual(['has no id', 'has no value'])
  })

  test('the file itself carries the keys the language says it must', () => {
    expect(problemsAt(check({ declare: [] }, 'f.yaml'), 'f.yaml')).toEqual([
      'has no scope',
      'has no title',
    ])
  })

  test('a property may leave out everything, because its type may come from above', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'string', primitive: true } },
          { entity: { id: 'A', properties: { x: { is: 'string' } } } },
          { entity: { id: 'B', is: 'A', properties: { x: { value: 'y' } } } },
        ],
      }),
    )
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('whether a property may be left out is checked as the boolean the language says it is', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'string', primitive: true } },
          { entity: { id: 'A', properties: { x: { is: 'string', optional: 'yes' } } } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[1].entity.properties.x.optional')).toEqual([
      'is not written as a boolean',
    ])
  })
})

describe('inherited properties', () => {
  const twoParents = (left: string, right: string, child?: Record<string, unknown>) =>
    file({
      declare: [
        { entity: { id: 'X' } },
        { entity: { id: 'Y' } },
        { entity: { id: 'L', properties: { p: { is: left } } } },
        { entity: { id: 'R', properties: { p: { is: right } } } },
        { entity: { id: 'C', is: ['L', 'R'], ...child } },
      ],
    })

  test('the same property arriving with one type from both parents is no conflict', () => {
    const r = only(twoParents('X', 'X'))
    expect(r.problems).toEqual([])
  })

  test('the same property arriving with different types is a problem naming both', () => {
    const r = only(twoParents('X', 'Y'))
    const what = problemsAt(r, 'declare[4].entity.properties.p')[0] ?? ''
    expect(what).toContain('"p" arrives from L and R with different types')
    expect(what).toContain('X and Y')
  })

  test('declaring it in the child resolves the conflict', () => {
    const r = only(twoParents('X', 'Y', { properties: { p: { is: 'X' } } }))
    expect(problemsAt(r, 'declare[4].entity.properties.p')).toEqual([])
  })

  test('one declaration reached by two paths is one property, not a conflict', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'X' } },
          { entity: { id: 'Top', properties: { p: { is: 'X' } } } },
          { entity: { id: 'L', is: 'Top' } },
          { entity: { id: 'R', is: 'Top' } },
          { entity: { id: 'C', is: ['L', 'R'] } },
        ],
      }),
    )
    expect(r.problems).toEqual([])
  })
})

describe('counting declarations', () => {
  test('counts across every section', () => {
    expect(
      countDeclarations({ declare: [{ entity: {} }, { choice: {} }, { external: { cast: {} } }] }),
    ).toBe(3)
  })

  test('a section that is absent or not a list counts nothing', () => {
    expect(countDeclarations({ declare: 'entity' })).toBe(0)
    expect(countDeclarations({})).toBe(0)
  })

  test('something that is not a mapping counts nothing', () => {
    expect(countDeclarations(null)).toBe(0)
    expect(countDeclarations([1])).toBe(0)
  })
})

describe('the report', () => {
  const none: CheckResult = { problems: [], blockers: [], pending: [] }

  test('says plainly when nothing is wrong', () => {
    const lines = reportLines('f.yaml', none, 0)
    expect(lines[0]).toBe('f.yaml — 0 declarations, checked against the approved vocabulary')
    expect(lines.at(-1)).toBe('Checked, and nothing is wrong.')
  })

  test('writes one declaration without an s', () => {
    expect(reportLines('f.yaml', none, 1)[0]).toContain('1 declaration,')
  })

  test('lists problems with where each one is', () => {
    const one = { problems: [{ where: 'declare[0]', what: 'has no id' }], blockers: [], pending: [] }
    const lines = reportLines('f.yaml', one, 1)
    expect(lines).toContain('1 problem:')
    expect(lines.some((l) => l.includes('declare[0]') && l.includes('has no id'))).toBe(true)
  })

  test('pluralises more than one problem', () => {
    const two = {
      problems: [
        { where: 'a', what: 'one' },
        { where: 'b', what: 'two' },
      ],
      blockers: [],
      pending: [],
    }
    expect(reportLines('f.yaml', two, 2)).toContain('2 problems:')
  })

  test('says what was not checked when only blockers came out', () => {
    const lines = reportLines('f.yaml', { problems: [], blockers: [{ where: 'a', what: 'b' }], pending: [] }, 1)
    expect(lines).toContain('1 blocker:')
    expect(lines.at(-1)).toBe(
      'Nothing is wrong with what could be checked. The blockers above were not checked.',
    )
  })

  test('says nothing reassuring when there are problems', () => {
    const lines = reportLines('f.yaml', { problems: [{ where: 'a', what: 'b' }], blockers: [], pending: [] }, 1)
    expect(lines.some((l) => l.startsWith('Checked, and nothing'))).toBe(false)
    expect(lines.some((l) => l.startsWith('Nothing is wrong'))).toBe(false)
  })
})

describe('running the checker over a file', () => {
  test('nothing is wrong with the live language file', () => {
    const doc = yaml.load(readFileSync(LANGUAGE_FILE, 'utf8'))
    expect(check(doc, 'metalang.yaml').problems).toEqual([])
  })

  test('the language file is checked whole: nothing is wrong and nothing is unchecked', () => {
    const doc = yaml.load(readFileSync(LANGUAGE_FILE, 'utf8'))
    const { problems, blockers } = check(doc, 'metalang.yaml')
    expect(problems).toEqual([])
    expect(blockers).toEqual([])
    expect(run(LANGUAGE_FILE)).toBe(0)
  })

  test('a file that is not there fails rather than throwing', () => {
    expect(run('/nonexistent/metalang.yaml')).toBe(1)
  })

  test('a file YAML cannot parse fails rather than throwing', async () => {
    const path = `${import.meta.dir}/.broken.test.yaml`
    await Bun.write(path, 'declare: [\n  - entity: {\n')
    try {
      expect(run(path)).toBe(1)
    } finally {
      await Bun.file(path).delete()
    }
  })

  test('a file with a problem in it fails', async () => {
    const path = `${import.meta.dir}/.wrong.test.yaml`
    await Bun.write(path, 'scope: [x]\ntitle: t\ndeclare:\n  - entity: {}\n')
    try {
      expect(run(path)).toBe(1)
    } finally {
      await Bun.file(path).delete()
    }
  })
})

describe('a type written out rather than named', () => {
  // A collection is written as one word naming the kind with its own properties inside it, so
  // what it holds is written where that kind says. The language's own file is written this way.
  const held = (property: unknown): unknown =>
    file({
      declare: [
        { entity: { id: 'name' } },
        { entity: { id: 'array', properties: { of: { is: 'name' } } } },
        { entity: { id: 'Thing', properties: { x: property } } },
      ],
    })

  test('a collection holding a declared type is neither a problem nor a blocker', () => {
    const r = only(held({ array: { of: 'name' } }))
    expect(r.problems).toEqual([])
    expect(blockersAt(r, 'declare[2].entity.properties.x.array.of')).toEqual([])
  })

  test('a collection of a collection is read the same way again', () => {
    const r = only(held({ array: { of: { array: { of: 'name' } } } }))
    expect(r.problems).toEqual([])
  })

  test('a word the collection does not carry is a problem naming what it does carry', () => {
    const r = only(held({ array: { holds: 'name' } }))
    expect(problemsAt(r, 'declare[2].entity.properties.x.array')[0]).toBe(
      '"holds" cannot be written on an array — the only ones are of, ordered, unique',
    )
  })

  test('a collection whose properties are not written as a mapping is a problem', () => {
    const r = only(held({ array: 'name' }))
    expect(problemsAt(r, 'declare[2].entity.properties.x.array')[0]).toBe(
      'must be a mapping of its properties',
    )
  })

  test('a property writing two words writes neither a type nor a property', () => {
    const r = only(held({ array: { of: 'name' }, set: { of: 'name' } }))
    expect(problemsAt(r, 'declare[2].entity.properties.x')[0]).toContain(
      'one word naming a type with its own properties inside',
    )
  })

  test('a property written as nothing at all has no type', () => {
    const r = only(held({}))
    expect(problemsAt(r, 'declare[2].entity.properties.x')[0]).toBe(
      'has no is, so its type is unknown',
    )
  })

  test('naming a type whose properties are undecided blocks rather than checking its body', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'boolean' } },
          { entity: { id: 'Thing', properties: { x: { boolean: { of: 'name' } } } } },
        ],
      }),
    )
    expect(blockersAt(r, 'declare[1].entity.properties.x.boolean')[0]).toBe(
      'the properties a boolean carries have not been decided, so nothing written here can be' +
        ' checked',
    )
  })

  test('a type written into itself by a YAML anchor is refused rather than followed down', () => {
    const inner: Record<string, unknown> = {}
    const outer = { array: inner }
    inner.of = outer
    const r = only(held(outer))
    expect(problemsAt(r, 'declare[2].entity.properties.x.array.of')[0]).toBe(
      'is written inside itself',
    )
  })
})

describe('a property written again to say what it always holds', () => {
  // A type deriving from the one that declared a property may write it again with only what it
  // changes, which is how each collection kind fixes whether it is ordered and whether it is
  // unique. The type comes from the declaration above, so it is not written twice.
  const derived = (fixed: unknown, from = 'Base'): unknown =>
    file({
      declare: [
        { entity: { id: 'boolean', primitive: true } },
        { entity: { id: 'Base', properties: { ordered: { is: 'boolean' } } } },
        { entity: { id: 'Mid', is: 'Base' } },
        { entity: { id: 'Kid', is: from, properties: { ordered: fixed } } },
      ],
    })

  test('writing only the value takes the type from the declaration above', () => {
    const r = only(derived({ value: true }))
    expect(r.problems).toEqual([])
  })

  test('the declaration above is found however far up it is', () => {
    const r = only(derived({ value: false }, 'Mid'))
    expect(r.problems).toEqual([])
  })

  test('the value is checked against the primitive the inherited type derives down to', () => {
    const r = only(derived({ value: 'true' }))
    expect(problemsAt(r, 'declare[3].entity.properties.ordered.value')[0]).toBe(
      'is not written as a boolean',
    )
  })

  test('the primitive is found however many types down to it there are', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'string', primitive: true } },
          { entity: { id: 'name', is: 'string' } },
          { entity: { id: 'A', properties: { x: { is: 'name', value: 7 } } } },
          { entity: { id: 'B', properties: { x: { is: 'name', value: 'ok' } } } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[2].entity.properties.x.value')[0]).toBe(
      'is not written as a string',
    )
    expect(problemsAt(r, 'declare[3].entity.properties.x.value')).toEqual([])
  })

  test('a type that is abstract may be named, and nothing may be written as one', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'name' } },
          { entity: { id: 'collection', abstract: true, properties: { of: { is: 'name' } } } },
          { entity: { id: 'A', properties: { x: { is: 'collection' } } } },
          { entity: { id: 'B', properties: { x: { collection: { of: 'name' } } } } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[2].entity.properties.x.is')).toEqual([])
    expect(problemsAt(r, 'declare[3].entity.properties.x')[0]).toBe(
      '"collection" is abstract, so nothing is ever directly of it — name one of the types' +
        ' deriving from it instead',
    )
  })

  test('a property no type above declares, and no type of its own, has no type', () => {
    const r = only(
      file({ declare: [{ entity: { id: 'A', properties: { x: { value: true } } } }] }),
    )
    expect(problemsAt(r, 'declare[0].entity.properties.x')[0]).toBe(
      'has no is, so its type is unknown',
    )
  })

  test('a type deriving from itself does not send the search above it round forever', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'A', is: 'B', properties: { x: { value: true } } } },
          { entity: { id: 'B', is: 'A' } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[0].entity.properties.x')[0]).toBe(
      'has no is, so its type is unknown',
    )
  })

  test('a type named above as something other than a name is passed over', () => {
    const r = only(
      file({ declare: [{ entity: { id: 'A', is: [7], properties: { x: { value: true } } } }] }),
    )
    expect(problemsAt(r, 'declare[0].entity.properties.x')[0]).toBe(
      'has no is, so its type is unknown',
    )
  })

  test('a collection written as something other than its properties names no type', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'array', properties: { of: { is: 'array' } } } },
          { entity: { id: 'A', properties: { x: { array: [1, 2] } } } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[1].entity.properties.x.array')[0]).toBe(
      'must be a mapping of its properties',
    )
  })

  test('a collection whose body names two words names one type and not two', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'name' } },
          { entity: { id: 'array', properties: { of: { is: 'name' } } } },
          { entity: { id: 'A', properties: { x: { array: { of: { a: 1, b: 2 } } } } } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[2].entity.properties.x.array.of')[0]).toBe(
      'names one type, written as one word with its own properties inside it',
    )
  })
})

describe('what a type says its values may be', () => {
  // A document that declares a primitive of its own, a kind of constraint, and a type narrowed by
  // one. The constraint word resolves in the document so nothing stray is reported about it.
  const narrowed = (constraints: unknown, value: unknown): unknown =>
    file({
      declare: [
        { entity: { id: 'string', primitive: true } },
        { entity: { id: 'pattern', properties: { value: { is: 'string' } } } },
        { entity: { id: 'code', is: 'string', constraints } },
        { entity: { id: 'A', properties: { x: { is: 'code', value } } } },
      ],
    })

  const held = (constraints: unknown, value: unknown): CheckResult => only(narrowed(constraints, value))

  test('a value meeting every constraint is neither a problem nor a blocker', () => {
    const r = held([{ pattern: { value: '[a-z]+' } }], 'abc')
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('a value that does not meet one says what it did not meet and which type requires it', () => {
    const r = held([{ pattern: { value: '[a-z]+' } }], 'AB')
    expect(problemsAt(r, 'declare[3].entity.properties.x.value')[0]).toBe(
      '"AB" does not match [a-z]+, which "code" requires of every value of it',
    )
  })

  test('a type is held to what it derives as well as to what it writes', () => {
    const r = only(
      file({
        declare: [
          { entity: { id: 'string', primitive: true } },
          { entity: { id: 'pattern', properties: { value: { is: 'string' } } } },
          { entity: { id: 'code', is: 'string', constraints: [{ pattern: { value: '[a-z]+' } }] } },
          { entity: { id: 'short', is: 'code' } },
          { entity: { id: 'A', properties: { x: { is: 'short', value: 'AB' } } } },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[4].entity.properties.x.value')[0]).toContain('"code" requires')
  })

  test('a pattern that is not a regular expression is a problem where it is written', () => {
    const r = held([{ pattern: { value: '[a-z' } }], 'abc')
    expect(problemsAt(r, 'declare[2].entity.constraints[0].pattern')[0]).toContain(
      'is not a regular expression',
    )
  })

  test('a pattern that could not be read says nothing about the values, which it did not judge', () => {
    const r = held([{ pattern: { value: '[a-z' } }], 'abc')
    expect(problemsAt(r, 'declare[3].entity.properties.x.value')).toEqual([])
  })

  test('what a pattern holds is checked as a value and not as the name of a type', () => {
    const r = held([{ pattern: { value: 7 } }], 'abc')
    expect(problemsAt(r, 'declare[2].entity.constraints[0].pattern.value')[0]).toBe(
      'is not written as an expression',
    )
  })

  test('a kind of constraint nothing applies blocks rather than letting a value through', () => {
    const r = held([{ string: {} }], 'abc')
    expect(blockersAt(r, 'declare[2].entity.constraints[0]')[0]).toContain(
      'nothing says how a string is applied',
    )
  })

  test('constraints written as anything but a list is a problem', () => {
    const r = held({ pattern: { value: '[a-z]+' } }, 'abc')
    expect(problemsAt(r, 'declare[2].entity.constraints')).toEqual(['must be written as a list'])
  })

  test('a constraint naming no kind is a problem', () => {
    const r = held([{}], 'abc')
    expect(problemsAt(r, 'declare[2].entity.constraints[0]')[0]).toContain(
      'names one type, written as one word with its own properties inside it',
    )
  })

  test('a constraint whose body is not a mapping is a problem', () => {
    const r = held([{ pattern: '[a-z]+' }], 'abc')
    expect(problemsAt(r, 'declare[2].entity.constraints[0].pattern')[0]).toBe(
      'must be a mapping of its properties',
    )
  })
})

describe('a keyword with no shape of its own', () => {
  // A cast is the one kind written externally, and every keyword it carries is checked by what
  // the language says that keyword holds rather than by anything written for casts.
  const cast = (body: Record<string, unknown>): CheckResult =>
    only(file({ declare: [{ external: { cast: { id: 'c', ...body } } }] }))

  test('a cast naming the types it converts between says nothing is wrong with it', () => {
    const r = cast({ from: 'uuid', to: 'string', implicit: true })
    expect(r.problems).toEqual([])
    // The types it names are the language's own, which this document neither declares nor brings
    // in, and that is the one thing left unchecked about it.
    for (const b of r.blockers) expect(b.what).toContain('does not bring in')
  })

  test('a keyword holding a type takes a type written out as well as a name', () => {
    const r = cast({ from: 'uuid', to: { array: { of: 'byte' } } })
    expect(r.problems).toEqual([])
  })

  test('a keyword holding a type is a problem when what is written is not one', () => {
    expect(problemsAt(cast({ from: 7 }), 'declare[0].external.cast.from')[0]).toBe('must name a type')
  })

  test('a keyword holding a value is checked against the type it holds', () => {
    expect(problemsAt(cast({ implicit: 'yes' }), 'declare[0].external.cast.implicit')[0]).toBe(
      'is not written as a boolean',
    )
  })

  test('what a type says about itself is checked the same way', () => {
    const r = only(file({ declare: [{ entity: { id: 'A', abstract: 'yes' } }] }))
    expect(problemsAt(r, 'declare[0].entity.abstract')[0]).toBe('is not written as a boolean')
  })

  test('a keyword the language says nothing about blocks rather than passing', () => {
    const words: Vocabulary = {
      ...WORDS,
      onKind: { ...WORDS.onKind, entity: ['id', 'colour'] },
    }
    const r = check(file({ declare: [{ entity: { id: 'A', colour: 'red' } }] }), 'f.yaml', words)
    expect(blockersAt(r, 'declare[0].entity.colour')[0]).toContain('nothing says what "colour" holds')
  })
})

describe('the live language file as a document', () => {
  const doc = yaml.load(readFileSync(LANGUAGE_FILE, 'utf8'))

  test('a value outside the range a byte allows is a problem', () => {
    const r = check(
      { ...(doc as object), declare: [...(doc as { declare: unknown[] }).declare,
        { entity: { id: 'A', properties: { x: { is: 'byte', value: 256 } } } }] },
      'metalang.yaml',
    )
    expect(r.problems[0]?.what).toBe(
      '256 is above 255, which "byte" requires of every value of it',
    )
  })

  test('a value outside the shape a uuid is written in is a problem', () => {
    const r = check(
      { ...(doc as object), declare: [...(doc as { declare: unknown[] }).declare,
        { entity: { id: 'A', properties: { x: { is: 'uuid', value: 'not-a-uuid' } } } }] },
      'metalang.yaml',
    )
    expect(r.problems[0]?.what).toContain('"not-a-uuid" does not match')
  })

  test('a value that meets both is neither a problem nor a blocker', () => {
    const r = check(
      { ...(doc as object), declare: [...(doc as { declare: unknown[] }).declare,
        { entity: { id: 'A', properties: {
          x: { is: 'byte', value: 255 },
          y: { is: 'uuid', value: '550e8400-e29b-41d4-a716-446655440000' },
        } } }] },
      'metalang.yaml',
    )
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })
})

describe('a document reaching the declarations of another scope', () => {
  // The one scope a checker is given is the one the vocabulary was read from, which is the
  // language's own. A document brings it in by writing it under `import`.
  const stage = (extra: Record<string, unknown>): unknown => ({
    scope: ['guard', 'process'],
    title: 'A stage of the process',
    ...extra,
    declare: [
      {
        entity: {
          id: 'stage',
          properties: { name: { is: 'string' }, tries: { is: 'byte', value: 3 } },
        },
      },
    ],
  })

  test('a document importing the language checks whole', () => {
    const r = check(stage({ import: [['metalang']] }), 'stage.yaml')
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('a document importing nothing reaches no name it does not declare', () => {
    const r = check(stage({}), 'stage.yaml')
    expect(blockersAt(r, 'declare[0].entity.properties.name.is')[0]).toBe(
      '"string" is declared in "metalang", which this document does not bring in',
    )
  })

  test('an imported type judges the values written against it', () => {
    const r = check(
      {
        scope: ['guard', 'process'],
        import: [['metalang']],
        declare: [{ entity: { id: 'stage', properties: { tries: { is: 'byte', value: 300 } } } }],
      },
      'stage.yaml',
    )
    expect(problemsAt(r, 'declare[0].entity.properties.tries.value')[0]).toBe(
      '300 is above 255, which "byte" requires of every value of it',
    )
  })

  test('a document declaring a name itself keeps its own over the imported one', () => {
    const r = check(
      {
        scope: ['guard', 'process'],
        import: [['metalang']],
        title: 'A stage of the process',
        declare: [
          { entity: { id: 'byte', primitive: true } },
          { entity: { id: 'stage', properties: { tries: { is: 'byte', value: 300 } } } },
        ],
      },
      'stage.yaml',
    )
    // The document's own byte is the one that judges the value, and it carries no range, so 300
    // stands where the imported byte would have refused it.
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('a scope this checker was not given blocks rather than passing', () => {
    const r = check(stage({ import: [['metalang'], ['guard', 'other']] }), 'stage.yaml')
    expect(blockersAt(r, 'stage.yaml.import[1]')[0]).toBe(
      '"guard.other" is not a scope this checker was given',
    )
  })

  test('an import written as anything but a list of scopes is a problem', () => {
    expect(problemsAt(check(stage({ import: 'metalang' }), 'stage.yaml'), 'stage.yaml.import')).toEqual([
      'must be written as a list',
    ])
    expect(problemsAt(check(stage({ import: ['metalang'] }), 'stage.yaml'), 'stage.yaml.import[0]')).toEqual([
      'a scope is written as a list of segments',
    ])
  })

  test('a segment of an imported scope written as anything but text is a problem', () => {
    const r = check(stage({ import: [[7]] }), 'stage.yaml')
    expect(problemsAt(r, 'stage.yaml.import[0][0]')[0]).toContain('must be written as text')
  })

  test('the language file imports nothing, because it declares everything it writes', () => {
    const doc = yaml.load(readFileSync(LANGUAGE_FILE, 'utf8')) as Record<string, unknown>
    expect(doc.import).toBeUndefined()
    expect(check(doc, 'metalang.yaml')).toEqual({ problems: [], blockers: [], pending: [] })
  })
})

// A document says which language it is written in, and the rulebook is loaded from the file that
// language names. What is checked here is that a document and the rulebook it was read against
// agree, because a document read against the wrong one has not been checked.
describe('a document written in a language of its own', () => {
  const parts = join(dirname(LANGUAGE_FILE), 'parts.yaml')
  const read = (file: string): Record<string, unknown> =>
    yaml.load(readFileSync(file, 'utf8')) as Record<string, unknown>

  test('naming a language it was not read against blocks, whatever else is written', () => {
    const r = check({ scope: ['parts'], language: 'arch', title: 'Parts' }, 'parts.yaml')
    expect(blockersAt(r, 'parts.yaml.language')[0]).toBe(
      'this document is written in "arch" and it was read against "metalang", so nothing in it' +
        ' was checked against its own language',
    )
  })

  test('read against the language it names, a real one checks whole', () => {
    const doc = read(parts)
    const { words, faults } = rulebookFor(doc, parts)
    expect(faults).toEqual([])
    expect(words).not.toBeNull()
    expect(check(doc, 'parts.yaml', words ?? undefined)).toEqual({ problems: [], blockers: [], pending: [] })
  })

  test('the language own words judge what is written against them', () => {
    const doc = read(parts)
    const { words } = rulebookFor(doc, parts)
    const declare = doc.declare as Record<string, Record<string, unknown>>[]
    const first = declare[1]?.constant?.value as Record<string, unknown> | undefined
    if (first !== undefined) first.form = 'struct'
    const r = check(doc, 'parts.yaml', words ?? undefined)
    expect(problemsAt(r, 'declare[1].constant.value.form')[0]).toBe(
      '"struct" is not a member of "forms" — the members are interface, class, record, enum,' +
        ' function, process',
    )
  })

  test('a name of the language it names is reached without bringing it in', () => {
    const doc = read(parts)
    const { words } = rulebookFor(doc, parts)
    // `forms` is declared in arch and this document imports only metalang, so naming the language
    // is what reaches it.
    expect(words?.declared.forms?.scope).toEqual(['arch'])
    expect(check(doc, 'parts.yaml', words ?? undefined).blockers).toEqual([])
  })

  // What a thing is made of. The document is built here rather than read from a file, but it is
  // resolved through a real path in `ts/metalang`, so it is read against the live architecture
  // language and not against a copy.
  describe('the members an element has', () => {
    /** `members` is left out altogether, which is what the language refuses to read as none. */
    const having = (members?: unknown): Record<string, unknown> => ({
      scope: ['guard'],
      language: 'arch',
      import: [['metalang']],
      title: 'The parts of the guard',
      declare: [
        {
          constant: {
            id: 'IGuard',
            is: 'element',
            value: {
              form: 'interface',
              does: 'The three behaviours every guard answers a tool call through.',
              reach: 'public',
              exactness: 'loose',
              groups: { concept: 'guard', job: 'port' },
              sits: { package: 'none', assembly: 'A.Abstractions', namespace: 'A.Abstractions.Contracts' },
              relates: { implements: 'none', answers: 'none', holds: 'none' },
              ...(members === undefined ? {} : { members }),
            },
          },
        },
      ],
    })

    const checked = (members: unknown): CheckResult => {
      const doc = having(members)
      const { words } = rulebookFor(doc, parts)
      return check(doc, 'guard.yaml', words ?? undefined)
    }

    const one = {
      name: 'PrecheckAsync',
      kind: 'method',
      reach: 'public',
      type: 'none',
      does: 'Judges a tool call before it runs.',
    }

    test('an element naming its members is wholly right', () => {
      expect(checked([one])).toEqual({ problems: [], blockers: [], pending: [] })
    })

    // Nothing written is not an answer to how many members a thing has. The language gives an
    // element with none the word for it, so leaving the line out is the one thing it refuses.
    test('an element with nothing written for its members is a problem saying so', () => {
      const doc = having()
      const { words } = rulebookFor(doc, parts)
      expect(problemsAt(check(doc, 'guard.yaml', words ?? undefined), 'declare[0].constant.value')).toEqual([
        'has no members',
      ])
    })

    test('an element that has none says so, and nothing is reported', () => {
      expect(checked('none')).toEqual({ problems: [], blockers: [], pending: [] })
    })

    test('members nobody has written yet are a pending value, so nothing reads as finished', () => {
      const r = checked('pending')
      expect(r.problems).toEqual([])
      expect(r.pending).toEqual([
        { where: 'declare[0].constant.value.members', what: 'is required and not yet given' },
      ])
    })

    test('a member name that is not an identifier is a problem, because a name is one', () => {
      const r = checked([{ ...one, name: 'Precheck Async' }])
      expect(problemsAt(r, 'declare[0].constant.value.members[0].name')).toEqual([
        '"Precheck Async" does not match [A-Za-z_][A-Za-z0-9_]*, which "name" requires of every' +
          ' value of it',
      ])
    })

    test('a kind that is not one of the five is a problem that lists the five', () => {
      const r = checked([{ ...one, kind: 'delegate' }])
      expect(problemsAt(r, 'declare[0].constant.value.members[0].kind')).toEqual([
        '"delegate" is not a member of "kinds" — the members are method, constructor, property,' +
          ' field, value',
      ])
    })

    test('a member with no name is a problem, and so is one with no kind', () => {
      const without = (part: string): Record<string, unknown> =>
        Object.fromEntries(Object.entries(one).filter(([key]) => key !== part))
      expect(problemsAt(checked([without('name')]), 'declare[0].constant.value.members[0]')).toEqual([
        'has no name',
      ])
      expect(problemsAt(checked([without('kind')]), 'declare[0].constant.value.members[0]')).toEqual([
        'has no kind',
      ])
    })

    test('a word a member does not carry is a problem at the word itself', () => {
      const r = checked([{ ...one, returns: 'Verdict' }])
      expect(problemsAt(r, 'declare[0].constant.value.members[0].returns')).toEqual([
        '"member" has no property called "returns"',
      ])
    })

    test('the same member written twice is a problem, because a list is unique', () => {
      const r = checked([one, one])
      expect(problemsAt(r, 'declare[0].constant.value.members[1]')).toEqual([
        'repeats an earlier member, and the members of a list are unique',
      ])
    })
  })

  test('a scope the rulebook brought in is one the checker was given', () => {
    const doc = read(parts)
    const { words } = rulebookFor(doc, parts)
    expect(words?.brought).toEqual([['arch'], ['metalang']])
    expect(blockersAt(check(doc, 'parts.yaml', words ?? undefined), 'parts.yaml.import[0]')).toEqual(
      [],
    )
  })
})

// The root every type derives from is primitive, and one of its values is a set of named parts.
// So a value written against a type with properties is read one level further down: each key
// against the property it names, by the same walk that read the key above it.
describe('a value of a type with properties', () => {
  /** A second document, which reaches `object` and the leaf types by importing the language. */
  const reaching = (extra: Record<string, unknown>): Record<string, unknown> => ({
    scope: ['arch'],
    import: [['metalang']],
    title: 'The architecture language',
    ...extra,
  })

  /** One type with parts, and an enum listing one member of it. */
  const listing = (properties: unknown, value: unknown): Record<string, unknown> =>
    reaching({
      declare: [
        { entity: { id: 'form', is: 'object', title: 'A form.', properties } },
        {
          enum: {
            id: 'forms',
            is: 'form',
            title: 'The forms.',
            values: [{ id: 'interface', value }],
          },
        },
      ],
    })

  const parts = { mark: { is: 'string' }, example: { is: 'string' } }
  const at = 'declare[1].enum.values[0].value'

  test("a mapping of the type's own parts is whole", () => {
    const r = only(listing(parts, { mark: '<>', example: 'IGuard' }))
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('a key naming no property of the type is a problem that names the type', () => {
    const r = only(listing(parts, { mark: '<>', example: 'IGuard', colour: 'blue' }))
    expect(problemsAt(r, `${at}.colour`)).toEqual(['"form" has no property called "colour"'])
  })

  test('a part that is not optional and is not written is a problem', () => {
    const r = only(listing(parts, { mark: '<>' }))
    expect(problemsAt(r, at)).toEqual(['has no example'])
  })

  test('a part that may be left out may be left out', () => {
    const r = only(
      listing({ ...parts, tries: { is: 'byte', optional: true } }, { mark: '<>', example: 'I' }),
    )
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('a part written as the wrong type is a problem against that part', () => {
    const r = only(listing(parts, { mark: 7, example: 'IGuard' }))
    expect(problemsAt(r, `${at}.mark`)).toEqual(['is not written as a string'])
  })

  test("the constraints of a part's type apply inside the value", () => {
    const r = only(
      listing({ ...parts, tries: { is: 'byte' } }, { mark: '<>', example: 'I', tries: 900 }),
    )
    expect(problemsAt(r, `${at}.tries`)[0]).toContain('900 is above 255')
  })

  test('a part whose own type has parts is read one level further down', () => {
    const r = only(
      reaching({
        declare: [
          {
            entity: {
              id: 'named',
              is: 'object',
              title: 'What it is called in each language.',
              properties: { csharp: { is: 'string' } },
            },
          },
          {
            entity: {
              id: 'form',
              is: 'object',
              title: 'A form.',
              properties: { equivalents: { is: 'named' } },
            },
          },
          {
            enum: {
              id: 'forms',
              is: 'form',
              title: 'The forms.',
              values: [{ id: 'interface', value: { equivalents: { csharp: 7 } } }],
            },
          },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[2].enum.values[0].value.equivalents.csharp')).toEqual([
      'is not written as a string',
    ])
  })

  test('a value written as something other than a mapping is a problem', () => {
    expect(problemsAt(only(listing(parts, 'interface')), at)).toEqual([
      'is not written as an object',
    ])
  })

  test('a part that writes its type out is read as the collection it writes', () => {
    const r = only(listing({ marks: { set: { of: 'string' } } }, { marks: ['<>', '()'] }))
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('a type deriving from nothing in particular still derives down to no primitive', () => {
    const r = only(
      reaching({
        declare: [
          { entity: { id: 'loose', title: 'Deriving from nothing in particular.' } },
          {
            enum: {
              id: 'looses',
              is: 'loose',
              title: 'The looses.',
              values: [{ id: 'one', value: { a: 1 } }],
            },
          },
        ],
      }),
    )
    expect(problemsAt(r, 'declare[1].enum.values[0].value')[0]).toContain(
      'derives down to no primitive',
    )
  })
})

// A value is written against a type in four ways beyond naming a leaf: the type is written out
// as a collection, it is a choice, it is an enum a bare id names a member of, or nothing can be
// said about it. The last must be loud, because a value nothing judged is a value unchecked.
describe('a value written against a collection, a choice and an enum', () => {
  const reaching = (extra: Record<string, unknown>): Record<string, unknown> => ({
    scope: ['arch'],
    import: [['metalang']],
    title: 'The architecture language',
    ...extra,
  })

  const around = (
    declare: unknown[],
    properties: unknown,
    value: unknown,
  ): Record<string, unknown> =>
    reaching({
      declare: [
        ...declare,
        { entity: { id: 'form', is: 'object', title: 'A form.', properties } },
        { enum: { id: 'forms', is: 'form', title: 'The forms.', values: [{ id: 'one', value }] } },
      ],
    })

  const at = (declared: number): string => `declare[${declared}].enum.values[0].value`

  describe('a type written out', () => {
    const marks = { marks: { set: { of: 'string' } } }

    test('a value that is not a list is a problem', () => {
      const r = only(around([], marks, { marks: 'x' }))
      expect(problemsAt(r, `${at(1)}.marks`)[0]).toContain('is not written as a list')
    })

    test('each member is judged against the type it holds', () => {
      const r = only(around([], marks, { marks: ['a', 7] }))
      expect(problemsAt(r, `${at(1)}.marks[1]`)).toEqual(['is not written as a string'])
    })

    test('a set holding the same member twice is a problem, and an array is not', () => {
      const twice = only(around([], marks, { marks: ['a', 'a'] }))
      expect(problemsAt(twice, `${at(1)}.marks[1]`)[0]).toContain('repeats an earlier member')
      const array = only(around([], { marks: { array: { of: 'string' } } }, { marks: ['a', 'a'] }))
      expect(array.problems).toEqual([])
    })

    test('a written-out word that is not a collection is a blocker, never passed', () => {
      const r = only(around([], { marks: { pattern: { value: 'x' } } }, { marks: ['a'] }))
      expect(blockersAt(r, `${at(1)}.marks`)[0]).toContain('written out is read')
    })

    test('a collection with no of is a problem', () => {
      const r = only(around([], { marks: { set: {} } }, { marks: ['a'] }))
      expect(problemsAt(r, `${at(1)}.marks`)[0]).toContain('has no "of"')
    })

    test('a type written out as more than one word is a problem', () => {
      const r = only(around([], { marks: { set: { of: 'string' }, bag: { of: 'string' } } }, { marks: [] }))
      expect(r.problems.length + r.blockers.length).toBeGreaterThan(0)
    })
  })

  describe('a choice', () => {
    const marking = { choice: { id: 'marking', options: ['string', 'uri'], title: 'Either.' } }
    const held = { mark: { is: 'marking' } }

    // `uri` derives from `string`, which is what orders a choice between the two: a value the
    // narrower option takes is of it, and the rest fall through to the wider one. `glyph` is one
    // character, and neither derives from `uri` nor has `uri` deriving from it.
    const glyph = {
      entity: {
        id: 'glyph',
        is: 'string',
        title: 'One character.',
        constraints: [{ pattern: { value: '[^\\s]' } }],
      },
    }
    const apart = { choice: { id: 'marking', options: ['glyph', 'uri'], title: 'Either.' } }

    test('a value of the first option is taken', () => {
      const r = only(around([glyph, apart], held, { mark: '◇' }))
      expect(r.problems).toEqual([])
      expect(r.blockers).toEqual([])
    })

    test('a value of the second option is taken', () => {
      const r = only(around([glyph, apart], held, { mark: 'https://a.example/b' }))
      expect(r.problems).toEqual([])
    })

    test('a value of neither is a problem that says why each option refused it', () => {
      const r = only(around([marking], held, { mark: 7 }))
      const said = problemsAt(r, `${at(2)}.mark`)[0] ?? ''
      expect(said).toContain('is not a value of any option of "marking"')
      expect(said).toContain('as string')
      expect(said).toContain('as uri')
    })

    test('the options are tried quietly: only the verdict is reported', () => {
      const r = only(around([glyph, apart], held, { mark: 7 }))
      expect(r.problems).toHaveLength(1)
    })

    test('a choice of nothing has no values', () => {
      const none = { choice: { id: 'marking', options: [], title: 'Nothing.' } }
      expect(problemsAt(only(around([none], held, { mark: 'a' })), `${at(2)}.mark`)[0]).toContain(
        'a choice of nothing',
      )
    })

    test('a choice that is one of itself is a problem rather than a loop', () => {
      const loop = { choice: { id: 'marking', options: ['marking'], title: 'Itself.' } }
      expect(problemsAt(only(around([loop], held, { mark: 'a' })), `${at(2)}.mark`)[0]).toContain(
        'one of itself',
      )
    })

    test('an option deriving from another is the order of the two, not a defect', () => {
      const r = only(around([marking], held, { mark: '\u25c7' }))
      expect(problemsAt(r, 'declare[0].choice.options')).toEqual([])
      expect(r.problems).toEqual([])
      expect(r.blockers).toEqual([])
    })

    test('the narrower option takes the values it accounts for', () => {
      // A uri is a string as well, and the narrower option is asked first, so a uri is a uri.
      const r = only(around([marking], held, { mark: 'https://a.example/b' }))
      expect(r.problems).toEqual([])
      expect(r.blockers).toEqual([])
    })

    test('a value the narrower option refuses falls through to the wider one', () => {
      const r = only(around([marking], held, { mark: 'not a uri at all' }))
      expect(r.problems).toEqual([])
      expect(r.blockers).toEqual([])
    })

    test('the same option written twice is a problem, because a choice is one of each once', () => {
      const twice = { choice: { id: 'marking', options: ['string', 'string'], title: 'Twice.' } }
      expect(problemsAt(only(around([twice], held, { mark: 'a' })), 'declare[0].choice.options')[0])
        .toContain('"string" is written twice')
    })

    test('two options taking one value with no order between them is reported at the value', () => {
      // Both take every single character, and neither derives from the other, so nothing written
      // in the document says which of the two was meant.
      const other = {
        entity: {
          id: 'tick',
          is: 'string',
          title: 'One character.',
          constraints: [{ pattern: { value: '[^\\s]' } }],
        },
      }
      const both = { choice: { id: 'marking', options: ['glyph', 'tick'], title: 'Both.' } }
      const r = only(around([glyph, other, both], held, { mark: '\u25c7' }))
      expect(problemsAt(r, `${at(4)}.mark`)[0]).toContain('neither derives from the other')
    })

    test('an option naming itself is read as one option, not as a loop', () => {
      const one = { choice: { id: 'marking', options: ['string'], title: 'One.' } }
      const r = only(around([one], held, { mark: 'a' }))
      expect(r.problems).toEqual([])
    })

    test('an option that cannot be judged is a blocker, never a pass', () => {
      const odd = { choice: { id: 'marking', options: ['loose'], title: 'Loose.' } }
      const loose = { entity: { id: 'loose', is: 'object', title: 'Loose.', properties: { n: { set: { of: 'string' } } } } }
      const r = only(around([loose, odd], held, { mark: { n: 'x' } }))
      expect(r.problems.concat(r.blockers).length).toBeGreaterThan(0)
    })
  })

  describe('an enum a bare id names a member of', () => {
    const language = { entity: { id: 'language', is: 'object', title: 'L.', properties: { name: { is: 'string' } } } }
    const languages = {
      enum: {
        id: 'languages',
        is: 'language',
        title: 'Ls.',
        values: [{ id: 'csharp', value: { name: 'C#' } }],
      },
    }
    const held = { language: { is: 'languages' } }

    test('a bare id of a member is that member', () => {
      const r = only(around([language, languages], held, { language: 'csharp' }))
      expect(r.problems).toEqual([])
      expect(r.blockers).toEqual([])
    })

    test('a bare id that names no member is a problem that lists the members', () => {
      const r = only(around([language, languages], held, { language: 'cobol' }))
      expect(problemsAt(r, `${at(3)}.language`)).toEqual([
        '"cobol" is not a member of "languages" — the members are csharp',
      ])
    })

    test('the member written out in full is still read', () => {
      const r = only(around([language, languages], held, { language: { name: 'C#' } }))
      expect(r.problems).toEqual([])
    })

    test('the parts written in another order are the same value written out', () => {
      const pair = {
        entity: {
          id: 'language',
          is: 'object',
          title: 'L.',
          properties: { name: { is: 'string' }, mark: { is: 'string' } },
        },
      }
      const two = {
        enum: {
          id: 'languages',
          is: 'language',
          title: 'Ls.',
          values: [{ id: 'csharp', value: { name: 'C#', mark: 'c' } }],
        },
      }
      const r = only(around([pair, two], held, { language: { mark: 'c', name: 'C#' } }))
      expect(r.problems).toEqual([])
    })

    test('a value written out that the enum does not list is not a value of it', () => {
      const r = only(around([language, languages], held, { language: { name: 'COBOL' } }))
      expect(problemsAt(r, `${at(3)}.language`)[0]).toContain('is not a member of "languages"')
    })

    test('an enum on a text type does not also take the text itself', () => {
      // Without this an enum would take every value the type it is based on takes, which is no
      // enum at all: the list of values it exists to close would go unchecked.
      const words = { enum: { id: 'words', is: 'string', title: 'W.', values: [{ id: 'a', value: 'x' }] } }
      const r = only(around([words], { w: { is: 'words' } }, { w: 'other' }))
      expect(problemsAt(r, `${at(2)}.w`)).toEqual([
        '"other" is not a member of "words" — the members are a',
      ])
    })

    test('an enum on a number type does not take a number that is not one of its values', () => {
      const codes = {
        enum: { id: 'codes', is: 'number', title: 'C.', values: [{ id: 'missing', value: 404 }] },
      }
      const r = only(around([codes], { c: { is: 'codes' } }, { c: 500 }))
      expect(problemsAt(r, `${at(2)}.c`)).toEqual([
        '500 is not a member of "codes" — the members are missing',
      ])
    })

    test('the value an enum lists is a value of it when it is written instead of the id', () => {
      const codes = {
        enum: { id: 'codes', is: 'number', title: 'C.', values: [{ id: 'missing', value: 404 }] },
      }
      const r = only(around([codes], { c: { is: 'codes' } }, { c: 404 }))
      expect(r.problems).toEqual([])
    })
  })

  describe('a choice between an enum and the type it is based on', () => {
    const colour = {
      enum: {
        id: 'colour',
        is: 'string',
        title: 'The colours there are.',
        values: [
          { id: 'red', value: 'red' },
          { id: 'blue', value: 'blue' },
        ],
      },
    }
    const shade = { choice: { id: 'shade', options: ['colour', 'string'], title: 'Either.' } }
    const held = { paint: { is: 'shade' } }

    test('a value the enum lists is the enum', () => {
      const r = only(around([colour, shade], held, { paint: 'red' }))
      expect(r.problems).toEqual([])
      expect(r.blockers).toEqual([])
    })

    test('a value the enum does not list is the type underneath', () => {
      const r = only(around([colour, shade], held, { paint: 'mauve' }))
      expect(r.problems).toEqual([])
      expect(r.blockers).toEqual([])
    })

    test('a value of neither option is still a problem', () => {
      const r = only(around([colour, shade], held, { paint: 7 }))
      expect(problemsAt(r, `${at(3)}.paint`)[0]).toContain('is not a value of any option of "shade"')
    })
  })

  describe('a value nothing could judge', () => {
    test('a type that is not declared where the value is read is a blocker', () => {
      const r = only(around([], { n: { is: 'ghost' } }, { n: 'x' }))
      expect(blockersAt(r, `${at(1)}.n`)[0]).toContain('"ghost" is not declared')
    })

    test('a part whose type nothing says is a blocker', () => {
      const r = only(around([], { n: { optional: true } }, { n: 'x' }))
      expect(blockersAt(r, `${at(1)}.n`)[0]).toContain('nothing says what type "n" holds')
    })

    test('a type with a constraint no logic applies is a blocker at the value', () => {
      const odd = {
        entity: { id: 'odd', is: 'string', title: 'Odd.', constraints: [{ length: { minimum: 0 } }] },
      }
      const r = only(around([odd], { n: { is: 'odd' } }, { n: 'x' }))
      expect(blockersAt(r, `${at(2)}.n`)[0]).toContain('nothing says how length is applied')
    })

    test('a type whose pattern cannot be read is a blocker at the value, not a pass', () => {
      const odd = {
        entity: { id: 'odd', is: 'string', title: 'Odd.', constraints: [{ pattern: { value: '[a-' } }] },
      }
      const r = only(around([odd], { n: { is: 'odd' } }, { n: 'x' }))
      expect(blockersAt(r, `${at(2)}.n`)[0]).toContain('regular expression')
    })

    test('a primitive nothing says how to read is a blocker', () => {
      const odd = { entity: { id: 'odd', is: 'object', primitive: true, title: 'Odd.' } }
      const r = only(around([odd], { n: { is: 'odd' } }, { n: 'x' }))
      expect(blockersAt(r, `${at(2)}.n`)[0]).toContain('nothing says what one value of an odd')
    })
  })

  describe('a uri', () => {
    const uri = { n: { is: 'uri' } }
    const check1 = (n: unknown): CheckResult => only(around([], uri, { n }))

    test('a scheme, a colon and anything but spaces is one', () => {
      for (const good of ['https://a.example/b', 'urn:x', 'file:///a/b.svg', 'a+b.c-d:e']) {
        expect(check1(good).problems).toEqual([])
      }
    })

    test('text without a scheme, a colon or a remainder, or with a space, is not', () => {
      for (const bad of ['◇', 'a', ':x', '1a:b', 'a:', 'a: b', 'a b:c']) {
        expect(check1(bad).problems).toHaveLength(1)
      }
    })

    test('it derives from string, so a number is not one', () => {
      expect(problemsAt(check1(7), `${at(1)}.n`)).toEqual(['is not written as a string'])
    })
  })
})

describe('the architecture language itself', () => {
  const arch = yaml.load(readFileSync(new URL('./arch.yaml', import.meta.url), 'utf8')) as {
    declare: {
      entity?: { id: string; properties?: Record<string, unknown> }
      enum?: { values: { value: Record<string, unknown> }[] }
    }[]
  }

  test('is checked in full: nothing wrong, nothing unverified, and nothing left to write', () => {
    expect(check(arch, 'arch.yaml')).toEqual({ problems: [], blockers: [], pending: [] })
  })

  test('a value left unwritten is reported, and the document is right until it is given', () => {
    const unwritten = structuredClone(arch)
    const forms = unwritten.declare.find((d) => d.enum?.values.some((v) => 'example' in v.value))
    const one = forms?.enum?.values.find((v) => v.value.name === 'Function')
    if (one === undefined) throw new Error('arch.yaml has no function form')
    one.value.example = 'pending'
    const r = check(unwritten, 'arch.yaml')
    expect(r.problems).toEqual([])
    expect(r.pending).toEqual([
      { where: 'declare[3].enum.values[4].value.example', what: 'is required and not yet given' },
    ])
  })

  test('a mark that is neither a glyph nor a uri is a problem', () => {
    const broken = structuredClone(arch)
    const forms = broken.declare.find((d) => d.enum?.values.some((v) => 'mark' in v.value))
    const first = forms?.enum?.values[0]
    if (first === undefined) throw new Error('arch.yaml has no form to break')
    first.value.mark = 7
    const r = check(broken, 'arch.yaml')
    expect(r.problems).toHaveLength(1)
    expect(r.problems[0]?.what).toContain('is not a value of any option of "mark"')
  })

  test('the element every architecture document names its parts with is declared here', () => {
    const element = arch.declare.find((d) => d.entity?.id === 'element')
    expect(Object.keys(element?.entity?.properties ?? {})).toEqual([
      'form',
      'does',
      'reach',
      'exactness',
      'groups',
      'members',
      'sits',
      'relates',
    ])
  })

  test('the member an element is made of is declared here, carrying its own name', () => {
    // A member is not declared and so has no id to be known by, which is why it carries `name`
    // where an element does not.
    const member = arch.declare.find((d) => d.entity?.id === 'member')
    expect(Object.keys(member?.entity?.properties ?? {})).toEqual([
      'name',
      'kind',
      'reach',
      'type',
      'does',
    ])
  })
})

// What each language calls each form is a document written in the architecture language, so it is
// read against that language rather than against MetaLang.
describe('the naming dictionary', () => {
  const file = join(dirname(LANGUAGE_FILE), 'names.yaml')
  const read = (): { declare: { enum?: { values: { value: Record<string, unknown> }[] } }[] } =>
    yaml.load(readFileSync(file, 'utf8')) as {
      declare: { enum?: { values: { value: Record<string, unknown> }[] } }[]
    }

  test('is checked in full against the architecture language', () => {
    const doc = read()
    const { words, faults } = rulebookFor(doc, file)
    expect(faults).toEqual([])
    expect(check(doc, 'names.yaml', words ?? undefined)).toEqual({
      problems: [],
      blockers: [],
      pending: [],
    })
  })

  test('an entry naming a language that is not declared is a problem', () => {
    const broken = read()
    const first = broken.declare.find((d) => d.enum?.values.some((v) => 'language' in v.value))
      ?.enum?.values[0]
    if (first === undefined) throw new Error('names.yaml has no entry to break')
    first.value.language = 'cobol'
    const { words } = rulebookFor(broken, file)
    const r = check(broken, 'names.yaml', words ?? undefined)
    expect(r.problems).toHaveLength(1)
    expect(r.problems[0]?.what).toContain('"cobol" is not a member of "languages"')
  })

  test('an entry naming a form that is not one is a problem', () => {
    const broken = read()
    const first = broken.declare.find((d) => d.enum?.values.some((v) => 'form' in v.value))?.enum
      ?.values[0]
    if (first === undefined) throw new Error('names.yaml has no entry to break')
    first.value.form = 'struct'
    const { words } = rulebookFor(broken, file)
    const r = check(broken, 'names.yaml', words ?? undefined)
    expect(r.problems).toHaveLength(1)
    expect(r.problems[0]?.what).toContain('"struct" is not a member of "forms"')
  })

  test('every form is named in every language, so no cell is left out', () => {
    const entries = read().declare.find((d) => d.enum?.values.some((v) => 'form' in v.value))?.enum
      ?.values
    expect(entries).toHaveLength(24)
  })
})

describe('a value that is required and not yet given', () => {
  // A property says which reserved states it takes, and the word is only read as one where it
  // does. The live vocabulary declares `reserved` and its two members, so nothing here does.
  const reaching = (extra: Record<string, unknown>): Record<string, unknown> => ({
    scope: ['arch'],
    import: [['metalang']],
    title: 'The architecture language',
    ...extra,
  })

  const written = (accepts: unknown, value: unknown): unknown =>
    reaching({
      declare: [
        {
          entity: {
            id: 'form',
            is: 'object',
            properties: {
              example: accepts === null ? { is: 'string' } : { is: 'string', accepts },
            },
          },
        },
        { enum: { id: 'forms', is: 'form', values: [{ id: 'one', value: { example: value } }] } },
      ],
    })

  const where = 'declare[1].enum.values[0].value.example'
  const pendingAt = (r: CheckResult, at: string): string[] =>
    r.pending.filter((p) => p.where === at).map((p) => p.what)

  test('is reported in a list of its own, and nothing is wrong with the document', () => {
    const r = only(written(['pending'], 'pending'))
    expect(pendingAt(r, where)).toEqual(['is required and not yet given'])
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('one deliberately left empty says nothing at all', () => {
    const r = only(written(['pending', 'none'], 'none'))
    expect(r.pending).toEqual([])
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
  })

  test('the word is the text it is written as where the property does not take it', () => {
    const r = only(written(null, 'pending'))
    expect(r.pending).toEqual([])
    expect(r.problems).toEqual([])
  })

  test('a state the property takes but nothing explains blocks rather than passing', () => {
    const r = only(written(['later'], 'later'))
    expect(blockersAt(r, where)).toEqual(['nothing says what "later" means where a value goes'])
  })

  test('the value is not also read against the type it would otherwise hold', () => {
    const r = only(written(['pending'], 'pending'))
    expect(r.problems).toEqual([])
    expect(r.pending).toHaveLength(1)
  })

  test('one inside the option of a choice that took the value is said, and a loser\'s is not', () => {
    const r = only(
      reaching({
        declare: [
          {
            entity: {
              id: 'A',
              is: 'object',
              properties: { example: { is: 'string', accepts: ['pending'] } },
            },
          },
          { entity: { id: 'B', is: 'object', properties: { other: { is: 'string' } } } },
          { choice: { id: 'either', options: ['A', 'B'] } },
          { entity: { id: 'form', is: 'object', properties: { held: { is: 'either' } } } },
          {
            enum: {
              id: 'forms',
              is: 'form',
              values: [{ id: 'one', value: { held: { example: 'pending' } } }],
            },
          },
        ],
      }),
    )
    expect(pendingAt(r, 'declare[4].enum.values[0].value.held.example')).toEqual([
      'is required and not yet given',
    ])
    expect(r.problems).toEqual([])
  })

  test('the run it is reported in exits non-zero, because the document is not finished', async () => {
    const path = `${import.meta.dir}/.pending.test.yaml`
    await Bun.write(
      path,
      'scope: [x]\ntitle: t\nlanguage: metalang\ndeclare:\n' +
        '  - entity:\n      id: form\n      is: object\n      properties:\n' +
        '        example: { is: string, accepts: [pending] }\n' +
        '  - enum:\n      id: forms\n      is: form\n      values:\n' +
        '        - id: one\n          value: { example: pending }\n',
    )
    try {
      expect(run(path)).toBe(1)
    } finally {
      await Bun.file(path).delete()
    }
  })
})

describe('the report, with a value not yet given in it', () => {
  const lines = (pending: { where: string; what: string }[], problems: typeof pending = []) =>
    reportLines('f.yaml', { problems, blockers: [], pending }, 1)

  test('is a list of its own, headed as what it holds', () => {
    expect(lines([{ where: 'a', what: 'is required and not yet given' }])).toContain(
      '1 pending value:',
    )
  })

  test('pluralises the way every other list does', () => {
    const two = lines([
      { where: 'a', what: 'one' },
      { where: 'b', what: 'two' },
    ])
    expect(two).toContain('2 pending values:')
  })

  test('closes by saying the document is right and not finished', () => {
    expect(lines([{ where: 'a', what: 'b' }]).at(-1)).toBe(
      'Nothing is wrong with the document, and it is not finished.',
    )
  })

  test('a document with nothing pending closes as it always did', () => {
    expect(reportLines('f.yaml', { problems: [], blockers: [], pending: [] }, 1).at(-1)).toBe(
      'Checked, and nothing is wrong.',
    )
  })

  test('what is wrong and what is not finished are counted apart', () => {
    const both = lines([{ where: 'a', what: 'not given' }], [{ where: 'b', what: 'wrong' }])
    expect(both).toContain('1 problem:')
    expect(both).toContain('1 pending value:')
    expect(both.some((l) => l.startsWith('Nothing is wrong'))).toBe(false)
  })
})

// `accepts` says which reserved states a property takes in place of a value. It is read the same
// way wherever it is written: on a part of a value, and on a keyword a declaration carries. Two
// readings of one word would let a decided absence check clean in one place and fail in the other.
describe('a reserved state written on a keyword a declaration carries', () => {
  const metalang = yaml.load(readFileSync(LANGUAGE_FILE, 'utf8'))

  /** A language whose one kind carries a keyword holding a list, and one holding text. */
  const words = ((): Vocabulary => {
    const { words: made, faults } = resolveVocabulary(
      {
        scope: ['tiny'],
        import: [['metalang']],
        title: 'A tiny language',
        declare: [
          {
            entity: {
              id: 'note',
              is: 'object',
              title: 'A note.',
              properties: {
                id: { is: 'name', title: 'The name it is known by.' },
                lines: {
                  list: { of: 'string' },
                  accepts: ['none', 'pending'],
                  title: 'The lines it carries.',
                },
                word: { is: 'string', accepts: ['none'], title: 'The word it names.' },
              },
            },
          },
          {
            choice: {
              id: 'declaration',
              options: ['note', 'entity', 'enum', 'choice', 'constant', 'rule', 'fact', 'external'],
              title: 'What may be declared.',
            },
          },
        ],
      },
      [metalang],
    )
    if (faults.length > 0) throw new Error(faults.join(' '))
    return made
  })()

  const doc = (body: Record<string, unknown>): CheckResult =>
    check(
      {
        scope: ['doc'],
        language: 'tiny',
        import: [['metalang']],
        title: 'A document',
        declare: [{ note: { id: 'A', ...body } }],
      },
      'f.yaml',
      words,
    )

  test('none on a keyword holding a list is a decided absence and nothing is wrong', () => {
    expect(doc({ lines: 'none', word: 'none' })).toEqual({ problems: [], blockers: [], pending: [] })
  })

  test('pending on a keyword holding a list is reported as pending, not as a problem', () => {
    const r = doc({ lines: 'pending', word: 'none' })
    expect(r.problems).toEqual([])
    expect(r.blockers).toEqual([])
    expect(r.pending.map((f) => [f.where, f.what])).toEqual([
      ['declare[0].note.lines', 'is required and not yet given'],
    ])
  })

  test('a real list is still read as a list, so the states do not replace the check', () => {
    expect(doc({ lines: ['a', 'b'], word: 'none' })).toEqual({
      problems: [],
      blockers: [],
      pending: [],
    })
  })

  test('a word the keyword does not accept is still checked as the value it is', () => {
    const r = doc({ lines: 'nothing', word: 'none' })
    expect(problemsAt(r, 'declare[0].note.lines')).toEqual([
      'is not written as a list, which is how a value of a list is written',
    ])
  })

  test('none on a keyword holding text is the state, and any other text is the text', () => {
    expect(doc({ lines: 'none', word: 'none' }).problems).toEqual([])
    expect(doc({ lines: 'none', word: 'role' }).problems).toEqual([])
  })
})

// ── a value keyed by a closed set ────────────────────────────────────────────
//
// `map` is written as one line for each word of the set named under `by`. The keys belong to the
// set, so what a document may write is decided by the language and not by whoever is writing. All
// four things that can go wrong are checked here: a key the set does not list, a word with no line
// at all, a line whose value is not of the type each line holds, and the whole thing written as a
// list, which is how every other collection is written and is not how this one is.
describe('a value keyed by a closed set', () => {
  const language = (sits: unknown, value: unknown): Record<string, unknown> => ({
    scope: ['arch'],
    import: [['metalang']],
    title: 'The architecture language',
    declare: [
      {
        entity: {
          id: 'place',
          is: 'object',
          title: 'One kind of place.',
          properties: { name: { is: 'string', title: 'The name it is shown by.' } },
        },
      },
      {
        enum: {
          id: 'places',
          is: 'place',
          title: 'Every kind of place, widest first.',
          values: [
            { id: 'package', value: { name: 'Package' } },
            { id: 'assembly', value: { name: 'Assembly' } },
          ],
        },
      },
      {
        entity: {
          id: 'element',
          is: 'object',
          title: 'One named piece of an architecture.',
          properties: { sits: { map: sits, title: 'Where it sits.' } },
        },
      },
      { constant: { id: 'thing', is: 'element', title: 'A thing.', value: { sits: value } } },
    ],
  })

  const keyed = { by: 'places', of: 'string', accepts: ['none'] }
  const where = 'declare[3].constant.value.sits'
  const written = (value: unknown): CheckResult => only(language(keyed, value))

  test('one line for each word of the set is what the whole set asks for', () => {
    expect(written({ package: 'AgentGuard.Engine', assembly: 'AgentGuard.Engine' }).problems).toEqual([])
  })

  test('each line is read against the type every line holds', () => {
    const r = written({ package: 'AgentGuard.Engine', assembly: 7 })
    expect(problemsAt(r, `${where}.assembly`)).toEqual(['is not written as a string'])
  })

  test('a key the set does not list is named against the words it does', () => {
    const r = written({ package: 'a', assembly: 'b', folder: 'src' })
    expect(problemsAt(r, `${where}.folder`)).toEqual([
      '"folder" is not a member of "places" — the members are package, assembly',
    ])
  })

  test('a word with no line is reported rather than read as a word holding nothing', () => {
    expect(problemsAt(written({ package: 'a' }), where)).toEqual(['has no assembly'])
  })

  test('a word that holds nothing says so with a state the keys are written to take', () => {
    expect(written({ package: 'none', assembly: 'a' }).problems).toEqual([])
  })

  test('a state the keys are not written to take is read as the value it is', () => {
    const plain = { by: 'places', of: 'string' }
    const r = only(language(plain, { package: 'none', assembly: 7 }))
    expect(problemsAt(r, `${where}.assembly`)).toEqual(['is not written as a string'])
    expect(problemsAt(r, `${where}.package`)).toEqual([])
  })

  test('written as a list, which is how every other collection is written, is a problem', () => {
    expect(problemsAt(written([{ place: 'package', name: 'a' }]), where)).toEqual([
      'is not written as one line for each word of "places", which is how a value of a map is written',
    ])
  })

  test('the states the keys take are checked where they are written', () => {
    const r = only(language({ by: 'places', of: 'string', accepts: ['sometime'] }, { package: 'a', assembly: 'b' }))
    expect(problemsAt(r, 'declare[2].entity.properties.sits.map.accepts[0]')[0]).toContain(
      'is not a member of "reserved"',
    )
  })
})

// ── a value naming one declaration ───────────────────────────────────────────
//
// A relation is one thing pointing at another, so the name it is written as has to be read against
// what the document reaches rather than taken as text. Everything that can go wrong with a pointer
// is checked here: a name nothing declares, a name that is a type rather than a thing carrying a
// value, and a name whose declaration carries a value of some other type.
describe('a value naming one declaration', () => {
  const language = (stands: unknown, relates: unknown): Record<string, unknown> => ({
    scope: ['arch'],
    import: [['metalang']],
    title: 'The architecture language',
    declare: [
      {
        entity: {
          id: 'element',
          is: 'object',
          title: 'One named piece of an architecture.',
          properties: {
            does: { is: 'string', title: 'One line saying what it does.' },
            stands: { link: { to: 'element' }, accepts: ['none'], title: 'The one it stands to.' },
            relates: {
              list: { of: { link: { to: 'element' } } },
              accepts: ['none'],
              title: 'The ones it stands to.',
            },
          },
        },
      },
      { constant: { id: 'note', is: 'string', title: 'A note.', value: 'anything at all' } },
      {
        constant: {
          id: 'IGuard',
          is: 'element',
          title: 'A surface.',
          value: { does: 'A declaration of surface.', stands: 'none', relates: 'none' },
        },
      },
      {
        constant: {
          id: 'FileGuard',
          is: 'element',
          title: 'A class.',
          value: { does: 'An implementation.', stands, relates },
        },
      },
    ],
  })

  const at = 'declare[3].constant.value'
  const stands = (value: unknown): CheckResult => only(language(value, 'none'))

  test('a name the document declares, carrying a value of the type named, is right', () => {
    expect(only(language('IGuard', ['IGuard'])).problems).toEqual([])
  })

  test('a name nothing the document reaches declares is reported where it is written', () => {
    expect(problemsAt(stands('Ghost'), `${at}.stands`)).toEqual([
      'nothing this document reaches declares "Ghost"',
    ])
  })

  test('a name that is a type rather than a thing carrying a value is reported as that', () => {
    expect(problemsAt(stands('element'), `${at}.stands`)).toEqual([
      '"element" carries no value of its own, so it is not an element',
    ])
  })

  test('a name whose declaration carries a value of another type is reported as what it is', () => {
    expect(problemsAt(stands('note'), `${at}.stands`)).toEqual([
      '"note" carries a value of "string", and what is named here is an element',
    ])
  })

  test('a name is a name, so anything else written in its place is a problem', () => {
    expect(problemsAt(stands(7), `${at}.stands`)).toEqual([
      'is not written as the name of a declaration, which is how a value of a link is written',
    ])
  })

  test('many of them is many names, each read the same way', () => {
    const r = only(language('none', ['IGuard', 'Ghost']))
    expect(problemsAt(r, `${at}.relates[0]`)).toEqual([])
    expect(problemsAt(r, `${at}.relates[1]`)).toEqual([
      'nothing this document reaches declares "Ghost"',
    ])
  })

  test('a thing standing to nothing says so with the state the property takes', () => {
    expect(only(language('none', 'none')).problems).toEqual([])
  })
})

/**
 * A word that is retired and the one that took its place. The retired word's declaration may be
 * gone; what the document is told is what to write now, rather than that it invented a name.
 */
describe('a word that has been retired', () => {
  const retiring = (declare: readonly unknown[]): CheckResult =>
    check({ scope: ['retired'], import: [['metalang']], title: 'A file', declare }, 'f.yaml')

  test('a document naming the gone word is told which one took its place', () => {
    const r = retiring([
      { entity: { id: 'newname', is: 'string', replaces: 'oldname', title: 'The one that took its place.' } },
      { entity: { id: 'thing', is: 'object', title: 'A thing.', properties: { part: { is: 'oldname', title: 'Old.' } } } },
    ])
    expect(blockersAt(r, 'declare[1].entity.properties.part.is')).toEqual([
      '"oldname" has been retired — "newname" takes its place',
    ])
  })

  test('a name nothing replaced is still a name nothing declares', () => {
    const r = retiring([
      { entity: { id: 'thing', is: 'object', title: 'A thing.', properties: { part: { is: 'nowhere', title: 'Gone.' } } } },
    ])
    expect(blockersAt(r, 'declare[0].entity.properties.part.is')).toEqual([
      '"nowhere" is declared nowhere in this file and no scope it imports brings it in',
    ])
  })

  test('nothing takes the place of a locked declaration by being approved', () => {
    const r = retiring([
      { entity: { id: 'settled', is: 'string', standing: 'locked', title: 'Locked.' } },
      {
        entity: {
          id: 'upstart',
          is: 'string',
          replaces: 'settled',
          standing: 'approved',
          title: 'One that tries.',
        },
      },
    ])
    expect(problemsAt(r, 'declare[1].entity')).toEqual([
      'it writes "approved" at "standing", which it may never carry, where "replaces.standing" is' +
        ' "locked", which "declared" requires of every value of it',
    ])
  })

  test('taking the place of a word that is not locked is nothing to report', () => {
    const r = retiring([
      { entity: { id: 'older', is: 'string', standing: 'withdrawn', title: 'Retired.' } },
      {
        entity: {
          id: 'newer',
          is: 'string',
          replaces: 'older',
          standing: 'approved',
          title: 'The one after it.',
        },
      },
    ])
    expect(problemsAt(r, 'declare[1].entity')).toEqual([])
  })
})
