// Fails when a rendered page is older than anything it is built from.
//
// The pages are computed, not written — every count and every verdict on them is derived
// when the page draws. That only helps if the page you are reading was built from the data
// as it stands now. Nothing on a page can say "I am out of date", so this says it.
//
// Every page is built from every source: one bundle, one component set, three documents.
// There is nothing to apportion, so the rule is simply that a page must be newer than all
// of them. It also checks itself — a page nobody declares, or a source nobody uses, is a
// gap in this file rather than something to pass over quietly.
import { readdirSync, statSync } from 'node:fs'

const here = new URL('.', import.meta.url).pathname
const view = here + 'view/'
const at = (f) => statSync(f).mtimeMs

const PAGES = ['contract.html', 'architecture.html', 'language.html', 'meta-language.html']

const yaml = readdirSync(here).filter((f) => f.endsWith('.yaml')).map((f) => here + f)
const code = readdirSync(view)
  .filter((f) => (f.endsWith('.mjs') || f.endsWith('.jsx') || f.endsWith('.css')) && !f.startsWith('.'))
  .map((f) => view + f)
const sources = [...yaml, ...code, here + 'language-types.mjs']

const problems = []
const rendered = readdirSync(here).filter((f) => f.endsWith('.html'))
for (const f of rendered) {
  if (!PAGES.includes(f) && f !== 'contract.faults.html') {
    problems.push(`${f} is rendered but is not one of the documents this file knows about`)
  }
}
for (const p of PAGES) if (!rendered.includes(p)) problems.push(`${p} was never built`)

const stale = []
if (!problems.length) {
  for (const p of PAGES) {
    const newer = sources.filter((s) => at(s) > at(here + p)).map((s) => s.replace(here, ''))
    if (newer.length) stale.push({ page: p, newer })
  }
}

for (const x of problems) console.error(`GAP    ${x}`)
for (const { page, newer } of stale) console.error(`STALE  ${page}  — older than ${newer.join(', ')}`)

if (problems.length || stale.length) {
  const n = problems.length + stale.length
  console.error(`\n${n} problem${n === 1 ? '' : 's'}. Run \`node view/build.mjs\`.`)
  process.exit(1)
}
console.log(`fresh: ${PAGES.length} pages, each newer than all ${sources.length} sources`)
