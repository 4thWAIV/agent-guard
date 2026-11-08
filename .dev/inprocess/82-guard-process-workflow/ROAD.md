# The architecture language: the road

The architecture language is `ts/metalang/arch.yaml`, a document written in MetaLang. Its job is to let
a project write down what its code is made of, so a person can read it and sign it, and so it can be
checked back against the real source in C#, Python, Rust and TypeScript.

Each step below is one sentence the language gains the power to write. They are in this order because
each needs the one above it. Every remaining step has a GitHub issue, which is the tracker; this file
says what the road is and where it stands.

Last read against the code on 2026-11-08, with all seven steps landed.

| step | the sentence it buys | where | what it needs |
|---|---|---|---|
| 1. Name the kinds | There are six kinds of thing an architecture can hold, and in Python an interface is called a Protocol or an ABC. | **done** | — |
| 2. Name one thing | `IGuard` is an interface. | **done** | — |
| 3. Say what a thing is made of | `IGuard` has a member called `PrecheckAsync`. | **done** | — |
| 4. Say where a thing sits | `IGuard` sits in the `Abstractions` assembly and in no package. | **done** | — |
| 5. Say how two things relate | `FileGuard` implements `IGuard`. | **done** | — |
| 6. Say how to find it in real code | To find `IGuard` in the C# source, search it for an interface and read back the name it caught. | **done** | — |
| 7. Say what must and must not be true | An element whose bytes are laid out has to say what each of its members carries. | mostly, issue 92 | a truth worked out rather than written |

Six steps are in whole and the seventh is in but for one shape, as of 27 October 2026. Every claim
`handoff/guard.yaml` and `ts/metalang/parts.yaml` make about what a thing is was held against the
source it describes, using only what the documents say — the form from the element, the expression
from the naming dictionary. Ten claims, ten found, and nothing matched outside a real declaration.

What step 7 does not reach: a truth worked out rather than written down. `rule` now says the type of
the value it works out, the part it works it out from and the conversion it works it out by, but
nothing is written as worked out and the reader runs no derivation. That is issue 92.

How many of a thing may exist landed on 27 October 2026 as `many`, which counts what a path reaches
and holds the count to a `range`. It is declared in `ts/metalang/metalang.yaml`.

## What is done, and what it is written as

**Step 1.** `ts/metalang/arch.yaml` declares the six forms — interface, class, record, enum, function
and process — each with the glyph it is drawn with and a real example from this repository.
`ts/metalang/names.yaml` is the naming dictionary: what each of the four languages calls each of the
six forms, one entry per pairing, twenty-four in all. No language has a word for a process, and those
four entries carry the reserved state `none`, so a decided absence reads as one.

**Step 2.** `element` is declared in `ts/metalang/arch.yaml`: one named piece of an architecture,
carrying which of the six forms it takes and one line saying what it does. The name it is known by is
the name it is declared under.

Each element is written as its own declaration, using MetaLang's `constant`:

```yaml
  - constant:
      id: IGuard
      is: element
      value:
        form: interface
        does: The three behaviours every guard answers a tool call through.
```

Tim approved that shape on 2026-10-24, together with the two keywords it needed — `constant` gained
`is`, the type its value is of, and `value`, the value itself. Two documents are written this way:
`ts/metalang/parts.yaml`, the six parts of this toolchain, and `handoff/guard.yaml`, the five parts of
the guard.

**The direction this replaced, and why.** Declaring the six forms as the six kinds an architecture
document may write was built, measured and declined on 2026-10-24. It required the architecture
language to re-declare MetaLang's own `declaration` choice and replace its list; a kind named `enum`
took MetaLang's `enum` away from every document written in the language, which left
`ts/metalang/names.yaml` unwritable; and it left the file a part lives in and the glyph it is drawn
with nowhere to live. It also left 115 of the tests failing against 4 for the shape that was taken.
The measurement is in `rebuild/STATE.md`.

**Step 3.** `kind` and `kinds` declare the five kinds a member may be — method, constructor, property,
field and value — each with its mark, a real example and one line saying what it is. `member` is one
named piece of an element, carrying its own `name`, because a member is not declared and so has no id
to be known by. `members` on `element` holds them as a `list`, in the order the source declares them:

