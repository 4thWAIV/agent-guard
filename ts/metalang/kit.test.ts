// Building the kit.
//
// The kit is assembled from the one copy of every source file, so nothing here edits or
// duplicates the checker. What is tested is what the kit carries, and that a file it must
// carry and cannot find stops the build instead of producing a kit missing a piece.

import { cp, mkdir, mkdtemp, readFile, readdir, rm } from 'node:fs/promises'
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
  /** The script a drawn page asks its origin for, which JARVIS serves and the kit does not carry. */
  const NARRATION_SCRIPT = '/api/packages/story.js'

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

  // A drawn page carries its own styles and its own script, so the readme tells whoever takes the
  // kit that a drawn page travels on its own. The one thing it does reach for is the narration
  // script, which JARVIS serves rather than the kit carrying it, so a page drawn here and opened
  // somewhere else is not narrated. The readme has to say that, or the one thing a drawn page
  // needs from outside itself is the one thing the readme leaves out.
  //
  // The script's path is written out here rather than read from the renderer, the same way the
  // kit's own story-helper test writes it out: this says what the readme has to name, not whatever
  // the renderer happens to be writing today.
  test('says a drawn page asks its origin for the narration script, and names it', () => {
    const narration = readmeText()
      .split('\n\n')
      .find((paragraph) => /narrat/i.test(paragraph) && paragraph.includes(NARRATION_SCRIPT))
    expect(narration).toBeDefined()
    expect(narration?.toLowerCase()).toContain('origin')
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

  /** The page a document draws through the built kit, under plain node. */
  const drawnByTheKit = async (out: string, name: string): Promise<string> => {
    const doc = join(dir, `${name}-document.yaml`)
    await Bun.write(doc, await Bun.file(join(import.meta.dir, 'parts.yaml')).text())
    const page = join(dir, `${name}.html`)
    const ran = Bun.spawnSync(['node', join(out, 'kit.js'), 'render', doc, page], CAPTURED)
    expect(ran.exitCode).toBe(0)
    return page
  }

  // A kit is cut from a branch and the page it draws is read by whoever took the kit, so the same
  // document drawn twice from the one kit has to be the same page. Two renders that differ by a
  // byte mean the page carries something that is not the document, and nobody downstream can tell
  // a real change from that.
  test('is drawn twice from one kit as the same bytes, so what a kit draws is reproducible', async () => {
    const out = join(dir, 'kit-arch-twice')
    await buildKit(out)
    const first = await readFile(await drawnByTheKit(out, 'arch-twice-one'))
    const second = await readFile(await drawnByTheKit(out, 'arch-twice-two'))
    expect(first.length).toBeGreaterThan(0)
    expect(first.equals(second)).toBe(true)
  })

  // The page shell is written in one place and the kit carries that one place, so a kit cut from
  // this branch draws a page JARVIS can narrate. The tag itself is written out rather than read
  // from the renderer, so this says what a released kit has to carry rather than agreeing with
  // whatever the renderer happens to write.
  test('the page it draws loads the story helper, so a released kit can be narrated', async () => {
    const out = join(dir, 'kit-arch-story')
    await buildKit(out)
    const html = await readFile(await drawnByTheKit(out, 'arch-story'), 'utf8')
    expect(html).toContain('<script defer src="/api/packages/story.js"></script>')
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

// ── the check that stands between a built kit and the release ────────────────
//
// The release job lists the files a kit asset must carry and is not allowed to ship one short of
// them. That list and the list `kit.ts` marks as required are two statements of one thing, so the
// check is read out of the workflow rather than written again here, and the two lists are held to
// each other.
describe('the required-file check the release job runs', () => {
  /** The release job's own check: the files it names, and the lines that enforce them. */
  const requiredFileCheck = async (): Promise<{
    readonly names: readonly string[]
    readonly script: string
  }> => {
    const workflow = join(import.meta.dir, '..', '..', '.github', 'workflows', 'ci.yml')
    const lines = (await readFile(workflow, 'utf8')).split('\n')
    const start = lines.findIndex((line) => line.includes('for required in'))
    if (start < 0) throw new Error(`${workflow} holds no required-file check to read`)
    const end = lines.findIndex((line, i) => i > start && line.trim() === 'done')
    if (end < 0) throw new Error(`${workflow}'s required-file check does not end`)
    const named = /for required in (.+); do/.exec(lines[start] ?? '')
    if (named === null) throw new Error(`${workflow}'s required-file check names no file`)
    return {
      names: (named[1] ?? '').trim().split(/\s+/),
      script: lines.slice(start, end + 1).join('\n'),
    }
  }

  /** One kit, built once. Building one is the slow part, so each case below copies this. */
  let built: string

  beforeAll(async () => {
    built = join(dir, 'kit-for-the-check')
    await buildKit(built)
  })

  /** That kit sitting where the release job builds it, so the check reads it where it looks. */
  const asset = async (name: string): Promise<string> => {
    const root = join(dir, name)
    await mkdir(join(root, 'dist'), { recursive: true })
    await cp(built, join(root, 'dist', 'evolang-kit'), { recursive: true })
    return root
  }

  // The release job runs the step as a script with no profile and no shell hook read, so the shell
  // here is started the same way. This machine sets `BASH_ENV`, and letting bash read it writes a
  // line of that hook's own onto the check's output and costs five seconds a run.
  const ranIn = async (root: string): Promise<ReturnType<typeof Bun.spawnSync>> =>
    Bun.spawnSync(['bash', '--noprofile', '--norc', '-c', (await requiredFileCheck()).script], {
      cwd: root,
      env: { ...process.env, BASH_ENV: '' },
      ...CAPTURED,
    })

  test('names every file the kit marks as required, so neither list can drift from the other', async () => {
    // What the kit writes, less the ones it says it can ship without: the required set, derived
    // rather than written out, so a file the kit starts requiring is required here the same day.
    const optional = carriedFiles()
      .filter((one) => !one.required)
      .map((one) => one.name)
    const required = (await readdir(built)).filter((name) => !optional.includes(name))
    expect([...(await requiredFileCheck()).names].sort()).toEqual([...required].sort())
  })

  for (const name of ['style.css', 'syntax.css']) {
    test(`refuses a kit asset with ${name} taken out of it`, async () => {
      const root = await asset(`ci-without-${name}`)
      await rm(join(root, 'dist', 'evolang-kit', name))
      const ran = await ranIn(root)
      expect(ran.exitCode).toBe(1)
      expect(said(ran)).toContain(`dist/evolang-kit/${name} is missing or empty`)
    })
  }

  test('passes a kit asset carrying every file it names', async () => {
    const ran = await ranIn(await asset('ci-complete'))
    expect(said(ran)).toBe('')
    expect(ran.exitCode).toBe(0)
  })
})
