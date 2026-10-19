// The vocabulary, read from the language file rather than held here.
//
// This file holds no list of words. It holds the BOOTSTRAP — the handful of words a reader must
// already know before it can read a vocabulary written in the language — and the function that
// turns a document's declarations into the Vocabulary every script takes. Everything else in the
// language arrives as a declaration and nothing here is a second copy of it.
//
// The language says what it is by declaring itself. A kind is an entity whose properties are the
// keywords that kind carries, and which kinds may be written where is said by choices. So the
// reader reads entities and choices, and there is no separate mechanism for declaring a word.
//
// Nothing in this file reaches the file system, so the page's bundle may import it. Reading the
// language file is the business of the scripts that read files.

/** A kind a declaration may be, wherever it may be written. */
export type Kind = string

/** The top-level key naming where a document's own declarations live. */
export const SCOPE = 'scope'

/** The top-level key naming the scopes a document brings into itself. */
export const IMPORT = 'import'

/** Whether two paths are the same path. */
export const samePath = (a: readonly unknown[], b: readonly unknown[]): boolean =>
  a.length === b.length && a.every((x, i) => x === b[i])

/** The scopes a document imports, each one a path. */
export function importedBy(doc: unknown): readonly (readonly unknown[])[] {
  if (!isMapping(doc)) return []
  const written = doc[IMPORT]
  if (!Array.isArray(written)) return []
  return (written as unknown[]).filter((one): one is unknown[] => Array.isArray(one))
}

/** Whether a document brings that scope into itself. A scope of no segments is no scope. */
export const importsScope = (doc: unknown, scope: readonly string[]): boolean =>
  scope.length > 0 && importedBy(doc).some((one) => samePath(one, scope))

/**
 * The keywords something carries, or `null` when it is approved and its keywords are not decided.
 * An entity written with no `properties` at all is undecided; one written with an empty
 * `properties` carries none. The two are different things and the checker treats them differently.
 */
export type Claimed = readonly string[] | null

/** Where a declaration is written. A document holds one list, and `external` wraps an item. */
export type Place = keyof Places

/**
 * Where a declaration may be written, and which kinds may be written there.
 *
 * A document holds ONE ordered list of declarations, under `declare`, and a kind may be written
 * in it as often as the document needs. A declaration whose implementation lives outside the
 * language is an item of that same list with `external` written in place of its kind and the
 * kind inside it, so it reads in the order the author put it rather than in a list of its own.
 */
export interface Places {
  /** Written as the item's own word. */
  readonly declare: readonly Kind[]
  /** Written inside `external`, which the item names in place of a kind. */
  readonly external: readonly Kind[]
}

/**
 * The whole vocabulary as one value. Everything that reads the vocabulary takes this, so the
 * checker and the page cannot be handed different subsets of it, and a test can drive either
 * one with a vocabulary of its own.
 */
export interface Vocabulary {
  readonly places: Places
  /** The kinds whose declarations are types, so a name under `is` may point at one. */
  readonly types: readonly Kind[]
  readonly topLevel: readonly string[]
  readonly kinds: readonly Kind[]
  readonly onKind: Readonly<Record<Kind, Claimed>>
  readonly onProperty: readonly string[]
  readonly onValue: readonly string[]
  /**
   * Every entity the file declares, and the properties it carries once what it derives from has
   * been folded in. The kinds are in here too, because a kind is an entity.
   */
  readonly entities: Readonly<Record<string, Claimed>>
  /** Every choice the file declares, and the options it lists. */
  readonly choices: Readonly<Record<string, readonly string[]>>
  /** Where the language file says its own declarations live, which is what a document imports. */
  readonly scope: readonly string[]
  /**
   * Every declaration of the language file itself, by id: the kind it was written as and what it
   * writes. The vocabulary says which properties a type carries; this is where to read what one of
   * them holds, which is how a keyword's own type — the type an id has, the type a collection's
   * `of` holds — is known, and it is what a document reaches when it imports the language's scope.
   */
  readonly declared: Readonly<Record<string, Declared>>
}

