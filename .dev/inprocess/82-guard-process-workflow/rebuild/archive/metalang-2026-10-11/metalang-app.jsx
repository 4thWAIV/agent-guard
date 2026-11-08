// The only path from data to page is Draw. Draw looks up a component by what the node IS.
// Nothing matches -> an error renders. A component must also ACCOUNT FOR EVERY KEY on the
// node it draws; whatever it does not claim renders as an error beside it. Silence is never
// an option.
import React from 'react'
import { createRoot } from 'react-dom/client'
import { MODEL, EXAMPLE, INSTANCE } from './.metalang-data.mjs'

const DOC = MODEL
const DEFS = Object.entries(DOC).filter(([k, v]) => !k.startsWith('$') && v && typeof v === 'object')
const ANCHORS = new Map(DEFS.filter(([, d]) => d.$anchor).map(([n, d]) => [d.$anchor, n]))
const BY_NAME = Object.fromEntries(DEFS)

const frag = ref => String(ref).split('#')[1] ?? String(ref)

// Who points at what. Built once, so each entity can show what reaches it.
const INBOUND = new Map()
for (const [owner, def] of DEFS)
  for (const [partName, part] of Object.entries(def.properties ?? {}))
    for (const key of ['target', 'type'])
      if (typeof part[key] === 'string') {
        const t = frag(part[key])
        if (!INBOUND.has(t)) INBOUND.set(t, [])
        INBOUND.get(t).push({ owner, partName, owns: part.owns === 'yes' })
      }

// Parts a definition gets from what it extends, nearest supertype winning.
function inheritedParts(name, seen = new Set()) {
  if (seen.has(name)) return []
  seen.add(name)
  const def = BY_NAME[name]
  if (!def) return []
  return (def.extends ?? []).flatMap(r => {
    const sup = frag(r)
    const supDef = BY_NAME[ANCHORS.get(sup) ?? sup]
    if (!supDef) return []
    const own = Object.entries(supDef.properties ?? {}).map(([n, p]) => [n, p, sup])
    return [...own, ...inheritedParts(ANCHORS.get(sup) ?? sup, seen)]
  })
}

// ── the error control ─────────────────────────────────────────────────────────
function Unrenderable({ path, why, detail }) {
  return (
    <div className="err">
      <em>cannot render</em>
      <code>{path}</code>
      <b>{why}</b>
      {detail ? <pre>{detail}</pre> : null}
    </div>
  )
}

function Unplaced({ path, keys }) {
  if (!keys.length) return null
  return (
    <div className="err">
      <em>nothing renders</em>
      <code>{path}</code>
      <b>{keys.length === 1 ? 'this key' : 'these keys'}: {keys.join(', ')}</b>
    </div>
  )
}

const leftover = (node, claimed) => Object.keys(node).filter(k => !claimed.has(k))

// ── small pieces ──────────────────────────────────────────────────────────────
function Ref({ value }) {
  if (typeof value !== 'string')
    return <Unrenderable path="(reference)" why="a reference must be text" detail={JSON.stringify(value)} />
  const f = frag(value)
  if (!ANCHORS.has(f)) return <span className="dangling">{value}</span>
  return <a className="ref" href={'#def-' + f}>{f}</a>
}

function RefList({ value }) {
  const many = Array.isArray(value) ? value : [value]
  return <>{many.map((r, i) => <React.Fragment key={i}>{i ? <span className="comma">,</span> : null}<Ref value={r} /></React.Fragment>)}</>
}

function Kind({ of, children }) { return <span className={'kind k-' + of}>{children ?? of}</span> }

function TypeToken({ value }) {
  if (typeof value !== 'string')
    return <Unrenderable path="(type)" why="a type must be text" detail={JSON.stringify(value)} />
  const f = frag(value)
  const target = BY_NAME[ANCHORS.get(f) ?? f]
  if (!target) return <span className="dangling">{value}</span>
  const what = target.abstract === 'yes' ? 'abstract' : target.is
  return <a className={'tok t-' + what} href={'#def-' + f} title={what}>{f}</a>
}

// Cardinality, in crow's-foot notation — the notation a data model actually uses.
// Reading left to right along the line: the first mark is the minimum, the mark nearest the
// target is the maximum. A ring is zero, a bar is one, a foot is many.
const COLLECTION_KEYS = ['order', 'unique', 'unique_by', 'min_items', 'max_items']

