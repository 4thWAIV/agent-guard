// The guardrails this repository holds over its own TypeScript source.
//
// Three rules, approved as written in `.dev/inprocess/114-kit-page-narration/contract.md` under
// `Rules to add`. None of them is an analyzer: the fence in `analyzers/` is Roslyn's and cannot
// see TypeScript, so each one is a test inside the `bun test` step, following examples.test.ts.
// The contract says it in those words — "Three rules, each a contract-directed test inside the
// existing `bun test` step" — and adds that "None of the three is an analyzer and none carries a
// diagnostic id", so a rule here is named by the test that holds it.
//
// Each rule reads one file at a time, given as a path and the text it holds, so a rule is proven
// on a fixture of a few lines and the repository's own files are more cases to run it on. The
// reading is the TypeScript compiler's own parser — already here as a development dependency,
// which is why nothing is parsed by hand: a word written in a comment, a word inside a longer
// sentence, a word in an import path and a quote inside a regular expression are then not
// mistaken for a literal naming that word.

import { readdirSync, readFileSync } from 'node:fs'
import { join, sep } from 'node:path'

import { describe, expect, test } from 'bun:test'
import * as ts from 'typescript'

import { BOOTSTRAP, ROLES, SELF } from './words.ts'

// ── reading one file ─────────────────────────────────────────────────────────

/** How a rule is given a file: its path from the repository root, and the text it holds. */
interface Source {
  readonly file: string
  readonly text: string
}

/** One thing a rule found: the file and line it is written on, and what is wrong there. */
interface Found {
  readonly file: string
  readonly line: number
  readonly what: string
}

/** A finding as the one line a failing rule prints, so the place and the reason arrive together. */
const reported = ({ file, line, what }: Found): string => `${file}:${line}: ${what}`

const treeOf = ({ file, text }: Source): ts.SourceFile =>
  ts.createSourceFile(
    file,
    text,
    ts.ScriptTarget.Latest,
    true,
    // A `.ts` file read as TSX parses a generic arrow function as markup, so each is read as what
    // it is rather than both as the wider one.
    file.endsWith('.tsx') ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
  )

/** Every node of a file, in the order the file writes them. */
const nodesOf = (tree: ts.SourceFile): readonly ts.Node[] => {
  const all: ts.Node[] = []
  const walk = (node: ts.Node): void => {
    all.push(node)
    ts.forEachChild(node, walk)
  }
  walk(tree)
  return all
}

/** The line a node starts on, counted from one, the way a reader of the file counts. */
const lineOf = (tree: ts.SourceFile, node: ts.Node): number =>
  tree.getLineAndCharacterOfPosition(node.getStart(tree)).line + 1

/**
 * Every named function or constant a node is written inside, nearest first. A component drawn by
 * a helper written inside another component is inside both, which is what lets a rule ask whether
 * a write sits anywhere within its one owning component.
 */
const insideOf = (node: ts.Node): readonly string[] => {
  const names: string[] = []
  let at: ts.Node = node
  while (!ts.isSourceFile(at)) {
    if (ts.isFunctionDeclaration(at) && at.name !== undefined) names.push(at.name.text)
    if (ts.isVariableDeclaration(at) && ts.isIdentifier(at.name)) names.push(at.name.text)
    at = at.parent
  }
  return names
}

/** One literal run of text a file holds: what it says, and the node that writes it. */
interface Literal {
  readonly held: string
  readonly node: ts.StringLiteralLike | ts.TemplateLiteralLikeNode
}

const isLiteral = (node: ts.Node): node is ts.StringLiteralLike | ts.TemplateLiteralLikeNode =>
  ts.isStringLiteralLike(node) ||
  ts.isTemplateHead(node) ||
  ts.isTemplateMiddle(node) ||
  ts.isTemplateTail(node)

/**
 * Every run of text a file writes as a literal: each quoted one, and each piece of a template.
 * The pieces are in here because a word written between two interpolations is written out just
 * as much as a quoted one is.
 */
