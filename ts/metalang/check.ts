// Checks a document against the APPROVED vocabulary and nothing else.
//
// Which words those are is in the language the document names, and nowhere else, this comment
// included. A document naming no language is written in MetaLang, whose words are in
// metalang.yaml.
//
// The vocabulary is taken as an argument. Loading it is rulebook.ts's job: the language the
// document names, read from that language's file along with the scopes it brings in. This file
// holds no word of its own, no table of types kept outside the language, and no default that
// papers over a gap.
//
// Two lists come out. PROBLEMS are things wrong with the document. BLOCKERS are things this
// checker cannot verify because the language has no word for them yet. A document passes
// only when both lists are empty, so nothing is ever reported as checked when it was not.
//
// Every check that can run, runs. One problem never stops another from being found, because
// a count that stops early is a lie about how much is wrong.
//
// Usage:  bun run ts/metalang/check.ts [file]

import { readFileSync } from 'node:fs'
import { basename } from 'node:path'

import * as yaml from 'js-yaml'

import { CONSTRAINTS, cannotApply, constraintsOn, logicFor, readerFor, unmetBy } from './constraints.ts'
import { LANGUAGE_FILE, rulebookFor, WORDS, type Rulebook } from './rulebook.ts'
import {
  acceptsOf,
  BOOTSTRAP,
  derivesFrom,
  holdsAType,
  IMPORT,
  importsScope,
  isMapping,
  LANGUAGE,
  mayBeLeftOut,
  onlyWord,
  optionsInOrder,
  optionsOf,
  primitiveOf,
  propertiesOf,
  propertyAbove,
  REPLACES,
  RESERVED,
  samePath,
  sameValue,
  saysOfProperty,
  SCOPE,
  SELF,
  typeOfOwnValue,
  typeOfPart as typeOfPartHolding,
  typeOfProperty,
  VALUE,
  valuesOf,
  type BodyOf,
  type CheckResult,
  type Fault,
  type Mapping,
  type Place,
  type Vocabulary,
} from './words.ts'

export type { CheckResult }

/**
 * The keywords of a declaration whose shape is more than the keyword's own type says, so each is
 * checked on its own rather than by the general pass. `constraints` is one of them.
 */
const APART_ON_KIND = [
  BOOTSTRAP.id,
  BOOTSTRAP.is,
  BOOTSTRAP.options,
  BOOTSTRAP.properties,
  VALUE,
  'values',
  CONSTRAINTS,
]

/** The same, on one of an enum's values and on one property of a type. */
const APART_ON_VALUE = [BOOTSTRAP.id, VALUE]
const APART_ON_PROPERTY = [BOOTSTRAP.is, VALUE]

const list = (words: readonly string[]): string => words.join(', ')
const a = (word: string): string => (/^[aeiou]/i.test(word) ? 'an' : 'a') + ' ' + word

/** One value as it reads inside a message, so a message can name what was written. */
const asWritten = (value: unknown): string => {
  if (typeof value === 'string') return `"${value}"`
  if (isMapping(value)) return 'what is written here'
  if (Array.isArray(value)) return 'the list written here'
  return String(value)
}

/** Every segment of a declaration, once its id is known. */
interface Declared {
  readonly at: string
  readonly kind: string
  readonly id: string
  readonly body: Mapping
  /** The keywords this declaration's kind carries, so the walk below need not look them up. */
  readonly claimed: readonly string[]
}

/**
 * Checks one already-parsed document. Pure: it reads nothing and writes nothing, so the whole
 * of it is reachable from a test with a document built in memory.
 *
 * @param doc  the parsed document, which may be anything YAML can produce
 * @param root how to name the document itself in a message, normally its file name
 * @param words the approved vocabulary
 */
