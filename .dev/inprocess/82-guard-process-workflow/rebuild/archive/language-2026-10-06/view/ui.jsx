// THE DRAWINGS. One component per kind, and one way to reach any of them.
//
// `Draw` is the only path from data to a page. It looks the node's kind up in DEVICES and
// renders that component. There is no branch that prints a string, no fallthrough that
// emits ordinary text, and no component that takes a name instead of a node — so the two
// ways the old renderers went wrong are both unreachable rather than forbidden.
//
// The component for a node nothing describes is `Undescribed`. That is the whole of the
// fail-closed behaviour: it is what you get by default, not what a flag turns on.
import React, { createContext, useContext, useId } from 'react'
import { vocab, toneOf } from './model.mjs'

// Opening a definition is state, not a DOM trick. A reference asks for one by name and the
// panel appears beneath the block it was asked from.
export const DefsContext = createContext({ byName: {}, open: null, setOpen: () => {} })


export const Draw = ({ node }) => {
  if (node == null) return <Undescribed node={{ why: 'nothing here', at: '', value: node }} />
  if (Array.isArray(node)) return <>{node.map((n, i) => <Draw key={i} node={n} />)}</>
  const C = DEVICES[node.kind]
  if (!C) return <Undescribed node={{ why: `no component draws a "${node.kind}"`, at: '', value: node }} />
  // A mark is drawn HERE too. The highlight file names a node at any depth; the node
  // carries what it was named with; no component implements marking and nothing holds a
  // list of what may be marked.
  // `mark` belongs to the language already — it is a form's or an alteration's glyph — so
  // this one is `highlight`. Naming it `mark` made every form and alteration in the
  // architecture look marked.
  if (node.highlight) {
    return (
      <div className="d-mark">
        <div className="d-mark-flag"><b>MARKED</b><span>{node.highlight.note}</span></div>
        <div className="d-mark-body"><Draw node={{ ...node, highlight: null }} /></div>
      </div>
    )
  }
  // A node's gaps are drawn HERE, not by the component. Three components forgot to draw
  // theirs, so 109 gaps were counted at the top of the page and shown nowhere on it. A
  // component cannot hide a gap by forgetting it any more.
  const gaps = node.extra ?? node.bad ?? null
  if (!gaps?.length) return <C node={node} />
  return <><C node={node} />{gaps.map((g, i) => <Draw key={i} node={g} />)}</>
}

// Two reference lists that say the same thing. A phase repeating its parent's inputs is
// true and is noise.
const sameRefs = (a, b) => Boolean(a?.length) && a.length === b?.length &&
  a.every((x, i) => x.name === b[i]?.name)

const Each = ({ nodes }) => <>{(nodes ?? []).map((n, i) => <Draw key={i} node={n} />)}</>

// The shapes the notation can draw. The language names which one a condition uses; this is
// the only place a shape is described, the way a font describes a character.
const ICONS = {
  magnifier: (
    <>
      <circle cx="10" cy="10" r="6" />
      <path d="M14.5 14.5 L20 20" />
    </>
  ),
  grid: (
    <>
      <rect x="3.5" y="3.5" width="7" height="7" />
      <rect x="13.5" y="3.5" width="7" height="7" />
      <rect x="3.5" y="13.5" width="7" height="7" />
      <rect x="13.5" y="13.5" width="7" height="7" />
    </>
  ),
  check: <path d="M4 12.5 L9.5 18 L20 6" />,
  speech: (
    <>
      <path d="M3.5 6.5a2 2 0 0 1 2-2h13a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H9l-4.5 4v-4h-1a2 2 0 0 1-2-2z" />
      <path d="M7.5 9.5h9" />
      <path d="M7.5 12.5h5" />
    </>
  ),
  pen: (
    <>
      <path d="M4 20.5l1.2-4.2L16.1 5.4a2.2 2.2 0 0 1 3.1 3.1L8.2 19.3z" />
      <path d="M14.4 7.1l3.1 3.1" />
      <path d="M4 20.5h16" />
    </>
  ),
  fingerprint: (
    <>
      <path d="M4 11a8 8 0 0 1 16 0" />
      <path d="M6.8 12.2a5.2 5.2 0 0 1 10.4 0c0 2.6-.5 5-1.4 7.2" />
      <path d="M9.6 12.2a2.4 2.4 0 0 1 4.8 0c0 3-.5 5.8-1.5 8.3" />
      <path d="M12 12.2v3.1" />
      <path d="M4.6 15.4c.5-1 .8-2.1.8-3.2" />
      <path d="M7.1 19.6c.7-1.2 1.2-2.5 1.5-3.9" />
    </>
  ),
}