```yaml
  - constant:
      id: IGuard
      is: element
      value:
        form: interface
        does: The three behaviours every guard answers a tool call through.
        members:
          - name: PrecheckAsync
            kind: method
            does: Judges a tool call before it runs.
```

`handoff/guard.yaml` names the members of all five parts of the guard, each one read out of the source
before it was written. Tim approved the five kinds on 2026-10-21 and the exact lines on 2026-10-24.

**Step 4.** `places` is the third closed vocabulary of this language, beside the six forms and the
five kinds of member: a package is shipped, an assembly is loaded, a namespace groups names inside
one. Each carries its own mark, a real example, the name it is shown by and one line saying what it
is. `sits` on an element holds one line for each of them, and a level the language has no word for
says `none`:

```yaml
  - constant:
      id: IGuard
      is: element
      value:
        sits:
          package: none
          assembly: AgentGuard.Abstractions
          namespace: AgentGuard.Abstractions.Contracts
        form: interface
        does: The three behaviours every guard answers a tool call through.
```

Writing it keyed by the vocabulary needed one new word in MetaLang, `map`: one of a thing for each
word of a closed set, named under `by`. Every word that set lists is written, a key it does not list
is refused against the words it does, and a word that holds nothing says so with a reserved state.
`role` gained a fourth member, `mark`, so a page reaches the glyph the way it already reaches the
kind, the prose and the name; the glyph came up onto `word` itself and the `appearance` wrapper was
retired. Tim approved all of it on 2026-10-25.

What each language calls each kind of place is not written. He decided the same day to leave it
until a document needs it, because the three words read the same in C#, Python, Rust and TypeScript.

## The eight sentences the road is measured against

Seven are sayable today.

1. **Yes** — There are six kinds of thing an architecture can hold.
2. **Yes** — In Python, an interface is called a Protocol or an ABC.
3. **Yes** — `IGuard` is an interface.
4. **Yes** — `IGuard` has a member called `PrecheckAsync`, and `PrecheckAsync` is a method.
5. **Yes** — `FileGuard` implements `IGuard`, answers with a `Verdict`, and `Verdict` holds a `VerdictKind`.
6. **Yes** — `IGuard` sits in the `Abstractions` assembly, and in no package of its own.
7. No — To find `IGuard` in the C# source, look for `interface IGuard`. *Step 6.*
8. No — Those six kinds are all the kinds there are. *Step 7.*

**Step 5.** `relation` and `relations` declare the three ways one element can stand to another —
implements, answers, holds — each with its mark, a real pair from this repository and one line saying
what it is. `relates` on an element holds one line for each of them, and each line names the elements
it stands to that way:

```yaml
  - constant:
      id: FileGuard
      is: element
      value:
        relates:
          implements: [IGuard]
          answers: [Verdict]
          holds: none
```

It is written with MetaLang's `link`, approved the same day: a value written as the id of a
declaration, with `to` naming the type that declaration must carry a value of. A name nothing the
document reaches declares is refused where it is written, so a relation can never point at nothing,
and the page draws each name as the same reference chip every other reference uses. Many of them is
`list: { of: { link: { to: element } } }`, so a class satisfying two interfaces needs nothing
further.

## Step 6, and what moved off it

Step 6 is a language change and nothing else: `ts/metalang/names.yaml` already says what each of the
four languages calls each of the six forms, and those are names written for a reader rather than a
way to find one. The word the language gains is the other half — to find `IGuard` in the C# source,
look for `interface IGuard` — written on the twenty entries that name a form, through a proposal page
the same shape as `map` and `link`.

Four answers were written and withdrawn between 25 and 26 October 2026, each a mechanism of our own
in place of the one each language already has: a source reader per language; a Roslyn analyzer
reading the architecture document; the claim that Python and Rust needed nothing because this
repository holds no source of either; and ast-grep as one tool over all four languages.

**What replaced them is issue 93, and it is not part of the language work.** Architecture rules are
enforced in Python by a pylint checker and in Rust by a dylint lint written as a `LateLintPass`,
mirroring the 57 Roslyn analyzers in C# and the ESLint fence in TypeScript. The standard, why every
alternative fails, and how each kind of rule we already have maps to both languages are written up in
`.dev/backlog/93-python-and-rust-architecture-rules/`.

