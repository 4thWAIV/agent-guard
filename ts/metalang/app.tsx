// The page, in React. Draw is the only path from data to page.
//
// TWO RULES, and the error control is how both are enforced.
//
// One. A component is chosen by what the node IS. Nothing matches, an error draws.
// Two. A component must ACCOUNT FOR every key on the node it draws. Whatever it does not
//      claim draws as an error beside it.
//
// So a word the language gains and this page does not handle appears on the page as an error,
// where it can be seen, rather than being quietly left out. Nothing is omitted silently.
//
// Nothing here is keyed on a particular kind. A kind's colour comes from its position in the
// vocabulary, so a kind added to the language draws in a colour of its own without this file
// being touched. The earlier page hand-wrote one rule per actor and a sixth actor drew in no
// colour and said nothing about it.
//
// The vocabulary and the document arrive as props rather than as build-time globals, so the
// whole page can be drawn from a test against a document built in memory.

import { createContext, Fragment, useContext, type CSSProperties, type ReactNode } from 'react'

import * as yaml from 'js-yaml'

import { CONSTRAINTS, constraintsOn } from './constraints.ts'
import { NO_MARKS, pathTo, type Marks } from './highlight.ts'
import { partsOf, type Part } from './syntax.ts'
import {
  BOOTSTRAP,
  declaredBodiesOf,
  derivesFrom,
  holdsAType,
  hueOfKind,
  IMPORT,
  importedBy,
  importsScope,
  ACCEPTS,
  acceptsOf,
  LANGUAGE,
  isMapping,
  membersOf,
  NOTHING_FOUND,
  onlyWord,
  primitiveOf,
  propertiesCarrying,
  propertiesOf,
  propertyAbove,
  propertyCarrying,
  ROLE,
  ROLES,
  samePath,
  SCOPE,
  SELF,
  typeOfOwnValue,
  typeOfPart,
  typeOfProperty,
  VALUE,
  valuesOf,
  type BodyOf,
  type CheckResult,
  type Fault,
  type Mapping,
  type Vocabulary,
} from './words.ts'

const list = (xs: readonly string[]): string => xs.join(', ')

// What to write for a value the page draws as text. Text is written as it stands; anything
// else is written as what it is, because "[object Object]" on the page says nothing at all.
export const text = (v: unknown): string => {
  if (typeof v === 'string') return v
  // JSON.stringify is typed as returning a string and returns undefined for these two.
  if (v === undefined) return 'nothing'
  if (typeof v === 'function') return 'a function'
  return JSON.stringify(v)
}

// ── what is said about a node ────────────────────────────────────────────────
//
// Two things point at a node by its path: a highlight file, which says what to look at, and the
// checker, which says what it found. The page draws both the same way, as a mark around whatever
// the node draws as, because a reader looking at a value wants to know what is said about THAT
// value wherever it was said.

/** Which list a thing said about a node came from. A highlight is not one of the checker's. */
type From = 'highlight' | 'problem' | 'blocker' | 'pending'

/** One thing said about one node: what it says, and which list it came from. */
export interface Said {
  readonly note: string
  readonly from: From
}

/** One thing the checker found, and the node on the page it is about. */
export interface Finding {
  readonly fault: Fault
  readonly from: 'problem' | 'blocker' | 'pending'
  /**
   * The path the page writes for the node it is about, or nothing when it names no node here —
   * which is what a fault about the whole file names. One of those is said in the band at the
   * top of the page and nowhere else, because there is no node to put it beside.
   */
  readonly at: string | null
}

/** Everything the checker found, each placed on the node it is about. */
export function findingsIn(doc: unknown, found: CheckResult): readonly Finding[] {
  const placed = (from: 'problem' | 'blocker' | 'pending') => (fault: Fault): Finding => ({
    fault,
    from,
    at: pathTo(doc, fault.where),
  })
  return [
    ...found.problems.map(placed('problem')),
    ...found.blockers.map(placed('blocker')),
    ...found.pending.map(placed('pending')),
  ]
}

/**
 * What is said about each node, by the path the page writes for that node. A fault is placed by
 * the same path form a highlight is placed by, which is why a path copied out of a report lands
 * on the node the report was about.
 */
export function saidAbout(
  marked: ReadonlyMap<string, string>,
  findings: readonly Finding[],
): ReadonlyMap<string, readonly Said[]> {
  const said = new Map<string, Said[]>()
  const add = (at: string, one: Said): void => {
    const held = said.get(at)
    if (held === undefined) said.set(at, [one])
    else held.push(one)
  }
  for (const finding of findings) {
    if (finding.at !== null) add(finding.at, { note: finding.fault.what, from: finding.from })
  }
  for (const [at, note] of marked) add(at, { note, from: 'highlight' })
  return said
}

/**
 * The colour a mark takes, which is the worst thing said at it. A mark with only a highlight on
 * it takes none of its own and draws in the colour a highlight has always drawn in.
 */
const colourOf = (says: readonly Said[]): string | null =>
  says.some((s) => s.from === 'problem')
    ? 'm-problem'
    : says.some((s) => s.from === 'blocker')
      ? 'm-blocker'
      : says.some((s) => s.from === 'pending')
        ? 'm-pending'
        : null

/** One class name made of the parts of it that there are. */
const classes = (...parts: readonly (string | null)[]): string =>
  parts.filter((p) => p !== null).join(' ')

// ── what the whole page is drawing against ───────────────────────────────────
interface Drawing {
  readonly words: Vocabulary
  /**
   * Every declaration this document writes, by the id it declares. A reference is drawn as
   * resolving or not by whether its name is in here, and a property written again on a type
   * reads the declaration above it through the same map.
   */
  readonly declared: ReadonlyMap<string, Mapping>
  /**
   * The scope a name this document does not declare is reached through, written the way the
   * document writes it, or nothing when the document reaches no such name. A document reaches a
   * scope by bringing it in, and reaches the language it names without bringing it in.
   */
  readonly brought: (name: string) => string | null
  /**
   * What is said about each node, by the path of the node it is said about. A highlight file and
   * the checker both point at a node by its path, and both are drawn as a mark around whatever
   * that node draws as, so one map holds both and every drawing site asks one question. Empty
   * where there is no highlight and nothing was found, which is how most documents draw.
   */
  readonly said: ReadonlyMap<string, readonly Said[]>
  /**
   * The path of every mark the page has drawn so far. The page says at the end which marks
   * nothing drew, and this is how it knows: a mark is drawn in one place, and the part of the
   * page that reports the rest is drawn after every part that could have drawn one.
   */
  readonly drawn: Set<string>
  /** Everything the checker found, in the order the report prints it. */
  readonly findings: readonly Finding[]
}

const DrawingContext = createContext<Drawing | null>(null)

const useDrawing = (): Drawing => {
  const drawing = useContext(DrawingContext)
  if (drawing === null) throw new Error('a component drew outside the page')
  return drawing
}

/**
 * How a name written in the document is looked up: what the document declares, and then what the
 * scope it imports declares. A document's own declaration wins over an imported one, which is the
 * rule the checker reads by.
 */
const useBodyOf = (): BodyOf => {
  const { words, declared, brought } = useDrawing()
  return (id: string) =>
    declared.get(id) ?? (brought(id) === null ? undefined : words.declared[id]?.body)
}

/**
 * What is said of the node drawn at this path, or nothing when nothing is said of it. A node is
 * marked by where it sits, so nothing on the node itself says it is marked and no kind holds a
 * list of what may be.
 */
const useSaid = (at: string): readonly Said[] | null => {
  const { said } = useDrawing()
  return said.get(at) ?? null
}

/**
 * The path an enclosing mark has already drawn. Two components can draw the same node — a part
 * of a value draws the name beside the value, and the value draws itself — and one node carries
 * one mark, so the outer one wins and the inner one leaves it alone.
 */
const MarkedAbove = createContext<string | null>(null)

/** The lines a mark carries: everything said at it, each on its own line. */
const Notes = ({ says }: { says: readonly Said[] }) => (
  <>
    {says.map((one, i) =>
      one.note === '' ? null : (
        <p key={i} className="d-note">
          {one.note}
        </p>
      ),
    )}
  </>
)

