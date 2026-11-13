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
//
// Nothing here is skipped, exempted or passed over on a guess. Each rule reports by default and
// subtracts only what the contract puts outside it, so a shape nobody thought of is reported
// rather than passed in silence. Where a rule cannot read what it needs — a call handed an
// attribute name worked out at run time — it reports that as a finding of its own rather than
// reporting the check as made.

import { readdirSync, readFileSync } from 'node:fs'
import { join, sep } from 'node:path'

import { describe, expect, test } from 'bun:test'
import * as ts from 'typescript'

import * as words from './words.ts'

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

/** Every node written inside one node, including itself, in the order the file writes them. */
const nodesOf = (held: ts.Node): readonly ts.Node[] => {
  const all: ts.Node[] = []
  const walk = (node: ts.Node): void => {
    all.push(node)
    ts.forEachChild(node, walk)
  }
  walk(held)
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

// ── every word words.ts names ────────────────────────────────────────────────
//
// The banned word set of rule 1, read from words.ts rather than listed again here, so a word the
// language gains is covered the day words.ts names it. Every string words.ts exports is a word of
// the language, on its own or as a member of one of the constants that group them — BOOTSTRAP,
// SELF, RESERVED and ROLES — and reading the module's exports is how all of them arrive without a
// second copy of any of them living in this file.

/** Every word `ts/metalang/words.ts` names, read from what that file exports. */
const LANGUAGE_WORDS: ReadonlySet<string> = new Set<string>(
  Object.values(words).flatMap((held: unknown): readonly string[] => {
    if (typeof held === 'string') return [held]
    if (held === null || typeof held !== 'object' || Array.isArray(held)) return []
    return Object.values(held).filter((one): one is string => typeof one === 'string')
  }),
)

// ── what the contract puts outside rule 1 ────────────────────────────────────
//
// The contract's count is the rule's reach: "Enforced on every word `words.ts` names, the rule
// flags twenty-four literals in `app.tsx`; fifteen are the defect and nine are these three
// kinds." So every literal of a language word in the two drawing files is reported, and the only
// subtraction is the three kinds the boundary Tim set on 2026-11-11 — answering `rule-1-boundary`
// with `lookups-and-renames` — puts outside the rule: "a member of the page's own union of
// finding kinds, English prose shown to the reader, and a comparison against a JavaScript type".
//
// Each of the three is recognised by what it IS rather than by the positions it has been seen in,
// and a literal none of them recognises is reported. A kind that cannot place a literal therefore
// reports it, which is the safe direction: the rule over-reports rather than passing something
// over in silence. That is the whole of the difference from a closed list of positions, where
// anything the list did not name was passed and reported as checked.

/** What puts a literal of a language word outside rule 1, where something does. */
type Outside =
  | 'a member of this page’s own union of finding kinds'
  | 'English prose shown to the reader'
  | 'a comparison against a JavaScript type'

/**
 * Every word a file declares a value of its own may be: each member of every union of text
 * literals the file writes as a type. `type From = 'highlight' | 'problem' | 'blocker' |
 * 'pending'` in app.tsx is one, which is why `pending` written anywhere in that file is the page's
 * own word for one of the lists it counts and not the language's word for a reserved state.
 */
const ownWordsOf = (tree: ts.SourceFile): ReadonlySet<string> => {
  const own = new Set<string>()
  for (const node of nodesOf(tree)) {
    if (!ts.isUnionTypeNode(node)) continue
    for (const one of node.types) {
      if (ts.isLiteralTypeNode(one) && ts.isStringLiteral(one.literal)) own.add(one.literal.text)
    }
  }
  return own
}

/** The comparisons that ask whether something is that word. */
const COMPARISONS: readonly ts.SyntaxKind[] = [
  ts.SyntaxKind.EqualsEqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsEqualsToken,
  ts.SyntaxKind.EqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsToken,
]

/** A value written in brackets is the value written inside them, so the brackets are stepped over. */
const heldBy = (node: ts.Expression): ts.Expression =>
  ts.isParenthesizedExpression(node) ? heldBy(node.expression) : node

/**
 * Whether a literal is compared with JavaScript's own answer for what a value is. `typeof value
 * === 'object'` compares with that answer and never with anything the drawn document says, which
 * is why the boundary puts it outside the rule. Both spellings of the comparison are read, the
 * literal on either side, and so is `switch (typeof value)`, because a case of that switch is the
 * same comparison written another way.
 */
const comparedWithAJavaScriptType = (node: ts.Node): boolean => {
  const at = node.parent
  if (ts.isBinaryExpression(at) && COMPARISONS.includes(at.operatorToken.kind)) {
    const other = at.left === node ? at.right : at.left
    return ts.isTypeOfExpression(heldBy(other))
  }
  if (ts.isCaseClause(at) && at.expression === node) {
    const switched = at.parent.parent
    return ts.isSwitchStatement(switched) && ts.isTypeOfExpression(heldBy(switched.expression))
  }
  return false
}

/** The operators that choose between the two values written either side of them. */
const CHOOSES: readonly ts.SyntaxKind[] = [
  ts.SyntaxKind.QuestionQuestionToken,
  ts.SyntaxKind.BarBarToken,
]

/** A run of text being assembled, and the node standing for the literal's own value inside it. */
interface InARun {
  readonly run: ts.TemplateExpression | ts.BinaryExpression
  readonly held: ts.Node
}

/**
 * The run of text a literal is written into, or nothing where it is written into none. A choice
 * between values — `?:`, `??`, `||` — is stepped through, because what the run holds is whichever
 * spelling the choice lands on: `n === 1 ? 'property' : 'properties'` is the value of the template
 * span it sits in. A literal that is itself a piece of a template is not written INTO the run, it
 * is the run, so it is not matched here and is reported.
 */
const runAround = (node: ts.Node): InARun | null => {
  let held: ts.Node = node
  while (!ts.isSourceFile(held)) {
    const at: ts.Node = held.parent
    if (ts.isParenthesizedExpression(at)) {
      held = at
      continue
    }
    if (ts.isConditionalExpression(at) && at.condition !== held) {
      held = at
      continue
    }
    if (ts.isBinaryExpression(at) && CHOOSES.includes(at.operatorToken.kind)) {
      held = at
      continue
    }
    if (ts.isTemplateSpan(at) && at.expression === held && ts.isTemplateExpression(at.parent)) {
      return { run: at.parent, held }
    }
    if (ts.isBinaryExpression(at) && at.operatorToken.kind === ts.SyntaxKind.PlusToken) {
      // The outermost joining, so every piece of the phrase is inside the run that is weighed.
      let run: ts.BinaryExpression = at
      while (
        ts.isBinaryExpression(run.parent) &&
        run.parent.operatorToken.kind === ts.SyntaxKind.PlusToken
      ) {
        run = run.parent
      }
      return { run, held }
    }
    return null
  }
  return null
}

/**
 * The text a run of text writes of its own, beside the value weighed: for a template its head and
 * the text between and after its interpolations, and for a joining every literal it joins that is
 * not part of the value weighed.
 */
const ownTextOf = ({ run, held }: InARun): readonly string[] => {
  if (ts.isTemplateExpression(run)) {
    return [run.head.text, ...run.templateSpans.map(({ literal }) => literal.text)]
  }
  const within = new Set<ts.Node>(nodesOf(held))
  return nodesOf(run)
    .filter(isLiteral)
    .filter((one) => !within.has(one))
    .map(({ text }) => text)
}

/**
 * Whether a literal is a word of a phrase rather than a name. A run of text that writes characters
 * of its own beside the word produces that phrase and not the word, so it can look nothing up by
 * that word and there is nobody left to read it but the reader: `${n} ${n === 1 ? 'property' :
 * 'properties'}` is prose. A run that writes nothing of its own produces the word itself, so it is
 * not prose and is reported.
 */
const proseShownToTheReader = (node: ts.Node): boolean => {
  const around = runAround(node)
  return around !== null && ownTextOf(around).some((text) => text !== '')
}

// ── rule 1: no language word by spelling in the drawing path ─────────────────
//
// The contract: "In `ts/metalang/app.tsx` and `ts/metalang/client.tsx`, a word of a drawn language
// may not be written as a literal where it is used to look something up in the document being
// drawn or in its rulebook, and may not be written as a local constant renaming a word
// `ts/metalang/words.ts` already names. The only legal route to a language word in those two uses
// is a named constant in `words.ts` or a rulebook lookup."
//
// What it stops: a member's identity being taken from the spelling of a column — `member['name']`,
// `columns.includes('name')`, `=== 'name'` — instead of being asked of the rulebook. The
// test-only `equivalent` entity at app.test.tsx:1074-1078 declares its columns language first and
// name second and types its name column `string`, so the spelling route passes every document and
// fixture in this tree today and breaks on the first shape that names the column otherwise.

/** The two files that draw a document, which is the path this rule fences. */
const DRAWING_PATH: readonly string[] = ['ts/metalang/app.tsx', 'ts/metalang/client.tsx']

/** One literal of a language word, and what puts it outside the rule where something does. */
interface Weighed {
  readonly file: string
  readonly line: number
  readonly held: string
  readonly outside: Outside | null
}

/** Every literal of a language word one file writes, each weighed against the three kinds. */
const languageWordsIn = (source: Source): readonly Weighed[] => {
  const tree = treeOf(source)
  const own = ownWordsOf(tree)
  const outsideOf = ({ held, node }: Literal): Outside | null => {
    if (own.has(held)) return 'a member of this page’s own union of finding kinds'
    if (comparedWithAJavaScriptType(node)) return 'a comparison against a JavaScript type'
    if (proseShownToTheReader(node)) return 'English prose shown to the reader'
    return null
  }
  return literalsIn(tree)
    .filter(({ held }) => LANGUAGE_WORDS.has(held))
    .map((one) => ({
      file: source.file,
      line: lineOf(tree, one.node),
      held: one.held,
      outside: outsideOf(one),
    }))
}

const wordsBySpelling = (source: Source): readonly Found[] =>
  languageWordsIn(source)
    .filter(({ outside }) => outside === null)
    .map(({ file, line, held }) => ({
      file,
      line,
      what: `the language word ${JSON.stringify(held)} is written out; reach it through the constant words.ts names it by, or ask the rulebook`,
    }))

/** The literals of a language word the three kinds take out of the rule, said the same way. */
const outsideTheRule = (source: Source): readonly Found[] =>
  languageWordsIn(source)
    .filter((one): one is Weighed & { readonly outside: Outside } => one.outside !== null)
    .map(({ file, line, held, outside }) => ({
      file,
      line,
      what: `${JSON.stringify(held)} is ${outside}`,
    }))

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
// The contract: "The strings `IGrantStore` and `IWindowsHelloNativeOps`, and any constant
// `data-declaration=` or `data-member=` attribute value, are banned anywhere in non-test `ts/`
// source."
//
// Those two bans are the whole of the rule. Its third ban, on any id declared in a document being
// drawn and on any member name, was dropped by Tim on 2026-11-12, answering `rule-2-middle-clause`
// with `drop-the-clause`: the contract now says "The rule holds those two bans and nothing wider"
// and that "Rule 1 governs how the drawing code may reach a language word". Nothing is derived
// from the tree's documents here, and no exemption list stands between the two bans and the code.
//
// What it stops: the two intended targets being special-cased so the acceptance passes while the
// general mechanism is absent or broken. No document in this tree declares either id — the
// surface they live on is in the JARVIS document store — so writing one into the source is the
// quickest route to a green check with nothing general behind it.

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

  // Rule 1's banned words are read from words.ts, so an empty set would be a rule that bans
  // nothing and passes. The words checked here are the ones the rule's own findings turn on.
  test('every word words.ts names is in rule 1’s banned set', () => {
    expect(LANGUAGE_WORDS.size).toBeGreaterThan(20)
    for (const word of ['id', 'is', 'properties', 'options', 'language', 'optional', 'object']) {
      expect([...LANGUAGE_WORDS]).toContain(word)
    }
  })
})