Whether a document's claims are held against real source, and by what, is not answered and is not
assumed on this road.

## Smaller items on the list, and what is Tim's

Not started, and not attached to a step: a way for a closed set to claim it is complete. It belongs
with step 7 and is named in issue 92. Saying which part of a thing is its name was the other item
here; it landed on 2026-10-25 as `role`, which a language writes on a property to say what that
property is to a reader — the kind, the prose, the name or the mark.

A role is given to at most one property of a type, and the checker now holds it. This was issue 98,
which is closed. The rule had been stated in MetaLang's prose since the day `role` landed with
nothing to enforce it, and the page drew an error naming both properties where it happened, so the
page said something was wrong while the check passed. `distinct` closed it on 27 October 2026: a new
constraint beside `pattern` and `range` saying that no two members of one list carry the same value
at one part, written on `entity` as `distinct: { in: properties, by: role }`. A declaration is now
held to the constraints its own kind carries, which it was not before. A word for a single bound
spelled "at most one" was proposed the same day and withdrawn the same day; it named one bound,
could say nothing else, and did not fit the constraint system.

Both questions raised by step 3 were answered on 2026-10-25. `members` is now required and accepts
`none` and `pending`, so an element with none says so and one nobody has written yet says that
instead. `form` and `kind` now derive from `word`, which carries the four properties they shared; the
three kinds of place are the third vocabulary to derive from it, written without a third copy.

Everything remaining on the language is put to Tim as one proposal, written in the proposal language
and drawn by its own renderer: `handoff/propose/remaining.yaml`, read as
`handoff/propose/remaining.html`. Each remaining item is written there as a sentence our own
documents already need and currently keep in a comment. The five decisions it carried were all
enacted on 27 October 2026, on Tim's word: who can see a thing, as `reach`; how exactly an element
pins the types of its members, as `exactness`; the built-in types an exact element may use, as
`builtin`; MetaLang's `external` gaining the power to hold a type and not only a conversion; and the
ways of grouping elements across the places they sit, as `grouping`. Nothing in the proposal is open.

What remains beside the proposal is step 7 (issue 92) and the levels a piece of work runs at (issue
97). Step 6 (issue 91), the unenforced role rule (issue 98) and paths that cross a link (issue 87)
are all closed.

A closed set of nine borrowed types called `outside` was proposed on 26 October and withdrawn the
same day. MetaLang's `external` already gives a definition and leaves the implementation out, so the
proposal extends that word rather than adding a parallel set. Two of the seventeen built-in types
named something MetaLang already declares, `bool` beside `boolean` and `uuid` beside `uuid`; both
are out and the set is fifteen. Both are recorded in `DUPLICATION.md` beside this file, which also
records the import walk that existed twice and now does not.

He also flagged that the language must account for the levels a piece of work runs at, said he would
come back to it, and said not to start it. That is issue 97. It was carried in the proposal until 27
October and was lost when the proposal was rewritten; filing it is what keeps it.

How many of a thing may exist was held out by his standing rule until 27 October 2026, when the rule
the guard's own document had to write could not be said without it; it went in that day as `many`.
The rule itself stands and governs MetaLang itself — no language feature is raised until a problem
cannot be solved without it.

## Where this fits

The architecture language is the tool. Issue 82's workflow is the subject: eleven stages, named in
`WorkflowLevel.cs` as Ground, Design, Contract, Rule, Architecture, Test, Implement, Readiness,
Refute, Gate and Report. One is designed and the rest are names, with nothing in its contract
signable because every decision in it is still proposed. The language has to be able to say what a
thing is made of and how two things relate before a rule or a test can be written about anything,
which is why the road comes first.

**Nothing of the workflow is written in the language yet.** `ts/metalang/stage.yaml` is a sample,
written to prove a file other than the language file can be checked, and says so in its own first
line. The documents written in the architecture language are `handoff/guard.yaml` and
`ts/metalang/parts.yaml`, which describe code rather than the process.

## How to check any of this

```
bun run check ts/metalang/<file>.yaml     # one document against its language
bun test                                   # the suite
bun run render ts/metalang/<f>.yaml ts/metalang/<f>.html   # the page
```

Every document in `ts/metalang` checks clean and every page draws with nothing it cannot draw. The
running log of every change, newest last, is `rebuild/STATE.md`.