const Icon = ({ name, title }) => {
  const shape = ICONS[name]
  if (!shape) return <Undescribed node={{ why: `the notation cannot draw a "${name}"`, value: name }} />
  return (
    <svg className="d-icon" viewBox="0 0 24 24" role="img" aria-label={title}>
      <title>{title}</title>
      {shape}
    </svg>
  )
}

// A declared value, whatever shape it is. Stringifying a structure gave "[object Object]"
// — a field with something inside it told the reader nothing at all.
const Value = ({ v }) => {
  if (v == null) return <b>&mdash;</b>
  if (Array.isArray(v)) return <b>{v.map((x) => String(x)).join(' · ')}</b>
  if (typeof v === 'object') {
    return (
      <span className="d-subfields">
        {Object.entries(v).map(([k, x]) => (
          <span className="d-subfield" key={k}><i>{k}</i><Value v={x} /></span>
        ))}
      </span>
    )
  }
  return <b>{String(v)}</b>
}

// ── the error display ────────────────────────────────────────────────────────────
// What the offending value IS, in a few words. A JSON dump of the whole node told the
// reader nothing and swallowed the row; the full value stays available on hover.
const label = (v) => {
  if (v == null) return null
  if (typeof v !== 'object') return String(v).slice(0, 48)
  const named = v.name ?? v.id ?? v.slug ?? v.kind ?? v.type
  if (named) return String(named).slice(0, 48)
  const keys = Object.keys(v)
  return keys.length ? `{ ${keys.slice(0, 3).join(', ')}${keys.length > 3 ? ', …' : ''} }` : '{ }'
}
const detail = (v) => (v == null ? '' : typeof v === 'object' ? JSON.stringify(v) : String(v))

const Undescribed = ({ node }) => {
  const short = label(node.value)
  return (
    <span className="d-err" title={[node.at, detail(node.value)].filter(Boolean).join('\n')}>
      <em>!</em>
      <b>{node.why}</b>
      {short && <code>{short}</code>}
    </span>
  )
}

// A part nobody has said anything about. The work is undesigned here — not the drawing of
// it — so it is the heaviest mark on the page, heavier than an error, because an error is
// something written wrongly and this is something not written at all.
const NotStated = ({ node }) => (
  <span className="d-notstated" title={node.what}>
    <em>not stated</em>
    <b>{node.part}</b>
    <i>on this {node.device}</i>
  </span>
)

const Undesigned = ({ node }) => (
  <div className="d-undesigned" id={`proc-${node.name}`}>
    <span className="d-undesigned-mark">not designed yet</span>
    <b>{node.name ?? 'unnamed'}</b>
    <span>No members, no research, no questions, no gate.</span>
  </div>
)

// ── the notation's own marks ─────────────────────────────────────────────────────
const KindChip = ({ node }) => (
  <em className={`d-chip k-${node.id}`} title={node.name}>{node.mark}</em>
)

// How far out a visibility reaches is DECLARED by the visibility, not counted here from
// its id. Adding a fourth one used to draw zero bars and say nothing.
const Reach = ({ node }) => {
  const tone = toneOf(node)
  if (tone?.miss) return <Undescribed node={{ why: tone.miss, value: node.id }} />
  if (node.reach === undefined) {
    return <Undescribed node={{ why: `"${node.id}" does not declare how far it reaches`, value: node.id }} />
  }
  return (
    <b className="d-reach" title={node.name} style={tone?.style}>
      {[1, 2, 3].map((i) => <i key={i} className={i <= node.reach ? 'on' : ''} />)}
    </b>
  )
}

const Alteration = ({ node }) =>
  node.id === 'unchanged' ? null
    : <em className={`d-alt a-${node.id}`} title={node.name}>{node.mark}</em>

// ── a type reference ─────────────────────────────────────────────────────────────
const Reference = ({ node }) => {
  const { byName, open, setOpen } = useContext(DefsContext)
  // Keyed by THIS occurrence. Keying by name alone opened the panel at every place the
  // name appeared, which is three panels for one click.
  const uid = useId()
  if (node.gap) return <Draw node={node.gap} />
  const badge = { defined: '◇', primitive: '#', external: '↗', parameter: 'T' }[node.resolves]
  const head = <><em className="d-ref-badge">{badge}</em><b>{node.name}</b></>
  const openable = node.resolves === 'defined' && Boolean(byName[node.name])
  const isOpen = open?.id === uid
  const chip = openable
    ? <button type="button" className={`d-ref r-defined${isOpen ? ' is-open' : ''}`}
        onClick={() => setOpen(isOpen ? null : { id: uid, name: node.name })}
        title="Defined here. Click to open.">{head}</button>
    : <span className={`d-ref r-${node.resolves}`}>{head}</span>
  // The angle brackets wrap the WHOLE reference. They used to sit inside the first chip and
  // outside the last, so a generic read as three separate things instead of one.
  const withPanel = (el) => isOpen ? <>{el}<Panel name={node.name} /></> : el
  if (node.wrongArity) return <>{withPanel(chip)}<Draw node={node.wrongArity} /></>
  if (!node.arguments.length) return withPanel(chip)
  return withPanel(
    <span className="d-generic">
      {chip}
      <i className="d-angle">&lt;</i>
      {node.arguments.map((a, i) => (
        <React.Fragment key={i}>{i > 0 && <i className="d-comma">,</i>}<Draw node={a} /></React.Fragment>
      ))}
      <i className="d-angle">&gt;</i>
    </span>
  )
}