export function check(doc: unknown, root: string, words: Vocabulary = WORDS): CheckResult {
  const problems: Fault[] = []
  const blockers: Fault[] = []
  const pending: Fault[] = []
  // Where a fault goes. It is the three lists above unless a trial is running: judging a value
  // against each option of a choice has to see what each option would say without saying it.
  let into: { problems: Fault[]; blockers: Fault[]; pending: Fault[] } = {
    problems,
    blockers,
    pending,
  }
  const fail = (where: string, what: string): void => void into.problems.push({ where, what })
  const block = (where: string, what: string): void => void into.blockers.push({ where, what })
  /** A value that is correctly written, fully checked, and not given yet. */
  const unfinished = (where: string, what: string): void => void into.pending.push({ where, what })
  const trial = (run: () => void): { problems: Fault[]; blockers: Fault[]; pending: Fault[] } => {
    const saved = into
    const mine = { problems: [] as Fault[], blockers: [] as Fault[], pending: [] as Fault[] }
    into = mine
    try {
      run()
    } finally {
      into = saved
    }
    return mine
  }

  // A document that is not a mapping cannot be walked, so this is the one thing that stops
  // the rest of the checks. Everything after it runs whatever else has already failed.
  if (doc === null || doc === undefined) {
    fail(root, 'the file is empty')
    return { problems, blockers, pending }
  }
  if (!isMapping(doc)) {
    fail(root, 'the file is not a mapping')
    return { problems, blockers, pending }
  }

  // ── the top level ──────────────────────────────────────────────────────────
  for (const key of Object.keys(doc)) {
    if (!words.topLevel.includes(key)) {
      fail(root, `"${key}" is not a top-level key — the only ones are ${list(words.topLevel)}`)
    }
  }

  // The shape each top-level keyword takes is a statement about that keyword, so it belongs in
  // the language. It is here because the language cannot say it yet, and it is one list rather
  // than two: every top-level keyword is named exactly once.
  const SHAPE: Record<string, 'list' | 'text'> = {
    [SCOPE]: 'list',
    [IMPORT]: 'list',
    language: 'text',
    title: 'text',
    description: 'text',
    [BOOTSTRAP.declare]: 'list',
  }

  // A top-level keyword with no approved shape is not a fault in the document — it is this
  // checker standing behind the language. So it is a blocker, and it is raised only when the
  // document actually carries the keyword, because a document that does not carry it had
  // nothing go unchecked.
  for (const key of words.topLevel.filter((k) => !(k in SHAPE))) {
    if (key in doc) {
      block(
        `${root}.${key}`,
        `"${key}" has no approved shape, so what is written under it cannot be checked`,
      )
    }
  }

  for (const [key, shape] of Object.entries(SHAPE)) {
    const v = doc[key]
    if (v === undefined) continue
    if (shape === 'text' && typeof v !== 'string') {
      fail(`${root}.${key}`, 'must be written as text')
    }
    if (shape === 'list' && !Array.isArray(v)) {
      fail(`${root}.${key}`, 'must be written as a list')
    }
  }

  const path = (at: string, written: unknown[]): void => {
    for (const [i, segment] of written.entries()) {
      if (typeof segment !== 'string' || segment.length === 0) {
        fail(`${at}[${i}]`, 'every segment of a scope must be written as text and cannot be empty')
      }
    }
  }

  path(`${root}.${SCOPE}`, Array.isArray(doc[SCOPE]) ? (doc[SCOPE] as unknown[]) : [])

  // What a document imports is the scopes it brings into itself, each one written the way a scope
  // is written. A scope this checker was not given cannot be brought in, so it is a blocker: the
  // document may be right and nothing here can say so.
  if (Array.isArray(doc[IMPORT])) {
    for (const [i, one] of (doc[IMPORT] as unknown[]).entries()) {
      const at = `${root}.${IMPORT}[${i}]`
      if (!Array.isArray(one)) {
        fail(at, 'a scope is written as a list of segments')
        continue
      }
      path(at, one)
      if (!words.brought.some((brought) => samePath(one, brought))) {
        block(at, `"${(one as unknown[]).join('.')}" is not a scope this checker was given`)
      }
    }
  }

  // The language a document names is the rulebook it is read against. Loading that rulebook is
  // the business of whoever calls this, so what is checked here is that the two agree: a document
  // read against a vocabulary that is not the one it names has not been checked against its own
  // language, and saying nothing about that would be passing it.
  const written = doc[LANGUAGE]
  const writtenIn = typeof written === 'string' && written.length > 0 ? [written] : null
  if (writtenIn !== null && !samePath(writtenIn, words.scope)) {
    block(
      `${root}.${LANGUAGE}`,
      `this document is written in "${written as string}" and it was read against` +
        ` "${words.scope.join('.')}", so nothing in it was checked against its own language`,
    )
  }

  /**
   * Whether this document reaches the declarations of that scope: it brings the scope in, or it
   * names it as the language it is written in. Naming the language is naming where the words come
   * from, so nothing further is needed to reach them.
   */
  const reaches = (scope: readonly string[]): boolean =>
    importsScope(doc, scope) || (writtenIn !== null && samePath(writtenIn, scope))

  /** One declaration of the vocabulary, when this document reaches the scope that declares it. */
  const fromVocabulary = (id: string): { kind: string; body: Mapping } | undefined => {
    const held = words.declared[id]
    return held !== undefined && reaches(held.scope) ? held : undefined
  }

  // ── the declarations ───────────────────────────────────────────────────────
  const seen = new Map<string, string>()
  const declarations: Declared[] = []
  const byId = new Map<string, { at: string; kind: string; body: Mapping }>()
  /** declaring id -> (property name -> type) */
  const own = new Map<string, Map<string, string>>()

  /**
   * One place where a type is named. A declaration's `is`, a property's `is` and one of a
   * choice's options are the same check, so there is one copy of it. It reads `seen`, which is
   * filled by the walk below, so it is only ever called after that walk has finished.
   */
  /**
   * The name that took the place of a retired one, or nothing where none did. A word that is
   * retired keeps its declaration and says so; the word that replaced it names the old one under
   * `replaces`. A document still writing the old name is told what to write, which is the whole
   * point of retiring a word rather than deleting it.
   */
  const tookThePlaceOf = (retired: string): string | null => {
    const everywhere = [
      ...[...byId.entries()].map(([id, one]) => [id, one.body] as const),
      ...Object.entries(words.declared).map(([id, one]) => [id, one.body] as const),
    ]
    for (const [id, body] of everywhere) {
      if (isMapping(body) && body[REPLACES] === retired) return id
    }
    return null
  }

  const namesType = (where: string, named: unknown): void => {
    if (typeof named !== 'string' || named.length === 0) {
      fail(where, 'must name a type')
      return
    }

    const found = byId.get(named) ?? fromVocabulary(named)
    if (found === undefined) {
      const elsewhere = words.declared[named]
      const instead = tookThePlaceOf(named)
      block(
        where,
        instead !== null
          ? `"${named}" has been retired — "${instead}" takes its place`
          : elsewhere === undefined
            ? `"${named}" is declared nowhere in this file and no scope it imports brings it in`
            : `"${named}" is declared in "${elsewhere.scope.join('.')}", which this document does` +
              ' not bring in',
      )
      return
    }

    // It resolves, so what it resolves to decides it. A constant is a declaration and not a type,
    // and nothing but the kinds the language calls types may be named here.
    if (words.types.length === 0) {
      block(where, 'which kinds are types has not been stated, so what this names cannot be judged')
      return
    }
    if (!words.types.includes(found.kind)) {
      fail(
        where,
        `"${named}" is ${a(found.kind)}, and only a type can be named here — the kinds that are` +
          ` types are ${list(words.types)}`,
      )
    }
  }

  /**
   * A type written out rather than named: one word naming the type, with its own properties
   * inside it. `{ array: { of: name } }` is an array whose `of` is `name`. It recurses, so a
   * collection of a collection is read the same way down.
   */
  const writesType = (where: string, value: unknown, inside: ReadonlySet<unknown>): void => {
    if (!isMapping(value)) {
      namesType(where, value)
      return
    }

    // A YAML anchor can write a mapping into itself, which has no bottom to read down to.
    if (inside.has(value)) {
      fail(where, 'is written inside itself')
      return
    }

    const written = onlyWord(value)
    if (written === null) {
      fail(where, 'names one type, written as one word with its own properties inside it')
      return
    }

    const [named, inner] = written
    namesType(where, named)
    if (isAbstract(named)) {
      fail(
        where,
        `"${named}" is abstract, so nothing is ever directly of it — name one of the types` +
          ' deriving from it instead',
      )
      return
    }

    const carries = words.entities[named]
    if (carries === undefined || carries === null) {
      block(
        `${where}.${named}`,
        `the properties ${a(named)} carries have not been decided, so nothing written here can be` +
          ' checked',
      )
      return
    }
    if (!isMapping(inner)) {
      fail(`${where}.${named}`, 'must be a mapping of its properties')
      return
    }

    for (const [key, held] of Object.entries(inner)) {
      if (!carries.includes(key)) {
        fail(
          `${where}.${named}`,
          `"${key}" cannot be written on ${a(named)} — the only ones are ${list(carries)}`,
        )
        continue
      }
      // What a collection holds is a type; what a pattern holds is a regular expression. Both are
      // written the same way, so the declaration of the property is what tells them apart.
      if (holdsAType(inVocabulary, named, key)) {
        writesType(`${where}.${named}.${key}`, held, new Set([...inside, value]))
      } else {
        const at = `${where}.${named}.${key}`
        // The type one part holds is asked for the same way everywhere, so a property written as
        // a collection of its own is read here exactly as it is read on a value.
        const holds = typeOfPart(inVocabulary, named, key)
        if (holds === undefined) {
          block(at, `nothing says what type "${key}" holds, so what is written here cannot be checked`)
        } else judges(at, holds, held, inVocabulary)
      }
    }
  }

  /**
   * How the rules that read the declarations above look a declaration up: in this document, and
   * then in what the document imports. A document's own declaration wins over an imported one.
   */
  const bodyOf = (id: string): Mapping | undefined =>
    byId.get(id)?.body ?? fromVocabulary(id)?.body

  /**
   * The same, in the language file rather than in this document. A type written out is one of the
   * vocabulary's types, and the type an id has is said on the vocabulary's own kinds, so both are
   * read from there whatever document is being checked.
   */
  const inVocabulary = (id: string): Mapping | undefined => words.declared[id]?.body

  /**
   * Every keyword a thing has to carry. The language says which of them may be left out, so this
   * names none of them itself. A declaration, one of its properties, one of an enum's values and
   * the file itself are all read the same way.
   */
  const mustCarry = (
    at: string,
    owner: string,
    body: Mapping,
    keywords: readonly string[],
    lookUp: BodyOf = inVocabulary,
  ): void => {
    for (const keyword of keywords) {
      if (body[keyword] === undefined && !mayBeLeftOut(lookUp, owner, keyword)) {
        fail(at, `has no ${keyword}`)
      }
    }
  }

  /**
   * Every keyword a thing carries, checked by what the language says that keyword holds: a type
   * where it holds one, and a value of that type where it does not. So `from` and `to` on a cast,
   * `abstract` on a type and `optional` on a property are checked by the same rule and none of
   * them is named here. The ones in `apart` are checked on their own underneath, because their
   * shape is more than the keyword's own type says.
   */
  const checksKeywords = (
    at: string,
    owner: string,
    body: Mapping,
    keywords: readonly string[],
    apart: readonly string[],
  ): void => {
    for (const keyword of keywords) {
      if (apart.includes(keyword) || body[keyword] === undefined) continue
      const where = `${at}.${keyword}`
      // The type a keyword holds may be named under `is` or written out as one word with its own
      // properties inside, which is how `accepts` says it holds several reserved states. Both are
      // read the one way, so a keyword written the second way is checked rather than blocked.
      const holds = typeOfPart(inVocabulary, owner, keyword)
      if (holdsAType(inVocabulary, owner, keyword)) writesType(where, body[keyword], new Set())
      // A reserved state is read the same way wherever `accepts` is written, whether the keyword
      // sits on a declaration or on a part of a value, so the two cannot disagree about what a
      // decided absence is.
      else if (reserved(where, inVocabulary, owner, keyword, body[keyword])) continue
      else if (holds === undefined) {
        block(where, `nothing says what "${keyword}" holds, so what is written here cannot be checked`)
      } else judges(where, holds, body[keyword], inVocabulary)
    }
  }

  // The file itself is a declaration of the language like any other, so which of its keys have to
  // be written is read from the same place.
  mustCarry(root, SELF.document, doc, words.topLevel)

  /** Whether nothing may ever be directly of that type, so only something deriving from it is. */
  const isAbstract = (named: string): boolean => byId.get(named)?.body.abstract === true

  /**
   * The type one property of a type holds, whether the property names it under `is` or writes it
   * out as one word with its own properties inside, and whether the type declares the property
   * itself or takes it from one above. Nothing when no declaration says.
   */
  const typeOfPart = (lookUp: BodyOf, owner: string, name: string): unknown =>
    typeOfPartHolding(lookUp, words.onProperty, owner, name)

  /**
   * Whether what is written for one property of a type is a reserved state rather than a value,
   * which it is when the property's own `accepts` names it. What each state means is said here
   * and nowhere else: one is reported and the other is not. A state a property accepts that this
   * checker has no rule for is a blocker, because a word it cannot account for has not been
   * checked.
   */
  const reservedState = (at: string, allowed: readonly string[], written: unknown): boolean => {
    if (typeof written !== 'string') return false
    if (!allowed.includes(written)) return false
    if (written === RESERVED.pending) unfinished(at, 'is required and not yet given')
    else if (written !== RESERVED.none) {
      block(at, `nothing says what "${written}" means where a value goes`)
    }
    return true
  }

  const reserved = (
    at: string,
    lookUp: BodyOf,
    type: string,
    name: string,
    written: unknown,
  ): boolean => reservedState(at, acceptsOf(lookUp, type, name), written)

  /**
   * The named parts of a value written against a type that derives down to `object`. Each key
   * names one of the type's properties and holds a value of that property's type, and which of
   * them have to be there is the same rule every other thing in the file is read by.
   */
  /**
   * A value held to every constraint its type carries, its own and the ones it derives. One that
   * cannot be applied is said so rather than counted as met, because a value nothing judged is not
   * a value that was checked.
   *
   * Both a value written in a document and a declaration written in one come here, because a
   * declaration is a value of its own kind and what that kind says its values may be is one
   * question with one answer.
   */
  /**
   * What one declaration carries, by the name it is known by. A constraint walking a path crosses
   * a name with this: the name holds the thing, not a part of it, so the route follows it to what
   * that declaration's value is and goes on from there. A name naming nothing this document
   * reaches answers nothing, and the route stops where it is and says so.
   */
  const carriedBy = (name: string): Mapping | undefined => {
    const body = byId.get(name)?.body ?? fromVocabulary(name)?.body
    if (body === undefined) return undefined
    // What a declaration carries is its own value where it has one, and the declaration itself
    // where it does not. A constant carries a value and a type carries what it says about itself,
    // and a route following a name wants whichever of those that name stands for.
    const value = body[VALUE]
    return isMapping(value) ? value : body
  }

  const holdsToConstraints = (
    where: string,
    type: string,
    value: unknown,
    lookUp: BodyOf = inVocabulary,
  ): void => {
    for (const constraint of constraintsOn(lookUp, type)) {
      const cannot = cannotApply(constraint)
      if (cannot !== null) {
        block(where, cannot)
        continue
      }
      const why = unmetBy(constraint, value, carriedBy)
      if (why !== null) {
        const shown = typeof value === 'string' ? `"${value}"` : isMapping(value) ? 'it' : String(value)
        fail(where, `${shown} ${why}, which "${constraint.from}" requires of every value of it`)
      }
    }
  }

  const judgesParts = (where: string, type: string, value: Mapping, lookUp: BodyOf): void => {
    const properties = propertiesOf(lookUp, type)
    for (const name of Object.keys(value)) {
      const at = `${where}.${name}`
      if (!properties.has(name)) {
        fail(at, `"${type}" has no property called "${name}"`)
        continue
      }
      const holds = typeOfPart(lookUp, type, name)
      if (holds === undefined) {
        block(at, `nothing says what type "${name}" holds, so what is written here cannot be checked`)
        continue
      }
      if (reserved(at, lookUp, type, name, value[name])) continue
      judges(at, holds, value[name], lookUp)
    }
    mustCarry(where, type, value, [...properties.keys()], lookUp)
  }

  /**
   * A value written against a type that is written out rather than named. Only a collection is
   * read this way: a list of members, each of the type its `of` holds, with no two the same when
   * the collection says its members are unique.
   */
  const judgesWritten = (where: string, written: Mapping, value: unknown, lookUp: BodyOf): void => {
    const one = onlyWord(written)
    if (one === null) {
      fail(where, 'names one type, written as one word with its own properties inside it')
      return
    }
    const [word, inner] = one
    if (lookUp(word) !== undefined && derivesFrom(lookUp, word, SELF.link)) {
      judgesLink(where, word, isMapping(inner) ? inner.to : undefined, value, lookUp)
      return
    }
    if (lookUp(word) === undefined || !derivesFrom(lookUp, word, SELF.collection)) {
      block(
        where,
        `nothing says how a value of ${a(word)} written out is read, so what is written here cannot` +
          ' be checked',
      )
      return
    }
    const members = isMapping(inner) ? inner.of : undefined
    if (members === undefined) {
      fail(where, `${a(word)} written out has no "of" to say what each member is`)
      return
    }
    if (isMapping(inner) && inner.by !== undefined) {
      judgesKeyed(where, word, inner.by, members, inner.accepts, value, lookUp)
      return
    }
    if (!Array.isArray(value)) {
      fail(where, `is not written as a list, which is how a value of ${a(word)} is written`)
      return
    }
    const unique = saysOfProperty(lookUp, word, 'unique', VALUE)
    if (typeof unique !== 'boolean') {
      block(where, `nothing says whether the members of ${a(word)} may repeat, so repeats cannot be checked`)
    }
    const earlier: unknown[] = []
    for (const [i, member] of value.entries()) {
      judges(`${where}[${i}]`, members, member, lookUp)
      if (unique === true && earlier.some((seen) => sameValue(seen, member))) {
        fail(`${where}[${i}]`, `repeats an earlier member, and the members of ${a(word)} are unique`)
      }
      earlier.push(member)
    }
  }

  /**
   * A value naming one declaration. The name is read against everything the document reaches, so a
   * relation can never point at nothing: a name nothing declares is reported where it is written,
   * and a name whose declaration carries a value of some other type is reported as what it is,
   * because a thing pointing at the wrong kind of thing is not a relation either.
   */
  const judgesLink = (
    where: string,
    word: string,
    to: unknown,
    value: unknown,
    lookUp: BodyOf,
  ): void => {
    if (typeof to !== 'string' || to.length === 0) {
      block(where, `nothing says what ${a(word)} here names, so what is written here cannot be checked`)
      return
    }
    if (typeof value !== 'string' || value.length === 0) {
      fail(where, `is not written as the name of a declaration, which is how a value of ${a(word)} is written`)
      return
    }
    const named = lookUp(value)
    if (named === undefined) {
      fail(where, `nothing this document reaches declares "${value}"`)
      return
    }
    const carries = typeOfOwnValue(named)
    if (named[VALUE] === undefined || typeof carries !== 'string' || carries.length === 0) {
      fail(where, `"${value}" carries no value of its own, so it is not ${a(to)}`)
      return
    }
    if (carries !== to && !derivesFrom(lookUp, carries, to)) {
      fail(where, `"${value}" carries a value of "${carries}", and what is named here is ${a(to)}`)
    }
  }

  /**
   * A value keyed by a closed set: one line for each word that set lists, written in the order it
   * lists them. The keys are the set's, so a key it does not list is named against the words it
   * does, and a word with no line is reported rather than read as a word that holds nothing. A
   * word that really holds nothing says so with the state written where the keys are said.
   */
  const judgesKeyed = (
    where: string,
    word: string,
    by: unknown,
    members: unknown,
    accepts: unknown,
    value: unknown,
    lookUp: BodyOf,
  ): void => {
    if (typeof by !== 'string' || lookUp(by) === undefined) {
      block(where, `nothing says which closed set ${a(word)} is keyed by, so the keys written here cannot be checked`)
      return
    }
    const words = valuesOf(lookUp, by)?.map((one) => one.id) ?? null
    if (words === null || words.length === 0) {
      block(where, `"${by}" lists no words, so the keys of ${a(word)} keyed by it cannot be checked`)
      return
    }
    if (!isMapping(value)) {
      fail(where, `is not written as one line for each word of "${by}", which is how a value of ${a(word)} is written`)
      return
    }
    const allowed = Array.isArray(accepts)
      ? accepts.filter((one): one is string => typeof one === 'string')
      : []
    for (const key of Object.keys(value)) {
      const at = `${where}.${key}`
      if (!words.includes(key)) {
        fail(at, `"${key}" is not a member of "${by}" — the members are ${list(words)}`)
        continue
      }
      if (reservedState(at, allowed, value[key])) continue
      judges(at, members, value[key], lookUp)
    }
    for (const one of words) {
      if (!(one in value)) fail(where, `has no ${one}`)
    }
  }

  /**
   * A value written against a choice. It belongs to whichever option it is a value of, and the
   * options are asked narrowest first: where one option derives from another, every value of the
   * narrower one is a value of the wider one as well, so the narrower one is asked first and the
   * value is of it when it takes it. A value the enum of a choice lists is therefore the enum,
   * and a value it does not list falls through to the type underneath.
   *
   * Two options that take the same value with no derivation between them leave nothing to tell
   * the value apart by, so that is reported rather than settled by the order they were written in.
   */
  const judgesChoice = (
    where: string,
    type: string,
    options: readonly unknown[],
    value: unknown,
    lookUp: BodyOf,
    via: readonly string[],
  ): void => {
    if (via.includes(type)) {
      fail(where, `"${type}" is a choice that is one of itself, so no value can be told apart`)
      return
    }
    if (options.length === 0) {
      fail(where, `"${type}" is a choice of nothing, so nothing is a value of it`)
      return
    }
    const tried = optionsInOrder(lookUp, options).map(({ option, named }) => ({
      option,
      named,
      ...trial(() => {
        judges(where, option, value, lookUp, [...via, type])
      }),
    }))
    const took = tried.filter((t) => t.problems.length === 0 && t.blockers.length === 0)
    const winner = took[0]
    if (winner !== undefined) {
      // The winner is the narrowest option that took the value. Any other option that also took
      // it has to be one the winner derives from, or the two say the same thing about this value
      // and nothing in the document says which was meant.
      for (const other of took.slice(1)) {
        if (
          winner.named !== null &&
          other.named !== null &&
          derivesFrom(lookUp, winner.named, other.named)
        ) {
          continue
        }
        fail(
          where,
          `is a value of both "${String(winner.option)}" and "${String(other.option)}" of` +
            ` "${type}", and neither derives from the other, so nothing tells the two apart`,
        )
        return
      }
      // What the winning option found that is not wrong is said, because it was said about this
      // value by the option the value belongs to. A losing option's findings are not.
      for (const one of winner.pending) unfinished(one.where, one.what)
      return
    }

    if (tried.every((t) => t.problems.length > 0)) {
      const says = tried.map((t) => `as ${String(t.option)} ${t.problems[0]?.what ?? ''}`)
      fail(where, `is not a value of any option of "${type}" — ${says.join('; ')}`)
      return
    }
    for (const t of tried) for (const b of t.blockers) block(b.where, b.what)
  }

  /**
   * One place where a value is written against a type: what a property always holds, and what one
   * of an enum's values stands for. A value is only ever directly of a primitive, so a type that
   * derives down to none cannot have a value written against it at all. A type that cannot be
   * found, or is not given, is a blocker: a value that nothing judged has not been checked.
   */
  const judges = (
    where: string,
    type: unknown,
    value: unknown,
    lookUp: BodyOf,
    via: readonly string[] = [],
  ): void => {
    if (isMapping(type)) {
      judgesWritten(where, type, value, lookUp)
      return
    }
    if (typeof type !== 'string' || type.length === 0) {
      block(where, 'nothing says what type this holds, so what is written here cannot be checked')
      return
    }
    // A property holding any constraint rather than one named kind of it is read the way a type's
    // own `constraints` are read: one word naming the kind, with what it holds inside. A property
    // naming one kind — a range, say — holds that kind's own parts and is read as any value is.
    if (type === SELF.constraint) {
      checksWritten(where, value)
      return
    }
    if (lookUp(type) === undefined) {
      block(where, `"${type}" is not declared where this value is read, so it cannot be checked`)
      return
    }

    const options = optionsOf(lookUp, type)
    if (options !== null) {
      judgesChoice(where, type, options, value, lookUp, via)
      return
    }

    const primitive = primitiveOf(lookUp, type)

    // An enum is a closed set of values. A value of one is written as the id naming a member, or
    // written out in full as one of the values listed. Nothing else is a value of the enum: one
    // that also took every value of the type it is based on would say nothing that type does not
    // say already, and the one thing it is for — listing the values there are — would go unchecked.
    const listed = valuesOf(lookUp, type)
    if (listed !== null) {
      if (typeof value === 'string' && listed.some((one) => one.id === value)) return
      if (listed.some((one) => sameValue(one.value, value))) return
      const members = listed.map((one) => one.id)
      fail(
        where,
        `${asWritten(value)} is not a member of "${type}" — the members are ${list(members)}`,
      )
      return
    }

    if (primitive === null) {
      fail(
        where,
        `"${type}" derives down to no primitive, so nothing can be written as a value of it`,
      )
      return
    }
    const reader = readerFor(primitive)
    if (reader === null) {
      block(where, `nothing says what one value of ${a(primitive)} looks like once a file is read`)
      return
    }
    if (!reader.is(value)) {
      fail(where, `is not written as ${a(primitive)}`)
      return
    }

    // It is written as the primitive says, so what the type allows decides the rest.
    holdsToConstraints(where, type, value, lookUp)

    // A value of `object` is a set of named parts, so the same walk goes one level down into them.
    // A leaf has no parts, so there is nothing below it to walk.
    if (primitive === SELF.object) judgesParts(where, type, value as Mapping, lookUp)
  }

  /** A value written against a type this document declares. */
  const holdsValue = (where: string, type: unknown, value: unknown): void => {
    judges(where, type, value, bodyOf)
  }

  /**
   * One place where an id is written. An id is a name, and both halves of what that means are
   * said in the language: the kind's own declaration gives the type an id has, and that type's
   * constraints say what its values may be.
   */
  const checksId = (where: string, owner: string, id: string): void => {
    const says = typeOfProperty(inVocabulary, owner, BOOTSTRAP.id)
    if (says === undefined) {
      block(where, `nothing says what type an id has, so "${id}" cannot be checked`)
      return
    }
    judges(where, says, id, inVocabulary)
  }

  /**
   * The constraints one declaration writes. Each is one word naming the kind of constraint with
   * what it holds inside it, which is the shape a type written out takes, so the same walk reads
   * it. What that kind of constraint MEANS is code rather than anything the language says, and a
   * kind nothing here applies is reported instead of passed over.
   */
  const checksConstraints = (at: string, written: unknown): void => {
    if (!Array.isArray(written)) {
      fail(`${at}.${CONSTRAINTS}`, 'must be written as a list')
      return
    }
    for (const [j, one] of (written as unknown[]).entries()) {
      checksWritten(`${at}.${CONSTRAINTS}[${j}]`, one)
    }
  }

  /**
   * One constraint as it is written: one word naming its kind with what it holds inside. A type
   * writes its own this way, and a constraint that holds another writes that one the same way, so
   * the two cannot disagree about what a constraint looks like.
   */
  const checksWritten = (where: string, one: unknown): void => {
    writesType(where, one, new Set())

    const named = isMapping(one) ? onlyWord(one) : null
    if (named === null) return
    const [kind, body] = named

    const logic = logicFor(kind)
    if (logic === null) {
      block(where, `nothing says how ${a(kind)} is applied, so no value can be checked against it`)
      return
    }
    if (!isMapping(body)) return
    const fault = logic.fault(body)
    if (fault !== null) fail(`${where}.${kind}`, fault)
  }

  // Names one word, which is either the kind itself or the wrapper that says the kind comes next.
  const nameOne = (node: Mapping, where: string): [string, unknown] | null => {
    const named = Object.entries(node)
    const first = named[0]
    if (first === undefined) {
      fail(where, 'names no kind')
      return null
    }
    if (named.length > 1) {
      fail(
        where,
        `names ${named.length} kinds, ${list(named.map(([k]) => k))} — one declaration is one kind`,
      )
      return null
    }
    return first
  }

  // One ordered list, so a kind may be written in it as often as a document needs. An item whose
  // word is `external` carries its kind inside it, which is how an external declaration reads in
  // the stream rather than in a list of its own.
  const here = doc[BOOTSTRAP.declare]
  if (Array.isArray(here)) {
    for (const [i, item] of (here as unknown[]).entries()) {
      const at = `${BOOTSTRAP.declare}[${i}]`

      if (!isMapping(item)) {
        fail(at, 'must be a mapping naming one kind')
        continue
      }

      const outer = nameOne(item, at)
      if (outer === null) continue

      let [kind, body] = outer
      let place: Place = 'declare'
      let names = at

      if (kind === BOOTSTRAP.external) {
        place = 'external'
        names = `${at}.${BOOTSTRAP.external}`
        if (!isMapping(body)) {
          fail(names, 'must be a mapping naming one kind')
          continue
        }
        const inner = nameOne(body, names)
        if (inner === null) continue
        ;[kind, body] = inner
      }

      if (!words.kinds.includes(kind)) {
        fail(names, `"${kind}" is not a kind — the only ones are ${list(words.kinds)}`)
        continue
      }
      if (!words.places[place].includes(kind)) {
        fail(
          names,
          place === 'external'
            ? `"${kind}" cannot be written inside ${BOOTSTRAP.external}`
            : `"${kind}" can only be written inside ${BOOTSTRAP.external}`,
        )
        continue
      }

      const where = `${names}.${kind}`
      if (!isMapping(body)) {
        fail(where, 'must be a mapping')
        continue
      }

      const allowed = words.onKind[kind]

      // A kind whose keywords are not decided cannot be read at all, so nothing further is said
      // about it. Checking its id or its type would be claiming a keyword nobody has approved.
      if (allowed === null || allowed === undefined) {
        block(
          where,
          `the keywords ${a(kind)} carries have not been decided, so nothing written here can be` +
            ' checked',
        )
        continue
      }

      for (const key of Object.keys(body)) {
        if (!allowed.includes(key)) {
          fail(where, `"${key}" cannot be written on ${a(kind)} — the only ones are ${list(allowed)}`)
        }
      }

      mustCarry(where, kind, body, allowed)

      // An id is the one thing nothing further can be said without, so a declaration missing one
      // stops here. It has already been reported as missing.
      const id = body.id
      if (id === undefined) continue
      if (typeof id !== 'string') {
        fail(`${where}.id`, 'must be written as text')
        continue
      }
      if (id.length === 0) {
        fail(`${where}.id`, 'is empty')
        continue
      }

      const already = seen.get(id)
      if (already !== undefined) fail(`${where}.id`, `"${id}" is already the id of ${already}`)
      else seen.set(id, where)

      declarations.push({ at: where, kind, id, body, claimed: allowed })
      byId.set(id, { at: where, kind, body })
      own.set(id, new Map())
    }
  }

  // ── what each declaration says, now that every id is known ─────────────────
  for (const { at, kind, id, body, claimed } of declarations) {
    checksId(`${at}.id`, kind, id)

    if (body[CONSTRAINTS] !== undefined) checksConstraints(at, body[CONSTRAINTS])

    checksKeywords(at, kind, body, claimed, APART_ON_KIND)

    // A declaration is a value of its own kind, so what that kind says its values may be is asked
    // of the whole declaration as well as of each keyword in it. A rule like "no two properties of
    // a type carry the same role" is a statement about the declaration entire, and the walk above,
    // which takes one keyword at a time, is the wrong place to see it.
    holdsToConstraints(at, kind, body)

    // A declaration whose kind lists values is based on exactly one type, because every value it
    // lists is a value of that type, and it has to say which. Everything else may derive from
    // several types, or from none.
    const listsValues = claimed.includes('values')

    if (body.is === undefined) {
      // Already reported, by the language saying whether this kind has to carry one.
    } else if (listsValues && Array.isArray(body.is)) {
      fail(`${at}.is`, 'must name exactly one type, because every value listed is a value of it')
    } else {
      const named: unknown[] = Array.isArray(body.is) ? body.is : [body.is]
      for (const [j, one] of named.entries()) {
        namesType(Array.isArray(body.is) ? `${at}.is[${j}]` : `${at}.is`, one)
      }
    }

    // A choice is one of several types, so each option names a type exactly as `is` does.
    if (body.options !== undefined) {
      if (!Array.isArray(body.options)) fail(`${at}.options`, 'must be written as a list')
      else {
        for (const [j, one] of (body.options as unknown[]).entries()) {
          namesType(`${at}.options[${j}]`, one)
        }

        // One option deriving from another is not a defect of the choice: it is what orders the
        // two. The narrower option is asked first and takes the values it accounts for, and the
        // wider one takes the rest, which is how a choice between an enum and the type it is
        // based on works. Where two options take the same value with no derivation between them
        // the value is the one that cannot be told apart, and it is reported where it is written.
        const options = (body.options as unknown[]).filter((o): o is string => typeof o === 'string')
        for (const [j, one] of options.entries()) {
          if (options.indexOf(one) !== j) {
            fail(`${at}.options`, `"${one}" is written twice, and a choice is one of each once`)
          }
        }
      }
    }

    // A declaration carrying one value of its own is checked against the type `typeOfOwnValue`
    // names, the same rule a value an enum lists is read by and the same rule the page draws it
    // against, so neither can read it as something the other does not.
    if (body[VALUE] !== undefined) holdsValue(`${at}.${VALUE}`, typeOfOwnValue(body), body[VALUE])

    // Every value an enum lists carries its own id and what that id stands for. The id names the
    // value inside this enum alone, so it is not added to the file's ids and nothing outside the
    // enum can reach it by that name.
    if (body.values !== undefined) {
      if (!Array.isArray(body.values)) fail(`${at}.values`, 'must be written as a list')
      else {
        const named = new Map<string, string>()
        for (const [j, one] of (body.values as unknown[]).entries()) {
          const where = `${at}.values[${j}]`

          if (!isMapping(one)) {
            fail(where, 'must be a mapping carrying an id and a value')
            continue
          }

          for (const key of Object.keys(one)) {
            if (!words.onValue.includes(key)) {
              fail(
                where,
                `"${key}" cannot be written on a value — the only ones are ${list(words.onValue)}`,
              )
            }
          }

          mustCarry(where, SELF.value, one, words.onValue)
          checksKeywords(where, SELF.value, one, words.onValue, APART_ON_VALUE)

          const vid = one.id
          if (vid === undefined) {
            // Already reported, by the language saying a value carries one.
          } else if (typeof vid !== 'string' || vid.length === 0) {
            fail(`${where}.id`, 'must be written as text')
          } else {
            const already = named.get(vid)
            if (already !== undefined) fail(`${where}.id`, `"${vid}" is already the id of ${already}`)
            else named.set(vid, where)
            checksId(`${where}.id`, SELF.value, vid)
          }

          // What a value is allowed to be comes from the one type the enum is based on. That it
          // has to carry one at all is said by the language and reported above.
          if (one.value !== undefined) holdsValue(`${where}.value`, body.is, one.value)
        }
      }
    }

    if (body.properties === undefined) continue
    if (!isMapping(body.properties)) {
      fail(`${at}.properties`, 'must be a mapping of name to property')
      continue
    }

    for (const [name, property] of Object.entries(body.properties)) {
      const where = `${at}.properties.${name}`

      // A mapping declares the property here. Anything else is a reference to one declared
      // elsewhere, and how a reference is written has not been settled.
      if (!isMapping(property)) {
        block(
          where,
          `"${String(property)}" is a reference to a property declared elsewhere, and how a` +
            ' reference is written has not been settled',
        )
        continue
      }

      // A property names its type under `is`. A type with properties of its own may instead be
      // written out, as one word naming it with its own properties inside — which is how a
      // collection says what it holds, and the same shape an item under `declare` is written in.
      // It is written beside the property's own keywords, so what is left once those are taken
      // away is the type and there can only be one of it.
      const outside = Object.keys(property).filter((k) => !words.onProperty.includes(k))

      // A property that names its type under `is` has written it, so anything left over is a
      // keyword a property does not carry rather than a second type.
      if (property[BOOTSTRAP.is] !== undefined) {
        for (const key of outside) {
          fail(
            where,
            `"${key}" cannot be written on a property — the only ones are` +
              ` ${list(words.onProperty)}`,
          )
        }
      } else if (outside.length > 1) {
        fail(
          where,
          `names ${outside.length} types — a property names its type under ${BOOTSTRAP.is}, or` +
            ' writes one out as one word naming a type with its own properties inside',
        )
        continue
      }

      mustCarry(where, SELF.property, property, words.onProperty)
      checksKeywords(where, SELF.property, property, words.onProperty, APART_ON_PROPERTY)

      const word = property[BOOTSTRAP.is] === undefined ? outside[0] : undefined
      if (word !== undefined) {
        // The property itself is what the written-out type is read out of, so it counts as
        // already entered: a mapping that writes itself into itself is caught at the first turn.
        writesType(where, { [word]: property[word] }, new Set([property]))
        own.get(id)?.set(name, word)
        continue
      }

      const type = property.is
      if (Array.isArray(type)) {
        fail(`${where}.is`, 'must name exactly one type, and a property holds one')
        continue
      }

      // A property written again on a type deriving from the one that declared it leaves `is`
      // off, and its type is the one that declaration above it gives.
      const above = type === undefined ? propertyAbove(bodyOf, id, name) : null
      if (type !== undefined) {
        namesType(`${where}.is`, type)
        if (typeof type === 'string' && type.length > 0) own.get(id)?.set(name, type)
      } else if (above === null) {
        fail(where, 'has no is, so its type is unknown')
        continue
      }

      if (property.value !== undefined) {
        holdsValue(`${where}.value`, typeOfPart(bodyOf, id, name), property.value)
      }
    }
  }

  // ── inheritance ────────────────────────────────────────────────────────────
  const parentsOf = (id: string): string[] => {
    const v = byId.get(id)?.body.is
    const many: unknown[] = Array.isArray(v) ? v : v === undefined ? [] : [v]
    return many.filter((x): x is string => typeof x === 'string' && byId.has(x))
  }

  // A type that reaches itself through `is` has no base, so nothing can be worked out about it.
  for (const { at, id } of declarations) {
    const walk = (from: string, path: readonly string[]): boolean => {
      for (const parent of parentsOf(from)) {
        if (parent === id) {
          fail(`${at}.is`, `"${id}" derives from itself: ${[...path, parent].join(' -> ')}`)
          return true
        }
        if (path.includes(parent)) continue
        if (walk(parent, [...path, parent])) return true
      }
      return false
    }
    walk(id, [id])
  }

  // Every property a type offers, and where each one was declared. A property is identified by
  // the type that declared it, so the same declaration reached by two paths is one property.
  const offeredBy = (id: string, path: readonly string[] = []): Map<string, Map<string, string>> => {
    const out = new Map<string, Map<string, string>>()
    if (path.includes(id) || !byId.has(id)) return out
    for (const parent of parentsOf(id)) {
      for (const [name, origins] of offeredBy(parent, [...path, id])) {
        for (const [from, type] of origins) {
          let into = out.get(name)
          if (into === undefined) {
            into = new Map()
            out.set(name, into)
          }
          into.set(from, type)
        }
      }
    }
    // What a type declares itself replaces everything it would have inherited by that name.
    for (const [name, type] of own.get(id) ?? []) out.set(name, new Map([[id, type]]))
    return out
  }

  for (const { at, id } of declarations) {
    for (const [name, origins] of offeredBy(id)) {
      if (origins.size < 2) continue
      const shapes = new Set(origins.values())
      // Same shape from both, so nothing to choose.
      if (shapes.size < 2) continue
      fail(
        `${at}.properties.${name}`,
        `"${name}" arrives from ${[...origins.keys()].join(' and ')} with different types, ` +
          `${[...shapes].join(' and ')}. Declare it here to say which it is.`,
      )
    }
  }

  return { problems, blockers, pending }
}

