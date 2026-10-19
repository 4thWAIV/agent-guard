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

import { CONSTRAINTS } from './constraints.ts'
import { NO_MARKS, type Marks } from './highlight.ts'
import {
  BOOTSTRAP,
  derivesFrom,
  holdsAType,
  IMPORT,
  importedBy,
  importsScope,
  isMapping,
  onlyWord,
  primitiveOf,
  propertiesOf,
  propertyAbove,
  SCOPE,
  SELF,
  typeOfPart,
  typeOfProperty,
  type BodyOf,
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
   * The scope this document imports, where it is the one the vocabulary was read from, written
   * the way the document writes it. Nothing when the document imports no scope this page has.
   */
  readonly imported: string | null
  /**
   * What a highlight file says to mark, by the path of the node it points at, with what it says
   * of that node. Empty where no highlight was given, which is how every document draws without
   * one.
   */
  readonly marked: ReadonlyMap<string, string>
  /** Every path a highlight gave that names no node here, so the page can say so. */
  readonly missing: readonly string[]
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
  const { words, declared, imported } = useDrawing()
  return (id: string) =>
    declared.get(id) ?? (imported === null ? undefined : words.declared[id]?.body)
}

/**
 * What a highlight says of the node drawn at this path, or nothing when it says nothing of it. A
 * node is marked by where it sits, so nothing on the node itself says it is marked and no kind
 * holds a list of what may be.
 */
const useMarked = (at: string): string | null => {
  const { marked } = useDrawing()
  return marked.get(at) ?? null
}

/** Whether a highlight points at one member of a table, which is marked on the row itself. */
const markedRow = (
  marked: ReadonlyMap<string, string>,
  where: string | undefined,
  i: number,
): string | undefined =>
  where !== undefined && marked.has(`${where}[${i}]`) ? 'r-marked' : undefined

/**
 * A node drawn where a highlight points. The mark goes around whatever the node draws as, so
 * every kind is markable and none of them knows it.
 */
const Marked = ({ at, children }: { at: string; children: ReactNode }) => {
  const note = useMarked(at)
  if (note === null) return <>{children}</>
  return (
    <div className="d-marked">
      {children}
      {note === '' ? null : <p className="d-note">{note}</p>}
    </div>
  )
}

/** Every declaration a document writes, by id, taking the external wrapper off where one is
    written. */
export function declaredBodiesOf(doc: unknown): ReadonlyMap<string, Mapping> {
  const bodies = new Map<string, Mapping>()
  if (!isMapping(doc)) return bodies
  const held = doc[BOOTSTRAP.declare]
  if (!Array.isArray(held)) return bodies
  for (const item of held as unknown[]) {
    const named = onlyWord(item)
    if (named === null) continue
    const inner = named[0] === BOOTSTRAP.external ? onlyWord(named[1]) : named
    if (inner === null) continue
    const body = inner[1]
    if (isMapping(body) && typeof body.id === 'string' && !bodies.has(body.id)) {
      bodies.set(body.id, body)
    }
  }
  return bodies
}

// A hue per kind, spread evenly round the wheel by position. Any number of kinds, no gaps.
// oklch rather than hsl because hsl at one lightness goes muddy through the yellows and
// greens, so a kind's colour would carry weight it did not mean.
export function kindStyle(kind: string, kinds: readonly string[]): CSSProperties | undefined {
  const i = kinds.indexOf(kind)
  if (i < 0) return undefined
  const hue = Math.round((i * 360) / kinds.length + 268) % 360
  return { '--kind': `oklch(54% 0.17 ${hue})` } as CSSProperties
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
  'values',
  'options',
]

// What a type says about itself rather than about its properties. Each is drawn only when the
// declaration writes it true, because writing it false says the same as leaving it off.
const SAID_OF_TYPE: readonly string[] = ['abstract', 'primitive']
const DRAWN_ON_PROPERTY: readonly string[] = ['is', ...PROSE, OPTIONAL, 'value']
const DRAWN_ON_VALUE: readonly string[] = ['id', ...PROSE, 'value']

