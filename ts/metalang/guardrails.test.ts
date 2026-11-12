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
// Nothing here is skipped, exempted or passed over on a guess. Where a rule cannot read what it
// needs — a call handed an attribute name worked out at run time, a language that has not said
// which property of a member carries its name — it reports that as a finding of its own rather
// than reporting the check as made.

import { readdirSync, readFileSync } from 'node:fs'
import { join, sep } from 'node:path'

import { describe, expect, test } from 'bun:test'
import * as yaml from 'js-yaml'
import * as ts from 'typescript'

import * as rulebook from './rulebook.ts'
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

// ── how a literal is used ────────────────────────────────────────────────────
//
// Rule 1 bans a word of the language in two uses and in no others, which is the boundary Tim set
// on 2026-11-11 answering `rule-1-boundary` with `lookups-and-renames`: "a literal used to look
// something up in the document being drawn or in its rulebook, and a local constant renaming a
// word `words.ts` already names". So the question this asks of a literal is what it is being used
// FOR, never which constant happens to name the same word.
//
// The three kinds of literal the boundary puts outside the rule come out of that question rather
// than out of a list of lines:
//
//   - English prose shown to the reader is outside because prose is in none of these uses. The
//     branches of `n === 1 ? 'property' : 'properties'` are read by a person, not looked up.
//   - A comparison against a JavaScript type is outside because `compared with` asks what the
//     other side of the comparison is, and `typeof value === 'object'` compares with JavaScript's
//     own answer rather than with anything the drawn document says.
//   - A member of the page's own union of finding kinds is outside because the page declares that
//     union itself, which `ownWordsOf` reads from the file being checked.

/** What a literal is being used for, where it is used for one of the things rule 1 bans. */
type Use =
  | 'read as a key of the drawn document'
  | 'asked of a set of the drawn document’s keys'
  | 'compared with a word of the drawn document'
  | 'listed as one of the drawn document’s keys'
  | 'handed to a drawing as the keyword it draws'
  | 'renamed by a constant of this file'

/** The calls that ask a set or a mapping about a word, which is a lookup by that word. */
const ASKED: readonly string[] = ['includes', 'has', 'get', 'indexOf', 'lastIndexOf']

/** The comparisons that ask whether something is that word. */
const COMPARISONS: readonly ts.SyntaxKind[] = [
  ts.SyntaxKind.EqualsEqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsEqualsToken,
  ts.SyntaxKind.EqualsEqualsToken,
  ts.SyntaxKind.ExclamationEqualsToken,
]

/**
 * What a literal is being used for, or nothing where it is used for none of the things rule 1
 * bans. A literal written as one member of a union type is used for nothing here: it declares
 * what a value of this page's own may be, and declaring is not looking up.
 */
const useOf = (node: ts.StringLiteralLike): Use | null => {
  const at = node.parent
  if (ts.isElementAccessExpression(at) && at.argumentExpression === node) {
    return 'read as a key of the drawn document'
  }
  if (
    ts.isCallExpression(at) &&
    at.arguments.includes(node) &&
    ts.isPropertyAccessExpression(at.expression) &&
    ASKED.includes(at.expression.name.text)
  ) {
    return 'asked of a set of the drawn document’s keys'
  }
  if (ts.isBinaryExpression(at) && COMPARISONS.includes(at.operatorToken.kind)) {
    const other = at.left === node ? at.right : at.left
    return ts.isTypeOfExpression(other) ? null : 'compared with a word of the drawn document'
  }
  if (ts.isArrayLiteralExpression(at)) return 'listed as one of the drawn document’s keys'
  if (ts.isJsxAttribute(at)) return 'handed to a drawing as the keyword it draws'
  if (ts.isJsxExpression(at) && ts.isJsxAttribute(at.parent)) {
    return 'handed to a drawing as the keyword it draws'
  }
  if (ts.isVariableDeclaration(at) && at.initializer === node) {
    return 'renamed by a constant of this file'
  }
  return null
}

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