/**
 * The words a reader must know before it can read a vocabulary. Every other word of the language
 * is declared in the language file and read from it.
 */
export const BOOTSTRAP = {
  /** The one top-level key a document's declarations are written under. */
  declare: 'declare',
  /** Written in place of a kind, to say the implementation of what follows lives outside. */
  external: 'external',
  /** The kind that declares a type with named properties. */
  entity: 'entity',
  /** The kind that declares a type that is one of several types. */
  choice: 'choice',
  /** On a declaration, the name it is known by. */
  id: 'id',
  /** On a declaration, the type it derives from. */
  is: 'is',
  /** On an entity, its named properties. */
  properties: 'properties',
  /** On a choice, the types it is one of. */
  options: 'options',
} as const

/**
 * The declarations the reader looks up by name, because each one says something about the language
 * itself rather than about a document written in it.
 */
export const SELF = {
  /** The entity whose properties are the top-level keys of a file. */
  document: 'document',
  /** The choice listing what may be written as an item under `declare`. */
  declaration: 'declaration',
  /** The choice listing which kinds may be written inside `external`. */
  external: 'external',
  /** The choice listing which kinds are types, so a name under `is` may point at one. */
  type: 'type',
  /** The entity whose properties are what a property carries. */
  property: 'property',
  /** The entity whose properties are what one value of an enum carries. */
  value: 'value',
  /** The root every type derives from, and the one whose values are a set of named parts. */
  object: 'object',
  /** The type every kind of many-of-a-thing derives from. */
  collection: 'collection',
} as const

export type Mapping = Record<string, unknown>

export const isMapping = (v: unknown): v is Mapping =>
  v !== null && typeof v === 'object' && !Array.isArray(v)

/** One declaration, as the vocabulary keeps it. */
export interface Declared {
  readonly kind: Kind
  readonly body: Mapping
}

/** A vocabulary, and everything that stopped it being read as written. */
export interface Resolved {
  readonly words: Vocabulary
  readonly faults: readonly string[]
}

/** One declaration's body, looked up by the id it declares. */
export type BodyOf = (id: string) => Mapping | undefined

/** A property declared by a type above, and the type that declares it. */
export interface Above {
  readonly from: string
  readonly property: Mapping
}

/**
 * The property of that name declared by something this type derives from, and which type that
 * is. A type may write a property again to say what it always holds, and then what it does not
 * write is taken from the declaration above. Nothing when no type above declares one.
 *
 * Both the checker and the page read a property this way, so the rule is written once here and
 * each of them passes its own way of looking a declaration up.
 */
export function propertyAbove(
  bodyOf: BodyOf,
  id: string,
  name: string,
  path: readonly string[] = [id],
): Above | null {
  const held = bodyOf(id)?.is
  const above: unknown[] = Array.isArray(held) ? held : held === undefined ? [] : [held]
  for (const parent of above) {
    if (typeof parent !== 'string' || path.includes(parent)) continue
    const properties = bodyOf(parent)?.properties
    if (isMapping(properties) && isMapping(properties[name])) {
      return { from: parent, property: properties[name] }
    }
    const further = propertyAbove(bodyOf, parent, name, [...path, parent])
    if (further !== null) return further
  }
  return null
}

/**
 * Every property a type has, keyed by name: the ones it declares itself and the ones it takes from
 * the types it derives from. A type writing a property again takes everything it does not write
 * from above, which is the rule `propertyAbove` already follows, so a name written twice is
 * resolved the same way here. The types above are read first, so what a type writes itself wins.
 */
export function propertiesOf(
  bodyOf: BodyOf,
  named: string,
  path: readonly string[] = [],
): ReadonlyMap<string, Mapping> {
  const found = new Map<string, Mapping>()
  const body = bodyOf(named)
  if (body === undefined || path.includes(named)) return found

  const held = body[BOOTSTRAP.is]
  const above: unknown[] = Array.isArray(held) ? held : held === undefined ? [] : [held]
  for (const parent of above) {
    if (typeof parent !== 'string') continue
    for (const [name, property] of propertiesOf(bodyOf, parent, [...path, named])) {
      found.set(name, property)
    }
  }

  const own = body[BOOTSTRAP.properties]
  if (isMapping(own)) {
    for (const [name, property] of Object.entries(own)) {
      if (isMapping(property)) found.set(name, { ...found.get(name), ...property })
    }
  }
  return found
}

