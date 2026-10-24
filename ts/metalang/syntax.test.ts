// Colouring a document written out as YAML.
//
// What these tests are about: the colours come from the vocabulary, not from a list of words kept
// here, and nothing a reader sees is lost or quietly passed. So two things are asked of every
// reading: that the parts put back together are the text that went in, and that a word the
// vocabulary does not account for is marked rather than drawn as if it were fine.

import { readFileSync } from 'node:fs'
import { join } from 'node:path'

import { describe, expect, test } from 'bun:test'
import * as yaml from 'js-yaml'

import { LANGUAGE_FILE, rulebookFor, WORDS } from './rulebook.ts'
import { htmlOf, namesDeclaredIn, partsOf, writtenHtml, type Part, type Role } from './syntax.ts'
import { declaredBodiesOf } from './words.ts'

const here = join(import.meta.dir)

/** The parts, as "role:text" pairs, with the gaps left out so a test says what it means. */
const said = (text: string, written = {}): string[] =>
  partsOf(text, WORDS, written)
    .filter((p) => p.is !== 'gap')
    .map((p) => `${p.is}:${p.text}`)

/** What one word in a reading is, or nothing when the reading does not hold that word. */
const roleOf = (text: string, word: string, written = {}): Role | undefined =>
  partsOf(text, WORDS, written).find((p) => p.text === word)?.is

const joined = (parts: readonly Part[]): string => parts.map((p) => p.text).join('')

describe('nothing is lost in the reading', () => {
  test('the parts put back together are the text that was read', () => {
    const text = readFileSync(LANGUAGE_FILE, 'utf8')
    expect(joined(partsOf(text, WORDS))).toBe(text)
  })

  test('the architecture language too, which is written in another language', () => {
    const text = readFileSync(join(here, 'arch.yaml'), 'utf8')
    expect(joined(partsOf(text, WORDS))).toBe(text)
  })

  test('an empty text reads as no parts at all', () => {
    expect(partsOf('', WORDS)).toEqual([])
  })
})

describe('nothing in the documents we have is left unaccounted for', () => {
  // The control marks what the vocabulary does not account for, which is only worth anything if
  // what it marks is rare. Every document in the repository reads with nothing marked, so a mark
  // on a page is a real one.
  const documents = ['metalang.yaml', 'arch.yaml', 'names.yaml', 'parts.yaml', 'stage.yaml']

  for (const name of documents) {
    test(`${name} reads with every word accounted for`, () => {
      const file = join(here, name)
      const text = readFileSync(file, 'utf8')
      const doc = yaml.load(text)
      const { words } = rulebookFor(doc, file)
      const parts = partsOf(text, words ?? WORDS, { declared: declaredBodiesOf(doc) })
      expect(parts.filter((p) => p.is === 'unknown').map((p) => p.text)).toEqual([])
    })
  }

  test('a document is read against its own vocabulary, not against the language file alone', () => {
    const file = join(here, 'arch.yaml')
    const text = readFileSync(file, 'utf8')
    const doc = yaml.load(text)
    // `form` is declared in this document, so without what the document declares its properties
    // are words nothing accounts for.
    const { words } = rulebookFor(doc, file)
    const blind = partsOf(text, words ?? WORDS).filter((p) => p.is === 'unknown')
    expect(blind.length).toBeGreaterThan(0)
  })
})

