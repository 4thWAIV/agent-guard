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
// rather than passed in silence. Where a rule cannot read what it needs — a name worked out at
// run time where a node's attributes are written, or an identity attribute named somewhere the
// reading does not place — it reports that place as a finding of its own rather than reporting
// the check as made. No rule here claims to have read every route; each one reports what it could
// not read.

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
//
// The same holds inside the first kind. It is the page's own union of FINDING KINDS, not any union
// the page declares, so a union counts only where the page says it is one by typing with it the
// property that says which list a thing came from. A union declared for anything else puts nothing
// outside the rule.

/** What puts a literal of a language word outside rule 1, where something does. */
type Outside =
  | 'a member of this page’s own union of finding kinds'
  | 'English prose shown to the reader'
  | 'a comparison against a JavaScript type'

/**
 * The page's own name for the property that says which list a thing said about a node came from.
 * It is what makes a union of text literals the page's finding kinds rather than a union it
 * declares for something else: app.tsx writes it at `Said.from` and at `Finding.from`.
 */
const FROM = 'from'

/** The members of a union of text literals, or nothing where a type is not one. */
const literalMembersOf = (type: ts.TypeNode): readonly string[] | null => {
  const held: string[] = []
  for (const one of ts.isUnionTypeNode(type) ? type.types : [type]) {
    if (!ts.isLiteralTypeNode(one) || !ts.isStringLiteral(one.literal)) return null
    held.push(one.literal.text)
  }
  return held
}

/**
 * Every word a file declares a finding kind to be: the members of each union of text literals
 * that types the property saying which list a thing came from. That is what the boundary puts
 * outside the rule — "a member of the page's own union of finding kinds" — and app.tsx declares
 * two of them, `Said.from` and `Finding.from`, which is why `pending` written anywhere in that
 * file is the page's own word for one of the lists it counts and not the language's word for a
 * reserved state. client.tsx declares none, so nothing in that file is outside the rule on these
 * grounds.
 *
 * A union is one of these only where the file says so by typing a `from` with it, written out
 * there or through a type alias of its own. Where that cannot be established the rule grants
 * nothing and the literal is reported, so a union the page gains for some other purpose never
 * widens this: the exemption follows what the boundary says these unions ARE, and not a list of
 * type names written into the rule.
 */