/** On an enum, the values it lists. */
const VALUES = 'values'

/** On a type, that its values come from whatever reads the file rather than from the language. */
const PRIMITIVE = 'primitive'

/**
 * The primitive a type derives down to, or nothing when it derives down to none. A value is only
 * ever directly of a primitive, so this is what decides both whether a value written against a
 * type can be read at all and how it is written back out.
 */
export function primitiveOf(
  bodyOf: BodyOf,
  named: string,
  path: readonly string[] = [],
): string | null {
  const body = bodyOf(named)
  if (body === undefined || path.includes(named)) return null
  if (body[PRIMITIVE] === true) return named

  const held = body[BOOTSTRAP.is]
  const above: unknown[] = Array.isArray(held) ? held : held === undefined ? [] : [held]
  for (const parent of above) {
    if (typeof parent !== 'string') continue
    const found = primitiveOf(bodyOf, parent, [...path, named])
    if (found !== null) return found
  }
  return null
}

/**
 * What the declaration of one property of a type says under one keyword. A type may write a
 * property again to say what it always holds, and then every keyword it does not write is taken
 * from the declaration above, so the two are read the same way here.
 */
export function saysOfProperty(
  bodyOf: BodyOf,
  owner: string,
  name: string,
  keyword: string,
): unknown {
  const properties = bodyOf(owner)?.properties
  const here = isMapping(properties) && isMapping(properties[name]) ? properties[name] : undefined
  return here?.[keyword] ?? propertyAbove(bodyOf, owner, name)?.property[keyword]
}

/**
 * The type a property of a type is declared to hold, whether that type declares the property
 * itself or takes it from one it derives from. Nothing when no declaration says, or when what is
 * written there is a type written out rather than a name.
 */
export function typeOfProperty(bodyOf: BodyOf, owner: string, name: string): string | undefined {
  const held = saysOfProperty(bodyOf, owner, name, BOOTSTRAP.is)
  return typeof held === 'string' && held.length > 0 ? held : undefined
}

/**
 * The type a property holds, whether it names one under `is` or writes one out as a single word
 * with its own properties inside, which is how a collection says what it holds. Nothing when no
 * declaration says, or when what is written there is neither. `onProperty` is the list of keywords
 * the language says a property carries, so what is left beside them is the written-out type.
 */
export function typeOfPart(
  bodyOf: BodyOf,
  onProperty: readonly string[],
  owner: string,
  name: string,
): unknown {
  const named = typeOfProperty(bodyOf, owner, name)
  if (named !== undefined) return named
  const declared = propertiesOf(bodyOf, owner).get(name)
  if (declared === undefined || declared[BOOTSTRAP.is] !== undefined) return undefined
  const outside = Object.keys(declared).filter((k) => !onProperty.includes(k))
  const word = outside[0]
  return outside.length === 1 && word !== undefined ? { [word]: declared[word] } : undefined
}

/** On a property, that it may be left out. */
export const OPTIONAL = 'optional'

/**
 * Whether that property may be left out. A property is written unless its declaration says
 * otherwise, so this is the one place the language's answer is read and both the checker and the
 * page ask it rather than deciding for themselves.
 */
export const mayBeLeftOut = (bodyOf: BodyOf, owner: string, name: string): boolean =>
  saysOfProperty(bodyOf, owner, name, OPTIONAL) === true

/**
 * Whether that property holds a type rather than a value of one. `of` on a collection holds a
 * type; `value` on a pattern holds a regular expression. Both are written the same way, so the
 * declaration of the property is the only thing that tells them apart, and the checker and the
 * page have to agree on it.
 */
