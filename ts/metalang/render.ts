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
// Usage:  bun run ts/metalang/render.ts [file] [out]

import { readFileSync, writeFileSync } from 'node:fs'
import { basename, dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import * as yaml from 'js-yaml'

import { bundlePage } from './bundle.ts'
import { LANGUAGE_FILE, WORDS } from './check.ts'

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

/** Where a document's page is written when the caller does not say. */
export function outputPathFor(file: string): string {
  return file.replace(/\.ya?ml$/, '.html')
}

export async function render(file: string, out: string): Promise<number> {
  let doc: unknown
  try {
    doc = yaml.load(readFileSync(file, 'utf8'))
  } catch (e) {
    // A file YAML cannot parse has no tree to draw, so there is nothing a page could show.
    const message = e instanceof Error ? e.message : String(e)
    console.error(`${basename(file)} — not drawn`)
    console.error()
    console.error(`  ${message.split('\n')[0]}`)
    return 1
  }

  const { code } = await bundlePage(join(here, 'client.tsx'), {
    __WORDS__: JSON.stringify(WORDS),
    __DOC__: JSON.stringify(doc ?? null),
  })

  const css = readFileSync(join(here, 'style.css'), 'utf8')
  writeFileSync(out, pageHtml(titleOf(doc, file), css, code))

  console.log(`${basename(out)} — ${(code.length / 1024).toFixed(0)}KB, drawn in the browser by React`)
  return 0
}

if (import.meta.main) {
  const file = process.argv[2] ?? LANGUAGE_FILE
  process.exit(await render(file, process.argv[3] ?? outputPathFor(file)))
}
