// What the reader makes of what is written: how a value of each primitive is read, and what a type
// says its values may be once it has been read.
//
// The language says that a primitive exists, that a constraint exists, what it holds, and which
// type carries it. What a value of a primitive LOOKS LIKE, and what a kind of constraint MEANS —
// that a pattern is matched against the whole value — are here, one piece per primitive and one
// per kind of constraint. Both are statements about the concrete syntax rather than about the
// vocabulary, and both are the place where the language stops and the reader begins. They are in
// one file because they answer one question between them, and because the checker and the
// colouring both ask it: a value one of them reads and the other does not is a value drawn as
// something the report calls a problem.
//
// A kind of constraint the language declares and nothing here applies is reported rather than
// passed over, because a value nothing judged is not a value that was checked.
//
// Nothing here reaches the file system, so the page's bundle may import it.

import { readExpression } from './expression.ts'
import { BOOTSTRAP, isMapping, onlyWord, type BodyOf, type Mapping } from './words.ts'

/** The keyword a type carries its constraints under. */
export const CONSTRAINTS = 'constraints'

/** The written form of a whole number, positive or negative, with or without a fractional part. */
const NUMERAL = /^-?\d+(\.\d+)?$/

/** How a value of one primitive is read. */
export interface Reader {
  /** Whether a value already read out of a file is one of this primitive. */
  readonly is: (value: unknown) => boolean
  /**
   * What one bare word carries, held in `read`, or nothing when the word is not a value of this
   * primitive at all. A primitive written as a set of named parts carries nothing as one word.
   */
  readonly from: (written: string) => { readonly read: unknown } | null
}

/**
 * What one value of each primitive looks like once the file has been read, and what one bare word
 * written in a file carries. In a YAML file `true` arrives as a boolean, `"a"` as text, `7` as a
 * number, a regular expression as text and a set of named parts as a mapping, and another syntax
 * would spell them differently without the language changing. A primitive with no entry here
 * cannot have its values read, which is said rather than passed over.
 *
 * `object` is the root every type derives from, so that row is what a value of any type that is
 * not one of the leaves looks like. Which parts it may carry and what each one holds is said in
 * the language, under `properties`, and is read from there rather than from here.
 *
 * What a value of a primitive may be, once it has been read, is the business of the constraints
 * the type carries. That is said in the language, so it is below rather than here.
 */
const numeral = (written: string): { read: number } | null =>
  NUMERAL.test(written) ? { read: Number(written) } : null

const text = (written: string): { read: string } => ({ read: written })

const READERS: Record<string, Reader> = {
  object: { is: isMapping, from: () => null },
  boolean: {
    is: (v) => typeof v === 'boolean',
    from: (w) => (w === 'true' ? { read: true } : w === 'false' ? { read: false } : null),
  },
  string: { is: (v) => typeof v === 'string', from: text },
  number: { is: (v) => typeof v === 'number', from: numeral },
  expression: { is: (v) => typeof v === 'string', from: text },
  byte: { is: (v) => typeof v === 'number', from: numeral },
  uuid: { is: (v) => typeof v === 'string', from: text },
}

/** How a value of one primitive is read, or nothing when nothing here says. */
export const readerFor = (primitive: string): Reader | null => READERS[primitive] ?? null

/** One constraint written on a type, and which type wrote it. */
export interface Constraint {
  /** The id of the type that wrote it. */
  readonly from: string
  /** Where it sits in that type's constraints, so it can be named where it is written. */
  readonly index: number
  /** The kind of constraint it names. */
  readonly kind: string
  /** What is written inside that word. */
  readonly body: unknown
}

/**
 * Every constraint a type carries, its own first and then the ones it derives, so the first thing
 * a value fails is the closest thing to it. A type deriving from `name` is held to the characters
 * a name may contain as well as to whatever it says itself.
 *
 * A constraint that does not name one word is left out here and reported where it is written.
 */
