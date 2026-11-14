// The page.
//
// The two rules the page exists to enforce are what these tests are about. A component is
// chosen by what the node IS, and a component must account for every key on the node it draws.
// So the tests that matter most are the ones proving that something the page has no drawing
// for appears ON the page as an error rather than being quietly left out.

import { readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'

import * as yaml from 'js-yaml'
import { JSDOM } from 'jsdom'
import { renderToStaticMarkup } from 'react-dom/server'

import { describe, expect, test } from 'bun:test'

import { kindStyle, Page, text } from './app.tsx'
import { checkedAgainst } from './check.ts'
import { LANGUAGE_FILE, rulebookFor, WORDS } from './rulebook.ts'
import { marksOf, pathTo, type Entry } from './highlight.ts'
import {
  declaredBodiesOf,
  hueOfKind,
  membersOf,
  propertyCarrying,
  ROLES,
  type CheckResult,
  type Fault,
  type Vocabulary,
} from './words.ts'

const draw = (doc: unknown, words: Vocabulary = WORDS): string =>
  renderToStaticMarkup(<Page words={words} doc={doc} />)

/** A file of this folder, by name, so the folder is written out in one place. */
const beside = (name: string): string => join(dirname(LANGUAGE_FILE), name)

/** A document this repository holds, read as the YAML it is written as. */
const held = (name: string): unknown => yaml.load(readFileSync(beside(name), 'utf8'))

/**
 * A document drawn the way the renderer draws it: read against the rulebook beside the file it is
 * written as, marked with what the checker found in it, and handed to the page. The one place
 * these tests build a page out of a document, so every test below reads the page a reader gets.
 */
const pageFor = (
  doc: unknown,
  where: string,
  only?: string,
): { readonly html: string; readonly found: CheckResult } => {
  const book = rulebookFor(doc, where)
  const found = checkedAgainst(doc, where, book)
  return {
    html: renderToStaticMarkup(
      <Page
        words={book.words ?? WORDS}
        doc={doc}
        found={found}
        {...(only === undefined ? {} : { only })}
      />,
    ),
    found,
  }
}

/** A real document written in the architecture language, read from the file. */
const parts = held('parts.yaml') as Record<string, unknown>

/** A document written in the architecture language, declaring what it is given. */
const archDocument = (...declare: readonly unknown[]): Record<string, unknown> => ({
  scope: ['guard'],
  language: 'arch',
  import: [['metalang']],
  title: 'The parts of the guard',
  declare,
})

const file = (extra: Record<string, unknown>): Record<string, unknown> => ({
  scope: ['metalang'],
  title: 'A file',
  ...extra,
})

/** The markup a value its type pins down draws as: the equals sign it follows, then the value. */
const val = (classes: string, shown: string): string =>
  `<span class="${classes}"><i class="v-eq" aria-hidden="true">=</i>${shown}</span>`

/**
 * The markup prose draws as. A run of characters its type constrains in no way and no enum lists
 * is matched against nothing, so it is read rather than recognised: no ground, no border and no
 * equals sign standing in the middle of a sentence.
 */
const prose = (classes: string, shown: string): string => `<span class="${classes}">${shown}</span>`

describe('writing a value as text', () => {
  test('text is written as it stands', () => {
    expect(text('MetaLang')).toBe('MetaLang')
  })

  test('a number or a boolean is written as what it is', () => {
    expect(text(7)).toBe('7')
    expect(text(true)).toBe('true')
  })

  test('a mapping is written as what it holds, never as [object Object]', () => {
    expect(text({ a: 1 })).toBe('{"a":1}')
    expect(text([1, 2])).toBe('[1,2]')
  })

  test('nothing at all says so', () => {
    expect(text(undefined)).toBe('nothing')
  })

  test('something not in a document at all still writes as words', () => {
    expect(text(() => 1)).toBe('a function')
  })
})

describe('the declarations a document writes', () => {
  test('collects them from the one list, external or not, each under the id it declares', () => {
    const bodies = declaredBodiesOf({
      declare: [{ entity: { id: 'A' } }, { external: { cast: { id: 'B' } } }],
    })
    expect([...bodies.keys()].sort()).toEqual(['A', 'B'])
    expect(bodies.get('A')).toEqual({ id: 'A' })
  })

  test('an external naming no kind, or carrying no mapping, declares nothing', () => {
    expect(declaredBodiesOf({ declare: [{ external: {} }, { external: 'cast' }] }).size).toBe(0)
  })

  test('skips anything that is not one kind with an id written as text', () => {
    const bodies = declaredBodiesOf(
      {
        declare: [
          'entity',
          {},
          { entity: {}, choice: {} },
          { entity: 'x' },
          { entity: { id: 7 } },
          { entity: { id: 'Kept' } },
        ],
      },
    )
    expect([...bodies.keys()]).toEqual(['Kept'])
  })

  test('one id declared twice keeps the first, which is the one the checker reads', () => {
    const bodies = declaredBodiesOf({
      declare: [{ entity: { id: 'A', is: 'one' } }, { entity: { id: 'A', is: 'two' } }],
    })
    expect(bodies.get('A')).toEqual({ id: 'A', is: 'one' })
  })

  test('a document that is not a mapping declares nothing', () => {
    expect(declaredBodiesOf(null).size).toBe(0)
    expect(declaredBodiesOf([1]).size).toBe(0)
  })

  test('a declare that is not a list declares nothing', () => {
    expect(declaredBodiesOf({ declare: 'entity' }).size).toBe(0)
  })
})

describe('the colour a kind draws in', () => {
  test('comes from the kind position, so every kind gets one', () => {
    for (const kind of WORDS.kinds) expect(kindStyle(kind, WORDS.kinds)).toBeDefined()
  })

  test('two kinds do not share a hue', () => {
    const hues = WORDS.kinds.map((k) => JSON.stringify(kindStyle(k, WORDS.kinds)))
    expect(new Set(hues).size).toBe(WORDS.kinds.length)
  })

  test('a word that is not a kind draws in no colour of its own', () => {
    expect(kindStyle('nothing', WORDS.kinds)).toBeUndefined()
  })
})

describe('the document as a whole', () => {
  test('something that is not a mapping draws one error rather than nothing', () => {
    const html = draw([1, 2])
    expect(html).toContain('is not a mapping, so there is nothing to draw.')
    expect(html).toContain('the file')
  })

  test('a file declaring nothing says so', () => {
    expect(draw(file({ declare: [] }))).toContain('This document declares nothing.')
  })

  test('the scope draws as its segments, labelled', () => {
    const html = draw({ scope: ['metalang', 'core'], title: 'T' })
    expect(html).toContain('scope')
    expect(html).toContain('<b>metalang</b>')
    expect(html).toContain('<b>core</b>')
  })

  test('a scope that is not a list of names draws an error and says it is not given', () => {
    const html = draw({ scope: ['ok', 7], title: 'T' })
    expect(html).toContain('a scope is a list of segments, each written as text.')
    expect(html).toContain('not given')
  })

  test('a missing scope says it is not given without calling the document wrong', () => {
    const html = draw({ title: 'T' })
    expect(html).toContain('not given')
    expect(html).not.toContain('a scope is a list of segments')
  })

  test('no title says so rather than drawing an empty heading', () => {
    expect(draw({ scope: ['x'] })).toContain('no title')
  })

  test('a description draws when there is one', () => {
    expect(draw(file({ description: 'What this is' }))).toContain('What this is')
  })

  test('the language draws when the document names one', () => {
    const html = draw(file({ language: 'other' }))
    expect(html).toContain('language')
    expect(html).toContain('<b>other</b>')
  })

  test('a top-level key the page cannot draw appears as an incomplete feature', () => {
    const html = draw(file({ notes: 'hello' }))
    expect(html).toContain('incomplete')
    expect(html).toContain('is an incomplete language feature')
    expect(html).toContain('the file.notes')
  })

  test('a section written as something other than a list draws an error in place', () => {
    expect(draw(file({ declare: 'entity' }))).toContain('must be written as a list.')
  })

  test('a section is headed by the keyword that names it', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A' } }] }))
    expect(html).toContain('<h2 class="d-section-name">declare</h2>')
  })

  test('an external declaration draws in the same list, saying it is external', () => {
    // An entity is written externally here so the head itself is reached whatever the kinds the
    // language writes there carry.
    const words: Vocabulary = { ...WORDS, places: { ...WORDS.places, external: ['entity'] } }
    const html = draw(
      file({ declare: [{ entity: { id: 'A' } }, { external: { entity: { id: 'B' } } }] }),
      words,
    )
    expect(html).toContain('<h2 class="d-section-name">declare</h2>')
    expect(html).toContain('<div class="d-decl-head"><span class="d-extern">external</span>')
  })

  test('a declaration written plainly carries no external marking', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A' } }] }))
    expect(html).not.toContain('<div class="d-decl-head"><span class="d-extern">')
  })

  test('external naming no kind draws an error at the wrapper', () => {
    const html = draw(file({ declare: [{ external: {} }] }))
    expect(html).toContain('external names one kind, and the kind is written inside it.')
    expect(html).toContain('declare[0].external')
  })

  test('a kind inside external that the page cannot draw is named at its own position', () => {
    const html = draw(file({ declare: [{ external: { thing: {} } }] }))
    expect(html).toContain('is not a kind')
    expect(html).toContain('declare[0].external')
  })

  test('the legend names the kinds under each section and the top-level keys', () => {
    const html = draw(file({ declare: [] }))
    for (const kind of WORDS.kinds) expect(html).toContain(`>${kind}<`)
    expect(html).toContain(WORDS.topLevel.join(', '))
  })
})

