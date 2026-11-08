// Mounts the page. The only thing in the bundle that touches the DOM.
//
// Everything it draws comes from the data block in the page itself, read at run time. That is
// what lets one bundle, built once, draw a document it has never seen: see page-data.ts for the
// block and for the two properties this keeps.

import { createRoot } from 'react-dom/client'

import { Page } from './app.tsx'
import { dataFrom, DATA_ID, marksFrom } from './page-data.ts'

const root = document.getElementById('root')
if (root === null) throw new Error('the page has no #root to draw into')

const block = document.getElementById(DATA_ID)
if (block === null) throw new Error(`the page has no #${DATA_ID} element to read its data from`)

const data = dataFrom(block.textContent)

createRoot(root).render(
  <Page words={data.words} doc={data.doc} marks={marksFrom(data.marks)} found={data.found} />,
)