/**
 * A node something is said about. The mark goes around whatever the node draws as, so every kind
 * is markable and none of them knows it. It carries the path as its id, so the band at the top
 * of the page can point at it.
 *
 * Drawing a mark writes the path into the set the page holds. That is a change made while the
 * page is being drawn, and it is deliberate: it is the only way the page can say at the end
 * which marks nothing drew, and the part that says so is drawn after every part that draws one.
 */
const Marked = ({ at, children }: { at: string; children: ReactNode }) => {
  const says = useSaid(at)
  const { drawn } = useDrawing()
  const above = useContext(MarkedAbove)
  if (says === null || at === above) return <>{children}</>
  drawn.add(at)
  return (
    <MarkedAbove.Provider value={at}>
      <div className={classes('d-marked', colourOf(says))} id={at}>
        {children}
        <Notes says={says} />
      </div>
    </MarkedAbove.Provider>
  )
}

// A hue per kind, spread evenly round the wheel by position. Any number of kinds, no gaps.
// oklch rather than hsl because hsl at one lightness goes muddy through the yellows and
// greens, so a kind's colour would carry weight it did not mean.
export function kindColour(kind: string, kinds: readonly string[]): string | null {
  const hue = hueOfKind(kind, kinds)
  return hue === null ? null : `oklch(54% 0.17 ${hue})`
}

export function kindStyle(kind: string, kinds: readonly string[]): CSSProperties | undefined {
  const colour = kindColour(kind, kinds)
  return colour === null ? undefined : ({ '--kind': colour } as CSSProperties)
}

/**
 * A value written out as YAML, coloured by the one control that colours every example of this
 * language — the same reading a page written by hand draws from, so an example on a page and an
 * example inside the language never read differently.
 *
 * What the keys mean is read against the type the value is written against. Where nothing says
 * what that type is, the parts say so rather than drawing a guess.
 */
const Yaml = ({ value, type }: { value: unknown; type?: string | null }) => {
  const { words, declared } = useDrawing()
  const written = yaml.dump(value, { lineWidth: 88, noRefs: true })
  const parts = partsOf(written, words, {
    declared,
    ...(typeof type === 'string' && type.length > 0 ? { inside: type } : {}),
  })
  return (
    <pre className="y">
      {parts.map((part: Part, i: number) =>
        part.is === 'gap' ? (
          <Fragment key={i}>{part.text}</Fragment>
        ) : (
          <span
            key={i}
            className={`y-${part.is}`}
            style={part.kind === undefined ? undefined : kindStyle(part.kind, words.kinds)}
          >
            {part.text}
          </span>
        ),
      )}
    </pre>
  )
}

// ── the error control ────────────────────────────────────────────────────────
const Err = ({
  at,
  loose,
  label = 'cannot draw',
  children,
}: {
  at: string
  loose?: boolean
  label?: string
  children: ReactNode
}) => (
  <div className={loose === true ? 'd-err d-err-loose' : 'd-err'}>
    <em>{label}</em>
    <b>{children}</b>
    <code>{at}</code>
  </div>
)

// Every key the caller has no drawing for. Whether the document is right is the checker's
// question, not this page's, so there is one thing to say here and the page says it.
const Unclaimed = ({
  at,
  node,
  drawn,
}: {
  at: string
  node: unknown
  drawn: readonly string[]
}) => (
  <>
    {Object.keys(isMapping(node) ? node : {})
      .filter((k) => !drawn.includes(k))
      .map((k) => (
        <Err key={k} at={`${at}.${k}`} label="incomplete">
          &quot;{k}&quot; is an incomplete language feature. There is nothing to draw until what
          it holds is decided.
        </Err>
      ))}
  </>
)

// What each component has a drawing for, kept apart from what the language approves. The two
// lists coincide today everywhere but the file, and keeping them separate is what makes a word
// the language gains ahead of this page show up instead of quietly vanishing.
//
// These are the keywords with a drawing of their OWN, because their shape is more than the
// keyword's type says. Every other keyword a kind carries draws from what the language says it
// holds: a reference where it holds a type, and a value where it holds one. A keyword the
// language has not said what it holds draws as the incomplete feature it is.
const TITLE = 'title'
const DESCRIPTION = 'description'
const OPTIONAL = 'optional'

// One line saying what a thing is, and prose explaining it. Both are written the same way on a
// declaration, on one of its properties and on one of an enum's values, so one list names them.
const PROSE: readonly string[] = [TITLE, DESCRIPTION]

const DRAWN_ON_KIND: readonly string[] = [
  'id',
  'is',
  ...PROSE,
  'abstract',
  'primitive',
  CONSTRAINTS,
  'properties',
  VALUE,
  'values',
  'options',
]

// What a type says about itself rather than about its properties. Each is drawn only when the
// declaration writes it true, because writing it false says the same as leaving it off.
const SAID_OF_TYPE: readonly string[] = ['abstract', 'primitive']
const DRAWN_ON_PROPERTY: readonly string[] = ['is', ...PROSE, OPTIONAL, ACCEPTS, ROLE, VALUE]
const DRAWN_ON_VALUE: readonly string[] = ['id', ...PROSE, VALUE]

// ── the prose a thing carries ────────────────────────────────────────────────
//
// A thing that says neither draws nothing at all, so a file that carries no prose costs no
// height. On a property or a value it is drawn under the second column, where what the row says
// about that name already is.
const Prose = ({
  node,
  row = false,
  line,
}: {
  node: unknown
  row?: boolean
  /**
   * The property a language says is the prose explaining the thing, drawn first and in the same
   * place, because a reader looking for what a thing is looks under its heading whichever word
   * the language spells it with.
   */
  line?: ReactNode
}) => {
  const title = isMapping(node) ? node[TITLE] : undefined
  const description = isMapping(node) ? node[DESCRIPTION] : undefined
  if (title === undefined && description === undefined && line === undefined) return null
  return (
    <div className={row ? 'd-prose d-prose-row' : 'd-prose'}>
      {line ?? null}
      {title !== undefined ? <b>{text(title)}</b> : null}
      {description !== undefined ? <p>{text(description)}</p> : null}
    </div>
  )
}

// ── a reference to a type ────────────────────────────────────────────────────
//
// Three channels, because colour alone excludes anyone who cannot tell these two apart: a
// shape, a word, and the colour. The shape and the word each carry the whole meaning on their
// own, so the colour is the one that can be lost without losing anything.
const Ref = ({ name }: { name: string }) => {
  const { declared, brought } = useDrawing()
  const from = brought(name)
  if (declared.has(name)) {
    return (
      <span className="d-ref r-declared" title="declared in this document">
        <b aria-hidden="true">&#9670;</b>
        {name}
      </span>
    )
  }
  if (from !== null) {
    return (
      <span className="d-ref r-imported" title={`declared in ${from}, which this document reaches`}>
        <b aria-hidden="true">&#9672;</b>
        {name}
        <i>{from}</i>
      </span>
    )
  }
  return (
    <span className="d-ref r-undeclared" title="no scope this document reaches declares it">
      <b aria-hidden="true">&#9671;</b>
      {name}
      <i>undeclared</i>
    </span>
  )
}

// ── a property ───────────────────────────────────────────────────────────────
// ── a type written out rather than named ─────────────────────────────────────
//
// `{ array: { of: name } }` draws as "array of name". One word names the type and its own
// properties follow, and a property holding another written-out type draws the same way again.
//
// What a collection holds is a type and what a pattern holds is a regular expression, written the
// same way, so each property is drawn from what the declaration of that property says it holds.
const Inside = ({ word, inside }: { word: string; inside: unknown }) => {
  const { words } = useDrawing()
  if (!isMapping(inside)) return <em className="d-pfrom">not written as its properties</em>
  return (
    <>
      {Object.entries(inside).map(([key, held]) => {
        const nested = isMapping(held) ? onlyWord(held) : null
        // The states a written-out type takes in place of a value are drawn as the states they
        // are, the way they are drawn on a property, rather than as a list of words a reader has
        // to recognise.
        const written = Array.isArray(held) ? (held as readonly unknown[]) : null
        const states =
          key === ACCEPTS && written !== null
            ? written.filter((one): one is string => typeof one === 'string')
            : null
        if (states !== null && written !== null && states.length === written.length) {
          return (
            <span key={key} className="d-said s-accepts" title="what this takes in place of a value">
              {ACCEPTS} {list(states)}
            </span>
          )
        }
        return (
          <span key={key}>
            <i className="d-pof">{key}</i>
            {!holdsAType((id) => words.declared[id]?.body, word, key) ? (
              <Val value={held} type={typeOfProperty((id) => words.declared[id]?.body, word, key)} />
            ) : nested !== null ? (
              <Written word={nested[0]} inside={nested[1]} />
            ) : typeof held === 'string' && held.length > 0 ? (
              <Ref name={held} />
            ) : (
              <em className="d-pfrom">not a type name</em>
            )}
          </span>
        )
      })}
    </>
  )
}