describe('a declaration', () => {
  const one = (node: unknown): string => draw(file({ declare: [node] }))

  test('something that is not a mapping draws an error naming its position', () => {
    const html = one('entity')
    expect(html).toContain('a declaration is a mapping naming one kind.')
    expect(html).toContain('declare[0]')
  })

  test('naming no kind draws an error counting them', () => {
    expect(one({})).toContain('a declaration names one kind, and this names 0.')
  })

  test('naming two kinds draws an error listing them', () => {
    expect(one({ entity: {}, choice: {} })).toContain('entity, choice')
  })

  test('a word that is not a kind draws an error naming the kinds', () => {
    const html = one({ thing: {} })
    expect(html).toContain('is not a kind')
    expect(html).toContain(WORDS.kinds.join(', '))
  })

  test('a kind whose keywords are undecided draws as an incomplete feature, not as wrong', () => {
    // Every kind the language has carries its keywords today, so this is drawn against a
    // vocabulary where one is approved and what it carries is not decided.
    const words: Vocabulary = { ...WORDS, onKind: { ...WORDS.onKind, cast: null } }
    const html = draw(file({ declare: [{ external: { cast: { from: 'string' } } }] }), words)
    expect(html).toContain('incomplete')
    expect(html).toContain('The keywords it carries have not been decided')
    expect(html).not.toContain('cannot draw')
  })

  test('a body that is not a mapping draws an error', () => {
    expect(one({ entity: 'x' })).toContain('must be a mapping.')
  })

  test('draws its kind, its id, and how many properties it has', () => {
    const html = one({ entity: { id: 'Thing', properties: { a: { is: 'x' }, b: { is: 'y' } } } })
    expect(html).toContain('>entity<')
    expect(html).toContain('Thing')
    expect(html).toContain('2 properties')
  })

  test('one property is counted without an s', () => {
    expect(one({ entity: { id: 'T', properties: { a: { is: 'x' } } } })).toContain('1 property')
  })

  test('no id draws an error saying every declaration carries one', () => {
    const html = one({ entity: {} })
    expect(html).toContain('no id')
    expect(html).toContain('every declaration carries an id, written as text.')
  })

  test('a kind that carries no properties keyword is not counted or drawn for them', () => {
    const html = one({ choice: { id: 'C' } })
    expect(html).toContain('>choice<')
    expect(html).not.toContain('no properties')
  })

  test('a keyword the page has no drawing for appears as an incomplete feature', () => {
    const words: Vocabulary = { ...WORDS, onKind: { ...WORDS.onKind, entity: ['id', 'is', 'properties', 'notes'] } }
    const html = draw(file({ declare: [{ entity: { id: 'A', notes: 'x' } }] }), words)
    expect(html).toContain('is an incomplete language feature')
    expect(html).toContain('notes')
  })
})

describe('the two closed kinds', () => {
  test('a choice draws the types it is one of, under the keyword that names them', () => {
    const html = draw(
      file({ declare: [{ entity: { id: 'A' } }, { choice: { id: 'C', options: ['A'] } }] }),
    )
    expect(html).toContain('<i>options</i>')
    expect(html).toContain('r-declared')
  })

  test('an option that does not name a type is counted in an error at its own position', () => {
    const html = draw(file({ declare: [{ choice: { id: 'C', options: ['A', '', 3] } }] }))
    expect(html).toContain('2 of 3 do not name a type.')
    expect(html).toContain('declare[0].choice.options')
  })

  test('an enum draws the one type it is based on and every value it lists', () => {
    const html = draw(
      file({
        declare: [{ enum: { id: 'E', is: 'number', values: [{ id: 'NOT_FOUND', value: 404 }] } }],
      }),
    )
    expect(html).toContain('<i>is</i>')
    expect(html).toContain('<i>values</i>')
    expect(html).toContain('NOT_FOUND')
    expect(html).toContain(val('d-val', '404'))
  })

  test('a value that is not a mapping draws an error naming its position', () => {
    const html = draw(file({ declare: [{ enum: { id: 'E', is: 'number', values: ['red'] } }] }))
    expect(html).toContain('a value carries an id and what that id stands for.')
    expect(html).toContain('declare[0].enum.values[0]')
  })

  test('a value with no id says so and draws an error', () => {
    const html = draw(
      file({ declare: [{ enum: { id: 'E', is: 'number', values: [{ value: 1 }] } }] }),
    )
    expect(html).toContain('no id')
    expect(html).toContain('every value carries an id, written as text.')
  })

  test('a value with no value says so and draws an error', () => {
    const html = draw(file({ declare: [{ enum: { id: 'E', is: 'number', values: [{ id: 'A' }] } }] }))
    expect(html).toContain('no value')
    expect(html).toContain('has no value, so nothing says what this id stands for.')
  })

  test('a value written as nothing at all is drawn as what it is', () => {
    const html = draw(
      file({ declare: [{ enum: { id: 'E', is: 'number', values: [{ id: 'A', value: null }] } }] }),
    )
    expect(html).toContain(val('d-val', 'null'))
  })

  test('a keyword on a value the page cannot draw appears as an incomplete feature', () => {
    const html = draw(
      file({
        declare: [{ enum: { id: 'E', is: 'number', values: [{ id: 'A', value: 1, note: 'x' }] } }],
      }),
    )
    expect(html).toContain('is an incomplete language feature')
    expect(html).toContain('note')
  })

  test('a single value written where a list belongs still draws', () => {
    const html = draw(
      file({ declare: [{ enum: { id: 'E', is: 'number', values: { id: 'A', value: 1 } } }] }),
    )
    expect(html).toContain(val('d-val', '1'))
  })

  test('neither word draws as incomplete, because the page has a drawing for both', () => {
    const html = draw(
      file({
        declare: [
          { enum: { id: 'E', is: 'number', values: [{ id: 'A', value: 1 }] } },
          { choice: { id: 'C', options: ['E'] } },
        ],
      }),
    )
    expect(html).not.toContain('is an incomplete language feature')
  })
})

describe('a type written out rather than named', () => {
  test('a collection draws the kind it is and what it holds', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'document', properties: { scope: { array: { of: 'name' } } } } },
        ],
      }),
    )
    expect(html).toContain('array')
    expect(html).toContain('<i class="d-pof">of</i>')
    expect(html).toContain('name')
  })

  test('a collection of a collection draws both, because the drawing recurses', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'a', properties: { rows: { list: { of: { array: { of: 'name' } } } } } } },
        ],
      }),
    )
    expect(html).toContain('list')
    expect(html).toContain('array')
    expect((html.match(/d-pof/g) ?? []).length).toBe(2)
  })

  test('a collection holding something that is not a type name says so', () => {
    const html = draw(file({ declare: [{ entity: { id: 'a', properties: { x: { set: { of: 7 } } } } }] }))
    expect(html).toContain('not a type name')
  })

  test('a collection not written as its properties says so rather than drawing empty', () => {
    const html = draw(file({ declare: [{ entity: { id: 'a', properties: { x: { bag: 'name' } } } }] }))
    expect(html).toContain('not written as its properties')
  })

  test('a property naming its type under is still draws as a reference', () => {
    const html = draw(file({ declare: [{ entity: { id: 'a', properties: { x: { is: 'name' } } } }] }))
    expect(html).not.toContain('d-pof')
    expect(html).toContain('name')
  })
})

describe('what a declaration derives from', () => {
  test('a type this document declares draws as declared, with no word on it', () => {
    const html = draw(
      file({ declare: [{ entity: { id: 'Base' } }, { entity: { id: 'Kid', is: 'Base' } }] }),
    )
    // Nothing most references are gets a word; the filled mark carries it, so the chip ends
    // right after the name.
    expect(html).toContain('<b aria-hidden="true">\u25C6</b>Base</span>')
  })

  test('a type this document does not declare draws as undeclared', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A', is: 'string' } }] }))
    expect(html).toContain('r-undeclared')
    expect(html).toContain('<i>undeclared</i>')
  })

  test('several types all draw', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A', is: ['x', 'y'] } }] }))
    expect(html).toContain('x')
    expect(html).toContain('y')
  })

  test('entries that do not name a type are counted in an error', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A', is: ['x', '', 3] } }] }))
    expect(html).toContain('2 of 3 do not name a type.')
  })

  test('nothing is drawn for is when the declaration leaves it off', () => {
    expect(draw(file({ declare: [{ entity: { id: 'A' } }] }))).not.toContain('<i>is</i>')
  })
})

