// The renderer.
//
// There is no refusal in it. A document the page cannot fully draw still builds, and the part
// it cannot draw lands on the page as an error. The only thing that stops it is a file YAML
// cannot parse, because then there is no tree to draw at all.

import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { dirname, join } from 'node:path'

import { afterAll, beforeAll, describe, expect, test } from 'bun:test'

import {
  inlineScript,
  outputFor,
  outputPathFor,
  PAGE_SOURCE,
  pageHtml,
  render,
  renderWith,
  titleOf,
} from './render.ts'
import { dataFrom, DATA_ID, NO_PAGE_MARKS, type PageData } from './page-data.ts'
import { LANGUAGE_FILE } from './rulebook.ts'

/** The least a page can be given to draw: a vocabulary, a document, no marks, nothing found. */
const DATA: PageData = {
  words: { kinds: [] } as never,
  doc: { title: 'a document' },
  marks: NO_PAGE_MARKS,
  found: { problems: [], blockers: [], pending: [] },
}

let dir: string

beforeAll(async () => {
  dir = await mkdtemp(join(tmpdir(), 'metalang-render-'))
})

afterAll(async () => {
  await rm(dir, { recursive: true, force: true })
})

describe('the page a document is written into', () => {
  test('is one self-contained file with the stylesheet and the script inside it', () => {
    const html = pageHtml('A title', '.a{color:red}', 'var x=1', DATA)
    expect(html.startsWith('<!doctype html>')).toBe(true)
    expect(html).toContain('<title>A title</title>')
    expect(html).toContain('<style>.a{color:red}</style>')
    expect(html).toContain('<script>var x=1</script>')
    expect(html).toContain('<main id="root"></main>')
    expect(html).toContain('<meta charset="utf-8">')
  })

  test('a title carrying markup cannot close the title element', () => {
    const html = pageHtml('</title><script>bad()</script>', '', '', DATA)
    // Escaping `<` is what makes it safe: the parser ends a title at `</title`, and there is
    // no longer one in the text. A bare `>` in element text needs no escaping.
    expect(html).toContain('<title>&lt;/title>&lt;script>bad()&lt;/script></title>')
    expect(html).not.toContain('<title></title>')
  })

  test('an ampersand in a title is written as one', () => {
    expect(pageHtml('this & that', '', '', DATA)).toContain('this &amp; that')
  })
})

describe('putting a bundle inside a script element', () => {
  test('nothing is left that the HTML parser would read as the end of the script', () => {
    const js = 'var a = "</script>", b = "<!-- x -->"'
    const out = inlineScript(js)
    expect(out).not.toContain('</script')
    expect(out).not.toContain('<!--')
  })

  test('the escaped text is the same string to JavaScript', () => {
    // eslint-disable-next-line @typescript-eslint/no-implied-eval, @typescript-eslint/no-unsafe-call
    const value = new Function(`return ${inlineScript('"</script><!--"')}`)() as unknown
    expect(value).toBe('</script><!--')
  })

  test('catches a closing tag however it is cased, and an unclosed one', () => {
    expect(inlineScript('"</SCRIPT>"')).not.toContain('</SCRIPT')
    expect(inlineScript('"</script"')).not.toContain('</script')
  })

  test('every occurrence is escaped, not only the first', () => {
    expect(inlineScript('"</script>" + "</script>"').match(/<\\\/script/g)).toHaveLength(2)
  })

  test('code that merely compares with a less-than is left alone', () => {
    expect(inlineScript('if (a < b) return 1')).toBe('if (a < b) return 1')
  })

  test('the real page carries nothing that could end its script', () => {
    const html = pageHtml('T', '', 'var a = "</script>"', DATA)
    const script = html.slice(html.indexOf('<script>') + 8, html.lastIndexOf('</script>'))
    expect(script).not.toContain('</script')
  })
})

