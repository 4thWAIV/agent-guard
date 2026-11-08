// The proposal renderer — one page from one document written in the proposal language.
//
// Not the renderer in `ts/metalang/render.ts`. That one draws a document as the declarations it
// makes, which is the right page for a language. A proposal is read to decide something, so the
// page is built around the document it is proposing: the document runs down two thirds of the
// page as one continuous read, and the third on the left is empty until a part of it is chosen,
// when it shows the lines that make that part and nothing else.
//
// So the reader is never made to fit a whole file beside a document. The lines are there when he
// asks what makes a thing, and out of the way when he is reading.
//
// What it does not do is as important. It writes no colours of its own: the YAML is coloured by
// `ts/metalang/syntax.ts`, against the vocabulary each block belongs to. The page reads both
// stylesheets straight out of `ts/metalang` rather than copying them. The document views are
// drawn by `ts/metalang/app.tsx`, the page control itself, into the same flow, so what a proposal
// promises a document will look like is what the renderer really makes of those same lines.
//
// Nothing is dropped quietly. The file is cut into regions and the regions are checked to add
// back up to the file, so no line can fall between two of them, and the lines of each region are
// counted again after they are coloured and laid out. What the checker found is drawn on the
// page. A declaration of a kind this renderer has no drawing for says so where it sits.
//
// It prints nothing standing. No line saying what a proposal is, no table of what the language
// lets a proposal write, no note under a drawing saying where the drawing came from, and no
// instructions in an empty panel. Tim rejected a proposal on 24 October 2026 for exactly this:
// text that is the same on every page costs a reader his attention and tells him nothing he did
// not have. Every word on the page has to come from the document.
//
// Usage:  bun run ts/metalang/propose.ts [document.yaml] [out.html]