export const holdsAType = (bodyOf: BodyOf, owner: string, name: string): boolean =>
  typeOfProperty(bodyOf, owner, name) === SELF.type

/** What a choice is one of, or nothing when the declaration is not a choice. */
export function optionsOf(bodyOf: BodyOf, id: string): readonly unknown[] | null {
  const written = bodyOf(id)?.[BOOTSTRAP.options]
  return Array.isArray(written) ? (written as unknown[]) : null
}

/**
 * The ids an enum lists, or nothing when the declaration is not an enum. A value written as a bare
 * id names the member of that id, so this is what a bare value is resolved against.
 */
export function membersOf(bodyOf: BodyOf, id: string): readonly string[] | null {
  const written = bodyOf(id)?.[VALUES]
  if (!Array.isArray(written)) return null
  return (written as unknown[]).flatMap((one) =>
    isMapping(one) && typeof one[BOOTSTRAP.id] === 'string' ? [one[BOOTSTRAP.id] as string] : [],
  )
}

/** Whether a type is, or derives from, another. */
export function derivesFrom(
  bodyOf: BodyOf,
  id: string,
  ancestor: string,
  path: readonly string[] = [],
): boolean {
  if (id === ancestor) return true
  const body = bodyOf(id)
  if (body === undefined || path.includes(id)) return false
  return namesOf(body[BOOTSTRAP.is]).some((parent) =>
    derivesFrom(bodyOf, parent, ancestor, [...path, id]),
  )
}

/** The one word a node names, or nothing when it names none or more than one. */
export function onlyWord(node: unknown): [string, unknown] | null {
  if (!isMapping(node)) return null
  const named = Object.entries(node)
  const first = named[0]
  return first === undefined || named.length !== 1 ? null : first
}

/** One entity declaration, once it has been read. */
interface Entity {
  readonly id: string
  /** What it derives from, in the order written. */
  readonly derives: readonly string[]
  /** The properties written on it, before anything it derives from is folded in. */
  readonly own: Claimed
}

const textList = (v: unknown): readonly string[] | null =>
  Array.isArray(v) && v.every((x) => typeof x === 'string' && x.length > 0)
    ? (v as readonly string[])
    : null

/** Whether everything written there is a non-empty name, so `namesOf` dropped nothing. */
const writtenAsNames = (v: unknown): boolean =>
  (Array.isArray(v) ? v : v === undefined ? [] : [v]).every((x) => typeof x === 'string' && x.length > 0)

const namesOf = (v: unknown): readonly string[] =>
  (Array.isArray(v) ? v : v === undefined ? [] : [v]).filter(
    (x): x is string => typeof x === 'string' && x.length > 0,
  )

/**
 * The vocabulary a document declares. Every declaration in the document's one list is read,
 * whether it is written plainly or inside `external`, because `external` says where a declaration
 * is implemented and not whether it is part of the language.
 *
 * It reports rather than throws, so a test can drive it with a document that is wrong and read
 * what it said. The scripts that load the language file are what refuse to run.
 */
