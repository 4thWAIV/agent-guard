// What the reader makes of what is written: how a value of each primitive is read, and what a type
// says its values may be.
//
// Three things are worth proving here. One table says how a value of a primitive is read, so the
// checker and the colouring cannot disagree about what a word carries. A type is held to what it
// derives as well as to what it writes, because that is the whole reason a constraint sits on a
// type rather than on a value. And a kind of constraint nothing here knows how to apply says so,
// rather than letting a value through unjudged.

import { describe, expect, test } from 'bun:test'

import { WORDS } from './rulebook.ts'
import {
  cannotApply,
  constraintsOn,
  logicFor,
  reachedBy,
  readerFor,
  unmetBy,
  type Constraint,
} from './constraints.ts'
import type { BodyOf, Mapping } from './words.ts'

/** A document's declarations, by id, in the shape the rules read them in. */
const bodies = (declared: Record<string, Mapping>): BodyOf => (id) => declared[id]

const pattern = (value: unknown): Mapping => ({ pattern: { value } })

const name: Record<string, Mapping> = {
  string: { id: 'string', primitive: true },
  name: { id: 'name', is: 'string', constraints: [pattern('[A-Za-z_][A-Za-z0-9_]*')] },
}

const one = (from: string, kind: string, body: unknown): Constraint => ({
  from,
  index: 0,
  kind,
  body,
})

describe('how a value of a primitive is read', () => {
  test('a word carries the value the primitive makes of it', () => {
    expect(readerFor('boolean')?.from('true')).toEqual({ read: true })
    expect(readerFor('boolean')?.from('false')).toEqual({ read: false })
    expect(readerFor('number')?.from('-1.5')).toEqual({ read: -1.5 })
    expect(readerFor('byte')?.from('3')).toEqual({ read: 3 })
    expect(readerFor('string')?.from('3')).toEqual({ read: '3' })
    expect(readerFor('expression')?.from('[a-z]')).toEqual({ read: '[a-z]' })
    expect(readerFor('uuid')?.from('x')).toEqual({ read: 'x' })
  })

  test('a word that is not a value of the primitive carries nothing', () => {
    expect(readerFor('boolean')?.from('yes')).toBeNull()
    expect(readerFor('number')?.from('three')).toBeNull()
    expect(readerFor('byte')?.from('')).toBeNull()
  })

  test('a set of named parts is never one word, so one word carries nothing of it', () => {
    expect(readerFor('object')?.from('wall')).toBeNull()
  })

  test('a value already read out of a file is judged as it stands', () => {
    expect(readerFor('number')?.is(7)).toBe(true)
    expect(readerFor('number')?.is('7')).toBe(false)
    expect(readerFor('boolean')?.is(true)).toBe(true)
    expect(readerFor('string')?.is('a')).toBe(true)
    expect(readerFor('expression')?.is('[a-z]')).toBe(true)
    expect(readerFor('uuid')?.is('x')).toBe(true)
    expect(readerFor('byte')?.is(3)).toBe(true)
    expect(readerFor('object')?.is({})).toBe(true)
    expect(readerFor('object')?.is('a')).toBe(false)
  })

  test('a primitive nothing here says is read by nothing, rather than read loosely', () => {
    expect(readerFor('instant')).toBeNull()
  })
})