describe('where a page is written when the caller does not say', () => {
  test('beside the document, with the yaml suffix swapped for html', () => {
    expect(outputPathFor('/x/metalang.yaml')).toBe('/x/metalang.html')
    expect(outputPathFor('/x/metalang.yml')).toBe('/x/metalang.html')
  })

  test('a path with no yaml suffix keeps its name and gains nothing', () => {
    expect(outputPathFor('/x/metalang')).toBe('/x/metalang')
  })

  test('only the suffix is replaced, not the same letters inside the name', () => {
    expect(outputPathFor('/x/yaml-notes.yaml')).toBe('/x/yaml-notes.html')
  })
})

describe('the title a page carries', () => {
  test('is the document title when it has one', () => {
    expect(titleOf({ title: 'MetaLang' }, '/x/metalang.yaml')).toBe('MetaLang')
  })

  test('falls back to the file name when the document has no title', () => {
    expect(titleOf({}, '/x/metalang.yaml')).toBe('metalang.yaml')
  })

  test('falls back when the title is not text', () => {
    expect(titleOf({ title: ['a'] }, '/x/metalang.yaml')).toBe('metalang.yaml')
  })

  test('falls back when the document is not a mapping', () => {
    expect(titleOf(null, '/x/f.yaml')).toBe('f.yaml')
    expect(titleOf([1], '/x/f.yaml')).toBe('f.yaml')
  })
})

describe('rendering a file', () => {
  test('writes a page for the live language file', async () => {
    const out = join(dir, 'live.html')
    expect(await render(LANGUAGE_FILE, out)).toBe(0)
    const html = await readFile(out, 'utf8')
    expect(html).toContain('<title>MetaLang</title>')
    // The vocabulary and the document are both inside the one file.
    expect(html).toContain('declare')
    expect(html).toContain('<script>')
    // The bundle holds React, which carries both of these in its own strings. Either one left
    // unescaped ends the script element early and the page does not run at all.
    const script = html.slice(html.indexOf('<script>') + 8, html.lastIndexOf('</script>'))
    expect(script).not.toContain('</script')
    expect(script).not.toContain('<!--')
  })

  test('a document the page cannot fully draw still builds', async () => {
    const path = join(dir, 'odd.yaml')
    await writeFile(path, 'scope: [x]\ntitle: Odd\nnotes: something\n', 'utf8')
    const out = join(dir, 'odd.html')
    expect(await render(path, out)).toBe(0)
    expect((await readFile(out, 'utf8')).length).toBeGreaterThan(0)
  })

  test('an empty document still builds', async () => {
    const path = join(dir, 'empty.yaml')
    await writeFile(path, '', 'utf8')
    const out = join(dir, 'empty.html')
    expect(await render(path, out)).toBe(0)
    expect(await readFile(out, 'utf8')).toContain('<title>empty.yaml</title>')
  })

  test('a file YAML cannot parse is the one thing that stops it', async () => {
    const path = join(dir, 'broken.yaml')
    await writeFile(path, 'declare: [\n  - entity: {\n', 'utf8')
    expect(await render(path, join(dir, 'broken.html'))).toBe(1)
  })

  test('a file that is not there stops it', async () => {
    expect(await render(join(dir, 'missing.yaml'), join(dir, 'missing.html'))).toBe(1)
  })

  test('what the checker found is written into the page, to be said on the document', async () => {
    const path = join(dir, 'wrong.yaml')
    await writeFile(
      path,
      'scope: [x]\ntitle: Wrong\nlanguage: metalang\ndeclare:\n  - entity:\n      id: 7\n',
      'utf8',
    )
    const out = join(dir, 'wrong.html')
    expect(await render(path, out)).toBe(0)
    const html = await readFile(out, 'utf8')
    expect(html).toContain('declare[0].entity.id')
    expect(html).toContain('must be written as text')
  })

  test('a document nothing was found in carries nothing to say about itself', async () => {
    const out = join(dir, 'clean.html')
    expect(await render(LANGUAGE_FILE, out)).toBe(0)
    // The bundler writes the value out as source, so it is read here the way it is written there.
    expect(await readFile(out, 'utf8')).toContain('problems:[],blockers:[]')
  })
})