describe('a property', () => {
  const prop = (p: unknown): string =>
    draw(file({ declare: [{ entity: { id: 'A', properties: { p } } }] }))

  test('its type draws as a reference', () => {
    expect(prop({ is: 'string' })).toContain('string')
  })

  test('something that is not a mapping draws as taken from elsewhere', () => {
    const html = prop('Other.q')
    expect(html).toContain('Other.q')
    expect(html).toContain('taken from there')
  })

  test('no type says so and draws an error', () => {
    const html = prop({})
    expect(html).toContain('no type')
    expect(html).toContain('has no is, so there is nothing to say what it holds.')
  })

  test('several types are counted and draw an error, because a property holds one', () => {
    const html = prop({ is: ['a', 'b'] })
    expect(html).toContain('2 types named')
    expect(html).toContain('a property holds one type, and this names 2.')
  })

  test('a type that is not a name draws an error', () => {
    const html = prop({ is: 7 })
    expect(html).toContain('not a type name')
    expect(html).toContain('must name a type.')
  })

  test('an empty type name draws an error', () => {
    expect(prop({ is: '' })).toContain('must name a type.')
  })

  test('a keyword the page has no drawing for appears as an incomplete feature', () => {
    const html = prop({ is: 'string', default: 1 })
    expect(html).toContain('is an incomplete language feature')
    expect(html).toContain('default')
  })

  // What a property is to a reader, beyond the value it holds. The page reads three of them, so
  // the word has to be claimed where a property is drawn or it would draw as a word the page has
  // no drawing for — which is what every other unclaimed keyword rightly does.
  test('what it is to a reader draws as the word the language gives it, not as an error', () => {
    const html = prop({ is: 'string', role: 'kind' })
    expect(html).toContain('<span class="d-said s-role"')
    expect(html).toContain('role kind')
    expect(html).not.toContain('is an incomplete language feature')
  })

  test('two properties of one type carrying one role draw an error naming both', () => {
    const html = draw(
      file({
        declare: [
          {
            entity: {
              id: 'A',
              properties: { one: { is: 'string', role: 'kind' }, two: { is: 'string', role: 'kind' } },
            },
          },
        ],
      }),
    )
    expect(html).toContain('A gives role kind to one, two, so nothing says which of them holds it.')
  })

  test('none written says so on the declaration, and draws no row for them', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A' } }] }))
    expect(html).toContain('<span class="d-count">no properties</span>')
    expect(html).not.toContain('d-prow')
  })

  test('an empty properties mapping says the same, and draws no row either', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A', properties: {} } }] }))
    expect(html).toContain('<span class="d-count">no properties</span>')
    expect(html).not.toContain('d-prow')
  })

  test('properties written as a list draws an error', () => {
    expect(draw(file({ declare: [{ entity: { id: 'A', properties: ['p'] } }] }))).toContain(
      'must be a mapping of name to property.',
    )
  })
})

describe('a property written again on the type below the one that declares it', () => {
  const two = (below: unknown): string =>
    draw(
      file({
        declare: [
          { entity: { id: 'collection', properties: { ordered: { is: 'boolean' } } } },
          { entity: { id: 'list', is: 'collection', properties: { ordered: below } } },
        ],
      }),
    )

  test('draws the value it always holds, and the type the declaration above gives it', () => {
    const html = two({ value: true })
    expect(html).toContain('boolean')
    expect(html).toContain('from collection')
    expect(html).toContain('>true</span>')
  })

  test('a boolean draws in the colour it reads as, green for true and red for false', () => {
    expect(two({ value: true })).toContain('d-val v-true')
    expect(two({ value: false })).toContain('d-val v-false')
  })

  test('a value that is not a boolean draws in no colour of its own', () => {
    const html = two({ is: 'string', value: 'x' })
    expect(html).toContain(val('d-val d-pval', 'x'))
  })

  test('is read as a value, not as the type that the word value also names', () => {
    const html = two({ value: false })
    expect(html).not.toContain('not written as its properties')
    expect(html).not.toContain('is an incomplete language feature')
    expect(html).toContain('>false</span>')
  })

  test('writing its own type as well draws that type rather than the one above', () => {
    const html = two({ is: 'string', value: 'x' })
    expect(html).toContain('string')
    expect(html).not.toContain('from collection')
  })

  test('nothing above declaring it draws the error that nothing says what it holds', () => {
    const html = draw(
      file({ declare: [{ entity: { id: 'list', is: 'collection', properties: { ordered: { value: true } } } }] }),
    )
    expect(html).toContain('has no is, so there is nothing to say what it holds.')
  })
})

describe('what a type says about itself', () => {
  test('abstract and primitive draw as chips on the declaration, when written true', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'object', abstract: true } },
          { entity: { id: 'string', primitive: true } },
        ],
      }),
    )
    expect(html).toContain('<span class="d-said s-abstract">abstract</span>')
    expect(html).toContain('<span class="d-said s-primitive">primitive</span>')
  })

  test('neither draws when it is left off, or written false', () => {
    const html = draw(
      file({ declare: [{ entity: { id: 'A' } }, { entity: { id: 'B', abstract: false } }] }),
    )
    expect(html).not.toContain('d-said')
  })

  test('neither draws as incomplete, because the page has a drawing for both', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A', abstract: true, primitive: true } }] }))
    expect(html).not.toContain('is an incomplete language feature')
  })
})

describe('the prose a thing carries', () => {
  test('a declaration draws its title and its description', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'A', title: 'A thing.', description: 'What the thing is for.' } },
        ],
      }),
    )
    expect(html).toContain('<div class="d-prose"><b>A thing.</b><p>What the thing is for.</p></div>')
  })

  test('a property draws its prose under the column that says what it holds', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'string', primitive: true } },
          { entity: { id: 'A', properties: { x: { is: 'string', title: 'One line.' } } } },
        ],
      }),
    )
    expect(html).toContain('<div class="d-prose d-prose-row"><b>One line.</b></div>')
  })

  test('one of an enum values draws its prose the same way', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'number', primitive: true } },
          {
            enum: {
              id: 'E',
              is: 'number',
              values: [{ id: 'A', value: 1, description: 'What this one stands for.' }],
            },
          },
        ],
      }),
    )
    expect(html).toContain(
      '<div class="d-prose d-prose-row"><p>What this one stands for.</p></div>',
    )
  })

  test('a property written again takes the prose of the one above when it writes none', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'boolean', primitive: true } },
          {
            entity: {
              id: 'collection',
              properties: { ordered: { is: 'boolean', title: 'Whether order counts.' } },
            },
          },
          { entity: { id: 'list', is: 'collection', properties: { ordered: { value: true } } } },
        ],
      }),
    )
    expect(html).toContain('<div class="d-prose d-prose-row"><b>Whether order counts.</b></div>')
  })

  test('a thing that says neither draws no prose at all', () => {
    expect(draw(file({ declare: [{ entity: { id: 'A' } }] }))).not.toContain('d-prose')
  })

  test('neither draws as incomplete, because the page has a drawing for both', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A', title: 'A.', description: 'B.' } }] }))
    expect(html).not.toContain('is an incomplete language feature')
  })
})

describe('whether a property may be left out', () => {
  const property = (held: Record<string, unknown>): string =>
    draw(
      file({
        declare: [
          { entity: { id: 'string', primitive: true } },
          { entity: { id: 'A', properties: { x: { is: 'string', ...held } } } },
        ],
      }),
    )

  test('one that may be left out draws a mark saying so', () => {
    expect(property({ optional: true })).toContain(
      '<span class="d-said s-optional" title="this property may be left out">optional</span>',
    )
  })

  test('one that must be written draws nothing, because that is most of them', () => {
    expect(property({})).not.toContain('s-optional')
    expect(property({ optional: false })).not.toContain('s-optional')
  })

  test('it does not draw as incomplete, because the page has a drawing for it', () => {
    expect(property({ optional: true })).not.toContain('is an incomplete language feature')
  })
})

describe('what a type says its values may be', () => {
  const narrowed = (constraints: unknown): unknown =>
    file({ declare: [{ entity: { id: 'code', is: 'string', constraints } }] })

  test('a declaration draws the constraints it carries', () => {
    const html = draw(narrowed([{ pattern: { value: '[a-z]+' } }]))
    expect(html).toContain('constraints')
    expect(html).toContain('pattern')
    expect(html).toContain('[a-z]+')
  })

  test('what a pattern holds draws as a value, not as the name of a type', () => {
    const html = draw(narrowed([{ pattern: { value: '[a-z]+' } }]))
    expect(html).toContain('[a-z]+')
    expect(html).not.toContain('[a-z]+<i>undeclared</i>')
  })

  test('each constraint draws on a line of its own', () => {
    const html = draw(narrowed([{ pattern: { value: 'a+' } }, { pattern: { value: 'b+' } }]))
    expect((html.match(/d-pof/g) ?? []).length).toBe(2)
    expect(html).toContain('a+')
    expect(html).toContain('b+')
  })

  test('a constraint naming no kind draws as an error rather than vanishing', () => {
    expect(draw(narrowed([{}]))).toContain('a constraint names one kind')
    expect(draw(narrowed(['pattern']))).toContain('a constraint names one kind')
  })

  test('constraints written as one rather than as a list still draw', () => {
    expect(draw(narrowed({ pattern: { value: 'a+' } }))).toContain('a+')
  })

  test('a pattern holding nothing draws what is there rather than nothing at all', () => {
    expect(draw(narrowed([{ pattern: {} }]))).toContain('pattern')
    expect(draw(narrowed([{ pattern: 'a+' }]))).toContain('not written as its properties')
  })
})