function bounds(part) {
  const isCollection = COLLECTION_KEYS.some(k => part[k] !== undefined)
  const lo = part.min_items ?? (part.required === 'yes' ? 1 : 0)
  const hi = part.max_items ?? (isCollection ? Infinity : 1)
  return { lo, hi }
}

const label = ({ lo, hi }) =>
  lo === hi ? String(lo) : `${lo}..${hi === Infinity ? '*' : hi}`

const WORDS = { '1': 'exactly one', '0..1': 'none or one', '0..*': 'any number', '1..*': 'one or more' }

function Foot({ lo, hi }) {
  const ring = n => <circle cx={n} cy="9" r="3.4" />
  const bar = n => <line x1={n} y1="3.5" x2={n} y2="14.5" />
  return (
    <svg className="foot" viewBox="0 0 40 18" width="40" height="18" aria-hidden="true">
      <line className="stem" x1="0" y1="9" x2="38" y2="9" />
      {lo === 0 ? ring(13) : bar(13)}
      {hi === Infinity
        ? <path className="prongs" d="M26,9 L38,2.5 M26,9 L38,9 M26,9 L38,15.5" />
        : bar(27)}
    </svg>
  )
}

// The glyph draws four cases. Anything else — a bound like 2..5 — it cannot draw, and that
// row shows the numbers instead. Never both; one or the other says it.
const drawable = ({ lo, hi }) => (lo === 0 || lo === 1) && (hi === 1 || hi === Infinity)

// The four collection kinds, as one badge rather than two tags to cross-reference.
const KINDS = {
  'ordered|unique':    { name: 'Roster', ordered: true,  unique: true  },
  'ordered|dupes':     { name: 'List',   ordered: true,  unique: false },
  'unordered|unique':  { name: 'Set',    ordered: false, unique: true  },
  'unordered|dupes':   { name: 'Bag',    ordered: false, unique: false },
}

function collectionKind(part) {
  const { hi } = bounds(part)
  const isCollection = hi === Infinity || COLLECTION_KEYS.some(k => part[k] !== undefined)
  if (!isCollection) return null
  const ordered = part.order === 'significant'
  const unique = part.unique === 'yes' || part.unique_by !== undefined
  return KINDS[`${ordered ? 'ordered' : 'unordered'}|${unique ? 'unique' : 'dupes'}`]
}

function Collection({ kind }) {
  return (
    <span className={'coll c-' + kind.name.toLowerCase()}
          title={`${kind.name} — ${kind.ordered ? 'ordered' : 'unordered'}, ${kind.unique ? 'no two members equal' : 'duplicates allowed'}`}>
      <b>{kind.ordered ? '\u2192' : '\u00b7'}{kind.unique ? '\u2260' : '\u00b7'}</b>{kind.name}
    </span>
  )
}

function Cardinality({ part }) {
  const b = bounds(part)
  const text = label(b)
  const words = WORDS[text] ?? `${b.lo} to ${b.hi === Infinity ? 'any number' : b.hi}`
  return (
    <span className="card" title={words}>
      {drawable(b) ? <Foot lo={b.lo} hi={b.hi} /> : <i>{text}</i>}
    </span>
  )
}

// A short list reads inline. A long one collapses to its count and opens on demand, so one
// enum of nineteen format names cannot bury the row it belongs to.
const INLINE_MAX = 4

function Tag({ name, value }) {
  if (Array.isArray(value) && value.every(v => typeof v === 'string')) {
    if (value.length <= INLINE_MAX)
      return <span className="tag"><b>{name}</b> {value.join(', ')}</span>
    return (
      <details className="tag taglist">
        <summary><b>{name}</b> {value.length} values</summary>
        <div className="vals">{value.map(v => <code key={v}>{v}</code>)}</div>
      </details>
    )
  }
  if (typeof value === 'string' || typeof value === 'number')
    return <span className="tag"><b>{name}</b> {String(value)}</span>
  return <Unrenderable path={name} why="no renderer for this value shape" detail={JSON.stringify(value)} />
}

