// The vocabulary a document is read against.
//
// What these tests are about: a document says which language it is written in and which scopes it
// brings in, and both are files. The rulebook is what those files add up to, with the language
// winning over everything it brings in. A file that is not there, or says it lives somewhere
// else, leaves no vocabulary at all rather than a partly guessed one.

import { mkdtemp, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import { afterAll, beforeAll, describe, expect, test } from 'bun:test'

import { fileForScope, LANGUAGE_FILE, nameOfScope, rulebookFor, WORDS } from './rulebook.ts'

let dir: string

/** A file in the temporary directory, written as YAML. */
const wrote = async (name: string, text: string): Promise<string> => {
  const file = join(dir, name)
  await writeFile(file, text)
  return file
}

beforeAll(async () => {
  dir = await mkdtemp(join(tmpdir(), 'metalang-rulebook-'))
  // A language of its own, declaring one type, which brings in nothing.
  await wrote(
    'colour.yaml',
    [
      'scope: [colour]',
      'title: The colour language',
      'declare:',
      '  - entity: { id: shade, is: object, primitive: true }',
      '',
    ].join('\n'),
  )
})

afterAll(async () => {
  await rm(dir, { recursive: true, force: true })
})

describe('the file a scope is written in', () => {
  test('its segments are the path, with yaml on the end', () => {
    expect(fileForScope(['metalang'], [dir, '.'])).toBeNull()
    // Joined rather than written out: the path the code hands back is separated the way the
    // platform separates paths, and on Windows that is not a forward slash.
    expect(fileForScope(['metalang'], ['ts/metalang'])).toBe(join('ts/metalang', 'metalang.yaml'))
  })

  test('several segments are several folders', () => {
    expect(fileForScope(['guard', 'process'], ['ts/metalang'])).toBeNull()
  })

  test('a scope of no segments is no scope', () => {
    expect(fileForScope([], ['ts/metalang'])).toBeNull()
  })

  test('a scope is written the way a person says it', () => {
    expect(nameOfScope(['guard', 'process'])).toBe('guard.process')
  })
})

describe('the rulebook a document is read against', () => {
  test('a document naming no language is read against the language file', () => {
    const { words, faults } = rulebookFor({ scope: ['x'] }, 'ts/metalang/x.yaml')
    expect(faults).toEqual([])
    expect(words?.scope).toEqual(['metalang'])
    expect(words?.brought).toEqual([['metalang']])
  })

  test('a document naming a language is read against that language', () => {
    const { words, faults } = rulebookFor(
      { scope: ['parts'], language: 'arch' },
      'ts/metalang/parts.yaml',
    )
    expect(faults).toEqual([])
    expect(words?.scope).toEqual(['arch'])
    // The architecture language brings in MetaLang, so both are in the rulebook, and the kinds
    // and the top-level keys come from MetaLang because the architecture language declares none.
    expect(words?.brought).toEqual([['arch'], ['metalang']])
    expect(words?.declared.forms?.kind).toBe('enum')
    expect(words?.declared.forms?.scope).toEqual(['arch'])
    expect(words?.declared.string?.scope).toEqual(['metalang'])
    expect(words?.kinds).toEqual(WORDS.kinds)
    expect(words?.topLevel).toEqual(WORDS.topLevel)
  })

  test('a scope the document itself brings in is in the rulebook', () => {
    const { words, faults } = rulebookFor(
      { scope: ['x'], import: [['colour']] },
      join(dir, 'x.yaml'),
    )
    expect(faults).toEqual([])
    expect(words?.declared.shade?.scope).toEqual(['colour'])
    expect(words?.brought).toEqual([['metalang'], ['colour']])
  })

  test('the language wins over a scope that declares the same name', async () => {
    await wrote(
      'other.yaml',
      ['scope: [other]', 'title: Another', 'declare:', '  - entity: { id: string }', ''].join('\n'),
    )
    const { words } = rulebookFor({ scope: ['x'], import: [['other']] }, join(dir, 'x.yaml'))
    // `string` is MetaLang's, and MetaLang is the language this document is written in, so the
    // one brought in does not take the name away.
    expect(words?.declared.string?.scope).toEqual(['metalang'])
  })

  test('one name two scopes brought in both declare is reported, not settled', async () => {
    await wrote(
      'warm.yaml',
      ['scope: [warm]', 'title: Warm', 'declare:', '  - entity: { id: shade }', ''].join('\n'),
    )
    const { words, faults } = rulebookFor(
      { scope: ['x'], import: [['colour'], ['warm']] },
      join(dir, 'x.yaml'),
    )
    // Both declare `shade`, and the rule that exists only covers a document's own name winning
    // over one it brings in. So the clash is said rather than decided here.
    expect(faults.map((f) => f.what).join(' ')).toContain(
      '"shade" is declared in "colour" as well, and which of two scopes brought in wins is not' +
        ' decided',
    )
    expect(words).not.toBeNull()
  })

  test('a language that is not there leaves no vocabulary, and says where it looked', () => {
    const { words, faults } = rulebookFor(
      { scope: ['x'], language: 'nowhere' },
      join(dir, 'x.yaml'),
    )
    expect(words).toBeNull()
    expect(faults[0]?.where).toBe('x.yaml.language')
    expect(faults[0]?.what).toContain('"nowhere" names no file this checker could find')
    expect(faults[0]?.what).toContain('nowhere.yaml')
  })

  test('a file that says it lives somewhere else is not read as that scope', async () => {
    await wrote('elsewhere.yaml', ['scope: [somewhere, else]', 'title: Elsewhere', ''].join('\n'))
    const { words, faults } = rulebookFor(
      { scope: ['x'], language: 'elsewhere' },
      join(dir, 'x.yaml'),
    )
    expect(words).toBeNull()
    expect(faults[0]?.what).toContain('it says its scope is "somewhere.else"')
  })

  test('a file that cannot be parsed leaves no vocabulary', async () => {
    await wrote('broken.yaml', 'scope: [broken]\ntitle: "unclosed\n')
    const { words, faults } = rulebookFor(
      { scope: ['x'], language: 'broken' },
      join(dir, 'x.yaml'),
    )
    expect(words).toBeNull()
    expect(faults[0]?.what).toContain('which cannot be read')
  })

  test('a language written in another language is not read through it yet', async () => {
    await wrote('nested.yaml', ['scope: [nested]', 'language: arch', 'title: Nested', ''].join('\n'))
    const { words, faults } = rulebookFor({ scope: ['x'], language: 'nested' }, join(dir, 'x.yaml'))
    expect(words).toBeNull()
    expect(faults[0]?.what).toContain('a language read through another language cannot be read yet')
  })

  test('a scope that reaches round to itself is read once', async () => {
    await wrote(
      'ring.yaml',
      ['scope: [ring]', 'title: A ring', 'import:', '  - [round]', ''].join('\n'),
    )
    await wrote(
      'round.yaml',
      ['scope: [round]', 'title: Round again', 'import:', '  - [ring]', ''].join('\n'),
    )
    const { words, faults } = rulebookFor({ scope: ['x'], import: [['ring']] }, join(dir, 'x.yaml'))
    expect(faults).toEqual([])
    expect(words?.brought).toEqual([['metalang'], ['round'], ['ring']])
  })

  test('a scope brought in twice is read once', () => {
    const { words, faults } = rulebookFor(
      { scope: ['x'], import: [['colour'], ['colour']] },
      join(dir, 'x.yaml'),
    )
    expect(faults).toEqual([])
    expect(words?.brought).toEqual([['metalang'], ['colour']])
  })

  test('a scope written as anything but a list of names brings nothing in', () => {
    const { words, faults } = rulebookFor({ scope: ['x'], import: ['colour'] }, join(dir, 'x.yaml'))
    // The checker reports the shape where it is written. Nothing is guessed from it here.
    expect(faults).toEqual([])
    expect(words?.brought).toEqual([['metalang']])
  })

  test('a language named as anything but text is read as naming none', () => {
    const { words } = rulebookFor({ scope: ['x'], language: 7 }, join(dir, 'x.yaml'))
    expect(words?.scope).toEqual(['metalang'])
  })

  test('the language file is read against itself', () => {
    const { words, faults } = rulebookFor({ scope: ['metalang'] }, LANGUAGE_FILE)
    expect(faults).toEqual([])
    expect(words?.scope).toEqual(['metalang'])
  })
})

/**
 * Reading every scope a document reaches is this function's job. It hands back what it read so
 * nothing else has to walk the imports a second time to reach the same documents.
 */
describe('the documents it read', () => {
  const scopesOf = (read: readonly unknown[]): unknown[] =>
    read.map((one) => (one !== null && typeof one === 'object' ? (one as { scope?: unknown }).scope : null))

  test('hands back the language it was read against', () => {
    const { documents } = rulebookFor({ scope: ['x'], language: 'colour' }, join(dir, 'x.yaml'))
    expect(scopesOf(documents)).toContainEqual(['colour'])
  })

  test('hands back a scope brought in, with what that scope itself brings in before it', () => {
    const { documents } = rulebookFor({ scope: ['x'], import: [['colour']] }, join(dir, 'x.yaml'))
    const scopes = scopesOf(documents)
    expect(scopes).toContainEqual(['colour'])
    expect(scopes).toContainEqual(['metalang'])
    // Deepest first: the language a document is read against is read before the scope that brings
    // it in, so a reader taking the first of a repeated name gets the nearer declaration.
    expect(scopes.indexOf(scopes.find((s) => nameOfScope(s as string[]) === 'metalang'))).toBeLessThan(
      scopes.indexOf(scopes.find((s) => nameOfScope(s as string[]) === 'colour')),
    )
  })

  test('hands back what it read even where a scope it needs is not there', () => {
    const { words, documents } = rulebookFor({ scope: ['x'], import: [['missing']] }, join(dir, 'x.yaml'))
    expect(words).not.toBeNull()
    expect(scopesOf(documents)).toContainEqual(['metalang'])
  })
})