describe('a value drawn as what its type says it is', () => {
  // A document with a primitive of its own standing in for each of the two, so the drawing is
  // read from the declarations rather than from the page knowing these names.
  const written = (type: string, value: unknown): unknown =>
    file({
      declare: [
        { entity: { id: 'expression', primitive: true } },
        { entity: { id: 'string', primitive: true } },
        { entity: { id: 'held', is: type } },
        { entity: { id: 'A', properties: { x: { is: 'held', value } } } },
      ],
    })

  // Whether a value is read or recognised is asked of the language and of nothing else: a type that
  // constrains what its values may be, or an enum that lists them, is something a value is matched
  // against, and a plain run of characters is not.
  test('a value its type pins down follows an equals sign', () => {
    const pinned = file({
      declare: [
        { entity: { id: 'string', primitive: true } },
        { entity: { id: 'held', is: 'string', constraints: [{ pattern: { value: 'x+' } }] } },
        { entity: { id: 'A', properties: { x: { is: 'held', value: 'x' } } } },
      ],
    })
    expect(draw(pinned)).toContain('<i class="v-eq" aria-hidden="true">=</i>')
  })

  test('a run of characters nothing pins down draws as prose, with no equals sign', () => {
    const html = draw(written('string', 'x'))
    expect(html).toContain(prose('d-val v-text d-pval', 'x'))
    expect(html).not.toContain('<i class="v-eq" aria-hidden="true">=</i>')
  })

  test('a value an enum lists is recognised, so it keeps the equals sign', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'string', primitive: true } },
          { enum: { id: 'forms', is: 'string', values: [{ id: 'interface', value: 'interface' }] } },
          { entity: { id: 'A', properties: { x: { is: 'forms', value: 'interface' } } } },
        ],
      }),
    )
    // The enum's own listed value is read against the type the enum is based on, which is a plain
    // run of characters, so that one is prose. What the property holds is a member of the enum,
    // and a member is recognised.
    expect(html).toContain(val('d-val d-pval', 'interface'))
  })

  test('a regular expression draws between slashes, in a colour of its own', () => {
    const html = draw(written('expression', '[a-z]+'))
    expect(html).toContain('<span class="d-val v-expr d-pval">')
    expect(html).toContain('<i aria-hidden="true">/</i>[a-z]+<i aria-hidden="true">/</i>')
  })

  test('a regular expression follows the equals sign too, before its slashes', () => {
    expect(draw(written('expression', '[a-z]+'))).toContain(
      '<i class="v-eq" aria-hidden="true">=</i><i aria-hidden="true">/</i>',
    )
  })

  test('text that is not a regular expression draws as it stands', () => {
    const html = draw(written('string', '[a-z]+'))
    expect(html).toContain(prose('d-val v-text d-pval', '[a-z]+'))
    expect(html).not.toContain('v-expr')
  })

  test('a type deriving down to an expression draws as one, however far down it is', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'expression', primitive: true } },
          { entity: { id: 'narrow', is: 'expression' } },
          { entity: { id: 'held', is: 'narrow' } },
          { entity: { id: 'A', properties: { x: { is: 'held', value: 'a+' } } } },
        ],
      }),
    )
    expect(html).toContain('v-expr')
  })

  test('a value of no type at all draws as it stands', () => {
    const html = draw(file({ declare: [{ entity: { id: 'A', properties: { x: { value: 7 } } } }] }))
    expect(html).toContain(val('d-val d-pval', '7'))
  })

  test('what an enum lists draws as the one type the enum is based on says', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'expression', primitive: true } },
          { enum: { id: 'E', is: 'expression', values: [{ id: 'any', value: '.*' }] } },
        ],
      }),
    )
    expect(html).toContain('<span class="d-val v-expr">')
    expect(html).toContain('.*')
  })

  test('the expression in the language file itself draws between slashes', () => {
    const html = draw({
      declare: [
        { entity: { id: 'expression', is: 'object', primitive: true } },
        { entity: { id: 'pattern', is: 'constraint', properties: { value: { is: 'expression' } } } },
        { entity: { id: 'name', is: 'string', constraints: [{ pattern: { value: 'a+' } }] } },
      ],
    })
    expect(html).toContain('<span class="d-val v-expr">')
  })
})

describe('a keyword with no drawing of its own', () => {
  const cast = (body: Record<string, unknown>): string =>
    draw(file({ declare: [{ external: { cast: { id: 'c', ...body } } }] }))

  test('a cast draws the types it converts between and whether it happens on its own', () => {
    const html = cast({ from: 'uuid', to: 'string', implicit: true })
    expect(html).toContain('<span class="d-pname">from</span>')
    expect(html).toContain('<span class="d-pname">to</span>')
    expect(html).toContain('<span class="d-pname">implicit</span>')
    expect(html).toContain(val('d-val v-true', 'true'))
  })

  test('a keyword holding a type written out draws the collection and what it holds', () => {
    const html = cast({ from: 'uuid', to: { array: { of: 'byte' } } })
    expect(html).toContain('<i class="d-pof">of</i>')
    expect(html).toContain('byte')
  })

  test('a keyword holding something that is not a type name says so', () => {
    expect(cast({ from: 7 })).toContain('not a type name')
  })

  test('a keyword the language says nothing about draws as the incomplete feature it is', () => {
    const words: Vocabulary = { ...WORDS, onKind: { ...WORDS.onKind, entity: ['id', 'colour'] } }
    const html = draw(file({ declare: [{ entity: { id: 'A', colour: 'red' } }] }), words)
    expect(html).toContain('incomplete')
    expect(html).toContain('Nothing says what it holds')
  })

  test('a keyword the kind carries and the declaration leaves off draws nothing', () => {
    expect(cast({ from: 'uuid', to: 'string' })).not.toContain('implicit')
  })
})

describe('a document reaching the declarations of another scope', () => {
  const stage = (extra: Record<string, unknown>): unknown => ({
    scope: ['guard', 'process'],
    title: 'A stage of the process',
    ...extra,
    declare: [{ entity: { id: 'stage', properties: { name: { is: 'string' } } } }],
  })

  test('the scopes a document imports draw beside the scope it is', () => {
    const html = draw(stage({ import: [['metalang']] }))
    expect(html).toContain('<i>import</i>')
    expect(html).toContain('<b>metalang</b>')
  })

  test('a name an imported scope declares draws as reached, and names the scope', () => {
    const html = draw(stage({ import: [['metalang']] }))
    expect(html).toContain('class="d-ref r-imported"')
    expect(html).toContain('<i>metalang</i>')
    expect(html).not.toContain('>string<i>undeclared</i>')
  })

  test('a name no imported scope declares draws as undeclared', () => {
    const html = draw(stage({}))
    expect(html).toContain('>string<i>undeclared</i>')
    expect(html).not.toContain('>string<i>metalang</i>')
  })

  test('a name the document declares itself draws as its own, not as reached', () => {
    const html = draw({
      scope: ['guard', 'process'],
      import: [['metalang']],
      declare: [
        { entity: { id: 'string' } },
        { entity: { id: 'stage', properties: { name: { is: 'string' } } } },
      ],
    })
    expect(html).toContain('<b aria-hidden="true">\u25C6</b>string</span>')
    expect(html).not.toContain('>string<i>metalang</i>')
  })

  test('a scope written as something other than a list of segments still draws', () => {
    expect(draw(stage({ import: 'metalang' }))).toContain('<i>import</i>')
  })

  test('the language file imports nothing and draws no import row', () => {
    expect(draw(file({ declare: [{ entity: { id: 'A' } }] }))).not.toContain('<i>import</i>')
  })

  test('a name of the language the document names draws as reached, naming that language', () => {
    // The page is drawn with the vocabulary the document is read against, which here holds the
    // architecture language's own declarations as well as MetaLang's. `element` is reached
    // because the document is written in that language, with no import saying so.
    const { words } = rulebookFor(parts, join(dirname(LANGUAGE_FILE), 'parts.yaml'))
    const html = draw(parts, words ?? undefined)
    expect(html).toContain('<b aria-hidden="true">\u25C8</b>element<i>arch</i>')
    expect(html).toContain('<b aria-hidden="true">\u25C8</b>string<i>metalang</i>')
    expect(html).not.toContain('>element<i>undeclared</i>')
  })

  test('a name the document neither declares nor reaches still draws as undeclared', () => {
    const { words } = rulebookFor(parts, join(dirname(LANGUAGE_FILE), 'parts.yaml'))
    const html = draw(
      { ...parts, declare: [{ entity: { id: 'p', properties: { f: { is: 'nothing' } } } }] },
      words ?? undefined,
    )
    expect(html).toContain('>nothing<i>undeclared</i>')
  })
})