const Written = ({ word, inside }: { word: string; inside: unknown }) => (
  <>
    <Ref name={word} />
    <Inside word={word} inside={inside} />
  </>
)

// ── a value written in the file ──────────────────────────────────────────────
//
// What a property always holds, and what one of an enum's values stands for, are the same thing
// drawn. A boolean carries a colour of its own, green for true and red for false, so the two are
// told apart before the word is read.
//
// Every value is drawn after an equals sign, so a value written in the file is told apart from the
// type beside it at a glance.
//
// A regular expression is drawn between the slashes it is written between in most languages, and
// in a colour of its own. Which values look like which is the same statement about the concrete
// syntax that the checker makes when it says what a value of each primitive is read as, so it is
// made the same way: by the primitive the value's type derives down to, and by nothing else.
// A value of the root type is a set of named parts, so it draws as those parts rather than as one
// run of text. Each part draws its name and then its own value by this same rule, so a part whose
// own type has parts nests. Whether a part names a property the type has is the checker's
// question, so a part nothing says a type for still draws as what is written.
const EXPRESSION = 'expression'
const ROOT = 'object'
const TEXT = 'string'

/**
 * Whether a value is a run of characters that is read rather than matched: its type constrains it
 * in no way and no enum lists it, so there is nothing to recognise it by and nothing to look it up
 * against. That is the prose a document carries, and it draws as text. A name, a uri, a member of
 * an enum and anything else a type pins down draws as a token, because what it is depends on
 * matching it. The question is asked of the language and of nothing else, so a word the page has
 * never heard of draws as prose the day its type says it is prose.
 */
const isProse = (bodyOf: BodyOf, type: string | null, primitive: string | null, value: unknown): boolean =>
  primitive === TEXT &&
  typeof value === 'string' &&
  type !== null &&
  membersOf(bodyOf, type) === null &&
  constraintsOn(bodyOf, type).length === 0

/**
 * The name the vocabulary gives the word this value names, or nothing where it has none to give.
 * It is the question `role: name` exists to answer: the value names one member of an enum, the
 * type that enum's values are of says which of its properties holds the name, and what that
 * member holds there is what the word is called. The id is what it is written as and the name is
 * what it is called, so a reader wants both.
 */
const shownBy = (
  bodyOf: BodyOf,
  type: string | null,
  value: unknown,
  role: string,
): string | null => {
  if (type === null || typeof value !== 'string') return null
  const of = typeOfOwnValue(bodyOf(type) ?? {})
  if (typeof of !== 'string' || of.length === 0) return null
  const part = propertyCarrying(bodyOf, of, role)
  if (part === undefined) return null
  const member = valuesOf(bodyOf, type)?.find((one) => one.id === value)
  const held = member === undefined || !isMapping(member.value) ? undefined : member.value[part]
  return typeof held === 'string' && held.length > 0 ? held : null
}

/**
 * The name a value naming one member of a closed set is drawn with, where that name says anything
 * the id does not. A set that names a member after itself leaves the two the same word in two
 * cases, and the same word read twice says nothing the first reading did not.
 */
const calledBy = (bodyOf: BodyOf, type: string | null, value: unknown): string | null => {
  const name = shownBy(bodyOf, type, value, ROLES.name)
  if (name === null || typeof value !== 'string') return null
  return name.toLowerCase() === value.toLowerCase() ? null : name
}

/**
 * Whether a value is several of one shape, drawn as a table. It is when the type is written out as
 * a collection — `set: { of: equivalent }` and the three others — the thing it holds is a type with
 * named parts, and every member is written as those parts. Anything else draws the way it did.
 */
const rowsOf = (
  bodyOf: BodyOf,
  type: unknown,
  value: unknown,
): { of: string; members: readonly Mapping[] } | null => {
  if (!isMapping(type) || !Array.isArray(value) || value.length === 0) return null
  const written = onlyWord(type)
  if (written === null) return null
  const [word, inner] = written
  if (bodyOf(word) === undefined || !derivesFrom(bodyOf, word, SELF.collection)) return null
  const of = isMapping(inner) ? inner.of : undefined
  if (typeof of !== 'string' || primitiveOf(bodyOf, of) !== ROOT) return null
  if (!value.every((member) => isMapping(member))) return null
  return { of, members: value as readonly Mapping[] }
}

/**
 * Many of a thing whose shape is written out rather than named: the shape each member is drawn
 * against, and the members. Nothing when the type is not a collection, or when the members are
 * several of one named shape, which is drawn as a table instead.
 */
const manyOf = (
  bodyOf: BodyOf,
  type: unknown,
  value: unknown,
): { of: unknown; members: readonly unknown[] } | null => {
  if (!isMapping(type) || !Array.isArray(value)) return null
  const written = onlyWord(type)
  if (written === null) return null
  const [word, inner] = written
  if (bodyOf(word) === undefined || !derivesFrom(bodyOf, word, SELF.collection)) return null
  if (!isMapping(inner) || inner.by !== undefined || inner.of === undefined) return null
  return { of: inner.of, members: value as readonly unknown[] }
}

/**
 * A value keyed by a closed set: the set its keys come from, the type each line holds, and the
 * states one line may take in place of a value. Nothing when the type is not keyed by a set.
 */
const keyedOf = (
  bodyOf: BodyOf,
  type: unknown,
  value: unknown,
): { by: string; of: unknown; accepts: readonly string[] } | null => {
  if (!isMapping(type) || !isMapping(value)) return null
  const written = onlyWord(type)
  if (written === null) return null
  const [word, inner] = written
  if (bodyOf(word) === undefined || !derivesFrom(bodyOf, word, SELF.collection)) return null
  if (!isMapping(inner) || typeof inner.by !== 'string') return null
  return {
    by: inner.by,
    of: inner.of,
    accepts: Array.isArray(inner.accepts)
      ? inner.accepts.filter((one): one is string => typeof one === 'string')
      : [],
  }
}

/**
 * One line for each word of a closed set, in the order the set lists them, each shown by the name
 * that set gives its words. The words are read out of the language, so the page names none of them
 * and a set with different words draws the same way. A word the value says nothing for is drawn as
 * the gap it is rather than left off the page.
 */
const Rungs = ({
  by,
  of,
  accepts,
  value,
  where,
}: {
  by: string
  of: unknown
  accepts: readonly string[]
  value: Mapping
  where?: string | undefined
}) => {
  const bodyOf = useBodyOf()
  const words = valuesOf(bodyOf, by) ?? []
  const ids = words.map((one) => one.id)
  const type = typeOfOwnValue(bodyOf(by) ?? {})
  const shown =
    typeof type === 'string' && type.length > 0 ? propertyCarrying(bodyOf, type, ROLES.name) : undefined
  const nameOf = (word: unknown): string | null =>
    shown === undefined || !isMapping(word) ? null : text(word[shown])
  // A line holding one value reads as a ladder, and the step says the order. A line holding many
  // has no single place on a ladder, so those draw level with each other instead.
  const ladder = words.every((one) => !Array.isArray(value[one.id]))
  return (
    <ul className="d-rungs">
      {words.map((one, step) => (
        <li
          key={one.id}
          className="d-rung"
          // The colour is the word's own place in its set, which is where every other colour on
          // the page comes from, so a word reads the same wherever it is drawn.
          style={{ ...kindStyle(one.id, ids), '--step': ladder ? step : 0 } as CSSProperties}
        >
          <span className="rung-word" title={one.id}>
            {nameOf(one.value) ?? one.id}
          </span>
          {one.id in value ? (
            <Val
              value={value[one.id]}
              type={of}
              accepts={accepts}
              where={where === undefined ? undefined : `${where}.${one.id}`}
            />
          ) : (
            <em className="gone">not written</em>
          )}
        </li>
      ))}
    </ul>
  )
}