// ── parts ─────────────────────────────────────────────────────────────────────
const PART_PROSE = new Set(['is', 'title', 'description', 'type', 'target', 'required',
                            'min_items', 'max_items', 'order', 'unique'])

function PartRow({ path, name, node, keyName, from }) {
  const pointsAt = node.is === 'field' ? node.type : node.target
  const kind = collectionKind(node)
  const said = new Set(kind ? ['order', 'unique'] : [])
  const secondary = Object.entries(node).filter(([k]) => !PART_PROSE.has(k) && !said.has(k))
  const isLink = node.is === 'link'
  return (
    <tr className={(isLink ? 'is-link' : 'is-field') + (from ? ' is-inherited' : '')}>
      <td className="c-mark">
        {isLink
          ? <span className={'mark m-link' + (node.owns === 'yes' ? ' m-owns' : '')}
                  title={node.owns === 'yes' ? 'link — owns its target' : 'link — points at an entity'}>
              {node.owns === 'yes' ? '◆' : '→'}
            </span>
          : <span className="mark m-field" title="field — holds a value">■</span>}
      </td>
      <td className="c-name">
        <code>{name}</code>
        {keyName === name ? <span className="pk" title="identifies an instance">key</span> : null}
      </td>
      <td className="c-card"><Cardinality part={node} /></td>
      <td className="c-type">{pointsAt ? <TypeToken value={pointsAt} /> : <span className="none">—</span>}</td>
      <td className="c-tags">
        <div className="tags">
          {kind ? <Collection kind={kind} /> : null}
          {secondary.map(([k, v]) => <Tag key={k} name={k} value={v} />)}
        </div>
      </td>
      <td className="c-desc">
        {node.title ?? <Unrenderable path={path} why="a part must have a title" />}
        {node.description ? <div className="pdetail">{node.description}</div> : null}
      </td>
      <td className="c-from">{from ? <a className="fromtag" href={'#def-' + from}>{from}</a> : null}</td>
    </tr>
  )
}

function OptionRow({ path, name, node }) {
  return (
    <tr className="is-option">
      <td className="c-mark"><span className="mark m-option" title="one named value">●</span></td>
      <td className="c-name"><code>{name}</code></td>
      <td className="c-card" />
      <td className="c-coll" />
      <td className="c-type" />
      <td className="c-tags" />
      <td className="c-desc">
        {node.title ?? <Unrenderable path={path} why="an option must have a title" />}
        <Unplaced path={path} keys={leftover(node, new Set(['is', 'title', 'description']))} />
      </td>
      <td className="c-from" />
    </tr>
  )
}

function Rows({ path, label, nodes, keyName, inherited = [] }) {
  const ownNames = new Set(Object.keys(nodes))
  const shown = inherited.filter(([n], i, a) => !ownNames.has(n) && a.findIndex(x => x[0] === n) === i)
  return (
    <>
      <div className="rowshead">
        {label}
        <span className="rowcount">{Object.keys(nodes).length} own{shown.length ? ` · ${shown.length} inherited` : ''}</span>
      </div>
      <table><tbody>
        {Object.entries(nodes).map(([n, p]) =>
          <Draw key={n} path={path + '.' + n} name={n} node={p} keyName={keyName} />)}
        {shown.map(([n, p, from]) =>
          <Draw key={'i-' + n} path={path + '.' + n} name={n} node={p} keyName={keyName} from={from} />)}
      </tbody></table>
    </>
  )
}

function ReferencedBy({ name }) {
  const inbound = INBOUND.get(name) ?? []
  if (!inbound.length) return null
  return (
    <div className="inbound">
      <i>referenced by</i>
      {inbound.map((r, i) =>
        <span className="inref" key={i}>
          {r.owns ? <span className="owns">◆</span> : null}
          <a href={'#def-' + (BY_NAME[r.owner]?.$anchor ?? r.owner)}>{r.owner}</a>
          <em>.{r.partName}</em>
        </span>)}
    </div>
  )
}

// ── definitions ───────────────────────────────────────────────────────────────
function Head({ name, node }) {
  return (
    <h2>
      <span className="dname">{name}</span>
      <Kind of={node.is} />
      {node.abstract === 'yes' ? <Kind of="abstract" /> : null}
      {node.extends ? <span className="ext">extends <RefList value={node.extends} /></span> : null}
    </h2>
  )
}