// A value of a type with properties draws as its named parts rather than as one run of text.
describe('a value of a type with properties', () => {
  /** A document declaring the root, one type with parts, and an enum listing one member. */
  const listing = (properties: unknown, value: unknown): string =>
    draw(
      file({
        declare: [
          { entity: { id: 'object', primitive: true } },
          { entity: { id: 'string', is: 'object', primitive: true } },
          { entity: { id: 'form', is: 'object', properties } },
          { enum: { id: 'forms', is: 'form', values: [{ id: 'interface', value }] } },
        ],
      }),
    )

  const parts = { mark: { is: 'string' }, example: { is: 'string' } }

  test('each part draws its name and its own value', () => {
    const html = listing(parts, { mark: '<>', example: 'IGuard' })
    expect(html).toContain(`<span class="d-part" data-part="mark"><i>mark</i>${prose('d-val v-text', '&lt;&gt;')}</span>`)
    expect(html).toContain(
      `<span class="d-part" data-part="example"><i>example</i>${prose('d-val v-text', 'IGuard')}</span>`,
    )
  })

  test('the value holding the parts carries none of a chip of its own', () => {
    expect(listing(parts, { mark: '<>' })).toContain('<span class="d-val v-parts">')
  })

  test('the equals sign is drawn once per part and not again around them', () => {
    const html = listing(parts, { mark: '<>', example: 'IGuard' })
    expect(html).not.toContain('<span class="d-val v-parts"><i class="v-eq"')
  })

  test('a part whose own type has parts nests', () => {
    const html = draw(
      file({
        declare: [
          { entity: { id: 'object', primitive: true } },
          { entity: { id: 'string', is: 'object', primitive: true } },
          { entity: { id: 'named', is: 'object', properties: { csharp: { is: 'string' } } } },
          { entity: { id: 'form', is: 'object', properties: { equivalents: { is: 'named' } } } },
          {
            enum: {
              id: 'forms',
              is: 'form',
              values: [{ id: 'interface', value: { equivalents: { csharp: 'interface' } } }],
            },
          },
        ],
      }),
    )
    expect(html).toContain(
      `<span class="d-part" data-part="csharp"><i>csharp</i>${prose('d-val v-text', 'interface')}</span>`,
    )
  })

  test('a part nothing says a type for still draws as what is written', () => {
    const html = listing(parts, { colour: 'blue' })
    expect(html).toContain(`<span class="d-part" data-part="colour"><i>colour</i>${val('d-val', 'blue')}</span>`)
  })

  test('a value of a type with no properties behind it is still drawn as text', () => {
    expect(listing(parts, 'interface')).toContain(val('d-val', 'interface'))
  })
})

describe('a value with no drawing of its own', () => {
  // A structure nothing on the page draws as a table or as named parts. It used to print as
  // JSON, which is a second language on a page about this one.
  const odd = (value: unknown): unknown =>
    file({ declare: [{ entity: { id: 'A', properties: { x: { value } } } }] })

  test('it is written out in the language, through the control that colours every example', () => {
    const html = draw(odd({ holds: ['one', 'two'] }))
    expect(html).toContain('<pre class="y">')
    expect(html).toContain('<span class="y-mark">-</span>')
    expect(html).not.toContain('&quot;one&quot;')
  })

  test('the value sits inside it, so the block keeps its own ground', () => {
    expect(draw(odd({ holds: [] }))).toContain('<span class="d-val v-written d-pval">')
  })

  test('a word nothing accounts for is marked where it is written', () => {
    // Nothing says what this value is, so none of its keys is a keyword of anything.
    expect(draw(odd({ holds: [] }))).toContain('<span class="y-unknown">holds</span>')
  })
})

describe('several of one shape', () => {
  // A set written out, holding a type with named parts: the shape the architecture language's
  // equivalents have.
  const table = (value: unknown, properties: Record<string, unknown> = {}): string =>
    draw(
      file({
        declare: [
          { entity: { id: 'object', primitive: true } },
          { entity: { id: 'string', is: 'object', primitive: true } },
          { entity: { id: 'collection', is: 'object', abstract: true } },
          { entity: { id: 'set', is: 'collection' } },
          {
            entity: {
              id: 'equivalent',
              is: 'object',
              properties: { language: { is: 'string' }, name: { is: 'string' } },
            },
          },
          {
            entity: {
              id: 'form',
              is: 'object',
              properties: { equivalents: { set: { of: 'equivalent' } }, ...properties },
            },
          },
          { enum: { id: 'forms', is: 'form', values: [{ id: 'interface', value }] } },
        ],
      }),
    )

  const cells = [
    { language: 'csharp', name: 'interface' },
    { language: 'rust', name: 'trait' },
  ]

  test('a column carries each part name once, in the order the shape declares them', () => {
    const html = table({ equivalents: cells })
    expect(html).toContain('<th data-col="language">language</th><th data-col="name">name</th>')
    expect(html.match(/<th data-col="language">language<\/th>/g)).toHaveLength(1)
  })

  test('a row carries each member', () => {
    const html = table({ equivalents: cells })
    expect(html).toContain(
      `<td data-col="language">${prose('d-val v-text', 'csharp')}</td>` +
        `<td data-col="name">${prose('d-val v-text', 'interface')}</td>`,
    )
    expect(html).toContain(
      `<td data-col="language">${prose('d-val v-text', 'rust')}</td>` +
        `<td data-col="name">${prose('d-val v-text', 'trait')}</td>`,
    )
  })

  test('the table carries no equals sign of its own', () => {
    expect(table({ equivalents: cells })).not.toContain('<span class="d-val v-table"><i class="v-eq"')
  })

  test('a part a member leaves out is drawn as absent rather than as empty', () => {
    expect(table({ equivalents: [{ language: 'python' }] })).toContain('class="t-none"')
  })

  test('an empty list is not a table', () => {
    expect(table({ equivalents: [] })).not.toContain('d-table')
  })

  test('a list of text is not a table', () => {
    const html = table({ equivalents: ['csharp', 'rust'] })
    expect(html).not.toContain('d-table')
  })

  test('one of the shape on its own is parts rather than a table', () => {
    const html = table({ held: { language: 'csharp', name: 'interface' } }, { held: { is: 'equivalent' } })
    expect(html).not.toContain('d-table')
    expect(html).toContain('<span class="d-part" data-part="language"><i>language</i>')
  })
})

describe('a value a property takes in place of a value', () => {
  const written = (value: unknown, accepts?: unknown): string =>
    draw(
      file({
        declare: [
          { entity: { id: 'object', primitive: true } },
          { entity: { id: 'string', is: 'object', primitive: true } },
          {
            entity: {
              id: 'form',
              is: 'object',
              properties: {
                example:
                  accepts === undefined ? { is: 'string' } : { is: 'string', accepts },
              },
            },
          },
          { enum: { id: 'forms', is: 'form', values: [{ id: 'one', value: { example: value } }] } },
        ],
      }),
    )

  test('is drawn as the state it names rather than as the text it reads as', () => {
    expect(written('pending', ['pending'])).toContain('d-val v-reserved v-pending')
  })

  test('one deliberately left empty is drawn quietly', () => {
    expect(written('none', ['none'])).toContain('v-reserved v-none')
  })

  test('the same word is the text it is where the property takes none', () => {
    const html = written('pending')
    expect(html).not.toContain('v-reserved')
    expect(html).toContain('pending')
  })

  test('the property says what it takes, so the page accounts for the word', () => {
    const html = written('pending', ['pending', 'none'])
    expect(html).toContain('accepts pending, none')
    expect(html).not.toContain('incomplete')
  })
})

/** A document carrying one of everything a mark can land on, read by the two describes below. */
const doc = {
  scope: ['metalang'],
  title: 'A file',
  declare: [
    { entity: { id: 'object', primitive: true } },
    { entity: { id: 'string', is: 'object', primitive: true } },
    { choice: { id: 'mark', options: ['string'] } },
    { entity: { id: 'collection', is: 'object', abstract: true } },
    { entity: { id: 'set', is: 'collection' } },
    {
      entity: {
        id: 'equivalent',
        is: 'object',
        properties: { language: { is: 'string' }, name: { is: 'string' } },
      },
    },
    {
      entity: {
        id: 'form',
        is: 'object',
        properties: {
          mark: { is: 'mark' },
          equivalents: { set: { of: 'equivalent' } },
          always: { is: 'string', value: { a: 'one' } },
        },
        constraints: [{ pattern: { value: 'x' } }],
      },
    },
    {
      enum: {
        id: 'forms',
        is: 'form',
        values: [
          {
            id: 'interface',
            value: {
              mark: '\u25c7',
              equivalents: [
                { language: 'csharp', name: 'interface' },
                { language: 'rust', name: 'trait' },
              ],
            },
          },
        ],
      },
    },
  ],
}

describe('where a highlight points', () => {
  const marked = (...entries: Entry[]): string =>
    renderToStaticMarkup(<Page words={WORDS} doc={doc} marks={marksOf(doc, entries)} />)

  const note = (path: string): string => marked({ path, note: 'Look here.' })

  test('a document with no highlight carries no mark', () => {
    expect(draw(doc)).not.toContain('d-marked')
  })

  test('a declaration is marked where it is drawn', () => {
    expect(note('declare[mark]')).toContain('d-marked')
  })

  test('the note is drawn with the node', () => {
    expect(note('declare[mark]')).toContain('Look here.')
  })

  test('an entry with no note marks without drawing one', () => {
    const html = marked({ path: 'declare[mark]', note: '' })
    expect(html).toContain('d-marked')
    expect(html).not.toContain('d-note')
  })

  test('one property of a type', () => {
    expect(note('declare[form].properties.mark')).toContain('d-marked')
  })

  test('one constraint of a type', () => {
    expect(note('declare[form].constraints[0]')).toContain('d-marked')
  })

  test('the options of a choice', () => {
    expect(note('declare[mark].options')).toContain('d-marked')
  })

  test('one of an enum\'s values', () => {
    expect(note('declare[forms].values[interface]')).toContain('d-marked')
  })

  test('a part inside a value', () => {
    expect(note('declare[forms].values[interface].value.mark')).toContain('d-marked')
  })

  test('a keyword on a declaration', () => {
    expect(note('declare[forms].is')).toContain('d-marked')
  })

  test('the value an id stands for', () => {
    expect(note('declare[forms].values[interface].value')).toContain('d-marked')
  })

  test('the value a property always holds', () => {
    expect(note('declare[form].properties.always.value')).toContain('d-marked')
  })

  test('one cell of a table', () => {
    const html = note('declare[forms].values[interface].value.equivalents[0].name')
    expect(html).toContain('d-marked')
    expect(html).toContain('Look here.')
  })

  test('one member of a table is marked on the row, and says what it says under it', () => {
    const html = note('declare[forms].values[interface].value.equivalents[0]')
    expect(html).toContain('r-marked')
    expect(html).toContain('Look here.')
  })

  test('a node marked where two components draw it carries one mark', () => {
    const html = note('declare[forms].values[interface].value.mark')
    expect(html.match(/d-marked/g)).toHaveLength(1)
  })

  test('a path naming nothing is said on the page rather than left out', () => {
    const html = note('declare[gone].options')
    expect(html).toContain('not drawn on this page')
    expect(html).toContain('declare[gone].options')
    expect(html).not.toContain('d-marked')
  })

  test('a mark no part of the page drew is said rather than dropped', () => {
    // The value is written out as text, so what is inside it is drawn by no component that
    // asks whether it was marked.
    const html = note('declare[form].properties.always.value.a')
    expect(html).toContain('not drawn on this page')
    expect(html).toContain('names a node this page draws no mark on')
    expect(html).toContain(pathTo(doc, 'declare[form].properties.always.value.a') ?? 'nowhere')
  })

  test('nothing is said of the page when every mark is drawn', () => {
    expect(note('declare[mark]')).not.toContain('not drawn on this page')
  })
})

