// Renders workflow-model.yaml to workflow-model.html.
// Throwaway design-review scaffolding. The YAML is the source; never edit the HTML.
// Run: npm run render
import { readFileSync, writeFileSync } from 'node:fs'
import yaml from 'js-yaml'

const SRC = 'workflow-model.yaml'
const OUT = 'workflow-model.html'
const model = yaml.load(readFileSync(SRC, 'utf8'))

const esc = (v) => String(v ?? '')
  .replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;')
const txt = (v) => esc(String(v ?? '').trim())
const STATES = ['required', 'required-unless-excepted', 'when-needed', 'absent', 'not-needed', 'undefined', 'always']
const LVLS = model.levels.map((l) => l.id)

// one pill per level; CSS reveals the active level's pill
function pills(node, kind = '') {
  return LVLS.map((l) => {
    const v = node[l] ?? 'undefined'
    if (!STATES.includes(v)) throw new Error(`${SRC}: unknown state "${v}" for ${l} on ${node.id ?? node.document}`)
    return `<span class="st s-${v} for-${l}${kind ? ' ' + kind : ''}">${esc(v.replace('-', ' '))}</span>`
  }).join('')
}
const lvlAttrs = (node) => LVLS.map((l) => `data-${l}="${esc(node[l] ?? 'undefined')}"`).join(' ')

// Actor icons. Stroke-based, currentColor, so one definition scales and recolours.
const ICON = {
  main: '<rect x="2.6" y="3.4" width="18.8" height="12.6" rx="3.2"/>' +
        '<path d="M7.6 16v4.4L13 16"/><path d="M9.4 7.9 12.1 10.6 9.4 13.3"/>',
  subagent: '<rect x="7.4" y="6.6" width="14" height="10.4" rx="2.8"/>' +
            '<path d="M11 17v3.6l4.4-3.6"/>' +
            '<path d="M2.6 3.2v4.1c0 2.2 1.8 4 4 4h1.1" stroke-dasharray="2.6 2"/>' +
            '<path d="M6.1 9.1 8.2 11.3 6.1 13.4"/>',
  workflow: '<rect x="8.4" y="2.4" width="7.2" height="4.8" rx="1.5"/>' +
            '<rect x="1.8" y="16.8" width="7.2" height="4.8" rx="1.5"/>' +
            '<rect x="15" y="16.8" width="7.2" height="4.8" rx="1.5"/>' +
            '<path d="M12 7.2v4.4M5.4 16.8v-2.6h13.2v2.6M5.4 14.2h13.2"/>',
  guard: '<path d="M12 2.4 20 5.5v6.1c0 4.6-3.3 7.9-8 10-4.7-2.1-8-5.4-8-10V5.5z"/>' +
         '<path d="M8.7 11.9 11.2 14.4 15.6 9.9"/>',
}
Object.assign(ICON, {
  // origin
  'origin:guard': ICON.guard,
  'origin:external': '<path d="M3.2 12h11.6"/><path d="M10.8 8.2 14.8 12l-4 3.8"/>' +
    '<path d="M18.4 3.6h2.2v16.8h-2.2" stroke-dasharray="2.4 2.2"/>',
  // form
  'form:yaml': '<path d="M5.4 2.8h8.2l5 5v13.4H5.4z"/><path d="M13.6 2.8v5h5"/>' +
    '<path d="M8.2 12.4h7.6M8.2 15.6h7.6M8.2 18h4.6"/>',
  'form:github': '<circle cx="7" cy="5.4" r="2.3"/><circle cx="7" cy="18.6" r="2.3"/>' +
    '<circle cx="17" cy="12" r="2.3"/><path d="M7 7.7v8.6"/><path d="M7 12h7.7"/>',
  'form:conversation': '<path d="M3.4 5.2h17.2v10.2H9.8l-4.6 3.6v-3.6H3.4z"/>' +
    '<path d="M8 10.3h8"/>',
  // instruction kind
  'kind:skill': '<path d="M12 2.6 14 9l6.4 2-6.4 2-2 6.4-2-6.4L3.6 11 10 9z"/>',
  // elements
  trigger: '<path d="M13.4 2.4 5.6 13.6h5.2l-1.2 8 7.8-11.2h-5.2z"/>',
  gate: '<path d="M4 20.2 6 14l9.2-9.2a2.3 2.3 0 0 1 3.3 3.3L9.2 17.4z"/>' +
        '<path d="M14.2 5.6 17.6 9"/><path d="M4 20.2 9.2 17.4"/>',
  undefined: '<circle cx="12" cy="12" r="9"/><path d="M12 7.4v6.2"/><path d="M12 16.8h.01" stroke-width="2.6"/>',
})

const icon = (id, px) => {
  const body = ICON[id]
  if (!body) throw new Error(`${SRC}: no icon for "${id}"`)
  return `<svg class="ic ic-${esc(id)}" width="${px}" height="${px}" viewBox="0 0 24 24" ` +
    `fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" ` +
    `aria-hidden="true">${body}</svg>`
}