const literalsIn = (tree: ts.SourceFile): readonly Literal[] =>
  nodesOf(tree)
    .filter(isLiteral)
    .map((node) => ({ held: node.text, node }))

// ── rule 1: no language word by spelling in the drawing path ─────────────────
//
// The contract: "A string literal naming a property or type of a drawn language is banned in
// `ts/metalang/app.tsx` and `ts/metalang/client.tsx`. The only legal route to a language word is
// a named constant in `ts/metalang/words.ts` or a rulebook lookup."
//
// What it stops: a member's identity being taken from the spelling of a column — `member['name']`,
// `columns.includes('name')`, `=== 'name'` — instead of being asked of the rulebook. The spelling
// route passes every document in this tree today and breaks on the first shape that names its
// columns otherwise.

/** The two files that draw a document, which is the path this rule fences. */
const DRAWING_PATH: readonly string[] = ['ts/metalang/app.tsx', 'ts/metalang/client.tsx']

/**
 * Every word this rule bans the spelling of, built from the four constants the contract names as
 * the legal route to one: `ROLES`, `SELF`, `BOOTSTRAP` and `ROOT`. A word the language gains is
 * banned the day it is named in one of them, because the set is read from them rather than listed
 * again here. `ROOT` holds `SELF.object`, so those three cover all four.
 *
 * What the four do not name is not in here, and this rule does not report it: the reserved states
 * a value takes in place of a value (`RESERVED`), and the words words.ts names one at a time
 * (`SCOPE`, `IMPORT`, `LANGUAGE`, `ACCEPTS`, `ROLE`, `VALUE`, `REPLACES`, `OPTIONAL`). So
 * `const OPTIONAL = 'optional'` in app.tsx is outside this rule although words.ts already names
 * that word. Widening the rule to the whole of words.ts is the human's call, not this file's.
 */
const LANGUAGE_WORDS: ReadonlySet<string> = new Set<string>([
  ...Object.values(BOOTSTRAP),
  ...Object.values(SELF),
  ...Object.values(ROLES),
])

const wordsBySpelling = (source: Source): readonly Found[] => {
  const tree = treeOf(source)
  return literalsIn(tree)
    .filter(({ held }) => LANGUAGE_WORDS.has(held))
    .map(({ held, node }) => ({
      file: source.file,
      line: lineOf(tree, node),
      what: `the language word ${JSON.stringify(held)} is written out; reach it through the constant words.ts names it by, or ask the rulebook`,
    }))
}

// ── the two attributes a narrated page is pointed at by ──────────────────────

/** One identity attribute, and the one component of the one file that may write it. */
interface Identity {
  readonly attribute: string
  readonly file: string
  readonly drawnBy: string
}

/**
 * Where each identity attribute is written. `data-declaration` is written by the component that
 * draws a declaration and `data-member` by the `Table` that draws members as rows, which the
 * contract's `Surfaces` section names as the two drawings in app.tsx. Reading app.tsx, those two
 * are `Declaration`, the exported component a document's items are drawn by, and `Table`, the one
 * that writes the `data-col` headers and cells.
 */
const IDENTITIES: readonly Identity[] = [
  { attribute: 'data-declaration', file: 'ts/metalang/app.tsx', drawnBy: 'Declaration' },
  { attribute: 'data-member', file: 'ts/metalang/app.tsx', drawnBy: 'Table' },
]

/** What a file gives an identity attribute where it writes one. */
type Gives = 'a constant' | 'a value worked out' | 'nothing'

/** One place a file writes an identity attribute. */
interface Write {
  readonly of: Identity
  readonly line: number
  readonly gives: Gives
  /** Every named function or constant the write sits inside, nearest first. */
  readonly inside: readonly string[]
}