describe('what the checker found, said on the document', () => {
  const found = (problems: Fault[], blockers: Fault[] = [], pending: Fault[] = []): string =>
    renderToStaticMarkup(<Page words={WORDS} doc={doc} found={{ problems, blockers, pending }} />)

  const wrongMark: Fault = {
    where: 'declare[forms].values[interface].value.mark',
    what: 'is not written as a string',
  }

  test('a document nothing was found in carries no band', () => {
    expect(draw(doc)).not.toContain('d-found')
  })

  test('the band says how many were found, in the words the report counts in', () => {
    const html = found([wrongMark, { where: 'declare[mark].options', what: 'names one type' }])
    expect(html).toContain('2 problems')
  })

  test('the band says one of each kind apart', () => {
    const html = found([wrongMark], [{ where: 'the file', what: 'could not be read' }])
    expect(html).toContain('1 problem, 1 blocker')
  })

  test('the band carries where it is and what the report says about it', () => {
    const html = found([wrongMark])
    expect(html).toContain(wrongMark.where)
    expect(html).toContain(wrongMark.what)
  })

  test('a problem is drawn on the node it is about, in its own colour', () => {
    expect(found([wrongMark])).toContain('d-marked m-problem')
  })

  test('a blocker is drawn in a colour of its own', () => {
    expect(found([], [wrongMark])).toContain('d-marked m-blocker')
  })

  test('a problem on a node a highlight points at draws one mark carrying both', () => {
    const html = renderToStaticMarkup(
      <Page
        words={WORDS}
        doc={doc}
        marks={marksOf(doc, [{ path: wrongMark.where, note: 'Look here.' }])}
        found={{ problems: [wrongMark], blockers: [], pending: [] }}
      />,
    )
    expect(html.match(/d-marked/g)).toHaveLength(1)
    expect(html).toContain('m-problem')
    expect(html).toContain('Look here.')
    expect(html).toContain(wrongMark.what)
  })

  test('a finding naming no node in the document is said in the band with nowhere to point', () => {
    const html = found([{ where: 'a.yaml', what: 'is not a mapping' }])
    expect(html).toContain('f-nowhere')
    expect(html).toContain('is not a mapping')
  })

  test('a value not yet given is said in its own words and drawn in its own colour', () => {
    const notGiven: Fault = {
      where: 'declare[forms].values[interface].value.mark',
      what: 'is required and not yet given',
    }
    const html = found([], [], [notGiven])
    expect(html).toContain('1 pending value')
    expect(html).toContain('f-what f-pending')
    expect(html).toContain('d-marked m-pending')
    // Nothing is wrong with the document, so the band is not the colour of a problem.
    expect(html).not.toContain('d-found has-problem')
  })

  test('what is wrong and what is not finished are counted apart in the band', () => {
    const html = found([wrongMark], [], [{ where: 'declare[mark].options', what: 'not yet given' }])
    expect(html).toContain('1 problem, 1 pending value')
    expect(html).toContain('d-found has-problem')
  })

  test('what a highlight asks is in the band as well, because it needs reading too', () => {
    const html = renderToStaticMarkup(
      <Page
        words={WORDS}
        doc={doc}
        marks={marksOf(doc, [{ path: 'declare[mark]', note: 'Shall we?' }])}
      />,
    )
    expect(html).toContain('1 highlight')
    expect(html).toContain('f-what f-highlight')
    expect(html).toContain('Shall we?')
  })

  test('a highlight and a finding are counted apart in the one band', () => {
    const html = renderToStaticMarkup(
      <Page
        words={WORDS}
        doc={doc}
        marks={marksOf(doc, [{ path: 'declare[mark]', note: 'Shall we?' }])}
        found={{ problems: [wrongMark], blockers: [], pending: [] }}
      />,
    )
    expect(html).toContain('1 problem, 1 highlight')
  })

  test('a document with nothing called out draws no band', () => {
    expect(draw(doc)).not.toContain('d-found')
  })

  test('the place is a link to the mark drawn there, and the band reads as the report', () => {
    const html = found([wrongMark])
    const at = pathTo(doc, wrongMark.where) ?? 'nowhere'
    expect(html).toContain(`href="#${encodeURIComponent(at)}"`)
    expect(html).toContain(`id="${at}"`)
    // The band says where the report says it is, which is the path the report printed.
    expect(html).toContain(`>${wrongMark.where}</a>`)
  })
})

// ── a declaration carrying one value of its own ──────────────────────────────
//
// The language declares `value` as holding the root type, because a keyword's `is` names a type and
// cannot name another keyword. Drawn against the root, every part inside a value loses its type and
// any part that is a structure falls to being written out as text — a page of YAML for a document
// the checker verified in full. These tests hold the page to the type the declaration names, which
// is the rule the checker reads it by, so the two cannot drift apart again.
describe('a declaration carrying one value of its own', () => {
  const where = beside('parts.yaml')

  /** A document in the architecture language, read against the live language beside it. */
  const written = (value: Record<string, unknown>): Record<string, unknown> =>
    archDocument({ constant: { id: 'IGuard', is: 'element', value } })

  const shown = (doc: Record<string, unknown>): string => pageFor(doc, where).html

  const guard = (): Record<string, unknown> =>
    written({
      form: 'interface',
      does: 'The three behaviours every guard answers a tool call through.',
      members: [
        { name: 'Name', kind: 'property', does: 'The name this guard reports itself by.' },
        { name: 'PrecheckAsync', kind: 'method', does: 'Judges a tool call before it runs.' },
      ],
    })

  test('the value is the body of the card, not a row named after the keyword', () => {
    // `value` is the word the language writes it under. A reader of the document never wrote it
    // and should not have to read it: what the declaration carries IS the declaration.
    const html = shown(guard())
    expect(html).toContain('<div class="d-own">')
    expect(html).not.toContain('<span class="d-pname">value</span>')
  })

  // What the thing IS heads the card, because that is what a reader came for. The keyword the
  // declaration happens to be written under is the language's word and not the reader's: every
  // part of the guard is a `constant`, and nobody reading the document ever wrote that.
  test('the card is headed by what its value says it is, not by the keyword', () => {
    const html = shown(guard())
    expect(html).toContain('<span class="d-kind">interface</span>')
    expect(html).not.toContain('<span class="d-kind">constant</span>')
  })

  test('the colour comes from where that word sits in its own vocabulary', () => {
    const bodies = declaredBodiesOf(held('arch.yaml'))
    const forms = membersOf((id) => bodies.get(id), 'forms') ?? []
    // `interface` is the first of the six forms and `constant` the fourth of the kinds, so the
    // hue says which vocabulary the head was taken from and not merely that a word changed.
    expect(hueOfKind('interface', forms)).not.toBe(hueOfKind('constant', WORDS.kinds))
    expect(shown(guard())).toContain(`--kind:oklch(54% 0.17 ${hueOfKind('interface', forms)})`)
  })

  test('the line the value says is its prose reads under the name, not as a labelled row', () => {
    expect(shown(guard())).toContain(
      '<div class="d-prose"><b>The three behaviours every guard answers a tool call through.</b></div>',
    )
  })

  test('neither is drawn again as a row of the value, because each is drawn once', () => {
    const drawn = shown(guard())
    expect(drawn).not.toContain('<i>form</i>')
    expect(drawn).not.toContain('<i>does</i>')
    expect(drawn).toContain('<i>members</i>')
  })

  test('a value naming one member of an enum draws with the mark that enum gives it', () => {
    expect(shown(guard())).toContain(
      '<i class="v-eq" aria-hidden="true">=</i><b class="v-mark">get/set</b>property',
    )
  })

  // `property` is called `Property`, which is the same word in another case and says nothing the
  // id has not already said. Reading it twice is what this stops.
  test('a name that is the id in another case is not drawn beside it', () => {
    expect(shown(guard())).not.toContain('<b class="v-name">Property</b>')
  })

  test('a value that is nothing but the head and the line draws no value row at all', () => {
    const only = written({ form: 'function', does: 'Turns what was found into lines.', members: 'none' })
    expect(shown(only)).toContain('<span class="d-val v-reserved v-none">none</span>')
    const bare = written({ form: 'function', does: 'Turns what was found into lines.' })
    expect(shown(bare)).not.toContain('<div class="d-own">')
  })

  test('several of one shape inside the value draw as a table, not as written-out text', () => {
    const html = shown(guard())
    expect(html).toContain('<table class="d-table"')
    expect(html).not.toContain('v-written')
  })

  test('the table names each column once, from the type the parts are of', () => {
    const html = shown(guard())
    for (const column of ['name', 'kind', 'does']) {
      expect(html).toContain(`<th data-col="${column}">${column}</th>`)
    }
  })

  test('nothing inside the value is marked as a word the vocabulary cannot account for', () => {
    expect(shown(guard())).not.toContain('y-unknown')
  })

  test('what the checker found inside the value is drawn where the thing sits', () => {
    const doc = guard()
    const members = (doc.declare as { constant: { value: { members: Record<string, unknown>[] } } }[])
    const member = members[0]?.constant.value.members[1]
    if (member === undefined) throw new Error('the document has no member to break')
    member.kind = 'delegate'
    const html = shown(doc)
    expect(html).toContain('id="declare[0].constant.value.members[1].kind"')
    expect(html).not.toContain('not drawn on this page')
  })

  // The head and the line are still the nodes they were, so what the checker says about either
  // is drawn where it now sits. A node drawn in a new place and left out of the marks is how a
  // finding goes missing, and the page says at the end which marks nothing drew.
  test('what the checker found on either of the two lifted parts is drawn where it now sits', () => {
    const doc = guard()
    const value = (doc.declare as { constant: { value: Record<string, unknown> } }[])[0]?.constant.value
    if (value === undefined) throw new Error('the document has no value to break')
    value.form = 'struct'
    const html = shown(doc)
    expect(html).toContain('id="declare[0].constant.value.form"')
    expect(html).not.toContain('not drawn on this page')
  })

  test('a value whose type nothing names is still written out, which is the last resort', () => {
    // The checker reports the missing `is`; the page has no type to draw against and says what is
    // written rather than nothing at all. This is the only path the written-out drawing is for.
    const doc = written({ form: 'interface', does: 'Something.' })
    delete (doc.declare as { constant: Record<string, unknown> }[])[0]?.constant.is
    expect(shown(doc)).toContain('v-written')
  })
})