function Prose({ path, node }) {
  return (
    <>
      {node.title ? <p className="title">{node.title}</p> : <Unrenderable path={path} why="a definition must have a title" />}
      {node.description ? <p className="desc">{node.description}</p> : null}
    </>
  )
}

const DEF_CLAIMED = ['$anchor', 'is', 'title', 'description']

function EntityCard({ path, name, node }) {
  const claimed = new Set([...DEF_CLAIMED, 'abstract', 'extends', 'identified_by', 'properties', 'closed'])
  return (
    <section className="def is-entity" id={'def-' + (node.$anchor ?? name)}>
      <Head name={name} node={node} />
      <Prose path={path} node={node} />
      {node.closed === 'no' ? <p className="of">parts not named here are allowed</p> : null}
      {node.properties
        ? <Rows path={path} label="parts" nodes={node.properties}
                keyName={node.identified_by ? frag(node.identified_by) : null}
                inherited={inheritedParts(name)} />
        : null}
      <ReferencedBy name={node.$anchor ?? name} />
      <Unplaced path={path} keys={leftover(node, claimed)} />
    </section>
  )
}

const REQ_KEYS = ['shaped_as', 'pattern', 'one_of', 'minimum', 'maximum', 'min_length', 'max_length']

function ScalarCard({ path, name, node }) {
  const claimed = new Set([...DEF_CLAIMED, 'extends', 'of', ...REQ_KEYS])
  const reqs = Object.entries(node).filter(([k]) => REQ_KEYS.includes(k))
  return (
    <section className="def is-value" id={'def-' + (node.$anchor ?? name)}>
      <Head name={name} node={node} />
      <Prose path={path} node={node} />
      <p className="of">holds <code>{node.of ?? '—'}</code></p>
      {reqs.length ? <div className="tags">{reqs.map(([k, v]) => <Tag key={k} name={k} value={v} />)}</div> : null}
      <ReferencedBy name={node.$anchor ?? name} />
      <Unplaced path={path} keys={leftover(node, claimed)} />
    </section>
  )
}

function ChoiceCard({ path, name, node }) {
  const claimed = new Set([...DEF_CLAIMED, 'extends', 'options'])
  return (
    <section className="def is-value" id={'def-' + (node.$anchor ?? name)}>
      <Head name={name} node={node} />
      <Prose path={path} node={node} />
      {node.options
        ? <Rows path={path} label="options" nodes={node.options} keyName={null} />
        : <Unrenderable path={path} why="a choice with no options offers nothing" />}
      <ReferencedBy name={node.$anchor ?? name} />
      <Unplaced path={path} keys={leftover(node, claimed)} />
    </section>
  )
}

function DocumentCard({ path, name, node }) {
  const claimed = new Set([...DEF_CLAIMED, '$id', 'defines'])
  return (
    <div className="top" id={'def-' + (node.$anchor ?? name)}>
      <p className="eyebrow">data model</p>
      <h1>{node.title}</h1>
      {node.description ? <p className="sub">{node.description}</p> : null}
      <div className="count">
        {DEFS.length} definitions · {ANCHORS.size} anchors · <code>{node.$id}</code>
      </div>
      <Unplaced path={path} keys={leftover(node, claimed)} />
    </div>
  )
}


// ── the diagram ───────────────────────────────────────────────────────────────
// Entities laid out by how deep their inheritance goes. Inheritance edges carry the hollow
// triangle that points at the supertype; containment arcs carry the filled diamond at the
// owner. Plain references are left out on purpose — they are in each card's referenced-by
// line, and drawing them all makes a hairball.
const BOX_H = 36, ROW_GAP = 86, GAP = 22, PAD = 18, ARC = 74