// A definition, opened where it was asked for.
export const Panel = ({ name }) => {
  const { byName, setOpen } = useContext(DefsContext)
  const node = byName[name]
  if (!node) return <Undescribed node={{ why: `nothing defines "${name}"`, value: name }} />
  return (
    <div className="d-panel">
      <div className="d-panel-head"><span>{name}</span>
        <button type="button" className="d-panel-close" onClick={() => setOpen(null)}>×</button></div>
      <div className="d-panel-body"><Draw node={node} /></div>
    </div>
  )
}

const Refs = ({ label, nodes, note }) => !nodes?.length ? null : (
  <div className="d-io">
    <i>{label}</i>
    <span>{nodes.map((n, idx) => (
      <React.Fragment key={idx}>{idx > 0 && <i className="d-or">or</i>}<Draw node={n} /></React.Fragment>
    ))}</span>
    {note && <em>{note}</em>}
  </div>
)

// ── a process, its steps, its research, its questions, its gates ─────────────────
const Process = ({ node, depth = 0, parentTakes }) => {
  if (node.undesigned) return <Draw node={node.undesigned} />
  const entry = node.gates.filter((g) => g.spec?.id === 'entry')
  const exit = node.gates.filter((g) => g.spec?.id === 'exit')
  const other = node.gates.filter((g) => g.kind === 'undescribed')
  return (
    <div className={`d-process lvl-${Math.min(depth, 2)}`}>
      <div className="d-process-head">
        <Draw node={{ kind: 'form', ...vocab.form.process }} />
        <b>{node.name ?? <Undescribed node={{ why: 'a process with no name', value: null }} />}</b>
        <i className="d-count">{node.members.length} members</i>
      </div>
      {sameRefs(node.takes, parentTakes) ? null : <Refs label="takes" nodes={node.takes} />}
      <Each nodes={entry} />
      <Research nodes={node.research} />
      <Questions nodes={node.questions} />
      {node.note && <p className="d-note">{node.note}</p>}
      <div className="d-members">
        {node.members.map((mem, i) => (
          <div className="d-member" key={i}>
            <i className="d-ord">{i + 1}</i>
            <div>{mem.kind === 'process'
              ? <Process node={mem} depth={depth + 1} parentTakes={node.takes} />
              : <Draw node={mem} />}</div>
          </div>
        ))}
      </div>
      <Each nodes={other} />
      <Each nodes={exit} />
      {node.repeats && <Draw node={node.repeats} />}
      <Refs label="produces" nodes={node.produces} />
    </div>
  )
}

const Step = ({ node }) => (
  <div className={`d-step${node.actor ? '' : ' act-unknown'}`}
    style={node.actor ? toneOf(node.actor)?.style : undefined}>
    <div className="d-step-top">
      <b>{node.unnamed ? <Draw node={node.unnamed} /> : node.name}</b>
      {node.actor
        ? <em className="d-actor">{node.actor.name}</em>
        : <Draw node={node.badActor} />}
      {node.fansOut && <span className="d-fanout">{node.fansOut}</span>}
    </div>
    <Refs label="takes" nodes={node.takes} />
    <Refs label="produces" nodes={node.produces} />
    {node.note && <div className="d-io"><i /><em className="d-stepnote">{node.note}</em></div>}
    {node.repeats && <Draw node={node.repeats} />}
  </div>
)

const ResearchNeed = ({ node }) => {
  const { open, setOpen } = useContext(DefsContext)
  const uid = useId()
  const isOpen = open?.id === uid
  const cls = `d-pill${node.required ? ' p-required' : ''}`
  const body = (
    <>
      <b>{node.spec.name}</b>
      <i>{node.required ? 'required' : 'optional'}</i>
    </>
  )
  // A research kind with no steps of its own cannot be run. That is an error, and it is
  // drawn as one — the same mark the page uses everywhere else for something it cannot
  // make sense of.
  if (!node.hasProcess) {
    return (
      <span className={cls} id={`res-${node.spec.id}`} title={node.spec.description}>
        {body}
        <em className="d-pill-none"><b>!</b>no steps</em>
      </span>
    )
  }
  return (
    <>
      <button type="button" className={`${cls}${isOpen ? ' is-open' : ''}`}
        onClick={() => setOpen(isOpen ? null : { id: uid, name: node.spec.name })}
        title={node.spec.description}>{body}
        <em className="d-pill-arrow">{isOpen ? '\u2304' : '\u203A'}</em></button>
      {isOpen && <Panel name={node.spec.name} />}
    </>
  )
}

