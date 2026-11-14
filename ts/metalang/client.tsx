// Mounts the page. The only thing in the bundle that touches the DOM.
//
// Everything it draws comes from the data block in the page itself, read at run time. That is
// what lets one bundle, built once, draw a document it has never seen: see page-data.ts for the
// block and for the two properties this keeps.
//
// It also reads whether JARVIS's story helper is there, because the window is where the helper
// says so and this file is the one thing here that reads the window.

import { createRoot } from 'react-dom/client'
import type { ReactNode } from 'react'

import { NotNarratedHere, Page } from './app.tsx'
import { dataFrom, DATA_ID, marksFrom } from './page-data.ts'

declare global {
  interface Window {
    /**
     * Set by JARVIS's story helper as it loads, and by nothing else. The helper reads it the same
     * way it writes it — `if (window.__jarvisStory) return; window.__jarvisStory = true;` — so
     * anything truthy is the announcement and the property being absent is no helper.
     */
    __jarvisStory?: unknown
  }
}

const root = document.getElementById('root')
if (root === null) throw new Error('the page has no #root to draw into')

const block = document.getElementById(DATA_ID)
if (block === null) throw new Error(`the page has no #${DATA_ID} element to read its data from`)

const data = dataFrom(block.textContent)

const page = (
  <Page words={data.words} doc={data.doc} marks={marksFrom(data.marks)} found={data.found} />
)

/** The page, and above it whatever the page has to say about being narrated. */
const drawnSaying = (says: ReactNode): ReactNode => (
  <>
    {says}
    {page}
  </>
)

const drawing = createRoot(root)

// Drawn saying nothing about narration, because whether the helper is there is not known yet: this
// bundle is written into the page inline, so it runs while the document is still being parsed and
// every deferred script — the helper among them — runs after it.
drawing.render(drawnSaying(null))

/**
 * Runs the reading once the deferred scripts have run, which is the earliest moment the helper
 * can have announced itself.
 *
 * The browser runs every deferred script after the document is parsed and BEFORE it fires
 * `DOMContentLoaded`, so that event is the moment, and this bundle is always listening for it:
 * render.ts writes the bundle into the page inline, and an inline script runs while the document
 * is still being parsed, which is before the event every time. The ready state cannot stand in
 * for the event — `interactive` is set before the deferred scripts run, not after.
 */
const onceTheDeferredScriptsHaveRun = (read: () => void): void => {
  document.addEventListener('DOMContentLoaded', read, { once: true })
}

onceTheDeferredScriptsHaveRun(() => {
  if (window.__jarvisStory) return
  drawing.render(drawnSaying(<NotNarratedHere />))
})