function layout() {
  const entities = DEFS.filter(([, d]) => d.is === 'entity')
  const depthOf = (name, seen = new Set()) => {
    if (seen.has(name)) return 0
    seen.add(name)
    const d = BY_NAME[name]
    const sups = (d?.extends ?? []).map(r => ANCHORS.get(frag(r)) ?? frag(r)).filter(n => BY_NAME[n])
    return sups.length ? 1 + Math.max(...sups.map(n => depthOf(n, seen))) : 0
  }
  const nodes = entities.map(([name, def]) => ({
    name, def, depth: depthOf(name), w: Math.max(86, name.length * 8.6 + 26),
  }))
  const rows = [...new Set(nodes.map(n => n.depth))].sort()
  const widest = Math.max(...rows.map(r => {
    const inRow = nodes.filter(n => n.depth === r)
    return inRow.reduce((a, n) => a + n.w, 0) + GAP * (inRow.length - 1)
  }))
  for (const r of rows) {
    const inRow = nodes.filter(n => n.depth === r)
    const total = inRow.reduce((a, n) => a + n.w, 0) + GAP * (inRow.length - 1)
    let x = PAD + (widest - total) / 2
    for (const n of inRow) { n.x = x; n.y = ARC + r * ROW_GAP; x += n.w + GAP }
  }
  const at = Object.fromEntries(nodes.map(n => [n.name, n]))
  const inherits = nodes.flatMap(n => (n.def.extends ?? [])
    .map(r => at[ANCHORS.get(frag(r)) ?? frag(r)]).filter(Boolean).map(sup => ({ from: n, to: sup })))
  const contains = nodes.flatMap(n => Object.entries(n.def.properties ?? {})
    .filter(([, p]) => p.owns === 'yes' && at[ANCHORS.get(frag(p.target ?? '')) ?? ''])
    .map(([partName, p]) => ({ from: n, to: at[ANCHORS.get(frag(p.target))], partName })))
  return { nodes, inherits, contains, width: widest + PAD * 2,
           height: ARC + (rows.length - 1) * ROW_GAP + BOX_H + 16 }
}

function Diagram() {
  const { nodes, inherits, contains, width, height } = layout()
  const cx = n => n.x + n.w / 2
  return (
    <figure className="diagram">
      <svg viewBox={`0 0 ${width} ${height}`} width="100%" role="img"
           aria-label="MetaLang metaclasses, inheritance and containment">
        <defs>
          <marker id="tri" viewBox="0 0 12 12" refX="11" refY="6" markerWidth="11" markerHeight="11" orient="auto">
            <path d="M0,0 L12,6 L0,12 Z" fill="#fff" stroke="#4b5363" strokeWidth="1.3" />
          </marker>
          <marker id="dia" viewBox="0 0 14 10" refX="1" refY="5" markerWidth="13" markerHeight="10" orient="auto">
            <path d="M0,5 L6,0 L12,5 L6,10 Z" fill="#8a5212" />
          </marker>
        </defs>

        {contains.map((e, i) => {
          const x1 = cx(e.from), x2 = cx(e.to), top = Math.min(e.from.y, e.to.y)
          // The arc never rises past the space reserved for it, however wide the span.
          const lift = top - Math.min(ARC - 16, 16 + Math.abs(x2 - x1) * 0.13)
          const apex = 0.25 * top + 0.75 * lift
          return (
            <g key={'c' + i} className="e-contain">
              <path d={`M${x1},${e.from.y} C${x1},${lift} ${x2},${lift} ${x2},${e.to.y}`}
                    fill="none" markerStart="url(#dia)" />
              <text x={(x1 + x2) / 2} y={apex - 7} textAnchor="middle">{e.partName}</text>
            </g>
          )
        })}

        {inherits.map((e, i) => {
          const midY = e.to.y + BOX_H + (e.from.y - e.to.y - BOX_H) / 2
          return (
            <path key={'i' + i} className="e-inherit" markerEnd="url(#tri)" fill="none"
                  d={`M${cx(e.from)},${e.from.y} V${midY} H${cx(e.to)} V${e.to.y + BOX_H + 1}`} />
          )
        })}

        {nodes.map(n => (
          <a key={n.name} href={'#def-' + (n.def.$anchor ?? n.name)}>
            <g className={'n' + (n.def.abstract === 'yes' ? ' is-abstract' : '')}>
              <rect x={n.x} y={n.y} width={n.w} height={BOX_H} rx="7" />
              <text x={cx(n)} y={n.y + BOX_H / 2 + 4.5} textAnchor="middle">{n.name}</text>
            </g>
          </a>
        ))}
      </svg>
      <figcaption>
        Every metaclass. The hollow triangle points at a supertype; the filled diamond sits on
        the owner of what it contains. A dashed box is abstract. Plain references are left out —
        each card lists what reaches it.
      </figcaption>
    </figure>
  )
}



