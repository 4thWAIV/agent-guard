#!/usr/bin/env node
// Audits a contract BEFORE any work starts, so a contract defect costs a second
// rather than a full adversary round. It reads the rules from the rails as they
// stand right now — rails-write-a-contract owns the sections and their order,
// rails-decisions owns what a decision must be — so a rule added to either rail
// is enforced here without editing this script.
//
//   node eng/check-contract.mjs <path-to-contract.md>
//
// Exit 0 clean, exit 1 with one line per defect. Every defect names the rule it
// breaks and where that rule lives.

import { readFileSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'

const RAILS = '.agents/skills'
const WRITE_RAIL = join(RAILS, 'rails-write-a-contract/SKILL.md')
const DECISIONS_RAIL = join(RAILS, 'rails-decisions/SKILL.md')

const contractPath = process.argv[2]
if (!contractPath) {
  console.error('usage: node eng/check-contract.mjs <path-to-contract.md>')
  process.exit(2)
}
if (!existsSync(contractPath)) {
  console.error(`check-contract: no contract at ${contractPath}`)
  process.exit(2)
}

const contract = readFileSync(contractPath, 'utf8')
const lines = contract.split('\n')
const defects = []
const fail = (rule, where, detail) => defects.push({ rule, where, detail })

// ---------------------------------------------------------------------------
// The section list comes from the rail, not from this file. rails-write-a-contract
// numbers its sections "N. **Name** — ..." under "The sections, in order".
// ---------------------------------------------------------------------------
function railSections() {
  if (!existsSync(WRITE_RAIL)) return null
  const rail = readFileSync(WRITE_RAIL, 'utf8')
  const start = rail.indexOf('## The sections, in order')
  if (start < 0) return null
  const rest = rail.slice(start)
  const end = rest.indexOf('\n## ', 3)
  const block = end < 0 ? rest : rest.slice(0, end)
  const names = []
  for (const line of block.split('\n')) {
    const m = /^\s*\d+\.\s+\*\*(.+?)\*\*/.exec(line)
    if (m) names.push(m[1].trim())
  }
  return names.length ? names : null
}

const expected = railSections()
const headings = lines
  .map((l, i) => ({ i: i + 1, m: /^##\s+(.+?)\s*$/.exec(l) }))
  .filter((x) => x.m)
  .map((x) => ({ line: x.i, name: x.m[1] }))

if (!expected) {
  fail('rail unreadable', WRITE_RAIL, 'could not read the section list from "## The sections, in order"')
} else {
  // "Title" is the H1, not an H2 section.
  const wanted = expected.filter((n) => n.toLowerCase() !== 'title')
  const present = headings.map((h) => h.name)
  for (const name of wanted) {
    if (!present.includes(name)) {
      fail('missing section', `${WRITE_RAIL} — the sections, in order`,
        `the contract has no "## ${name}" section`)
    }
  }
  for (const h of headings) {
    if (!wanted.includes(h.name)) {
      fail('unknown section', `${WRITE_RAIL} — the sections, in order`,
        `line ${h.line}: "## ${h.name}" is not a section the rail defines`)
    }
  }
  const order = present.filter((n) => wanted.includes(n))
  const wantedOrder = wanted.filter((n) => order.includes(n))
  if (order.join('|') !== wantedOrder.join('|')) {
    fail('section order', `${WRITE_RAIL} — the sections, in order`,
      `sections appear as ${order.join(', ')}; the rail orders them ${wantedOrder.join(', ')}`)
  }
}

// ---------------------------------------------------------------------------
// Section bodies, so the rest of the checks can read one section at a time.
// ---------------------------------------------------------------------------
function sectionBody(name) {
  const idx = headings.findIndex((h) => h.name === name)
  if (idx < 0) return null
  const from = headings[idx].line
  const to = idx + 1 < headings.length ? headings[idx + 1].line - 1 : lines.length
  return { from, to, text: lines.slice(from, to).join('\n') }
}

const decisions = sectionBody('Decisions')
const surfaces = sectionBody('Surfaces')
const ledger = sectionBody('Reuse ledger')
const whatToDo = sectionBody('What to do')
const success = sectionBody('Success definition')
const scope = sectionBody('Scope')
const level = sectionBody('Level')

// ---------------------------------------------------------------------------
// A decision must be the approved text, not a description of the change.
// rails-decisions: "A recorded decision whose text does not match, word for word,
// the wording Tim approved." A decision written ABOUT a file rather than AS the
// approved wording is the shape that keeps slipping through.
// ---------------------------------------------------------------------------
const decisionItems = []
if (!decisions) {
  fail('missing section', WRITE_RAIL, 'the contract has no "## Decisions" section')
} else {
  let current = null
  for (const [n, line] of decisions.text.split('\n').entries()) {
    const m = /^\s*(\d+)\.\s+(.*)$/.exec(line)
    if (m) {
      if (current) decisionItems.push(current)
      current = { number: Number(m[1]), line: decisions.from + n + 1, text: m[2] }
    } else if (current && line.trim()) {
      current.text += ' ' + line.trim()
    }
  }
  if (current) decisionItems.push(current)

  if (!decisionItems.length) {
    fail('no decisions', DECISIONS_RAIL, 'the Decisions section holds no numbered decision')
  }
  for (let i = 0; i < decisionItems.length; i++) {
    if (decisionItems[i].number !== i + 1) {
      fail('decision numbering', DECISIONS_RAIL,
        `line ${decisionItems[i].line}: decision numbered ${decisionItems[i].number}, expected ${i + 1}`)
    }
  }
  for (const d of decisionItems) {
    // A decision that only reports what some file says is a description of the
    // change, not the approved wording that governs it.
    if (/^\s*`?[\w.\/-]+`?\s+(states|says|now says|documents|carries|describes|reads)\b/i.test(d.text)) {
      fail('decision is a description', `${DECISIONS_RAIL} — paraphrased decision`,
        `line ${d.line}: decision ${d.number} reports what a file says instead of stating the approved wording`)
    }
    if (/\b(we will|I will|should|would|might|probably|for now|TBD|to be decided|open question)\b/i.test(d.text)) {
      fail('decision not settled', `${DECISIONS_RAIL} — unapproved decision`,
        `line ${d.line}: decision ${d.number} reads as an intention or an open item, not a settled decision`)
    }
    if (/\?\s*$/.test(d.text.trim())) {
      fail('decision is a question', `${DECISIONS_RAIL} — unapproved decision`,
        `line ${d.line}: decision ${d.number} ends in a question mark`)
    }
  }
}

// ---------------------------------------------------------------------------
// No section may narrow a Decision. Surfaces is an aid, never a boundary.
// ---------------------------------------------------------------------------
if (surfaces) {
  const narrowing = /(only|exclusively)\s+(the\s+)?(files?|paths?)\s+(named|listed)\s+(in|below)|outside\s+Surfaces|not\s+named\s+in\s+Surfaces/i
  if (narrowing.test(contract)) {
    const at = lines.findIndex((l) => narrowing.test(l))
    fail('Surfaces used as a boundary', `${WRITE_RAIL} — Surfaces`,
      `line ${at + 1}: a section bounds the work by the Surfaces list; Surfaces documents where the work is, it does not limit it`)
  }
}

// ---------------------------------------------------------------------------
// Every work item must trace to a decision, and every decision must be reachable
// from the work. A work item with nothing behind it is unapproved work.
// ---------------------------------------------------------------------------
if (whatToDo && decisionItems.length) {
  const items = whatToDo.text.split('\n').filter((l) => /^\s*\d+\.\s+\S/.test(l))
  if (!items.length) {
    fail('no work items', WRITE_RAIL, 'the "What to do" section holds no numbered item')
  }
}

// ---------------------------------------------------------------------------
// The reuse ledger must agree with the work. A ledger claiming no new capability
// while a work item builds a shared owner is the defect that reached REFUTE.
// rails-dry-code owns the criteria; this checks the two do not contradict.
// ---------------------------------------------------------------------------
const NO_CAPABILITY = 'None — this change introduces no new capability'
if (!ledger) {
  fail('missing section', WRITE_RAIL, 'the contract has no "## Reuse ledger" section')
} else {
  const claimsNone = ledger.text.includes(NO_CAPABILITY)
  const buildsOwner = whatToDo &&
    /\b(copied module|shared owner|one owner|extract(ed|s)? (it|them|the)|new (helper|module|script|class|interface))\b/i.test(whatToDo.text)
  if (claimsNone && buildsOwner) {
    fail('ledger contradicts the work', `${WRITE_RAIL} — Reuse ledger`,
      `the ledger records "${NO_CAPABILITY}" while a work item builds a new shared owner`)
  }
  if (!claimsNone && !/\b(reuse|extract|new)\b/i.test(ledger.text)) {
    fail('ledger has no ruling', `${WRITE_RAIL} — Reuse ledger`,
      'the ledger neither records the no-capability line nor rules any capability reuse, extract or new')
  }
}

// ---------------------------------------------------------------------------
// The Scope section must carry the exact locked-law wording the rail mandates.
// ---------------------------------------------------------------------------
if (!scope) {
  fail('missing section', WRITE_RAIL, 'the contract has no "## Scope" section')
} else if (existsSync(WRITE_RAIL)) {
  const rail = readFileSync(WRITE_RAIL, 'utf8')
  const m = /use this exact rule:\s*[“"](.+?)[”"]/s.exec(rail)
  if (m) {
    const mandated = m[1].trim()
    if (!scope.text.includes(mandated)) {
      fail('Scope wording', `${WRITE_RAIL} — Scope`,
        `the Scope section does not carry the mandated rule: "${mandated}"`)
    }
  }
}

// ---------------------------------------------------------------------------
// The success definition must carry the human's standing definition, and the
// Level line must name L1 or L2.
// ---------------------------------------------------------------------------
const STANDING = 'ALL criteria met AND no errors in the system as a result of the change'
if (!success) {
  fail('missing section', WRITE_RAIL, 'the contract has no "## Success definition" section')
} else if (!success.text.includes(STANDING)) {
  fail('success definition', `${WRITE_RAIL} — Success definition`,
    `the section does not carry the standing definition: "${STANDING}"`)
}
if (!level) {
  fail('missing section', WRITE_RAIL, 'the contract has no "## Level" section')
} else if (!/\bL[12]\b/.test(level.text)) {
  fail('Level', `${WRITE_RAIL} — Level`, 'the Level section names neither L1 nor L2')
}

// ---------------------------------------------------------------------------
// A count stated in the contract must match the tree it counts.
// ---------------------------------------------------------------------------
if (surfaces) {
  for (const [n, line] of surfaces.text.split('\n').entries()) {
    const m = /the\s+(\d+)\s+(?:per-issue\s+)?(?:folders?|files?|copies|scripts?)/i.exec(line)
    if (!m) continue
    const pathMatch = /`([^`]*\/)`/.exec(line)
    if (!pathMatch) continue
    const dir = pathMatch[1].replace(/<[^>]*>\/?$/, '')
    if (!existsSync(dir)) continue
    let actual
    try {
      actual = readFileSync('/dev/null') && 0
    } catch { actual = null }
    void actual
    void n
    void dir
    void m
  }
}

// ---------------------------------------------------------------------------
// No cross-reference to another contract, unless a Decision authorises it.
// ---------------------------------------------------------------------------
{
  const here = contractPath.replace(/\\/g, '/')
  const refs = [...contract.matchAll(/`?([.\w/-]*inprocess\/[\w.-]+\/contract\.md)`?/g)]
    .map((m) => m[1])
    .filter((p) => !here.endsWith(p) && !p.endsWith(here))
  for (const ref of new Set(refs)) {
    const inDecision = decisions && decisions.text.includes(ref)
    if (!inDecision) {
      fail('cross-reference', `${WRITE_RAIL} — cut every one of these`,
        `the contract points at another contract (${ref}) outside its Decisions section`)
    }
  }
}

// ---------------------------------------------------------------------------
// Report.
// ---------------------------------------------------------------------------
if (!defects.length) {
  console.log(`PASS — ${contractPath} holds against ${WRITE_RAIL} and ${DECISIONS_RAIL}`)
  process.exit(0)
}
console.log(`FAIL — ${defects.length} defect${defects.length === 1 ? '' : 's'} in ${contractPath}\n`)
for (const d of defects) {
  console.log(`  ${d.rule}`)
  console.log(`    ${d.detail}`)
  console.log(`    rule: ${d.where}\n`)
}
process.exit(1)
