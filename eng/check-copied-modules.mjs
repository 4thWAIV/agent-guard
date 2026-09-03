#!/usr/bin/env node
// The Workflow runtime offers no module import and caps workflow() nesting at one level, so shared
// workflow code is COPIED into each script that needs it. This check is what keeps those copies honest:
// it extracts every marked block from every workflow script and requires that all copies of a given
// block name are byte-identical. Update one copy and you must update them all.
//
// It hardcodes no expected content. It only compares the copies against each other.
import fs from 'fs'
import path from 'path'

const DIR = path.join(process.cwd(), '.agents/workflows')
const BLOCK = /^\/\/ ##COPIED-MODULE-BEGIN## (\S+)$\n([\s\S]*?)^\/\/ ##COPIED-MODULE-END## \1$/gm

const byName = new Map()   // block name -> [{ file, body }]
let files = 0, blocks = 0

for (const file of fs.readdirSync(DIR).filter((f) => f.endsWith('.js')).sort()) {
  files++
  const src = fs.readFileSync(path.join(DIR, file), 'utf8')
  const seen = new Set()
  for (const m of src.matchAll(BLOCK)) {
    const [, name, body] = m
    if (seen.has(name)) {
      console.error(`FAIL  ${file} contains block "${name}" more than once`)
      process.exit(1)
    }
    seen.add(name)
    blocks++
    if (!byName.has(name)) byName.set(name, [])
    byName.get(name).push({ file, body })
  }
  // an unbalanced marker would silently drop a block from the comparison
  const begins = (src.match(/^\/\/ ##COPIED-MODULE-BEGIN##/gm) || []).length
  const ends = (src.match(/^\/\/ ##COPIED-MODULE-END##/gm) || []).length
  if (begins !== ends || begins !== seen.size) {
    console.error(`FAIL  ${file} has unbalanced or unparsable markers (${begins} begin, ${ends} end, ${seen.size} matched)`)
    process.exit(1)
  }
}

let drifted = 0
for (const [name, copies] of [...byName].sort()) {
  if (copies.length < 2) {
    console.log(`  ${name.padEnd(26)} 1 copy   (${copies[0].file}) — nothing to compare`)
    continue
  }
  const reference = copies[0]
  const differing = copies.filter((c) => c.body !== reference.body)
  if (!differing.length) {
    console.log(`  ${name.padEnd(26)} ${String(copies.length).padStart(2)} copies  identical`)
    continue
  }
  drifted++
  console.error(`FAIL  ${name}: ${differing.length} of ${copies.length} copies differ from ${reference.file}`)
  for (const c of differing) {
    const a = reference.body.split('\n'), b = c.body.split('\n')
    const i = a.findIndex((line, idx) => line !== b[idx])
    console.error(`        ${c.file} first differs at block line ${i + 1}`)
    console.error(`          ${reference.file}: ${JSON.stringify((a[i] ?? '<missing>').trim().slice(0, 90))}`)
    console.error(`          ${c.file}: ${JSON.stringify((b[i] ?? '<missing>').trim().slice(0, 90))}`)
  }
}

console.log(`\n${files} workflow files, ${blocks} copied blocks, ${byName.size} distinct block names`)
if (drifted) { console.error(`\nFAIL — ${drifted} block name(s) drifted. Every copy of a block must be byte-identical.`); process.exit(1) }
console.log('PASS — every duplicated block is byte-identical across all copies')
