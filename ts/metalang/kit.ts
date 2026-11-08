// Builds the build kit: one folder someone else can take and use.
//
// The kit is ASSEMBLED from the one copy of every source file. Nothing here is a second
// checker or a second renderer, and no file is moved out of this folder to make it: the bundler
// reads the same modules the repository's own commands read, so the kit cannot disagree with
// what this repository proves about itself.
//
// What comes out is one bundled command, the language files the checker reads documents
// against, the two stylesheets drawing a page reads from disk, a package.json saying the bundle
// is a module, and a readme. A required file that is not there stops the build and is named;
// an optional one that is not there is reported as absent, because a kit quietly missing a
// piece is worse than one that was never written.
//
// Usage:  bun run ts/metalang/kit.ts [out]

import { copyFileSync, existsSync, mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import { bundleCommand, bundlePage } from './bundle.ts'
import { PAGE_BUNDLE, PAGE_SOURCE } from './render.ts'

const here = dirname(fileURLToPath(import.meta.url))

/** Where the kit is written when the caller does not say. */
export const KIT_DIR = join(here, '..', '..', 'dist', 'evolang-kit')

export interface Carried {
  /** The name it has inside the kit. */
  readonly name: string
  /** Where it is read from, which is always this one folder. */
  readonly from: string
  /** Whether the kit is refused without it. */
  readonly required: boolean
}

/**
 * Every file the kit carries beside the bundle.
 *
 * EVERY language, not only the one a document names by default. A document says which language
 * it is written in, and rulebook.ts resolves that language's file beside the module asking for
 * it — in the kit, beside the bundle. Carrying only MetaLang meant a document written in the
 * architecture language was refused with "names no file this checker could find", which is a
 * build kit that cannot read three quarters of what this repository writes.
 *
 * The three are languages. `names.yaml` and `parts.yaml` are documents written in the
 * architecture language and are not carried: a document is the consumer's own, and the kit
 * exists to read theirs.
 *
 * The stylesheets are read by render.ts the same way. `evolang.yaml` is listed and not required:
 * not one word of EvoLang is written, so the kit is proven against the three that exist and
 * carries EvoLang the day it does.
 */
export function carriedFiles(): readonly Carried[] {
  const beside = (name: string, required: boolean): Carried => ({
    name,
    from: join(here, name),
    required,
  })
  return [
    beside('metalang.yaml', true),
    beside('arch.yaml', true),
    beside('propose.yaml', true),
    beside('evolang.yaml', false),
    beside('style.css', true),
    beside('syntax.css', true),
  ]
}

/** The names of the required files that are not where they are read from. */
export function missingOf(files: readonly Carried[]): readonly string[] {
  return files.filter((file) => file.required && !existsSync(file.from)).map((file) => file.name)
}

/** What the kit tells whoever takes it. */
export function readmeText(): string {
  return `# The EvoLang build kit

One folder. It checks a document against the language it names, and draws it as one
self-contained page.

## Checking a document

    node kit.js check <file.yaml>

Checking reads the document and the language beside this readme. It needs node and nothing else.
It prints what is wrong with the document and exits 1, or says nothing is wrong and exits 0.

## Drawing a document as a page

    node kit.js render <file.yaml> [out.html]

Drawing needs node and nothing else. The page's own bundle was built once, when this kit was built,
and sits beside this readme as \`page.js\`; the document travels in the page's data block rather
than inside that bundle, which is what lets one bundle draw a document it has never seen.

The page that comes out carries its own styles and its own script and needs nothing from this folder
afterwards, so a drawn page travels on its own.

## What is in here

- \`kit.js\` — the command, with the checker and js-yaml built in.
- \`page.js\` — the page's bundle, built once when this kit was. Read when a page is drawn.
- \`metalang.yaml\` — the language that defines languages. A document naming no language is read
  against this one.
- \`arch.yaml\` — the architecture language: what code is made of. A document saying
  \`language: arch\` is read against it.
- \`propose.yaml\` — the proposal language, for putting a decision to a reader.
- \`style.css\`, \`syntax.css\` — read from this folder while a page is drawn.
- \`package.json\` — says the bundle is a module, which is how node loads it.

\`evolang.yaml\` joins this folder when EvoLang is written. Until then a document naming EvoLang is
refused by name rather than read against a language that does not exist.
`
}

export interface Built {
  readonly out: string
  readonly carried: readonly string[]
  readonly absent: readonly string[]
}

/**
 * Writes the kit into `out` and says what it carried and what was absent.
 *
 * The manifest is an argument so the refusal below can be exercised without moving a real file
 * out of the source folder. Every caller but a test uses the one real list.
 */
export async function buildKit(out: string, files: readonly Carried[] = carriedFiles()): Promise<Built> {
  const missing = missingOf(files)
  if (missing.length > 0) {
    throw new Error(
      `the kit was not built: ${missing.join(', ')} ` +
        `${missing.length === 1 ? 'is' : 'are'} not beside the source it is read from`,
    )
  }

  mkdirSync(out, { recursive: true })

  const { code } = await bundleCommand(join(here, 'kit-entry.ts'))
  writeFileSync(join(out, 'kit.js'), code)

  // The page's bundle, built once, here. It is what lets the kit draw a document under plain
  // node: the document travels in the page's data block rather than inside this bundle.
  const { code: page } = await bundlePage(join(here, PAGE_SOURCE), {})
  writeFileSync(join(out, PAGE_BUNDLE), page)

  // Node loads an ES module only when something says the file is one.
  writeFileSync(
    join(out, 'package.json'),
    `${JSON.stringify({ name: 'evolang-kit', private: true, type: 'module' }, null, 2)}\n`,
  )

  const carried: string[] = []
  const absent: string[] = []
  for (const file of files) {
    if (existsSync(file.from)) {
      copyFileSync(file.from, join(out, file.name))
      carried.push(file.name)
    } else {
      absent.push(file.name)
    }
  }

  writeFileSync(join(out, 'README.md'), readmeText())

  return { out, carried, absent }
}

if (import.meta.main) {
  const built = await buildKit(process.argv[2] ?? KIT_DIR)
  console.log(`${built.out} — kit.js, package.json, README.md, and ${built.carried.join(', ')}`)
  if (built.absent.length > 0) {
    console.log(`  not carried, because it is not written yet: ${built.absent.join(', ')}`)
  }
}