// ── the cheatsheet ────────────────────────────────────────────────────────────
// What you are allowed to write inside each metaclass, split into what must be there and
// what may be. Read out of the model, never typed here.
function vocabulary(name, seen = new Set()) {
  if (seen.has(name)) return { required: [], allowed: [] }
  seen.add(name)
  const def = BY_NAME[name]
  if (!def) return { required: [], allowed: [] }
  const own = Object.entries(def.properties ?? {})
  const inherited = (def.extends ?? []).map(r => vocabulary(ANCHORS.get(frag(r)) ?? frag(r), seen))
  return {
    required: [...own.filter(([, p]) => p.required === 'yes').map(([k]) => k), ...inherited.flatMap(v => v.required)],
    allowed: [...own.filter(([, p]) => p.required !== 'yes').map(([k]) => k), ...inherited.flatMap(v => v.allowed)],
  }
}

function Cheatsheet() {
  const concrete = DEFS.filter(([, d]) => d.is === 'entity' && d.abstract !== 'yes')
  return (
    <section className="sheet">
      <div className="sheethead">
        <h3>What you may write</h3>
        <span>every keyword each metaclass accepts, read out of the model</span>
      </div>
      <div className="sheetgrid">
        {concrete.map(([name, def]) => {
          const v = vocabulary(name)
          return (
            <div className="sheetcard" key={name}>
              <h4><a href={'#def-' + (def.$anchor ?? name)}>{name}</a></h4>
              <div className="sheetrow">
                <i>must</i>
                <span>{v.required.map(k => <code className="must" key={k}>{k}</code>)}</span>
              </div>
              <div className="sheetrow">
                <i>may</i>
                <span>{v.allowed.map(k => <code key={k}>{k}</code>)}</span>
              </div>
            </div>
          )
        })}
      </div>
    </section>
  )
}

// ── the neighbourhood ─────────────────────────────────────────────────────────
// One definition in the middle, everything that reaches it on the left, everything it
// reaches on the right. Click a box to re-centre. The whole link graph, one hop at a time.
const N_W = 150, N_H = 30, N_GAP = 12, N_COL = 230

function neighbours(name) {
  const out = []
  for (const [p, part] of Object.entries(BY_NAME[name]?.properties ?? {})) {
    const ref = part.is === 'field' ? part.type : part.target
    if (typeof ref !== 'string') continue
    const t = ANCHORS.get(frag(ref)) ?? frag(ref)
    if (BY_NAME[t]) out.push({ other: t, part: p, owns: part.owns === 'yes', is: part.is })
  }
  const inn = []
  for (const [owner, def] of DEFS)
    for (const [p, part] of Object.entries(def.properties ?? {})) {
      const ref = part.is === 'field' ? part.type : part.target
      if (typeof ref !== 'string') continue
      if ((ANCHORS.get(frag(ref)) ?? frag(ref)) === name)
        inn.push({ other: owner, part: p, owns: part.owns === 'yes', is: part.is })
    }
  return { inn, out }
}