describe('the constraints a type carries', () => {
  test('a type carries the ones it writes', () => {
    const found = constraintsOn(bodies(name), 'name')
    expect(found.length).toBe(1)
    expect(found[0]?.kind).toBe('pattern')
    expect(found[0]?.from).toBe('name')
    expect(found[0]?.index).toBe(0)
  })

  test('a type carries what it derives as well, its own first', () => {
    const found = constraintsOn(
      bodies({
        ...name,
        upper: { id: 'upper', is: 'name', constraints: [pattern('[A-Z].*')] },
      }),
      'upper',
    )
    expect(found.map((c) => c.from)).toEqual(['upper', 'name'])
  })

  test('a type deriving from several carries the constraints of each', () => {
    const found = constraintsOn(
      bodies({
        ...name,
        short: { id: 'short', constraints: [pattern('.{1,8}')] },
        both: { id: 'both', is: ['name', 'short'] },
      }),
      'both',
    )
    expect(found.map((c) => c.from)).toEqual(['name', 'short'])
  })

  test('a type that reaches itself is read once rather than for ever', () => {
    const found = constraintsOn(
      bodies({
        a: { id: 'a', is: 'b', constraints: [pattern('a+')] },
        b: { id: 'b', is: 'a', constraints: [pattern('b+')] },
      }),
      'a',
    )
    expect(found.map((c) => c.from)).toEqual(['a', 'b'])
  })

  test('a type with none, and a type declared nowhere, each carry none', () => {
    expect(constraintsOn(bodies(name), 'string')).toEqual([])
    expect(constraintsOn(bodies(name), 'nothing')).toEqual([])
  })

  test('constraints written as anything but a list carry none', () => {
    expect(constraintsOn(bodies({ a: { id: 'a', constraints: 'pattern' } }), 'a')).toEqual([])
  })

  test('a constraint naming no word is left out, to be reported where it is written', () => {
    const found = constraintsOn(bodies({ a: { id: 'a', constraints: [{}, 7, pattern('x')] } }), 'a')
    expect(found.map((c) => c.kind)).toEqual(['pattern'])
  })
})

describe('applying a pattern', () => {
  test('a value matching the whole pattern meets it', () => {
    expect(unmetBy(one('name', 'pattern', { value: '[a-z]+' }), 'scope')).toBeNull()
  })

  test('a value that does not match says what it did not match', () => {
    expect(unmetBy(one('name', 'pattern', { value: '[a-z]+' }), '2nd')).toBe(
      'does not match [a-z]+',
    )
  })

  test('a value that is not text cannot be matched against a pattern at all', () => {
    expect(unmetBy(one('name', 'pattern', { value: '[a-z]+' }), 7)).toContain('is not text')
  })

  test('a pattern that is not a regular expression says so where it is written', () => {
    const logic = logicFor('pattern')
    expect(logic?.fault({ value: '[a-z' })).toContain('is not a regular expression')
    expect(logic?.fault({ value: '[a-z]+' })).toBeNull()
  })

  test('a pattern holding something that is not text is the type of the property to report', () => {
    expect(logicFor('pattern')?.fault({ value: 7 })).toBeNull()
  })
})

describe('a kind of constraint nothing here applies', () => {
  test('nothing says how it is applied', () => {
    expect(logicFor('length')).toBeNull()
  })

  test('it is said to be one that cannot be applied, and a value is never judged to meet it', () => {
    const length = one('string', 'length', { minimum: 0 })
    expect(cannotApply(length)).toContain('nothing says how length is applied')
    expect(() => unmetBy(length, 7)).toThrow('cannot be applied')
  })

  test('a constraint whose body is not a mapping cannot be applied', () => {
    const bad = one('name', 'pattern', '[a-z]+')
    expect(cannotApply(bad)).toContain('not written as a mapping')
    expect(() => unmetBy(bad, 'x')).toThrow('cannot be applied')
  })

  test('a pattern with no readable expression cannot be applied, whatever it holds', () => {
    expect(cannotApply(one('name', 'pattern', { value: '[a-z' }))).toContain('regular expression')
    expect(cannotApply(one('name', 'pattern', { value: 7 }))).toContain('written as text')
    expect(cannotApply(one('name', 'pattern', { value: '[a-z]+' }))).toBeNull()
  })

  test('a range that allows nothing cannot be applied', () => {
    expect(cannotApply(one('byte', 'range', {}))).toContain('neither a minimum nor a maximum')
  })
})

