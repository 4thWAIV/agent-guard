// Colours the MetaLang examples in a page written by hand.
//
// A page written for a person to read holds examples as blocks of YAML. This gives those blocks
// the same colours the rendered pages use, from the same reading in syntax.ts, so an example in a
// page and the same example inside the language never read differently.
//
// A block is coloured when it says what it is written inside:
//
//   <pre data-inside="declaration">  one or more declarations
//   <pre data-inside="property" data-named>  properties, by the names they are given
//   <pre data-inside="value" data-of="form" data-declared="ts/metalang/arch.yaml">
//
// `data-declared` names the document whose declarations the example reaches, for an example
// written against a language of our own rather than against MetaLang alone. A block saying none
// of this is left exactly as it is, which is how a block holding something else — the checker's
// own output, say — stays untouched.
//
// Running it again on a page it has already coloured gives the same page: the text is read back
// out of the block before it is coloured again.
//
// Usage:  bun run ts/metalang/examples.ts <page.html> [...]

import { readFileSync, writeFileSync } from 'node:fs'
import { basename, dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import * as yaml from 'js-yaml'

import { rulebookFor, WORDS } from './rulebook.ts'
import { htmlOf, partsOf, type Written } from './syntax.ts'
import { declaredBodiesOf, type Vocabulary } from './words.ts'

const here = dirname(fileURLToPath(import.meta.url))

/** Where the colours of the examples are written into a page, so running again replaces them. */
export const FROM = '/* ─── the colours of the examples, from ts/metalang/syntax.css ─── */'
export const TO = '/* ─── end of the colours of the examples ─── */'

const UNESCAPE: Record<string, string> = {
  '&amp;': '&',
  '&lt;': '<',
  '&gt;': '>',
  '&quot;': '"',
  '&#39;': "'",
}

/** The text a block holds, with the colouring of an earlier run taken back off. */
export function textOf(html: string): string {
  return html
    .replace(/<[^>]*>/g, '')
    .replace(/&amp;|&lt;|&gt;|&quot;|&#39;/g, (found) => UNESCAPE[found] ?? found)
}

/** What a block says about itself, which is what it is coloured against. */
export interface Marked extends Written {
  /** The document whose declarations the example reaches, as the page writes its path. */
  readonly from?: string
}

/** What a block says it is written inside, or nothing when it says nothing and is left alone. */
export function writtenBy(attributes: string): Marked | null {
  const inside = /data-inside="([^"]*)"/.exec(attributes)?.[1]
  if (inside === undefined) return null
  const of = /data-of="([^"]*)"/.exec(attributes)?.[1]
  const from = /data-declared="([^"]*)"/.exec(attributes)?.[1]
  return {
    inside,
    ...(/\bdata-named\b/.test(attributes) ? { named: true } : {}),
    ...(of === undefined ? {} : { of }),
    ...(from === undefined ? {} : { from }),
  }
}

/** The vocabulary a document is read against, and the declarations the document itself makes. */
export function reach(file: string | undefined): { words: Vocabulary; written: Written } {
  if (file === undefined) return { words: WORDS, written: {} }
  const doc = yaml.load(readFileSync(file, 'utf8'))
  const { words, faults } = rulebookFor(doc, file)
  // A vocabulary that could not be read would colour the example against the wrong words, so it
  // stops here rather than colouring it anyway.
  if (words === null) {
    throw new Error(`${basename(file)} — ${faults.map((f) => f.what).join('; ')}`)
  }
  return { words, written: { declared: declaredBodiesOf(doc) } }
}

/** The page, with every block that says what it is coloured, and the colours written into it. */
export function coloured(page: string, root: string): string {
  const withBlocks = page.replace(
    /<pre\b([^>]*)>([\s\S]*?)<\/pre>/g,
    (whole, attributes: string, held: string) => {
      const said = writtenBy(attributes)
      if (said === null) return whole
      const { from, ...written } = said
      const { words, written: reached } = reach(from === undefined ? undefined : join(root, from))
      const text = textOf(held)
      const coloured = htmlOf(partsOf(text, words, { ...reached, ...written }), words.kinds)
      return `<pre${classed(attributes)}>${coloured}</pre>`
    },
  )
  return withStyle(withBlocks)
}

/**
 * The block's attributes, carrying the class the colours are written against. A block already
 * carrying it keeps the attributes it has, so running again changes nothing.
 */
export function classed(attributes: string): string {
  const already = /class="([^"]*)"/.exec(attributes)
  if (already === null) return `${attributes} class="y"`
  const names = (already[1] ?? '').split(/\s+/).filter((n) => n.length > 0)
  if (names.includes(BLOCK)) return attributes
  return attributes.replace(already[0], `class="${[...names, BLOCK].join(' ')}"`)
}

/** The class a coloured block carries, which is what syntax.css draws a block by. */
export const BLOCK = 'y'

/** The page with the colours of the examples in its stylesheet, written once however often. */
export function withStyle(page: string): string {
  const css = readFileSync(join(here, 'syntax.css'), 'utf8').trimEnd()
  const block = `${FROM}\n${css}\n${TO}`
  const already = new RegExp(`${escapeForFind(FROM)}[\\s\\S]*?${escapeForFind(TO)}`)
  if (already.test(page)) return page.replace(already, block)
  const end = page.lastIndexOf('</style>')
  if (end < 0) throw new Error('the page holds no stylesheet for the colours to be written into')
  return `${page.slice(0, end)}\n${block}\n${page.slice(end)}`
}

const escapeForFind = (s: string): string => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')

if (import.meta.main) {
  const pages = process.argv.slice(2)
  if (pages.length === 0) {
    console.error('say which page to colour: bun run ts/metalang/examples.ts <page.html>')
    process.exit(1)
  }
  for (const page of pages) {
    const was = readFileSync(page, 'utf8')
    const now = coloured(was, process.cwd())
    writeFileSync(page, now)
    const blocks = [...now.matchAll(/<pre\b[^>]*data-inside=/g)].length
    console.log(`${basename(page)} — ${blocks} examples coloured`)
  }
}