// What a value naming one word of a vocabulary reads as. The id is what the document writes and
// the name is what the vocabulary calls it, so a reader of a naming dictionary wants both, and
// which property holds the name is the vocabulary's answer rather than this page's guess.
describe('a value naming one member of a closed set', () => {
  const page = (name: string): string => pageFor(held(name), beside(name)).html

  test('draws with the name that set gives it where the name says what the id does not', () => {
    const html = page('names.yaml')
    expect(html).toContain('csharp<b class="v-name">C#</b>')
  })

  // Every form is named after itself, so the name beside the id would be the same word twice.
  // The mark the set gives it is drawn instead, which says what the word cannot.
  test('a name that is the id in another case gives way to the mark that set gives it', () => {
    const html = page('names.yaml')
    expect(html).not.toContain('<b class="v-name">Interface</b>')
    expect(html).toContain('<b class="v-mark">◇</b>interface')
  })

  test('a set whose type says which property is the name is the only one drawn that way', () => {
    // MetaLang's own enums are of a primitive, so no type of theirs says which property is the
    // name and no value in the whole file draws one. The page guesses nothing where nothing said.
    expect(page('metalang.yaml')).not.toContain('v-name')
  })
})

// Every document this repository holds, drawn. A document the checker passes and the page cannot
// account for is the two disagreeing about the same file, which is the defect this guards against.
describe('every document in this repository draws whole', () => {
  const documents = ['metalang.yaml', 'arch.yaml', 'names.yaml', 'parts.yaml', 'stage.yaml']

  for (const name of documents) {
    test(`${name} draws with nothing unaccounted for and nothing left undrawn`, () => {
      const { html, found } = pageFor(held(name), beside(name))
      expect(found.problems).toEqual([])
      expect(found.blockers).toEqual([])
      expect(html).not.toContain('y-unknown')
      expect(html).not.toContain('not drawn on this page')
      expect(html).not.toContain('class="d-err')
    })
  }
})

// What a page makes of a value that names another declaration, and of many of a shape written out
// rather than named. Both are new ways to write a value, and a page that cannot draw one falls
// back to the text of a file, which is the thing this language exists to stop a reader having to
// read.
describe('a value naming another declaration', () => {
  const where = beside('parts.yaml')

  const doc = (relates: unknown): Record<string, unknown> =>
    archDocument(
      {
        constant: {
          id: 'IGuard',
          is: 'element',
          value: {
            form: 'interface',
            does: 'The three behaviours every guard answers a tool call through.',
            members: 'none',
            sits: { package: 'none', assembly: 'A.Abstractions', namespace: 'A.Abstractions' },
            relates: { implements: 'none', answers: 'none', holds: 'none' },
          },
        },
      },
      {
        constant: {
          id: 'FileGuard',
          is: 'element',
          value: {
            form: 'class',
            does: 'The guard that blocks a write to a sealed file.',
            members: 'none',
            sits: { package: 'A.Engine', assembly: 'A.Engine', namespace: 'A.Engine' },
            relates,
          },
        },
      },
    )

  const standing = { implements: ['IGuard'], answers: 'none', holds: 'none' }

  const shown = (node: Record<string, unknown>, only?: string): string =>
    pageFor(node, where, only).html

  test('the name is drawn as the reference it is, not as the text of a file', () => {
    const html = shown(doc(standing))
    expect(html).toContain('<span class="d-ref r-declared" title="declared in this document">')
    expect(html).toContain('IGuard')
  })

  test('many of a shape written out draws its members, rather than falling back to written text', () => {
    const html = shown(doc({ implements: ['IGuard', 'IGuard'], answers: 'none', holds: 'none' }))
    expect(html).toContain('v-many')
    expect(html).not.toContain('v-written')
  })

  test('a name nothing declares draws as undeclared, so a broken relation reads as one', () => {
    expect(shown(doc({ implements: ['Ghost'], answers: 'none', holds: 'none' }))).toContain(
      'r-undeclared',
    )
  })

  // A reading drawn from one declaration of a document is still read against the whole of it, or a
  // name reaching another declaration in the same file would draw as undeclared.
  test('one declaration can be drawn while the document keeps every declaration it makes', () => {
    const html = shown(doc(standing), 'FileGuard')
    expect(html).toContain('FileGuard')
    expect(html).toContain('r-declared')
    expect(html).not.toContain('>The three behaviours every guard answers a tool call through.<')
  })
})

// ── which declaration, and which member of it ────────────────────────────────
//
// JARVIS narrates a page one beat at a time, and a beat that says "this member" has to reach that
// one member row. Every members table was drawn alike before this, so a selector naming members
// reached every table on the page at once and a beat could point at the page but not at a part of
// it. Which property of a member carries its name is the LANGUAGE's answer, asked for at the one
// place that question is answered, so the page never reads a property by its spelling.

/** One member of an element, with everything `member` asks of one written. */
const archMember = (name: string, does: string): Record<string, unknown> => ({
  name,
  kind: 'property',
  reach: 'public',
  type: 'none',
  does,
})

/** One element of the architecture language, declared under the id it is known by. */
const archElement = (id: string, members: unknown): Record<string, unknown> => ({
  constant: {
    id,
    is: 'element',
    value: {
      form: 'interface',
      does: `What ${id} answers for, in one line.`,
      reach: 'public',
      exactness: 'loose',
      groups: { concept: 'checking', job: 'container' },
      members,
      sits: { package: 'none', assembly: 'A.Abstractions', namespace: 'A.Abstractions' },
      relates: { implements: 'none', answers: 'none', holds: 'none' },
    },
  },
})

/**
 * The page a document draws, read by a real DOM, with what the checker found in it. A selector is
 * matched here the way a browser matches one, which is the only way to prove a target reaches one
 * node and not several — the written page's root is empty and the page is drawn in the browser.
 *
 * The one place a DOM is built out of a page, so there is one crossing into the DOM library and
 * nothing below reads the page as text where it means to read it as a tree.
 */
const matched = (
  doc: Record<string, unknown>,
  where: string,
): { readonly page: Document; readonly found: CheckResult } => {
  const { html, found } = pageFor(doc, where)
  return { page: new JSDOM(html).window.document, found }
}

describe('which property of a member carries its name', () => {
  test('the architecture language says it is the member’s own name property', () => {
    const bodies = declaredBodiesOf(held('arch.yaml'))
    expect(propertyCarrying((id) => bodies.get(id), 'member', ROLES.name)).toBe('name')
  })

  // A member is not declared and carries no id, which `arch.yaml` says where it declares `member`.
  // The role's recorded meaning was written for a name drawn BESIDE an id, so it has to say what
  // it means where there is no id to draw beside, or the language says one thing and the page
  // another.
  test('MetaLang’s record of the name role covers a thing that has a name and no id', () => {
    const role = declaredBodiesOf(held('metalang.yaml')).get('role')
    const values = Array.isArray(role?.values) ? (role.values as Record<string, unknown>[]) : []
    const name = values.find((one) => one.id === ROLES.name)
    expect(name).toBeDefined()
    expect(text(name?.description)).toContain('no id')
  })
})