describe('what the vocabulary says a word is', () => {
  test("a file's own keys are the keywords the file carries", () => {
    expect(roleOf('scope: [metalang]\n', 'scope')).toBe('word')
    expect(roleOf('title: A file\n', 'title')).toBe('word')
  })

  test('a key the vocabulary does not carry is marked, not drawn as a keyword', () => {
    expect(roleOf('scoop: [metalang]\n', 'scoop')).toBe('unknown')
  })

  test('the word naming what a declaration is carries its kind, so it keeps its colour', () => {
    const parts = partsOf('declare:\n  - entity:\n      id: shade\n', WORDS)
    const kind = parts.find((p) => p.text === 'entity')
    expect(kind?.is).toBe('kind')
    expect(kind?.kind).toBe('entity')
  })

  test('a kind the language does not have is marked', () => {
    expect(roleOf('declare:\n  - entitty:\n      id: shade\n', 'entitty')).toBe('unknown')
  })

  test('a keyword is read against the kind it is written on', () => {
    const text = 'declare:\n  - entity:\n      id: shade\n      primitive: true\n'
    expect(roleOf(text, 'primitive')).toBe('word')
    // `values` belongs to an enum, not to an entity, so on an entity it is accounted for by
    // nothing.
    expect(roleOf('declare:\n  - entity:\n      values: []\n', 'values')).toBe('unknown')
  })

  test('a name the document gives is a name, not a keyword', () => {
    const text = [
      'declare:',
      '  - entity:',
      '      id: shade',
      '      properties:',
      '        depth:',
      '          is: number',
      '',
    ].join('\n')
    expect(roleOf(text, 'depth')).toBe('name')
    expect(roleOf(text, 'is')).toBe('word')
  })
})

describe('what a value is read against', () => {
  test('a word answering a keyword that holds a type points at that type', () => {
    expect(roleOf('declare:\n  - entity:\n      is: object\n', 'object')).toBe('ref')
  })

  test('a word pointing at nothing a reader can reach is marked', () => {
    expect(roleOf('declare:\n  - entity:\n      is: objekt\n', 'objekt')).toBe('unknown')
  })

  test('a name the text itself declares is reached from inside the text', () => {
    const text = [
      'declare:',
      '  - enum:',
      '      id: reserved',
      '      is: name',
      '  - entity:',
      '      id: holder',
      '      properties:',
      '        state:',
      '          is: reserved',
      '',
    ].join('\n')
    // `reserved` is declared on the line above rather than in the language, and a reader of this
    // text reaches it, so it draws as what it is.
    expect(said(text).filter((s) => s.endsWith(':reserved'))).toEqual([
      'name:reserved',
      'ref:reserved',
    ])
  })

  test('a true and a false are drawn as what they say', () => {
    expect(roleOf('declare:\n  - entity:\n      abstract: true\n', 'true')).toBe('true')
    expect(roleOf('declare:\n  - entity:\n      abstract: false\n', 'false')).toBe('false')
  })

  test('a word written where a true or a false belongs is marked', () => {
    expect(roleOf('declare:\n  - entity:\n      abstract: yes\n', 'yes')).toBe('unknown')
  })

  test('what a keyword says of itself is text, because its type is a string', () => {
    const text = 'declare:\n  - entity:\n      title: A colour.\n'
    expect(roleOf(text, 'A colour.')).toBe('text')
  })

  test('the name a declaration is known by is a name', () => {
    expect(roleOf('declare:\n  - entity:\n      id: shade\n', 'shade')).toBe('name')
  })

  test('a comment is a comment, written beside a value or on its own line', () => {
    expect(said('# what this file is\nscope: [x]\n')[0]).toBe('note:# what this file is')
    expect(said('scope: [x] # the scope\n')).toContain('note:# the scope')
  })

  test('prose written under a block marker is text, line after line', () => {
    const text = [
      'declare:',
      '  - entity:',
      '      description: >',
      '        Written again on a type deriving from the one that declared it.',
      '        It takes what it does not write from above.',
      '',
    ].join('\n')
    const text_parts = said(text).filter((s) => s.startsWith('text:'))
    expect(text_parts).toHaveLength(2)
    expect(text_parts[0]).toContain('Written again on a type')
  })
})

