import { chromium } from 'playwright-core'
const b = await chromium.launch({ channel: 'chrome' })
const p = await b.newPage({ viewportSize: { width: 1240, height: 1500 }, deviceScaleFactor: 2 })
const errs = []
p.on('pageerror', e => errs.push(e.message))
await p.goto('file://' + process.argv[2]); await p.waitForTimeout(400)
console.log('page errors:', errs.length ? errs : 'none')
console.log('error blocks:', await p.evaluate(() => document.querySelectorAll('.err').length))
await p.screenshot({ path: process.argv[3] })
await b.close()