const Research = ({ nodes }) => !nodes?.length ? null : (
  <div className="d-band b-research">
    <span className="d-band-lab">Research</span>
    <span className="d-band-body"><Each nodes={nodes} /></span>
  </div>
)

const Questions = ({ nodes }) => !nodes?.length ? null : (
  <div className="d-band b-questions">
    <span className="d-band-lab">Questions</span>
    <span className="d-band-body"><Each nodes={nodes} /></span>
  </div>
)

const Question = ({ node }) => (
  <div className="d-question">
    <span className="d-question-text">{node.text}</span>
    <span className="d-question-by">
      {node.answeredBy.map((r, i) => r.kind === 'undescribed'
        ? <Draw key={i} node={r} /> : <i key={i}>{r.name}</i>)}
    </span>
    {node.unanswerable && <Draw node={node.unanswerable} />}
  </div>
)

// What makes this run again, and what stops it.
const Repeat = ({ node }) => (
  <div className="d-repeat">
    <div className="d-repeat-head">
      <em className="d-repeat-mark">repeats</em>
      <span>until every one of these holds</span>
      {node.atMost && <i className="d-count">at most {node.atMost}</i>}
    </div>
    {node.empty && <Draw node={node.empty} />}
    <div className="d-gate-body"><Each nodes={node.until} /></div>
  </div>
)

const Gate = ({ node }) => (
  <div className={`d-gate g-${node.spec.id}`}>
    <div className="d-gate-head">
      <em className="d-gate-kind">{node.spec.name}</em>
      <span>{node.spec.description}</span>
      <i className="d-count">{node.conditions.length} conditions</i>
    </div>
    {node.empty && <Draw node={node.empty} />}
    <div className="d-gate-body"><Each nodes={node.conditions} /></div>
    <Each nodes={node.unmet} />
  </div>
)

const GateCondition = ({ node }) => (
  <div className={`d-cond cat-${node.category?.id ?? 'unknown'}`}>
    <Icon name={node.spec.icon} title={node.category?.name ?? node.spec.name} />
    <span className="d-cond-kind">
      {node.spec.name}
      {node.dependsOn?.length > 0 && (
        <i className="d-cond-after">after {node.dependsOn.join(', ')}</i>
      )}
    </span>
    <span className="d-cond-body">
      <b>{node.over ?? <Undescribed node={{ why: 'a condition with nothing to be over', value: null }} />}</b>
      <em>{node.spec.question}</em>
    </span>
    {node.by && <span className="d-cond-by"><i>satisfied by</i>{node.by}</span>}
    <Each nodes={node.missing} />
  </div>
)

// ── elements and members ─────────────────────────────────────────────────────────
const Element = ({ node }) => (
  <div className={`d-element f-${node.form?.id ?? 'unknown'}${node.generated ? ' is-generated' : ''}`}>
    <div className="d-element-head">
      {node.form ? <Draw node={node.form} /> : null}
      <i className="d-form-name">{node.form?.name.toLowerCase()}</i>
      <b>{node.name}</b>
      {node.parameters.length > 0 && <span className="d-params">&lt;{node.parameters.join(', ')}&gt;</span>}
      {node.storage && <span className="d-store" title={node.storage.name}>{node.storage.id}</span>}
      {node.precision && <span className="d-prec">{node.precision.id}</span>}
      {node.alteration && <Draw node={node.alteration} />}
      {node.generated && <span className="d-gen">generated</span>}
    </div>
    <div className="d-element-body"><Each nodes={node.members} /></div>
    {node.purpose && <p className="d-purpose">{node.purpose}</p>}
  </div>
)

const Member = ({ node }) => (
  <div className="d-mrow">
    {node.visibility ? <Draw node={node.visibility} /> : null}
    <span className="d-mname">{node.name}</span>
    {node.memberKind
      ? <em className={`d-mkind m-${node.memberKind.id}`} title={node.memberKind.name}>{node.memberKind.mark}</em>
      : null}
    <span className="d-msig">
      {node.type ? <Draw node={node.type} /> : (node.note ? <em className="d-mnote">{node.note}</em> : null)}
    </span>
    {node.alteration && <Draw node={node.alteration} />}
  </div>
)

const Relation = ({ node }) => (
  <span className="d-rel">
    <Draw node={node.from} />
    <em className="d-rel-kind">{node.spec.name}</em>
    <Draw node={node.to} />
    {node.toArguments.length > 0 && <span className="d-rel-args"><Each nodes={node.toArguments} /></span>}
    {node.note && <span className="d-rel-note">{node.note}</span>}
  </span>
)

