// The kit bundle's entry point, and nothing else.
//
// `import.meta.main` is Bun's, and the kit runs under node, so the entry the kit is built from
// cannot use it: it calls the command unconditionally. cli.ts keeps its own `import.meta.main`
// for running the command inside this repository.

import { main } from './cli.ts'

process.exit(await main(process.argv.slice(2)))