describe('a value of a choice', () => {
  // The whole of one small language, so the colouring is asked the same question the checker is:
  // a choice between an enum and the type it is based on, with a type holding a value of it.
  const painted = (held: string): string =>
    [
      'declare:',
      '  - enum:',
      '      id: colour',
      '      is: string',
      '      values:',
      '        - { id: red, value: red }',
      '        - { id: blue, value: blue }',
      '  - choice:',
      '      id: shade',
      '      options: [colour, string]',
      '  - entity:',
      '      id: wall',
      '      is: object',
      '      properties:',
      '        paint:',
      '          is: shade',
      `          value: ${held}`,
      '',
    ].join('\n')

  // A whole file, so the reading starts where a document starts and the file's own declarations
  // are what a word in it is read against.
  const inside = (text: string) => ({ declared: declaredBodiesOf(yaml.load(text)) })

  test('a value the enum lists is drawn as the member it is', () => {
    // `red` is written three times: the id of a value, that value itself, and the paint. The
    // paint is the last of them, and it is the one written against the choice.
    const text = painted('red')
    expect(said(text, inside(text)).filter((s) => s.endsWith(':red')).at(-1)).toBe('member:red')
  })

  test('a value the enum does not list is drawn as the type underneath', () => {
    const text = painted('mauve')
    expect(roleOf(text, 'mauve', inside(text))).toBe('text')
  })

  test('a key naming one of the options is still the word it names', () => {
    // `declaration` is a choice, and an item under `declare` names one of its options.
    expect(roleOf('declare:\n  - entity:\n', 'entity')).toBe('kind')
  })

  test('a choice that is one of itself is marked rather than read for ever', () => {
    const loop = [
      'declare:',
      '  - choice:',
      '      id: shade',
      '      options: [shade]',
      '  - entity:',
      '      id: wall',
      '      is: object',
      '      properties:',
      '        paint:',
      '          is: shade',
      '          value: red',
      '',
    ].join('\n')
    expect(roleOf(loop, 'red', inside(loop))).toBe('unknown')
  })
})

describe('a value held to what its type says its values may be', () => {
  // A type carrying a pattern says what its values may be, and what a value IS decides how it is
  // drawn: a word the type refuses is not a value of it, so it is marked rather than drawn as
  // though it had been read. `link` is a string carrying the pattern a uri carries.
  const written = (declared: readonly string[], property: string, held: string): string =>
    [
      'declare:',
      ...declared,
      '  - entity:',
      '      id: form',
      '      is: object',
      '      properties:',
      `        ${property}:`,
      ...held.split('\n'),
      '',
    ].join('\n')

  const link = [
    '  - entity:',
    '      id: link',
    '      is: string',
    "      constraints:",
    "        - pattern: { value: '[A-Za-z][A-Za-z0-9+.-]*:[^\\s]+' }",
  ]

  const inside = (text: string) => ({ declared: declaredBodiesOf(yaml.load(text)) })

  test('a value its type takes is drawn as the type says', () => {
    const text = written(link, 'mark', '          is: link\n          value: https://a.example/b')
    expect(roleOf(text, 'https://a.example/b', inside(text))).toBe('text')
  })

  test('a value its type refuses is marked, not drawn as a value of it', () => {
    const text = written(link, 'mark', '          is: link\n          value: notalinkatall')
    expect(roleOf(text, 'notalinkatall', inside(text))).toBe('unknown')
  })

  test('the option a value is refused by is passed over for the one that takes it', () => {
    // `link` and `name` are both strings carrying a pattern, and neither derives from the other,
    // so each is asked in turn. A word that is not a link is drawn as the name it is.
    const options = [...link, '  - choice:', '      id: mark', '      options: [link, name]']
    const text = written(options, 'mark', '          is: mark\n          value: wall')
    expect(roleOf(text, 'wall', inside(text))).toBe('name')
  })

  test('a number is read as the number it is, whichever primitive it derives down to', () => {
    // `byte` is its own primitive, written as a number and held between 0 and 255.
    const text = written([], 'tries', '          is: byte\n          value: 3')
    expect(roleOf(text, '3', inside(text))).toBe('number')
  })

  test('a number its type puts out of range is marked rather than drawn as a number', () => {
    const text = written([], 'tries', '          is: byte\n          value: 300')
    expect(roleOf(text, '300', inside(text))).toBe('unknown')
  })
})

