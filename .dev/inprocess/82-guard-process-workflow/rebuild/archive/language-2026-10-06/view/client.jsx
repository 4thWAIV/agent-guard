// The app. Mounts in the browser and draws one document; nothing is pre-rendered.
import { createRoot } from 'react-dom/client'
import { useState } from 'react'
import { Draw, DefsContext } from './ui.jsx'
import { DOCUMENTS } from './page.jsx'

const App = () => {
  const build = DOCUMENTS[__DOC__]
  const { doc, definitions } = build()
  const [open, setOpen] = useState(null)
  const byName = Object.fromEntries(definitions.map((d) => [d.name, d.node]))
  return (
    <DefsContext.Provider value={{ byName, open, setOpen }}>
      <Draw node={doc} />
    </DefsContext.Provider>
  )
}

createRoot(document.getElementById('root')).render(<App />)