describe('a highlight drawn over the file it names', () => {
  const doc = `
scope: [arch]
title: The architecture language
declare:
  - choice: { id: mark, options: [glyph, uri] }
`

  const highlight = (body: string): string => `of: doc.yaml\n${body}`

  test('the file it names is the one drawn, and the page is written beside that file', async () => {
    await writeFile(join(dir, 'doc.yaml'), doc)
    await writeFile(
      join(dir, 'doc.highlight.yaml'),
      highlight('highlight:\n  - path: declare[mark].options\n    note: Look here.\n'),
    )
    const out = outputPathFor(outputFor(join(dir, 'doc.highlight.yaml')))
    expect(out).toBe(join(dir, 'doc.html'))
    expect(await render(join(dir, 'doc.highlight.yaml'), out)).toBe(0)
    const html = await readFile(out, 'utf8')
    expect(html).toContain('<title>The architecture language</title>')
    expect(html).toContain('declare[0].choice.options')
    expect(html).toContain('Look here.')
  })

  test('a path naming nothing still draws the page, and the page carries the path', async () => {
    await writeFile(join(dir, 'doc.yaml'), doc)
    await writeFile(
      join(dir, 'stale.highlight.yaml'),
      highlight('highlight:\n  - path: declare[gone].options\n    note: stale\n'),
    )
    const out = join(dir, 'stale.html')
    expect(await render(join(dir, 'stale.highlight.yaml'), out)).toBe(0)
    expect(await readFile(out, 'utf8')).toContain('declare[gone].options')
  })

  test('a highlight whose own shape is wrong still draws the file it names', async () => {
    await writeFile(join(dir, 'doc.yaml'), doc)
    await writeFile(join(dir, 'bad.highlight.yaml'), 'of: doc.yaml\nhighlight: one\n')
    const out = join(dir, 'bad.html')
    expect(await render(join(dir, 'bad.highlight.yaml'), out)).toBe(0)
    expect(await readFile(out, 'utf8')).toContain('<title>The architecture language</title>')
  })

  test('a highlight naming a file that is not there is not drawn', async () => {
    await writeFile(join(dir, 'lost.highlight.yaml'), 'of: nowhere.yaml\nhighlight: []\n')
    expect(await render(join(dir, 'lost.highlight.yaml'), join(dir, 'lost.html'))).toBe(1)
  })

  test('a document is named after itself', () => {
    expect(outputFor(join(dir, 'doc.yaml'))).toBe(join(dir, 'doc.yaml'))
  })

  test('a file that cannot be read is named after itself', () => {
    expect(outputFor(join(dir, 'nothing-here.yaml'))).toBe(join(dir, 'nothing-here.yaml'))
  })
})

describe('the data a page carries', () => {
  test('sits in its own element, read back as what was written', () => {
    const html = pageHtml('t', '', 'var x=1', DATA)
    expect(html).toContain(`id="${DATA_ID}"`)
    const block = html.split(`id="${DATA_ID}">`)[1]?.split('</script>')[0] ?? ''
    expect(dataFrom(block)).toEqual(DATA)
  })

  test('a document that carries a closing script tag cannot end the block', () => {
    const hostile: PageData = { ...DATA, doc: { title: '</script><script>bad()</script>' } }
    const html = pageHtml('t', '', '', hostile)
    const block = html.split(`id="${DATA_ID}">`)[1]?.split('</script>')[0] ?? ''
    expect(dataFrom(block)).toEqual(hostile)
  })
})

describe('drawing with a bundle the caller already has', () => {
  test('uses it, and never asks for a bundler', async () => {
    const out = join(dir, 'prebuilt.html')
    let asked = 0
    const code = await renderWith(LANGUAGE_FILE, out, () => {
      asked += 1
      return Promise.resolve('/* a bundle built earlier */')
    })
    expect(code).toBe(0)
    expect(asked).toBe(1)
    const html = await readFile(out, 'utf8')
    expect(html).toContain('/* a bundle built earlier */')
    expect(html).toContain(`id="${DATA_ID}"`)
  })

  test('the page it writes carries the document, so one bundle draws any document', async () => {
    const out = join(dir, 'carries.html')
    await renderWith(LANGUAGE_FILE, out, () => Promise.resolve(''))
    const html = await readFile(out, 'utf8')
    const block = html.split(`id="${DATA_ID}">`)[1]?.split('</script>')[0] ?? ''
    expect(dataFrom(block).doc).not.toBeNull()
  })
})

