// The data block a page carries, and the one description of it both sides of the page share.
//
// A page is one self-contained file. Everything it draws — the vocabulary it is read against, the
// document, the marks, and what the checker found — travels inside that file as one JSON block,
// written here by the renderer and read here by the bundle. Neither side owns a second copy of
// the shape, so the page cannot drift from what wrote it.
//
// It used to be replaced inside the bundle at build time, which meant a bundle per document and
// a bundler wherever a page was written. Reading it from the page instead lets one bundle, built
// once, draw a document it has never seen. The two properties that mattered are untouched:
// nothing in the bundle reaches the filesystem, because the block is in the same file; and the
// page cannot disagree with the checker, because the block is written by it.
//
// This module imports types only, so it is safe in the browser and on the command line alike.

import type { Marks } from './highlight.ts'
import type { CheckResult, Vocabulary } from './words.ts'

/** The element the block sits in. One name, used by whoever writes it and whoever reads it. */
export const DATA_ID = 'page-data'

/**
 * The marks as they travel. A Map cannot be written as JSON, so the pairs one is built from go
 * in its place and the page builds the Map back.
 */
export interface PageMarks {
  readonly marked: readonly [string, string][]
  readonly missing: readonly string[]
}

/** A page with nothing marked on it. */
export const NO_PAGE_MARKS: PageMarks = { marked: [], missing: [] }

/** Everything a page needs to draw itself. */
export interface PageData {
  readonly words: Vocabulary
  readonly doc: unknown
  readonly marks: PageMarks
  readonly found: CheckResult
}

/** The marks as the page uses them, built back from the pairs. */
export function marksFrom(marks: PageMarks): Marks {
  return { marked: new Map(marks.marked), missing: marks.missing }
}

/**
 * The block as it sits inside the page.
 *
 * Every `<` becomes its JSON escape. A document is free to contain the text that ends a script
 * element, and without this the HTML parser would end the block early and treat the rest of the
 * document as page text. `\\u003c` is valid JSON, unlike the backslash the JavaScript bundle is
 * escaped with, so the page still reads it back as what went in.
 */
export function dataBlock(data: PageData): string {
  return JSON.stringify(data).replace(/</g, '\\u003c')
}

/**
 * The data a page carries, or a loud refusal.
 *
 * A page that cannot read its own block has nothing to draw, and a blank page says nothing about
 * why. Each refusal names what is wrong instead.
 */
export function dataFrom(json: string): PageData {
  if (json.trim() === '') throw new Error('the page carries no data to draw')

  let read: unknown
  try {
    read = JSON.parse(json)
  } catch (e) {
    const message = e instanceof Error ? e.message : String(e)
    throw new Error(`the page data is not JSON: ${message}`)
  }

  if (read === null || typeof read !== 'object' || Array.isArray(read)) {
    throw new Error('the page data is not an object holding words, doc, marks and found')
  }

  const held = read as Record<string, unknown>
  const absent = ['words', 'marks', 'found'].filter((key) => held[key] === undefined)
  if (absent.length > 0) {
    throw new Error(`the page data is missing ${absent.join(', ')}`)
  }

  return read as PageData
}
