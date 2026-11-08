// A regular expression written in a file.
//
// The one thing here that is not obvious is that a match is always of the whole value. A pattern
// says what a value IS, so a pattern matching a part of it would let every id through that
// happened to contain a letter.

import { describe, expect, test } from 'bun:test'

import { readExpression } from './expression.ts'

/** The expression itself, for a test that has already said it reads. */
const read = (source: string): { source: string; matches: (v: string) => boolean } => {
  const { expression } = readExpression(source)
  if (expression === null) throw new Error(`"${source}" did not read`)
  return expression
}

describe('reading a regular expression', () => {
  test('text that is a regular expression reads, and keeps what it was written as', () => {
    const { expression, fault } = readExpression('[a-z]+')
    expect(fault).toBeNull()
    expect(expression?.source).toBe('[a-z]+')
  })

  test('text that is not a regular expression says so rather than throwing', () => {
    const { expression, fault } = readExpression('[a-z')
    expect(expression).toBeNull()
    expect(fault).toContain('is not a regular expression')
  })

  test('anything not written as text says so', () => {
    expect(readExpression(7).fault).toBe('must be written as text')
    expect(readExpression(undefined).fault).toBe('must be written as text')
    expect(readExpression({ a: 1 }).fault).toBe('must be written as text')
  })

  test('an expression matching nothing but the empty value is still an expression', () => {
    expect(read('').matches('')).toBe(true)
    expect(read('').matches('a')).toBe(false)
  })
})

describe('matching a value', () => {
  test('the whole value has to match, not a part of it', () => {
    const name = read('[A-Za-z_][A-Za-z0-9_]*')
    expect(name.matches('scope')).toBe(true)
    expect(name.matches('a_1')).toBe(true)
    expect(name.matches('2nd')).toBe(false)
    expect(name.matches('a b')).toBe(false)
    expect(name.matches('has a space in it')).toBe(false)
  })

  test('an alternation is matched as a whole, not one branch at a time', () => {
    const yes = read('yes|no')
    expect(yes.matches('yes')).toBe(true)
    expect(yes.matches('yes please')).toBe(false)
  })

  test('a value ending in a line break is not the value the pattern matches', () => {
    expect(read('[a-z]+').matches('abc\n')).toBe(false)
  })

  test('an empty value matches only what allows one', () => {
    expect(read('[a-z]*').matches('')).toBe(true)
    expect(read('[a-z]+').matches('')).toBe(false)
  })
})
