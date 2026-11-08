# EvoLang: the destination, and the four questions that are still open

EvoLang is the Evolutionary Language. Tim described it on 2026-10-08 and nothing of it has been approved or written. This file is the destination as the record holds it, with his own words quoted where the record has them, and with every open question named as open.

Everything in this file is recorded intent. No word of EvoLang exists in any language file.

## What it is meant to be, in his words

From `rebuild/STATE.md:6319`, his own description of 2026-10-08, transcribed with his spelling:

> "It defines things as it would define a grammer. It defines verbs and nowns. It has some 'special devices', like the ability to describe a prepositional equation or any set/predicate/lambda evaluation... it hast the devices to describe it's own self before a message by definign the vocabulary and the parts of speach and then putting the words together."

So: a language that declares nouns and verbs, carries devices for sets, predicates and lambda evaluation, and declares its own vocabulary at the head of a message before using it.

In standard terms, that is a **controlled natural language with an inline vocabulary preamble**. The prior art named on the same day, at `rebuild/STATE.md:3396`:

- **SBVR**, the OMG's Semantics of Business Vocabulary and Rules — declare a vocabulary of noun concepts, verb concepts and fact types, then state rules over it in structured English with a defined mapping to logic. The record calls it a direct match, and also notes it is famously hard to implement.
- **Attempto Controlled English** — a subset of English with a formal semantics that translates to first-order logic. The working proof that a sentence a human reads can be a predicate a machine evaluates.
- **Categorial grammar, and CCG** — each word carries a type and composition is type application. The formal version of parts of speech, then putting the words together.
- **Avro's object container format** — the preamble idea exactly: the schema is written in the file header, then the records, so a reader that has never seen the schema can read the data.

The reason it is wanted over an expression language, from `rebuild/STATE.md:3419`: CEL, the Common Expression Language, was chosen for predicates and rejected for contracts, because a human signs an expression with arrows in it and an agent could write a condition nobody can audit. A controlled natural language passes that test — the condition reads as a sentence and is still a predicate.

The cost is recorded as honest and large: a controlled natural language is far more work, and ambiguity is the permanent risk.

## The division of labour, which is settled

From `rebuild/STATE.md:6463`:

```
MetaLang   SHAPE      what things exist, what parts they have, what values are legal.
                      Answers: is this document well formed?
EvoLang    MEANING    what the data says and what follows from it. Verbs, propositions,
                      derivation, inference. Answers: what follows, and is this true?
```

And the sentence the whole effort is driving at, `rebuild/STATE.md:6525`: "MetaLang defines EvoLang, EvoLang carries the rules, and the rules are data."

By that line MetaLang was judged complete for its own job, audited against Ecore's concrete metaclasses. What Ecore had left over was operations and parameters — behaviour — and behaviour is EvoLang's half.

## The five gaps between here and there

Recorded 2026-10-11 at `rebuild/STATE.md:6325`, in dependency order. The first is closed; the other four are EvoLang's.

**A. The instance layer** — writing and checking a document in a defined language. This was the blocker for everything downstream, because a contract, a rule, a test and a plan are all instances. It is closed: documents written in the architecture language are checked today.

**B. Set construction.** Tim settled six ways to define a collection — by listing, by restriction, by union, by intersection, by difference, by product. None of the five derivations exist.

**C. Expressions over more than one value.** A statement relating two things cannot be written. The record gives his own example of such a rule: "this phase's exit criteria include everything it produces".

**D. Functions with parameters.** A verb is a function: it takes a subject and an object and yields a statement. With no parameter and no expression, a verb cannot be declared.

**E. Parts of speech and statements.** D, plus an ordering convention saying which parameter is the subject and which the object. Nouns are entities, which exist; verbs are D, which does not.

## Datalog is the intended evaluator

From `rebuild/STATE.md:6477`: a Datalog rule derives a relation, which is a set, so all six of the collection operations Tim settled fall out of one mechanism — facts for listing, a body condition for restriction, two rules with one head for union, two conditions for intersection, negation for difference, two independent variables for product. Recursion comes with it, which none of the six give on their own.

The recorded conclusion: building the six operations and then Datalog would be building the same thing twice, so do Datalog, because the set operations are what it is made of. The order written down was to define Datalog in MetaLang first, as the real test of whether MetaLang can define EvoLang, and write the interpreter after, in TypeScript first and C# later.

Nothing of this has been built.

## Why Grammatical Framework was not adopted

