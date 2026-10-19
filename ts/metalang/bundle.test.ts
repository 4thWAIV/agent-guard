// The bundler.
//
// The workflow half of this is the part worth testing hard, because the format it produces is
// not valid JavaScript and the only thing standing between a wrong build and a script that
// fails when the pipeline next runs is the check on the output.

import { mkdtemp, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

import { afterAll, beforeAll, describe, expect, test } from 'bun:test'

import {
  bundlePage,
  bundleWorkflowScript,
  workflowBanner,
  workflowOutputFaults,
  WORKFLOW_FOOTER,
} from './bundle.ts'

let dir: string

beforeAll(async () => {
  dir = await mkdtemp(join(tmpdir(), 'metalang-bundle-'))
})

afterAll(async () => {
  await rm(dir, { recursive: true, force: true })
})

const write = async (name: string, body: string): Promise<string> => {
  const path = join(dir, name)
  await writeFile(path, body, 'utf8')
  return path
}

describe('the banner that carries meta', () => {
  test('puts the meta literal first and declares the identifier the footer returns', () => {
    const banner = workflowBanner({ name: 'ground' })
    expect(banner.startsWith('export const meta = {')).toBe(true)
    expect(banner).toContain('"name": "ground"')
    expect(banner).toContain('let __result')
  })

  test('writes the literal across lines, because the runtime reads it as written', () => {
    expect(workflowBanner({ name: 'g', phases: [{ title: 'Explore' }] })).toContain('\n  "phases"')
  })

  test('the footer is the top-level return', () => {
    expect(WORKFLOW_FOOTER.trim()).toBe('return __result')
  })
})

describe('checking built workflow output', () => {
  const good = 'export const meta = {}\n\nlet __result\nvar x = 1\n__result = { x }\n\nreturn __result'

  test('says nothing about output in the shape the runtime wants', () => {
    expect(workflowOutputFaults(good)).toEqual([])
  })

  test('catches output that does not begin with the meta export', () => {
    const faults = workflowOutputFaults('var a = 1\nexport const meta = {}\n\nreturn __result')
    expect(faults.some((f) => f.includes('does not begin with `export const meta`'))).toBe(true)
  })

  test('catches an export other than meta, and says which line it is on', () => {
    const faults = workflowOutputFaults(
      'export const meta = {}\nlet __result\nvar x = 1\nexport { x };\nreturn __result',
    )
    expect(faults.some((f) => f.includes('exports something other than meta, on line 4'))).toBe(true)
  })

  test('does not mistake the meta export itself for an unexpected one', () => {
    expect(workflowOutputFaults(good).some((f) => f.includes('exports something other'))).toBe(false)
  })

  test('does not mistake a word starting with meta for the meta export', () => {
    const faults = workflowOutputFaults('export const meta = {}\nexport const metadata = 1\nreturn __result')
    expect(faults.some((f) => f.includes('exports something other than meta'))).toBe(true)
  })

  test('catches output that does not end with the return', () => {
    const faults = workflowOutputFaults('export const meta = {}\nlet __result\n__result = 1\n')
    expect(faults.some((f) => f.includes('does not end with the top-level return'))).toBe(true)
  })

  test('reports every fault at once rather than the first', () => {
    expect(workflowOutputFaults('var x = 1\nexport { x };\n')).toHaveLength(3)
  })
})

describe('building a workflow script', () => {
  test('inlines a shared module once and produces the runtime shape', async () => {
    await write(
      'shared.ts',
      'export async function input(a: { caller: string }): Promise<number> {\n' +
        '  return a.caller.length\n' +
        '}\n',
    )
    const entry = await write(
      'ground.ts',
      "import { input } from './shared.ts'\n" +
        "declare const log: (m: string) => void\n" +
        "declare global { var __result: unknown }\n" +
        "const n = await input({ caller: 'ground' })\n" +
        'log(`n=${n}`)\n' +
        '__result = { n }\n',
    )

    const { code } = await bundleWorkflowScript(entry, { name: 'ground', phases: [] })

    expect(code.startsWith('export const meta = {')).toBe(true)
    expect(code.trimEnd().endsWith('return __result')).toBe(true)
    expect(code).toContain('async function input(')
    // The shared module is inlined once, not twice.
    expect(code.split('async function input(')).toHaveLength(2)
    // Top-level await survives rather than being wrapped in a function.
    expect(code).toContain('await input(')
  })

  test('refuses a body that exports something, because the runtime rejects it', async () => {
    const entry = await write('exports.ts', 'export const x = 1\n__result = { x }\n')
    expect(bundleWorkflowScript(entry, { name: 'x' })).rejects.toThrow('built wrongly')
  })

  test('refuses a body that never assigns the result', async () => {
    // A plain const is dropped by the bundler because nothing reads it, so the footer returns
    // an identifier that was never set. The body must assign __result.
    const entry = await write('noassign.ts', 'const result = { a: 1 }\n')
    const { code } = await bundleWorkflowScript(entry, { name: 'x' })
    expect(code).not.toContain('a: 1')
  })

  test('fails loudly when the entry point does not exist', () => {
    expect(bundleWorkflowScript(join(dir, 'missing.ts'), { name: 'x' })).rejects.toThrow(
      'did not build',
    )
  })
})

describe('building the browser page', () => {
  test('replaces the injected names at build time', async () => {
    const entry = await write(
      'page.ts',
      'declare const __WORDS__: { kinds: string[] }\n' +
        'globalThis.console.log(__WORDS__.kinds.join("|"))\n',
    )
    const { code } = await bundlePage(entry, { __WORDS__: JSON.stringify({ kinds: ['entity'] }) })
    expect(code).toContain('entity')
    expect(code).not.toContain('__WORDS__')
  })

  test('fails loudly when the entry point does not exist', () => {
    expect(bundlePage(join(dir, 'missing.ts'), {})).rejects.toThrow('did not build')
  })
})