/**
 * An identity attribute written inside a run of text, which is how anything writing markup as a
 * string rather than as markup would write one — the page shell is written that way. A name
 * inside square brackets is a selector READING the attribute, so the bracket is matched in order
 * to be skipped: a stylesheet and a test naming a target are not places that write one.
 *
 * The attribute names come from `IDENTITIES`, so they are written once in this file.
 */
const IN_TEXT = new RegExp(
  `(\\[\\s*)?(${IDENTITIES.map(({ attribute }) => attribute).join('|')})\\s*=\\s*(?:"([^"]*)"|'([^']*)')?`,
  'g',
)

/**
 * The one call that puts an attribute on a node rather than reading one, which is the third way a
 * file can write one: not as markup, and not as text, but by asking the node to carry it.
 *
 * A call handed its attribute's name through a value worked out at run time cannot be read, so
 * neither rule can tell whether it writes an identity. That is reported as something the rules
 * could not read rather than passed over: a check that cannot be made is never reported as made.
 */
const SETS = 'setAttribute'

/** Whether a call is a node being asked to carry an attribute. */
const setsAnAttribute = (node: ts.CallExpression): boolean => {
  const called = node.expression
  return ts.isPropertyAccessExpression(called) && called.name.text === SETS
}

/** What a markup attribute is given, read from the value written beside it. */
const givenBy = (written: ts.JsxAttribute): Gives => {
  const value = written.initializer
  if (value === undefined) return 'nothing'
  if (ts.isStringLiteral(value)) return 'a constant'
  if (!ts.isJsxExpression(value)) return 'a value worked out'
  const held = value.expression
  return held !== undefined && ts.isStringLiteralLike(held) ? 'a constant' : 'a value worked out'
}

/** What reading one file for identity attributes found: the writes, and what could not be read. */
interface Writes {
  readonly written: readonly Write[]
  readonly unreadable: readonly Found[]
}

/**
 * Every place one file writes an identity attribute — as markup, inside a run of text, or by
 * asking a node to carry one — and every place it writes an attribute these rules cannot read.
 */
const writesIn = (source: Source): Writes => {
  const tree = treeOf(source)
  const written: Write[] = []
  const unreadable: Found[] = []
  for (const node of nodesOf(tree)) {
    if (ts.isJsxAttribute(node)) {
      const named = node.name.getText(tree)
      const of = IDENTITIES.find(({ attribute }) => attribute === named)
      if (of !== undefined) {
        written.push({ of, line: lineOf(tree, node), gives: givenBy(node), inside: insideOf(node) })
      }
    }
    if (ts.isCallExpression(node) && setsAnAttribute(node)) {
      const [named, value] = node.arguments
      if (named === undefined || !ts.isStringLiteralLike(named)) {
        unreadable.push({
          file: source.file,
          line: lineOf(tree, node),
          what: `${SETS} is given a name that is worked out, so neither identity rule can read which attribute this writes`,
        })
      } else {
        const of = IDENTITIES.find(({ attribute }) => attribute === named.text)
        if (of !== undefined) {
          written.push({
            of,
            line: lineOf(tree, node),
            gives:
              value !== undefined && ts.isStringLiteralLike(value)
                ? 'a constant'
                : 'a value worked out',
            inside: insideOf(node),
          })
        }
      }
    }
    if (!isLiteral(node)) continue
    // The source text rather than what it stands for, so a line counts out exactly and a
    // template's `${` is seen where the piece ends.
    const raw = node.getText(tree)
    for (const match of raw.matchAll(IN_TEXT)) {
      const [, bracket, named, quoted, apostrophed] = match
      if (bracket !== undefined || named === undefined) continue
      const of = IDENTITIES.find(({ attribute }) => attribute === named)
      if (of === undefined) {
        throw new Error(`${source.file}: ${named} matched as an identity attribute and is not one`)
      }
      const before = raw.slice(0, match.index)
      written.push({
        of,
        line: lineOf(tree, node) + before.split('\n').length - 1,
        gives:
          quoted === undefined && apostrophed === undefined ? 'a value worked out' : 'a constant',
        inside: insideOf(node),
      })
    }
  }
  return { written, unreadable }
}