const RelationEnd = ({ node }) => (
  <span className="d-rel-end">
    <b>{node.element}</b>
    {node.member && <i>.{node.member}</i>}
  </span>
)

// ── constraints ──────────────────────────────────────────────────────────────────
const Constraint = ({ node }) => (
  <div className={`d-constraint c-${node.spec.id}`}>
    <div className="d-constraint-head">
      <b>{node.spec.name}</b>
      <code>{node.id}</code>
    </div>
    <div className="d-constraint-body">
      {node.fields.map((f) => (
        <span className={`d-field${f.absence === 'error' ? '' : ' is-optional'}`} key={f.id}>
          <i>{f.id}</i><Value v={f.value} /></span>
      ))}
    </div>
    {node.source && <p className="d-source"><i>source</i>{node.source}</p>}
    <Each nodes={node.missing} />
  </div>
)

// ── payloads ─────────────────────────────────────────────────────────────────────
const Payload = ({ node }) => <Draw node={node.body} />

// Held in the setup, drawn here. A reference, not a link: what you see is what the setup
// holds, so a contract cannot claim something the setup does not have.
// This run's architecture, which is a document of its own.
const PayloadArchitecture = ({ node }) => (
  <div className="d-from">
    <div className="d-from-head">
      <span>this run's own design</span>
      <a href="architecture.html"><code>architecture.html</code></a>
      <i className="d-count">one per run</i>
    </div>
  </div>
)

const PayloadFrom = ({ node }) => (
  <div className="d-from">
    <div className="d-from-head">
      <span>stood up once in the setup</span>
      <code>{node.from}</code>
      <i className="d-count">referenced, not copied</i>
    </div>
    <div className="d-from-body"><Draw node={node.body} /></div>
  </div>
)

const Sentence = ({ node }) => (
  <div className="d-sentence">
    <div className="d-sentence-text">{node.text}</div>
    <Draw node={node.needs} />
  </div>
)

const Needs = ({ node }) => (
  <div className="d-needs">
    <div className="d-needs-head"><span>words only — proposed structure</span><b>{node.name}</b></div>
    <p className="d-needs-why">{node.why}</p>
    <span className="d-needs-q">{node.question}</span>
    <code className="d-needs-shape">{(node.shape ?? []).join(' · ')}</code>
    <div className="d-needs-ex"><i>would read</i><em>{node.example}</em></div>
    <Each nodes={node.missing} />
  </div>
)

const CommandSet = ({ node }) => (
  <div className="d-cmd">
    <div className="d-cmd-head"><code>{node.prefix}</code><span>over</span><Draw node={node.over} /></div>
    <div className="d-cmd-body"><Each nodes={node.verbs} /></div>
  </div>
)

const Verb = ({ node }) => (
  <div className="d-verb">
    <code>{node.id}</code>
    <span><i>takes</i>{node.takes}</span>
    <span><i>returns</i>{node.returns}</span>
  </div>
)

// Built code that refuses something. The judge is handed its results before judging, so
// what matters here is what it forbids, where it lives, and what proves it fires.
const Rule = ({ node }) => (
  <div className="d-rule">
    <div className="d-rule-head">
      <em className="d-rule-mark">rule</em>
      <code>{node.id}</code>
      {node.mechanises && <i className="d-rule-was">replaces the constraint {node.mechanises}</i>}
    </div>
    <p className="d-rule-forbids">{node.forbids}</p>
    <div className="d-rule-rows">
      <span className="d-field"><i>built as</i><b>{node.built_as}</b></span>
      <span className="d-field"><i>proven by</i><b>{node.proven_by}</b></span>
    </div>
  </div>
)

// What a behaviour-level test must prove. Agreed with the human.
const Spec = ({ node }) => (
  <div className="d-spec">
    <div className="d-spec-head"><em className="d-spec-mark">spec</em><code>{node.id}</code>
      {node.over && <Draw node={node.over} />}</div>
    <p className="d-spec-proves">{node.proves}</p>
    {node.when && <span className="d-field"><i>when</i><b>{node.when}</b></span>}
  </div>
)

// What one test asserts about one piece of the architecture.
const UnitTest = ({ node }) => (
  <div className="d-utest">
    <div className="d-spec-head"><em className="d-utest-mark">unit test</em><code>{node.id}</code>
      {node.over && <Draw node={node.over} />}</div>
    <p className="d-spec-proves">{node.proves}</p>
    {node.from_spec && <span className="d-field"><i>from spec</i><b>{node.from_spec}</b></span>}
  </div>
)

