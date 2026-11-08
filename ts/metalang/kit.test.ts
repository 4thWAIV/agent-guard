// Building the kit.
//
// The kit is assembled from the one copy of every source file, so nothing here edits or
// duplicates the checker. What is tested is what the kit carries, and that a file it must
// carry and cannot find stops the build instead of producing a kit missing a piece.

import { mkdtemp, readFile, readdir, rm } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import { afterAll, beforeAll, describe, expect, test } from 'bun:test'

/** Everything a run printed, on either stream, with neither assumed to be there. */
const said = (ran: { stdout?: Buffer | undefined; stderr?: Buffer | undefined }): string =>
  `${ran.stdout?.toString() ?? ''}${ran.stderr?.toString() ?? ''}`

/** Bun inherits a child's stderr unless told otherwise, and a refusal is written there. */
const CAPTURED = { stdout: 'pipe', stderr: 'pipe' } as const

import { buildKit, carriedFiles, LOCAL_VERSION, missingOf, readmeText } from './kit.ts'

let dir: string

beforeAll(async () => {
  dir = await mkdtemp(join(tmpdir(), 'metalang-kit-'))
})

afterAll(async () => {
  await rm(dir, { recursive: true, force: true })
})

describe('what the kit carries', () => {
  test('carries every language, not only the one a document names by default', () => {
    const names = carriedFiles().map((f) => f.name)
    expect(names).toContain('metalang.yaml')
    expect(names).toContain('arch.yaml')
    expect(names).toContain('propose.yaml')
  })

  test('carries no document, because a document is the consumer\'s own', () => {
    const names = carriedFiles().map((f) => f.name)
    expect(names).not.toContain('parts.yaml')
    expect(names).not.toContain('names.yaml')
  })

  test('carries both stylesheets, because drawing a page reads them from disk', () => {
    const names = carriedFiles().map((f) => f.name)
    expect(names).toContain('style.css')
    expect(names).toContain('syntax.css')
  })

  test('every carried file says whether the kit is refused without it', () => {
    for (const file of carriedFiles()) expect(typeof file.required).toBe('boolean')
  })

  test('every written language is required and EvoLang is not, because it is not written', () => {
    const byName = new Map(carriedFiles().map((f) => [f.name, f]))
    expect(byName.get('metalang.yaml')?.required).toBe(true)
    expect(byName.get('arch.yaml')?.required).toBe(true)
    expect(byName.get('propose.yaml')?.required).toBe(true)
    expect(byName.get('evolang.yaml')?.required).toBe(false)
  })
})

describe('a file the kit must carry and cannot find', () => {
  test('is named, rather than left out of the kit quietly', () => {
    const missing = missingOf([
      { name: 'metalang.yaml', from: '/nowhere/metalang.yaml', required: true },
    ])
    expect(missing).toEqual(['metalang.yaml'])
  })

  test('an absent optional file is not a refusal', () => {
    const missing = missingOf([
      { name: 'evolang.yaml', from: '/nowhere/evolang.yaml', required: false },
    ])
    expect(missing).toEqual([])
  })
})

describe('the readme the kit ships with', () => {
  test('shows both commands, because a consumer calls a command', () => {
    const text = readmeText()
    expect(text).toContain('check')
    expect(text).toContain('render')
  })

  test('says plainly that drawing a page needs bun and checking does not', () => {
    const text = readmeText()
    expect(text).toContain('bun')
    expect(text.toLowerCase()).toContain('node')
  })
})

describe('building the kit into a folder', () => {
  test('writes one bundle, the carried files, and the readme', async () => {
    const out = join(dir, 'evolang-kit')
    const built = await buildKit(out)

    const written = await readdir(out)
    expect(written).toContain('kit.js')
    expect(written).toContain('metalang.yaml')
    expect(written).toContain('style.css')
    expect(written).toContain('syntax.css')
    expect(written).toContain('README.md')
    // Node loads the bundle as a module only because this file says it is one.
    expect(written).toContain('package.json')
    expect(JSON.parse(await readFile(join(out, 'package.json'), 'utf8')).type).toBe('module')

    expect(built.carried).toContain('metalang.yaml')
    expect(built.absent).toContain('evolang.yaml')
  })

  test('the bundle holds the checker rather than an import of it', async () => {
    const out = join(dir, 'kit-two')
    await buildKit(out)
    const code = await readFile(join(out, 'kit.js'), 'utf8')
    expect(code).not.toContain("from './check.ts'")
    expect(code.length).toBeGreaterThan(10_000)
  })

  test('the built command checks a real document through the kit alone', async () => {
    const out = join(dir, 'kit-three')
    await buildKit(out)
    const ran = Bun.spawnSync(['node', join(out, 'kit.js'), 'check', join(out, 'metalang.yaml')], CAPTURED)
    expect(said(ran)).toContain('nothing is wrong')
    expect(ran.exitCode).toBe(0)
  })

  test('the built command refuses an unknown word with the usage, not a stack trace', async () => {
    const out = join(dir, 'kit-four')
    await buildKit(out)
    const ran = Bun.spawnSync(['node', join(out, 'kit.js'), 'renders', 'x.yaml'], CAPTURED)
    expect(said(ran)).toContain('no command called renders')
    expect(said(ran)).not.toContain('at Object')
    expect(ran.exitCode).toBe(2)
  })
})

