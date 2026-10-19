// The renderer.
//
// There is no refusal in it. A document the page cannot fully draw still builds, and the part
// it cannot draw lands on the page as an error. The only thing that stops it is a file YAML
// cannot parse, because then there is no tree to draw at all.

import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import { afterAll, beforeAll, describe, expect, test } from 'bun:test'

import { inlineScript, outputPathFor, pageHtml, render, titleOf } from './render.ts'
import { LANGUAGE_FILE } from './check.ts'

let dir: string

beforeAll(async () => {
  dir = await mkdtemp(join(tmpdir(), 'metalang-render-'))
})

afterAll(async () => {
  await rm(dir, { recursive: true, force: true })
})

describe('the page a document is written into', () => {
  test('is one self-contained file with the stylesheet and the script inside it', () => {
    const html = pageHtml('A title', '.a{color:red}', 'var x=1')
    expect(html.startsWith('<!doctype html>')).toBe(true)
    expect(html).toContain('<title>A title</title>')
    expect(html).toContain('<style>.a{color:red}</style>')
    expect(html).toContain('<script>var x=1</script>')
    expect(html).toContain('<main id="root"></main>')
    expect(html).toContain('<meta charset="utf-8">')
  })

  test('a title carrying markup cannot close the title element', () => {
    const html = pageHtml('</title><script>bad()</script>', '', '')
    // Escaping `<` is what makes it safe: the parser ends a title at `</title`, and there is
    // no longer one in the text. A bare `>` in element text needs no escaping.
    expect(html).toContain('<title>&lt;/title>&lt;script>bad()&lt;/script></title>')
    expect(html).not.toContain('<title></title>')
  })

  test('an ampersand in a title is written as one', () => {
    expect(pageHtml('this & that', '', '')).toContain('this &amp; that')
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
    const html = pageHtml('T', '', 'var a = "</script>"')
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
})