describe('what the language says a byte and a uuid may be', () => {
  const live: BodyOf = (id) => WORDS.declared[id]?.body

  const why = (type: string, value: unknown): string | null => {
    for (const constraint of constraintsOn(live, type)) {
      const found = unmetBy(constraint, value)
      if (found !== null) return found
    }
    return null
  }

  test('a byte is every whole number from 0 to 255 and nothing else', () => {
    expect(why('byte', 0)).toBeNull()
    expect(why('byte', 255)).toBeNull()
    expect(why('byte', 256)).toBe('is above 255')
    expect(why('byte', -1)).toBe('is below 0')
  })

  test('a uuid is the shape a uuid is written in', () => {
    expect(why('uuid', '550e8400-e29b-41d4-a716-446655440000')).toBeNull()
    expect(why('uuid', '550E8400-E29B-41D4-A716-446655440000')).toBeNull()
    expect(why('uuid', '550e8400e29b41d4a716446655440000')).toContain('does not match')
    expect(why('uuid', '550e8400-e29b-41d4-a716-44665544000')).toContain('does not match')
    expect(why('uuid', 'zzze8400-e29b-41d4-a716-446655440000')).toContain('does not match')
  })

  test('a name is a letter or an underscore, then letters, digits and underscores', () => {
    expect(why('name', 'scope')).toBeNull()
    expect(why('name', '_a9')).toBeNull()
    expect(why('name', '9a')).toContain('does not match')
    expect(why('name', 'a-b')).toContain('does not match')
  })
})

describe('applying a range', () => {
  const range = (body: unknown): Constraint => ({ from: 'byte', index: 0, kind: 'range', body })

  test('a value inside the range meets it, at either end', () => {
    expect(unmetBy(range({ minimum: 1, maximum: 3 }), 1)).toBeNull()
    expect(unmetBy(range({ minimum: 1, maximum: 3 }), 3)).toBeNull()
  })

  test('a range with one end open holds only that end', () => {
    expect(unmetBy(range({ minimum: 1 }), 900)).toBeNull()
    expect(unmetBy(range({ maximum: 3 }), -900)).toBeNull()
  })

  test('a value that is not a number cannot be read against a range at all', () => {
    expect(unmetBy(range({ minimum: 1 }), 'two')).toContain('is not a number')
  })

  test('a range saying neither end is a range that was not worth writing', () => {
    expect(logicFor('range')?.fault({})).toBe('says neither a minimum nor a maximum')
  })

  test('a range whose minimum is above its maximum allows nothing', () => {
    expect(logicFor('range')?.fault({ minimum: 9, maximum: 2 })).toContain('allows nothing')
  })

  test('an end written as something other than a number is the type of the property to report', () => {
    expect(logicFor('range')?.fault({ minimum: 'nine' })).toBeNull()
  })
})

/**
 * No two members of one list carry the same value at one part. It is the constraint MetaLang's own
 * `role` needed: a role is given to at most one property of a type, which the language said in
 * prose and could not write.
 */
describe('distinct', () => {
  const found = logicFor('distinct')
  if (found === null) throw new Error('the reader says nothing about how distinct is applied')
  const logic = found

  test('says so where it names neither part', () => {
    expect(logic.fault({ by: 'role' })).toBe('does not name the part it reads')
    expect(logic.fault({ in: 'properties' })).toBe('does not name the part no two may share')
    expect(logic.fault({ in: 'properties', by: 'role' })).toBeNull()
  })

  test('passes members that differ, and members leaving the part out', () => {
    const body = { in: 'properties', by: 'role' }
    expect(logic.unmet(body, { properties: { a: { role: 'name' }, b: { role: 'mark' } } })).toBeNull()
    expect(logic.unmet(body, { properties: { a: { role: 'name' }, b: {} } })).toBeNull()
    expect(logic.unmet(body, { properties: {} })).toBeNull()
  })

  test('names the part, the value and both that carry it when two share one', () => {
    const why = logic.unmet(
      { in: 'properties', by: 'role' },
      { properties: { label: { role: 'name' }, alias: { role: 'name' } } },
    )
    expect(why).toBe('writes "name" at "role" on more than one of "properties" — label and alias')
  })

  test('reads a list as well as a block keyed by name', () => {
    const why = logic.unmet(
      { in: 'values', by: 'mark' },
      { values: [{ id: 'one', mark: '#' }, { id: 'two', mark: '#' }] },
    )
    expect(why).toBe('writes "#" at "mark" on more than one of "values" — one and two')
  })

  test('says what is wrong where the value is not a set of named parts', () => {
    expect(logic.unmet({ in: 'properties', by: 'role' }, 'text')).toBe(
      'is not a set of named parts, so "properties" cannot be read from it',
    )
  })
})

/**
 * A route to a part of a value. It descends only through content the value owns, which forms a
 * tree, so it always ends; a part holding many of something fans out, so one path names the same
 * part of every member of a list.
 */
