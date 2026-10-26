// How a document written out as YAML is coloured.
//
// This is not YAML colouring with MetaLang words added to it. Every colour here is decided by the
// vocabulary: a key is a keyword because the type it is written on declares that property, a word
// is a type reference because the keyword answering it holds a type, a word is a member because
// the keyword's type lists it, and a kind takes the colour its position in the vocabulary gives
// it. Nothing in this file lists a word of the language, so a word the language gains is coloured
// the day it is declared and a word it loses stops being coloured the same day.
//
// Anything the vocabulary does not account for is marked as not accounted for. A misspelled
// keyword and a real one never draw the same, because a reader who cannot see the difference is
// reading something this control said was fine.
//
// Not the same thing as highlight.ts, which says WHICH nodes of a document are marked in a render.
// This file is about how the text itself reads.
//
// Nothing here reads a file or touches the browser. The page draws these parts as React nodes and
// a page written by hand draws them as HTML, both from this one reading, so an example shown in
// the language and an example shown in a page cannot drift apart.

import { cannotApply, constraintsOn, readerFor, unmetBy } from './constraints.ts'
import {
  ACCEPTS,
  acceptsOf,
  BOOTSTRAP,
  derivesFrom,
  hueOfKind,
  isMapping,
  membersOf,
  onlyWord,
  optionsInOrder,
  optionsOf,
  primitiveOf,
  propertiesOf,
  SELF,
  typeOfPart,
  typeOfProperty,
  type BodyOf,
  type Mapping,
  type Vocabulary,
} from './words.ts'

/**
 * What one part of the text is. Everything the text is made of is one of these, including the
 * spaces and the punctuation, so the parts put back together are the text that was read.
 */
export type Role =
  /** Whitespace, which carries no colour but keeps the text as it was written. */
  | 'gap'
  /** The punctuation YAML is written with: the colon, the dash, the brackets, the quotes. */
  | 'mark'
  /** A comment. */
  | 'note'
  /** The word naming what a declaration is. */
  | 'kind'
  /** A keyword the language declares on the thing it is written on. */
  | 'word'
  /** A name the document gives: what it declares, and what it calls one of its properties. */
  | 'name'
  /** A word pointing at a declared type. */
  | 'ref'
  /** A word the keyword's own type lists as one of its values. */
  | 'member'
  /** A word standing where a value goes, saying the value is not there. */
  | 'reserved'
  | 'true'
  | 'false'
  | 'number'
  /** A regular expression, which is written as text and is not text. */
  | 'expr'
  /** Text: a title, prose, anything whose type is a string. */
  | 'text'
  /** Anything the vocabulary does not account for, drawn so it is seen. */
  | 'unknown'

/** One part of the text, and what it is. */
export interface Part {
  readonly text: string
  readonly is: Role
  /** On a kind, the kind itself, so it is drawn in the colour the vocabulary gives that kind. */
  readonly kind?: string
}

/**
 * Where a piece of text sits, for an example that is part of a file rather than a whole one.
 * Left unsaid, the text is read as a whole file, whose outermost keys are the file's own.
 */
export interface Written {
  /** The type each outermost thing is: a `declaration` for a list of declarations, and so on. */
  readonly inside?: string
  /** That the outermost keys are names, as the keys under `properties` are. */
  readonly named?: boolean
  /**
   * The type a value written here is, where the piece shows a value of something declared
   * elsewhere. It stands in for the `is` a whole document would have written above the piece.
   */
  readonly of?: string
  /**
   * What the document itself declares, by id, which `declaredBodiesOf` reads off a document. A
   * document is read against its vocabulary, so what it declares itself is only here: without
   * it, a type the document declares is a name and not something with properties.
   */
  readonly declared?: ReadonlyMap<string, Mapping>
}

/** On a collection, the keyword naming what it holds. */
const OF = 'of'

/** The two types a value's drawing asks for by name. Everything else is read from the value. */
const EXPRESSION = 'expression'
const NAME = 'name'

