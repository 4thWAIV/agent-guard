// The kit's command: check a document, or draw it as a page.
//
// This is the whole surface a consumer of the kit meets. It owns no checking and no drawing of
// its own: `check` is check.ts's `run` and `render` is render.ts's `render`, the same two the
// repository's own scripts call, so the kit cannot drift from what this repository proves.
//
// Every refusal here is a line the caller can act on, with the usage under it. Nothing falls
// through to a stack trace, and nothing guesses at a word it did not recognise.
//
// Drawing a page needs the page's bundle. A kit carries one, built when the kit was, and then
// drawing needs nothing but node. Inside this repository there is no carried bundle, so one is
// built on the spot, which needs bun. With neither, `render` says which is missing rather than
// failing inside the bundler.
//
// Usage:  bun run ts/metalang/cli.ts check <file.yaml>

import { existsSync } from 'node:fs'
import { basename, dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import { readFile } from 'node:fs/promises'

import { run } from './check.ts'
import {
  builtPageBundle,
  outputFor,
  outputPathFor,
  PAGE_BUNDLE,
  PAGE_SOURCE,
  renderWith,
} from './render.ts'

const here = dirname(fileURLToPath(import.meta.url))

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
  readonly hasPageBundle: boolean
  readonly hasBun: boolean
  readonly hasPageSource: boolean
}): string | null {
  // A bundle already built asks nothing of the runtime: the document travels in the page rather
  // than inside the bundle, so there is nothing left to build.
  if (runtime.hasPageBundle) return null

  if (!runtime.hasPageSource) {
    return (
      `render needs either ${PAGE_BUNDLE}, built when a kit is built, or ${PAGE_SOURCE} to build` +
      ' one from. Neither is beside this command, so it cannot draw a page; check works here.'
    )
  }
  if (!runtime.hasBun) {
    return (
      `render has ${PAGE_SOURCE} but no ${PAGE_BUNDLE}, so it would have to build one, and that` +
      ' needs bun on the path. check needs only node, so checking works here and drawing does not.'
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

  const carried = join(here, PAGE_BUNDLE)
  const hasPageBundle = existsSync(carried)

  const why = whyRenderCannotRun({
    hasPageBundle,
    hasBun: typeof Bun !== 'undefined',
    hasPageSource: existsSync(join(here, PAGE_SOURCE)),
  })
  if (why !== null) {
    console.error(why)
    return 2
  }

  const bundle = hasPageBundle ? () => readFile(carried, 'utf8') : builtPageBundle
  return renderWith(asked.file, asked.out, bundle)
}

if (import.meta.main) process.exit(await main(process.argv.slice(2)))