describe('drawing a page from the built kit', () => {
  test('draws one under plain node, from the bundle the kit carries', async () => {
    const out = join(dir, 'kit-render-node')
    await buildKit(out)
    const page = join(out, 'drawn.html')
    const ran = Bun.spawnSync(
      ['node', join(out, 'kit.js'), 'render', join(out, 'metalang.yaml'), page],
      CAPTURED,
    )
    expect(ran.exitCode).toBe(0)
    const html = await readFile(page, 'utf8')
    expect(html.startsWith('<!doctype html>')).toBe(true)
    expect(html).toContain('id="page-data"')
    // The document is in the page, not in the bundle the kit carries.
    expect(html).toContain('MetaLang')
  })

  test('carries the page bundle, built when the kit was', async () => {
    const out = join(dir, 'kit-page-bundle')
    await buildKit(out)
    expect(await readdir(out)).toContain('page.js')
    const bundle = await readFile(join(out, 'page.js'), 'utf8')
    // One bundle for any document: the document is not inside it.
    expect(bundle).not.toContain('the approved vocabulary of the language')
    expect(bundle.length).toBeGreaterThan(10_000)
  })
})

describe('a required file the kit cannot find', () => {
  test('stops the build and names the file, rather than writing a kit without it', () => {
    expect(
      buildKit(join(dir, 'never-written'), LOCAL_VERSION, [
        { name: 'metalang.yaml', from: join(dir, 'gone.yaml'), required: true },
      ]),
    ).rejects.toThrow('metalang.yaml is not beside the source')
  })
})

describe('a document written in a language other than MetaLang', () => {
  test('is checked from the kit alone, under node, because the kit carries that language', async () => {
    const out = join(dir, 'kit-arch')
    await buildKit(out)
    // A real document written in the architecture language, copied away from the repository so
    // nothing but the kit can be resolving its language.
    const doc = join(dir, 'arch-document.yaml')
    await Bun.write(doc, await Bun.file(join(import.meta.dir, 'parts.yaml')).text())

    const ran = Bun.spawnSync(['node', join(out, 'kit.js'), 'check', doc], CAPTURED)
    expect(said(ran)).toContain('checked against "arch"')
    expect(said(ran)).toContain('nothing is wrong')
    expect(said(ran)).not.toContain('names no file this checker could find')
    expect(ran.exitCode).toBe(0)
  })

  test('is drawn as a page from the kit alone, under node', async () => {
    const out = join(dir, 'kit-arch-page')
    await buildKit(out)
    const doc = join(dir, 'arch-page-document.yaml')
    await Bun.write(doc, await Bun.file(join(import.meta.dir, 'parts.yaml')).text())
    const page = join(dir, 'arch.html')

    const ran = Bun.spawnSync(['node', join(out, 'kit.js'), 'render', doc, page], CAPTURED)
    expect(ran.exitCode).toBe(0)
    const html = await readFile(page, 'utf8')
    expect(html).toContain('id="page-data"')
    expect(html).toContain('The parts of the MetaLang toolchain')
  })
})

describe('the version the kit says it is', () => {
  test('is the one it was built with, so a consumer can tell which kit they have', async () => {
    const out = join(dir, 'kit-versioned')
    const built = await buildKit(out, '0.1.108169728-pre-release')
    expect(built.version).toBe('0.1.108169728-pre-release')
    const manifest = JSON.parse(await readFile(join(out, 'package.json'), 'utf8'))
    expect(manifest.version).toBe('0.1.108169728-pre-release')
  })

  test('says it is a local build when nobody gave it a version, rather than claiming one', async () => {
    const out = join(dir, 'kit-unversioned')
    const built = await buildKit(out)
    expect(built.version).toBe(LOCAL_VERSION)
    expect(LOCAL_VERSION).toContain('local')
    const manifest = JSON.parse(await readFile(join(out, 'package.json'), 'utf8'))
    expect(manifest.version).toBe(LOCAL_VERSION)
  })

  test('the readme names the version, because the folder travels away from this repository', async () => {
    const out = join(dir, 'kit-readme-version')
    await buildKit(out, '9.9.9-test')
    expect(await readFile(join(out, 'README.md'), 'utf8')).toContain('9.9.9-test')
  })
})