const docById = Object.fromEntries(model.documents.map((d) => [d.id, d]))
const opById = Object.fromEntries((model.operations ?? []).map((o) => [o.id, o]))
function opChip(id, big) {
  if (!id) return ''
  const o = opById[id]
  if (!o) throw new Error(`${SRC}: step via references unknown operation "${id}"`)
  return `<span class="op sf-${esc(o.surface)}${big ? ' big' : ''}" title="${txt(o.note)}">
    <span class="opsf">${o.surface === 'cli+mcp' ? 'cli &middot; mcp' : esc(o.surface)}</span>
    <code>${esc(o.name)}</code>
    <span class="stat s-${esc(o.status)}">${esc(o.status)}</span></span>`
}
const skillById = Object.fromEntries((model.skills ?? []).map((i) => [i.id, i]))
// every short vocabulary renders as a mark; an unknown value fails the render
const MARKS = {
  origin: { guard: ['\u2b22', 'written by Guard'], external: ['\u21a7', 'arrives from outside'] },
  form:   { yaml: ['\u25a4', 'YAML file'], github: ['\u2317', 'GitHub record'], conversation: ['\u275d', 'conversation only'] },
  actor:  { main: ['\u25a3', 'main agent'], subagent: ['\u2b21', 'subagent'],
            workflow: ['\u229e', 'workflow script'], guard: ['\u2b22', 'Guard'] },
}
function mark(group, value) {
  const hit = MARKS[group]?.[value]
  if (!hit) throw new Error(`${SRC}: unknown ${group} "${value}" — expected ${Object.keys(MARKS[group] ?? {}).join(', ')}`)
  const key = `${group}:${value}`
  const body = ICON[key] ? icon(key, 17) : hit[0]
  return `<span class="mk m-${esc(group)} v-${esc(value)}" title="${esc(hit[1])}">${body}</span>`
}
const skillMark = () => `<span class="mk m-kind v-skill">${icon('kind:skill', 17)}</span>`
const docMarks = (d) => mark('origin', d.origin) + mark('form', d.form)
const ACTOR_LABEL = { main: 'main agent', subagent: 'subagent', workflow: 'workflow', guard: 'Guard' }
// which fields of a document a step writes, from its writes list
function writesIn(st, docId) {
  return (st.writes ?? []).map((w) => String(w).trim())
    .map((w) => /^([a-z][a-z0-9-]*)\.([a-z][a-z0-9-]*)$/.exec(w))
    .filter((m) => m && m[1] === docId).map((m) => m[2])
}
function opArgs(st, o, gateId) {
  if (o.id === 'gate-sign' || o.id === 'gate-request') return gateId ? [gateId] : []
  const fields = writesIn(st, o.target)
  if (fields.length) return fields
  // whole-document write, e.g. a verdict
  return docById[o.target] && (st.writes ?? []).some((w) => String(w).trim() === o.target) ? [] : []
}

function callChain(st, px, gateId) {
  const hops = []
  if (st.actor === 'subagent') hops.push(st.spawned_by === 'workflow' ? 'workflow' : 'main')
  hops.push(st.actor)
  const o = st.via ? opById[st.via] : null
  if (!o) throw new Error(`${SRC}: step ${st.id} has no via`)
  const guardCall = o.surface === 'cli+mcp'
  const parts = hops.map((h) => `<span class="a-${esc(h)}" title="${esc(ACTOR_LABEL[h])}">${icon(h, px)}</span>`)
  if (guardCall) parts.push(`<span class="a-guard" title="Guard">${icon('guard', px)}</span>`)
  const args = guardCall ? opArgs(st, o, gateId) : []
  const tail = guardCall
    ? `<span class="cc-call"><code class="cc-op">${esc(o.name)}</code>` +
      (args.length ? `<span class="cc-args">${args.map((a) => `<code class="cc-arg">${esc(a)}</code>`).join('')}</span>` : '') +
      `</span>`
    : `<span class="cc-none">${o.surface === 'hook' ? 'guarded by the tool hook' : 'writes nothing'}</span>`
  return `<span class="cc">${parts.join('<i class="cca">\u2192</i>')}${tail}</span>`
}

const presenceChip = (on) => on ? '<span class="rl r-presence">presence required</span>' : ''

// open items for ONE phase at ONE level, attributed to where they sit in its flow
function phaseOpen(ph, l) {
  const out = []
  const stub = (ph.status ?? 'defined') === 'stub'
  if (stub) { if (ph[l] !== 'absent') out.push(['phase', 'the whole phase is undefined']); return out }
  if (ph[l] === 'undefined') out.push(['phase', 'the phase has no state at this level'])
  for (const [k, label] of [['inputs', 'input'], ['outputs', 'output'], ['triggers', 'trigger']])
    for (const x of ph[k] ?? []) if (x[l] === 'undefined') out.push([k, `${label}: ${x.id ?? x.document}`])
  for (const st of ph.steps ?? []) {
    if (st[l] === 'undefined') out.push(['steps', `step ${st.id}`])
    for (const id of st.loads ?? [])
      if ((skillById[id]?.status) === 'undefined') out.push(['skill', `${st.id} has no skill chosen`])
  }
  if (ph.gate && ph.gate[l] === 'undefined') out.push(['gate', `gate ${ph.gate.name}`])
  // a field is open where it is written
  const written = new Set((ph.steps ?? []).flatMap((st) => (st.writes ?? [])
    .map((w) => String(w).trim()).filter((w) => /^[a-z][a-z0-9-]*\.[a-z][a-z0-9-]*$/.test(w))))
  for (const d of model.documents)
    for (const sec of d.sections ?? [])
      if (sec[l] === 'undefined' && written.has(`${d.id}.${sec.id}`))
        out.push(['fields', `${d.id}.${sec.id} has no state`])
  return out
}

const openStrip = (ph) => `${LVLS.map((l) => {
  const o = phaseOpen(ph, l)
  if (!o.length) return ''
  return `<div class="ohere for-${esc(l)}">${icon('undefined', 20)}
    <span class="ohlab">${o.length} open here</span>
    <span class="ohset">${o.map(([k, t]) => `<span class="ohi k-${esc(k)}">${esc(t)}</span>`).join('')}</span></div>`
}).join('')}`

const openCount = (ph) => `${LVLS.map((l) => {
  const n = phaseOpen(ph, l).length
  return n ? `<span class="ocnt for-${esc(l)}">${icon('undefined', 15)}${n}</span>` : ''
}).join('')}`

function openWork() {
  const rows = []
  const push = (l, where, what, kind) => rows.push({ l, where, what, kind })
  for (const ph of model.phases) {
    const stub = (ph.status ?? 'defined') === 'stub'
    for (const l of LVLS) {
      if (stub && ph[l] !== 'absent') push(l, ph.name, 'the whole phase is undefined', 'phase')
      if (!stub && ph[l] === 'undefined') push(l, ph.name, 'the phase itself has no level state', 'phase')
      if (stub) continue
      for (const k of ['inputs', 'outputs', 'steps', 'triggers'])
        for (const x of ph[k] ?? [])
          if (x[l] === 'undefined') push(l, ph.name, `${k.slice(0, -1)} ${x.id ?? x.document}`, k)
      if (ph.gate && ph.gate[l] === 'undefined') push(l, ph.name, `gate ${ph.gate.name}`, 'gate')
      for (const st of ph.steps ?? [])
        for (const id of st.loads ?? [])
          if ((skillById[id]?.status) === 'undefined') push(l, ph.name, `${st.id} loads an unchosen instruction source`, 'instructions')
    }
  }
  for (const d of model.documents)
    for (const sec of d.sections ?? [])
      for (const l of LVLS) if (sec[l] === 'undefined') push(l, d.name, `field ${sec.id}`, 'field')
  const byLevel = Object.fromEntries(LVLS.map((l) => [l, rows.filter((r) => r.l === l)]))
  return `<div class="ow">${LVLS.map((l) => {
    const rs = byLevel[l]
    return `<div class="owl for-${esc(l)}">${icon('undefined', 20)}
      <b>${rs.length}</b> <span>${rs.length === 1 ? 'thing' : 'things'} still undefined at ${esc(l.toUpperCase())},
      marked in place as you walk down the flow.</span></div>`
  }).join('')}</div>`
}