// ── rule 2: no literal target in non-test source ─────────────────────────────
//
// The contract: "The strings `IGrantStore` and `IWindowsHelloNativeOps`, any declaration id, any
// member name, and any constant `data-declaration=` or `data-member=` attribute value are banned
// anywhere in non-test `ts/` source."
//
// What it stops: the two intended targets being special-cased so the acceptance passes while the
// general mechanism is absent or broken. No document in this tree declares either id — the
// surface they live on is in the JARVIS document store — so writing one into the source is the
// quickest route to a green check with nothing general behind it.
//
// A declaration id and a member name reach the page as the value of one of the two identity
// attributes, and nowhere else, so a constant written as that value is how any id or member name
// other than the two named ones is caught. That is the whole of what is decidable here: there is
// no list of the ids a document may declare, because a document the kit renders is any document.

/**
 * The two declarations the narration is first pointed at. They are named here, in a test, because
 * a ban has to name what it bans; the rule is that no file outside a test writes them.
 */
const NAMED_TARGETS: readonly string[] = ['IGrantStore', 'IWindowsHelloNativeOps']

const literalTargets = (source: Source): readonly Found[] => {
  const { written, unreadable } = writesIn(source)
  return [
    ...source.text.split('\n').flatMap((line, at) =>
      NAMED_TARGETS.filter((target) => line.includes(target)).map((target) => ({
        file: source.file,
        line: at + 1,
        what: `${target} is a declaration this page draws, written into source that is not a test`,
      })),
    ),
    ...written
      .filter(({ gives }) => gives === 'a constant')
      .map(({ of, line }) => ({
        file: source.file,
        line,
        what: `${of.attribute} is given a constant value; the identity it holds is the drawn document's, so it comes from the document`,
      })),
    ...unreadable,
  ]
}

// ── rule 3: one owning site per identity attribute ───────────────────────────
//
// The contract: "In `ts/metalang`, the JSX attribute `data-declaration` may be written only in
// the component that draws a declaration, and `data-member` only in the component that draws
// member rows."
//
// What it stops: a second, hand-written copy of an identity attribute growing beside the owner
// with a different value derivation, so one selector means two things depending on which
// component drew the node. Narration that rings the wrong node reports nothing, while narration
// that rings no node is reported as missing, so the drifted copy is worse than no attribute.
//
// A write inside a run of text counts as a write, not only markup: the page shell in render.ts is
// written as text, and a second site there would be the same drift by another spelling. A
// selector reading the attribute is not a write, which is what the bracket in `IN_TEXT` is for.
//
// The rule says nothing about whether either attribute is written at all. Drawing them is the
// standard's business and the acceptance tests', not this rule's.

const copiedIdentity = (source: Source): readonly Found[] => {
  const { written, unreadable } = writesIn(source)
  return [
    ...written
      .filter(({ of, inside }) => source.file !== of.file || !inside.includes(of.drawnBy))
      .map(({ of, line }) => ({
        file: source.file,
        line,
        what: `${of.attribute} is written here, and its one owning site is ${of.drawnBy} in ${of.file}`,
      })),
    ...unreadable,
  ]
}

// ── the tree these rules are run on ──────────────────────────────────────────

const root = join(import.meta.dir, '..', '..')

/** One file of this repository, read as a rule is given it. */
const sourceOf = (file: string): Source => ({ file, text: readFileSync(join(root, file), 'utf8') })

/**
 * Every file of the TypeScript side that is not a test, pathed from the repository root with
 * forward slashes whatever this platform separates them with: the rules name the files they own,
 * and on Windows `readdir` answers with backslashes.
 */
const NON_TEST_SOURCE: readonly string[] = readdirSync(join(root, 'ts'), {
  recursive: true,
  encoding: 'utf8',
})
  .map((found) => ['ts', ...found.split(sep)].join('/'))
  .filter((file) => /\.tsx?$/.test(file) && !/\.test\.tsx?$/.test(file))
  .sort()

