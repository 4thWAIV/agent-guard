// The data block a page carries.
//
// It is the one thing both sides of the page agree on: the renderer writes it, the bundle reads
// it. So what is tested is the round trip, the escaping that stops the document ending the
// element it sits in, and that a block which cannot be read says so instead of drawing nothing.

import { describe, expect, test } from 'bun:test'

import { dataBlock, dataFrom, DATA_ID, NO_PAGE_MARKS } from './page-data.ts'

const page = {
  words: { kinds: ['entity'] } as never,
  doc: { title: 'a document' },
  marks: NO_PAGE_MARKS,
  found: { problems: [], blockers: [], pending: [] },
}

describe('the block the renderer writes', () => {
  test('is JSON, and reads back as what went in', () => {
    expect(dataFrom(dataBlock(page))).toEqual(page)
  })

  test('escapes every angle bracket, so nothing in a document can end the script element', () => {
    const hostile = { ...page, doc: { title: '</script><script>bad()</script>' } }
    const block = dataBlock(hostile)
    expect(block).not.toContain('</script')
    expect(block).toContain('\\u003c')
  })

  test('the escaping is valid JSON, so the page can still read it', () => {
    const hostile = { ...page, doc: { title: '</script> and <!-- a comment' } }
    expect(dataFrom(dataBlock(hostile))).toEqual(hostile)
  })

  test('survives a document carrying the characters YAML and HTML fight over', () => {
    const awkward = { ...page, doc: { title: 'a & b', note: '"quoted"\nand a newline—dash' } }
    expect(dataFrom(dataBlock(awkward))).toEqual(awkward)
  })

  test('the element id is one name both sides use', () => {
    expect(DATA_ID).toBe('page-data')
  })
})

describe('a block that cannot be read', () => {
  test('says the page carries no data rather than drawing an empty page', () => {
    expect(() => dataFrom('')).toThrow('the page carries no data')
  })

  test('says a block that is not JSON is not JSON, and does not guess', () => {
    expect(() => dataFrom('{ not json')).toThrow('the page data is not JSON')
  })

  test('refuses a block that parsed but holds no vocabulary, naming what is missing', () => {
    expect(() => dataFrom('{"doc":{}}')).toThrow('words')
  })
})