const topology = () => `<div class="topo">
  <div class="tnodes">${model.actors.map((a) => `<div class="tn n-${esc(a.id)}">
    ${icon(a.id, 26)}<div><b>${txt(a.name)}</b><span class="tnn">${txt(a.note)}</span></div></div>`).join('')}</div>
  <div class="tcalls">${model.calls.map((c) => `<div class="tc ${c.allowed ? 'ok' : 'no'}">
    <span class="a-${esc(c.from)}">${icon(c.from, 22)}</span>
    <span class="tarr">${c.allowed ? '\u2192' : '\u2715'}</span>
    <span class="a-${esc(c.to)}">${icon(c.to, 22)}</span>
    <span class="thow">${txt(c.how)}</span></div>`).join('')}</div>
</div>`
const stepLoads = (ids) => (ids ?? []).map((id) => {
  const i = skillById[id]
  if (!i) throw new Error(`${SRC}: step loads unknown skill "${id}"`)
  return `<span class="ld">${skillMark()}<code>${esc(i.name)}</code>` +
    `<span class="stat s-${esc(i.status)}">${esc(i.status)}</span></span>`
}).join('')
const legend = (group) => Object.entries(MARKS[group])
  .map(([v, [g, label]]) => `<span>${mark(group, v)} ${esc(label)}</span>`).join('')
const list = (items) => (items ?? []).map((i) => `<li>${txt(i)}</li>`).join('')

function ioRow(io) {
  const d = docById[io.document]
  if (!d) throw new Error(`${SRC}: input/output references unknown document "${io.document}"`)
  return `<tr ${lvlAttrs(io)}>
    <td><b>${txt(d.name)}</b><span class="meta">${docMarks(d)}</span>
      ${io.oneof ? `<span class="oneof">one of: ${esc(io.oneof)}</span>` : ''}</td>
    <td>${txt(io.note ?? d.purpose)}</td>
    <td class="p">${pills(io)}</td></tr>`
}

function fileCard(io, dir, steps) {
  const d = docById[io.document]
  if (!d) throw new Error(`${SRC}: references unknown document "${io.document}"`)
  return `<div class="fc d-${dir}" ${lvlAttrs(io)} title="${txt(io.note ?? d.purpose)}">
    <span class="fcmk">${mark('origin', d.origin)}${mark('form', d.form)}</span>
    <b>${txt(d.name)}</b>${dir === 'both' ? '<span class="fcboth">in · updated</span>' : ''}
    ${(() => {
      if (dir === 'in') return ''
      const f = [...new Set((steps ?? []).flatMap((st) => writesIn(st, io.document)))]
      return f.length ? `<span class="fcf">${f.map((x) => `<code>${esc(x)}</code>`).join('')}</span>` : ''
    })()}
    <span class="fcst">${pills(io)}</span></div>`
}

const refChip = (v) => {
  const t = String(v ?? '').trim()
  const m = /^([a-z][a-z0-9-]*)\.([a-z][a-z0-9-]*)$/.exec(t)
  if (m && docById[m[1]]) return `<span class="rc is-ref">${mark('form', docById[m[1]].form)}<code>${esc(t)}</code></span>`
  return `<span class="rc">${esc(t)}</span>`
}

function chainStep(st, i, last, gateId) {
  const lead = st.actor ?? 'main'
  const framed = st.spawned_by === 'workflow'
  return `<details class="cs lead-${lead}${framed ? ' framed' : ''}" ${lvlAttrs(st)}>
    <summary>
      <span class="csnum">${i + 1}</span>
      <span class="cstxt"><b>${txt(st.name)}</b>
        ${callChain(st, 20, gateId)}
        ${(st.loads ?? []).length || st.presence ? `<span class="csmeta">${presenceChip(st.presence)}${stepLoads(st.loads)}</span>` : ''}</span>
      ${pills(st)}
    </summary>
    <div class="csbody">
      <div class="mini">
        <div class="mrow"><h6>reads</h6><div class="mset">${(st.reads ?? []).map(refChip).join('') || '<span class="rc rc-none">nothing</span>'}</div></div>
        <div class="cdown">\u25be</div>
        <div class="mact">
          <b>${txt(st.name)}</b>
          <div class="mchain">${callChain(st, 28, gateId)}</div>
          <span class="mwho">${esc(ACTOR_LABEL[lead])}${framed ? ' \u00b7 launched by the workflow script' : ''}${st.presence ? ' \u00b7 presence required' : ''}</span>
          ${(st.loads ?? []).length ? `<div class="mload">${stepLoads(st.loads)}</div>` : ''}
          ${st.via && opById[st.via].surface === 'cli+mcp' ? `<div class="mvia"><h6>operation</h6>${opChip(st.via, true)}</div>` : ''}</div>
        <div class="cdown">\u25be</div>
        <div class="mrow"><h6>writes</h6><div class="mset">${(st.writes ?? []).map(refChip).join('') || '<span class="rc rc-none">nothing</span>'}</div></div>
      </div>
      ${st.note ? `<p class="snote">${txt(st.note)}</p>` : ''}
    </div></details>${last ? '' : '<div class="cdown">\u25be</div>'}`
}

function gateBand(g) {
  return `<div class="gband" ${lvlAttrs(g)}>
    <span class="gdia">${icon('gate', 21)}</span><b>${txt(g.name)}</b>
    <span class="gsub">${(g.locks ?? []).map((l) => `<code>${esc(String(l).trim())}</code>`).join('')}</span>
    ${pills(g, 'gatepill')}</div>`
}

