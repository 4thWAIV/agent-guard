// Mounts the page. The only thing in the bundle that touches the DOM.

import { createRoot } from 'react-dom/client'

import { Page } from './app.tsx'
import type { Vocabulary } from './words.ts'

// Replaced at build time by bundlePage. Nothing in this bundle reaches the filesystem.
declare const __WORDS__: Vocabulary
declare const __DOC__: unknown

const root = document.getElementById('root')
if (root === null) throw new Error('the page has no #root to draw into')

createRoot(root).render(<Page words={__WORDS__} doc={__DOC__} />)