describe('the part a path reaches', () => {
  test('reaches a part of the value, and a part of a part', () => {
    expect(reachedBy({ a: 1 }, 'a')).toEqual({ reached: [1] })
    expect(reachedBy({ a: { b: 'x' } }, 'a.b')).toEqual({ reached: ['x'] })
  })

  test('fans out through many of something', () => {
    expect(reachedBy({ members: [{ type: 'a' }, { type: 'b' }] }, 'members.type')).toEqual({
      reached: ['a', 'b'],
    })
  })

  test('reaches nothing where nothing is written there, rather than reaching an absence', () => {
    expect(reachedBy({ a: 1 }, 'b')).toEqual({ reached: [] })
    expect(reachedBy({ a: { b: 1 } }, 'a.c')).toEqual({ reached: [] })
  })

  // A route that asks for a part of something with no parts is not an absence. A name pointing at
  // another declaration is the case that matters: until a path may cross one, a rule written
  // through a reference would find nothing and pass, which is the loudest kind of quiet failure.
  test('stops and says so where it asks for a part of something that has none', () => {
    expect(reachedBy({ a: 1 }, 'a.b')).toEqual({
      stopped: 'cannot be read at "a.b", because "b" is asked of something with no named parts',
    })
    expect(reachedBy({ holds: 'InEngine' }, 'holds.where')).toEqual({
      stopped: 'cannot be read at "holds.where", because "where" is asked of something with no named parts',
    })
    expect(reachedBy('text', 'a')).toEqual({
      stopped: 'cannot be read at "a", because "a" is asked of something with no named parts',
    })
  })
})

/** A prohibition: nothing the path reaches may carry one of these. */
describe('never', () => {
  const found = logicFor('never')
  if (found === null) throw new Error('the reader says nothing about how never is applied')
  const logic = found

  test('says so where it names neither the part nor what is refused', () => {
    expect(logic.fault({ carries: ['none'] })).toBe('does not name the part it is about')
    expect(logic.fault({ at: 'x' })).toBe('does not say which values are refused')
    expect(logic.fault({ at: 'x', carries: ['none'] })).toBeNull()
  })

  test('passes a value carrying something else, and one carrying nothing there', () => {
    const body = { at: 'members.type', carries: ['none'] }
    expect(logic.unmet(body, { members: [{ type: 'uint32' }] })).toBeNull()
    expect(logic.unmet(body, { members: [{}] })).toBeNull()
    expect(logic.unmet(body, {})).toBeNull()
  })

  test('says so where the route could not be followed, rather than passing what it never read', () => {
    expect(logic.unmet({ at: 'holds.where', carries: ['engine'] }, { holds: 'InEngine' })).toBe(
      'cannot be read at "holds.where", because "where" is asked of something with no named parts',
    )
  })

  test('names the word and the part where one of them carries it', () => {
    expect(
      logic.unmet({ at: 'members.type', carries: ['none'] }, { members: [{ type: 'uint32' }, { type: 'none' }] }),
    ).toBe('writes "none" at "members.type", which it may never carry')
  })
})

/** A condition: the constraint it holds is applied only where another part says so. */
describe('when', () => {
  const found = logicFor('when')
  if (found === null) throw new Error('the reader says nothing about how when is applied')
  const logic = found

  const inner = { never: { at: 'members.type', carries: ['none'] } }
  const body = { at: 'exactness', holds: ['exact'], then: inner }

  test('says so where what it holds could not be applied', () => {
    expect(logic.fault({ holds: ['exact'], then: inner })).toBe('does not name the part that decides it')
    expect(logic.fault({ at: 'exactness', then: inner })).toBe('does not say which values make it apply')
    expect(logic.fault({ at: 'exactness', holds: ['exact'] })).toBe(
      'does not hold exactly one constraint to apply',
    )
    expect(logic.fault({ at: 'exactness', holds: ['exact'], then: { never: { at: 'x' } } })).toBe(
      'holds never, which does not say which values are refused',
    )
    expect(logic.fault(body)).toBeNull()
  })

  test('says so where the route that decides it could not be followed', () => {
    expect(logic.unmet({ at: 'a.b', holds: ['x'], then: inner }, { a: 'word' })).toBe(
      'cannot be read at "a.b", because "b" is asked of something with no named parts',
    )
  })

  test('is not applied where the part it names says otherwise', () => {
    expect(logic.unmet(body, { exactness: 'loose', members: [{ type: 'none' }] })).toBeNull()
    expect(logic.unmet(body, { members: [{ type: 'none' }] })).toBeNull()
  })

  test('is applied where it says so, and says which part made it apply', () => {
    expect(logic.unmet(body, { exactness: 'exact', members: [{ type: 'none' }] })).toBe(
      'writes "none" at "members.type", which it may never carry, where "exactness" is "exact"',
    )
    expect(logic.unmet(body, { exactness: 'exact', members: [{ type: 'uint32' }] })).toBeNull()
  })
})