function trigStrip(ts) {
  return `<div class="tstrip"><span class="tlab">can interrupt</span>
    ${ts.map((t) => `<span class="tchip" ${lvlAttrs(t)} title="${txt(t.decides)}">
      ${icon('trigger', 17)}${txt(t.name)}<code>${txt(t.predicate)}</code></span>`).join('')}</div>`
}

function phaseBlock(p) {
  const status = p.status ?? 'defined'
  if (!['defined', 'stub'].includes(status)) throw new Error(`${SRC}: unknown phase status "${status}" on ${p.id}`)
  if (status === 'stub') return `<div class="phase stub" ${lvlAttrs(p)}>
    <div class="shell">
      <span class="sym">${esc(p.symbol)}</span>
      <span class="ptitle"><b>${txt(p.name)}</b><span class="purpose">${txt(p.purpose)}</span>
        ${p.note ? `<span class="stubnote">${txt(p.note)}</span>` : ''}</span>
      ${openCount(p)}<span class="stat s-undefined">not defined</span>${pills(p)}
    </div></div>`

  const ins = p.inputs ?? [], outs = p.outputs ?? []
  const outIds = new Set(outs.map((o) => o.document))
  const both = ins.filter((i) => outIds.has(i.document))
  const bothIds = new Set(both.map((b) => b.document))
  const onlyIn = ins.filter((i) => !bothIds.has(i.document))
  const onlyOut = outs.filter((o) => !bothIds.has(o.document))
  const steps = p.steps ?? []

  return `<details class="phase" ${lvlAttrs(p)} open>
    <summary>
      <span class="sym">${esc(p.symbol)}</span>
      <span class="ptitle"><b>${txt(p.name)}</b><span class="purpose">${txt(p.purpose)}</span></span>
      ${openCount(p)}${pills(p)}
    </summary>
    <div class="pbody">
      <div class="flow">
        ${onlyIn.length || both.length ? `<div class="frow"><h5>In</h5><div class="fset">
          ${onlyIn.map((i) => fileCard(i, 'in', steps)).join('')}${both.map((b) => fileCard(b, 'both', steps)).join('')}
        </div></div><div class="cdown big">\u25be</div>` : ''}
        <div class="chain">${steps.map((st, i) => chainStep(st, i, i === steps.length - 1, p.gate && p.gate.id)).join('')}</div>
        ${(p.triggers ?? []).length ? trigStrip(p.triggers) : ''}
        ${p.gate ? gateBand(p.gate) : ''}
        ${onlyOut.length ? `<div class="cdown big">\u25be</div><div class="frow"><h5>Out</h5><div class="fset">
          ${onlyOut.map((o) => fileCard(o, 'out', steps)).join('')}</div></div>`
          : '<div class="frow"><h5>Out</h5><p class="meta">No file. Everything shown is already saved.</p></div>'}
      </div>
      ${openStrip(p)}
      ${p.note ? `<p class="pnote">${txt(p.note)}</p>` : ''}
      ${p.close ? `<p class="close"><b>Closes when:</b> ${txt(p.close)}</p>` : ''}
      ${p.gate ? `<details class="deep"><summary>What the gate presents and locks</summary>
        <div class="dbody">
          <div class="gdetail"><h5>Presents</h5><ul>${list(p.gate.presents)}</ul>
            <h5>Locks</h5><ul>${list(p.gate.locks)}</ul>
          ${p.gate.reopen ? `<p class="reopen"><b>To change later:</b> ${txt(p.gate.reopen)}</p>` : ''}</div>
        </div></details>` : ''}
    </div></details>`
}


function docBlock(d) {
  const rows = (d.sections ?? []).map((s) => `<tr ${lvlAttrs(s)}>
      <td><code>${esc(d.id)}.${esc(s.id)}</code>${s.contract_section ? `<span class="csec">contract \u00a7${esc(String(s.contract_section))}</span>` : ''}</td>
      <td class="meta">${txt(s.type)}</td>
      <td>${txt(s.purpose)}</td>
      <td class="p">${pills(s)}</td></tr>`).join('')
  return `<details class="doc"><summary><b>${txt(d.name)}</b>
      <span class="meta">${docMarks(d)}</span></summary>
    <div class="dbody"><p class="purpose">${txt(d.purpose)}</p>
      ${rows ? `<table><thead><tr><th>Field</th><th>Type</th><th>Purpose</th><th>State</th></tr></thead>
        <tbody>${rows}</tbody></table>` : '<p class="meta">No fields defined — external input.</p>'}
    </div></details>`
}

const buttons = model.levels.map((l, i) =>
  `<button type="button" data-lvl="${esc(l.id)}" aria-pressed="${i === 0}">${esc(l.name)} — ${esc(l.title)}<small>${esc(l.note)}</small></button>`).join('')