// ── decisions ────────────────────────────────────────────────────────────────────
const Decision = ({ node }) => (
  <div className={`d-decision s-${String(node.state).toLowerCase()}`} id={`dec-${node.slug}`}>
    <div className="d-decision-head">
      <span className="d-state">{node.state}</span>
      <span className="d-ptype">{node.payload?.type ?? '—'}</span>
      <code className="d-slug">{node.slug}</code>
    </div>
    {node.badState && <Draw node={node.badState} />}
    <div className="d-decision-body"><Draw node={node.payload} /></div>
  </div>
)

const DecisionGroup = ({ node }) => (
  <div className={`d-group${node.decisions.length ? '' : ' is-empty'}`}>
    <div className="d-group-head">
      <b>{node.name}</b>
      <span>{node.question}</span>
      <i className="d-count">{node.decisions.length || 'no'} decisions</i>
    </div>
    {node.decisions.length
      ? <Each nodes={node.decisions} />
      : <p className="d-group-empty">This band is declared and holds nothing. Either a decision
        belongs here and has not been written, or the band should not exist.</p>}
  </div>
)

// A list of declared things, each as a card. Most of the language document is this one
// device: the forms, the actors, the research kinds, the lenses, the gate kinds, the
// constraint kinds, the dimensions, the views, the tones. They used to be nine bespoke
// grids with nine sets of class names.
const VocabGrid = ({ node }) => (
  <div className="d-vgrid">{node.items.map((it, i) => <Draw key={i} node={it} />)}</div>
)

const VocabCard = ({ node }) => (
  <div className={`d-vcard${node.tone ? ` v-${node.tone}` : ''}`}>
    <div className="d-vcard-head">
      {node.mark && <em className="d-vmark">{node.mark}</em>}
      <b>{node.name}</b>
      {node.tags?.map((t, i) => <i className="d-vtag" key={i}>{t}</i>)}
    </div>
    {node.description && <span className="d-vdesc">{node.description}</span>}
    {node.example && <code className="d-vex">{node.example}</code>}
    {node.rows?.length > 0 && <div className="d-vrows"><Each nodes={node.rows} /></div>}
  </div>
)

// A pair of a key and a value, used wherever a declared thing lists its parts.
const Pair = ({ node }) => (
  <div className="d-pair"><code>{node.key}</code><span>{node.value}</span></div>
)

// Containment drawn as containment: the levels of the language, nested.
const Nest = ({ node }) => (
  <div className="d-nest">
    {node.items.reduceRight((inner, it, i) => (
      <div className={`d-nbox n${i}`} key={i}>
        <div className="d-nhead"><b>{it.name}</b><code>{it.example}</code>
          {it.kinds && <i>its kinds are the {it.kinds}</i>}</div>
        <span className="d-ndesc">{it.description}</span>
        {inner}
      </div>
    ), null)}
  </div>
)

// Visibility drawn as how far out a thing can be seen.
const Rings = ({ node }) => (
  <div className="d-rings">
    {node.items.reduceRight((inner, v, i) => (
      <div className={`d-ring r-${v.id}`} key={i}>
        <span className="d-ring-lab">{v.id}</span>
        <code>{v.example}</code>
        {inner}
      </div>
    ), null)}
  </div>
)

// An exact record is a byte layout: where each field starts and how wide it is.
const Bytes = ({ node }) => (
  <div className="d-bytes">
    <div className="d-bytes-head"><b>{node.name}</b>
      {node.placeholder && <em className="d-vtag is-warn">placeholder</em>}
      <i className="d-count">{node.size} bytes</i></div>
    {node.rows.map((r, i) => (
      <div className={`d-byterow${r.pad ? ' is-pad' : ''}`} key={i}>
        <code className="d-byteat">{r.at}</code>
        <code className="d-bytew">{r.width}</code>
        <b>{r.name || 'padding'}</b>
        <span>{r.type}</span>
      </div>
    ))}
  </div>
)

// Who may do what, as a grid, because the empty cell is the point.
const Permissions = ({ node }) => (
  <div className="d-perm" style={{ gridTemplateColumns: `140px repeat(${node.verbs.length}, 1fr)` }}>
    <span />
    {node.verbs.map((v) => <span className="d-perm-verb" key={v.id}>{v.id}</span>)}
    {node.actors.map((a) => (
      <React.Fragment key={a.id}>
        <span className="d-perm-actor" style={toneOf(a)?.style}>{a.name}</span>
        {node.verbs.map((v) => {
          const c = node.cells[`${a.id}|${v.id}`]
          if (!c) return <span className="d-perm-cell is-none" key={v.id}>·</span>
          return (
            <span className={`d-perm-cell ${c.allowed ? 'is-yes' : 'is-no'}`} key={v.id}>
              <b>{c.allowed ? 'MAY' : 'MAY NOT'}</b>
              <em>{c.object}</em>
              {c.only_when && <i>only when {c.only_when}</i>}
            </span>
          )
        })}
      </React.Fragment>
    ))}
  </div>
)

