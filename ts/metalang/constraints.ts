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
import { BOOTSTRAP, isMapping, onlyWord, RESERVED, type BodyOf, type Mapping } from './words.ts'

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

/**
 * What a path came to: the values it reached, or why the walk could not be made at all. A route
 * nothing could follow is said rather than answered with an empty hand, because a rule that found
 * nothing to judge and a rule that could not look are not the same answer.
 */
export type Reached = { readonly reached: readonly unknown[] } | { readonly stopped: string }

/**
 * What one declaration carries, by the name it is known by, or nothing where the name names no
 * declaration the document reaches. It is what lets a path cross from a thing to the thing it
 * points at; without one a path descends only through content a value owns.
 */
export type Resolve = (name: string) => Mapping | undefined

/**
 * Every value a path reaches, in the order it reaches them. Each part names a property of whatever
 * the part before it held; a part holding many of something fans out, so one path reaching through
 * a list names the same part of every member. A part nothing is written at reaches nothing, which
 * is an empty answer and not a wrong one.
 *
 * It descends only through content a value owns. Ownership forms a tree, so the walk always ends
 * and nothing here has to count its own steps.
 */
export function reachedBy(value: unknown, path: string, through?: Resolve): Reached {
  let here: unknown[] = [value]
  // Every declaration the route has already been through. Two declarations are free to name each
  // other, so this is what makes a route that crosses them end: a name it has already followed is
  // where it stops, rather than where it goes round again.
  const crossed = new Set<string>()

  for (const part of path.split('.')) {
    const next: unknown[] = []
    for (const one of here) {
      // Nothing written there is an empty answer and not a wrong one, so the walk goes on with
      // one fewer thing in hand. A word standing in place of a value says the same: a part that
      // was deliberately left empty, or that nobody has written yet, holds nothing to take a part
      // of, and reading it as a name to follow would make an absence look like a broken route.
      if (one === undefined || one === null) continue
      if (one === RESERVED.none || one === RESERVED.pending) continue

      // A part naming another declaration holds the name, not the thing. The route asks for a
      // part of it, so the name is followed to what that declaration carries and the walk goes on
      // from there.
      let at = one
      if (!isMapping(at) && typeof at === 'string' && through !== undefined) {
        if (crossed.has(at)) {
          return { stopped: `cannot be read at "${path}", because it returns to "${at}", which it has already been through` }
        }
        const found = through(at)
        if (found !== undefined) {
          crossed.add(at)
          at = found
        }
      }

      // Something written there that has no named parts, and that names no declaration either, is
      // the end of the road: the route asks for a part of it and there is no way to take one, so
      // it stops and says so rather than coming back empty as though the part had been left out.
      if (!isMapping(at)) {
        return {
          stopped: `cannot be read at "${path}", because "${part}" is asked of something with no named parts`,
        }
      }
      const held = at[part]
      if (held === undefined) continue
      if (Array.isArray(held)) next.push(...(held as unknown[]))
      else next.push(held)
    }
    here = next
  }
  return { reached: here }
}

/** One value as the word a constraint compares it against. */
const asWord = (value: unknown): string => (typeof value === 'string' ? value : JSON.stringify(value))