/** How many declarations a document holds. */
export function countDeclarations(doc: unknown): number {
  if (!isMapping(doc)) return 0
  const held = doc[BOOTSTRAP.declare]
  return Array.isArray(held) ? held.length : 0
}

const WIDTH = 30

/**
 * The whole report as lines, so a test can read what would be printed. `against` names the
 * language the document was read against when it names one, because a document checked against
 * one rulebook and a document checked against another are not the same report.
 */
export function reportLines(
  file: string,
  result: CheckResult,
  declared: number,
  against: string | null = null,
): string[] {
  const out: string[] = []
  const print = (heading: string, rows: readonly Fault[]): void => {
    out.push(`${rows.length} ${heading}${rows.length === 1 ? '' : 's'}:`)
    for (const r of rows) out.push('  ' + `${r.where}  `.padEnd(WIDTH) + r.what)
    out.push('')
  }

  out.push(
    `${file} — ${declared} declaration${declared === 1 ? '' : 's'}, checked against ` +
      (against === null ? 'the approved vocabulary' : `"${against}", the language it names`),
  )
  out.push('')

  if (result.problems.length > 0) print('problem', result.problems)
  if (result.blockers.length > 0) print('blocker', result.blockers)
  if (result.pending.length > 0) print('pending value', result.pending)

  // Whether anything is wrong with the document and whether the document is finished are two
  // questions, so the line that closes the report answers the one the report leaves open.
  if (result.problems.length === 0 && result.blockers.length === 0) {
    out.push(
      result.pending.length === 0
        ? 'Checked, and nothing is wrong.'
        : 'Nothing is wrong with the document, and it is not finished.',
    )
  } else if (result.problems.length === 0) {
    out.push('Nothing is wrong with what could be checked. The blockers above were not checked.')
  }

  return out
}