/** What the things written beneath a key are, which is what decides how they are coloured. */
type Holds = (
  /** Keys that are the properties of one type. */
  | { readonly of: 'mapping'; readonly owner: string }
  /** Many of one thing, written either as a list or as a key for each one. */
  | { readonly of: 'collection'; readonly member: unknown }
  /** One word naming which of several things this is, as a declaration names its kind. */
  | { readonly of: 'oneWord'; readonly among: readonly string[] }
  /**
   * A value of a choice. A key here names one of the options and what follows is written as that
   * option, which is how a declaration names its kind; a value here belongs to whichever option
   * takes it, asked narrowest first.
   */
  | { readonly of: 'choice'; readonly among: readonly string[]; readonly options: readonly unknown[] }
  /** A value of one type. */
  | { readonly of: 'scalar'; readonly type: string }
  /** Nothing the vocabulary accounts for. */
  | { readonly of: 'unknown' }
) & {
  /**
   * The reserved states the property holding this takes in place of a value. A word among them is
   * the state it names rather than the text it reads as. It stands in place of the WHOLE value,
   * so it is carried whatever that value would have been — one of a type, or many of one.
   */
  readonly accepts?: readonly string[]
}

const UNKNOWN: Holds = { of: 'unknown' }

/** Where the reader is: the column its contents sit at, and what they are. */
interface Frame {
  readonly at: number
  readonly holds: Holds
  /** The type written under `is` here, which is what a value of this declaration is. */
  is?: string
  /** The name written under `id` here, which is the declaration a property is being written on. */
  id?: string
  /** The property this frame is the body of, when it is one. */
  name?: string
}

/** A word that may be a key or a bare value. */
const WORD = /^[A-Za-z_][A-Za-z0-9_.-]*/

/** Every name the text itself declares, so a reference to one of them resolves inside it. */
export function namesDeclaredIn(text: string): ReadonlySet<string> {
  const found = new Set<string>()
  for (const [, name] of text.matchAll(/\bid:\s*([A-Za-z_][A-Za-z0-9_.-]*)/g)) {
    if (name !== undefined) found.add(name)
  }
  return found
}

/**
 * The text, read as MetaLang written out as YAML. Every character of the text comes back in one
 * part or another, in the order it was written, so nothing is quietly dropped on the way to a
 * page.
 */