const findingKindsOf = (tree: ts.SourceFile): ReadonlySet<string> => {
  const all = nodesOf(tree)
  const aliased = new Map<string, readonly string[]>()
  for (const node of all) {
    if (!ts.isTypeAliasDeclaration(node)) continue
    const members = literalMembersOf(node.type)
    if (members !== null) aliased.set(node.name.text, members)
  }
  const kinds = new Set<string>()
  for (const node of all) {
    if (!ts.isPropertySignature(node) && !ts.isPropertyDeclaration(node)) continue
    const typed = node.type
    if (!ts.isIdentifier(node.name) || node.name.text !== FROM || typed === undefined) continue
    const members =
      literalMembersOf(typed) ??
      (ts.isTypeReferenceNode(typed) && ts.isIdentifier(typed.typeName)
        ? (aliased.get(typed.typeName.text) ?? null)
        : null)
    if (members === null) continue
    for (const one of members) kinds.add(one)
  }
  return kinds
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
  const kinds = findingKindsOf(tree)
  const outsideOf = ({ held, node }: Literal): Outside | null => {
    if (kinds.has(held)) return 'a member of this page’s own union of finding kinds'
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
//
// Rules 2 and 3 both turn on one question — where does this file give one of the two identity
// attributes a value? — so the reading is written once, here, and each rule asks it for what it
// bans. Rule 2 bans a value written out; rule 3 bans a write anywhere but the one owning site.
//
// The reading is built from what giving an attribute a value LOOKS LIKE, and not from the ways
// this tree happens to write markup today, because a route nobody listed is exactly how an
// identity written by hand arrives. An attribute has the two names the DOM gives it — the
// attribute, `data-member`, and the property of a node's dataset, `member` — and a name can be
// written as markup, as a property of an object a node is drawn from, as a value handed to a call,
// as the property an assignment writes, or inside a run of text that writes markup or script.
// Each of those is read below, and an object a node is drawn from is read wherever that object is
// written, because these rules read every file of the non-test source rather than the file that
// draws the node.
//
// Nothing here claims to have read every route. Where a name is worked out at run time, or a name
// is written somewhere this reading does not place, the place itself is reported as one the rules
// could not read — so a route nobody thought of arrives as a finding rather than as silence.

/** One identity attribute, and the one component of the one file that may write it. */
interface Identity {
  readonly attribute: string
  /** The same attribute as a node's dataset spells it, which is the DOM's other name for it. */
  readonly onADataset: string
  readonly file: string
  readonly drawnBy: string
}

/**
 * How a `data-` attribute is spelled on a node's dataset: without the `data-`, and with the first
 * letter of every word after the first raised. The DOM gives every data attribute both spellings
 * and writing either one writes the attribute, so a rule reading one name reads both.
 */
const onADatasetAs = (attribute: string): string => {
  const [held, ...rest] = attribute.split('-')
  if (held !== 'data' || rest.length === 0 || rest.some((word) => word === '')) {
    throw new Error(`${attribute} is not a data attribute, so a dataset spells it nothing`)
  }
  return rest
    .map((word, at) => (at === 0 ? word : `${word.slice(0, 1).toUpperCase()}${word.slice(1)}`))
    .join('')
}

/** One identity attribute, under both of the names the DOM gives it. */
const identity = (attribute: string, file: string, drawnBy: string): Identity => ({
  attribute,
  onADataset: onADatasetAs(attribute),
  file,
  drawnBy,
})

/**
 * Where each identity attribute is written. `data-declaration` is written by the component that
 * draws a declaration and `data-member` by the `Table` that draws members as rows, which the
 * contract's `Surfaces` section names as the two drawings in app.tsx. Reading app.tsx, those two
 * are `Declaration`, the exported component a document's items are drawn by, and `Table`, the one
 * that writes the `data-col` headers and cells.
 */
const IDENTITIES: readonly Identity[] = [
  identity('data-declaration', 'ts/metalang/app.tsx', 'Declaration'),
  identity('data-member', 'ts/metalang/app.tsx', 'Table'),
]

/** The identity an attribute name names, where it names one. */
const namedAs = (held: string): Identity | undefined =>
  IDENTITIES.find(({ attribute }) => attribute === held)

/** The identity a dataset property names, where it names one. */
const spelledAs = (held: string): Identity | undefined =>
  IDENTITIES.find(({ onADataset }) => onADataset === held)

/** Both identity attributes, said in one phrase, for a finding that cannot say which it is. */
const EITHER = IDENTITIES.map(({ attribute }) => attribute).join(' or ')

/**
 * What a file gives an identity attribute where it writes one. An attribute written with no value
 * beside it carries an empty one, and an empty value is written out as much as any other, so there
 * is no third answer here: either what the attribute holds was written in this file, or it came
 * from somewhere this file worked out.
 */
type Gives = 'a constant' | 'a value worked out'

/** One place a file writes an identity attribute. */
interface Write {
  readonly of: Identity
  readonly line: number
  readonly gives: Gives
  /** Every named function or constant the write sits inside, nearest first. */
  readonly inside: readonly string[]
}

/**
 * Whether a value is written out where it is given, whichever spelling a choice between values
 * lands on: `ok ? 'one' : 'other'` writes both of them out, and neither of them comes from the
 * document being drawn. A value reached any other way is worked out, which is what an identity
 * taken from the document looks like.
 */
const writtenOut = (value: ts.Expression): boolean => {
  const held = heldBy(value)
  if (ts.isStringLiteralLike(held)) return true
  if (ts.isConditionalExpression(held)) {
    return writtenOut(held.whenTrue) && writtenOut(held.whenFalse)
  }
  if (ts.isBinaryExpression(held) && CHOOSES.includes(held.operatorToken.kind)) {
    return writtenOut(held.left) && writtenOut(held.right)
  }
  return false
}

/** What a value given somewhere gives the attribute it is given to. */
const given = (value: ts.Expression | undefined): Gives =>
  value === undefined || writtenOut(value) ? 'a constant' : 'a value worked out'

/** What a markup attribute is given, read from the value written beside it. */
const givenBy = (written: ts.JsxAttribute): Gives => {
  const value = written.initializer
  if (value === undefined) return 'a constant'
  return ts.isJsxExpression(value) ? given(value.expression) : given(value)
}

/**
 * One of the calls the DOM gives a node to carry an attribute by its name, and where that call
 * writes the name and the value. Each is read as the DOM declares it rather than all of them as
 * the first: `setAttributeNS` names the attribute in its second argument, and `toggleAttribute`
 * carries an empty value rather than one of its own, which is a value written out.
 *
 * A call handed its attribute's name through a value worked out at run time cannot be read, so
 * neither rule can tell whether it writes an identity. That is reported as something the rules
 * could not read rather than passed over: a check that cannot be made is never reported as made.
 */
interface Carries {
  readonly call: string
  /** Which argument names the attribute. */
  readonly names: number
  /** Which argument holds the value, or nothing where the call carries an empty one. */
  readonly gives: number | null
}

const CARRY: readonly Carries[] = [
  { call: 'setAttribute', names: 0, gives: 1 },
  { call: 'setAttributeNS', names: 1, gives: 2 },
  { call: 'toggleAttribute', names: 0, gives: null },
]

/** The name a call is written by, where it is written by one. */
const calledAs = (node: ts.CallExpression): string | null => {
  const called = heldBy(node.expression)
  if (ts.isIdentifier(called)) return called.text
  if (ts.isPropertyAccessExpression(called)) return called.name.text
  return null
}

/** The way of carrying an attribute a call is, where it is one of them. */
const carriedBy = (node: ts.CallExpression): Carries | undefined => {
  const named = calledAs(node)
  return named === null ? undefined : CARRY.find(({ call }) => call === named)
}

/** The call that draws a node from an object of its attributes. */
const DRAWS_A_NODE = 'createElement'

/**
 * Whether an object is drawn into a node where it is written: spread into markup, or handed to the
 * call that draws a node from an object of its attributes.
 *
 * This is what decides whether a name worked out at run time is read. The reading reports a name
 * it knows wherever it cannot read what that name is written onto, and reports a name it cannot
 * read wherever it knows that what is written onto is a node's attributes — a dataset, a call that
 * carries one, or an object drawn into a node. It reports neither for a name it cannot read
 * written onto something it does not know, because that is every record keyed by a word in the
 * tree and says nothing about an identity attribute.
 */
const drawnIntoANode = (held: ts.ObjectLiteralExpression): boolean => {
  let at: ts.Node = held
  while (!ts.isSourceFile(at)) {
    const up: ts.Node = at.parent
    if (ts.isJsxSpreadAttribute(up)) return true
    if (ts.isCallExpression(up)) return calledAs(up) === DRAWS_A_NODE
    const stepped =
      ts.isParenthesizedExpression(up) ||
      ts.isSpreadAssignment(up) ||
      ts.isSpreadElement(up) ||
      (ts.isObjectLiteralExpression(up) && ts.isSpreadAssignment(at)) ||
      (ts.isConditionalExpression(up) && up.condition !== at) ||
      (ts.isBinaryExpression(up) && CHOOSES.includes(up.operatorToken.kind))
    if (!stepped) return false
    at = up
  }
  return false
}

/** The DOM's other name for a node's attributes, which writing a property of writes one. */
const DATASET = 'dataset'

/** Whether what is written onto is a node's dataset. */
const isADataset = (node: ts.Expression): boolean => {
  const held = heldBy(node)
  if (ts.isPropertyAccessExpression(held)) return held.name.text === DATASET
  if (ts.isElementAccessExpression(held)) {
    const named = heldBy(held.argumentExpression)
    return ts.isStringLiteralLike(named) && named.text === DATASET
  }
  return ts.isIdentifier(held) && held.text === DATASET
}

/**
 * JavaScript's own call for writing properties onto a thing, which is how several attributes are
 * written onto a node's dataset at once. The thing written onto is its first argument, so the
 * direction is read rather than guessed: copying out OF a dataset writes no attribute.
 */
const ASSIGNS = { held: 'Object', call: 'assign' }

/** Whether a call writes properties onto the thing it is given first. */
const assignsOnto = (node: ts.CallExpression): boolean => {
  const called = heldBy(node.expression)
  if (!ts.isPropertyAccessExpression(called) || called.name.text !== ASSIGNS.call) return false
  const held = heldBy(called.expression)
  return ts.isIdentifier(held) && held.text === ASSIGNS.held
}

/** The name a property of an object is written under, where it is written out. */
const keyOf = (
  property: ts.ObjectLiteralElementLike,
): Exclude<ts.PropertyName, ts.ComputedPropertyName | ts.PrivateIdentifier> | null => {
  if (!ts.isPropertyAssignment(property) && !ts.isShorthandPropertyAssignment(property)) return null
  return ts.isComputedPropertyName(property.name) || ts.isPrivateIdentifier(property.name)
    ? null
    : property.name
}

/** The property an access writes, where its name is written out rather than worked out. */
const nameAccessed = (
  held: ts.PropertyAccessExpression | ts.ElementAccessExpression,
): ts.Identifier | ts.StringLiteralLike | null => {
  if (ts.isPropertyAccessExpression(held)) {
    return ts.isIdentifier(held.name) ? held.name : null
  }
  const named = heldBy(held.argumentExpression)
  return ts.isStringLiteralLike(named) ? named : null
}

/**
 * Every occurrence of an identity attribute's name inside a run of text, with the bracket that
 * would make it a selector and the value that would make it a write. Text is how anything writing
 * markup as a string rather than as markup writes an attribute — the page shell is written that
 * way — and how a script carried to the browser as text writes one.
 *
 * A name inside square brackets is a selector READING the attribute, so the bracket is matched in
 * order to be skipped: a stylesheet and a test naming a target are not places that write one. A
 * name with a value beside it is a write. A name with neither bracket nor value is a place this
 * reading does not account for, and is weighed against what the file writes rather than passed.
 *
 * The attribute names come from `IDENTITIES`, so they are written once in this file.
 */
const IN_TEXT = new RegExp(
  `(\\[\\s*)?(${IDENTITIES.map(({ attribute }) => attribute).join('|')})(\\s*=\\s*(?:"([^"]*)"|'([^']*)')?)?`,
  'g',
)

/** A property of a dataset written out inside a run of text, and the value it is given. */
const ON_A_DATASET_IN_TEXT = new RegExp(
  `${DATASET}\\s*(?:\\.\\s*([A-Za-z0-9_$]+)|\\[\\s*'([^']*)'\\s*\\]|\\[\\s*"([^"]*)"\\s*\\])\\s*=(?![=>])\\s*(?:'([^']*)'|"([^"]*)")?`,
  'g',
)

/** A property of a dataset written inside a run of text under a name worked out at run time. */
const WORKED_OUT_ON_A_DATASET_IN_TEXT = new RegExp(
  `${DATASET}\\s*\\[\\s*(?!['"])[^\\]]*\\]\\s*=(?![=>])`,
  'g',
)

/** The start of an attribute name, which a run of text ending here goes on to work out. */
const ASSEMBLED = 'data-'

/** What reading one file for identity attributes found: the writes, and what could not be read. */
interface Writes {
  readonly written: readonly Write[]
  readonly unreadable: readonly Found[]
}

/**
 * Every place one file gives an identity attribute a value, and every place it could be giving one
 * that this reading cannot read.
 *
 * The names are read first out of what the file writes as code — markup, a property of an object,
 * a value handed to a call, the property an assignment writes — and then out of every run of text
 * the file holds. A literal the code read accounts for is not weighed again as text, which is what
 * `accounted` carries between the two.
 */
const writesIn = (source: Source): Writes => {
  const tree = treeOf(source)
  const written: Write[] = []
  const unreadable: Found[] = []
  const accounted = new Set<ts.Node>()
  const writes = (of: Identity, node: ts.Node, gives: Gives): void => {
    written.push({ of, line: lineOf(tree, node), gives, inside: insideOf(node) })
  }
  const cannotRead = (line: number, what: string): void => {
    unreadable.push({ file: source.file, line, what })
  }
  const nodes = nodesOf(tree)

  for (const node of nodes) {
    // Markup, which is how the two owning components write theirs.
    if (ts.isJsxAttribute(node)) {
      const of = namedAs(node.name.getText(tree))
      if (of !== undefined) writes(of, node, givenBy(node))
    }

    // A property of an object, which is how an object a node is drawn from names an attribute,
    // whether that object is handed to the call that draws the node or spread into markup.
    if (ts.isPropertyAssignment(node)) {
      const key = node.name
      if (ts.isComputedPropertyName(key)) {
        if (ts.isObjectLiteralExpression(node.parent) && drawnIntoANode(node.parent)) {
          cannotRead(
            lineOf(tree, node),
            `an object a node is drawn from is given a property name worked out at run time, so neither identity rule can read whether the name is ${EITHER}`,
          )
        }
      } else {
        const of = namedAs(key.text)
        if (of !== undefined) {
          accounted.add(key)
          writes(of, node, given(node.initializer))
        }
      }
    }

    // A value handed to a call: the DOM's own way of asking a node to carry an attribute, and any
    // other call handed the name, where the value is what is written beside it.
    if (ts.isCallExpression(node)) {
      const carries = carriedBy(node)
      if (carries !== undefined) {
        const argued = node.arguments[carries.names]
        const named = argued === undefined ? undefined : heldBy(argued)
        if (named === undefined || !ts.isStringLiteralLike(named)) {
          cannotRead(
            lineOf(tree, node),
            `${carries.call} is given a name that is worked out, so neither identity rule can read which attribute this writes`,
          )
        } else {
          accounted.add(named)
          const of = namedAs(named.text)
          if (of !== undefined) {
            // A call carrying no value of its own carries an empty one, which is written out here
            // as much as any other constant.
            const gives =
              carries.gives === null ? 'a constant' : given(node.arguments[carries.gives])
            writes(of, node, gives)
          }
        }
      } else {
        node.arguments.forEach((one, at) => {
          const held = heldBy(one)
          if (!ts.isStringLiteralLike(held)) return
          const of = namedAs(held.text)
          if (of === undefined) return
          accounted.add(held)
          const value = node.arguments[at + 1]
          if (value === undefined) {
            cannotRead(
              lineOf(tree, node),
              `${of.attribute} is named here with no value beside it, so neither identity rule can read what this writes`,
            )
          } else {
            writes(of, node, given(value))
          }
        })
      }
    }

    // Properties written onto a node's dataset all at once, where every name is the dataset's
    // spelling of an attribute. A source this reading cannot read — anything but an object written
    // out here — is reported, because what it is written onto is known to be a node's attributes.
    if (ts.isCallExpression(node) && assignsOnto(node)) {
      const [onto, ...sources] = node.arguments
      if (onto !== undefined && isADataset(onto)) {
        for (const one of sources) {
          const held = heldBy(one)
          if (!ts.isObjectLiteralExpression(held)) {
            cannotRead(
              lineOf(tree, node),
              `properties this reading cannot read are written onto a node's dataset, so neither identity rule can read which attributes this writes`,
            )
            continue
          }
          for (const property of held.properties) {
            const key = keyOf(property)
            if (key === null) {
              cannotRead(
                lineOf(tree, property),
                `a property name this reading cannot read is written onto a node's dataset, so neither identity rule can read which attribute this writes`,
              )
              continue
            }
            const of = spelledAs(key.text)
            if (of === undefined) continue
            writes(
              of,
              property,
              ts.isPropertyAssignment(property) ? given(property.initializer) : 'a value worked out',
            )
          }
        }
      }
    }

    // The property an assignment writes: a node's dataset, which is the DOM's other name for the
    // same attributes, and an attribute written straight onto an object of them.
    if (ts.isBinaryExpression(node) && node.operatorToken.kind === ts.SyntaxKind.EqualsToken) {
      const target = heldBy(node.left)
      if (ts.isPropertyAccessExpression(target) || ts.isElementAccessExpression(target)) {
        const onADataset = isADataset(target.expression)
        const named = nameAccessed(target)
        if (named === null) {
          if (onADataset) {
            cannotRead(
              lineOf(tree, node),
              `a name worked out at run time is written onto a node's dataset, so neither identity rule can read which attribute this writes`,
            )
          }
        } else {
          const byAttribute = namedAs(named.text)
          const onTheDataset = spelledAs(named.text)
          if (byAttribute !== undefined) {
            accounted.add(named)
            writes(byAttribute, node, given(node.right))
          } else if (onTheDataset !== undefined) {
            if (onADataset) writes(onTheDataset, node, given(node.right))
            else {
              cannotRead(
                lineOf(tree, node),
                `${named.text} is written here, and neither identity rule can read whether what it is written onto is a node's dataset, where it would be ${onTheDataset.attribute}`,
              )
            }
          }
        }
      }
    }
  }

  for (const node of nodes) {
    if (!isLiteral(node)) continue
    // The source text rather than what it stands for, so a line counts out exactly and a
    // template's `${` is seen where the piece ends.
    const raw = node.getText(tree)
    const lineAt = (index: number): number =>
      lineOf(tree, node) + raw.slice(0, index).split('\n').length - 1
    let unplaced: Found | null = null
    for (const match of raw.matchAll(IN_TEXT)) {
      const [, bracket, named, value, quoted, apostrophed] = match
      if (named === undefined) continue
      const of = namedAs(named)
      if (of === undefined) {
        throw new Error(`${source.file}: ${named} matched as an identity attribute and is not one`)
      }
      if (bracket !== undefined) continue
      if (value === undefined) {
        unplaced ??= {
          file: source.file,
          line: lineAt(match.index),
          what: `${of.attribute} is named here, in a place neither identity rule can read, so neither can read what this gives it`,
        }
        continue
      }
      written.push({
        of,
        line: lineAt(match.index),
        gives:
          quoted === undefined && apostrophed === undefined ? 'a value worked out' : 'a constant',
        inside: insideOf(node),
      })
    }
    // A name the code read accounts for — an object's property, a call's argument, the property an
    // assignment writes — is not weighed again here.
    if (unplaced !== null && !accounted.has(node)) unreadable.push(unplaced)

    for (const match of raw.matchAll(ON_A_DATASET_IN_TEXT)) {
      const [, dotted, apostrophed, quoted, written1, written2] = match
      const named = dotted ?? apostrophed ?? quoted
      const of = named === undefined ? undefined : spelledAs(named)
      if (of === undefined) continue
      written.push({
        of,
        line: lineAt(match.index),
        gives: written1 === undefined && written2 === undefined ? 'a value worked out' : 'a constant',
        inside: insideOf(node),
      })
    }
    for (const match of raw.matchAll(WORKED_OUT_ON_A_DATASET_IN_TEXT)) {
      cannotRead(
        lineAt(match.index),
        `a name worked out at run time is written onto a node's dataset, so neither identity rule can read which attribute this writes`,
      )
    }
    if (node.text.endsWith(ASSEMBLED)) {
      cannotRead(
        lineOf(tree, node),
        `an attribute name is assembled here out of \`${ASSEMBLED}\` and a value worked out, so neither identity rule can read which attribute this writes`,
      )
    }
  }
  // In the order the file writes them, because the code is read before the text it holds and a
  // reader of a finding reads down the file.
  const byLine = <Held extends { readonly line: number }>(
    one: Held,
    other: Held,
  ): number => one.line - other.line
  return { written: [...written].sort(byLine), unreadable: [...unreadable].sort(byLine) }
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
// This rule reads the same writes rule 2 does, so its reach is that reading's: a second site
// arrives by any route one of them is given a value by, and a place that reading could not read
// is reported here too.
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
 *
 * The union of finding kinds is written the way the page writes one, as the type of the property
 * saying which list a thing came from, because that is what makes it the page's finding kinds
 * rather than a union declared for something else.
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
    'interface Said {',
    '  readonly note: string',
    '  readonly from: From',
    '}',
    "const worst = (said: Said): boolean => said.from === 'pending'",
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

/**
 * A union of text literals that is NOT the page's finding kinds, and the two shapes rule 1's own
 * sentence names read through it: a member read by the spelling of a column, and a column compared
 * against one. These three lines appended to client.tsx left the rule reporting nothing while a
 * member of any union a file declared was outside it, and every literal of them is reported now:
 * the union says nothing about which list a thing came from, so it grants nothing, and client.tsx
 * declares no union of finding kinds at all.
 */
const NOT_FINDING_KINDS: Source = {
  file: 'ts/metalang/client.tsx',
  text: [
    "type Col = 'name' | 'id'",
    "const nameOf = (m: Record<string, unknown>): unknown => m['name']",
    "const heads = (c: Col): boolean => c === 'id'",
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

  // The exemption is for a member of the page's own union of FINDING KINDS. A union declared for
  // anything else grants nothing, which is what these two shapes — the two rule 1's own sentence
  // names — turn on: with a member of any union the file declared outside the rule, all four of
  // these literals were passed in silence.
  test('a union that is not the page’s finding kinds puts nothing outside the rule', () => {
    expect(wordsBySpelling(NOT_FINDING_KINDS).map(reported)).toEqual([
      'ts/metalang/client.tsx:1: the language word "name" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/client.tsx:1: the language word "id" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/client.tsx:2: the language word "name" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
      'ts/metalang/client.tsx:3: the language word "id" is written out; reach it through the constant words.ts names it by, or ask the rulebook',
    ])
    expect(outsideTheRule(NOT_FINDING_KINDS)).toEqual([])
  })

  // The two unions app.tsx declares, read off the live file: `Said.from` and `Finding.from`. They
  // are found by what the boundary says they are — the type of the property saying which list a
  // thing came from — and not by their names, so a union the page gains for something else never
  // widens the exemption. client.tsx declares none.
  test('the exemption comes from the unions that say which list a thing came from', () => {
    expect([...findingKindsOf(treeOf(sourceOf('ts/metalang/app.tsx')))].sort()).toEqual([
      'blocker',
      'highlight',
      'pending',
      'problem',
    ])
    expect([...findingKindsOf(treeOf(sourceOf('ts/metalang/client.tsx')))]).toEqual([])
  })

  // The three kinds are what the compliant fixture is let through BY, so each one is named rather
  // than left to a count: a kind that stopped recognising its own shape would otherwise show up
  // as nothing at all.
  test('each of the three kinds is what lets its own literal through', () => {
    expect(outsideTheRule(BY_THE_LANGUAGE).map(reported)).toEqual([
      'ts/metalang/app.tsx:8: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:13: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:14: "pending" is a member of this page’s own union of finding kinds',
      'ts/metalang/app.tsx:15: "object" is a comparison against a JavaScript type',
      'ts/metalang/app.tsx:16: "property" is English prose shown to the reader',
      'ts/metalang/app.tsx:16: "properties" is English prose shown to the reader',
      'ts/metalang/app.tsx:17: "properties" is English prose shown to the reader',
      'ts/metalang/app.tsx:20: "object" is a comparison against a JavaScript type',
    ])
  })
})

// ── rule 2, proven ───────────────────────────────────────────────────────────

/**
 * A target written into the source by every route a constant identity value arrives by: the name on
 * its own, markup, the page shell's text, a node asked to carry the attribute, an object a node is
 * drawn from — handed to the call that draws it, and built to be spread into markup somewhere else
 * — a node's dataset by either spelling of the property and all at once, and a script written as
 * text. Then the five places a value could be arriving that the reading cannot read: a name worked
 * out at a call that carries one, a name worked out on a dataset, a name worked out on an object a
 * node is drawn from, properties written onto a dataset out of something unreadable, and the
 * attribute named somewhere this reading does not place. Last, an attribute written with no value
 * beside it, which carries an empty one the document had no part in.
 */
const LITERAL_TARGETS: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    "const first = 'IGrantStore'",
    'const Declaration = () => <section data-declaration="IWindowsHelloNativeOps" />',
    'const Table = () => <tr data-member={"BeginVerification"} />',
    'const shell = (): string => `<section data-declaration="IGrantStore">`',
    "const ring = (node: Element): void => { node.setAttribute('data-member', 'BeginVerification') }",
    "const row = () => createElement('tr', { 'data-member': 'BeginVerification' })",
    "const props = { 'data-declaration': 'first' }",
    'const Spread = () => <section {...props} />',
    "const held = (node: HTMLElement): void => { node.dataset.member = 'first' }",
    "const also = (node: HTMLElement): void => { node.dataset['declaration'] = 'first' }",
    "const many = (node: HTMLElement): void => { Object.assign(node.dataset, { member: 'first' }) }",
    "const script = (): string => `<script>row.dataset.member = 'first'</script>`",
    "const mark = (node: Element, word: string): void => { node.setAttribute(word, 'first') }",
    "const worked = (node: HTMLElement, word: string): void => { node.dataset[word] = 'first' }",
    "const keyed = (word: string) => createElement('tr', { [word]: 'first' })",
    "const spread = (node: HTMLElement, held: object): void => { Object.assign(node.dataset, held) }",
    "const named: readonly string[] = ['data-member']",
    'const Bare = () => <tr data-member />',
  ].join('\n'),
}

/**
 * The same routes with every identity coming from the document being drawn, a selector read, and a
 * dataset read out of rather than written onto. A spread carries whatever the object it spreads
 * holds, and that object is read where it is written, which is the line above it here.
 */
const NO_LITERAL_TARGET: Source = {
  file: 'ts/metalang/app.tsx',
  text: [
    'const Declaration = ({ id }: { id: string }) => <section data-declaration={id} />',
    'const Table = ({ name }: { name: string }) => <tr data-member={name} />',
    'const shell = (id: string): string => `<section data-declaration="${id}">`',
    "const style = '.d-row[data-member] { color: red }'",
    "const ring = (node: Element, name: string): void => { node.setAttribute('data-member', name) }",
    "const row = (name: string) => createElement('tr', { 'data-member': name })",
    "const props = (id: string) => ({ 'data-declaration': id })",
    'const Spread = ({ id }: { id: string }) => <section {...props(id)} />',
    'const held = (node: HTMLElement, name: string): void => { node.dataset.member = name }',
    "const many = (node: HTMLElement, name: string): void => { Object.assign(node.dataset, { member: name }) }",
    'const script = (): string => `<script>row.dataset.member = rowName</script>`',
    'const read = (node: HTMLElement): object => Object.assign({}, node.dataset)',
  ].join('\n'),
}

describe('no literal target in non-test source', () => {
  test('a constant identity value is caught by every route it is given one by', () => {
    expect(literalTargets(LITERAL_TARGETS).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: IGrantStore is a declaration this page draws, written into source that is not a test',
      'ts/metalang/app.tsx:2: IWindowsHelloNativeOps is a declaration this page draws, written into source that is not a test',
      'ts/metalang/app.tsx:4: IGrantStore is a declaration this page draws, written into source that is not a test',
      "ts/metalang/app.tsx:2: data-declaration is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:3: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:4: data-declaration is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:5: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:6: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:7: data-declaration is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:9: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:10: data-declaration is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:11: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:12: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      "ts/metalang/app.tsx:18: data-member is given a constant value; the identity it holds is the drawn document's, so it comes from the document",
      'ts/metalang/app.tsx:13: setAttribute is given a name that is worked out, so neither identity rule can read which attribute this writes',
      "ts/metalang/app.tsx:14: a name worked out at run time is written onto a node's dataset, so neither identity rule can read which attribute this writes",
      'ts/metalang/app.tsx:15: an object a node is drawn from is given a property name worked out at run time, so neither identity rule can read whether the name is data-declaration or data-member',
      "ts/metalang/app.tsx:16: properties this reading cannot read are written onto a node's dataset, so neither identity rule can read which attributes this writes",
      'ts/metalang/app.tsx:17: data-member is named here, in a place neither identity rule can read, so neither can read what this gives it',
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

/**
 * A second site writing each identity attribute: as markup, into a run of text, by asking a node
 * to carry one, and onto a node's dataset. The rule reads the same writes rule 2 does, so its
 * reach follows rule 2's and a second site arrives by any of those routes.
 */
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
      'const held = (node: HTMLElement, name: string): void => { node.dataset.member = name }',
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
      'const Table = ({ names }: { names: readonly string[] }) => {',
      '  const mark = (row: HTMLElement, name: string): void => {',
      '    row.dataset.member = name',
      '  }',
      '  return (',
      '    <tbody ref={() => mark}>',
      '      {names.map((name) => (',
      '        <tr key={name} data-member={name} />',
      '      ))}',
      '    </tbody>',
      '  )',
      '}',
    ].join('\n'),
  },
  {
    file: 'ts/metalang/propose.ts',
    text: 'const style = (name: string): string => `.d-row[data-member="${name}"]{background:red}`',
  },
]

describe('one owning site per identity attribute', () => {
  test('a second site writing an identity attribute is caught, by every route it writes by', () => {
    expect(COPIED_IDENTITY.flatMap(copiedIdentity).map(reported)).toEqual([
      'ts/metalang/app.tsx:1: data-member is written here, and its one owning site is Table in ts/metalang/app.tsx',
      'ts/metalang/app.tsx:2: data-declaration is written here, and its one owning site is Declaration in ts/metalang/app.tsx',
      'ts/metalang/render.ts:1: data-declaration is written here, and its one owning site is Declaration in ts/metalang/app.tsx',
      'ts/metalang/render.ts:2: data-member is written here, and its one owning site is Table in ts/metalang/app.tsx',
      'ts/metalang/render.ts:3: data-member is written here, and its one owning site is Table in ts/metalang/app.tsx',
      'ts/metalang/render.ts:4: setAttribute is given a name that is worked out, so neither identity rule can read which attribute this writes',
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
  // The contract, under decision `impact-test`: "It asserts the exemptions by the word and the
  // reason, with no totals and no line numbers, so a tenth exemption or a changed reason still
  // fails it while moving code does not." No total of what the rule weighs and no total of what it
  // flags, because the test below already requires nothing flagged anywhere on the drawing path;
  // no line and no order, so every word is pinned by what it is and never by where it sits; and
  // one entry per literal rather than one per distinct word, so a tenth exemption fails this.
  test('rule 1 leaves app.tsx these words, each to the kind that puts it outside the rule', () => {
    const app = sourceOf('ts/metalang/app.tsx')
    expect(
      outsideTheRule(app)
        .map(({ what }) => what)
        .sort(),
    ).toEqual([
      '"object" is a comparison against a JavaScript type',
      '"pending" is a member of this page’s own union of finding kinds',
      '"pending" is a member of this page’s own union of finding kinds',
      '"pending" is a member of this page’s own union of finding kinds',
      '"pending" is a member of this page’s own union of finding kinds',
      '"pending" is a member of this page’s own union of finding kinds',
      '"pending" is a member of this page’s own union of finding kinds',
      '"properties" is English prose shown to the reader',
      '"property" is English prose shown to the reader',
    ])
  })

  test('writes no word of the language out', () => {
    expect(DRAWING_PATH.flatMap((file) => wordsBySpelling(sourceOf(file))).map(reported)).toEqual(
      [],
    )
  })
})