import { readFileSync, writeFileSync } from 'node:fs'
import { basename, dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

import * as yaml from 'js-yaml'
import { createElement } from 'react'
import { renderToStaticMarkup } from 'react-dom/server'

import { idOf, kindColour, Page } from './app.tsx'
import { checkedAgainst } from './check.ts'
import { rulebookFor } from './rulebook.ts'
import { htmlOf, partsOf, type Part, type Written } from './syntax.ts'
import {
  declaredBodiesOf,
  isMapping,
  propertyCarrying,
  ROLES,
  typeOfOwnValue,
  type BodyOf,
  type Mapping,
  type Vocabulary,
} from './words.ts'

const here = dirname(fileURLToPath(import.meta.url))
const REPO = resolve(here, '..', '..')
const STYLE = join(REPO, 'ts', 'metalang', 'style.css')
const SYNTAX = join(REPO, 'ts', 'metalang', 'syntax.css')

const escape = (s: string): string =>
  s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

/** The same, for text sitting inside a quoted attribute. */
const escapeAttribute = (s: string): string => escape(s).replace(/"/g, '&quot;')

/** A value written as text, or nothing, so a missing word is drawn as missing and not as "undefined". */
const text = (held: unknown): string | null => (typeof held === 'string' ? held : null)

/** What a line says, with the word itself drawn where it is not there. */
const said = (held: unknown): string => {
  const written = text(held)
  return written === null ? '<em class="gone">not written</em>' : escape(written)
}

/** What this document is read against, and what it declares itself. */
interface Own {
  readonly words: Vocabulary
  readonly written: Written
}

// ── the file, cut into the regions a reader asks about ───────────────────────

/** One piece of the file: the header, or one declaration, with the text exactly as written. */
interface Region {
  /** The lines themselves, straight out of the file. */
  readonly source: string
  /** The kind declared here, or nothing for the header above `declare:`. */
  readonly kind: string | null
  /** What is written under that kind. */
  readonly body: Mapping | null
}

/**
 * The file cut at each declaration, in the order it is written.
 *
 * The regions are put back together and compared with the file before any of them is drawn, so a
 * line that fell between two regions stops the page instead of going missing from it.
 */
function regionsOf(file: string, declared: readonly unknown[]): readonly Region[] {
  const source = readFileSync(file, 'utf8')
  const lines = source.split('\n')
  const starts: number[] = []
  let inDeclare = false
  for (const [at, line] of lines.entries()) {
    if (/^declare:\s*$/.test(line)) {
      inDeclare = true
      continue
    }
    if (!inDeclare) continue
    // A key back at the left margin would be a part of the file after the declarations, and this
    // renderer has nowhere to put it.
    if (/^\S/.test(line)) {
      throw new Error(`${basename(file)} line ${at + 1} — this renderer reads nothing after declare:`)
    }
    if (/^ {2}- /.test(line)) starts.push(at)
  }
  if (starts.length !== declared.length) {
    throw new Error(
      `${basename(file)} — ${starts.length} declarations in the text and ${declared.length} read from it`,
    )
  }

  const cut = (from: number, to: number): string => lines.slice(from, to).join('\n')
  const texts = [
    cut(0, starts[0] ?? lines.length),
    ...starts.map((start, at) => cut(start, starts[at + 1] ?? lines.length)),
  ]
  if (texts.join('\n') !== source) {
    throw new Error(`${basename(file)} — the regions do not add back up to the file`)
  }

  return texts.map((region, at) => {
    if (at === 0) return { source: region, kind: null, body: null }
    const one = declared[at - 1]
    const written = isMapping(one) ? Object.entries(one) : []
    const [first] = written
    if (written.length !== 1 || first === undefined) {
      throw new Error(
        `${basename(file)} — declaration ${at} is written with ${written.length} words, not one`,
      )
    }
    const [kind, body] = first
    return { source: region, kind, body: isMapping(body) ? body : null }
  })
}

// ── the lines, coloured and laid out to be read in a narrow column ───────────

/**
 * The coloured text cut back into its lines.
 *
 * `htmlOf` writes one flat span per part and nothing nested, so a line ends by closing the span
 * it is in and the next line starts by opening it again. A tag that never closes stops the page.
 */
function linesOfHtml(html: string): readonly string[] {
  const out: string[] = []
  let open: string | null = null
  let line = ''
  let at = 0
  while (at < html.length) {
    const tag = html.indexOf('<', at)
    if (tag !== at) {
      const to = tag < 0 ? html.length : tag
      const pieces = html.slice(at, to).split('\n')
      for (const [piece, held] of pieces.entries()) {
        if (piece > 0) {
          out.push(line + (open === null ? '' : '</span>'))
          line = open ?? ''
        }
        line += held
      }
      at = to
      continue
    }
    const end = html.indexOf('>', at)
    if (end < 0) throw new Error('a tag in the coloured lines is never closed')
    const held = html.slice(at, end + 1)
    open = held.startsWith('</') ? null : held
    line += held
    at = end + 1
  }
  out.push(line + (open === null ? '' : '</span>'))
  return out
}

/**
 * A block of YAML, coloured against the vocabulary it belongs to and laid out to be read in a
 * column a third of the page wide.
 *
 * Each line is its own block, indented by what it is written under and hanging by two more, so a
 * line too long for the column wraps beneath itself and the shape of the file survives. The lines
 * are counted again afterwards, because a lost line is the one thing this must not do.
 */
function block(lines: string, words: Vocabulary, written: Written): string {
  const parts: readonly Part[] = partsOf(lines, words, written)
  const coloured = linesOfHtml(htmlOf(parts, words.kinds))
  const source = lines.split('\n')
  if (coloured.length !== source.length) {
    throw new Error(`${source.length} lines went in and ${coloured.length} came out`)
  }
  const laid = coloured
    .map((line, at) => {
      const indent = /^ */.exec(source[at] ?? '')?.[0].length ?? 0
      const body = line.replace(/^((?:<span[^>]*>)?) */, '$1')
      if ((source[at] ?? '').trim() === '') return '<span class="l gap"></span>'
      return `<span class="l" style="--in:${indent}ch">${body}</span>`
    })
    .join('')
  const lost = parts.filter((p) => p.is === 'unknown').length
  const note =
    lost === 0
      ? ''
      : `<p class="lost">${lost} word${lost === 1 ? '' : 's'} here the vocabulary does not account for, drawn in red.</p>`
  return `<pre class="y lines">${laid}</pre>${note}`
}

// ── the drawn view: the same lines, through the page control itself ──────────

/** A drawn page, split from the keys it carries so a key is drawn once instead of once per page. */
interface Drawn {
  readonly sheet: string
  readonly keys: readonly string[]
}

/**
 * The lines of one block, drawn as a page.
 *
 * `against` names a real file whose header these lines are read with. The lines go into that
 * file's own header, are read against that file's rulebook, and are drawn by the control that
 * draws every other document, so the view is what the renderer really makes of them rather than a
 * picture of one.
 */
function drawn(body: Mapping): Drawn {
  const from = text(body.against)
  const lines = text(body.yaml)
  if (lines === null) {
    return { sheet: '<p class="gone">no lines are written here, so there is nothing to draw.</p>', keys: [] }
  }
  if (from === null) {
    return {
      sheet: `<p class="gone">no file is named under <code>against</code>, so these lines belong to
        no document yet and there is no page to draw them on.</p>`,
      keys: [],
    }
  }
  const items = yaml.load(lines)
  if (!Array.isArray(items)) throw new Error(`${from} — the lines under yaml are not declarations`)
  return pageOf(from, items)
}

/**
 * One document's own header, with these declarations in place of the ones it makes, drawn as a
 * page. Everything a declaration holds is drawn, because the control draws the whole of it, so a
 * document that says more than this renderer knows about still draws all of it.
 */
function pageOf(from: string, items: readonly unknown[], only?: string): Drawn {
  const path = join(REPO, from)
  const host = yaml.load(readFileSync(path, 'utf8'))
  if (!isMapping(host)) throw new Error(`${from} — this is not a document`)

  // A name nothing in the document declares would narrow the page to nothing and draw an empty
  // document, which reads exactly like a declaration that says nothing. It is refused here instead.
  if (only !== undefined && !items.some((one) => idOf(one) === only)) {
    throw new Error(`${from} — nothing in it is declared "${only}"`)
  }

  const doc = { ...host, declare: items }
  const rulebook = rulebookFor(doc, path)
  if (rulebook.words === null) {
    throw new Error(`${from} — ${rulebook.faults.map((f) => f.what).join('; ')}`)
  }
  const found = checkedAgainst(doc, path, rulebook)
  const markup = renderToStaticMarkup(
    createElement(Page, { words: rulebook.words, doc, found, ...(only === undefined ? {} : { only }) }),
  )

  // The control draws a whole page, so it ends in the key telling a reader what the marks mean.
  // Several pages on one page would carry it several times, and it is the same key, so it comes
  // out here and is drawn once at the end.
  if (!markup.startsWith('<main>') || !markup.endsWith('</main>')) {
    throw new Error(`${from} — the drawn page is not the shape this renderer takes apart`)
  }
  const foot = markup.lastIndexOf('<div class="d-foot">')
  if (foot < 0) throw new Error(`${from} — the drawn page carries no key to lift out`)
  const inner = markup.slice('<main>'.length, markup.length - '</main>'.length)
  const to = foot - '<main>'.length
  return { sheet: `<div class="sheet">${inner.slice(0, to)}</div>`, keys: [inner.slice(to)] }
}

// ── one drawing for each kind of line ────────────────────────────────────────

/** What one declaration comes to in the document, and any keys the pages it drew carried. */
type Shows = (body: Mapping) => Drawn

const plain = (html: string): Drawn => ({ sheet: html, keys: [] })

/** One document, read whole. */
function documentAt(file: string): Mapping {
  const doc = yaml.load(readFileSync(join(REPO, file), 'utf8'))
  if (!isMapping(doc)) throw new Error(`${file} — this is not a document`)
  return doc
}

/** Every declaration a document makes, whatever kind each one is written as. */
const bodiesIn = (doc: Mapping): readonly Mapping[] =>
  (Array.isArray(doc.declare) ? doc.declare : [])
    .filter(isMapping)
    .flatMap((one) => Object.values(one).filter(isMapping))

/**
 * Every declaration a document can reach: the ones it writes, the ones the language it names
 * writes, and the ones every scope it brings in writes, deepest first so the nearest one wins.
 *
 * A vocabulary takes its words from a base type, and that base type is nearly always declared in
 * the language rather than in the document being drawn. A lookup that stopped at the document's
 * own declarations found nothing, and every word of the set was then drawn with its mark missing
 * and nothing said about it.
 *
 * The documents come from the rulebook, which reads every scope a document reaches to build the
 * vocabulary and now hands back what it read. Walking the imports again here was the same walk
 * written twice.
 */
function bodiesReachedBy(file: string, doc: Mapping): ReadonlyMap<string, Mapping> {
  const bodies = new Map<string, Mapping>()
  for (const one of rulebookFor(doc, join(REPO, file)).documents) {
    if (!isMapping(one)) continue
    for (const [id, body] of declaredBodiesOf(one)) if (!bodies.has(id)) bodies.set(id, body)
  }
  // The document's own declarations go on last, so one it writes itself wins over the same name
  // brought in from somewhere else.
  for (const [id, body] of declaredBodiesOf(doc)) bodies.set(id, body)
  return bodies
}

/** One word a closed set lists: the entry as written, and what its id stands for. */
interface Word {
  readonly id: string
  readonly entry: Mapping
  readonly value: Mapping
}

/**
 * One closed set, read whole: every word it lists, and what a word of it is shown by. The name is
 * asked of the vocabulary rather than read off a property this renderer names, so a language that
 * spells its words differently draws the same way and no field name of a language is in this code.
 */
interface Vocab {
  readonly words: readonly Word[]
  /** What a word of this set is shown by, falling back to the id rather than to a guess. */
  readonly named: (id: string, word: Mapping | undefined) => string
  /**
   * Whether the words of this set are drawn with a mark at all. A set whose words carry none draws
   * as it drew before the language had the word; a set whose words carry one and whose word does
   * not hold it is a gap, and a gap is drawn rather than left out.
   */
  readonly marks: boolean
  /** The character a word of this set is drawn with, where this set gives its words one. */
  readonly marked: (word: Mapping | undefined) => string | null
}

/**
 * Every word one closed set lists, in the order it is written. A set whose entries this renderer
 * cannot read comes back as a fault rather than as a shorter set, because a vocabulary drawn from
 * half a list would read as the whole of one.
 */
function entriesOf(file: string, id: string): Vocab {
  const doc = documentAt(file)
  const found = bodiesIn(doc).find((body) => text(body.id) === id)
  if (found === undefined) throw new Error(`${file} declares nothing called "${id}"`)
  const listed = Array.isArray(found.values) ? found.values : []
  const words = listed.filter(isMapping).flatMap((entry): Word[] => {
    const name = text(entry.id)
    if (name === null) return []
    return [{ id: name, entry, value: isMapping(entry.value) ? entry.value : {} }]
  })
  if (words.length !== listed.length || words.length === 0) {
    throw new Error(`${file} — "${id}" lists ${listed.length} words and this renderer read ${words.length}`)
  }
  const held = heldIn(file, doc, found)
  const name = held(ROLES.name)
  const mark = held(ROLES.mark)
  return {
    words,
    marks: mark.carried,
    named: (id, word) => (word === undefined ? id : (name.of(word) ?? id)),
    marked: (word) => (word === undefined ? null : mark.of(word)),
  }
}

/**
 * What one word of a closed set holds for a role, read through the property the type of its values
 * says carries that role. The vocabulary is asked which property that is — the question `role`
 * exists to answer — so a set whose words are spelled differently reads the same way and no field
 * name of a language is in this code.
 */
function heldIn(
  file: string,
  doc: Mapping,
  set: Mapping,
): (role: string) => { carried: boolean; of: (word: Mapping) => string | null } {
  const bodies = bodiesReachedBy(file, doc)
  const bodyOf: BodyOf = (named) => bodies.get(named)
  const of = typeOfOwnValue(set)
  return (role) => {
    const part =
      typeof of === 'string' && of.length > 0 ? propertyCarrying(bodyOf, of, role) : undefined
    return {
      carried: part !== undefined,
      of: (word) => (part === undefined ? null : text(word[part])),
    }
  }
}

/** The document and declaration a drawing is read out of, or why it cannot be. */
function readsFrom(body: Mapping): { file: string; id: string } | string {
  const file = text(body.file)
  const id = text(body.declaration)
  if (file === null || id === null) return 'no document and declaration are named here.'
  return { file, id }
}

/**
 * One declaration as it would read, drawn out of the real document by the control that draws that
 * document's own page. Nothing is chosen here, so everything the declaration holds is on the card
 * and a reader sees what the page would really make of it.
 */
const showReading: Shows = (body) => {
  const from = readsFrom(body)
  if (typeof from === 'string') return plain(`<p class="gone">${from}</p>`)
  // The whole document goes in, so a name one declaration writes still reaches the others and the
  // checker reads the document that really exists. Only the one asked for is drawn.
  const whole = Array.isArray(documentAt(from.file).declare) ? documentAt(from.file).declare : []
  const page = pageOf(from.file, whole as readonly unknown[], from.id)
  return { sheet: `<p class="plain says-line">${said(body.says)}</p>${page.sheet}`, keys: page.keys }
}

/** A closed set, drawn whole: every word it lists, the mark it is drawn with, and a real one. */
/**
 * The mark chip on a tile. A set whose words carry no mark draws without one, as it did before the
 * language had the word. A set whose words do carry one and whose word holds nothing draws the gap
 * where the mark would be, because a tile missing a piece of its word silently reads as a whole
 * one.
 */
const markOn = (set: Vocab, mark: string | null): string => {
  if (mark !== null) return `<span class="tile-mark">${escape(mark)}</span>`
  if (!set.marks) return ''
  return '<span class="tile-mark missing" title="this word is drawn with a mark and holds none">?</span>'
}

const showVocabulary: Shows = (body) => {
  const from = readsFrom(body)
  if (typeof from === 'string') return plain(`<p class="gone">${from}</p>`)
  const set = entriesOf(from.file, from.id)
  const ids = set.words.map((one) => one.id)
  const tiles = set.words
    .map((word) => {
      const colour = kindColour(word.id, ids)
      const mark = set.marked(word.value)
      const example = text(word.value.example)
      const line = text(word.entry.title) ?? text(word.value.description) ?? text(word.entry.description)
      if (line === null) {
        throw new Error(`${from.file} — "${from.id}" says nothing about "${word.id}"`)
      }
      return `<div class="tile"${colour === null ? '' : ` style="--kind:${colour}"`}>
        ${markOn(set, mark)}
        <b class="tile-name">${escape(set.named(word.id, word.value))}</b>
        ${example === null ? '' : `<code class="tile-eg">${escape(example)}</code>`}
        <p class="tile-says">${escape(line)}</p>
      </div>`
    })
    .join('')
  return plain(`<p class="plain">${said(body.says)}</p><div class="vocab">${tiles}</div>`)
}

/** One thing wrong with what a reader has today. */
const showProblem: Shows = (body) => plain(`<p class="problem">${said(body.says)}</p>`)

/** One labelled part of a decision, written once however it is filled. */
const under = (label: string, inside: string): string =>
  `<p class="dec-label">${escape(label)}</p>${inside}`

/** A part of a decision written as one run of text, with a decided absence read as one. */
const asText = (held: unknown, how: string): string =>
  text(held) === 'none'
    ? '<p class="dec-none">none</p>'
    : `<p class="${how}">${said(held)}</p>`

/**
 * One decision, with every part a reader needs to answer it.
 *
 * Nothing here is folded away and nothing is summarised: the word is drawn as the word, the lines
 * are drawn as the document they would make, and what the checker says is quoted from the checker.
 * A part that does not apply reads `none`, which the language made it say out loud.
 */
const showDecision: Shows = (body) => {
  const keys: string[] = []

  const changes =
    text(body.changes) === 'none'
      ? '<p class="dec-none">none — it changes no lines.</p>'
      : (Array.isArray(body.changes) ? body.changes : [])
          .filter(isMapping)
          .map((block) => {
            const page = drawn(block)
            keys.push(...page.keys)
            return `<p class="where"><code>${said(block.file)}</code></p>
              <p class="does">${said(block.says)}</p>${page.sheet}`
          })
          .join('')

  const refuses =
    text(body.refuses) === 'none'
      ? '<p class="dec-none">none — it refuses nothing it did not refuse before.</p>'
      : (Array.isArray(body.refuses) ? body.refuses : [])
          .filter(isMapping)
          .map(
            (one) => `<div class="pair"><pre class="given">${said(one.given)}</pre>
              <p class="checker">${said(one.says)}</p></div>`,
          )
          .join('')

  // The word in use, drawn inside the block about that word. A definition and an example on two
  // different pages make a reader hold one against the other himself, which is what this part
  // exists to stop.
  const used =
    text(body.used) === 'none'
      ? '<p class="dec-none">none — there is nothing to draw it in.</p>'
      : (Array.isArray(body.used) ? body.used : [])
          .filter(isMapping)
          .map((one) => {
            const page = showReading(one)
            keys.push(...page.keys)
            const shows = (Array.isArray(one.shows) ? one.shows : [])
              .filter((part): part is string => typeof part === 'string' && part.length > 0)
            if (shows.length === 0) {
              throw new Error(`${said(one.id)} — an example says nothing about which part is the word`)
            }
            // The rule naming the part is written beside the drawing it marks, generated from the
            // same list, so a drawing and the thing picked out of it cannot come apart.
            const rules = shows
              .map(
                (part) =>
                  `.inuse[data-shows~="${part}"] .d-part[data-part="${part}"],` +
                  `.inuse[data-shows~="${part}"] .d-table th[data-col="${part}"],` +
                  `.inuse[data-shows~="${part}"] .d-table td[data-col="${part}"]{background:var(--warm-soft);` +
                  'box-shadow:inset 0 0 0 1px var(--warm-line);border-radius:7px}',
              )
              .join('')
            return `<div class="inuse" data-shows="${escape(shows.join(' '))}"><style>${rules}</style>${page.sheet}</div>`
          })
          .join('')

  const parts =
    text(body.parts) === 'none'
      ? ''
      : `<div class="senses">${(Array.isArray(body.parts) ? body.parts : [])
          .filter(isMapping)
          .map(
            (one) => `<div class="sense"><code class="sense-word">${said(one.word)}</code>
              <p class="sense-says">${said(one.says)}</p></div>`,
          )
          .join('')}</div>`

  return {
    sheet: `<section class="dec">
      <p class="dec-name">${said(body.id)}
        <span class="state">${said(body.state)}</span></p>
      <h3 class="dec-says">${said(body.says)}</h3>
      ${under('The word', `<p class="dec-word"><code>${said(body.word)}</code></p>${parts}`)}
      ${under('What it means', asText(body.means, 'dec-text'))}
      ${under('The word in use', used)}
      ${under('The lines', changes)}
      ${under('What you would read', asText(body.result, 'dec-result'))}
      ${under('What the checker refuses', refuses)}
      ${under('Recommended', asText(body.pick, 'pick'))}
      ${under('Instead', asText(body.instead, 'dec-text'))}
      ${under('If it is not taken', asText(body.rejected, 'dec-text'))}
      ${under('It costs', asText(body.costs, 'dec-text'))}
    </section>`,
    keys,
  }
}

/**
 * The three stages a proposal is read in, in order. Each is drawn once, where the first thing
 * belonging to it sits, and says what it holds and what the reader does with it, because a band
 * naming a stage and nothing else leaves a reader to work out why it is there.
 */
interface Step {
  readonly at: number
  readonly head: string
  readonly why: string
}

const STEPS = {
  failure: {
    at: 1,
    head: 'The failure now',
    why: 'What the page draws today, and why the architecture cannot be read off it. Nothing to answer here.',
  },
  intended: {
    at: 2,
    head: 'What it should read',
    why:
      'The same part of the guard, drawn as it would be, with the closed sets its marks come from.' +
      ' Read it and say whether it is what you want. Nothing to answer here either.',
  },
  decide: {
    at: 3,
    head: 'What you decide',
    why:
      'One block for each thing waiting on you. Each carries the word it adds, what that word means,' +
      ' the exact lines, and what you would read once it is in. Answer each one by the name at its head.',
  },
} as const satisfies Readonly<Record<string, Step>>

interface Kind {
  /** The stage of the read this kind belongs to. */
  readonly step: Step
  readonly show: Shows
}

const KINDS: Readonly<Record<string, Kind>> = {
  problem: { step: STEPS.failure, show: showProblem },
  reading: { step: STEPS.intended, show: showReading },
  vocabulary: { step: STEPS.intended, show: showVocabulary },
  decision: { step: STEPS.decide, show: showDecision },
}

// ── the page ─────────────────────────────────────────────────────────────────

/** What the checker found, drawn where it cannot be missed. */
function found(doc: unknown, file: string): string {
  const rulebook = rulebookFor(doc, file)
  const result = checkedAgainst(doc, file, rulebook)
  const faults = [
    ...rulebook.faults.map((f) => `${f.where} ${f.what}`),
    ...result.blockers.map((f) => `${f.where} ${f.what}`),
    ...result.problems.map((f) => `${f.where} ${f.what}`),
    ...result.pending.map((f) => `${f.where} ${f.what}`),
  ]
  if (faults.length === 0) return ''
  return `<div class="wrong"><p><b>${faults.length} the checker found in this proposal</b></p><ul>${faults
    .map((f) => `<li>${escape(f)}</li>`)
    .join('')}</ul></div>`
}

/** The page's own look. Every element selector is left to the stylesheet this page reads in. */
const PAGE = `
  .split { display:grid; grid-template-columns: minmax(0,1fr) minmax(0,2fr); gap:0 32px;
           align-items:start; max-width:1460px; margin:0 auto; padding:26px 24px 90px; }
  @media (max-width: 980px) { .split { grid-template-columns:1fr; } .look { position:static; } }

  /* The lines, on the left, for whatever is chosen on the right and nothing else. */
  .look { position:sticky; top:22px; max-height:calc(100vh - 44px); overflow:auto; }
  .look-head { font:600 11px var(--mono); letter-spacing:.09em; text-transform:uppercase;
               color:var(--dim); margin:0 0 8px; }
  pre.y.lines { white-space:normal; margin:0; }
  .l { display:block; white-space:pre-wrap; padding-left:calc(var(--in, 0ch) + 2ch);
       text-indent:-2ch; }
  .gap { height:.55em; }

  /* The document, on the right, read straight down. */
  .doc { min-width:0; }
  .sec { font:600 11px var(--mono); letter-spacing:.1em; text-transform:uppercase; color:var(--dim);
         margin:34px 0 10px; border-bottom:1px solid var(--rule); padding-bottom:6px; }
  .sec:first-child { margin-top:0; }
  .area { border-left:3px solid transparent; border-radius:0 8px 8px 0; padding:10px 12px;
          margin:0 0 4px -15px; cursor:pointer; }
  .area:hover { border-left-color:var(--rule); background:var(--card); }
  .area.on { border-left-color:var(--accent); background:var(--card); }
  .area:focus-visible { outline:2px solid var(--accent); outline-offset:2px; }

  .standfirst { color:var(--soft); margin:0; font-size:17px; }
  .says { font-size:19px; line-height:1.45; margin:0 0 12px; }
  .pick { background:var(--accent-soft); border-radius:8px; padding:10px 12px; color:var(--ink); margin:0; }
  .stateline { margin:0 0 8px; }
  .state { font:600 11px var(--mono); letter-spacing:.08em; text-transform:uppercase;
           background:var(--warm-soft); color:var(--warm); border:1px solid var(--warm-line);
           border-radius:999px; padding:3px 10px; }
  .plain { margin:0; }
  .where { margin:0 0 4px; }
  .where code { font:600 13px var(--mono); background:var(--band); padding:2px 7px; border-radius:5px; }
  .does { margin:0 0 12px; color:var(--soft); }
  .sheet { border:1px solid var(--rule); border-radius:10px; padding:18px 20px; background:var(--paper); }
  .pair { display:grid; grid-template-columns:minmax(0,1fr) minmax(0,1fr); gap:10px; align-items:start; }
  @media (max-width: 760px) { .pair { grid-template-columns:1fr; } }
  pre.given { font:400 12.5px/1.6 var(--mono); background:var(--band); border:1px solid var(--rule);
              border-radius:8px; padding:10px 12px; margin:0; overflow-x:auto; white-space:pre-wrap; }
  .checker { background:var(--bad-soft); border:1px solid var(--bad-line); color:var(--bad);
             border-radius:8px; padding:10px 12px; font:400 12.5px/1.6 var(--mono); margin:0; }
  .lost { color:var(--bad); font-size:13px; margin:6px 0 0; }
  .gone { color:var(--bad); }
  .wrong { background:var(--bad-soft); border:1px solid var(--bad-line); border-radius:8px;
           padding:12px 14px; margin:0 0 18px; }
  .wrong ul { margin:6px 0 0; padding-left:20px; font:400 12.5px/1.7 var(--mono); }
  details.key { margin:0 0 8px; }
  details.key > summary { cursor:pointer; color:var(--soft); font-size:14px; padding:6px 0; }
  details.key > .sheet { margin-top:8px; }
  #store { display:none; }

  /* A drawn page is drawn into this page's own flow, so it gives up the margins of a page, and
     the chrome naming the file it came from, which the line above the sheet already says. */
  .sheet .d-top { display:none; }
  .sheet .d-section-name { display:none; }
  .sheet .d-foot { margin-top:0; }

  /* ── what is wrong: short statements, read one after another ───────────── */
  .says-line { margin:0 0 12px; color:var(--soft); }
  .problem { font-size:18px; line-height:1.5; margin:0; padding-left:14px;
             border-left:3px solid var(--bad-line); color:var(--ink); }

  /* ── a closed set, drawn whole ──────────────────────────────────────────── */
  .vocab { display:grid; grid-template-columns:repeat(3, minmax(0,1fr)); gap:10px;
           margin:12px 0 0; }
  @media (max-width: 900px) { .vocab { grid-template-columns:repeat(2, minmax(0,1fr)); } }
  @media (max-width: 560px) { .vocab { grid-template-columns:1fr; } }
  /* ── the three stages, drawn as a progression ──────────────────────────── */
  .step { display:grid; grid-template-columns:auto minmax(0,1fr); gap:0 16px; align-items:start;
          margin:52px 0 18px; padding-top:22px; border-top:2px solid var(--ink); }
  .step:first-child { margin-top:0; }
  .step-no { display:inline-grid; place-items:center; width:42px; height:42px; border-radius:50%;
             background:var(--ink); color:var(--paper); font:600 21px/1 var(--mono); }
  .step-head { font:600 25px/1.2 var(--sans); margin:4px 0 4px; letter-spacing:-.015em; }
  .step-why { margin:0; color:var(--soft); font-size:15.5px; line-height:1.5; max-width:70ch; }

  /* ── the parts of a word, side by side ─────────────────────────────────── */
  .senses { display:grid; grid-template-columns:repeat(3, minmax(0,1fr)); gap:10px; margin:12px 0 0; }
  @media (max-width: 820px) { .senses { grid-template-columns:1fr; } }
  .sense { background:var(--band); border:1px solid var(--rule); border-radius:10px;
           padding:12px 13px 13px; }
  .sense-word { display:inline-block; font:600 14px var(--mono); color:var(--accent);
                background:var(--accent-soft); border:1px solid var(--accent-line);
                border-radius:6px; padding:2px 8px; }
  .sense-says { margin:8px 0 0; color:var(--ink); font-size:14.5px; line-height:1.45; }

  /* ── one decision, with every part a reader needs to answer it ──────────── */
  .dec { background:var(--card); border:1px solid var(--rule); border-radius:12px;
         padding:20px 22px 18px; margin:0; }
  .dec-name { font:600 13px var(--mono); color:var(--accent); margin:0 0 6px;
              display:flex; align-items:center; gap:10px; flex-wrap:wrap; }
  .dec-says { font:600 21px/1.35 var(--sans); margin:0 0 4px; letter-spacing:-.01em; }
  .dec-label { font:600 11px var(--mono); letter-spacing:.1em; text-transform:uppercase;
               color:var(--dim); margin:20px 0 6px; border-top:1px solid var(--band);
               padding-top:12px; }
  .dec-text { margin:0; color:var(--soft); font-size:15.5px; line-height:1.5; max-width:74ch; }
  .dec-word { margin:0; }
  .dec-word code { font:600 19px var(--mono); background:var(--accent-soft);
                   border:1px solid var(--accent-line); color:var(--accent);
                   border-radius:8px; padding:4px 10px; }
  .dec-result { margin:0; font-size:16px; line-height:1.5; max-width:74ch; color:var(--ink);
                background:var(--warm-soft); border:1px solid var(--warm-line);
                border-radius:8px; padding:11px 13px; }
  .dec-none { margin:0; color:var(--dim); font-size:15px; }
  .dec .where { margin:14px 0 4px; }
  .dec .where:first-of-type { margin-top:0; }
  .dec .pair + .pair { margin-top:10px; }
  .dec .sheet { background:var(--paper); }
  .tile { background:var(--card); border:1px solid var(--rule); border-radius:10px;
          padding:12px 14px 13px; border-left:3px solid var(--kind,var(--rule)); }
  /* The word the example is there to show is picked out of the drawing it sits in, so a reader
     sees the one new line without being told which it is. The rule itself is written beside each
     drawing, from the parts that drawing names. */
  .inuse .d-part { padding:3px 7px; margin:0 -7px }
  .inuse .d-table th[data-col], .inuse .d-table td[data-col] { padding-left:7px; padding-right:7px }
  .tile-mark.missing { color:var(--bad,#b3261e); border-color:var(--bad,#b3261e); font-weight:700 }
  .tile-mark { display:inline-grid; place-items:center; min-width:26px; height:26px; padding:0 6px;
               border-radius:7px; background:var(--band); color:var(--kind,var(--ink));
               font:600 14px var(--mono); }
  .tile-name { display:block; margin:8px 0 2px; font-size:15px; color:var(--kind,var(--ink)); }
  .tile-eg { display:block; font:400 12px var(--mono); color:var(--accent); margin:0 0 6px;
             overflow-wrap:anywhere; }
  .tile-says { margin:0; color:var(--soft); font-size:13.5px; line-height:1.4; }
`

/**
 * Choosing a part of the document puts the lines that make it on the left.
 *
 * A part whose lines are not on the page says so in place of them, because an empty panel would
 * read as a part made of nothing.
 */
const CHOOSE = `
  const panel = document.getElementById('panel')
  const head = document.getElementById('panel-head')
  const store = document.getElementById('store')
  const show = (area) => {
    const held = store.querySelector('[data-for="' + area.dataset.at + '"]')
    for (const other of document.querySelectorAll('.area.on')) other.classList.remove('on')
    area.classList.add('on')
    head.textContent = area.dataset.says
    panel.innerHTML = held
      ? held.innerHTML
      : '<p class="lost">the lines for this part are not on the page.</p>'
    document.querySelector('.look').scrollTop = 0
  }
  for (const area of document.querySelectorAll('.area')) {
    area.addEventListener('click', () => show(area))
    area.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); show(area) }
    })
  }
`

/** One page from one proposal document. */
export function pageFor(file: string): string {
  const doc = yaml.load(readFileSync(file, 'utf8'))
  if (!isMapping(doc)) throw new Error(`${basename(file)} — this is not a document`)
  const rulebook = rulebookFor(doc, file)
  if (rulebook.words === null) {
    throw new Error(`${basename(file)} — ${rulebook.faults.map((f) => f.what).join('; ')}`)
  }
  const own: Own = { words: rulebook.words, written: { declared: declaredBodiesOf(doc) } }
  const declared = Array.isArray(doc.declare) ? doc.declare : []
  const regions = regionsOf(file, declared)

  const title = escape(text(doc.title) ?? basename(file))
  const keys = new Set<string>()
  const held: string[] = []
  let under: number | null = null

  const area = (at: number, says: string, inside: string): string =>
    `<div class="area" tabindex="0" data-at="${at}" data-says="${escapeAttribute(says)}">${inside}</div>`

  const parts = regions
    .map((region, at) => {
      const inside: Written = region.kind === null ? own.written : { ...own.written, inside: 'declaration' }
      held.push(
        `<div data-for="${at}">${block(region.source.replace(/\n+$/, ''), own.words, inside)}</div>`,
      )

      if (region.kind === null) {
        under = null
        return area(
          at,
          'the head of the file',
          `<h1>${title}</h1>
           <p class="standfirst">${escape(text(doc.description) ?? '')}</p>`,
        )
      }

      const kind = region.kind
      const known = KINDS[kind]
      const step = known === undefined ? null : known.step
      const sec =
        step === null
          ? `<p class="sec">${escape(kind)}</p>`
          : under === step.at
            ? ''
            : `<div class="step"><span class="step-no">${step.at}</span>
               <div class="step-of"><h2 class="step-head">${escape(step.head)}</h2>
               <p class="step-why">${escape(step.why)}</p></div></div>`
      under = step === null ? null : step.at

      // A kind this renderer has no drawing for is still the document's, so it says so where that
      // declaration sits rather than being quietly left off the page.
      if (known === undefined) {
        return `${sec}${area(
          at,
          `${kind} ${text(region.body?.id) ?? ''}`.trim(),
          `<div class="wrong"><p><b>This renderer has no section for ${escape(
            kind,
          )}</b>, so what the document says under it is not drawn here.</p></div>`,
        )}`
      }

      const shown = known.show(region.body ?? {})
      for (const one of shown.keys) keys.add(one)
      return `${sec}${area(at, `${kind} ${text(region.body?.id) ?? ''}`.trim(), shown.sheet)}`
    })
    .join('')

  // Every drawn page ends with a key saying what its marks mean. It is kept, folded shut and
  // unlabelled beyond what it is, because a reader who does not need it should not have to read
  // past it, and a reader who does needs one click.
  const folded = [...keys]
    .map((shown) => `<details class="key"><summary>what the marks mean</summary>
      <div class="sheet">${shown}</div></details>`)
    .join('')

  const band = found(doc, file)
  const css = [readFileSync(STYLE, 'utf8'), readFileSync(SYNTAX, 'utf8'), PAGE].join('\n')

  return `<!doctype html>
<html lang="en">
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>${title}</title>
<style>${css}</style>
${band === '' ? '' : `<div class="split"><div></div>${band}</div>`}
<div class="split">
  <div class="look">
    <p class="look-head" id="panel-head">The lines</p>
    <div id="panel"></div>
  </div>
  <div class="doc">
    ${parts}
    ${folded}
  </div>
</div>
<div id="store">${held.join('')}</div>
<script>${CHOOSE}</script>
</html>
`
}

if (import.meta.main) {
  const file = process.argv[2] ?? join(REPO, 'handoff', 'propose', 'kinds.yaml')
  const out = process.argv[3] ?? file.replace(/\.ya?ml$/, '.html')
  writeFileSync(out, pageFor(file))
  console.log(`${basename(out)} — drawn from ${basename(file)}`)
}
