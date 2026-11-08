// The kit's command.
//
// The parsing is where a consumer meets the kit, so a wrong word has to come back as a line
// they can act on rather than a stack trace. Every refusal is tested for what it says.

import { describe, expect, test } from 'bun:test'

import { join } from 'node:path'

import { main, parse, USAGE, whyRenderCannotRun } from './cli.ts'
import { LANGUAGE_FILE } from './rulebook.ts'

describe('parsing the command', () => {
  test('reads check and the file it is given', () => {
    expect(parse(['check', 'guard.yaml'])).toEqual({ name: 'check', file: 'guard.yaml' })
  })

  test('reads render, the file, and where the page goes', () => {
    expect(parse(['render', 'guard.yaml', 'guard.html'])).toEqual({
      name: 'render',
      file: 'guard.yaml',
      out: 'guard.html',
    })
  })

  test('render with no output names the page after the document', () => {
    const read = parse(['render', 'guard.yaml'])
    expect(read).toEqual({ name: 'render', file: 'guard.yaml', out: 'guard.html' })
  })

  test('names the word it did not know, and does not guess at it', () => {
    const read = parse(['renders', 'guard.yaml'])
    expect(read).toEqual({ why: 'there is no command called renders' })
  })

  test('says a command is needed when none is given', () => {
    expect(parse([])).toEqual({ why: 'no command given' })
  })

  test('says which command is missing its file', () => {
    expect(parse(['check'])).toEqual({ why: 'check needs a file to check' })
    expect(parse(['render'])).toEqual({ why: 'render needs a file to draw' })
  })

  test('refuses a file that is not a document, rather than reading it as one', () => {
    expect(parse(['check', 'guard.txt'])).toEqual({
      why: 'guard.txt is not a .yaml or .yml file',
    })
  })

  test('the usage line names both commands', () => {
    expect(USAGE).toContain('check')
    expect(USAGE).toContain('render')
  })
})

describe('what render needs that check does not', () => {
  test('a bundle already built asks nothing of the runtime', () => {
    expect(
      whyRenderCannotRun({ hasPageBundle: true, hasBun: false, hasPageSource: false }),
    ).toBeNull()
  })

  test('the page source and a bundler is the other way through', () => {
    expect(
      whyRenderCannotRun({ hasPageBundle: false, hasBun: true, hasPageSource: true }),
    ).toBeNull()
  })

  test('names bun when a bundle would have to be built and there is none', () => {
    const why = whyRenderCannotRun({ hasPageBundle: false, hasBun: false, hasPageSource: true })
    expect(why).not.toBeNull()
    expect(why).toContain('bun')
    expect(why).toContain('check')
  })

  test('names both routes when neither is open', () => {
    const why = whyRenderCannotRun({ hasPageBundle: false, hasBun: true, hasPageSource: false })
    expect(why).not.toBeNull()
    expect(why).toContain('page.js')
    expect(why).toContain('client.tsx')
  })
})

describe('running one command', () => {
  test('checks the language itself and reports nothing wrong', async () => {
    expect(await main(['check', LANGUAGE_FILE])).toBe(0)
  })

  test('a document with something wrong in it comes back as 1, not as a throw', async () => {
    const path = join(import.meta.dir, 'cli-wrong.test.yaml')
    await Bun.write(path, 'scope: [x]\ntitle: t\ndeclare:\n  - entity: {}\n')
    try {
      expect(await main(['check', path])).toBe(1)
    } finally {
      await Bun.file(path).delete()
    }
  })

  test('a file that is not there is reported, rather than throwing out of the command', async () => {
    expect(await main(['check', join(import.meta.dir, 'not-a-file.yaml')])).toBe(1)
  })

  test('an unknown word is refused with 2', async () => {
    expect(await main(['renders', 'x.yaml'])).toBe(2)
  })

  test('no command at all is refused with 2', async () => {
    expect(await main([])).toBe(2)
  })

  test('draws a page, because bun is the runtime here', async () => {
    const out = join(import.meta.dir, 'cli-drawn.test.html')
    try {
      expect(await main(['render', LANGUAGE_FILE, out])).toBe(0)
      expect((await Bun.file(out).text()).startsWith('<!doctype html>')).toBe(true)
    } finally {
      await Bun.file(out).delete()
    }
  })
})
