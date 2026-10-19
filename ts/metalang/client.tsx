// Mounts the page. The only thing in the bundle that touches the DOM.

import { createRoot } from 'react-dom/client'

import { Page } from './app.tsx'
import { NO_MARKS } from './highlight.ts'
import type { Vocabulary } from './words.ts'

// Replaced at build time by bundlePage. Nothing in this bundle reaches the filesystem.
declare const __WORDS__: Vocabulary
declare const __DOC__: unknown
// A map cannot be written as JSON, so the marks arrive as the pairs one is built from.
declare const __MARKS__: { marked: readonly [string, string][]; missing: readonly string[] } | undefined

const root = document.getElementById('root')
if (root === null) throw new Error('the page has no #root to draw into')

const marks =
  typeof __MARKS__ === 'undefined'
    ? NO_MARKS
    : { marked: new Map(__MARKS__.marked), missing: __MARKS__.missing }

createRoot(root).render(<Page words={__WORDS__} doc={__DOC__} marks={marks} />)