// ── rule 1, proven ───────────────────────────────────────────────────────────

/**
 * Taking a member's identity from the spelling of a column, which is what rule 1 is for, and then
 * four more positions: a word handed to a call, a word written as the value of a property, a word
 * returned, and a word wrapped in a template that writes nothing else so the string is still the
 * word. None of the four is a lookup anybody listed, and every one of them is reported, because
 * the rule's reach is every literal of a language word and not a list of positions.
 */
const BY_SPELLING: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    "const nameOf = (member: Record<string, unknown>): unknown => member['name']",
    "const named = (columns: readonly string[]): boolean => columns.includes('properties')",
    'const heads = (column: string): boolean => column === `id`',
    'const Keyword = () => <Types keyword="is" />',
    "const DRAWN: readonly string[] = ['options', 'language']",
    "const OPTIONAL = 'optional'",
    "const drawn = (draw: (word: string) => void): void => draw('kind')",
    "const held = { keyword: 'entity' }",
    "const first = (): string => 'scope'",
    "const wrapped = (member: Record<string, unknown>): unknown => member[`${'prose'}`]",
  ].join('\n'),
}

/**
 * The same questions asked of the language instead, and the three kinds the boundary puts outside
 * the rule: the page's own union of finding kinds, prose shown to the reader — assembled as a
 * template and as a joining — and a comparison against a JavaScript type, written both as a
 * comparison and as the switch that is the same comparison.
 */
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
    "type From = 'highlight' | 'pending'",
    "const worst = (from: From): boolean => from === 'pending'",
    "const COUNTED: readonly (readonly [From, string])[] = [['pending', 'pending value']]",
    "const written = (value: unknown): boolean => typeof value === 'object'",
    "const counted = (n: number): string => `${n} ${n === 1 ? 'property' : 'properties'}`",
    "const joined = (n: number): string => n + ' ' + 'properties'",
    'const kindOf = (value: unknown): string => {',
    '  switch (typeof value) {',
    "    case 'object':",
    "      return 'a structure of its own'",
    '    default:',
    "      return 'one run of text'",
    '  }',
    '}',
  ].join('\n'),
}