export function partsOf(text: string, words: Vocabulary, written: Written = {}): readonly Part[] {
  // What the document declares itself answers first: a document may declare a name the language
  // also declares, and inside the document the document's own is the one that is written about.
  const bodyOf: BodyOf = (id) => written.declared?.get(id) ?? words.declared[id]?.body
  const everyName = new Set([...Object.keys(words.declared), ...(written.declared?.keys() ?? [])])
  const declaredHere = namesDeclaredIn(text)
  const parts: Part[] = []

  const put = (s: string, is: Role, kind?: string): void => {
    if (s.length === 0) return
    parts.push(kind === undefined ? { text: s, is } : { text: s, is, kind })
  }

  /** What is written beneath a keyword that holds this type. */
  const holdsFor = (held: unknown): Holds => {
    if (typeof held === 'string' && held.length > 0) {
      // A name pointing at a type is a value, not a mapping, however many kinds it may point at.
      if (held === SELF.type) return { of: 'scalar', type: held }
      // A choice holds a key naming one of its options, or a value of one of them, and which it
      // is cannot be told from the type alone. Both are kept, and whichever is written decides.
      const options = optionsOf(bodyOf, held) ?? words.choices[held]
      if (options !== undefined) {
        return {
          of: 'choice',
          among: options.filter((o): o is string => typeof o === 'string'),
          options,
        }
      }
      // A value of an abstract type says which type it is, the way a constraint is written as
      // `pattern` or `range`. The root is left out: its values are named parts, not one word.
      if (held !== SELF.object && bodyOf(held)?.abstract === true) {
        return { of: 'oneWord', among: below(held) }
      }
      if (propertiesOf(bodyOf, held).size > 0) return { of: 'mapping', owner: held }
      return { of: 'scalar', type: held }
    }
    // A type written out rather than named is a collection saying what it holds, or a value
    // naming one declaration.
    if (isMapping(held)) {
      const word = onlyWord(held)
      // A name pointing at a declaration is read the same way a name pointing at a type is, so a
      // name nothing a reader can reach declares is marked here as it is anywhere else.
      if (word !== null && derivesFrom(bodyOf, word[0], SELF.link)) {
        return { of: 'scalar', type: SELF.type }
      }
      if (word !== null && derivesFrom(bodyOf, word[0], SELF.collection)) {
        const body = isMapping(word[1]) ? word[1] : {}
        const taken = Array.isArray(body[ACCEPTS])
          ? (body[ACCEPTS] as unknown[]).filter((one): one is string => typeof one === 'string')
          : []
        // The states written where a collection says what it holds stand in place of one of
        // those, so they are carried down to each one rather than to the collection itself.
        return taken.length > 0
          ? { of: 'collection', member: body[OF], accepts: taken }
          : { of: 'collection', member: body[OF] }
      }
    }
    return UNKNOWN
  }

  /** Whether a type may be written out where a type is named, rather than named. */
  const writtenOut = (name: string): boolean =>
    derivesFrom(bodyOf, name, SELF.collection) || derivesFrom(bodyOf, name, SELF.link)

  /** Every type that derives from this one, which is what a value of an abstract type may be. */
  const below = (type: string): readonly string[] =>
    [...everyName].filter((name) => name !== type && derivesFrom(bodyOf, name, type))

  /** Whether a word points at something a reader of this text can reach. */
  const resolves = (word: string): boolean =>
    declaredHere.has(word) ||
    everyName.has(word) ||
    word in words.entities ||
    word in words.choices

  /**
   * Whether a value meets what a type says its values may be. What a type constrains is part of
   * what its values ARE, so a value that fails a constraint is not a value of that type: in a
   * choice it falls through to the next option, and where nothing takes it it is marked. A
   * constraint nothing can apply leaves the value unchecked, which is marked rather than drawn as
   * though it had been read.
   */
  const meets = (type: string, value: unknown): boolean =>
    constraintsOn(bodyOf, type).every(
      (constraint) => cannotApply(constraint) === null && unmetBy(constraint, value) === null,
    )

  /** What one bare value is, read against the type the keyword answering it holds. */
  const roleOfValue = (value: string, holds: Holds, via: readonly string[] = []): Role => {
    // A word the property takes in place of a value is read as the state it names before it is
    // read as a value at all, which is the order the checker reads it in — and whatever the value
    // would have been, because the state stands in place of all of it.
    if (holds.accepts?.includes(value) === true) return 'reserved'
    if (holds.of === 'collection') return roleOfValue(value, holdsFor(holds.member), via)
    if (holds.of === 'oneWord') return holds.among.includes(value) ? 'word' : 'unknown'
    // A value of a choice belongs to whichever option takes it, and the options are asked in the
    // order words.ts puts them in — narrowest first, so a value an enum lists is drawn as that
    // member and a value it does not list is drawn as whatever the type underneath makes of it.
    if (holds.of === 'choice') {
      for (const { option, named } of optionsInOrder(bodyOf, holds.options)) {
        if (named !== null && via.includes(named)) continue
        const role = roleOfValue(value, holdsFor(option), named === null ? via : [...via, named])
        if (role !== 'unknown') return role
      }
      return 'unknown'
    }
    // A type with properties of its own still has values written as one word: an enum lists
    // them, and a bare word names the one it lists.
    const type = holds.of === 'scalar' ? holds.type : holds.of === 'mapping' ? holds.owner : null
    if (type === null) return 'unknown'
    if (type === SELF.type) return resolves(value) ? 'ref' : 'unknown'
    if (membersOf(bodyOf, type)?.includes(value) === true) return 'member'
    // What the word carries is read the one way the reader reads it, and what the type says its
    // values may be is asked of what was read. A word neither of those takes is marked.
    const primitive = primitiveOf(bodyOf, type)
    const got = primitive === null ? null : (readerFor(primitive)?.from(value) ?? null)
    if (got === null || !meets(type, got.read)) return 'unknown'
    const read = got.read
    if (typeof read === 'boolean') return read ? 'true' : 'false'
    if (typeof read === 'number') return 'number'
    if (primitive === EXPRESSION) return 'expr'
    // A name is what the document calls something; everything else written as text is text. A
    // name derives down to a string, so the type is asked rather than the primitive.
    return derivesFrom(bodyOf, type, NAME) ? 'name' : 'text'
  }

  const frames: Frame[] = [
    {
      at: 0,
      holds:
        written.named === true
          ? { of: 'collection', member: written.inside }
          : holdsFor(written.inside ?? SELF.document),
      ...(written.of === undefined ? {} : { is: written.of }),
    },
  ]

  /** The name of the declaration a frame is written inside. */
  const idAbove = (from: number): string | undefined => {
    for (let i = from; i >= 0; i--) {
      const said = frames[i]?.id
      if (said !== undefined) return said
    }
    return undefined
  }

  /**
   * The type a value written here is, where the keyword holds a value of whatever the thing
   * around it is of. On a property that is the type the property holds, which the type above may
   * be the one to declare; otherwise it is the type written under `is`.
   */
  const baseAbove = (): string | undefined => {
    for (let i = frames.length - 1; i >= 0; i--) {
      const frame = frames[i]
      if (frame === undefined) continue
      if (frame.name !== undefined) {
        const owner = idAbove(i - 1)
        const held = owner === undefined ? undefined : typeOfProperty(bodyOf, owner, frame.name)
        if (held !== undefined) return held
      }
      if (frame.is !== undefined) return frame.is
    }
    return undefined
  }

  /**
   * The reserved states the property a value is written for takes, read the same way the type it
   * holds is read: from the property named above, on the declaration above that.
   */
  const acceptsAbove = (): readonly string[] => {
    for (let i = frames.length - 1; i >= 0; i--) {
      const frame = frames[i]
      if (frame === undefined) continue
      if (frame.name !== undefined) {
        const owner = idAbove(i - 1)
        const taken = owner === undefined ? [] : acceptsOf(bodyOf, owner, frame.name)
        if (taken.length > 0) return taken
      }
      if (frame.is !== undefined) return []
    }
    return []
  }

  /** What is written beneath a keyword, with the reserved states it takes where it takes any. */
  const takes = (child: Holds, accepts: readonly string[]): Holds =>
    accepts.length > 0 ? { ...child, accepts } : child

  /** What is written beneath a keyword, with `object` meaning a value of the type above. */
  const childOf = (held: unknown): Holds => {
    if (held === SELF.object) {
      const base = baseAbove()
      return base === undefined ? UNKNOWN : takes(holdsFor(base), acceptsAbove())
    }
    return holdsFor(held)
  }

  /** What a key is, and what is written beneath it. */
  const keyIn = (holds: Holds, name: string): { role: Role; kind?: string; child: Holds } => {
    if (holds.of === 'mapping') {
      if (propertiesOf(bodyOf, holds.owner).has(name)) {
        const held = typeOfPart(bodyOf, words.onProperty, holds.owner, name)
        return { role: 'word', child: takes(childOf(held), acceptsOf(bodyOf, holds.owner, name)) }
      }
      // A type written out in place of a name, which is how a property says it holds many of
      // something, or one name pointing at a declaration. Nothing else may be written out, so
      // nothing else is read as one.
      if (writtenOut(name)) {
        return { role: 'word', child: holdsFor(name) }
      }
      return { role: 'unknown', child: UNKNOWN }
    }
    // A key naming one of several things it may be: one of a choice's options, or one of the
    // types below an abstract one. What follows it is written as the thing it names.
    if (holds.of === 'oneWord' || holds.of === 'choice') {
      if (!holds.among.includes(name)) return { role: 'unknown', child: UNKNOWN }
      // A kind carries its own name, because its colour comes from where it sits among them.
      if (words.kinds.includes(name)) return { role: 'kind', kind: name, child: holdsFor(name) }
      return { role: 'word', child: holdsFor(name) }
    }
    // Many of one thing, written as a key for each one: the key is the name, not a keyword.
    if (holds.of === 'collection') {
      return { role: 'name', child: takes(holdsFor(holds.member), holds.accepts ?? []) }
    }
    // Where a type is named, one may be written out instead of named.
    if (holds.of === 'scalar' && holds.type === SELF.type) {
      if (writtenOut(name)) return { role: 'word', child: holdsFor(name) }
    }
    return { role: 'unknown', child: UNKNOWN }
  }

  /** A bare value, with the quotes it may be written in drawn as the marks they are. */
  const putValue = (value: string, holds: Holds): void => {
    const quote = value.startsWith('"') || value.startsWith("'") ? value[0] : null
    if (quote !== undefined && quote !== null && value.length > 1 && value.endsWith(quote)) {
      const held = value.slice(1, -1)
      // Quotes say how the value is written, not what it is, so the type decides its colour the
      // same way it decides an unquoted one. A quoted value the vocabulary cannot account for is
      // marked, because a quoted mistake is still a mistake.
      put(quote, 'mark')
      put(held, roleOfValue(held, holds))
      put(quote, 'mark')
      return
    }
    put(value, roleOfValue(value, holds))
  }

  /**
   * A mapping or a list written on one line, between its brackets. Returns where it ends, so
   * whatever follows it on the line is read where it was written.
   */
  const putFlow = (s: string, from: number, holds: Holds): number => {
    const open = s[from]
    if (open !== '{' && open !== '[') return from
    put(open, 'mark')
    let i = from + 1
    const close = open === '{' ? '}' : ']'
    let inside: Holds = holds
    // The property whatever comes next is written inside, while it is being read.
    let opened: Frame | null = null
    while (i < s.length) {
      const space = /^\s*/.exec(s.slice(i))?.[0] ?? ''
      put(space, 'gap')
      i += space.length
      const ch = s[i]
      if (ch === undefined) return i
      if (ch === close) {
        put(close, 'mark')
        return i + 1
      }
      if (ch === ',') {
        put(',', 'mark')
        i += 1
        continue
      }
      if (open === '{') {
        const key = WORD.exec(s.slice(i))?.[0]
        if (key === undefined || s[i + key.length] !== ':') {
          // Nothing here reads as a key, so what is here is said to be unaccounted for rather
          // than skipped past.
          const to = nextStop(s, i, close)
          const held = s.slice(i, to)
          put(held.trimEnd(), 'unknown')
          put(held.slice(held.trimEnd().length), 'gap')
          i = to
          continue
        }
        const said = keyIn(holds, key)
        put(key, said.role, said.kind)
        put(':', 'mark')
        i += key.length + 1
        inside = said.child
        opened = said.role === 'name' ? { at: -1, holds: said.child, name: key } : null
        const said_value = WORD.exec(s.slice(i).trimStart())?.[0]
        const frame = frames[frames.length - 1]
        if (frame !== undefined && said_value !== undefined) {
          if (key === BOOTSTRAP.is) frame.is = said_value
          if (key === BOOTSTRAP.id) frame.id = said_value
        }
      } else {
        inside = holds.of === 'collection' ? holdsFor(holds.member) : holds
      }
      const gap = /^\s*/.exec(s.slice(i))?.[0] ?? ''
      put(gap, 'gap')
      i += gap.length
      const next = s[i]
      if (opened !== null) frames.push(opened)
      if (next === '{' || next === '[') {
        i = putFlow(s, i, inside)
        if (opened !== null) frames.pop()
        opened = null
        continue
      }
      const to = nextStop(s, i, close)
      if (to > i) putValue(s.slice(i, to).trimEnd(), inside)
      if (opened !== null) frames.pop()
      opened = null
      const trailing = s.slice(i, to).length - s.slice(i, to).trimEnd().length
      put(s.slice(to - trailing, to), 'gap')
      i = to
    }
    return i
  }

  /**
   * Where a value written inside brackets ends: at a comma, or at the bracket that closes it. A
   * comma inside quotes is part of what is written, not the end of it.
   */
  const nextStop = (s: string, from: number, close: string): number => {
    let i = from
    let quote: string | null = null
    while (i < s.length) {
      const ch = s[i]
      if (ch === undefined) break
      if (quote !== null) {
        if (ch === quote) quote = null
      } else if (ch === '"' || ch === "'") {
        quote = ch
      } else if (ch === ',' || ch === close) {
        break
      }
      i++
    }
    return i
  }

  const lines = text.split('\n')
  // A block of text written under `>` or `|`: its lines are the text, not the language.
  let block: number | null = null
  // What a key opened, which belongs to whatever is written deeper than it on the lines below.
  let pending: Frame | null = null

  for (let n = 0; n < lines.length; n++) {
    const line = lines[n] ?? ''
    const ends = n === lines.length - 1 ? '' : '\n'
    const indent = line.length - line.trimStart().length
    const content = line.slice(indent)

    if (content === '') {
      put(line, 'gap')
      put(ends, 'gap')
      continue
    }
    if (block !== null) {
      if (indent > block) {
        put(line.slice(0, indent), 'gap')
        put(content, 'text')
        put(ends, 'gap')
        continue
      }
      block = null
    }

    if (pending !== null) {
      if (indent > pending.at) frames.push({ ...pending, at: indent })
      pending = null
    }
    while (frames.length > 1 && (frames[frames.length - 1]?.at ?? 0) > indent) frames.pop()

    put(line.slice(0, indent), 'gap')
    let rest = content
    let at = indent
    let frame = frames[frames.length - 1] ?? { at: 0, holds: UNKNOWN }
    let holds = frame.holds

    // One list marker for each level the line opens, so an item written on the same line as its
    // dash is read inside the list it belongs to.
    while (/^-(\s|$)/.test(rest)) {
      put('-', 'mark')
      rest = rest.slice(1)
      at += 1
      const space = /^\s*/.exec(rest)?.[0] ?? ''
      put(space, 'gap')
      rest = rest.slice(space.length)
      at += space.length
      // A dash says this is one of them, and what one of them is comes from what the key holds.
      holds = holds.of === 'collection' ? holdsFor(holds.member) : holds
      frame = { at, holds }
      frames.push(frame)
    }

    if (rest.startsWith('#')) {
      put(rest, 'note')
      put(ends, 'gap')
      continue
    }

    const key = WORD.exec(rest)?.[0]
    if (key !== undefined && rest[key.length] === ':') {
      const said = keyIn(holds, key)
      put(key, said.role, said.kind)
      put(':', 'mark')
      rest = rest.slice(key.length + 1)
      const space = /^\s*/.exec(rest)?.[0] ?? ''
      put(space, 'gap')
      rest = rest.slice(space.length)

      // The property a body is the body of, so a value written inside it is read against the
      // type that property holds, wherever that type was declared.
      const opened: Frame =
        said.role === 'name' ? { at, holds: said.child, name: key } : { at, holds: said.child }

      if (rest === '') {
        pending = opened
        put(ends, 'gap')
        continue
      }
      // What a declaration is, and what it is of: both are what the values it writes are read
      // against, so the frame they are written in keeps them.
      const written_here = WORD.exec(rest)?.[0]
      if (written_here !== undefined) {
        if (key === BOOTSTRAP.is) frame.is = written_here
        if (key === BOOTSTRAP.id) frame.id = written_here
      }
      // What follows on the same line is written inside the key, so it is read inside it.
      frames.push(opened)
      block = putRest(rest, said.child)
      frames.pop()
      put(ends, 'gap')
      continue
    }

    // Not a key: a value of a list, or a line of text under a block.
    block = putRest(rest, holds)
    put(ends, 'gap')
  }

  /**
   * What is written after a key, or as an item of a list, to the end of its line. Where that is
   * the marker opening a block of text, the column that block is written under comes back, so
   * the lines beneath it are read as the text they are.
   */
  function putRest(rest: string, holds: Holds): number | null {
    if (rest.startsWith('>') || rest.startsWith('|')) {
      const marker = /^[>|][-+\d]*/.exec(rest)?.[0] ?? rest
      put(marker, 'mark')
      put(rest.slice(marker.length), 'gap')
      return frames[frames.length - 1]?.at ?? 0
    }
    if (rest.startsWith('{') || rest.startsWith('[')) {
      const end = putFlow(rest, 0, holds)
      putTail(rest.slice(end))
      return null
    }
    const note = / #/.exec(rest)
    const value = note === null ? rest : rest.slice(0, note.index)
    const trimmed = value.trimEnd()
    putValue(trimmed, holds)
    put(value.slice(trimmed.length), 'gap')
    if (note !== null) putTail(rest.slice(note.index))
    return null
  }

  /** Whatever is written after a value on its line: spaces, then a comment if there is one. */
  function putTail(tail: string): void {
    const note = tail.indexOf('#')
    if (note < 0) {
      put(tail, 'gap')
      return
    }
    put(tail.slice(0, note), 'gap')
    put(tail.slice(note), 'note')
  }

  return parts
}

const escape = (s: string): string =>
  s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

/**
 * The parts as HTML, for a page written by hand. A gap carries no element of its own, so what the
 * page holds is the text with the words that mean something wrapped.
 */
export function htmlOf(parts: readonly Part[], kinds: readonly string[] = []): string {
  return parts
    .map((part) => {
      if (part.is === 'gap') return escape(part.text)
      const hue = part.kind === undefined ? null : hueOfKind(part.kind, kinds)
      const style = hue === null ? '' : ` style="--kind:oklch(54% 0.17 ${hue})"`
      return `<span class="y-${part.is}"${style}>${escape(part.text)}</span>`
    })
    .join('')
}

/** A whole example, ready to sit in a page: the text read, coloured, inside its own block. */
export function writtenHtml(text: string, words: Vocabulary, written: Written = {}): string {
  return `<pre class="y">${htmlOf(partsOf(text, words, written), words.kinds)}</pre>`
}