// A worked example, shown as the figure it is rather than described in words.
const Figure = ({ node }) => (
  <div className="d-figure">
    {node.caption && <span className="d-figcap">{node.caption}</span>}
    <div className="d-figbody"><Each nodes={node.body} /></div>
  </div>
)

// ── page furniture ───────────────────────────────────────────────────────────────
// ── the metalanguage ─────────────────────────────────────────────────────────────
// Nothing here narrates, and nothing here chooses a colour. A tone carries its own ink and
// tint, so a mark is drawn with what the file says and never with a class keyed off an id.
//
// A device is an instance of `device`, so it draws as one. The verb on a part row is the
// glyph and the word the declared `holding` carries. The column headings are the parts of
// `part`. Every word on this page but three is a word in the file.
const look = (x) => (x?.look ? { '--t-ink': x.look.ink, '--t-tint': x.look.tint } : undefined)

const kindOf = (node) =>
  node.union ? { part: 'one_of', detail: `${node.alternatives.length}` }
    : node.leaf ? { part: 'value', detail: node.value ?? '' }
      : node.closed ? { part: 'instances', detail: `${node.instances.length}` }
        : node.parts.length ? { part: 'parts', detail: `${node.parts.length}` }
          : null

const MetaBadge = ({ node }) => {
  const k = kindOf(node)
  if (!k) return null
  return <i className={`d-mbadge b-${k.part}`}><code>{k.part}</code>{k.detail && <b>{k.detail}</b>}</i>
}

const MetaIndex = ({ node }) => (
  <nav className="d-mindex">
    {node.devices.map((d) => (
      <a className="d-mindex-item" href={`#mdev-${d.id}`} key={d.id}>
        <code>{d.id}</code><MetaBadge node={d} />
      </a>
    ))}
  </nav>
)

// The key. Every closed vocabulary whose instances carry a glyph teaches how to read a
// row, so it is shown before the rows rather than only where it is declared.
const MetaKey = ({ node }) => (
  <div className="d-mkey">
    <div className="d-mkey-head"><code>{node.id}</code><span>{node.description}</span></div>
    <div className="d-mkey-items">
      {node.entries.map((e) => (
        <div className="d-mkey-item" key={e.id} style={look(e)}>
          <b className="d-mglyph">{e.glyph}</b>
          <span className="d-mkey-name">{e.reads ?? e.name ?? e.id}</span>
          <i className="d-mkey-desc">{e.description}</i>
        </div>
      ))}
    </div>
  </div>
)

const Held = ({ name, target }) =>
  target ? <a className="d-mlink" href={`#mdev-${target.id}`}>{name}</a> : <span>{name ?? '—'}</span>

// Whether a slot must be written is read FIRST, at the left edge, as the mark its
// part_absence declares. It was the faintest thing on the page and is the most important.
const MetaPart = ({ node }) => (
  <div className="d-mpart">
    <b className="d-mglyph d-mabs" style={look(node.absence)} title={node.absence?.description}>
      {node.absence?.glyph ?? '?'}
    </b>
    <code className="d-mpart-id">{node.id}</code>
    <span className="d-mpart-rel">
      <b className="d-mglyph d-mverb" style={look(node.by)} title={node.by?.description}>
        {node.by?.glyph ?? '?'}
      </b>
      <Held name={node.holds.name} target={node.holds.target} />
      {node.many && <i className="d-mflag">many</i>}
      {node.scope && (
        <i className="d-mscope" style={look(node.scope)} title={node.scope.description}>
          {node.scope.glyph} {node.scope.name}
        </i>
      )}
    </span>
    <span className="d-mpart-desc">{node.description}</span>
  </div>
)

// The headings are the parts of `part`. The third column carries three of them.
const MetaPartHead = () => (
  <div className="d-mpart is-head">
    <span>absence</span><code>id</code>
    <span>by &middot; holds &middot; within</span><span>description</span>
  </div>
)

const MetaInstance = ({ node }) => (
  <div className="d-minst">
    {node.name && <b>{node.name}</b>}
    {node.rows.length > 0 && <div className="d-mrows"><Each nodes={node.rows} /></div>}
  </div>
)

const MetaAlternatives = ({ node }) => (
  <div className="d-malts">
    {node.alternatives.map((a, i) => (
      <div className={`d-malt${a.known ? '' : ' is-missing'}`} key={a.id}>
        {i > 0 && <span className="d-malt-or">or</span>}
        <a className="d-mlink" href={`#mdev-${a.id}`}>{a.id}</a>
        {a.requires.map((r) => <i className="d-mflag" key={r}>{r}</i>)}
      </div>
    ))}
  </div>
)