// ── the prose a thing carries ────────────────────────────────────────────────
//
// A thing that says neither draws nothing at all, so a file that carries no prose costs no
// height. On a property or a value it is drawn under the second column, where what the row says
// about that name already is.
const Prose = ({ node, row = false }: { node: unknown; row?: boolean }) => {
  const title = isMapping(node) ? node[TITLE] : undefined
  const description = isMapping(node) ? node[DESCRIPTION] : undefined
  if (title === undefined && description === undefined) return null
  return (
    <div className={row ? 'd-prose d-prose-row' : 'd-prose'}>
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
  const { words, declared, imported } = useDrawing()
  if (declared.has(name)) {
    return (
      <span className="d-ref r-declared" title="declared in this document">
        <b aria-hidden="true">&#9670;</b>
        {name}
      </span>
    )
  }
  if (imported !== null && words.declared[name] !== undefined) {
    return (
      <span className="d-ref r-imported" title={`brought in by importing ${imported}`}>
        <b aria-hidden="true">&#9672;</b>
        {name}
        <i>{imported}</i>
      </span>
    )
  }
  return (
    <span className="d-ref r-undeclared" title="no scope this document imports declares it">
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
  const { words, marked } = useDrawing()
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
        {members.map((member, i) => (
          <tr key={i} className={markedRow(marked, where, i)}>
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
                    where={where === undefined ? undefined : `${where}[${i}].${name}`}
                  />
                )}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  )
}

const Val = ({
  value,
  type,
  className = '',
  where,
}: {
  value: unknown
  /** The type the value is written against, so what it is drawn as is known. */
  type?: unknown
  className?: string
  /** Where this value sits, so a highlight can point at a part of it. Nothing where unknown. */
  where?: string | undefined
}) => {
  const bodyOf = useBodyOf()
  const named = typeof type === 'string' && type.length > 0 ? type : null
  const primitive = named === null ? null : primitiveOf(bodyOf, named)

  const rows = rowsOf(bodyOf, type, value)
  const parts = primitive === ROOT && named !== null && isMapping(value)
  const expression = primitive === EXPRESSION && typeof value === 'string'
  const said = parts
    ? ' v-parts'
    : value === true
      ? ' v-true'
      : value === false
        ? ' v-false'
        : expression
          ? ' v-expr'
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

  return (
    <span className={at}>
      <i className="v-eq" aria-hidden="true">
        =
      </i>
      {expression ? (
        <>
          <i aria-hidden="true">/</i>
          {value}
          <i aria-hidden="true">/</i>
        </>
      ) : (
        text(value)
      )}
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
          {node.value !== undefined ? (
            <Val value={node.value} type={held} className="d-pval" />
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

export const Declaration = ({ at, node }: { at: string; node: unknown }) => {
  const { words } = useDrawing()

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

  return (
    <section className="d-decl" style={kindStyle(kind, words.kinds)}>
      <div className="d-decl-head">
        {external ? <span className="d-extern">{BOOTSTRAP.external}</span> : null}
        <span className="d-kind">{kind}</span>
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

      <Prose node={body} />

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

export const Page = ({
  words,
  doc,
  marks = NO_MARKS,
}: {
  words: Vocabulary
  doc: unknown
  /** What a highlight file says to mark here. A document with no highlight draws the same way. */
  marks?: Marks
}) => {
  const { marked, missing } = marks
  if (!isMapping(doc)) {
    return (
      <main>
        <DrawingContext.Provider
          value={{ words, declared: new Map(), imported: null, marked, missing }}
        >
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

  // A name this document does not declare is reached by importing the scope that declares it. The
  // one scope this page has is the one the vocabulary was read from, so that is the one it can
  // draw a name as coming from.
  const imported = importsScope(doc, words.scope) ? words.scope.join('.') : null

  return (
    <DrawingContext.Provider
      value={{ words, declared: declaredBodiesOf(doc), imported, marked, missing }}
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
              (declared as unknown[]).map((d, i) => (
                <Marked key={i} at={`${BOOTSTRAP.declare}[${i}]`}>
                  <Declaration at={`${BOOTSTRAP.declare}[${i}]`} node={d} />
                </Marked>
              ))
            )}
          </section>
        )}

        {/* A highlight that points at nothing is said here rather than left out, because a path
            that stopped resolving is the one thing a highlight cannot show by drawing. */}
        {missing.length === 0 ? null : (
          <section className="d-section">
            <h2 className="d-section-name">highlight names nothing</h2>
            {missing.map((path) => (
              <Err key={path} loose at={path}>
                names no node in this document.
              </Err>
            ))}
          </section>
        )}

        <div className="d-foot">
          <p>
            A declaration draws as its kind, its id, what it is, and its properties. An enum
            draws the one type it is based on and every value it lists, each with its own id and
            what that id stands for. A choice draws the types it is one of. A property is a name
            and a type, and a property holding many draws the collection it is and what that
            collection holds.
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
              A scope this document imports declares it, and the chip names that scope. The mark is
              a diamond within a diamond, because the name is reached rather than written here.
            </dd>

            <dt>
              <span className="d-ref r-undeclared" title="this document does not declare it">
                <b aria-hidden="true">&#9671;</b>a type<i>undeclared</i>
              </span>
            </dt>
            <dd>
              Neither this document nor any scope it imports declares it, so nothing here says what
              it is. The mark is hollow and the chip is <b className="c-undeclared">amber</b>.
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
