// Builds a document into ONE self-contained page that React draws in the browser.
//
// The YAML is read here, at build time. The vocabulary is read from words.ts, the one copy
// every script shares, and injected into the bundle — so the page cannot drift from the
// checker, and nothing in the bundle reaches the filesystem.
//
// There is no refusal in this file. A document this page cannot fully draw still builds, and
// the part it cannot draw appears ON THE PAGE as an error, where it can be seen. That is the
// whole point of the error control: a problem you can look at beats a command that stopped.
//
// Given a highlight file rather than a document, the document it names is the one drawn, with the
// marks it asks for. A highlight file names the file it highlights by that file's path, so it is
// the one thing needed to draw a marked page, and it can be rewritten without touching what it
// points at.
//
// Usage:  bun run ts/metalang/render.ts [file] [out]

import { readFileSync, writeFileSync } from 'node:fs'
import { basename, dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import * as yaml from 'js-yaml'

import { bundlePage } from './bundle.ts'
import { checkedAgainst } from './check.ts'
import { LANGUAGE_FILE, rulebookFor, WORDS } from './rulebook.ts'
import { highlightOf, isHighlight, marksOf, NO_MARKS, type Marks } from './highlight.ts'

const here = dirname(fileURLToPath(import.meta.url))

const escapeHtml = (s: string): string => s.replace(/&/g, '&amp;').replace(/</g, '&lt;')

/**
 * Makes a bundle safe to sit inside a `<script>` element.
 *
 * A script element holds raw text, so the HTML parser ends it at the first `</script` and
 * treats everything after as page text. React's bundle contains that string, and `<!--`, inside
 * its own string literals. Both are rewritten with a backslash, which JavaScript reads as the
 * same characters and the HTML parser no longer recognises.
 *
 * This is the price of one self-contained file, and it is the whole price: nothing else in a
 * bundle can end the element.
 */
export function inlineScript(js: string): string {
  return js.replace(/<\/(script)/gi, '<\\/$1').replace(/<!--/g, '<\\!--')
}

/** The whole page as one string of HTML. */
export function pageHtml(title: string, css: string, js: string): string {
  return `<!doctype html>
<html lang="en">
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>${escapeHtml(title)}</title>
<style>${css}</style>
<main id="root"></main>
<script>${inlineScript(js)}</script>
</html>
`
}

/** The title a document's page carries: its own title, or the file it came from. */
export function titleOf(doc: unknown, file: string): string {
  if (doc !== null && typeof doc === 'object' && !Array.isArray(doc)) {
    const title = (doc as Record<string, unknown>).title
    if (typeof title === 'string') return title
  }
  return basename(file)
}

/**
 * The file a page is named after: the document itself, or, given a highlight, the document it
 * names. A highlight draws the same page with marks on it, so it writes to the same place.
 */
export function outputFor(file: string): string {
  const read = loaded(file)
  if ('why' in read || !isHighlight(read.doc)) return file
  return join(dirname(file), highlightOf(read.doc).of)
}

/** Where a document's page is written when the caller does not say. */
export function outputPathFor(file: string): string {
  return file.replace(/\.ya?ml$/, '.html')
}

/** A file read as YAML, or the one line saying why it could not be. */
function loaded(file: string): { doc: unknown } | { why: string } {
  try {
    return { doc: yaml.load(readFileSync(file, 'utf8')) }
  } catch (e) {
    const message = e instanceof Error ? e.message : String(e)
    return { why: message.split('\n')[0] ?? 'could not be read' }
  }
}

const notDrawn = (file: string, why: string): number => {
  console.error(`${basename(file)} — not drawn`)
  console.error()
  console.error(`  ${why}`)
  return 1
}

export async function render(file: string, out: string): Promise<number> {
  const read = loaded(file)
  // A file YAML cannot parse has no tree to draw, so there is nothing a page could show.
  if ('why' in read) return notDrawn(file, read.why)

  let doc = read.doc
  let drawing = file
  let marks: Marks = NO_MARKS

  // A highlight says which file it is about, so that file is the one drawn and the page is
  // written beside it rather than beside the highlight.
  if (isHighlight(doc)) {
    const highlight = highlightOf(doc)
    const target = join(dirname(file), highlight.of)
    const held = loaded(target)
    if ('why' in held) return notDrawn(target, held.why)
    for (const wrong of highlight.wrong) console.error(`  ${basename(file)} — ${wrong}`)
    doc = held.doc
    drawing = target
    marks = marksOf(doc, highlight.entries)
    const said = `${marks.marked.size} marked`
    const lost = marks.missing.length === 0 ? '' : `, ${marks.missing.length} naming nothing`
    console.log(`${basename(file)} — ${said}${lost} in ${basename(target)}`)
  }

  // The page is drawn with the same vocabulary the checker reads the document against: the
  // language it names and the scopes it brings in. A document whose rulebook could not be read is
  // drawn against the one vocabulary there is, and the page says what could not be read, because
  // a page that shows the document is worth more than a command that stopped.
  const rulebook = rulebookFor(doc, drawing)
  const { words, faults } = rulebook
  for (const fault of faults) console.error(`  ${basename(drawing)} — ${fault.where} ${fault.what}`)

  // The page says what the checker found, on the document itself. It is the same checker reading
  // the same document against the same vocabulary, so the page and the report cannot disagree.
  const found = checkedAgainst(doc, drawing, rulebook)
  const wrong = found.problems.length + found.blockers.length + found.pending.length
  if (wrong > 0) console.log(`${basename(drawing)} — ${wrong} found, and drawn on the page`)

  const { code } = await bundlePage(join(here, 'client.tsx'), {
    __WORDS__: JSON.stringify(words ?? WORDS),
    __DOC__: JSON.stringify(doc ?? null),
    __MARKS__: JSON.stringify({
      marked: [...marks.marked],
      missing: marks.missing,
    }),
    __FOUND__: JSON.stringify(found),
  })

  // Both stylesheets, because the page draws examples through the same control a page written
  // by hand draws them through, and that control's colours live in their own file.
  const css = [
    readFileSync(join(here, 'style.css'), 'utf8'),
    readFileSync(join(here, 'syntax.css'), 'utf8'),
  ].join('\n')
  writeFileSync(out, pageHtml(titleOf(doc, drawing), css, code))

  console.log(`${basename(out)} — ${(code.length / 1024).toFixed(0)}KB, drawn in the browser by React`)
  return 0
}

if (import.meta.main) {
  const file = process.argv[2] ?? LANGUAGE_FILE
  process.exit(await render(file, process.argv[3] ?? outputPathFor(outputFor(file))))
}