// ── the story helper ─────────────────────────────────────────────────────────
//
// JARVIS narrates a package by ringing a beat and pointing at what the beat is about. The helper
// that rings and points is served by JARVIS rather than built here, so the page loads it from the
// path JARVIS serves it at, and it is loaded by every page a document is written into — including
// the one a kit draws, because the page shell is written in one place.
describe('the story helper a narrated page loads', () => {
  /** The tag the contract settled, written out so the test says what a page has to carry. */
  const HELPER = '<script defer src="/api/packages/story.js"></script>'

  test('the page loads it', () => {
    expect(pageHtml('T', '', 'var x=1', DATA)).toContain(HELPER)
  })

  test('it sits after the data the page draws and before the bundle that draws it', () => {
    const html = pageHtml('T', '', 'var x=1', DATA)
    const data = html.indexOf(`id="${DATA_ID}"`)
    const helper = html.indexOf(HELPER)
    const bundle = html.indexOf('<script>var x=1</script>')
    expect(data).toBeGreaterThan(-1)
    expect(helper).toBeGreaterThan(data)
    expect(bundle).toBeGreaterThan(helper)
  })

  // The helper carries a closing script tag of its own. Written last in the page it would end the
  // search this file reads the bundle out of, and the bundle's own closing tag would land inside
  // that slice — which is what forces the helper to sit before the bundle rather than after it.
  test('the slice the bundle is read out of is still the bundle and nothing else', () => {
    const html = pageHtml('T', '', 'var a = "x"', DATA)
    expect(html.slice(html.indexOf('<script>') + 8, html.lastIndexOf('</script>'))).toBe(
      'var a = "x"',
    )
  })
})