Recorded 2026-10-09 at `rebuild/STATE.md:4239`. The relevant finding, at `rebuild/STATE.md:4265`, quoted in full because the clause in the middle of it matters: "EvoLang's abstract syntax IS a MetaLang model. A category is a definition. A function is a definition with parameters and a result. Nothing new is needed for that half." Read with `rebuild/STATE.md:6472` — "What Ecore has left over is EOperation and EParameter — behaviour — and behaviour is EvoLang's half" — the record treats a parameter as EvoLang's job rather than as something missing from MetaLang. That is the reading `02-controls-against-the-language.md` reports as a gap, and the two have never been put side by side. Which of them governs is part of the first open question below. And at `rebuild/STATE.md:4293`: the preamble is impossible with a compiled grammar and free with an interpreted one. A compiled lexicon cannot accept a vocabulary declared in the message it is about to read, which is the one thing EvoLang must do.

## The two rulings that bound what EvoLang may ever say

Both are Tim's, both from 2026-10-10, and both are recorded as rulings rather than as built work.

**Turing completeness** — `rebuild/STATE.md:4299`:

> the language definition system used to define, validate and create the deterministic language is not Turing complete. A language it defines may nevertheless describe a Turing-complete operation, such as defining a function in a contract.

The reasoning recorded with it: Turing completeness belongs to an evaluator, not to a notation. A tree describing a function is data; the checker reads it, validates its shape and derives facts about it, and never runs it. So the termination guarantee survives whatever is described.

The consequence, which the record calls the rule that decides how every code-facing gate gets written:

```
over the structure      "this function does not call X"      derivable from the tree
over an observed result "this test passed"                   a fact fed in from a run
over the behaviour      "this function is correct"           NOT AVAILABLE, EVER
```

**Scope** — `rebuild/STATE.md:4343`:

> the language must only be able to provide the analyzer rules, the tests, and the implementation plan for the agents to execute. The rules provide the validation. The language exists to allow quick construction of how we communicate a set of tests.

What that settles, as recorded: code structure is validated by generated analyzer rules running in the real build, behaviour is validated by the tests running in the real test pass, and the language itself validates nothing about code. It emits the three documents and checks their internal consistency.

## The cooperative purpose, recorded nine days later

From `rebuild/STATE.md:13911`, recovered on 2026-10-19. Tim described a cooperative: an instance of the assistant running on each member's own computer, members sharing each other's instances across their accounts, with something like a Kafka bus between them, so that one session can send a request to another session to do work on its behalf. EvoLang is the language those requests are to be written in.

The entry labels itself plainly: "This is a purpose, not a design. It does not add or change a word of any language, and nothing about it is approved."

**This was resolved on 2026-11-06, and the resolution is that both stand.** EvoLang exists to describe an obligation about work: what must be true, who must make it so, under what condition, and how anyone would know it happened. Tim approved that on 2026-11-06 and said it is the entire purpose. A test is a claim that must hold about a piece of work; a request to another session is a claim about work to be done, with a condition on it and a party answerable for it. Both are the general form's named relation over named parts with the five facets filled differently, so the 2026-10-10 ruling and the 2026-10-19 purpose are one subject at two scales and neither is withdrawn. Because it is the entire purpose it is also a bound: EvoLang is not a general language for saying true things, and a proposed word earns its place against the test of whether it says something about work.

## The general form of a statement

Read off every plan document in the issue folder on 2026-10-18, recorded in `rebuild/GENERAL-FORM.md`, which says of itself "Nothing here is approved."

**A statement is a named relation over named parts, carrying five facets.** The relation and the parts are the subject of that sentence; the five facets are truth, condition, quantity, polarity and modality.

```
relation        what is being said: parent, runs-at, is-called-in, lives-in, produces
parts           the things it relates, each in a named role
truth           written down, computed from named operands by a named party, or
                neither — a slot declared and left to be filled
condition       always, or only when something else holds
quantity        one, none, every, exactly n, at least one, one per member of a set
polarity        affirms or denies
modality        what is, what must be, what may be, or what must not be
```

**A statement may be the subject of another statement.** Three of these recur in every document read, and four more appear in some:

```
standing        proposed, approved, locked, withdrawn, not yet decided
provenance      the source it was read from, who wrote it, the date
ground          the reason for it, or the statement it follows from
absence         what it means that this statement is not written
order           whether the order of a set of statements carries meaning
contribution    whether this statement counts toward a stated total
entailment      that one statement does not license a conclusion from it
```