/** The values one constraint names, or nothing when what is written there is not a list of them. */
const wordsOf = (written: unknown): readonly string[] | null =>
  Array.isArray(written) && (written as unknown[]).every((one) => typeof one === 'string')
    ? (written as string[])
    : null

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
  /**
   * Why a value does not meet it, or nothing when it does. `through` is how a name is followed to
   * the declaration it points at, for the constraints that walk a path; one given none descends
   * only through content the value owns.
   */
  readonly unmet: (body: Mapping, value: unknown, through?: Resolve) => string | null
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

  // Distinct names a part of the value holding many of something, and a part of each of those. No
  // two of them may carry the same value there. A property's named parts are declared as a list and
  // written as a block keyed by name, so both are read; the key is the name a member is reported
  // by when it has none of its own. A member leaving the part out is passed over, because an
  // absence is not a value two things can share, and whether it may be left out at all is what the
  // property's own `optional` says.
  distinct: {
    fault: (body) => {
      const { in: held, by } = body
      if (typeof held !== 'string' || held.length === 0) return 'does not name the part it reads'
      if (typeof by !== 'string' || by.length === 0) return 'does not name the part no two may share'
      return null
    },
    unmet: (body, value) => {
      const held = body.in as string
      const by = body.by as string
      if (!isMapping(value)) return `is not a set of named parts, so "${held}" cannot be read from it`
      const written = value[held]
      // Written as neither is the property's own type to report, and it does, so there is nothing
      // here to judge rather than something that quietly passed.
      const members: readonly (readonly [string | null, unknown])[] = Array.isArray(written)
        ? (written as unknown[]).map((one) => [null, one] as const)
        : isMapping(written)
          ? Object.entries(written)
          : []

      const taken = new Map<string, string>()
      for (const [key, member] of members) {
        if (!isMapping(member)) continue
        const carried = member[by]
        if (carried === undefined) continue
        const word = typeof carried === 'string' ? carried : JSON.stringify(carried)
        const named = key ?? (typeof member[BOOTSTRAP.id] === 'string' ? (member[BOOTSTRAP.id] as string) : '')
        const first = taken.get(word)
        if (first !== undefined) {
          const both = [first, named].filter((one) => one !== '')
          return (
            `writes "${word}" at "${by}" on more than one of "${held}"` +
            (both.length === 2 ? ` — ${both[0]} and ${both[1]}` : '')
          )
        }
        taken.set(word, named)
      }
      return null
    },
  },

  // A prohibition. Nothing the path reaches may be one of the words written here.
  never: {
    fault: (body) => {
      if (typeof body.at !== 'string' || body.at.length === 0) return 'does not name the part it is about'
      return wordsOf(body.carries) === null ? 'does not say which values are refused' : null
    },
    unmet: (body, value, through) => {
      const carries = wordsOf(body.carries) ?? []
      const came = reachedBy(value, body.at as string, through)
      if ('stopped' in came) return came.stopped
      for (const found of came.reached) {
        const word = asWord(found)
        if (carries.includes(word)) return `writes "${word}" at "${body.at as string}", which it may never carry`
      }
      return null
    },
  },

  // A condition. The constraint it holds is applied only where the part it names carries one of
  // these, and is not applied at all where that part reaches nothing.
  when: {
    fault: (body) => {
      if (typeof body.at !== 'string' || body.at.length === 0) return 'does not name the part that decides it'
      if (wordsOf(body.holds) === null) return 'does not say which values make it apply'
      const then = onlyWord(body.then)
      if (then === null) return 'does not hold exactly one constraint to apply'
      const inner = logicFor(then[0])
      if (inner === null) return `holds ${then[0]}, and nothing says how that is applied`
      if (!isMapping(then[1])) return `holds ${then[0]} written as something other than a mapping`
      const fault = inner.fault(then[1]) ?? inner.unusable?.(then[1]) ?? null
      return fault === null ? null : `holds ${then[0]}, which ${fault}`
    },
    unmet: (body, value, through) => {
      const holds = wordsOf(body.holds) ?? []
      const came = reachedBy(value, body.at as string, through)
      if ('stopped' in came) return came.stopped
      const applies = came.reached.some((found) => holds.includes(asWord(found)))
      if (!applies) return null
      const then = onlyWord(body.then)
      if (then === null || !isMapping(then[1])) throw new Error('a when with no readable constraint was applied')
      const inner = logicFor(then[0])
      if (inner === null) throw new Error(`a when holding ${then[0]} was applied with nothing to apply`)
      const why = inner.unmet(then[1], value, through)
      return why === null ? null : `${why}, where "${body.at as string}" is ${holds.map((one) => `"${one}"`).join(' or ')}`
    },
  },

  // How many of a thing a part holds. It counts what the path reaches and hands the count to a
  // range, which is already the word for a lowest and a highest, rather than saying one again.
  many: {
    fault: (body) => {
      if (typeof body.at !== 'string' || body.at.length === 0) return 'does not name the part it counts'
      if (!isMapping(body.bounds)) return 'does not say how many are allowed'
      const bounds = LOGIC.range?.fault(body.bounds) ?? null
      return bounds === null ? null : `says how many as a range that ${bounds}`
    },
    unmet: (body, value, through) => {
      const came = reachedBy(value, body.at as string, through)
      if ('stopped' in came) return came.stopped
      const bounds = body.bounds as Mapping
      const off = LOGIC.range?.unmet(bounds, came.reached.length) ?? null
      if (off === null) return null
      const many = came.reached.length
      return `holds ${many} at "${body.at as string}", which ${off}`
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
export function unmetBy(constraint: Constraint, value: unknown, through?: Resolve): string | null {
  const logic = logicFor(constraint.kind)
  const cannot = cannotApply(constraint)
  if (logic === null || cannot !== null || !isMapping(constraint.body)) {
    throw new Error(`unmetBy was asked to judge a constraint that cannot be applied: ${cannot}`)
  }
  return logic.unmet(constraint.body, value, through)
}