/** Several of one shape: a column for every part the shape has, in the order it declares them. */
const Table = ({
  of,
  members,
  where,
}: {
  of: string
  members: readonly Mapping[]
  where?: string | undefined
}) => {
  const { words, said, drawn } = useDrawing()
  const bodyOf = useBodyOf()
  const columns = [...propertiesOf(bodyOf, of).keys()]
  return (
    <table className="d-table">
      <thead>
        <tr>
          {columns.map((name) => (
            <th key={name}>{name}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {members.map((member, i) => {
          // A row cannot be wrapped the way every other node is, so the mark is the row itself:
          // the colour on the row, and what is said under it, spanning the columns. A whole
          // member and one cell of it are two nodes, and each carries its own mark.
          const rowAt = where === undefined ? '' : `${where}[${i}]`
          const says = said.get(rowAt) ?? null
          if (says !== null) drawn.add(rowAt)
          return (
            <Fragment key={i}>
              <tr
                className={says === null ? undefined : classes('r-marked', colourOf(says))}
                id={says === null ? undefined : rowAt}
              >
                {columns.map((name) => (
                  <td key={name}>
                    {member[name] === undefined ? (
                      <i className="t-none" aria-hidden="true">
                        —
                      </i>
                    ) : (
                      <Val
                        value={member[name]}
                        type={typeOfPart(bodyOf, words.onProperty, of, name)}
                        accepts={acceptsOf(bodyOf, of, name)}
                        where={where === undefined ? undefined : `${where}[${i}].${name}`}
                      />
                    )}
                  </td>
                ))}
              </tr>
              {says === null ? null : (
                <tr className="r-note">
                  <td colSpan={columns.length}>
                    <Notes says={says} />
                  </td>
                </tr>
              )}
            </Fragment>
          )
        })}
      </tbody>
    </table>
  )
}

/** What one value needs to be drawn: the value, the type it is written against, and where it sits. */
interface OneValue {
  value: unknown
  /** The type the value is written against, so what it is drawn as is known. */
  type?: unknown
  className?: string
  /** Where this value sits, so anything said about it can be drawn on it. Nothing where unknown. */
  where?: string | undefined
  /**
   * The reserved states the property this value is written for takes, so a word that is one of
   * them is drawn as the state it is rather than as the text it would otherwise be. Empty
   * everywhere a property says nothing, and where the value belongs to no property at all.
   */
  accepts?: readonly string[]
}

/**
 * A value, with whatever is said about it drawn on it. Every value the page draws comes through
 * here, so there is one place that asks, and a value drawn anywhere — a property's own value, a
 * value an enum's id stands for, a named part, one cell of a table — carries what is said about
 * it. A value drawn where nothing says where it sits asks nothing, because nothing could point
 * at it.
 */
const Val = (props: OneValue) => (
  <Marked at={props.where ?? ''}>
    <Drawn {...props} />
  </Marked>
)

const Drawn = ({ value, type, className = '', where, accepts = [] }: OneValue) => {
  const bodyOf = useBodyOf()

  // A word the property takes in place of a value is the state it names, not the text it reads
  // as. It is drawn where the value would go, with a dashed edge, because the value is not there.
  if (typeof value === 'string' && accepts.includes(value)) {
    return (
      <span className={classes('d-val', 'v-reserved', `v-${value}`, className === '' ? null : className)}>
        {value}
      </span>
    )
  }
  const named = typeof type === 'string' && type.length > 0 ? type : null
  const primitive = named === null ? null : primitiveOf(bodyOf, named)

  const rows = rowsOf(bodyOf, type, value)
  const parts = primitive === ROOT && named !== null && isMapping(value)
  const expression = primitive === EXPRESSION && typeof value === 'string'
  const prose = isProse(bodyOf, named, primitive, value)
  const said = parts
    ? ' v-parts'
    : value === true
      ? ' v-true'
      : value === false
        ? ' v-false'
        : expression
          ? ' v-expr'
          : prose
            ? ' v-text'
            : ''
  const at = `d-val${said}${className === '' ? '' : ` ${className}`}`

  // Several of one shape read as a table, which says each column's name once instead of once per
  // member. The table says what it holds, so there is no equals sign before it either.
  if (rows !== null) {
    return (
      <span className={`d-val v-table${className === '' ? '' : ` ${className}`}`}>
        <Table of={rows.of} members={rows.members} where={where} />
      </span>
    )
  }

  // The parts carry an equals sign each, so the one before them would be a second copy of what
  // they already say.
  if (parts) {
    return (
      <span className={at}>
        <Parts type={named} value={value} where={where} />
      </span>
    )
  }

  // Many of a thing written out rather than named: each member is drawn against the shape the
  // collection says it holds, so a list of names reads as those names and never as a file's text.
  const many = manyOf(bodyOf, type, value)
  if (many !== null) {
    return (
      <span className={`d-val v-many${className === '' ? '' : ` ${className}`}`}>
        {many.members.map((member, i) => (
          <Val
            key={i}
            value={member}
            type={many.of}
            where={where === undefined ? undefined : `${where}[${i}]`}
          />
        ))}
      </span>
    )
  }

  // A value naming a declaration is that declaration, so it draws as every other reference to a
  // declared thing does, and says in the same way whether anything here declares it.
  if (typeof value === 'string' && isMapping(type)) {
    const written = onlyWord(type)
    if (written !== null && bodyOf(written[0]) !== undefined && derivesFrom(bodyOf, written[0], SELF.link)) {
      return (
        <span className={`d-val v-link${className === '' ? '' : ` ${className}`}`}>
          <Ref name={value} />
        </span>
      )
    }
  }

  // One of a thing for each word of a closed set reads as the set, not as a block of keys: the
  // words come out in the set's own order, whether or not the value wrote them in it.
  const keyed = keyedOf(bodyOf, type, value)
  if (keyed !== null && isMapping(value)) {
    return (
      <span className={`d-val v-rungs${className === '' ? '' : ` ${className}`}`}>
        <Rungs by={keyed.by} of={keyed.of} accepts={keyed.accepts} value={value} where={where} />
      </span>
    )
  }

  // A value that is a structure of its own is written out the way this language is written,
  // through the control that colours every example of it. JSON on the page would be a second
  // language to read, and the reader is here to read this one.
  if (value !== null && typeof value === 'object') {
    return (
      <span className={`d-val v-written${className === '' ? '' : ` ${className}`}`}>
        <Yaml value={value} type={named} />
      </span>
    )
  }

  // Prose is read as the sentence it is, so the equals sign that tells a value from the type beside
  // it would be a mark in the middle of a sentence and says nothing a reader of prose needs.
  const called = calledBy(bodyOf, named, value)
  const mark = shownBy(bodyOf, named, value, ROLES.mark)
  return (
    <span className={at}>
      {prose ? null : (
        <i className="v-eq" aria-hidden="true">
          =
        </i>
      )}
      {mark === null ? null : <b className="v-mark">{mark}</b>}
      {expression ? (
        <>
          <i aria-hidden="true">/</i>
          {value}
          <i aria-hidden="true">/</i>
        </>
      ) : (
        text(value)
      )}
      {called === null ? null : <b className="v-name">{called}</b>}
    </span>
  )
}

const Parts = ({
  type,
  value,
  where,
}: {
  type: string
  value: Mapping
  where?: string | undefined
}) => {
  const { words } = useDrawing()
  const bodyOf = useBodyOf()
  return (
    <span className="d-parts">
      {Object.entries(value).map(([name, held]) => (
        <Marked key={name} at={where === undefined ? '' : `${where}.${name}`}>
          <span className="d-part">
            <i>{name}</i>
            <Val
              value={held}
              type={typeOfPart(bodyOf, words.onProperty, type, name)}
              accepts={acceptsOf(bodyOf, type, name)}
              where={where === undefined ? undefined : `${where}.${name}`}
            />
          </span>
        </Marked>
      ))}
    </span>
  )
}

// What a property holds, drawn from the name it gives under `is` or from the declaration above.
const Held = ({ type }: { type: unknown }) =>
  type === undefined ? (
    <em className="d-pfrom">no type</em>
  ) : Array.isArray(type) ? (
    <em className="d-pfrom">{type.length} types named</em>
  ) : typeof type !== 'string' || type.length === 0 ? (
    <em className="d-pfrom">not a type name</em>
  ) : (
    <Ref name={type} />
  )

const Property = ({
  at,
  owner,
  name,
  node,
}: {
  at: string
  /** The id of the declaration this property is written on, so the one above it can be read. */
  owner: string
  name: string
  node: unknown
}) => {
  const { words } = useDrawing()
  const bodyOf = useBodyOf()

  // A mapping declares the property here. Anything else refers to one declared elsewhere.
  if (!isMapping(node)) {
    return (
      <div className="d-prow">
        <span className="d-pname">{name}</span>
        <span>
          <Ref name={text(node)} />
          <em className="d-pfrom">taken from there</em>
        </span>
      </div>
    )
  }

  // A type with properties of its own may be written out instead of named, as one word naming it
  // with its properties inside — which is how a collection says what it holds. It is written
  // beside the property's own keywords, so what is left once those are taken away is the type.
  const outside = Object.keys(node).filter((k) => !words.onProperty.includes(k))
  const written = node.is === undefined ? (outside.length === 1 ? outside[0] : undefined) : undefined

  // A property written again on a type deriving from the one that declared it says what it
  // always holds and leaves `is` off, so its type is the one the declaration above gives.
  const type = node.is
  const above =
    written === undefined && type === undefined ? propertyAbove(bodyOf, owner, name) : null
  const held = above === null ? type : above.property.is
  const namesNoType = type !== undefined && !Array.isArray(type) && (typeof type !== 'string' || type.length === 0)
  const accepted = acceptsOf(bodyOf, owner, name)

  // What this property is to a reader, beyond the value it holds. A role answers one question
  // about a type once, so where the type gives the same role to a second property the question
  // has no answer: the page says which two carry it rather than picking one, because picking
  // would be the page deciding something the language has not said.
  const role = node[ROLE]
  const carrying = typeof role === 'string' ? propertiesCarrying(bodyOf, owner, role) : []

  return (
    <>
      <div className="d-prow">
        <span className="d-pname">{name}</span>
        <span>
          {written !== undefined ? (
            <Written word={written} inside={node[written]} />
          ) : (
            <Held type={held} />
          )}
          {above !== null ? <em className="d-pfrom">from {above.from}</em> : null}
          {node[OPTIONAL] === true ? (
            <span className="d-said s-optional" title="this property may be left out">
              {OPTIONAL}
            </span>
          ) : null}
          {accepted.length === 0 ? null : (
            <span className="d-said s-accepts" title="what this property takes in place of a value">
              {ACCEPTS} {list(accepted)}
            </span>
          )}
          {role === undefined ? null : (
            <span
              className="d-said s-role"
              title="what this property is to a reader, beyond the value it holds"
            >
              {ROLE} {text(role)}
            </span>
          )}
          {node.value !== undefined ? (
            <Val
              value={node.value}
              type={held}
              className="d-pval"
              accepts={acceptsOf(bodyOf, owner, name)}
              where={`${at}.value`}
            />
          ) : null}
        </span>
      </div>
      {/* A property written again takes what it does not write from the declaration above, which
          is already how its type is read, so its prose is read the same way. */}
      <Prose node={above === null ? node : { ...above.property, ...node }} row />
      {Array.isArray(type) ? (
        <Err at={`${at}.is`}>a property holds one type, and this names {type.length}.</Err>
      ) : null}
      {namesNoType ? <Err at={`${at}.is`}>must name a type.</Err> : null}
      {carrying.length > 1 ? (
        <Err at={`${at}.${ROLE}`}>
          {owner} gives {ROLE} {text(role)} to {list(carrying)}, so nothing says which of them
          holds it.
        </Err>
      ) : null}
      {written === undefined && type === undefined && above === null ? (
        <Err at={at}>has no is, so there is nothing to say what it holds.</Err>
      ) : null}
      <Unclaimed
        at={at}
        node={node}
        drawn={written === undefined ? DRAWN_ON_PROPERTY : [...DRAWN_ON_PROPERTY, written]}
      />
    </>
  )
}

// A declaration with none draws no row for them at all. The count on the declaration's own line
// is what says so, so the page spends no height on saying a thing is absent.
const Properties = ({ at, owner, node }: { at: string; owner: string; node: unknown }) => {
  if (node === undefined) return null
  if (!isMapping(node)) {
    return <Err at={`${at}.properties`}>must be a mapping of name to property.</Err>
  }
  const names = Object.keys(node)
  if (names.length === 0) return null
  return (
    <>
      {names.map((n) => (
        <Marked key={n} at={`${at}.properties.${n}`}>
          <Property at={`${at}.properties.${n}`} owner={owner} name={n} node={node[n]} />
        </Marked>
      ))}
    </>
  )
}

// ── a list of type names ─────────────────────────────────────────────────────
//
// `is` on a declaration and `options` on a choice are the same thing drawn: a keyword followed
// by the types it names. One component draws both, and the keyword it was written under is the
// label, so neither has a drawing of its own to drift from the other.
const Types = ({
  at,
  keyword,
  value,
  inline = false,
}: {
  at: string
  keyword: string
  value: unknown
  /** Drawn on the declaration's own line rather than on a line of its own. */
  inline?: boolean
}) => {
  const many: unknown[] = Array.isArray(value) ? value : [value]
  const good = many.filter((x): x is string => typeof x === 'string' && x.length > 0)
  const bad = many.length - good.length
  return (
    <>
      <div className={inline ? 'd-is d-is-head' : 'd-is'}>
        <i>{keyword}</i>
        <span>
          {good.map((x, i) => (
            <Ref key={i} name={x} />
          ))}
        </span>
      </div>
      {bad > 0 ? (
        <Err at={`${at}.${keyword}`}>
          {bad} of {many.length} do not name a type.
        </Err>
      ) : null}
    </>
  )
}

// ── the values an enum lists ─────────────────────────────────────────────────
//
// Each one carries its own id and what that id stands for. The value is drawn as what is
// written, with none of a reference's shape, word or colour, because it is a value rather than
// the name of a type.
const Value = ({ at, is, node }: { at: string; is: unknown; node: unknown }) => {
  if (!isMapping(node)) {
    return <Err at={at}>a value carries an id and what that id stands for.</Err>
  }
  const id = node.id
  return (
    <>
      <div className="d-prow">
        <span className="d-pname">
          {typeof id === 'string' && id.length > 0 ? id : <em className="d-pfrom">no id</em>}
        </span>
        <span>
          {node.value === undefined ? (
            <em className="d-pfrom">no value</em>
          ) : (
            <Val value={node.value} type={is} where={`${at}.value`} />
          )}
        </span>
      </div>
      <Prose node={node} row />
      {typeof id !== 'string' || id.length === 0 ? (
        <Err at={`${at}.id`}>every value carries an id, written as text.</Err>
      ) : null}
      {node.value === undefined ? (
        <Err at={at}>has no value, so nothing says what this id stands for.</Err>
      ) : null}
      <Unclaimed at={at} node={node} drawn={DRAWN_ON_VALUE} />
    </>
  )
}

const Values = ({ at, is, value }: { at: string; is: unknown; value: unknown }) => {
  const many: unknown[] = Array.isArray(value) ? value : [value]
  return (
    <>
      <div className="d-is">
        <i>values</i>
      </div>
      {many.map((v, i) => (
        <Marked key={i} at={`${at}.values[${i}]`}>
          <Value at={`${at}.values[${i}]`} is={is} node={v} />
        </Marked>
      ))}
    </>
  )
}

/**
 * The one value a declaration carries of its own type, drawn against the type the declaration
 * names rather than against what the language says the keyword holds. `value` is declared as
 * holding the root type, because a keyword's `is` names a type and cannot name another keyword, so
 * the root is as much as the language can say about it. Drawn against the root, every part inside
 * the value loses its type and any part that is a structure of its own falls to being written out
 * as text. `typeOfOwnValue` is the rule the checker reads it by as well, so the page cannot show as
 * unaccounted for what the checker verified.
 */
const OwnValue = ({ at, type, value }: { at: string; type: unknown; value: unknown }) => (
  <div className="d-own">
    <Val value={value} type={type} where={`${at}.${VALUE}`} />
  </div>
)

// ── what a type says its values may be ───────────────────────────────────────
//
// Each constraint is one word naming the kind of constraint with what it holds inside it, which
// is the shape a type written out takes, so it draws as one: the kind in the name column and what
// it holds beside it.
const Constraint = ({ at, node }: { at: string; node: unknown }) => {
  const written = isMapping(node) ? onlyWord(node) : null
  if (written === null) {
    return <Err at={at}>a constraint names one kind, with what it holds inside it.</Err>
  }
  return (
    <div className="d-prow">
      <span className="d-pname">
        <Ref name={written[0]} />
      </span>
      <span>
        <Inside word={written[0]} inside={written[1]} />
      </span>
    </div>
  )
}

const Constraints = ({ at, value }: { at: string; value: unknown }) => {
  const many: unknown[] = Array.isArray(value) ? value : [value]
  return (
    <>
      <div className="d-is">
        <i>{CONSTRAINTS}</i>
      </div>
      {many.map((one, i) => (
        <Marked key={i} at={`${at}.${CONSTRAINTS}[${i}]`}>
          <Constraint at={`${at}.${CONSTRAINTS}[${i}]`} node={one} />
        </Marked>
      ))}
    </>
  )
}

// ── a keyword with no drawing of its own ─────────────────────────────────────
//
// What the language says the keyword holds is the whole drawing: a reference where it holds a
// type, and the value itself where it holds one. `from` and `to` on a cast draw this way, and so
// would a keyword added tomorrow, which is why neither is named here.
const Keyword = ({
  at,
  kind,
  keyword,
  value,
}: {
  at: string
  kind: string
  keyword: string
  value: unknown
}) => {
  const { words } = useDrawing()
  const bodyOf = (id: string): Mapping | undefined => words.declared[id]?.body
  const written = isMapping(value) ? onlyWord(value) : null

  if (holdsAType(bodyOf, kind, keyword)) {
    return (
      <div className="d-prow">
        <span className="d-pname">{keyword}</span>
        <span>
          {written !== null ? (
            <Written word={written[0]} inside={written[1]} />
          ) : typeof value === 'string' && value.length > 0 ? (
            <Ref name={value} />
          ) : (
            <em className="d-pfrom">not a type name</em>
          )}
        </span>
      </div>
    )
  }

  const holds = typeOfProperty(bodyOf, kind, keyword)
  if (holds === undefined) {
    return (
      <Err at={`${at}.${keyword}`} label="incomplete">
        &quot;{keyword}&quot; is an incomplete language feature. Nothing says what it holds, so
        there is nothing to draw.
      </Err>
    )
  }
  return (
    <div className="d-prow">
      <span className="d-pname">{keyword}</span>
      <span>
        <Val value={value} type={holds} />
      </span>
    </div>
  )
}

/**
 * What heads a declaration's card: the word it is headed by, the vocabulary its colour is taken
 * from, and where that word sits so anything said about it is drawn on it.
 *
 * A declaration carrying one value of its own, whose type says which of its properties holds the
 * kind, is headed by what that property holds, because that is what the thing IS: `IGuard` reads
 * as an interface rather than as the `constant` it happens to be written under. A declaration
 * whose type says nothing is headed by its kind, as every declaration was, and `at` is nothing
 * because the word is the keyword rather than a node of the document.
 */
interface Heads {
  readonly word: string
  readonly among: readonly string[]
  readonly at: string | null
}

export const Declaration = ({ at, node }: { at: string; node: unknown }) => {
  const { words } = useDrawing()
  const bodyOf = useBodyOf()

  if (!isMapping(node)) {
    return (
      <Err loose at={at}>
        a declaration is a mapping naming one kind.
      </Err>
    )
  }

  const named = Object.entries(node)
  const first = named[0]
  if (first === undefined || named.length !== 1) {
    return (
      <Err loose at={at}>
        a declaration names one kind, and this names {named.length}
        {named.length > 0 ? `: ${list(named.map(([k]) => k))}` : ''}.
      </Err>
    )
  }

  // `external` is written in place of the kind, with the kind inside it. Everything below is
  // drawn the same either way; the one difference is that the declaration says it is external.
  const external = first[0] === BOOTSTRAP.external
  const inner = external ? onlyWord(first[1]) : first
  if (inner === null) {
    return (
      <Err loose at={`${at}.${BOOTSTRAP.external}`}>
        external names one kind, and the kind is written inside it.
      </Err>
    )
  }

  const [kind, body] = inner
  const names = external ? `${at}.${BOOTSTRAP.external}` : at
  if (!words.kinds.includes(kind)) {
    return (
      <Err loose at={names}>
        &quot;{kind}&quot; is not a kind. The kinds are {list(words.kinds)}.
      </Err>
    )
  }

  // Always present: words.ts refuses to load when a kind is missing from the keyword table, so
  // a page is never built in that state and this page has no weaker copy of it. Null is the
  // other thing entirely — the kind is approved and its keywords are not decided.
  const claimed = words.onKind[kind]
  if (claimed === null || claimed === undefined) {
    return (
      <Err loose at={`${names}.${kind}`} label="incomplete">
        &quot;{kind}&quot; is an incomplete language feature. The keywords it carries have not
        been decided, so there is nothing to draw.
      </Err>
    )
  }

  if (!isMapping(body)) {
    return (
      <Err loose at={`${names}.${kind}`}>
        must be a mapping.
      </Err>
    )
  }

  const here = `${names}.${kind}`
  // How many properties, said on the declaration's own line. A kind that carries the keyword
  // and writes none says "no properties" here, because no row is drawn for them below.
  const counted = (): string | null => {
    if (!claimed.includes('properties')) return null
    if (body.properties === undefined) return 'no properties'
    if (!isMapping(body.properties)) return null
    const n = Object.keys(body.properties).length
    return n === 0 ? 'no properties' : `${n} ${n === 1 ? 'property' : 'properties'}`
  }
  const count = counted()

  // The one value this declaration carries, and the type it is of. What that type says two of
  // its properties are to a reader is the whole of what follows: one holds the kind of the thing,
  // and one is the prose explaining it. Each is drawn once — the kind in the head, the prose
  // under it — and so neither is drawn again as a row of the value below.
  const of = typeOfOwnValue(body)
  const ofType = typeof of === 'string' && of.length > 0 ? of : null
  const own = claimed.includes(VALUE) && isMapping(body[VALUE]) ? body[VALUE] : null
  const partFor = (role: string): string | null =>
    ofType === null || own === null ? null : (propertyCarrying(bodyOf, ofType, role) ?? null)
  const kindPart = partFor(ROLES.kind)
  const prosePart = partFor(ROLES.prose)

  const held = kindPart === null || own === null ? undefined : own[kindPart]
  const vocabulary = kindPart === null || ofType === null
    ? undefined
    : typeOfProperty(bodyOf, ofType, kindPart)
  const among = vocabulary === undefined ? null : membersOf(bodyOf, vocabulary)
  const heads: Heads =
    kindPart !== null && typeof held === 'string' && held.length > 0 && among !== null
      ? { word: held, among, at: `${here}.${VALUE}.${kindPart}` }
      : { word: kind, among: words.kinds, at: null }

  const says = prosePart === null || own === null ? undefined : own[prosePart]
  const line =
    prosePart === null || says === undefined ? undefined : (
      <Marked at={`${here}.${VALUE}.${prosePart}`}>
        <b>{text(says)}</b>
      </Marked>
    )

  // What is left of the value once the head and the line have taken their parts. A declaration
  // whose value is nothing but those two draws no value row at all, because both are already on
  // the page above.
  const lifted = [kindPart, prosePart].filter((one): one is string => one !== null)
  const rest =
    own === null || lifted.length === 0
      ? body[VALUE]
      : Object.fromEntries(Object.entries(own).filter(([k]) => !lifted.includes(k)))
  const drawsValue =
    claimed.includes(VALUE) && body[VALUE] !== undefined && !(isMapping(rest) && Object.keys(rest).length === 0)

  // The character the thing is drawn with, asked of the vocabulary the head came from. A set
  // whose words carry none, and a head that is the kind itself, draw no mark.
  const mark =
    heads.at === null || typeof vocabulary !== 'string'
      ? null
      : shownBy(bodyOf, vocabulary, heads.word, ROLES.mark)

  return (
    <section className="d-decl" style={kindStyle(heads.word, heads.among)}>
      <div className="d-decl-head">
        {external ? <span className="d-extern">{BOOTSTRAP.external}</span> : null}
        {mark === null ? null : <span className="d-mark">{mark}</span>}
        {heads.at === null ? (
          <span className="d-kind">{heads.word}</span>
        ) : (
          <Marked at={heads.at}>
            <span className="d-kind">{heads.word}</span>
          </Marked>
        )}
        <span className="d-id">
          {typeof body.id === 'string' && body.id.length > 0 ? (
            body.id
          ) : (
            <em className="d-pfrom">no id</em>
          )}
        </span>
        {SAID_OF_TYPE.filter((k) => claimed.includes(k) && body[k] === true).map((k) => (
          <span key={k} className={`d-said s-${k}`}>
            {k}
          </span>
        ))}
        {claimed.includes('is') && body.is !== undefined ? (
          <Marked at={`${here}.is`}>
            <Types at={here} keyword="is" value={body.is} inline />
          </Marked>
        ) : null}
        {count !== null ? <span className="d-count">{count}</span> : null}
      </div>

      <Prose node={body} line={line} />

      {typeof body.id !== 'string' || body.id.length === 0 ? (
        <Err at={`${here}.id`}>every declaration carries an id, written as text.</Err>
      ) : null}

      {claimed.includes(CONSTRAINTS) && body[CONSTRAINTS] !== undefined ? (
        <Constraints at={here} value={body[CONSTRAINTS]} />
      ) : null}
      {claimed.includes('options') && body.options !== undefined ? (
        <Marked at={`${here}.options`}>
          <Types at={here} keyword="options" value={body.options} />
        </Marked>
      ) : null}
      {claimed.includes('values') && body.values !== undefined ? (
        <Values at={here} is={body.is} value={body.values} />
      ) : null}
      {drawsValue ? <OwnValue at={here} type={of} value={rest} /> : null}
      {claimed.includes('properties') ? (
        <Properties
          at={here}
          owner={typeof body.id === 'string' ? body.id : ''}
          node={body.properties}
        />
      ) : null}
      {claimed
        .filter((k) => !DRAWN_ON_KIND.includes(k) && body[k] !== undefined)
        .map((k) => (
          <Marked key={k} at={`${here}.${k}`}>
            <Keyword at={here} kind={kind} keyword={k} value={body[k]} />
          </Marked>
        ))}
      <Unclaimed at={here} node={body} drawn={claimed} />
    </section>
  )
}

// ── the document ─────────────────────────────────────────────────────────────
//
// The top-level keys this page has a drawing for. This is deliberately NOT words.topLevel: a
// keyword the language gains and this page cannot yet draw has to show up as an error, and
// claiming the whole vocabulary would hide exactly that. words.topLevel still drives the
// legend, which is a list of the language's keys rather than of this page's drawings.
const DRAWN_AT_FILE: readonly string[] = [
  SCOPE,
  IMPORT,
  'language',
  'title',
  'description',
  BOOTSTRAP.declare,
]

// A scope, written as the list of segments it is rather than as a dotted string. A document's own
// scope and each scope it imports are the same thing written, so they draw the same way.
const Path = ({ segments }: { segments: readonly unknown[] }) => (
  <span className="d-path">
    {segments.map((seg, i) => (
      <Fragment key={i}>
        {i > 0 ? <em aria-hidden="true">&#8250;</em> : null}
        <b>{text(seg)}</b>
      </Fragment>
    ))}
  </span>
)

/** A count in the words the report counts in, so the page and the report never disagree. */
const counted = (many: number, word: string): string => `${many} ${word}${many === 1 ? '' : 's'}`

/** One line of the band: where it is, what is said there, and which list it came from. */
interface CalledOut {
  readonly from: From
  /** Where it is, written the way whatever said it writes it. */
  readonly where: string
  readonly says: string
  /** The path of the mark to jump to, or nothing where it sits on no node. */
  readonly at: string | null
}

/**
 * Everything called out on this page, in the order the band lists it: what the checker found,
 * and then what a highlight asks. A highlight is allowed only where it needs Tim's attention, so
 * it belongs in the same list as a finding rather than waiting below to be scrolled past.
 */
const calledOut = (
  findings: readonly Finding[],
  said: ReadonlyMap<string, readonly Said[]>,
): readonly CalledOut[] => [
  ...findings.map((one) => ({
    from: one.from,
    where: one.fault.where,
    says: one.fault.what,
    at: one.at,
  })),
  ...[...said].flatMap(([at, says]) =>
    says
      .filter((one) => one.from === 'highlight')
      .map((one) => ({ from: one.from, where: at, says: one.note, at })),
  ),
]

/** What each list is counted in, in the order the band says them. */
const COUNTED: readonly (readonly [From, string])[] = [
  ['problem', 'problem'],
  ['blocker', 'blocker'],
  ['pending', 'pending value'],
  ['highlight', 'highlight'],
]

/**
 * What needs reading, under the document's title and above everything it is about. Each line says
 * which list it came from, where it is, and what is said there, and the place is a link to the
 * mark drawn there. A document with nothing called out draws no band at all: the page spends no
 * height saying that a thing is absent.
 *
 * A finding naming no node in this document — one about the file itself — is said here and
 * nowhere else, because there is no node to put it beside, so it carries no link.
 */
const Found = () => {
  const { findings, said } = useDrawing()
  const rows = calledOut(findings, said)
  if (rows.length === 0) return null
  const many = (from: From): number => rows.filter((one) => one.from === from).length
  return (
    <section className={many('problem') > 0 ? 'd-found has-problem' : 'd-found'}>
      <p className="d-found-head">
        {COUNTED.map(([from, word]) => (many(from) === 0 ? null : counted(many(from), word)))
          .filter((one) => one !== null)
          .join(', ')}
      </p>
      <ul>
        {rows.map((one, i) => (
          <li key={i}>
            <span className={`f-what f-${one.from}`}>{one.from}</span>
            {one.at === null ? (
              <span className="f-at f-nowhere">{one.where}</span>
            ) : (
              <a className="f-at" href={`#${encodeURIComponent(one.at)}`}>
                {one.where}
              </a>
            )}
            <span className="f-says">{one.says}</span>
          </li>
        ))}
      </ul>
    </section>
  )
}

/**
 * Everything said about this document that is not drawn on it, which is the one thing a mark
 * cannot show by being drawn. A path may name no node at all, or it may name a node the page
 * draws no mark on — a part of a value written out as text, say — and either way what was said
 * about it would otherwise be counted and then dropped without a word.
 *
 * It is drawn after every part of the page that draws a mark, because what it says is which
 * marks those parts did not draw.
 */
const NotOnThePage = ({ missing }: { missing: readonly string[] }) => {
  const { said, drawn } = useDrawing()
  const undrawn = [...said.keys()].filter((at) => !drawn.has(at))
  if (missing.length === 0 && undrawn.length === 0) return null
  return (
    <section className="d-section">
      <h2 className="d-section-name">not drawn on this page</h2>
      {missing.map((path) => (
        <Err key={path} loose at={path}>
          names no node in this document.
        </Err>
      ))}
      {undrawn.map((at) => (
        <div key={at} id={at}>
          <Err loose at={at} label="not drawn">
            names a node this page draws no mark on, so what is said about it is not beside it.
          </Err>
        </div>
      ))}
    </section>
  )
}

/** The name one item under `declare` declares, whatever kind it is written as. */
const idOf = (item: unknown): string | null => {
  if (!isMapping(item)) return null
  for (const held of Object.values(item)) {
    if (!isMapping(held)) continue
    const id = held[BOOTSTRAP.id]
    if (typeof id === 'string') return id
  }
  return null
}

export const Page = ({
  words,
  doc,
  marks = NO_MARKS,
  found = NOTHING_FOUND,
  only,
}: {
  words: Vocabulary
  doc: unknown
  /** What a highlight file says to mark here. A document with no highlight draws the same way. */
  marks?: Marks
  /** What the checker found in this document. A page drawn without one draws no band. */
  found?: CheckResult
  /**
   * The id of the one declaration to draw. The document keeps every declaration it makes, so a
   * name one of them writes still reaches the others and the checker reads the document it really
   * is; only what is drawn is narrowed.
   */
  only?: string
}) => {
  const { marked, missing } = marks
  const findings = findingsIn(doc, found)
  const said = saidAbout(marked, findings)
  // Filled while the page is drawn, by the marks that are drawn. See `Marked`.
  const drawn = new Set<string>()

  // A name this document does not declare is reached by bringing in the scope that declares it,
  // or by naming that scope as the language the document is written in. Every declaration the
  // vocabulary holds says which scope declared it, so the one it came from is the one drawn.
  const written = isMapping(doc) ? doc[LANGUAGE] : undefined
  const writtenIn = typeof written === 'string' && written.length > 0 ? [written] : null
  const brought = (name: string): string | null => {
    const held = words.declared[name]
    if (held === undefined) return null
    const reaches =
      importsScope(doc, held.scope) || (writtenIn !== null && samePath(writtenIn, held.scope))
    return reaches ? held.scope.join('.') : null
  }

  if (!isMapping(doc)) {
    return (
      <main>
        <DrawingContext.Provider
          value={{ words, declared: new Map(), brought, said, drawn, findings }}
        >
          <Found />
          <Err loose at="the file">
            is not a mapping, so there is nothing to draw.
          </Err>
        </DrawingContext.Provider>
      </main>
    )
  }

  const declared = doc[BOOTSTRAP.declare]
  const nothingDeclared = declared === undefined || (Array.isArray(declared) && declared.length === 0)
  const scopeOk =
    Array.isArray(doc.scope) &&
    (doc.scope as unknown[]).every((s) => typeof s === 'string' && s.length > 0)
  const scope: string[] = scopeOk ? (doc.scope as string[]) : []

  return (
    <DrawingContext.Provider
      value={{ words, declared: declaredBodiesOf(doc), brought, said, drawn, findings }}
    >
      <main>
        <div className="d-top">
          {/* The scope names the file. Every declaration below belongs to it, so it is drawn as
              the list of segments it actually is rather than as a dotted string, and labelled,
              because nothing on the page would otherwise say what it is. */}
          <div className="d-ident">
            <div className="d-ident-row">
              <i>{SCOPE}</i>
              {scopeOk ? <Path segments={scope} /> : <span className="d-path d-path-none">not given</span>}
            </div>
            {doc[IMPORT] !== undefined ? (
              <div className="d-ident-row">
                <i>{IMPORT}</i>
                <span className="d-imports">
                  {importedBy(doc).map((one, i) => (
                    <Path key={i} segments={one} />
                  ))}
                </span>
              </div>
            ) : null}
            {doc.language !== undefined ? (
              <div className="d-ident-row">
                <i>language</i>
                <span className="d-path">
                  <b>{text(doc.language)}</b>
                </span>
              </div>
            ) : null}
          </div>
          <h1>
            {typeof doc.title === 'string' ? doc.title : <em className="d-pfrom">no title</em>}
          </h1>
          {doc.description !== undefined ? (
            <p className="d-sub">{text(doc.description)}</p>
          ) : null}
        </div>

        <Found />

        <Unclaimed at="the file" node={doc} drawn={DRAWN_AT_FILE} />

        {doc.scope !== undefined && !scopeOk ? (
          <Err loose at="the file.scope">
            a scope is a list of segments, each written as text.
          </Err>
        ) : null}

        {/* One list, headed by the keyword that names it, drawn in the order it was written,
            because the order a document is read in is the author's and nothing may reorder it. */}
        {nothingDeclared ? (
          <p className="d-nothing">This document declares nothing.</p>
        ) : (
          <section className="d-section">
            <h2 className="d-section-name">{BOOTSTRAP.declare}</h2>
            {!Array.isArray(declared) ? (
              <Err loose at={`the file.${BOOTSTRAP.declare}`}>
                must be written as a list.
              </Err>
            ) : (
              (declared as unknown[])
                .map((d, i) => [d, i] as const)
                .filter(([d]) => only === undefined || idOf(d) === only)
                .map(([d, i]) => (
                  <Marked key={i} at={`${BOOTSTRAP.declare}[${i}]`}>
                    <Declaration at={`${BOOTSTRAP.declare}[${i}]`} node={d} />
                  </Marked>
                ))
            )}
          </section>
        )}

        <NotOnThePage missing={missing} />

        <div className="d-foot">
          <p>
            A declaration draws as its kind, its id, what it is, and its properties. One carrying
            a value of a type that says which of its properties holds the kind is headed by what
            that property holds instead, and the property that type says is its prose reads as the
            line under that heading. An enum draws the one type it is based on and every value it
            lists, each with its own id and what that id stands for. A choice draws the types it
            is one of. A property is a name and a type, and a property holding many draws the
            collection it is and what that collection holds.
          </p>

          <dl className="d-legend">
            <dt>
              <span className="d-ref r-declared" title="declared in this document">
                <b aria-hidden="true">&#9670;</b>a type
              </span>
            </dt>
            <dd>
              This document declares it. The filled mark and the{' '}
              <b className="c-declared">teal</b> chip say so, and no word is needed because this is
              what most references are.
            </dd>

            <dt>
              <span className="d-ref r-imported" title="brought in by an import">
                <b aria-hidden="true">&#9672;</b>a type<i>a scope</i>
              </span>
            </dt>
            <dd>
              A scope this document reaches declares it, and the chip names that scope. A document
              reaches a scope by bringing it in, and reaches the language it is written in without.
              The mark is a diamond within a diamond, because the name is reached rather than
              written here.
            </dd>

            <dt>
              <span className="d-ref r-undeclared" title="this document does not declare it">
                <b aria-hidden="true">&#9671;</b>a type<i>undeclared</i>
              </span>
            </dt>
            <dd>
              Neither this document nor any scope it reaches declares it, so nothing here says
              what it is. The mark is hollow and the chip is <b className="c-undeclared">amber</b>.
            </dd>

            <dt className="d-legend-kinds">
              {words.places.declare.map((k) => (
                <span key={k} className="d-kind" style={kindStyle(k, words.kinds)}>
                  {k}
                </span>
              ))}
            </dt>
            <dd>
              The kinds a declaration may be. Each has a colour of its own, taken from its place
              in the vocabulary, and the chip always spells the kind out.
            </dd>

            <dt className="d-legend-kinds">
              <span className="d-extern">{BOOTSTRAP.external}</span>
              {words.places.external.map((k) => (
                <span key={k} className="d-kind" style={kindStyle(k, words.kinds)}>
                  {k}
                </span>
              ))}
            </dt>
            <dd>
              The kinds written inside <b>{BOOTSTRAP.external}</b>, which gives the definition and leaves
              the implementation outside the language. They are written in the same list as
              everything else, in the order the author wants them read.
            </dd>
            <dt>
              <span className="d-legend-keys">{list(words.topLevel)}</span>
            </dt>
            <dd>The keys a file may carry at its top level, and no others.</dd>
          </dl>
        </div>
      </main>
    </DrawingContext.Provider>
  )
}
