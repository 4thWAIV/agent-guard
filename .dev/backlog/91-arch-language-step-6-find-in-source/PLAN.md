# Step 6: the word the language gains for finding a form in source

Rewritten 26 October 2026. Step 6 is a language change and nothing else. The mechanism for enforcing
architecture rules in Python and Rust moved to issue 93 on the same day, with its documents in
`.dev/backlog/93-python-and-rust-architecture-rules/`.

## What the step is

`ts/metalang/names.yaml` already holds one entry for each of the six forms in each of the four
languages — twenty-four entries, approved 2026-10-21. Each says what that language calls that form:
in Python an interface is "a Protocol or an ABC", in Rust it is "a trait", in TypeScript a record is
"interface, or a type alias".

Those are names written for a reader. They name a form; they do not give a way to find one. The
sentence step 6 buys is the other half: to find `IGuard` in the C# source, look for
`interface IGuard`.

So the step is one new property on the entries that name a form. Four of the twenty-four are
`process`, which no language has a word for and which carry the reserved absence, so twenty entries
gain it.

## How it is approved

A written proposal page, the same shape as the two Tim approved on 2026-10-25: `map` for step 4 and
`link` for step 5. It states the word, what it is written on, what the checker refuses, and how the
page draws it.

## What this step does not include

Holding a document's claims against real source. Whether that happens, and by what, is not answered
and is not assumed here.

Enforcing architecture rules in Python and Rust. That is issue 93: a pylint checker in Python and a
dylint lint written as a `LateLintPass` in Rust, mirroring the 57 Roslyn analyzers in C# and the
ESLint fence in TypeScript.

## The directions this replaced

Four answers were written and withdrawn between 25 and 26 October 2026, each a mechanism of our own
in place of the one each language already has.

1. A source reader per language, run by a new command beside `bun run check`. Withdrawn 2026-10-25:
   each language already has its own code-analysis system for enforcing rules.
2. A new Roslyn analyzer reading the architecture document, with a flat file format to feed it and
   six new rule ids. Withdrawn 2026-10-26: the same second mechanism in another shape.
3. The answer that Python and Rust needed nothing because this repository holds no source of either.
   Withdrawn 2026-10-26: the question was how Python and Rust checking is done, not whether this
   repository needs it.
4. ast-grep as one tool reading all four languages with rules generated from the document. Withdrawn
   2026-10-26: a tool of our own choosing laid over four languages instead of the normal one each
   language already uses.

The settled answer to what replaced them is in issue 93. Step 6 itself is the language change above.