The measured frequency across the corpus, which is the order the work falls into:

| shape | roughly |
|---|---|
| a thing written down with named parts | thousands |
| a part whose value names another declaration, and the set it is looked up in | 250 |
| a value indexed by a dimension — one per language, one per level | 190 |
| quantity | 100 |
| condition | 80 |
| polarity, as a prohibition | 80 |
| what an absence means | 40 |
| standing | 40 |
| a truth that is computed rather than written | 40 |
| provenance | 15 |

Of these, the first three and standing, condition, polarity and quantity-as-a-count are now writable in MetaLang: named parts, `link`, `map`, `standing`, `when`, `never` and `many` all landed on or before 27 October 2026. Two are not closed. A computed truth has nothing behind it — `rule` says what one would look like and nothing runs one. And provenance is only part written: the general form defines it as three things, the source it was read from, who wrote it and the date, and `origin` at `ts/metalang/metalang.yaml:1068` is one optional string titled "Where it was read from — the document, the conversation or the person it came from." One free-text string is not three named parts.

Three findings recorded with the table, each bearing on a decision already taken:

- **A value indexed by a dimension is one shape, not two.** The per-language table and the per-level table are the same statement shape: a value that varies along a named dimension. Together they are the second largest body of content in the corpus. `map` is the word that landed for it.
- **There are three absences and they are confused everywhere.** Not written and correct, so a default applies; not written and wrong; not written because nobody has decided. MetaLang has `optional` for the first and `pending` for the third. The second still has no word, and `none` is not it — `none` is a fourth category the split does not contain, a decided emptiness rather than any kind of mistake.
- **An order that carries meaning has to say so.** Lists of both kinds exist in the documents and the distinction survives only in comments.

What fitted no shape, and is therefore where the next finding is: a claim that a set is complete so nothing outside it exists; a claim that one place is the only place something is held; a statement withdrawn by a later statement in the same file; a comparison of two things on a scale that is never named; and a statement whose subject is a name rather than a thing, saying the name refers to nothing.

## The eight controls against the general form

Tim asked for practical examples for the eight. What follows is deliberately not a set of proposed words, because minting one would break the standing rule that no language feature is raised until a problem cannot be solved without it. Instead each control is placed against the general form above, and against what a document can write today.

Two things in the general form are worth holding in mind while reading this, because they are not Tim's. The record notes at `handoff/evolang-reply.md:32` that two devices appeared in a sketch shown back to him and are not part of his description: a verb carrying named roles that say what fills them, and a quantifier on a statement. He has answered neither. The general form's "each in a named role" and its quantity facet are those two devices, so every row below rests on something unanswered.

**What can be written today, for all eight.** A document may declare its own entity and hang it on the language's own types, which `handoff/guard.yaml:14` already does. So any of the eight can be written as a property of a declared type right now, checked, and drawn — the probe in `02-controls-against-the-language.md` writes a method's four parameter types and its return type that way. The cost is that the document has added words, and the words are Tim's to approve. Nothing below needs a change to MetaLang.

**What cannot be written today, for any of them**, and these are the four gaps of 2026-10-11 rather than anything specific to the eight: a set defined by anything other than listing it; a statement relating two things, which is what "this phase's exit criteria include everything it produces" needs and is Tim's own example; a function with parameters as a declared thing rather than a list written down; and parts of speech.

Taken one at a time:

- **Inputs and outputs** are a relation over two parts with quantity, condition and polarity on them — one step may take several things, an input may be required only when something else holds, and a step may be forbidden a thing. `produces` is the only one of the eight whose relation name is in the source, at `rebuild/GENERAL-FORM.md:44`. The architecture language's `answers` is the nearest built thing and it sits on the element rather than the member.
- **Dependencies** are the same shape over two steps, or a step and a thing. Three relations are built and all three are element to element.
- **Events** are a relation whose facet is modality — whether the thing must happen, may happen, or must not. Modality is the one facet of the five with nothing built behind it in any form.
- **Attention** is not a statement but a statement about a statement: the standing facet with a party attached. Standing is built, in both MetaLang and the proposal language. The party, the moment and what is shown are not, and the design for them is `06-attention-the-one-control-with-a-design.md`.
- **Workflows and steps** are an ordered set, and order is a facet the general form says has to be declared rather than assumed — `rebuild/GENERAL-FORM.md:104` records that lists of both kinds exist in these documents and the distinction survives only in comments. MetaLang does distinguish them: a `list` and an `array` differ by whether order carries meaning.
- **State changes** are a relation over a thing, a state and a next state, with condition for what permits the move and polarity for what forbids it. The one statement of this kind the language makes is about itself: nothing takes the place of a locked declaration by being approved, written as a `when` holding a `never`.

