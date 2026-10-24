// Colouring the examples in a page written by hand.
//
// What these tests are about: a page keeps its examples as the text they are, a block holding
// anything else is left exactly as it was, and running the same page through again gives the
// same page rather than colouring the colours.

import { mkdtempSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import { describe, expect, test } from 'bun:test'

import { classed, coloured, FROM, reach, textOf, TO, withStyle, writtenBy } from './examples.ts'

const root = import.meta.dir.replace(/\/ts\/metalang$/, '')

const page = (body: string): string => `<style>\n  pre { padding: 1rem; }\n</style>\n${body}\n`

describe('the text a block holds', () => {
  test('a block of plain text is the text it holds', () => {
    expect(textOf('title: A file')).toBe('title: A file')
  })

  test('what a page writes for the markup is read back as the characters it stands for', () => {
    expect(textOf('title: a &lt; b &amp; c')).toBe('title: a < b & c')
  })

  test('a block coloured already is read back as the text that was coloured', () => {
    const once = coloured(page('<pre data-inside="document">title: A file</pre>'), root)
    const held = /<pre[^>]*>([\s\S]*?)<\/pre>/.exec(once)?.[1] ?? ''
    expect(textOf(held)).toBe('title: A file')
  })
})

describe('what a block says it is', () => {
  test('a block saying nothing says nothing, and is left alone', () => {
    expect(writtenBy(' class="report"')).toBeNull()
  })

  test('a block says what it is written inside', () => {
    expect(writtenBy(' data-inside="declaration"')).toEqual({ inside: 'declaration' })
  })

  test('it says when its keys are names, and what its values are of', () => {
    expect(writtenBy(' data-inside="property" data-named data-of="form"')).toEqual({
      inside: 'property',
      named: true,
      of: 'form',
    })
  })
})

describe('the vocabulary an example is read against', () => {
  test('an example naming no document is read against the language', () => {
    expect(reach(undefined).words.kinds.length).toBeGreaterThan(0)
  })

  test('a document whose vocabulary cannot be read stops, rather than being coloured wrong', () => {
    const dir = mkdtempSync(join(tmpdir(), 'metalang-examples-'))
    const file = join(dir, 'lost.yaml')
    writeFileSync(file, 'scope: [lost]\nlanguage: nowhere\ntitle: Written in no language\n')
    // Coloured against the wrong words, every word of the example would be marked or, worse,
    // drawn as something it is not.
    expect(() => reach(file)).toThrow('nowhere')
  })
})

describe('the block the colours are drawn in', () => {
  test('a block that carries no class is given the one the colours are drawn against', () => {
    expect(classed(' data-inside="document"')).toBe(' data-inside="document" class="y"')
  })

  test('a block that carries a class of its own keeps it', () => {
    expect(classed(' class="wide" data-inside="document"')).toBe(
      ' class="wide y" data-inside="document"',
    )
  })

  test('a block that already carries it is left exactly as it is', () => {
    expect(classed(' class="y" data-inside="document"')).toBe(' class="y" data-inside="document"')
  })
})

describe('the page that comes back', () => {
  test('an example is coloured by what the vocabulary says each word is', () => {
    const html = coloured(page('<pre data-inside="document">title: A file</pre>'), root)
    expect(html).toContain('<span class="y-word">title</span>')
    expect(html).toContain('<span class="y-text">A file</span>')
  })

  test('an example is drawn in the block the colours are written for', () => {
    const html = coloured(page('<pre data-inside="document">title: A file</pre>'), root)
    expect(html).toContain('<pre data-inside="document" class="y">')
  })

  test('a block that says nothing is left exactly as it was', () => {
    const was = page('<pre class="report">declare[1].choice.options  "reserved" derives</pre>')
    expect(coloured(was, root)).toContain(
      '<pre class="report">declare[1].choice.options  "reserved" derives</pre>',
    )
  })

  test('an example reaching another document reaches what that document declares', () => {
    const was = page(
      '<pre data-inside="declaration" data-declared="ts/metalang/arch.yaml">' +
        '- entity:\n    id: holder\n    properties:\n      mark: { is: mark }</pre>',
    )
    // `mark` is declared in the architecture language and in no part of MetaLang, so without
    // that document it would be a word pointing at nothing.
    expect(coloured(was, root)).toContain('<span class="y-ref">mark</span>')
  })

  test('the colours are written into the page once, however often it is run', () => {
    const once = coloured(page('<pre data-inside="document">title: A file</pre>'), root)
    const twice = coloured(once, root)
    expect(twice).toBe(once)
    expect(once.split(FROM)).toHaveLength(2)
    expect(once.split(TO)).toHaveLength(2)
  })

  test('a page with nowhere to write the colours says so rather than losing them', () => {
    expect(() => withStyle('<p>no stylesheet here</p>')).toThrow(
      'the page holds no stylesheet for the colours to be written into',
    )
  })

  test('an example naming a document that cannot be read stops, rather than colouring it wrong', () => {
    const was = page('<pre data-inside="document" data-declared="ts/metalang/none.yaml">x: 1</pre>')
    expect(() => coloured(was, root)).toThrow()
  })
})