function Neighbourhood() {
  const [focus, setFocus] = React.useState(DEFS[0][0])
  const { inn, out } = neighbours(focus)
  const rows = Math.max(inn.length, out.length, 1)
  const h = rows * (N_H + N_GAP) + 40
  const mid = h / 2
  const yAt = (i, n) => mid - ((n - 1) * (N_H + N_GAP)) / 2 + i * (N_H + N_GAP)
  const W = N_COL * 2 + N_W + 40
  const box = (name, x, y, cls, onClick) => (
    <g key={cls + name + y} className={'nn ' + cls} onClick={onClick}>
      <rect x={x} y={y - N_H / 2} width={N_W} height={N_H} rx="6" />
      <text x={x + N_W / 2} y={y + 4}>{name}</text>
    </g>
  )
  return (
    <figure className="hood">
      <div className="hoodhead">
        <h3>What <code>{focus}</code> touches</h3>
        <span>click any box to re-centre</span>
      </div>
      <div className="hoodscroll">
        <svg viewBox={`0 0 ${W} ${h}`} width={W} height={h}>
          {inn.map((e, i) => {
            const y = yAt(i, inn.length)
            const x1 = 20 + N_W, x2 = 20 + N_COL - 8
            return (
              <g key={'i' + i} className={'edge' + (e.owns ? ' is-owns' : '')}>
                <path d={`M${x1},${y} C${x1 + 44},${y} ${x2 - 44},${mid} ${x2},${mid}`}
                      markerEnd="url(#nArrow)" />
                <text x={x1 + 9} y={y - 7}>{e.part}</text>
              </g>
            )
          })}
          {out.map((e, i) => {
            const y = yAt(i, out.length)
            const x1 = 20 + N_COL + N_W, x2 = 20 + N_COL * 2 - 8
            return (
              <g key={'o' + i} className={'edge' + (e.owns ? ' is-owns' : '')}>
                <path d={`M${x1},${mid} C${x1 + 44},${mid} ${x2 - 44},${y} ${x2},${y}`}
                      markerEnd="url(#nArrow)" />
                <text className="end" x={x2 - 9} y={y - 7}>{e.part}</text>
              </g>
            )
          })}
          <defs>
            <marker id="nArrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto">
              <path d="M0,1 L9,5 L0,9" fill="none" stroke="#8a5212" strokeWidth="1.6" />
            </marker>
          </defs>
          {inn.map((e, i) => box(e.other, 20, yAt(i, inn.length), 'is-in', () => setFocus(e.other)))}
          {out.map((e, i) => box(e.other, 20 + N_COL * 2, yAt(i, out.length), 'is-out', () => setFocus(e.other)))}
          {box(focus, 20 + N_COL, mid, 'is-focus', null)}
        </svg>
      </div>
      <figcaption>
        Everything that reaches <code>{focus}</code> on the left, everything it reaches on the
        right, with the part that makes each connection written on the line. A thicker line is
        ownership.
      </figcaption>
    </figure>
  )
}

