# The language read against itself

Written 27 October 2026, after Tim said the proposal keeps reintroducing systems the language
already has and asked for the whole language to be read against itself.

Every declaration in `ts/metalang/metalang.yaml`, `ts/metalang/arch.yaml`, `ts/metalang/names.yaml`
and the proposal's scaffold was listed and compared. Six places where something is said twice.
Three are in the proposal and are mine to withdraw. Three are in the language as it stands.

## What MetaLang already has, which the proposal kept missing

**A constraint system, and it is in use.** `constraint` is an abstract type, "What a type says its
values may be". Two constraints derive from it: `pattern`, a regular expression the whole value has
to match, and `range`, a lowest and a highest value. Every entity carries `constraints`, a set of
them. `ts/metalang/arch.yaml` uses it on `path`, and `ts/metalang/propose.yaml` uses it too, so it
is live rather than declared and forgotten.

**A way to say no two the same.** `collection` carries `unique`, "Whether two members may be the
same". `list` and `set` set it to true.

**A way to say how many.** `range`, with `minimum` and `maximum`.

**A way to say a set is closed.** `enum`. Nothing outside the values it lists exists; that is what
an enum is.

## 1. Step 7 recreates the constraint system

**Where.** The decision `say-what-must-and-must-not-be-true` in `handoff/propose/remaining.yaml`,
written 27 October 2026.

**What it says.** That MetaLang's `rule` and `fact` are empty shells and step 7 fills them with
eight new shapes: only when, never, standing, worked out, where it came from, nothing else exists,
no two the same, and how many.

**What it duplicates.** Five of the eight are constraints, and a constraint is already a type
deriving from `constraint`, hung on an entity's `constraints`. Writing them as words on `rule`
builds a second place for the same statement.

- **how many** is `range`, which exists and is used nowhere yet.
- **no two the same** is `unique`, which exists on a collection. What is missing is saying it of a
  type's properties rather than of a collection's members.
- **nothing else exists** is `enum`. A closed set already claims nothing outside it exists.
- **never this** and **only when something else holds** are new constraints, which is one entity
  each deriving from `constraint`, not a word on `rule`.
- **worked out rather than written** is the one job `rule` already names for itself: "A way to
  derive something. A rule derives data, where a fact is data."
- **standing** and **where it came from** are not constraints at all. They are facts about a
  declaration, and they need their own answer rather than being folded in here.

**What has to change.** Step 7 is new constraints deriving from `constraint`, plus `rule` keeping
the one job it already names. The eight-word list goes.

## 2. The built-in types re-encode a number with a range

**Where.** `name-the-built-in-types`, and the fifteen values of `builtins` in
`handoff/remaining/arch_lines.yaml`.

**What it duplicates.** Eleven of the fifteen are a number of a stated width. MetaLang has `number`
and it has `range`, which is a lowest and a highest value. The width of each of the eleven is
written only in its prose description, so nothing can read it. The payoff claimed for `exact` is
that a page draws the element as a byte layout with each member's offset and width; nothing in the
document holds a width, so no page can.

**What has to change.** Each built-in word carries its bounds where something can read them,
through the word MetaLang already has, rather than in a sentence.

## 3. `word.description` duplicates MetaLang's `value.title`

**Where.** `word` in `ts/metalang/arch.yaml`. In the language today, not in the proposal.

**What it duplicates.** A vocabulary word is an enum value. MetaLang's `value` already carries
`title`, "One line saying what the value stands for", and `description`, "Prose explaining the
value". `word` adds a third, `description`, "One line saying what it is", which is `value.title`
said again. Every vocabulary in this repository writes the third and leaves the first two empty,
and the proposal renderer falls through all three in turn looking for a line to draw.

**What has to change.** A word says its line once, in the slot MetaLang already gives it.

## 4. `groupings.layer` duplicates `sits.assembly`

**Where.** `groupings` in `handoff/remaining/arch_lines.yaml`.

**What it duplicates.** The layers written as its example are `abstractions, engine, boundaries,
crossplatform, per-os`. The assemblies the real documents write are `AgentGuard.Abstractions` and
`AgentGuard.Engine`. For every element in this repository the layer is the assembly with the prefix
taken off, so the same fact would be written in two places and the two are free to disagree.

**What has to change.** Either the layer is worked out from the assembly rather than written, or
the grouping drops `layer` and keeps the two that genuinely cut across where a thing sits.

## 5. `groupings.role` collides with MetaLang's `role`

**Where.** The `role` value of `groupings`.

**What it duplicates.** MetaLang declares `role` as an enum — what a property is to a reader: the
kind, the prose, the name or the mark. The grouping `role` means what a type is for. Same word,
two meanings, one level apart. This is the same fault as `bool` beside `boolean` and `uuid` beside
`uuid`, which were taken out of the built-in types earlier the same day.

**What has to change.** The grouping needs its own word.

## 6. `written` is declared as text where the language has a word for a pattern

**Where.** `findable.written` in `handoff/remaining/arch_lines.yaml`, and the `equivalent` change in
the step 6 decision.

**What it duplicates.** It holds `interface <name>`, which is how a declaration is found in source.
MetaLang has `expression`, a regular expression, and `pattern`, the constraint that holds one.
Declared as a bare `string`, nothing checks it and nothing can use it to search.

**What has to change.** Either it is an `expression`, and reuses what `pattern` is built on, or it
is a template with a placeholder and the language has to say that is what it is.

## The common fault

All six are the same mistake: reaching for a new word before reading what is declared. The check
that exists to catch it is a locked law in this repository, and its searchable trees stop at `src`,
`tests`, `analyzers` and `eng`, so it never reaches `ts/`. That gap is issue 96. The habit is mine
and the gap only explains why nothing caught it.

## What the proposal looked like after the audit, and what was still wrong with it

The page still opened by saying the language was missing four closed vocabularies, which was its
shape on 26 October. Seven of its eleven decisions sat outside that framing with nothing introducing
them, and its second section promised a part drawn as it would read and drew none.

Fixed on 27 October. The first section now names five failures rather than two: a part can only be
described in prose; nothing a document says can be held against the source it describes; nothing can
be stated that must be true; and MetaLang states a rule about its own documents that it cannot
write. The second section draws `SignedHeader` written with every word the proposal adds at once —
who can see it, that its bytes are laid out, each member's built-in type, and what it is about and
what its job is — so a reader sees the whole before being asked about the parts.

The scaffold for that drawing derives from the narrower one rather than restating its properties,
which is the same rule the audit applies to everything else.