export function constraintsOn(
  bodyOf: BodyOf,
  id: string,
  path: readonly string[] = [],
): readonly Constraint[] {
  const body = bodyOf(id)
  if (body === undefined || path.includes(id)) return []

  const out: Constraint[] = []
  const written = body[CONSTRAINTS]
  if (Array.isArray(written)) {
    for (const [index, one] of (written as unknown[]).entries()) {
      const named = onlyWord(one)
      if (named !== null) out.push({ from: id, index, kind: named[0], body: named[1] })
    }
  }

  const held = body[BOOTSTRAP.is]
  const above: unknown[] = Array.isArray(held) ? held : held === undefined ? [] : [held]
  for (const parent of above) {
    if (typeof parent === 'string') out.push(...constraintsOn(bodyOf, parent, [...path, id]))
  }
  return out
}

/** What applying one kind of constraint means. */
export interface Logic {
  /** Why the constraint cannot be applied as it is written, or nothing when it can. */
  readonly fault: (body: Mapping) => string | null
  /**
   * Why the constraint cannot judge a value even though `fault` found nothing to report where it
   * is written: the property's own type reports a wrong kind of thing there, and this is what
   * stops a value being passed against it meanwhile.
   */
  readonly unusable?: (body: Mapping) => string | null
  /** Why a value does not meet it, or nothing when it does. */
  readonly unmet: (body: Mapping, value: unknown) => string | null
}

// A pattern holds its regular expression under `value`, and the whole of the value has to match.
// A constraint whose own expression could not be read cannot judge a value, and that is said at
// every value it would have judged: a value passed because its constraint was broken is a value
// nobody checked.
const LOGIC: Record<string, Logic> = {
  pattern: {
    // What is written there being text at all is the type of the property saying so, and it is
    // already reported as that, so the only thing left to say here is whether the text reads.
    fault: (body) => (typeof body.value === 'string' ? readExpression(body.value).fault : null),
    unusable: (body) => readExpression(body.value).fault,
    unmet: (body, value) => {
      const { expression } = readExpression(body.value)
      if (expression === null) throw new Error('a pattern with no readable expression was applied')
      if (typeof value !== 'string') return 'is not text, so no pattern can be matched against it'
      return expression.matches(value) ? null : `does not match ${expression.source}`
    },
  },

  // A range holds the lowest and the highest value allowed. Either may be left off, and a range
  // that leaves both off allows everything, which is a range that was not worth writing.
  range: {
    fault: (body) => {
      const { minimum, maximum } = body
      if (minimum === undefined && maximum === undefined) return 'says neither a minimum nor a maximum'
      if (typeof minimum === 'number' && typeof maximum === 'number' && minimum > maximum) {
        return `allows nothing: its minimum ${minimum} is above its maximum ${maximum}`
      }
      return null
    },
    unmet: (body, value) => {
      const { minimum, maximum } = body
      if (typeof value !== 'number') return 'is not a number, so no range can be read against it'
      if (typeof minimum === 'number' && value < minimum) return `is below ${minimum}`
      if (typeof maximum === 'number' && value > maximum) return `is above ${maximum}`
      return null
    },
  },
}

/** How a kind of constraint is applied, or nothing when nothing here says. */
export const logicFor = (kind: string): Logic | null => LOGIC[kind] ?? null

/**
 * Why a constraint cannot be applied to any value, or nothing when it can. A value that meets a
 * constraint nothing could apply has not been checked, so the caller reports this rather than
 * treating the value as fine.
 */
export function cannotApply(constraint: Constraint): string | null {
  const logic = logicFor(constraint.kind)
  if (logic === null) {
    return `nothing says how ${constraint.kind} is applied, so no value can be checked against it`
  }
  if (!isMapping(constraint.body)) {
    return `${constraint.kind} on "${constraint.from}" is not written as a mapping, so no value can be checked against it`
  }
  const fault = logic.fault(constraint.body) ?? logic.unusable?.(constraint.body) ?? null
  return fault === null ? null : `${constraint.kind} on "${constraint.from}" ${fault}, so no value can be checked against it`
}

/**
 * Why a value does not meet one constraint, or nothing when it does. It refuses a constraint that
 * `cannotApply` names, because answering "met" for one nothing could judge is a silent pass.
 */
export function unmetBy(constraint: Constraint, value: unknown): string | null {
  const logic = logicFor(constraint.kind)
  const cannot = cannotApply(constraint)
  if (logic === null || cannot !== null || !isMapping(constraint.body)) {
    throw new Error(`unmetBy was asked to judge a constraint that cannot be applied: ${cannot}`)
  }
  return logic.unmet(constraint.body, value)
}
