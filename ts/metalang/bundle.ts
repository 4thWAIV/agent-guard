// The bundler. One module, two shapes of output.
//
// `bundlePage` produces the browser bundle the language page is built from.
//
// `bundleWorkflowScript` produces the shape Claude Code's Workflow runtime demands, which is
// not valid JavaScript: the runtime requires a file that both exports `meta` and ends with a
// top-level `return`, and a file with `export` cannot carry a top-level return. Nothing that
// emits JavaScript can emit it directly, so the meta literal and the return are supplied as a
// banner and a footer around a body that exports nothing.
//
// No workflow script is built here yet. The scripts live in .agents/workflows/ as maintained
// JavaScript with their shared code copied into each one, and moving them is tracked as the
// backlog item 58-workflows-to-typescript. This function is what that move will use, so the
// move is a matter of adding entry points rather than working the build out again.

import type { BuildConfig } from 'bun'

/** What a workflow script's `meta` must be: a plain JSON-shaped object. */
export type WorkflowMeta = Record<string, unknown>

export interface BundleResult {
  readonly code: string
}

/**
 * Anything exported from a built workflow script other than `meta` itself. The runtime rejects
 * the file on the first unexpected `export`, so the build looks for one rather than shipping a
 * script that fails when it is next run.
 */
const UNEXPECTED_EXPORT = /^export (?!const meta\b)/m

/** The banner that puts `meta` on line one and declares the identifier the body assigns to. */
export function workflowBanner(meta: WorkflowMeta): string {
  return `export const meta = ${JSON.stringify(meta, null, 2)}\n\nlet __result\n`
}

/** The footer that turns the body's assignment into the runtime's top-level return. */
export const WORKFLOW_FOOTER = '\nreturn __result'

/**
 * Checks built workflow output for the two things that make the runtime reject it, and returns
 * what is wrong. Separate from the build so it can be exercised on text directly.
 */
export function workflowOutputFaults(code: string): readonly string[] {
  const faults: string[] = []

  if (!code.startsWith('export const meta = ')) {
    faults.push('the built script does not begin with `export const meta`, which the runtime reads first')
  }

  const unexpected = UNEXPECTED_EXPORT.exec(code)
  if (unexpected !== null) {
    const line = code.slice(0, unexpected.index).split('\n').length
    faults.push(
      `the built script exports something other than meta, on line ${line}, and the runtime` +
        ' rejects the file on that export',
    )
  }

  if (!code.trimEnd().endsWith('return __result')) {
    faults.push('the built script does not end with the top-level return the runtime expects')
  }

  return faults
}

const SHARED: Partial<BuildConfig> = {
  format: 'esm',
  minify: false,
  // Bun throws on a failed build by default, which loses the entry point's name. Turning that
  // off hands back the logs instead, so the error below can say which script failed.
  throw: false,
  // Without this every em-dash in the source becomes an escape sequence in the output.
  // The scripts are written in prose and read as prose.
  // (Bun emits UTF-8 by default; stated here so a future change to the default is visible.)
  sourcemap: 'none',
}

/**
 * Builds one workflow script: a TypeScript body that assigns `__result` and exports nothing,
 * wrapped in the banner and footer the runtime requires.
 *
 * A plain `const result = {...}` in the body is dropped by the bundler, because nothing reads
 * it. The body must assign to `__result`, which the banner declares and the footer returns.
 */
export async function bundleWorkflowScript(
  entrypoint: string,
  meta: WorkflowMeta,
): Promise<BundleResult> {
  const built = await Bun.build({
    ...SHARED,
    entrypoints: [entrypoint],
    target: 'node',
    banner: workflowBanner(meta),
    footer: WORKFLOW_FOOTER,
  })

  if (!built.success) {
    throw new Error(`${entrypoint} did not build:\n${built.logs.map(String).join('\n')}`)
  }

  const output = built.outputs[0]
  if (output === undefined) throw new Error(`${entrypoint} built no output`)

  const code = await output.text()
  const faults = workflowOutputFaults(code)
  if (faults.length > 0) throw new Error(`${entrypoint} built wrongly: ${faults.join('; ')}`)

  return { code }
}

/**
 * Builds the browser bundle for the language page. The vocabulary and the document are
 * replaced at build time, so nothing in the bundle reaches the filesystem.
 */
export async function bundlePage(
  entrypoint: string,
  define: Readonly<Record<string, string>>,
): Promise<BundleResult> {
  const built = await Bun.build({
    ...SHARED,
    entrypoints: [entrypoint],
    target: 'browser',
    minify: true,
    define: { 'process.env.NODE_ENV': '"production"', ...define },
  })

  if (!built.success) {
    throw new Error(`${entrypoint} did not build:\n${built.logs.map(String).join('\n')}`)
  }

  const output = built.outputs[0]
  if (output === undefined) throw new Error(`${entrypoint} built no output`)

  return { code: await output.text() }
}