describe('the tree the rules are run on', () => {
  // A rule that reads no file reports nothing and passes, which would be the quietest way for
  // all three of these to stop working. So the list itself is checked.
  test('every file of the TypeScript side that is not a test is in the list', () => {
    expect(NON_TEST_SOURCE).toContain('ts/metalang/app.tsx')
    expect(NON_TEST_SOURCE).toContain('ts/metalang/client.tsx')
    expect(NON_TEST_SOURCE).toContain('ts/metalang/render.ts')
    expect(NON_TEST_SOURCE).toContain('ts/metalang/propose.ts')
    expect(NON_TEST_SOURCE.length).toBeGreaterThan(10)
  })

  test('a test is not in the list, because a test names what the rules ban', () => {
    expect(NON_TEST_SOURCE.filter((file) => file.includes('.test.'))).toEqual([])
  })

  test('both files of the drawing path are files of the tree', () => {
    for (const file of DRAWING_PATH) expect(NON_TEST_SOURCE).toContain(file)
  })
})

// ── rule 1, proven ───────────────────────────────────────────────────────────

/** Taking a member's identity from the spelling of a column, which is what rule 1 is for. */
const BY_SPELLING: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    "const nameOf = (member: Record<string, unknown>): unknown => member['name']",
    "const named = (columns: readonly string[]): boolean => columns.includes('properties')",
    'const heads = (column: string): boolean => column === `id`',
    'const Keyword = () => <Types keyword="is" />',
  ].join('\n'),
}

/** The same three questions asked of the language instead. */
const BY_THE_LANGUAGE: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    "// A column spelled 'name' in a comment is prose, and `id` in a comment is prose too.",
    "import { BOOTSTRAP, ROLES } from './words.ts'",
    'const nameOf = (bodyOf: BodyOf, of: string): string | undefined =>',
    '  propertyCarrying(bodyOf, of, ROLES.name)',
    'const named = (columns: readonly string[]): boolean => columns.includes(BOOTSTRAP.properties)',
    'const quoted = /["\']/',
    "const path = './words.ts'",
  ].join('\n'),
}

describe('no language word by spelling in the drawing path', () => {
  test('a word of the language written out is caught, wherever it is written out', () => {
    expect(wordsBySpelling(BY_SPELLING).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: the language word "name" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:2: the language word "properties" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:3: the language word "id" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:4: the language word "is" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
    ])
  })

  test('the same questions asked of the language are not caught', () => {
    expect(wordsBySpelling(BY_THE_LANGUAGE).map(reported)).toEqual([])
  })

  test('the drawing path writes no word of the language out', () => {
    expect(DRAWING_PATH.flatMap((file) => wordsBySpelling(sourceOf(file))).map(reported)).toEqual(
      [],
    )
  })
})

// ── rule 2, proven ───────────────────────────────────────────────────────────

/** A target written into the source: by name, as markup, and inside the page shell's text. */
const LITERAL_TARGETS: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    "const first = 'IGrantStore'",
    'const Declaration = () => <section data-declaration="IWindowsHelloNativeOps" />',
    'const Table = () => <tr data-member={"BeginVerification"} />',
    'const shell = (): string => `<section data-declaration="IGrantStore">`',
    "const ring = (node: Element): void => { node.setAttribute('data-member', 'BeginVerification') }",
    "const mark = (node: Element, word: string): void => { node.setAttribute(word, 'first') }",
  ].join('\n'),
}

/** The same four lines with every identity coming from the document being drawn. */
const NO_LITERAL_TARGET: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    'const Declaration = ({ id }: { id: string }) => <section data-declaration={id} />',
    'const Table = ({ name }: { name: string }) => <tr data-member={name} />',
    'const shell = (id: string): string => `<section data-declaration="${id}">`',
    "const style = '.d-row[data-member] { color: red }'",
  ].join('\n'),
}