describe('a value written in quotes', () => {
  test('quoting changes how a value is written, not what it is', () => {
    const text = 'declare:\n  - entity:\n      abstract: "true"\n'
    expect(roleOf(text, 'true')).toBe('true')
  })

  test('a quoted value the vocabulary cannot account for is still marked', () => {
    const text = 'declare:\n  - entity:\n      abstract: "yes"\n'
    expect(roleOf(text, 'yes')).toBe('unknown')
  })

  test('the quotes themselves are drawn as the marks they are', () => {
    expect(said('declare:\n  - entity:\n      title: "A colour."\n')).toContain('mark:"')
  })
})

describe('written on one line, between brackets', () => {
  test('a mapping written on one line is read as the mapping it is', () => {
    const text = 'declare:\n  - entity: { id: shade, is: object, primitive: true }\n'
    expect(said(text)).toEqual([
      'word:declare',
      'mark::',
      'mark:-',
      'kind:entity',
      'mark::',
      'mark:{',
      'word:id',
      'mark::',
      'name:shade',
      'mark:,',
      'word:is',
      'mark::',
      'ref:object',
      'mark:,',
      'word:primitive',
      'mark::',
      'true:true',
      'mark:}',
    ])
  })

  test('a list written on one line is read against what the list holds', () => {
    const text = 'declare:\n  - entity:\n      is: [object, name]\n'
    expect(said(text)).toContain('ref:object')
    expect(said(text)).toContain('ref:name')
  })

  test('a type written out rather than named says what it holds', () => {
    const text = [
      'declare:',
      '  - entity:',
      '      id: holder',
      '      properties:',
      '        marks:',
      '          set: { of: name }',
      '',
    ].join('\n')
    expect(roleOf(text, 'set')).toBe('word')
    expect(roleOf(text, 'of')).toBe('word')
    expect(roleOf(text, 'name')).toBe('ref')
  })
})

describe('what the vocabulary cannot account for is said, never passed over', () => {
  test('a type written out that is not a collection accounts for nothing beneath it', () => {
    const text = [
      'declare:',
      '  - entity:',
      '      id: holder',
      '      properties:',
      '        example:',
      '          unwritten: { of: string }',
      '',
    ].join('\n')
    // Only a collection may be written out, so a word that is not one is not read as a type and
    // what is written inside it is not read as that type's properties either.
    expect(roleOf(text, 'unwritten')).toBe('unknown')
    expect(roleOf(text, 'of')).toBe('unknown')
  })

  test('a value of a property with no declaration above it is marked, not guessed at', () => {
    const text = ['example:', '  value: 3', ''].join('\n')
    // A piece showing one property has no declaration above it saying what that property holds,
    // so what it always holds cannot be read against anything.
    expect(roleOf(text, '3', { inside: 'property', named: true })).toBe('unknown')
  })

  test('something written between brackets that is not a key is marked where it is written', () => {
    expect(said('declare:\n  - entity: { 12 }\n')).toContain('unknown:12')
  })

  test('a collection written out that does not say what it holds accounts for nothing in it', () => {
    // The declaration is written wrongly: `set` says a collection without saying of what. What
    // is written against it cannot be read, so it is marked rather than taken as anything.
    const declared = new Map([['holder', { id: 'holder', properties: { marks: { set: 'name' } } }]])
    expect(roleOf('marks: [one]\n', 'one', { inside: 'holder', declared })).toBe('unknown')
  })
})