const wordsBySpelling = (source: Source): readonly Found[] => {
  const tree = treeOf(source)
  const own = ownWordsOf(tree)
  const found: Found[] = []
  for (const node of nodesOf(tree)) {
    if (!ts.isStringLiteralLike(node)) continue
    if (!LANGUAGE_WORDS.has(node.text) || own.has(node.text)) continue
    const use = useOf(node)
    if (use === null) continue
    found.push({
      file: source.file,
      line: lineOf(tree, node),
      what: `the language word ${JSON.stringify(node.text)} is written out and ${use}; reach it through the constant words.ts names it by, or ask the rulebook`,
    })
  }
  return found
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

// ── the identities a drawn document declares ─────────────────────────────────
//
// Rule 2 bans "any id declared in a document being drawn, any member name" in non-test `ts/`
// source, and the boundary Tim set on 2026-11-11, answering `rule-2-boundary` with
// `drawn-documents`, says which ids those are: "A declaration id means one declared in a document
// being drawn. It does not mean the architecture language's own declared ids, which are its
// vocabulary". So the set is derived from the documents this repository holds, with every
// language's vocabulary taken out of it.
//
// Three things decide it, each one read from the documents rather than listed here:
//
//   1. A document is a LANGUAGE when some document names its scope under `language`, when its
//      scope is the one every document is read against by default, or when it declares the choice
//      saying what may be written under `declare` — which is how a language says what may be
//      declared in it. arch.yaml is the first, metalang.yaml the second, propose.yaml the third.
//      The boundary names arch.yaml alone; the same sentence's reason, that a language's declared
//      ids "are its vocabulary", holds for every language in the tree, and calling metalang.yaml's
//      own ids a drawn document's identities would say the language is a document drawn against
//      itself.
//   2. Every id a language declares is vocabulary, and so is every word words.ts names. The
//      boundary says so for the second: "words.ts:395-404 states it is the single owner of which
//      property of a type carries which role, and rule 1 governs how the drawing code may reach
//      those words." A word rule 1 governs is not rule 2's to ban, which is also what keeps the
//      owner able to own its own words.
//   3. Everything else is a document being drawn. Its declared ids are identities, and so is the
//      name of each member it lists — read by asking the language which property of a member
//      carries the name role, through `propertyCarrying`, the one owner of that question. Where
//      the language has not said, no member name is checked and the rule reports exactly that.

/** One document of this repository, read as the rules are given it. */
interface Document {
  readonly file: string
  readonly doc: unknown
}

/** The shape one named piece of a drawn declaration is written as, which is what a member is. */
const MEMBER = 'member'

/** What the documents say the source may not write: each identity, and why it is one. */
interface Identities {
  readonly banned: ReadonlyMap<string, string>
  readonly unreadable: readonly Found[]
}

const scopeOf = ({ doc }: Document): string =>
  words.isMapping(doc) && Array.isArray(doc[words.SCOPE])
    ? (doc[words.SCOPE] as readonly unknown[]).join('.')
    : ''

/** The scope every document is read against when it names no language of its own. */
const DEFAULT_RULEBOOK = rulebook.nameOfScope(rulebook.WORDS.scope)

/** Every declaration of every document given, by id, so one lookup reaches all of them. */
const bodiesOf = (documents: readonly Document[]): words.BodyOf => {
  const bodies = new Map<string, words.Mapping>()
  for (const { doc } of documents) {
    for (const [id, body] of words.declaredBodiesOf(doc)) {
      if (!bodies.has(id)) bodies.set(id, body)
    }
  }
  return (id: string) => bodies.get(id)
}

/**
 * Every identity the documents given declare, and everything about them that could not be read.
 * Fixtures are given their own documents, so the rule is proven on a language and a document of a
 * few lines and this repository's six documents are more cases to run it on.
 */
const identitiesIn = (documents: readonly Document[]): Identities => {
  const named = new Set(
    documents
      .map(({ doc }) => (words.isMapping(doc) ? doc[words.LANGUAGE] : undefined))
      .filter((one): one is string => typeof one === 'string'),
  )
  const isALanguage = (one: Document): boolean =>
    named.has(scopeOf(one)) ||
    scopeOf(one) === DEFAULT_RULEBOOK ||
    words.declaredBodiesOf(one.doc).has(words.SELF.declaration)

  const vocabulary = new Set<string>(LANGUAGE_WORDS)
  for (const one of documents) {
    if (!isALanguage(one)) continue
    for (const id of words.declaredBodiesOf(one.doc).keys()) vocabulary.add(id)
  }

  const bodyOf = bodiesOf(documents)
  const unreadable: Found[] = []
  const declaring = documents.find(({ doc }) => words.declaredBodiesOf(doc).has(MEMBER))
  const carrying =
    declaring === undefined ? undefined : words.propertyCarrying(bodyOf, MEMBER, words.ROLES.name)
  if (declaring === undefined) {
    unreadable.push({
      file: documents.map(({ file }) => file).join(', '),
      line: 1,
      what: `no document given declares ${MEMBER}, so no member name could be checked`,
    })
  } else if (carrying === undefined) {
    unreadable.push({
      file: declaring.file,
      line: 1,
      what: `${MEMBER} declares no one property carrying the ${words.ROLES.name} role, so no member name could be checked`,
    })
  }
  const onMember = declaring === undefined ? new Map<string, words.Mapping>() : words.propertiesOf(bodyOf, MEMBER)
  const required = [...onMember.keys()].filter((name) => !words.mayBeLeftOut(bodyOf, MEMBER, name))

  /**
   * Whether a mapping written inside a declaration is one member: every key it writes is a
   * property the member shape declares, and every property that may not be left out is written.
   * A value carrying only a name is some other named thing and not a member of an element.
   */
  const isAMember = (held: words.Mapping): boolean => {
    const keys = Object.keys(held)
    return (
      carrying !== undefined &&
      keys.every((key) => onMember.has(key)) &&
      required.every((name) => held[name] !== undefined)
    )
  }

  const banned = new Map<string, string>()
  for (const one of documents) {
    if (isALanguage(one)) continue
    for (const id of words.declaredBodiesOf(one.doc).keys()) {
      if (!vocabulary.has(id)) banned.set(id, `${one.file} declares it`)
    }
    const walk = (held: unknown): void => {
      if (Array.isArray(held)) {
        for (const each of held as readonly unknown[]) walk(each)
        return
      }
      if (!words.isMapping(held)) return
      if (carrying !== undefined && isAMember(held)) {
        const name = held[carrying]
        if (typeof name === 'string' && !vocabulary.has(name)) {
          banned.set(name, `${one.file} names it as a member`)
        }
      }
      for (const each of Object.values(held)) walk(each)
    }
    walk(one.doc)
  }
  return { banned, unreadable }
}

// ── rule 2: no literal target in non-test source ─────────────────────────────
//
// The contract: "The strings `IGrantStore` and `IWindowsHelloNativeOps`, any id declared in a
// document being drawn, any member name, and any constant `data-declaration=` or `data-member=`
// attribute value are banned anywhere in non-test `ts/` source."
//
// What it stops: the two intended targets being special-cased so the acceptance passes while the
// general mechanism is absent or broken. No document in this tree declares either id — the
// surface they live on is in the JARVIS document store — so writing one into the source is the
// quickest route to a green check with nothing general behind it.
//
// All four parts of the sentence are checked here: the two named strings wherever a line holds
// them, every identity the drawn documents declare wherever a literal says one, a constant given
// to either identity attribute, and every attribute write these rules could not read.

/**
 * The two declarations the narration is first pointed at. They are named here, in a test, because
 * a ban has to name what it bans; the rule is that no file outside a test writes them.
 */
const NAMED_TARGETS: readonly string[] = ['IGrantStore', 'IWindowsHelloNativeOps']

const literalTargets = (source: Source, identities: Identities): readonly Found[] => {
  const { written, unreadable } = writesIn(source)
  const tree = treeOf(source)
  return [
    ...source.text.split('\n').flatMap((line, at) =>
      NAMED_TARGETS.filter((target) => line.includes(target)).map((target) => ({
        file: source.file,
        line: at + 1,
        what: `${target} is a declaration this page draws, written into source that is not a test`,
      })),
    ),
    ...literalsIn(tree).flatMap(({ held, node }) => {
      const why = identities.banned.get(held)
      return why === undefined
        ? []
        : [
            {
              file: source.file,
              line: lineOf(tree, node),
              what: `${JSON.stringify(held)} is an identity of a document this page draws — ${why} — written into source that is not a test`,
            },
          ]
    }),
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

/** Every document this repository holds, which is where rule 2's identities are read from. */
const DOCUMENTS: readonly Document[] = readdirSync(join(root, 'ts', 'metalang'), {
  encoding: 'utf8',
})
  .filter((file) => /\.ya?ml$/.test(file))
  .sort()
  .map((file) => ({
    file: `ts/metalang/${file}`,
    doc: yaml.load(readFileSync(join(root, 'ts', 'metalang', file), 'utf8')),
  }))

const IN_TREE: Identities = identitiesIn(DOCUMENTS)

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

  // Rule 2's identities are read from the documents, so an empty document list would be a rule
  // that bans nothing beyond the two named strings and passes.
  test('every document of the tree is read, and the languages among them are known', () => {
    expect(DOCUMENTS.map(({ file }) => file)).toEqual([
      'ts/metalang/arch.yaml',
      'ts/metalang/metalang.yaml',
      'ts/metalang/names.yaml',
      'ts/metalang/parts.yaml',
      'ts/metalang/propose.yaml',
      'ts/metalang/stage.yaml',
    ])
    // The five arch.yaml ids the boundary names as written out in non-test source today, which
    // are its vocabulary and stay legal.
    for (const id of ['form', 'kind', 'mark', 'member', 'word']) {
      expect(IN_TREE.banned.has(id)).toBe(false)
    }
    // An id a drawn document declares, which is what the rule bans.
    expect(IN_TREE.banned.get('Vocabulary')).toBe('ts/metalang/parts.yaml declares it')
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
    "const DRAWN: readonly string[] = ['options', 'language']",
    "const OPTIONAL = 'optional'",
  ].join('\n'),
}

/** The same questions asked of the language instead, and the three kinds outside the rule. */
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
  ].join('\n'),
}

describe('no language word by spelling in the drawing path', () => {
  test('a word of the language in a lookup, or renamed by a constant, is caught', () => {
    expect(wordsBySpelling(BY_SPELLING).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: the language word "name" is written out and read as a key of the drawn document; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:2: the language word "properties" is written out and asked of a set of the drawn document’s keys; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:3: the language word "id" is written out and compared with a word of the drawn document; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:4: the language word "is" is written out and handed to a drawing as the keyword it draws; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:5: the language word "options" is written out and listed as one of the drawn document’s keys; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:5: the language word "language" is written out and listed as one of the drawn document’s keys; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/app.tsx:6: the language word "optional" is written out and renamed by a constant of this file; reach it through the constant words.ts names it by, or ask the rulebook',
    ])
  })

  test('the language asked, the page’s own union, prose and a JavaScript type are not caught', () => {
    expect(wordsBySpelling(BY_THE_LANGUAGE).map(reported)).toEqual([])
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

/** Nothing the documents say, so a fixture about the named targets alone is about those alone. */
const NO_IDENTITY: Identities = { banned: new Map<string, string>(), unreadable: [] }

/**
 * A language of two declarations: the choice that says what may be declared in it, which is what
 * makes it a language, and the shape a member is, whose name property carries the name role.
 */
const FIXTURE_LANGUAGE: Document = {
  file: 'fixture/language.yaml',
  doc: {
    scope: ['fixture'],
    declare: [
      { choice: { id: words.SELF.declaration, options: ['entity'] } },
      {
        entity: {
          id: MEMBER,
          properties: {
            name: { is: 'string', role: words.ROLES.name },
            does: { is: 'string' },
          },
        },
      },
    ],
  },
}

/** The same language with the name role left off, so it cannot say what a member's name is. */
const FIXTURE_LANGUAGE_SAYING_NOTHING: Document = {
  file: 'fixture/language.yaml',
  doc: {
    scope: ['fixture'],
    declare: [
      { choice: { id: words.SELF.declaration, options: ['entity'] } },
      {
        entity: {
          id: MEMBER,
          properties: { name: { is: 'string' }, does: { is: 'string' } },
        },
      },
    ],
  },
}

/** A document drawn against that language: one declaration, with one member and one other name. */
const FIXTURE_DRAWN: Document = {
  file: 'fixture/drawn.yaml',
  doc: {
    scope: ['drawn'],
    language: 'fixture',
    declare: [
      {
        entity: {
          id: 'IRinger',
          value: {
            members: [{ name: 'BeginRinging', does: 'rings the node' }],
            named: { name: 'not a member, because it writes no does' },
          },
        },
      },
    ],
  },
}

const FIXTURE_IDENTITIES = identitiesIn([FIXTURE_LANGUAGE, FIXTURE_DRAWN])

/** A drawn document's id and member name written into the source instead of read from it. */
const DRAWN_IDENTITY_WRITTEN: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    "const first = 'IRinger'",
    "const beat = 'BeginRinging'",
    "const says = 'the ringer checked IRinger and found nothing'",
    "const style = '.d-row[data-member] { color: red }'",
  ].join('\n'),
}

describe('no literal target in non-test source', () => {
  test('a target written by name, as markup or into the page shell is caught', () => {
    expect(literalTargets(LITERAL_TARGETS, NO_IDENTITY).map(reported)).toEqual([
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
    expect(literalTargets(NO_LITERAL_TARGET, FIXTURE_IDENTITIES).map(reported)).toEqual([])
  })

  test('a declaration id and a member name of a drawn document are read from the documents', () => {
    expect([...FIXTURE_IDENTITIES.banned.entries()].sort()).toEqual([
      ['BeginRinging', 'fixture/drawn.yaml names it as a member'],
      ['IRinger', 'fixture/drawn.yaml declares it'],
    ])
    expect(FIXTURE_IDENTITIES.unreadable.map(reported)).toEqual([])
  })

  test('either of them written into the source is caught', () => {
    expect(literalTargets(DRAWN_IDENTITY_WRITTEN, FIXTURE_IDENTITIES).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: "IRinger" is an identity of a document this page draws — fixture/drawn.yaml declares it — written into source that is not a test',
      'ts/metalang/app.tsx:2: "BeginRinging" is an identity of a document this page draws — fixture/drawn.yaml names it as a member — written into source that is not a test',
    ])
  })

  // A derived identity is matched as the whole of one literal run, so a name inside a sentence is
  // prose and not a target. The two named targets are matched anywhere on a line instead, because
  // they are the acceptance targets and naming them in non-test source at all is what rule 2 bans.
  test('an identity named inside a sentence is prose, not a target', () => {
    const said = { file: 'ts/metalang/app.tsx', text: DRAWN_IDENTITY_WRITTEN.text.split('\n')[2] ?? '' }
    expect(literalTargets(said, FIXTURE_IDENTITIES).map(reported)).toEqual([])
  })

  test('a language that has not said what a member’s name is reports that, and checks none', () => {
    const said = identitiesIn([FIXTURE_LANGUAGE_SAYING_NOTHING, FIXTURE_DRAWN])
    expect(said.unreadable.map(reported)).toEqual([
      'fixture/language.yaml:1: member declares no one property carrying the name role, so no member name could be checked',
    ])
    expect([...said.banned.keys()]).toEqual(['IRinger'])
  })

  // The language has to have said which property of a member carries its name, or no member name
  // is checked. That is reported here rather than passed over, because a check that cannot be
  // made is never reported as made.
  test('the documents say which property of a member carries its name', () => {
    expect(IN_TREE.unreadable.map(reported)).toEqual([])
  })

  test('no file outside a test writes a target', () => {
    expect(
      NON_TEST_SOURCE.flatMap((file) => literalTargets(sourceOf(file), IN_TREE)).map(reported),
    ).toEqual([])
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
  test('writes no word of the language out in either banned use', () => {
    expect(DRAWING_PATH.flatMap((file) => wordsBySpelling(sourceOf(file))).map(reported)).toEqual(
      [],
    )
  })
})