describe('pointing at one declaration and at one member of it', () => {
  const where = beside('parts.yaml')

  /** Two elements, each with its own members, one member name held by both of them. */
  const two = (): Record<string, unknown> =>
    archDocument(
      archElement('IGuard', [
        archMember('Name', 'The name this guard reports itself by.'),
        archMember('PrecheckAsync', 'Judges a tool call before it runs.'),
      ]),
      archElement('IStore', [archMember('Name', 'The name of this store.')]),
    )

  test('a selector naming one declaration’s members reaches that one table', () => {
    const { page } = matched(two(), where)
    // Both tables are drawn, so the selector below is narrowing rather than finding the only one.
    expect(page.querySelectorAll('table.d-table').length).toBe(2)
    expect(page.querySelectorAll('[data-declaration="IGuard"] table.d-table').length).toBe(1)
    expect(
      page.querySelector('[data-declaration="IGuard"] table.d-table')?.textContent,
    ).toContain('PrecheckAsync')
  })

  test('a selector naming one member of one declaration reaches that one row', () => {
    const rows = matched(two(), where).page.querySelectorAll(
      '[data-declaration="IGuard"] [data-member="PrecheckAsync"]',
    )
    expect(rows.length).toBe(1)
    expect(rows[0]?.tagName).toBe('TR')
    expect(rows[0]?.textContent).toContain('Judges a tool call before it runs.')
  })

  test('a member name two declarations share resolves to the one asked for', () => {
    const { page } = matched(two(), where)
    expect(page.querySelectorAll('[data-member="Name"]').length).toBe(2)
    const rows = page.querySelectorAll('[data-declaration="IStore"] [data-member="Name"]')
    expect(rows.length).toBe(1)
    expect(rows[0]?.textContent).toContain('The name of this store.')
  })

  // A CSS string ends at an unescaped quote of the kind it opened with, and a backslash escapes
  // whatever follows it, so those are the two characters the selector has to carry escaped. The
  // name itself travels through an HTML attribute on the way, which is where an unescaped quote
  // would have ended the attribute instead.
  test('a member name carrying a double quote, a single quote and a backslash is reachable', () => {
    const hard = 'Quote"And\'Apostrophe\\Backslash'
    const { page } = matched(
      archDocument(archElement('IGuard', [archMember(hard, 'The odd one out.')])),
      where,
    )
    const rows = page.querySelectorAll(
      `[data-member="${hard.replace(/\\/g, '\\\\').replace(/"/g, '\\"')}"]`,
    )
    expect(rows.length).toBe(1)
    expect(rows[0]?.getAttribute('data-member')).toBe(hard)
  })

  // Two declarations writing the same id is a question the document has not answered. The page
  // says so by carrying both, so whoever asks for that id is told there are two of them rather
  // than handed the first and left believing it was the only one.
  test('two declarations sharing an id read as the two they are, not as the first of them', () => {
    const sections = [
      ...matched(
        archDocument(
          archElement('IGuard', [archMember('First', 'The member of the first.')]),
          archElement('IGuard', [archMember('Second', 'The member of the second.')]),
        ),
        where,
      ).page.querySelectorAll('[data-declaration="IGuard"]'),
    ]
    expect(sections.length).toBe(2)
    expect(
      sections.map((one) => one.querySelector('[data-member]')?.getAttribute('data-member')),
    ).toEqual(['First', 'Second'])
  })
})

// ── one name carried by two members of one declaration ───────────────────────
//
// A beat that says "this member" has to reach one row, and a name two members of one declaration
// share reaches both of them, which identifies neither. So a shared name is not an identity on its
// own: each row sharing it carries its place among the rows sharing it, counted from one in the
// order the source declares them, and the bare name stands on no row at all. A declaration that
// names each of its members once is untouched by this, which is what keeps every target written
// against the kit today working.

describe('two members of one declaration carrying one name', () => {
  const where = beside('parts.yaml')

  /**
   * One declaration whose two `PrecheckAsync` members sit under an unrelated member, so what the
   * place in each identity counts is visible: among the rows sharing the name the two are the
   * first and the second, while among all of the declaration's members they would be the second
   * and the third.
   */
  const overloaded = (): Record<string, unknown> =>
    archDocument(
      archElement('IGuard', [
        archMember('Name', 'The name this guard reports itself by.'),
        archMember('PrecheckAsync', 'Judges a tool call before it runs.'),
        archMember('PrecheckAsync', 'Judges a tool call it is handed the session with.'),
      ]),
    )

  /**
   * The one row a member's identity reaches on the drawn page, as the one node it has to be. The
   * identity is written into the expectation rather than only into the selector, so a failure
   * names the identity that went unreached and what was drawn instead.
   */
  const oneRow = (page: Document, member: string): Element | undefined => {
    const rows = [...page.querySelectorAll(`[data-declaration="IGuard"] [data-member="${member}"]`)]
    expect(rows.map((row) => `${row.tagName} ${row.getAttribute('data-member')}`)).toEqual([
      `TR ${member}`,
    ])
    return rows[0]
  }

  test('each of the two is reached by its place among the members sharing the name', () => {
    const { page } = matched(overloaded(), where)
    expect(oneRow(page, 'PrecheckAsync#1')?.textContent).toContain(
      'Judges a tool call before it runs.',
    )
    expect(oneRow(page, 'PrecheckAsync#2')?.textContent).toContain(
      'Judges a tool call it is handed the session with.',
    )
  })

  test('the shared name on its own reaches no row, because it names neither of them', () => {
    const { page } = matched(overloaded(), where)
    expect(page.querySelectorAll('[data-member="PrecheckAsync"]').length).toBe(0)
  })

  test('a declaration naming each of its members once is drawn as it was', () => {
    const { page } = matched(
      archDocument(
        archElement('IGuard', [
          archMember('Name', 'The name this guard reports itself by.'),
          archMember('PrecheckAsync', 'Judges a tool call before it runs.'),
        ]),
      ),
      where,
    )
    expect(oneRow(page, 'Name')?.textContent).toContain('The name this guard reports itself by.')
    expect(oneRow(page, 'PrecheckAsync')?.textContent).toContain(
      'Judges a tool call before it runs.',
    )
    expect(page.querySelectorAll('[data-member*="#"]').length).toBe(0)
  })
})

describe('a shape whose language has not said which property is the name', () => {
  // Three shapes drawn side by side in one document: one whose language gives the name role to a
  // property, one that gives it to none, and one that gives it to two. The question has one
  // answer or it has none, and where it has none the page draws no identity rather than picking a
  // property for itself. The shape that DID answer, beside them, is what says the page asked.
  const shapes = (): Record<string, unknown> =>
    file({
      declare: [
        { entity: { id: 'object', primitive: true } },
        { entity: { id: 'string', is: 'object', primitive: true } },
        { entity: { id: 'collection', is: 'object', abstract: true } },
        { entity: { id: 'set', is: 'collection' } },
        {
          entity: {
            id: 'titled',
            is: 'object',
            properties: { label: { is: 'string', role: ROLES.name }, note: { is: 'string' } },
          },
        },
        {
          entity: {
            id: 'plain',
            is: 'object',
            properties: { label: { is: 'string' }, note: { is: 'string' } },
          },
        },
        {
          entity: {
            id: 'both',
            is: 'object',
            properties: {
              label: { is: 'string', role: ROLES.name },
              other: { is: 'string', role: ROLES.name },
            },
          },
        },
        {
          entity: {
            id: 'form',
            is: 'object',
            properties: {
              titleds: { set: { of: 'titled' } },
              plains: { set: { of: 'plain' } },
              boths: { set: { of: 'both' } },
            },
          },
        },
        {
          enum: {
            id: 'forms',
            is: 'form',
            values: [
              {
                id: 'interface',
                value: {
                  titleds: [
                    { label: 'One', note: 'a' },
                    { label: 'Two', note: 'b' },
                  ],
                  plains: [
                    { label: 'Three', note: 'c' },
                    { label: 'Six', note: 'f' },
                  ],
                  boths: [{ label: 'Four', other: 'Five' }],
                },
              },
            ],
          },
        },
      ],
    })

  /** What each row of one of the three tables carries as its identity, in the order drawn. */
  const identities = (page: Document, part: string): (string | null)[] =>
    [...page.querySelectorAll(`[data-part="${part}"] table.d-table tbody tr`)].map((row) =>
      row.getAttribute('data-member'),
    )

  test('a shape no property of which carries the role draws its rows with no identity', () => {
    const { page } = matched(shapes(), LANGUAGE_FILE)
    expect(identities(page, 'titleds')).toEqual(['One', 'Two'])
    expect(identities(page, 'plains')).toEqual([null, null])
  })

  test('a shape two properties of which carry the role draws its rows with no identity', () => {
    const { page, found } = matched(shapes(), LANGUAGE_FILE)
    expect(identities(page, 'titleds')).toEqual(['One', 'Two'])
    expect(identities(page, 'boths')).toEqual([null])
    // The two-carrier report already exists and says which two carry it. One of it, not two.
    expect(found.problems.filter((one) => one.what.includes(ROLES.name))).toHaveLength(1)
  })
})