describe('no language word by spelling in the drawing path', () => {
  test('every literal of a language word is caught, whatever position it is written in', () => {
    expect(wordsBySpelling(BY_SPELLING).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: the language word "name" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:2: the language word "properties" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:3: the language word "id" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:4: the language word "is" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:5: the language word "options" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:5: the language word "language" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:6: the language word "optional" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:7: the language word "kind" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:8: the language word "entity" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:9: the language word "scope" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:10: the language word "prose" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
    ])
  })

  test('the language asked, the page’s own union, prose and a JavaScript type are not caught', () => {
    expect(wordsBySpelling(BY_THE_LANGUAGE).map(reported)).toEqual([])
  })

  // The three kinds are what the compliant fixture is let through BY, so each one is named rather
  // than left to a count: a kind that stopped recognising its own shape would otherwise show up
  // as nothing at all.
  test('each of the three kinds is what lets its own literal through', () => {
    expect(outsideTheRule(BY_THE_LANGUAGE).map(reported)).toEqual([
      'ts/metalang/app.tsx:8: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:9: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:10: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:11: "object" is a comparison against a JavaScript type',
      'ts/metalang/app.tsx:12: "property" is English prose shown to the reader',
      'ts/metalang/app.tsx:12: "properties" is English prose shown to the reader',
      'ts/metalang/app.tsx:13: "properties" is English prose shown to the reader',
      'ts/metalang/app.tsx:16: "object" is a comparison against a JavaScript type',
    ])
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

/** The same lines with every identity coming from the document being drawn, and a selector read. */
const NO_LITERAL_TARGET: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    'const Declaration = ({ id }: { id: string }) => <section data-declaration={id} />',
    'const Table = ({ name }: { name: string }) => <tr data-member={name} />',
    'const shell = (id: string): string => `<section data-declaration="${id}">`',
    "const style = '.d-row[data-member] { color: red }'",
    "const ring = (node: Element, name: string): void => { node.setAttribute('data-member', name) }",
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

// ── the drawing path, run against rule 1 ─────────────────────────────────────
//
// Last, because this is the rule's impact on existing code rather than proof that the rule works:
// the fifteen literals the contract records under `Rules to add` rule 1 as what IMPLEMENT clears.

describe('the drawing path', () => {
  // The contract's count, which is what says the rule's reach is its sentence and not a list of
  // positions: "the rule flags twenty-four literals in `app.tsx`; fifteen are the defect and nine
  // are these three kinds."
  test('rule 1 weighs all twenty-four literals of app.tsx and leaves nine to the three kinds', () => {
    const app = sourceOf('ts/metalang/app.tsx')
    expect(languageWordsIn(app).length).toBe(24)
    expect(wordsBySpelling(app).length).toBe(15)
    expect(outsideTheRule(app).map(reported)).toEqual([
      'ts/metalang/app.tsx:85: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:96: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:107: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:115: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:150: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:914: "object" is a comparison against a JavaScript type',
      'ts/metalang/app.tsx:1415: "property" is English prose shown to the reader',
      'ts/metalang/app.tsx:1415: "properties" is English prose shown to the reader',
      'ts/metalang/app.tsx:1603: "pending" is a member of this page’s own union of finding kinds',
    ])
  })

  test('writes no word of the language out', () => {
    expect(DRAWING_PATH.flatMap((file) => wordsBySpelling(sourceOf(file))).map(reported)).toEqual(
      [],
    )
  })
})