export function resolveVocabulary(doc: unknown): Resolved {
  const faults: string[] = []
  const entities: Entity[] = []
  const choices = new Map<string, readonly string[]>()
  const declared: Record<string, Declared> = {}
  const seen = new Set<string>()

  const held = isMapping(doc) ? doc[BOOTSTRAP.declare] : undefined
  if (held !== undefined && !Array.isArray(held)) {
    faults.push(`${BOOTSTRAP.declare}: the declarations are a list`)
  }
  const items: unknown[] = Array.isArray(held) ? held : []

  const written = isMapping(doc) ? doc[SCOPE] : undefined
  const scope = namesOf(written)
  if (written !== undefined && !writtenAsNames(written)) {
    faults.push(`${SCOPE}: the scope is a list of names, written as text`)
  }

  for (const [i, item] of items.entries()) {
    const at = `${BOOTSTRAP.declare}[${i}]`
    const outer = onlyWord(item)
    if (outer === null) {
      faults.push(`${at}: a declaration is a mapping naming exactly one kind`)
      continue
    }
    const inner = outer[0] === BOOTSTRAP.external ? onlyWord(outer[1]) : outer
    if (inner === null) {
      faults.push(`${at}: ${BOOTSTRAP.external} is a mapping naming exactly one kind`)
      continue
    }

    // Every declaration is kept, whatever kind it is, because a document importing this scope
    // reaches all of them. Only an entity or a choice says anything about the vocabulary itself,
    // so only those two are read further. A declaration that cannot be kept is reported whatever
    // its kind, because one that is dropped without a word is one nobody can find missing.
    const [kind, body] = inner
    const says = kind === BOOTSTRAP.entity || kind === BOOTSTRAP.choice

    if (!isMapping(body)) {
      faults.push(`${at}: ${a(kind)} is a mapping`)
      continue
    }

    const id = body[BOOTSTRAP.id]
    if (typeof id !== 'string' || id.length === 0) {
      faults.push(`${at}: ${a(kind)} declares its name as its id, written as text`)
      continue
    }
    if (seen.has(id)) {
      faults.push(`${at}: "${id}" is declared twice`)
      continue
    }
    seen.add(id)
    declared[id] = { kind, body }
    if (!says) continue

    if (kind === BOOTSTRAP.choice) {
      const named = textList(body[BOOTSTRAP.options])
      if (named === null) faults.push(`${at}: "${id}" lists its options as names`)
      else choices.set(id, named)
      continue
    }

    let own: Claimed = null
    const written = body[BOOTSTRAP.properties]
    if (written !== undefined) {
      if (!isMapping(written)) faults.push(`${at}: "${id}" writes its properties as a mapping`)
      else own = Object.keys(written)
    }

    const derives = namesOf(body[BOOTSTRAP.is])
    if (!writtenAsNames(body[BOOTSTRAP.is])) {
      faults.push(`${at}: "${id}" names what it derives from as text`)
    }
    entities.push({ id, derives, own })
  }

  // What an entity carries is what it writes plus what everything it derives from writes. A type
  // that reaches itself through `is` is reported by the checker; here the walk simply stops, so a
  // file that does it is still read rather than hanging.
  const byId = new Map(entities.map((e) => [e.id, e]))
  const carried = (id: string, path: readonly string[]): Claimed => {
    const self = byId.get(id)
    if (self === undefined || path.includes(id)) return null
    const inherited: string[] = []
    let anyDecided = self.own !== null
    for (const parent of self.derives) {
      const from = carried(parent, [...path, id])
      if (from === null) continue
      anyDecided = true
      for (const word of from) if (!inherited.includes(word)) inherited.push(word)
    }
    if (!anyDecided) return null
    for (const word of self.own ?? []) if (!inherited.includes(word)) inherited.push(word)
    return inherited
  }

  const all: Record<string, Claimed> = {}
  for (const entity of entities) all[entity.id] = carried(entity.id, [])

  const options = (id: string): readonly string[] => choices.get(id) ?? []
  const external = options(SELF.external)
  const declare = options(SELF.declaration).filter((k) => k !== BOOTSTRAP.external)
  const kinds = [...new Set([...declare, ...external])]

  const onKind: Record<Kind, Claimed> = {}
  for (const kind of kinds) onKind[kind] = all[kind] ?? null

  for (const kind of options(SELF.type)) {
    if (!kinds.includes(kind)) faults.push(`"${SELF.type}" lists "${kind}", which is not a kind`)
  }

  return {
    words: {
      places: { declare, external },
      types: options(SELF.type),
      topLevel: all[SELF.document] ?? [],
      kinds,
      onKind,
      onProperty: all[SELF.property] ?? [],
      onValue: all[SELF.value] ?? [],
      entities: all,
      choices: Object.fromEntries(choices),
      scope: scope,
      declared,
    },
    faults,
  }
}

const a = (word: string): string => (/^[aeiou]/i.test(word) ? 'an' : 'a') + ' ' + word