/**
 * A path crossing a name. A part naming another declaration holds the name, not the thing, so the
 * route follows it; a route returning to one it has already been through ends there, which is what
 * makes a path that crosses references end at all.
 */
describe('a path crossing a reference', () => {
  const held: Record<string, Mapping> = {
    InEngine: { where: 'engine' },
    InAbstractions: { where: 'abstractions' },
    A: { where: 'good', points: 'B' },
    B: { where: 'good', points: 'A' },
  }
  const through = (name: string): Mapping | undefined => held[name]

  test('descends through owned content without one, as it always did', () => {
    expect(reachedBy({ a: { b: 'x' } }, 'a.b')).toEqual({ reached: ['x'] })
  })

  test('follows a name to what that declaration carries', () => {
    expect(reachedBy({ holds: 'InEngine' }, 'holds.where', through)).toEqual({ reached: ['engine'] })
  })

  test('follows every name a part holds many of', () => {
    expect(
      reachedBy({ holds: ['InEngine', 'InAbstractions'] }, 'holds.where', through),
    ).toEqual({ reached: ['engine', 'abstractions'] })
  })

  test('ends where it returns to a declaration it has already been through', () => {
    expect(reachedBy({ points: 'A' }, 'points.points.points.where', through)).toEqual({
      stopped:
        'cannot be read at "points.points.points.where", because it returns to "A", which it has already been through',
    })
  })

  test('stops where the name names nothing it can reach', () => {
    expect(reachedBy({ holds: 'Nowhere' }, 'holds.where', through)).toEqual({
      stopped:
        'cannot be read at "holds.where", because "where" is asked of something with no named parts',
    })
  })
})

/** How many of a thing a part holds: the count of what a path reaches, held to a range. */
describe('many', () => {
  const found = logicFor('many')
  if (found === null) throw new Error('the reader says nothing about how many is applied')
  const logic = found

  test('says so where it names neither the part nor how many are allowed', () => {
    expect(logic.fault({ bounds: { minimum: 1 } })).toBe('does not name the part it counts')
    expect(logic.fault({ at: 'parts' })).toBe('does not say how many are allowed')
    expect(logic.fault({ at: 'parts', bounds: {} })).toBe(
      'says how many as a range that says neither a minimum nor a maximum',
    )
    expect(logic.fault({ at: 'parts', bounds: { minimum: 1 } })).toBeNull()
  })

  test('counts what the part holds and passes a count inside the bounds', () => {
    const body = { at: 'parts', bounds: { minimum: 1, maximum: 3 } }
    expect(logic.unmet(body, { parts: ['a', 'b'] })).toBeNull()
    expect(logic.unmet(body, { parts: ['a'] })).toBeNull()
  })

  test('names the count and the bound it is outside', () => {
    const body = { at: 'parts', bounds: { minimum: 1, maximum: 3 } }
    expect(logic.unmet(body, { parts: [] })).toBe('holds 0 at "parts", which is below 1')
    expect(logic.unmet(body, {})).toBe('holds 0 at "parts", which is below 1')
    expect(logic.unmet(body, { parts: ['a', 'b', 'c', 'd'] })).toBe(
      'holds 4 at "parts", which is above 3',
    )
  })

  test('says so where the route could not be followed, rather than counting nothing', () => {
    expect(logic.unmet({ at: 'a.b', bounds: { minimum: 1 } }, { a: 'word' })).toBe(
      'cannot be read at "a.b", because "b" is asked of something with no named parts',
    )
  })
})