## The two blockers of 2026-10-08, read against the language as it stands

On 2026-10-08 the record named exactly two things EvoLang needed that MetaLang could not say. Read against the working tree today, both appear to be closed. **No document records this, so it is a reading and not a decision, and it is Tim's to confirm.**

**A four-way union, for a filler in a statement.** A filler is a noun, a literal, a nested statement or a quantified set, and the language of the time held exactly one device per part. MetaLang now declares `choice` at `ts/metalang/metalang.yaml:1150`: "A type that is one of several types." Its description adds the condition that comes with it — "Untagged, so the types it is one of have to be distinguishable from one another" — so a four-way union is writable as long as the four can be told apart without a tag. On 2026-10-15 Tim renamed the old `choice`, which was an enumeration of values, to `enum`, and gave the name `choice` to the union of types.

**A reference that resolves against what the message itself declared**, which is the preamble. The device sketched for it in October was called `within`, and it had three scopes: among the instances written on the held thing itself, among every instance anywhere in the file, and in another document. The second of those is the one the record says the preamble needs. The word `within` does not exist in MetaLang; the string appears nowhere in the file.

What does exist is `link`, approved 2026-10-25 and declared at `ts/metalang/metalang.yaml:887`. Its own description carries both halves, and the second half is the one that decides this: "It is written as the id of a declaration, and the one it names has to carry a value of the type written under `to`. A name nothing the document reaches declares is reported, so a thing written down here can never point at nothing."

So the scope is file-wide, which is what the preamble needs, but what a link may name is not. The declaration it names has to carry a value of its own. A probe against the real checker shows it: a document that declares an entity and then writes that entity's name where a link belongs is refused with `"signature" carries no value of its own, so it is not an element`. A link names an instance. It cannot name a word.

A second, narrower reference also landed with it: a bare value on a property typed by an enum names one member of that enum. `ts/metalang/check.ts:802` states the rule — "An enum is a closed set of values. A value of one is written as the id naming a member, or written out in full as one of the values listed." That one resolves inside the enum alone and is not the file-wide scope; it is the narrowest of `within`'s three. It matters because it is how a closed vocabulary is named, which is the other half of a preamble: declare the words, then use them by name.

The 2026-10-19 entry that recorded the preamble gap said of the enum case, at `rebuild/STATE.md:13991`: "That is the same scoped reference recorded on 2026-10-08 as `within` with document scope, which is also what the EvoLang preamble needs. One word serves both." That word landed on 2026-10-25.

**Where that leaves the two blockers.** The first is closed. The second is half closed: a message can declare instances and enum members and then name them, which is how a closed vocabulary is written and is the shape the naming dictionary already uses, and a probe of exactly that passes. What it cannot do is declare a word and then name the word. A preamble in the sense the 8 October entry meant — declare the vocabulary and the parts of speech, then put the words together — still has no device for its second half.

One condition on the first, which the record does not state. A `choice` is untagged, so its options have to be distinguishable, and the checker holds that in two different ways at `ts/metalang/check.ts:730`. Where two options both take a value and neither derives from the other, it refuses: the value "is a value of both" options "and neither derives from the other, so nothing tells the two apart". Where one option derives from the other, the narrower one wins and nothing is said — so a choice between a name and a string reads every value as the name, because `name` derives from `string`. The four fillers EvoLang needs can be told apart; the thing to avoid when writing them is a pair where one derives from the other.

## The three questions carried to Tim and never answered

These are open. They are not a menu for Denice to decide; they are what the design is waiting on. A fourth — what EvoLang is for — was answered on 2026-11-06 and is above.

1. **Does a verb carry named roles?** The general form says a statement's parts are each in a named role. A verb declared as a function with an ordering convention gives positional roles instead. The two produce different languages and the record recommends neither.
2. **Does a statement carry a quantifier?** Quantity is one of the five facets and was measured at around a hundred occurrences. Whether a quantifier is part of a statement or a separate statement about one has not been decided.
3. **Is a sentence parsed from text, or written as structure?** A controlled natural language implies the first. Everything built so far is the second: documents are written as structure and checked. Which one EvoLang is decides whether a parser exists at all, and it is the largest single question on this list.
