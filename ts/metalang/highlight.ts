// What to mark in a render, and where.
//
// A highlight is its own file. It names the file it highlights by that file's path, and it says
// what is highlighted and why. Nothing in it changes a document: every document draws the same way
// with no highlight file at all, and a highlight file can be rewritten without touching what it
// points at.
//
// A path names any node at any depth. Each segment is a property name, or a position in a list
// written as EITHER its index OR the node's own name, and the two may be mixed. A declaration is
// one word naming its kind with the body inside, and that word may be written or left out, so a
// path copied from a problem the checker reported resolves as it stands.
//
// A path that names nothing is reported on the page, so a highlight left behind by an edit says so
// rather than disappearing.

import { BOOTSTRAP, isMapping, onlyWord, type Mapping } from './words.ts'

/** The key naming the file a highlight file highlights. */
export const OF = 'of'

/** The key holding what is highlighted. */
export const HIGHLIGHT = 'highlight'

/** On one entry: where it points, and what it says about what is there. */
export const PATH = 'path'
export const NOTE = 'note'

/** One thing a highlight file points at. */
export interface Entry {
  readonly path: string
  readonly note: string
}

/** What a highlight file says, once read. */
export interface Highlight {
  /** The path of the file highlighted, as the highlight file writes it. */
  readonly of: string
  readonly entries: readonly Entry[]
  /** Anything written in the file that is not an entry, said in full so it can be reported. */
  readonly wrong: readonly string[]
}

/** The marks a document carries, and the paths that reached nothing in it. */
export interface Marks {
  /** The path of every marked node, as the page writes it, with what the highlight says of it. */
  readonly marked: ReadonlyMap<string, string>
  /** Every path that named nothing, written as the highlight file wrote it. */
  readonly missing: readonly string[]
}

/** Whether a file is a highlight rather than a document: it says which file it is about. */
export const isHighlight = (file: unknown): boolean =>
  isMapping(file) && typeof file[OF] === 'string'

/** One step along a path: into a named part, or to a position in a list. */
type Step = { readonly part: string } | { readonly position: string }

/**
 * The steps a path is made of, or nothing when it is not a path. A position is written inside
 * square brackets and may hold anything but a closing bracket, because a node's own name may
 * carry spaces and punctuation.
 */
export function stepsOf(path: string): readonly Step[] | null {
  const steps: Step[] = []
  let i = 0
  while (i < path.length) {
    const here = path[i]
    if (here === '.') {
      i += 1
      continue
    }
    if (here === '[') {
      const end = path.indexOf(']', i)
      if (end === -1) return null
      steps.push({ position: path.slice(i + 1, end) })
      i = end + 1
      continue
    }
    let j = i
    while (j < path.length && path[j] !== '.' && path[j] !== '[') j += 1
    steps.push({ part: path.slice(i, j) })
    i = j
  }
  return steps.length === 0 ? null : steps
}

/**
 * The name a node is known by among its siblings: its own id, or the id inside the one word it
 * names. A declaration written inside `external` names a word that names another, so the words are
 * followed until one of them carries an id.
 */
export function nameOf(node: unknown): string | null {
  if (!isMapping(node)) return null
  const own = node[BOOTSTRAP.id]
  if (typeof own === 'string') return own
  const word = onlyWord(node)
  return word === null ? null : nameOf(word[1])
}

/** A declaration is one word naming its kind with the body inside. The body, or nothing. */
const bodyInside = (node: unknown): Mapping | null => {
  if (!isMapping(node)) return null
  const word = onlyWord(node)
  return word !== null && isMapping(word[1]) ? word[1] : null
}

/**
 * The path the page writes for the node a highlight points at, or nothing when the path names no
 * node in this document. The page and the checker write one path form, which carries the kind a
 * declaration names. A highlight may leave that kind out, so a path written either way resolves,
 * and what comes back is always the one form.
 */
export function pathTo(doc: unknown, path: string): string | null {
  const steps = stepsOf(path)
  if (steps === null) return null

  let node: unknown = doc
  let drawn = ''

  /** Steps through the one word a declaration names, which the one path form carries. */
  const through = (word: string, body: Mapping): void => {
    drawn = drawn === '' ? word : `${drawn}.${word}`
    node = body
  }

  for (const step of steps) {
    // The kind, written out.
    const word = isMapping(node) ? onlyWord(node) : null
    const inside = bodyInside(node)
    if (inside !== null && word !== null && 'part' in step && word[0] === step.part) {
      through(word[0], inside)
      continue
    }

    // The kind, left out. It is stepped through all the same, so both ways of writing a path
    // arrive at the same place and the page is asked by the one form. A declaration written
    // inside `external` names a word that names another, so this follows as many as it finds.
    const takes = (): boolean =>
      'part' in step ? isMapping(node) && node[step.part] !== undefined : Array.isArray(node)
    while (!takes()) {
      const next = isMapping(node) ? onlyWord(node) : null
      const body = bodyInside(node)
      if (next === null || body === null) break
      through(next[0], body)
    }

    if ('part' in step) {
      if (!isMapping(node) || node[step.part] === undefined) return null
      drawn = drawn === '' ? step.part : `${drawn}.${step.part}`
      node = node[step.part]
      continue
    }

    if (!Array.isArray(node)) return null
    const at = /^\d+$/.test(step.position)
      ? Number(step.position)
      : node.findIndex((member) => nameOf(member) === step.position)
    if (at < 0 || at >= node.length) return null
    drawn = `${drawn}[${at}]`
    node = node[at]
  }
  return drawn
}

/**
 * What a highlight file says, read from what the file holds. Anything written where an entry
 * belongs, and is not one, is reported rather than left out.
 */
export function highlightOf(file: unknown): Highlight {
  const wrong: string[] = []
  if (!isMapping(file)) return { of: '', entries: [], wrong: ['a highlight is a mapping'] }

  const of = file[OF]
  if (typeof of !== 'string' || of.length === 0) {
    wrong.push(`"${OF}" names the file this highlights, written as its path`)
  }

  const held = file[HIGHLIGHT]
  const entries: Entry[] = []
  if (held === undefined) {
    wrong.push(`"${HIGHLIGHT}" holds what is highlighted, written as a list`)
  } else if (!Array.isArray(held)) {
    wrong.push(`"${HIGHLIGHT}" must be written as a list`)
  } else {
    for (const [i, one] of held.entries()) {
      const where = `${HIGHLIGHT}[${i}]`
      if (!isMapping(one)) {
        wrong.push(`${where} carries a path and a note`)
        continue
      }
      const path = one[PATH]
      const note = one[NOTE]
      if (typeof path !== 'string' || path.length === 0) {
        wrong.push(`${where}.${PATH} says where this points, written as a path`)
        continue
      }
      if (note !== undefined && typeof note !== 'string') {
        wrong.push(`${where}.${NOTE} says what is there, written as text`)
        continue
      }
      entries.push({ path, note: note ?? '' })
    }
  }

  return { of: typeof of === 'string' ? of : '', entries, wrong }
}

/** Where each entry points in this document, and which of them point nowhere. */
export function marksOf(doc: unknown, entries: readonly Entry[]): Marks {
  const marked = new Map<string, string>()
  const missing: string[] = []
  for (const entry of entries) {
    const where = pathTo(doc, entry.path)
    if (where === null) missing.push(entry.path)
    else marked.set(where, entry.note)
  }
  return { marked, missing }
}

/** No highlight at all, which is how every document draws when none is given. */
export const NO_MARKS: Marks = { marked: new Map(), missing: [] }
