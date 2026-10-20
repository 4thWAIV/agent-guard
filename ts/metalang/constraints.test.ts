// What a type says its values may be.
//
// Two things are worth proving here. A type is held to what it derives as well as to what it
// writes, because that is the whole reason a constraint sits on a type rather than on a value.
// And a kind of constraint nothing here knows how to apply says so, rather than letting a value
// through unjudged.

import { describe, expect, test } from 'bun:test'

import { WORDS } from './rulebook.ts'
import { cannotApply, constraintsOn, logicFor, unmetBy, type Constraint } from './constraints.ts'
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