const html = `<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>Workflow model</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:opsz,wght@8..60,400;8..60,600;8..60,700&display=swap" rel="stylesheet">
<style>
:root{--paper:#fbfaf7;--card:#fff;--ink:#1c2230;--soft:#4b5363;--dim:#8a909c;--rule:#e4e1d9;--band:#f3f1ec;
 --stage:#1f5f5b;--stage-soft:#e6f0ee;--gate:#6b3fa0;--gate-soft:#f1ebf9;--gate-line:#cdb9e6;
 --opt:#9c5c00;--opt-bg:#fce6bd;--undef:#7a7f87;--undef-bg:#f0f0ee;
 --inst:#2f5d8a;--inst-soft:#e8f0f8;--skill:#1d6fb8;--skill-soft:#e3eff9;--wf:#a0397c;
 --serif:"Source Serif 4",Georgia,serif;--sans:"Inter",-apple-system,"Segoe UI",Helvetica,Arial,sans-serif;
 --mono:"JetBrains Mono",ui-monospace,SFMono-Regular,Menlo,Consolas,monospace}
*{box-sizing:border-box}html{background:var(--paper)}
body{margin:0;background:var(--paper);color:var(--ink);font:15px/1.55 var(--sans);-webkit-font-smoothing:antialiased}
.page{max-width:900px;margin:0 auto;padding:52px 28px 90px}
.mast{display:flex;justify-content:space-between;gap:16px;font-size:11.5px;letter-spacing:.14em;
 text-transform:uppercase;color:var(--dim);border-bottom:1px solid var(--rule);padding-bottom:10px}
h1{font:600 38px/1.15 var(--serif);margin:28px 0 6px;letter-spacing:-.01em}
.sub{color:var(--soft);margin:0;font-size:15.5px}
h2{font:600 24px/1.25 var(--serif);margin:48px 0 10px}
h4{font:600 11.5px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;color:var(--dim);margin:20px 0 8px}
h5{font:600 11px/1 var(--sans);letter-spacing:.1em;text-transform:uppercase;color:var(--dim);margin:0 0 6px}
.levels{display:flex;gap:10px;margin:24px 0 0;flex-wrap:wrap}
.levels button{appearance:none;cursor:pointer;font:600 14px/1 var(--sans);color:var(--soft);background:var(--card);
 border:1px solid var(--rule);border-radius:10px;padding:11px 16px;display:flex;flex-direction:column;gap:5px;
 align-items:flex-start;min-width:170px;text-align:left}
.levels button small{font:400 11.5px/1.3 var(--sans);color:var(--dim)}
.levels button[aria-pressed="true"]{background:var(--stage);border-color:var(--stage);color:#fff}
.levels button[aria-pressed="true"] small{color:rgba(255,255,255,.82)}
.key{display:flex;gap:18px;flex-wrap:wrap;margin:16px 0 0;font-size:12.5px;color:var(--dim)}
.key span{display:flex;align-items:center;gap:7px}

/* pills: only the active level's pill shows */
.st{display:none;font:600 10.5px/1 var(--sans);letter-spacing:.09em;text-transform:uppercase;
 padding:5px 9px;border-radius:20px;white-space:nowrap}
.s-required,.s-always{background:var(--stage);color:#fff}
.gatepill.s-always,.gatepill.s-required{background:var(--gate)}
.s-when-needed,.s-required-unless-excepted{background:var(--opt-bg);color:var(--opt)}
.s-not-needed{background:var(--band);color:var(--dim)}
.s-absent{background:var(--band);color:var(--dim)}
.s-undefined{background:var(--undef-bg);border:1px dashed var(--undef);color:var(--undef)}
${LVLS.map((l) => `.lvl-${l} .for-${l}{display:inline-block}`).join('\n')}
${LVLS.map((l) => `.lvl-${l} [data-${l}="absent"],.lvl-${l} [data-${l}="not-needed"]{opacity:.45}`).join('\n')}

details{background:var(--card);border:1px solid var(--rule);border-radius:12px;margin:0 0 10px}
details[open]{box-shadow:0 1px 0 rgba(0,0,0,.02)}
summary{cursor:pointer;list-style:none;display:flex;align-items:center;gap:13px;padding:14px 18px}
summary::-webkit-details-marker{display:none}
summary::after{content:"▸";margin-left:auto;color:var(--dim);font-size:13px;transition:transform .15s}
details[open]>summary::after{transform:rotate(90deg)}
.phase.stub{border:1px dashed var(--rule);background:var(--band);border-radius:12px;margin:0 0 10px;opacity:.75}
.phase.stub .shell{display:flex;align-items:flex-start;gap:13px;padding:14px 18px}
.phase.stub .sym{background:var(--undef-bg);color:var(--undef)}
.phase.stub .stat{margin-left:auto;flex:none;align-self:center}
.phase.stub .st{align-self:center}

/* vertical flow */

.flow{display:block;margin:18px 0 0}
.frow h5{margin:0 0 7px}
.fset{display:flex;flex-wrap:wrap;gap:9px}
.fset .fc{flex:0 1 190px;margin:0}
.cdown{text-align:center;color:var(--rule);font-size:16px;line-height:1;padding:5px 0}
.cdown.big{color:var(--dim)}
.chain{display:block}
details.cs{display:block;background:var(--card);border:1px solid var(--rule);border-left:4px solid var(--rule);
 border-radius:10px;margin:0}
details.cs>summary{display:flex;align-items:center;gap:10px;padding:11px 14px;flex-wrap:wrap}
.cs.lead-main{border-left-color:#8e9aa8}
.cs.lead-subagent{border-left-color:var(--skill)}
.cs.lead-guard{border-left-color:var(--stage)}
.cs.lead-workflow{border-left-color:var(--wf)}
details.cs.framed{box-shadow:inset 3px 0 0 var(--wf),0 0 0 2px #f7e6f1;margin-left:16px;position:relative}
details.cs.framed::before{content:"\\229E workflow";position:absolute;left:-16px;top:50%;transform:translateY(-50%) rotate(-90deg);
 transform-origin:center;font:700 8.5px/1 var(--sans);letter-spacing:.1em;color:var(--wf);white-space:nowrap}
.csbody{padding:0 14px 14px}
.mini{display:block;background:var(--band);border-radius:9px;padding:13px}
.mrow h6{font:600 9.5px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;color:var(--dim);margin:0 0 7px}
.mset{display:flex;flex-wrap:wrap;gap:0}
.mact{display:block}
.mact .ic{margin:0 auto 4px}
.mwho{display:block;font:600 9.5px/1 var(--sans);letter-spacing:.08em;text-transform:uppercase;color:var(--dim);margin-top:5px}
.rc{display:inline-flex;align-items:center;gap:5px;background:var(--card);border:1px solid var(--rule);
 border-radius:7px;padding:5px 8px;margin:0 5px 5px 0;font-size:12px}
.rc code{background:none;padding:0;font-size:11.5px}
.rc .mk{width:16px;height:16px;font-size:10px;margin:0;vertical-align:0}
.rc-none{color:var(--dim);font-style:italic}
.marr{color:var(--dim);font-size:14px}
.mact{background:var(--card);border:1px solid var(--rule);border-radius:9px;padding:12px 14px;text-align:center}
.mact b{display:block;font:600 13px/1.35 var(--sans);margin-top:5px}
.mload{margin-top:7px}


.fcol h5{margin:0 0 7px}
.fcol h5+.fc{margin-top:0}
.fc{position:relative;background:var(--card);border:1px solid var(--rule);padding:10px 11px 9px;margin:0 0 8px;
 clip-path:polygon(0 0,calc(100% - 11px) 0,100% 11px,100% 100%,0 100%)}
.fc::after{content:"";position:absolute;top:0;right:0;width:11px;height:11px;background:var(--band);
 border-left:1px solid var(--rule);border-bottom:1px solid var(--rule)}
.fc.d-in{border-left:3px solid var(--inst)}
.fc.d-out{border-left:3px solid var(--stage)}
.fc.d-both{border-left:3px solid var(--opt)}
.fc b{display:block;font:600 13.5px/1.3 var(--sans);margin:4px 0 0}
.fcmk{display:block;line-height:1}
.fcboth{display:block;font:600 9.5px/1 var(--sans);letter-spacing:.09em;text-transform:uppercase;color:var(--opt);margin-top:4px}
.fcst{display:block;margin-top:6px}
.mid{min-width:0}
.tstrip{display:flex;align-items:center;gap:8px;flex-wrap:wrap;margin:10px 0 0;padding:9px 11px;
 background:var(--opt-bg);border-radius:9px}
.tlab{font:600 9.5px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;color:var(--opt)}
.tchip{display:inline-flex;align-items:center;gap:6px;font:600 11.5px/1.4 var(--sans);color:var(--ink)}
.tchip code{font-size:10px;background:#fff8e8;border:1px solid var(--trig-line);color:var(--opt)}
.gband{display:flex;align-items:center;gap:11px;margin:10px 0 0;padding:12px 15px;background:var(--gate-soft);
 border:2px solid var(--gate-line);border-radius:10px}
.gband .gdia{color:var(--gate);font-size:19px;line-height:1}
.gband b{font:600 15px/1.3 var(--sans);color:var(--gate)}
.gband .gsub{font-size:12px;color:var(--soft);flex:1 1 auto}
.gdetail{border-top:1px solid var(--rule);padding-top:14px;margin-top:6px}
@media(max-width:820px){.flow{grid-template-columns:1fr}.carr{display:none}.cs{flex:1 1 100%}}
.pnote{margin:18px 0 0;font-size:13.5px;color:var(--soft);padding:11px 14px;background:var(--band);border-left:3px solid var(--rule);border-radius:0 8px 8px 0}
.mk.m-trig{background:var(--opt-bg);color:var(--opt)}
.stubnote{display:block;font-size:12.5px;color:var(--dim);margin-top:6px;max-width:56ch}
.sym{flex:none;width:42px;height:42px;border-radius:11px;display:grid;place-items:center;
 background:var(--stage);color:#fff;font:700 14px/1 var(--sans);letter-spacing:.06em}
.ptitle{display:flex;flex-direction:column;gap:2px;flex:1 1 auto}
.ptitle b{font:600 17px/1.3 var(--serif)}
.purpose{font-size:13.5px;color:var(--soft)}
.pbody{padding:0 18px 18px}
.meta{font:400 12.5px/1.4 var(--sans);color:var(--dim)}
.oneof{display:block;font:600 11px/1 var(--sans);letter-spacing:.08em;text-transform:uppercase;
 color:var(--gate);margin-top:5px}
table{width:100%;border-collapse:collapse;font-size:13.5px;border:1px solid var(--rule);border-radius:10px;overflow:hidden}
th,td{text-align:left;padding:9px 12px;border-bottom:1px solid var(--rule);vertical-align:top}
tr:last-child td{border-bottom:none}
th{font:600 10.5px/1 var(--sans);letter-spacing:.1em;text-transform:uppercase;color:var(--dim);background:var(--band);padding:9px 12px}
td.p{width:120px;text-align:right}
code{font:.88em/1.3 var(--mono);background:var(--band);padding:1px 5px;border-radius:4px}
.mk{display:inline-grid;place-items:center;width:22px;height:22px;border-radius:6px;
 font-size:13px;line-height:1;margin-right:5px;vertical-align:-5px;background:var(--band);color:var(--soft)}
.mk.m-kind{margin-right:8px}
.v-skill{background:var(--skill-soft)!important;color:var(--skill)!important}
.m-kind.v-other{border:1px dashed var(--undef);color:var(--undef)!important}
.m-origin.v-guard,.m-actor.v-guard{background:var(--stage-soft)!important;color:var(--stage)!important}
.m-actor.v-main{background:#e9edf2!important;color:#3b4a5c!important}
.m-actor.v-subagent{background:var(--skill-soft)!important;color:var(--skill)!important}
.m-actor.v-workflow{background:#f7e6f1!important;color:var(--wf)!important}
.runs{display:inline-flex;align-items:center;gap:3px;flex-wrap:wrap}
.rl{font:600 10.5px/1 var(--sans);letter-spacing:.07em;text-transform:uppercase;padding:4px 8px;
 border-radius:20px;margin-right:9px;background:#e9edf2;color:#3b4a5c}
.rl.r-subagent{background:var(--skill-soft);color:var(--skill)}
.rl.r-guard{background:var(--stage-soft);color:var(--stage)}
.rl.r-workflow{background:#f7e6f1;color:var(--wf)}
.rl.r-presence{background:var(--gate-soft);color:var(--gate)}
.loads{display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin:9px 0 0;padding:9px 12px;
 background:var(--band);border-radius:9px}
.op{display:inline-flex;align-items:center;gap:6px;border:1px solid var(--rule);border-radius:7px;
 padding:4px 7px;background:var(--card)}
.op code{font-size:10.5px;background:none;padding:0}
.op .stat{padding:3px 6px;font-size:9px}
.opsf{font:600 8.5px/1 var(--sans);letter-spacing:.09em;text-transform:uppercase;color:var(--inst);
 background:var(--inst-soft);padding:3px 5px;border-radius:4px}
.op.sf-hook .opsf{color:var(--stage);background:var(--stage-soft)}
.op.sf-none .opsf{color:var(--dim);background:var(--band)}
.op.big{margin-top:2px}
.mvia{margin-top:9px;border-top:1px solid var(--rule);padding-top:9px}
.mvia h6{font:600 9.5px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;color:var(--dim);margin:0 0 6px}
.topo{background:var(--card);border:1px solid var(--rule);border-radius:12px;padding:16px 18px}
.tnodes{display:flex;gap:11px;flex-wrap:wrap;margin-bottom:16px}
.tn{display:flex;align-items:flex-start;gap:10px;border:1px solid var(--rule);border-radius:10px;padding:10px 12px;flex:1 1 200px}
.tn b{font:600 13px/1.3 var(--sans)}
.tng{font-size:16px;line-height:1}
.tn.n-main{border-left:3px solid #8e9aa8}.tn.n-main .tng{color:#3b4a5c}
.tn.n-subagent{border-left:3px solid var(--skill)}.tn.n-subagent .tng{color:var(--skill)}
.tn.n-workflow{border-left:3px solid var(--wf)}.tn.n-workflow .tng{color:var(--wf)}
.tn.n-guard{border-left:3px solid var(--stage)}.tn.n-guard .tng{color:var(--stage)}
.tcalls{display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:8px}
.tc{display:flex;align-items:center;gap:9px;font-size:12.5px;padding:9px 11px;border-radius:9px;background:var(--band)}
.tc.no{background:#f6f2f2;color:var(--dim)}
.tarr{font-size:14px;color:var(--stage);font-weight:700}
.tc.no .tarr{color:#b2544f}
.thow{color:var(--soft);font-size:11.5px}
.tc.no .thow{font-style:italic}
.ic{display:block;flex:none}
.a-main{color:#3b4a5c}.a-subagent{color:var(--skill)}.a-workflow{color:var(--wf)}.a-guard{color:var(--stage)}
.key span .ic,.ab .ic{display:inline-block;vertical-align:-4px}
.ab{display:inline-flex;align-items:center;gap:6px;font:600 10.5px/1 var(--sans);letter-spacing:.08em;
 text-transform:uppercase;padding:4px 9px 4px 6px;border-radius:20px;background:var(--band)}
.ab.a-main{background:#e9edf2;color:#3b4a5c}
.ab.a-subagent{background:var(--skill-soft);color:var(--skill)}
.ab.a-workflow{background:#f7e6f1;color:var(--wf)}
.ab.a-guard{background:var(--stage-soft);color:var(--stage)}
.tn>.ic{flex:none}
.tnn{display:block;font-size:11.5px;line-height:1.45;color:var(--dim);margin-top:3px}
.cc{display:inline-flex;align-items:center;gap:5px;flex-wrap:wrap;margin-top:6px}
.cca{font-style:normal;color:var(--dim);font-size:13px;line-height:1}
.cc-op{font-size:11px;background:var(--stage-soft);color:var(--stage);border-radius:5px;padding:3px 7px}
.cc-none{font:italic 11.5px/1 var(--sans);color:var(--dim)}
.csmeta{display:flex;align-items:center;gap:8px;flex-wrap:wrap;margin-top:7px}
.mchain{display:flex;justify-content:center;margin:8px 0 2px}
.mchain .cc-op{font-size:12px}
.cc-call{display:inline-flex;align-items:center;gap:5px;flex-wrap:wrap}
.cc-args{display:inline-flex;gap:4px;flex-wrap:wrap}
.cc-arg{font-size:10.5px;background:var(--card);border:1px solid var(--stage);color:var(--stage);
 border-radius:5px;padding:2px 6px}
.fcf{display:flex;flex-wrap:wrap;gap:3px;margin-top:6px}
.fcf code{font-size:9.5px;background:var(--band);color:var(--soft);padding:2px 5px;border-radius:4px}
.gband .gsub{display:flex;flex-wrap:wrap;gap:4px}
.gband .gsub code{font-size:10.5px;background:#fff;color:var(--gate);border:1px solid var(--gate-line);
 border-radius:5px;padding:3px 6px}
.s-undefined{background:repeating-linear-gradient(135deg,#fdf0d5 0 7px,#f8e2b4 7px 14px)!important;
 border:1px solid #d8a93f!important;color:#7a5200!important;font-weight:700!important}
.lvl-l1 .row[data-l1="undefined"],.lvl-l2 .row[data-l2="undefined"],.lvl-l3 .row[data-l3="undefined"],
.lvl-l1 details.cs[data-l1="undefined"],.lvl-l2 details.cs[data-l2="undefined"],.lvl-l3 details.cs[data-l3="undefined"],
.lvl-l1 .fc[data-l1="undefined"],.lvl-l2 .fc[data-l2="undefined"],.lvl-l3 .fc[data-l3="undefined"]
{box-shadow:inset 0 0 0 2px #d8a93f}
.ow{margin:0 0 16px}
.owl{display:none;align-items:center;gap:9px;background:#fdf7e8;border:1px solid #e8cf9a;
 border-radius:10px;padding:11px 14px;font-size:13.5px;color:var(--soft)}
.owl b{font:700 17px/1 var(--serif);color:#9a6a00}
.ohere{display:none;align-items:flex-start;gap:10px;margin:14px 0 0;padding:12px 14px;
 background:repeating-linear-gradient(135deg,#fdf6e6 0 10px,#fbefd6 10px 20px);
 border:2px solid #d8a93f;border-radius:10px;color:#7a5200}
${LVLS.map((l) => `.lvl-${l} .ohere.for-${l}{display:flex}`).join('\n')}
.ohlab{font:700 10.5px/1.6 var(--sans);letter-spacing:.09em;text-transform:uppercase;white-space:nowrap}
.ohset{display:flex;flex-wrap:wrap;gap:5px}
.ohi{font-size:12px;background:#fff;border:1px solid #e3cfa0;border-radius:6px;padding:4px 8px;color:var(--ink)}
.ohi.k-phase{border-color:#b2544f;color:#8d3b36;font-weight:600}
.ocnt{display:none;align-items:center;gap:4px;font:700 12px/1 var(--sans);color:#9a6a00;
 background:#fdf0d5;border:1px solid #d8a93f;border-radius:20px;padding:4px 9px 4px 6px}
${LVLS.map((l) => `.lvl-${l} .ocnt.for-${l}{display:inline-flex}`).join('\n')}
${LVLS.map((l) => `.lvl-${l} .owl.for-${l}{display:block}`).join('\n')}
.owhead{display:flex;align-items:center;gap:10px;color:#9a6a00}
.owhead b{font:700 26px/1 var(--serif)}
.owhead span{font-size:14px;color:var(--soft)}
.csec{display:block;font:600 9.5px/1.4 var(--sans);letter-spacing:.06em;text-transform:uppercase;
 color:var(--wf);margin-top:4px}
.llab{font:600 10px/1 var(--sans);letter-spacing:.11em;text-transform:uppercase;color:var(--dim)}
.ld{display:inline-flex;align-items:center;gap:7px}
.stat{font:600 10.5px/1 var(--sans);letter-spacing:.09em;text-transform:uppercase;padding:5px 9px;border-radius:20px}
.stat.s-exists{background:var(--stage);color:#fff}
.stat.s-proposed{background:var(--skill-soft);color:var(--skill)}
.stat.s-undefined{background:var(--undef-bg);border:1px dashed var(--undef);color:var(--undef)}

.gate{background:var(--gate-soft);border:2px solid var(--gate-line);border-radius:12px;padding:14px 16px}
.ghead{display:flex;align-items:center;gap:10px;margin-bottom:10px}
.ghead .dia{color:var(--gate);font-size:22px;line-height:1}
.ghead b{font:600 16px/1.3 var(--sans);color:var(--gate)}
.ghead .st{margin-left:auto}
.gcols{display:grid;grid-template-columns:1fr 1fr;gap:18px}
.gcols ul,.rw ul{margin:0;padding-left:18px}
.gcols li,.rw li{margin-bottom:4px;font-size:13.5px}
.reopen{margin:12px 0 0;font-size:13px;color:var(--soft)}
.close{margin:16px 0 0;font-size:13.5px;color:var(--soft);padding:11px 14px;background:var(--band);border-radius:9px}

details.deep{margin:18px 0 0;border-style:dashed;background:transparent}
details.deep>summary{font:600 12px/1 var(--sans);letter-spacing:.09em;text-transform:uppercase;color:var(--dim);padding:12px 16px}
.dbody{padding:0 16px 16px}
.step{border-top:1px solid var(--rule);padding:14px 0 4px}
.step:first-child{border-top:none;padding-top:4px}
.shead{display:flex;align-items:center;gap:11px;flex-wrap:wrap}
.shead b{font:600 15px/1.3 var(--sans)}
.actor{font:600 11px/1 var(--sans);letter-spacing:.07em;text-transform:uppercase;color:var(--stage);
 background:var(--stage-soft);padding:4px 8px;border-radius:20px}
.shead .st{margin-left:auto}
.snote{margin:8px 0 10px;font-size:13.5px;color:var(--soft)}
.rw{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-bottom:6px}
details.doc>summary{gap:11px}
footer{margin-top:50px;padding-top:16px;border-top:1px solid var(--rule);color:var(--dim);font-size:13px}
@media(max-width:700px){.gcols,.rw{grid-template-columns:1fr}}
@media print{html,body{background:#fff}.levels{display:none}details{break-inside:avoid}}
</style></head><body><div class="page">

<div class="mast"><span>AgentGuard · Workflow model</span><span>Generated from ${SRC}</span></div>
<h1>Workflow model</h1>
<p class="sub">${model.phases.filter((p)=>(p.status??'defined')==='defined').length} of ${model.phases.length} phases defined. Expand a phase for its documents and gate; expand again for its steps and the fields they write.</p>

<div class="levels" role="group" aria-label="Level">${buttons}</div>
<div class="key">
  <span><i class="st s-required" style="display:inline-block">required</i></span>
  <span><i class="st s-when-needed" style="display:inline-block">when needed</i></span>
  <span><i class="st s-not-needed" style="display:inline-block">not needed</i></span>
  <span><i class="st s-absent" style="display:inline-block">absent</i></span>
  <span><i class="st s-undefined" style="display:inline-block">undefined</i></span>
</div>
<div class="key">${model.actors.map((a) => `<span class="a-${esc(a.id)}">${icon(a.id, 18)} ${esc(a.name)}</span>`).join('')}${legend('origin')}${legend('form')}<span>${icon('kind:skill', 18)} skill</span></div>

<h2>The four processes, and the calls that are possible</h2>
${topology()}

<h2>Phases</h2>
${openWork()}
<div id="phases">${model.phases.map(phaseBlock).join('')}</div>

<h2>Guard operations</h2>
<table><thead><tr><th>Operation</th><th>Surface</th><th>Status</th><th>Writes</th></tr></thead><tbody>
${(model.operations ?? []).map((o) => `<tr><td><code>${esc(o.name)}</code>
  <span class="meta">${txt(o.note)}</span></td>
  <td class="meta">${o.surface === 'cli+mcp' ? 'CLI &amp; MCP' : esc(o.surface)}</td>
  <td><span class="stat s-${esc(o.status)}">${esc(o.status)}</span></td>
  <td>${txt(o.writes)}</td></tr>`).join('')}
</tbody></table>

<h2>Skills</h2>
<table><thead><tr><th>Source</th><th>Status</th><th>What it owns</th></tr></thead><tbody>
${(model.skills ?? []).map((i) => `<tr><td>${skillMark()}<code>${esc(i.name)}</code>
  ${i.source ? `<span class="meta">${esc(i.source)}</span>` : ''}</td>
  <td><span class="stat s-${esc(i.status)}">${esc(i.status)}</span></td>
  <td>${txt(i.owns)}</td></tr>`).join('')}
</tbody></table>

<h2>Documents</h2>
<div>${model.documents.map(docBlock).join('')}</div>

<footer>Generated from <code>${SRC}</code> by <code>render-model.mjs</code>. Edit the YAML, then <code>npm run render</code>. Nothing here is approved.</footer>

<script>
const root=document.documentElement;
function set(l){root.className='lvl-'+l;
 document.querySelectorAll('.levels button').forEach(b=>b.setAttribute('aria-pressed',b.dataset.lvl===l));}
document.querySelectorAll('.levels button').forEach(b=>b.addEventListener('click',()=>set(b.dataset.lvl)));
set('${LVLS[0]}');
</script>
</div></body></html>`

writeFileSync(OUT, html)
console.log(`${OUT}: ${model.phases.length} phase(s), ${model.documents.length} document(s)`)
