// The proposal renderer, where it draws a closed set.
//
// A vocabulary takes its words from a base type, and that base type is nearly always declared in
// the language rather than in the document being drawn. These two hold the line that every word
// reaches its mark across that boundary, and that a word owing a mark and holding none is drawn as
// the gap it is rather than quietly left whole.

import { unlink, writeFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import { afterEach, describe, expect, test } from 'bun:test'

import { pageFor } from './propose.ts'

const here = dirname(fileURLToPath(import.meta.url))
const REPO = join(here, '..', '..')
const written: string[] = []

/** One fixture document, written beside the language so its imports resolve as any document's do. */
async function put(name: string, text: string): Promise<string> {
  const path = join(here, `${name}.yaml`)
  await writeFile(path, text, 'utf8')
  written.push(path)
  return `ts/metalang/${name}.yaml`
}

afterEach(async () => {
  for (const path of written.splice(0)) await unlink(path)
})

const linesDocument = (marks: readonly (string | null)[]): string => `scope: [propose_fixture_lines]
import:
  - [arch]
title: A set whose base type is declared somewhere else
description: Written to be drawn, not to be read.
declare:
  - entity:
      id: fixtureword
      is: word
      title: One word of the set.
      description: It takes its four properties from the language rather than declaring them.

  - enum:
      id: fixturewords
      is: fixtureword
      title: Every word of the set.
      values:
${marks
  .map(
    (mark, i) => `        - id: word${i}
          value:
${mark === null ? '' : `            mark: "${mark}"\n`}            example: pending
            name: Word ${i}
            description: One word of a set declared beside the language.`,
  )
  .join('\n')}
`

const proposalDocument = (lines: string): string => `scope: [propose_fixture]
language: propose
import:
  - [arch]
title: A proposal that draws one set
description: Written to be drawn, not to be read.
declare:
  - vocabulary:
      id: the-set
      says: The set, drawn whole.
      file: ${lines}
      declaration: fixturewords
`

const marksIn = (html: string): string[] =>
  [...html.matchAll(/<span class="tile-mark[^"]*"[^>]*>([^<]*)<\/span>/g)].map((found) => found[1] ?? '')

describe('a closed set whose base type is declared in another document', () => {
  test('draws every word with the mark that word holds', async () => {
    const lines = await put('propose_fixture_lines', linesDocument(['◎', '◑']))
    const proposal = await put('propose_fixture', proposalDocument(lines))
    expect(marksIn(pageFor(proposal))).toEqual(['◎', '◑'])
  })

  test('draws the gap where a word owes a mark and holds none', async () => {
    const lines = await put('propose_fixture_lines', linesDocument(['◎', null]))
    const proposal = await put('propose_fixture', proposalDocument(lines))
    const html = pageFor(proposal)
    expect(marksIn(html)).toEqual(['◎', '?'])
    expect(html).toContain('tile-mark missing')
  })
})


const decisionDocument = (lines: string, shows: string): string => `scope: [propose_fixture]
language: propose
import:
  - [arch]
title: A proposal with one decision
description: Written to be drawn, not to be read.
declare:
  - decision:
      id: a-decision
      says: Does it gain the word?
      word: fixtureword
      parts: none
      means: What the word means, in one line.
      used:
        - id: the-word-in-use
          says: The word written down.
          file: ${lines}
          declaration: fixturewords
${shows}
      changes: none
      result: What you would read.
      refuses: none
      pick: Yes.
      instead: No.
      rejected: Nothing changes.
      costs: none
      state: proposed
`

describe('the word in use, drawn inside the decision that adds it', () => {
  test('marks the part the example says is the word, with a rule written beside it', async () => {
    const lines = await put('propose_fixture_lines', linesDocument(['◎']))
    const proposal = await put('propose_fixture', decisionDocument(lines, '          shows: [mark]'))
    const html = pageFor(proposal)
    expect(html).toContain('<div class="inuse" data-shows="mark">')
    expect(html).toContain('.inuse[data-shows~="mark"] .d-part[data-part="mark"]')
    expect(html).toContain('.inuse[data-shows~="mark"] .d-table td[data-col="mark"]')
  })

  test('refuses an example that says nothing about which part is the word', async () => {
    const lines = await put('propose_fixture_lines', linesDocument(['◎']))
    const proposal = await put('propose_fixture', decisionDocument(lines, '          shows: []'))
    expect(() => pageFor(proposal)).toThrow('says nothing about which part is the word')
  })
})

const readingDocument = (lines: string, named: string): string => `scope: [propose_fixture]
language: propose
import:
  - [arch]
title: A proposal that draws one declaration
description: Written to be drawn, not to be read.
declare:
  - reading:
      id: the-declaration
      says: The declaration, as it reads.
      file: ${lines}
      declaration: ${named}
`

const externalDocument = (): string => `scope: [propose_fixture_lines]
import:
  - [metalang]
title: A document that leaves one definition's implementation out
description: Written to be drawn, not to be read.
declare:
  - external:
      cast:
        id: fixture_cast
        from: uuid
        to: string
        title: A uuid as the text it is written in.
`

describe('a declaration asked for by name', () => {
  test('draws one written inside another, where the name sits a level further in', async () => {
    const lines = await put('propose_fixture_lines', externalDocument())
    const proposal = await put('propose_fixture', readingDocument(lines, 'fixture_cast'))
    const html = pageFor(proposal)
    expect(html).toContain('fixture_cast')
    expect(html).toContain('A uuid as the text it is written in.')
  })

  test('refuses a name nothing in the document declares, rather than drawing an empty document', async () => {
    const lines = await put('propose_fixture_lines', externalDocument())
    const proposal = await put('propose_fixture', readingDocument(lines, 'no_such_name'))
    expect(() => pageFor(proposal)).toThrow('nothing in it is declared "no_such_name"')
  })
})

test('the repository the renderer reads from is the one these fixtures are written into', () => {
  expect(join(REPO, 'ts', 'metalang')).toBe(here)
})