const MetaDevice = ({ node }) => (
  <section className={`d-mdev k-${kindOf(node)?.part ?? 'bare'}`} id={`mdev-${node.id}`}>
    <div className="d-mdev-head"><b>{node.id}</b><MetaBadge node={node} /></div>
    {node.description && <p className="d-mdev-desc">{node.description}</p>}
    {node.rows.length > 0 && <div className="d-mdev-rows"><Each nodes={node.rows} /></div>}
    {node.union && <MetaAlternatives node={node} />}
    {node.parts.length > 0 && (
      <div className="d-mparts"><MetaPartHead /><Each nodes={node.parts} /></div>
    )}
    {node.instances.length > 0 && <div className="d-minsts"><Each nodes={node.instances} /></div>}
  </section>
)

const Section = ({ node }) => (
  <section className="d-section">
    <h2>{node.title}</h2>
    {node.question && <p className="d-lede">{node.question}</p>}
    <Each nodes={node.body} />
  </section>
)

// An element already built. Used only where a component wraps another's output.
const Raw = ({ node }) => node.body

const Field = ({ node }) => (
  <div className={`d-reqrow${node.value ? '' : ' is-missing'}`} id={`field-${node.name}`}>
    <span className="d-reqname">{node.name}</span>
    {node.value
      ? <span className="d-reqval">{node.value}</span>
      : <span className="d-reqval is-none">not written &mdash; this is required before signing</span>}
  </div>
)

// Signability belongs to a contract. A specification has nothing to sign, so it reports
// only what it could not describe, and says nothing at all when there is nothing to say.
const Verdict = ({ node }) => {
  const signable = Array.isArray(node.blocks)
  const problems = node.problems ?? []
  if (!signable && !node.undescribed && !problems.length) return null
  const blocked = signable && node.blocks.length > 0
  const groups = []
  for (const p of problems) {
    const g = groups.find((x) => x.name === p.group)
    if (g) g.items.push(p)
    else groups.push({ name: p.group, tone: p.tone ?? 'bad', items: [p] })
  }
  return (
    <div className={`d-verdict${blocked ? ' is-blocked' : node.undescribed ? ' is-warn' : ' is-clear'}`}>
      {signable && <b>{blocked ? 'Not signable' : 'Nothing blocks signing.'}</b>}
      {node.undescribed > 0 && groups.length === 0 && (
        <b>{node.undescribed} thing{node.undescribed === 1 ? '' : 's'} on this page the language does not describe.</b>
      )}
      {groups.length > 0 && (
        <div className="d-problems">
          {groups.map((g) => (
            <div className={`d-pgroup t-${g.tone}`} key={g.name}>
              <div className="d-pgroup-head"><span>{g.name}</span><i>{g.items.length}</i></div>
              <div className="d-pitems">
                {g.items.map((p, i) => (p.anchor
                  ? <a className={`d-pitem t-${p.tone ?? 'bad'}`} href={`#${p.anchor}`} key={i}>{p.label}</a>
                  : <span className={`d-pitem is-flat t-${p.tone ?? 'bad'}`} key={i}>{p.label}</span>))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

const Document = ({ node }) => (
  <>
    <header className="d-top">
      <p className="d-eyebrow">{node.eyebrow}</p>
      <h1>{node.title}</h1>
      {node.sub && <p className="d-sub">{node.sub}</p>}
    </header>
    <Each nodes={node.body} />
    <footer className="d-foot">{node.footer}</footer>
  </>
)

// THE REGISTRY. A kind with no entry here cannot be drawn, and the dispatcher says so.
export const DEVICES = {
  undescribed: Undescribed,
  undesigned: Undesigned,
  notStated: NotStated,
  metaDevice: MetaDevice,
  metaIndex: MetaIndex,
  metaKey: MetaKey,
  metaPart: MetaPart,
  metaInstance: MetaInstance,
  reference: Reference,
  process: Process,
  step: Step,
  researchNeed: ResearchNeed,
  question: Question,
  gate: Gate,
  repeat: Repeat,
  gateCondition: GateCondition,
  element: Element,
  member: Member,
  relation: Relation,
  relationEnd: RelationEnd,
  constraint: Constraint,
  rule: Rule,
  spec: Spec,
  unitTest: UnitTest,
  payload: Payload,
  payloadFrom: PayloadFrom,
  payloadArchitecture: PayloadArchitecture,
  sentence: Sentence,
  needs: Needs,
  commandSet: CommandSet,
  verb: Verb,
  decision: Decision,
  decisionGroup: DecisionGroup,
  section: Section,
  field: Field,
  vocabGrid: VocabGrid,
  vocabCard: VocabCard,
  pair: Pair,
  nest: Nest,
  rings: Rings,
  bytes: Bytes,
  permissions: Permissions,
  figure: Figure,
  raw: Raw,
  verdict: Verdict,
  document: Document,
  // The language's own vocabularies draw as the marks they declare.
  form: KindChip,
  visibility: Reach,
  alteration: Alteration,
}
