// The vocabulary a document is read against, loaded from files.
//
// A document is written in a language and brings in scopes. Both are files, and one rule reads
// all of them: the language the document names, the scopes that language brings in, and the
// scopes the document itself brings in. So a document written in MetaLang and a document written
// in a language MetaLang defines are loaded the same way, and neither is a special case.
//
// A file whose scope is `[a, b]` is the file `a/b.yaml`, looked for beside the document and then
// beside the language file. Where a file sits is the business of the filesystem rather than of
// the language, so that rule is here and nowhere else.
//
// The language a document names wins over everything it brings in, which is the rule a document
// is already read by: what is written here beats what is brought in.
//
// Nothing is read half way. A language that cannot be found, cannot be parsed, or says it lives
// somewhere else comes back as a fault with no vocabulary at all, because a document checked
// against a vocabulary that is partly guessed has not been checked.

import { existsSync, readFileSync } from 'node:fs'
import { basename, dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

import * as yaml from 'js-yaml'

import {
  importedBy,
  IMPORT,
  isMapping,
  LANGUAGE,
  resolveVocabulary,
  samePath,
  SCOPE,
  type Fault,
  type Vocabulary,
} from './words.ts'

/** Where the language file is. It declares the vocabulary every script reads. */
export const LANGUAGE_FILE = join(dirname(fileURLToPath(import.meta.url)), 'metalang.yaml')

/**
 * The vocabulary a language file declares. It refuses to produce one it could not read, so no
 * script ever runs on a vocabulary that is partly guessed.
 */
export function loadVocabulary(file: string = LANGUAGE_FILE): Vocabulary {
  const { words, faults } = resolveVocabulary(yaml.load(readFileSync(file, 'utf8')))
  if (faults.length > 0) throw new Error(`metalang-words: ${faults.join(' ')}`)
  return words
}

/** The live vocabulary, read once. */
export const WORDS: Vocabulary = loadVocabulary()

/** A scope, written the way a person says it. */
export const nameOfScope = (scope: readonly unknown[]): string => scope.join('.')

/** The vocabulary a document is read against, or nothing when it could not be read whole. */
export interface Rulebook {
  /** The vocabulary, or nothing when a language it needs could not be read. */
  readonly words: Vocabulary | null
  /** Everything that stopped it being read as written. */
  readonly faults: readonly Fault[]
  /**
   * Every document this read to build the vocabulary, deepest first: everything a language brings
   * in comes before the language itself. The document given is not among them.
   *
   * Reading a scope is this function's job and nobody else's. Anything that needs the documents a
   * scope reaches takes them from here rather than walking the imports a second time.
   */
  readonly documents: readonly unknown[]
}

/**
 * The file a scope is written in: its segments as a path, with `.yaml` on the end, looked for in
 * each of the given directories in turn. Nothing when no such file is there.
 */
export function fileForScope(
  scope: readonly string[],
  beside: readonly string[],
): string | null {
  if (scope.length === 0) return null
  for (const dir of beside) {
    const file = `${join(dir, ...scope)}.yaml`
    if (existsSync(file)) return file
  }
  return null
}

/** Every segment of a scope, as text, or nothing when one of them is not. */
const scopeNamed = (written: unknown): readonly string[] | null => {
  if (!Array.isArray(written)) return null
  const names = written.filter((one): one is string => typeof one === 'string' && one.length > 0)
  return names.length === written.length && names.length > 0 ? names : null
}

/**
 * The vocabulary this document is read against: the language it names, every scope that language
 * brings in, and every scope the document itself brings in.
 *
 * A document naming no language is written in MetaLang, which is read the same way as any other
 * language rather than assumed, so the one rule covers it.
 */
export function rulebookFor(doc: unknown, file: string): Rulebook {
  const faults: Fault[] = []
  const root = basename(file)
  const beside = [dirname(file), dirname(LANGUAGE_FILE)]

  /** Every document read, by the scope it says it is, so a scope is read once and a cycle ends. */
  const read = new Map<string, unknown>()
  /** The documents, deepest first: everything a language brings in is read before the language. */
  const order: unknown[] = []

  const load = (scope: readonly string[], where: string): unknown => {
    const key = nameOfScope(scope)
    const already = read.get(key)
    if (already !== undefined) return already

    const found = fileForScope(scope, beside)
    if (found === null) {
      faults.push({
        where,
        what:
          `"${key}" names no file this checker could find — it would be` +
          ` ${beside.map((dir) => `${join(dir, ...scope)}.yaml`).join(' or ')}`,
      })
      return null
    }

    let loaded: unknown
    try {
      loaded = yaml.load(readFileSync(found, 'utf8'))
    } catch (e) {
      const message = e instanceof Error ? e.message : String(e)
      faults.push({ where, what: `"${key}" is ${basename(found)}, which cannot be read — ${message.split('\n')[0]}` })
      return null
    }

    const says = scopeNamed(isMapping(loaded) ? loaded[SCOPE] : undefined)
    if (says === null || !samePath(says, scope)) {
      faults.push({
        where,
        what:
          `${basename(found)} is where "${key}" would be, and it says its scope is` +
          ` "${says === null ? '' : nameOfScope(says)}"`,
      })
      return null
    }

    // Kept before what it brings in is read, so a scope that reaches round to itself ends here
    // rather than being read for ever.
    read.set(key, loaded)
    bringsIn(loaded, key)
    order.push(loaded)
    return loaded
  }

  /**
   * Every scope one document brings in, read deepest first. A scope written as something other
   * than a list of names is left to the checker, which reports the shape where it is written;
   * guessing one here would bring in a scope nobody asked for.
   */
  function bringsIn(from: unknown, within: string): void {
    for (const [i, one] of importedBy(from).entries()) {
      const names = scopeNamed(one)
      if (names !== null) load(names, `${within}.${IMPORT}[${i}]`)
    }
  }

  const named = isMapping(doc) ? doc[LANGUAGE] : undefined
  const names = typeof named === 'string' && named.length > 0 ? named : null
  const base = load(names === null ? WORDS.scope : [names], names === null ? root : `${root}.${LANGUAGE}`)
  if (base === null) return { words: null, faults, documents: order }

  if (isMapping(base) && base[LANGUAGE] !== undefined) {
    faults.push({
      where: `${root}.${LANGUAGE}`,
      what:
        `"${names ?? ''}" is itself written in another language, and a language read through` +
        ' another language cannot be read yet',
    })
    return { words: null, faults, documents: order }
  }

  // The document's own imports go on top of the language's, and the language still wins over all
  // of them, so it is the one read last.
  bringsIn(doc, root)

  const { words, faults: said } = resolveVocabulary(
    base,
    order.filter((one) => one !== base),
  )
  for (const what of said) faults.push({ where: nameOfScope(words.scope), what })
  return { words, faults, documents: order }
}