// ── what a drawn page says about being narrated ──────────────────────────────
//
// The helper is served by JARVIS rather than built into the page, so a page drawn anywhere else
// loads a script that is not there. Until the page says so it draws exactly as a page that can be
// narrated draws, and whoever is looking at it has no way to tell — which is the silent failure
// `CLAUDE.md` forbids. So the page's own bundle reads the helper's announcement and, finding
// none, draws one plain line saying the page cannot be narrated here.
//
// What a page SHOWS is what is read here, not what the bundle's source says: the line is drawn in
// the browser and is written into the page's own text nowhere. So the real page is written to
// disk and the real bundle entry is run over it in a real DOM — one process per case, because a
// bundle mounts a page once and the two cases are two pages.
describe('what a drawn page says about being narrated', () => {
  /**
   * The harness, which runs a page's own bundle over the page and reports what the page shows.
   *
   * It is written beside the page rather than kept in this folder because it is not part of the
   * page or of the renderer: it is how this test looks at a drawn page. The DOM library reaches it
   * by path for the same reason — run from a folder outside the repository, a bare import of it
   * resolves to nothing.
   */
  const HARNESS = `
import { readFileSync } from 'node:fs'

const [page, entry, helper] = process.argv.slice(2)
const { JSDOM } = await import(process.env.JSDOM_MODULE)

// The page as a browser holds it: parsed, with its own scripts left unrun. The bundle is run
// below instead, which is the order a browser runs it in.
const dom = new JSDOM(readFileSync(page, 'utf8'), { url: 'https://jarvis.local/package/one/' })
globalThis.window = dom.window
globalThis.document = dom.window.document
globalThis.navigator = dom.window.navigator

await import(entry)

const tick = () => new Promise((settle) => setTimeout(settle, 10))

// The page has to have been drawn before anything is read off it, or a page that drew nothing
// reads as a page that had nothing to say.
const root = dom.window.document.getElementById('root')
if (root === null) throw new Error('the page has no root for the bundle to draw into')
for (let i = 0; i < 200 && root.children.length === 0; i += 1) await tick()
if (root.children.length === 0) throw new Error('the page drew nothing, so nothing can be read off it')

// JARVIS's story helper announces itself on the window as it loads. A page drawn away from
// JARVIS loads nothing, so there is no announcement to find. In a browser the window IS the
// global, so the announcement is written to both here and either way of reading it finds it.
//
// It is written here, after the page's inline bundle has run and drawn and before the two events
// below, because that is the order a browser puts them in: an inline script runs first and a
// deferred script announces itself after it. A bundle that read the announcement at mount would
// find nothing in this case and draw the line, which is what makes the announced case able to
// fail at all. The silent case never announces, so this leaves it as it was.
if (helper === 'announced') {
  dom.window.__jarvisStory = true
  globalThis.__jarvisStory = true
}

// The two events a browser fires once the deferred scripts have run, so a reading that waits
// for them is given them here as well.
dom.window.document.dispatchEvent(new dom.window.Event('DOMContentLoaded', { bubbles: true }))
dom.window.dispatchEvent(new dom.window.Event('load'))
for (let i = 0; i < 20; i += 1) await tick()

// Every element saying the page cannot be narrated, less the ones saying it only because
// something inside them does. What is left is the lines a reader reads.
const said = /cannot be narrated/i
const matching = [...dom.window.document.querySelectorAll('*')].filter((el) => said.test(el.textContent ?? ''))
const lines = matching.filter((el) => !matching.some((other) => other !== el && el.contains(other)))

process.stdout.write(JSON.stringify({ lines: lines.map((el) => (el.textContent ?? '').trim()) }))
`

  /** Whether the story helper had announced itself before the page's bundle looked for it. */
  type Helper = 'announced' | 'silent'

  /** What a drawn page shows: the lines on it saying the page cannot be narrated here. */
  interface Shown {
    readonly lines: readonly string[]
  }

  let page: string
  let harness: string

  beforeAll(async () => {
    // The real page, written by the renderer, with the bundle left out of the shell: the bundle is
    // run by the harness instead, so what it is given is the page a reader is given.
    page = join(dir, 'narrated.html')
    await renderWith(LANGUAGE_FILE, page, () => Promise.resolve(''))
    harness = join(dir, 'in-a-browser.mjs')
    await writeFile(harness, HARNESS, 'utf8')
  })

  /** The page read with the helper either announced or silent, by the bundle the page carries. */
  const shownBy = (helper: Helper): Shown => {
    const ran = Bun.spawnSync(
      [process.execPath, 'run', harness, page, join(dirname(LANGUAGE_FILE), PAGE_SOURCE), helper],
      {
        env: { ...process.env, JSDOM_MODULE: Bun.resolveSync('jsdom', import.meta.dir) },
        stdout: 'pipe',
        stderr: 'pipe',
      },
    )
    const out = ran.stdout.toString()
    if (ran.exitCode !== 0) {
      throw new Error(
        `the page could not be read with the helper ${helper}: ${out}${ran.stderr.toString()}`,
      )
    }
    return JSON.parse(out) as Shown
  }

  /**
   * How long one case is given to read its page.
   *
   * Reading one starts a DOM subprocess that parses the page and runs its bundle, and `spawnSync`
   * blocks for all of it. That cost ran to 5392 milliseconds on one machine, against bun's five
   * second default, so a page saying exactly what it should went red on the clock instead; the
   * macOS, Windows and Linux runners are slower again. Stated here, each case fails only for what
   * its page shows, and still fails rather than hanging.
   */
  const READING_A_PAGE = 60_000

  test(
    'a page the helper never announced itself on says it cannot be narrated here, once',
    () => {
      expect(shownBy('silent').lines).toHaveLength(1)
    },
    READING_A_PAGE,
  )

  test(
    'a page the helper announced itself on says nothing about not being narrated',
    () => {
      expect(shownBy('announced').lines).toEqual([])
    },
    READING_A_PAGE,
  )
})
