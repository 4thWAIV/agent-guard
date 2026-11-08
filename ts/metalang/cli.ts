// The kit's command: check a document, or draw it as a page.
//
// This is the whole surface a consumer of the kit meets. It owns no checking and no drawing of
// its own: `check` is check.ts's `run` and `render` is render.ts's `render`, the same two the
// repository's own scripts call, so the kit cannot drift from what this repository proves.
//
// Every refusal here is a line the caller can act on, with the usage under it. Nothing falls
// through to a stack trace, and nothing guesses at a word it did not recognise.
//
// Drawing a page builds a browser bundle while it runs, which needs bun. Checking does not, so
// `check` runs under plain node and `render` says what it needs rather than failing inside the
// bundler.
//
// Usage:  bun run ts/metalang/cli.ts check <file.yaml>

import { existsSync } from 'node:fs'
import { basename, dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import { run } from './check.ts'
import { outputFor, outputPathFor, render } from './render.ts'

const here = dirname(fileURLToPath(import.meta.url))

/**
 * The file a page is built from. render.ts bundles it as the page is written, so drawing a page
 * needs this source beside the module asking for it — which the kit, being one built file, does
 * not have.
 */
const PAGE_SOURCE = 'client.tsx'

export const USAGE = [
  'usage:  evolang check <file.yaml>',
  '        evolang render <file.yaml> [out.html]',
].join('\n')

/** A document is read against the language it names; anything else is refused by name. */
const DOCUMENT = /\.ya?ml$/

export interface Checking {
  readonly name: 'check'
  readonly file: string
}

export interface Drawing {
  readonly name: 'render'
  readonly file: string
  readonly out: string
}

export interface Refused {
  readonly why: string
}

/**
 * What the words on the command line ask for, or the one line saying why they ask for nothing.
 *
 * The output path for a page comes from render.ts's own two rules — a highlight names the
 * document it marks, and a page is written beside its document — so the kit and the repository's
 * own command put a page in the same place.
 */
export function parse(argv: readonly string[]): Checking | Drawing | Refused {
  const [command, file, out] = argv

  if (command === undefined) return { why: 'no command given' }
  if (command !== 'check' && command !== 'render') {
    return { why: `there is no command called ${command}` }
  }
  if (file === undefined) {
    return { why: command === 'check' ? 'check needs a file to check' : 'render needs a file to draw' }
  }
  if (!DOCUMENT.test(file)) return { why: `${basename(file)} is not a .yaml or .yml file` }

  if (command === 'check') return { name: 'check', file }
  return { name: 'render', file, out: out ?? outputPathFor(outputFor(file)) }
}

/**
 * Why `render` cannot run here, or null when it can.
 *
 * Drawing a page bundles React around the document AS THE PAGE IS WRITTEN. That needs two
 * things checking does not: Bun, whose bundler does the build, and the page's own source, which
 * is what gets bundled. Inside this repository both are there. In the built kit neither is, so
 * the kit says which is missing rather than failing inside the bundler.
 */
export function whyRenderCannotRun(runtime: {
  readonly hasBun: boolean
  readonly hasPageSource: boolean
}): string | null {
  if (!runtime.hasPageSource) {
    return (
      `render builds a page from ${PAGE_SOURCE} and React as it writes it, and this kit is one` +
      ' built file, so it does not carry them. check works here; drawing a page is a command in' +
      ' the repository the kit was built from.'
    )
  }
  if (!runtime.hasBun) {
    return (
      'render draws a page by building a browser bundle, which needs bun on the path.' +
      ' check needs only node, so checking a document works here and drawing one does not.'
    )
  }
  return null
}

/** Runs one command and returns its exit code. Refusals are 2; the work's own code is its own. */
export async function main(argv: readonly string[]): Promise<number> {
  const asked = parse(argv)

  if ('why' in asked) {
    console.error(asked.why)
    console.error()
    console.error(USAGE)
    return 2
  }

  if (asked.name === 'check') return run(asked.file)

  const why = whyRenderCannotRun({
    hasBun: typeof Bun !== 'undefined',
    hasPageSource: existsSync(join(here, PAGE_SOURCE)),
  })
  if (why !== null) {
    console.error(why)
    return 2
  }
  return render(asked.file, asked.out)
}

if (import.meta.main) process.exit(await main(process.argv.slice(2)))