// ── a language written in MetaLang ────────────────────────────────────────────
// The source text as a person types it, with every metaclass name linked back to the card
// that defines it. Nothing is reformatted; this is the file.
function Line({ text }) {
  if (/^\s*#/.test(text) || text.trim() === '') return <span className="cmt">{text || ' '}</span>
  const m = text.match(/^(\s*)(is|type|target|identified_by):\s*(.+)$/)
  if (m) {
    const [, pad, key, raw] = m
    const name = frag(raw.replace(/['"]/g, ''))
    const target = BY_NAME[ANCHORS.get(name) ?? name]
    if (target) {
      const what = target.abstract === 'yes' ? 'abstract' : target.is
      return (
        <>{pad}<span className="k">{key}:</span>{' '}
          <a className={'lnk t-' + what} href={'#def-' + name} title={`a ${what} defined in MetaLang`}>{raw}</a>
        </>
      )
    }
  }
  const kv = text.match(/^(\s*)([A-Za-z_$][\w.$]*):(.*)$/)
  if (kv) {
    const [, pad, key, rest] = kv
    return <>{pad}<span className="k">{key}:</span><span className={/['"]/.test(rest) ? 'str' : ''}>{rest}</span></>
  }
  return <>{text}</>
}

function Source({ text }) {
  return <pre>{text.replace(/\n+$/, '').split('\n').map((l, i) =>
    <React.Fragment key={i}><Line text={l} />{'\n'}</React.Fragment>)}</pre>
}

function WrittenIn() {
  return (
    <section className="usedin">
      <div className="usedinhead">
        <h3>The stack, working</h3>
        <span>each document is checked against the one below it</span>
      </div>
      <div className="stack">
        <div>
          <div className="layer">
            <span className="layernum">layer 2</span>
            <span className="layername">a language</span>
            <span className="layerwhat">the id — a namespaced name whose parts may repeat</span>
          </div>
          <Source text={EXAMPLE} />
          <div className="verdict">Legal MetaLang — checked against the metalanguage.</div>
        </div>
        <div>
          <div className="layer">
            <span className="layernum">layer 3</span>
            <span className="layername">a document</span>
            <span className="layerwhat">written in the id language, not in MetaLang</span>
          </div>
          <Source text={INSTANCE} />
          <div className="verdict">Legal ids — checked against the language beside it.</div>
        </div>
      </div>
    </section>
  )
}

// ── the registry, and the only path from data to page ─────────────────────────
const RENDERERS = { document: DocumentCard, entity: EntityCard, scalar: ScalarCard, choice: ChoiceCard, field: PartRow, link: PartRow, option: OptionRow }

function Draw({ path, name, node, keyName, from }) {
  if (!node || typeof node !== 'object')
    return <Unrenderable path={path} why="not a definition" detail={JSON.stringify(node)} />
  if (node.is === undefined)
    return <Unrenderable path={path} why="no `is`, so nothing can know what draws it" />
  const Component = RENDERERS[node.is]
  if (!Component)
    return <Unrenderable path={path} why={`nothing knows how to render a "${node.is}"`} />
  return <Component path={path} name={name} node={node} keyName={keyName} from={from} />
}

function Coverage() {
  const declared = DEFS.filter(([, d]) => d.is === 'entity' && d.abstract !== 'yes').map(([n]) => n)
  const uncovered = declared.filter(n => !RENDERERS[n])
  if (!uncovered.length) return null
  return (
    <div className="err">
      <em>no component</em>
      <b>these metaclasses are declared in the file and nothing renders them: {uncovered.join(', ')}</b>
    </div>
  )
}

const STRUCTURE = ['entity']
const VALUES = ['scalar', 'choice']

function FootKey() {
  const counts = [
    { lo: 1, hi: 1, text: 'exactly one' },
    { lo: 0, hi: 1, text: 'none or one' },
    { lo: 1, hi: Infinity, text: 'one or more' },
    { lo: 0, hi: Infinity, text: 'any number' },
  ]
  return (
    <div className="key">
      <div className="keyrow">
        <i>part</i>
        <span><span className="mark m-field">■</span>holds a value</span>
        <span><span className="mark m-link">→</span>points at an entity</span>
        <span><span className="mark m-link m-owns">◆</span>owns what it points at</span>
      </div>
      <div className="keyrow">
        <i>type</i>
        <span><span className="tok t-scalar">scalar</span></span>
        <span><span className="tok t-choice">choice</span></span>
        <span><span className="tok t-entity">entity</span></span>
        <span><span className="tok t-abstract">abstract</span></span>
      </div>
      <div className="keyrow">
        <i>how many</i>
        {counts.map((c, n) => <span key={n}><Foot lo={c.lo} hi={c.hi} />{c.text}</span>)}
      </div>
      <div className="keyrow">
        <i>collection</i>
        {Object.values(KINDS).map(k => (
          <span key={k.name}>
            <Collection kind={k} />
            {k.ordered ? 'ordered' : 'unordered'}, {k.unique ? 'unique' : 'duplicates'}
          </span>
        ))}
      </div>
    </div>
  )
}

function Group({ title, lede, members }) {
  if (!members.length) return null
  return (
    <>
      <div className="grouphead"><h3>{title}</h3><span>{lede}</span></div>
      {members.map(([n, d]) => <Draw key={n} path={n} name={n} node={d} />)}
    </>
  )
}

function App() {
  const structure = DEFS.filter(([, d]) => STRUCTURE.includes(d.is))
  const values = DEFS.filter(([, d]) => VALUES.includes(d.is))
  const other = DEFS.filter(([, d]) => ![...STRUCTURE, ...VALUES].includes(d.is))
  return (
    <main>
      <Draw path="$" name="$" node={{ is: 'document', $id: DOC.$id, title: DOC.title, description: DOC.description }} />
      <Coverage />
      <nav>{DEFS.map(([n, d]) => <a key={n} href={'#def-' + (d.$anchor ?? n)}><Kind of={d.is}>{n}</Kind></a>)}</nav>
      <Diagram />
      <Neighbourhood />
      <FootKey />
      <Group title="Structure" lede="what has parts" members={structure} />
      <Group title="Value types" lede="what a part may hold" members={values} />
      <Group title="Everything else" lede="declared in the file and not grouped above" members={other} />
      <WrittenIn />
      <Cheatsheet />
    </main>
  )
}

createRoot(document.getElementById('root')).render(<App />)