describe('no literal target in non-test source', () => {
  test('a target written by name, as markup or into the page shell is caught', () => {
    expect(literalTargets(LITERAL_TARGETS).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: IGrantStore is a declaration this page draws, written into source that is not a test',
      'ts/metalang/app.tsx:2: IWindowsHelloNativeOps is a declaration this page draws, written into source that is not a test',
      'ts/metalang/app.tsx:4: IGrantStore is a declaration this page draws, written into source that is not a test',
      "ts/metalang/app.tsx:2: data-declaration is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:3: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:4: data-declaration is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:5: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      'ts/metalang/app.tsx:6: setAttribute is given a name that is worked out, so neither identity rule can read which attribute this writes',
    ])
  })

  test('an identity taken from the document being drawn is not caught', () => {
    expect(literalTargets(NO_LITERAL_TARGET).map(reported)).toEqual([])
  })

  test('no file outside a test writes a target', () => {
    expect(NON_TEST_SOURCE.flatMap((file) => literalTargets(sourceOf(file))).map(reported)).toEqual(
      [],
    )
  })
})

// ── rule 3, proven ───────────────────────────────────────────────────────────

/** A second site writing each identity attribute: one as markup, one into a run of text. */
const COPIED_IDENTITY: readonly Source[] = [
  {
    file: 'ts/metalang/app.tsx',
    text: [
      'const Rungs = ({ name }: { name: string }) => <span data-member={name} />',
      'const Parts = ({ id }: { id: string }) => <span data-declaration={id} />',
    ].join('\n'),
  },
  {
    file: 'ts/metalang/render.ts',
    text: [
      'const pageHtml = (id: string): string => `<section data-declaration="${id}">`',
      "const ring = (node: Element, name: string): void => { node.setAttribute('data-member', name) }",
      "const mark = (node: Element, word: string): void => { node.setAttribute(word, 'first') }",
    ].join('\n'),
  },
]

/** Each identity attribute written at its one owning site, and read anywhere. */
const ONE_OWNING_SITE: readonly Source[] = [
  {
    file: 'ts/metalang/app.tsx',
    text: [
      'const Declaration = ({ id }: { id: string }) => <section data-declaration={id} />',
      'const Table = ({ names }: { names: readonly string[] }) => (',
      '  <tbody>',
      '    {names.map((name) => (',
      '      <tr key={name} data-member={name} />',
      '    ))}',
      '  </tbody>',
      ')',
    ].join('\n'),
  },
  {
    file: 'ts/metalang/propose.ts',
    text: 'const style = (name: string): string => `.d-row[data-member="${name}"]{background:red}`',
  },
]

describe('one owning site per identity attribute', () => {
  test('a second site writing an identity attribute is caught, as markup or as text', () => {
    expect(COPIED_IDENTITY.flatMap(copiedIdentity).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: data-member is written here, and its one owning site is Table in ts/metalang/app.tsx',
      'ts/metalang/app.tsx:2: data-declaration is written here, and its one owning site is Declaration in ts/metalang/app.tsx',
      'ts/metalang/render.ts:1: data-declaration is written here, and its one owning site is Declaration in ts/metalang/app.tsx',
      'ts/metalang/render.ts:2: data-member is written here, and its one owning site is Table in ts/metalang/app.tsx',
      'ts/metalang/render.ts:3: setAttribute is given a name that is worked out, so neither identity rule can read which attribute this writes',
    ])
  })

  test('the owning site writing it, and a selector reading it, are not caught', () => {
    expect(ONE_OWNING_SITE.flatMap(copiedIdentity).map(reported)).toEqual([])
  })

  test('no file outside a test writes an identity attribute away from its owning site', () => {
    expect(
      NON_TEST_SOURCE.flatMap((file) => copiedIdentity(sourceOf(file))).map(reported),
    ).toEqual([])
  })
})