describe('an example that is part of a file rather than a whole one', () => {
  test('a piece written inside a property is read as what it is written inside', () => {
    const text = ['accepts:', '  set: { of: reserved }', '  optional: true', ''].join('\n')
    const written = { inside: 'property', named: true }
    expect(roleOf(text, 'accepts', written)).toBe('name')
    expect(roleOf(text, 'optional', written)).toBe('word')
    expect(roleOf(text, 'true', written)).toBe('true')
  })

  test('a piece read as the whole file it is not says so, rather than drawing it as fine', () => {
    const text = ['accepts:', '  optional: true', ''].join('\n')
    expect(roleOf(text, 'accepts')).toBe('unknown')
  })

  test('a piece showing a value says what that value is of', () => {
    const text = ['- id: one', '  value:', '    title: One line.', ''].join('\n')
    // Written inside a whole file, the enum above says what the values it lists are. A piece has
    // no enum above it, so it says that itself, and without it the parts of the value are words
    // nothing accounts for.
    expect(roleOf(text, 'title', { inside: 'value' })).toBe('unknown')
    expect(roleOf(text, 'title', { inside: 'value', of: 'property' })).toBe('word')
    expect(roleOf(text, 'One line.', { inside: 'value', of: 'property' })).toBe('text')
  })

  test('a list of declarations is read as declarations', () => {
    const text = ['- enum:', '    id: reserved', '    is: name', ''].join('\n')
    expect(roleOf(text, 'enum', { inside: 'declaration' })).toBe('kind')
    expect(roleOf(text, 'reserved', { inside: 'declaration' })).toBe('name')
  })
})

describe('the names a text declares', () => {
  test('every id it writes, wherever it writes it', () => {
    const text = 'declare:\n  - entity: { id: shade }\n  - enum:\n      id: depth\n'
    expect([...namesDeclaredIn(text)]).toEqual(['shade', 'depth'])
  })

  test('a text declaring none declares none', () => {
    expect(namesDeclaredIn('title: nothing here\n').size).toBe(0)
  })
})

describe('the parts as HTML', () => {
  test('a gap carries no element, and everything else says what it is', () => {
    const html = htmlOf(partsOf('title: A file\n', WORDS))
    expect(html).toBe('<span class="y-word">title</span><span class="y-mark">:</span> ' +
      '<span class="y-text">A file</span>\n')
  })

  test('a kind carries the colour its place in the vocabulary gives it', () => {
    const html = htmlOf(partsOf('declare:\n  - entity:\n', WORDS), WORDS.kinds)
    expect(html).toContain('<span class="y-kind" style="--kind:oklch(54% 0.17 ')
  })

  test('what is written is escaped, so a page draws the text and not the markup', () => {
    const html = htmlOf(partsOf('title: a < b & c\n', WORDS))
    expect(html).toContain('a &lt; b &amp; c')
    expect(html).not.toContain('a < b')
  })

  test('a whole example comes back inside a block of its own', () => {
    const html = writtenHtml('title: A file\n', WORDS)
    expect(html.startsWith('<pre class="y">')).toBe(true)
    expect(html.endsWith('</pre>')).toBe(true)
  })
})

describe('a word standing where a value goes', () => {
  // A property says which reserved states it takes, and the word is read as one only there.
  const written = (accepts: string | null, value: string): string =>
    [
      'declare:',
      '  - entity:',
      '      id: form',
      '      is: object',
      '      properties:',
      '        example:',
      '          is: string',
      ...(accepts === null ? [] : [`          accepts: ${accepts}`]),
      `          value: ${value}`,
      '',
    ].join('\n')

  // The word appears twice where the property takes it: once where the property says what it
  // takes, and once where the value goes. They come back in the order they are written.
  const rolesOf = (text: string, word: string): string[] =>
    partsOf(text, WORDS, { declared: declaredBodiesOf(yaml.load(text)) })
      .filter((p) => p.text === word && p.is !== 'gap')
      .map((p) => p.is)

  test('is drawn as the state it names, not as the text it reads as', () => {
    expect(rolesOf(written('[pending]', 'pending'), 'pending')).toEqual(['member', 'reserved'])
  })

  test('the same word is the text it is where the property takes none', () => {
    expect(rolesOf(written(null, 'pending'), 'pending')).toEqual(['text'])
  })

  test('one deliberately left empty is drawn the same way', () => {
    expect(rolesOf(written('[none]', 'none'), 'none')).toEqual(['member', 'reserved'])
  })

  test('a word the property does not take is read against the type as it always was', () => {
    expect(rolesOf(written('[pending]', 'other'), 'other')).toEqual(['text'])
  })
})