/**
 * What the checker makes of a document read against a rulebook, with whatever stopped that
 * rulebook being read whole already in it. A rulebook that could not be read leaves nothing to
 * check against, so that is the whole of what comes back: a document checked against a
 * vocabulary that is partly guessed has not been checked.
 *
 * Both the command that reports and the command that draws the page read a document this way,
 * so the page cannot show one thing while the report says another.
 */
export function checkedAgainst(doc: unknown, file: string, rulebook: Rulebook): CheckResult {
  const { words, faults } = rulebook
  if (words === null) return { problems: [], blockers: faults, pending: [] }
  const { problems, blockers, pending } = check(doc, basename(file), words)
  return { problems, blockers: [...faults, ...blockers], pending }
}

/** Reads a file, checks it, prints the report, and returns the exit code. */
export function run(file: string): number {
  let doc: unknown
  try {
    doc = yaml.load(readFileSync(file, 'utf8'))
  } catch (e) {
    const message = e instanceof Error ? e.message : String(e)
    console.log(`${file} — cannot be read`)
    console.log()
    console.log(`  ${message.split('\n')[0]}`)
    return 1
  }

  // The vocabulary comes from the language the document names and the scopes it brings in.
  const named = isMapping(doc) && typeof doc[LANGUAGE] === 'string' ? doc[LANGUAGE] : null
  const checked = checkedAgainst(doc, file, rulebookFor(doc, file))

  for (const line of reportLines(file, checked, countDeclarations(doc), named)) console.log(line)
  return checked.problems.length > 0 || checked.blockers.length > 0 || checked.pending.length > 0
    ? 1
    : 0
}

if (import.meta.main) process.exit(run(process.argv[2] ?? LANGUAGE_FILE))
