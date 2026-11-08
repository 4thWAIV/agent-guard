EvoLang is the evolutionary language: the half of this project's language work that carries meaning rather than shape. MetaLang says what a document is made of and whether it is well formed. EvoLang says what a document's statements claim, what follows from them, and whether they are true. Nothing of it exists in any file.

This issue exists because the whole of EvoLang's design has been carried in a running log and two reference documents, where no tracker can see it.

## What it is for

**Approved 2026-11-06.** EvoLang exists to describe an obligation about work: what must be true, who must make it so, under what condition, and how anyone would know it happened. Tim approved that reading as written and said it is the entire purpose. The record is in `.dev/inprocess/82-guard-process-workflow/rebuild/STATE.md`, in the entry headed "EvoLang's purpose, settled".

That closed a fork which had stood for three weeks. Two statements of purpose were on the record: the ruling of 2026-10-10, that the language exists to provide the analyzer rules, the tests and the implementation plan, and the cooperative purpose of 2026-10-19, that EvoLang is the language in which one session asks another to do work on its behalf. They are one subject at two scales, and neither is withdrawn. A test is a claim that must hold about a piece of work; a request to another session is a claim about work to be done, with a condition on it and a party answerable for it.

Because it is the entire purpose, it is also a bound. EvoLang is not a general language for saying true things. Every sentence it writes is a sentence about work: an obligation, its condition, the party answerable for it, and the evidence that would settle it. A proposed word earns its place against that test, which is narrower than expressiveness.

## What it is, as a notation

A controlled natural language with an inline vocabulary preamble. It declares nouns and verbs, carries devices for sets, predicates and lambda evaluation, and declares its own vocabulary and parts of speech at the head of a message before putting the words together. That is the description of 2026-10-08; nothing about it is approved.

The prior art named the same day: SBVR, the OMG's Semantics of Business Vocabulary and Rules, recorded as a direct match and as famously hard to implement; Attempto Controlled English, the working proof that a sentence a person reads can be a predicate a machine evaluates; categorial grammar and CCG, the formal version of parts of speech and then putting the words together; and Avro's object container format, which is the preamble idea exactly — the schema is written in the file header, then the records.

Grammatical Framework was examined on 2026-10-09 and not adopted. A compiled lexicon cannot accept a vocabulary declared in the message it is about to read, which is the one thing EvoLang must do.

An expression language was considered and rejected for this job. CEL was chosen for predicates and rejected for contracts, because a person signing a condition full of arrows cannot audit it. A controlled natural language passes that test: the condition reads as a sentence and is still a predicate. The cost is recorded as honest and large, and ambiguity is the permanent risk.

## The two rulings that bound what it may ever say

Both are Tim's, both from 2026-10-10, and both are recorded as rulings rather than as built work.

**Turing completeness.** The language definition system used to define, validate and create the deterministic language is not Turing complete. A language it defines may nevertheless describe a Turing-complete operation, such as defining a function in a contract. Turing completeness belongs to an evaluator and not to a notation: a tree describing a function is data, the checker reads it, validates its shape and derives facts about it, and never runs it. The consequence decides how every code-facing gate is written. A statement over the structure, such as "this function does not call X", is derivable from the tree. A statement over an observed result, such as "this test passed", is a fact fed in from a run. A statement over the behaviour, such as "this function is correct", is not available, ever.

**Scope.** The language must only be able to provide the analyzer rules, the tests and the implementation plan for the agents to execute. Code structure is validated by generated analyzer rules running in the real build, behaviour is validated by the tests running in the real test pass, and the language itself validates nothing about code. It emits the three documents and checks their internal consistency.

## What is built beneath it

MetaLang, and three languages written in it: the architecture language, which says what code is made of; the naming dictionary, which says what each of four programming languages calls each of six forms and how to find one in source; and the proposal language, which puts a decision to a reader and is itself not approved. Three documents are written in the architecture language, describing this toolchain, the naming dictionary and the guard. Every document checks clean, the suite passes, and each document draws as a page. The architecture language's seven-step road finished on 2026-10-27.

## Progress, newest first

- **2026-11-08 — the four open design questions were answered, and the start was held. Approved.** An EvoLang sentence is written as structure rather than parsed from text; a verb carries named roles rather than positional ones; a quantifier sits inside a statement rather than being a separate statement about one; and a method's parameters and its return type are EvoLang's half, not the architecture language's. Whether to build EvoLang is decided later, so nothing is written. Full record in `decisions.md` beside this file.
- **2026-11-06 — purpose settled and bounded. Approved.** Nothing was added to or changed in any language.
- **2026-10-27 — eight MetaLang words landed that the general form needs:** `path`, `never`, `when`, `many`, `distinct`, `declared`, `replaces` and `against`. With them, four of the shapes the corpus reading measured became writable — standing, condition, polarity, and quantity as a count.
- **2026-10-25 — `link` and `map` landed.** `link` resolves a name against everything the document reaches, which is the file-wide scope the preamble needs, and closes half of the second blocker below. `map` writes one of a thing for each word of a closed set, which is the second largest body of content in the corpus.
- **2026-10-19 — the cooperative purpose was recorded**, and the name was settled by Tim himself: EvoLang, for Evolutionary Language.
- **2026-10-18 — the general form of a statement was read off every plan document in the issue folder.** A statement is a named relation over named parts, carrying five facets: truth, condition, quantity, polarity and modality. Seven further uses appear where a statement is the subject of another statement: standing, provenance, ground, absence, order, contribution and entailment. Recorded in `rebuild/GENERAL-FORM.md`, which says of itself that nothing in it is approved.
- **2026-10-15 — `choice` was named for the union of types**, which closes the first blocker below.
- **2026-10-11 — the five gaps between MetaLang and EvoLang were recorded in dependency order.** The first, the instance layer, was approved and built the same day: documents written in a defined language are written and checked today. The other four are below.
- **2026-10-10 — the two rulings above.**
- **2026-10-08 — described. Nothing approved, nothing written.**

## The four gaps that remain, in dependency order

- **Set construction.** Six ways to define a collection are settled — by listing, by restriction, by union, by intersection, by difference, by product. Only listing exists.
- **An expression over more than one value.** A statement relating two things cannot be written. Tim's own example of such a rule: this phase's exit criteria include everything it produces.
- **A function with parameters** as a declared thing rather than a list written down. A verb is a function: it takes a subject and an object and yields a statement. With no parameter and no expression, a verb cannot be declared.
- **Parts of speech and statements**, which needs the gap above plus an ordering convention saying which parameter is the subject and which the object. Nouns are entities, which exist; verbs are the gap above, which does not.

## The two blockers of 2026-10-08, read against the tree as it stands

On 2026-10-08 exactly two things were named that EvoLang needed and MetaLang could not say. **No document records the reading below, so it is a reading of the files and not a decision, and it is Tim's to confirm.**

**A four-way union, for a filler in a statement.** A filler is a noun, a literal, a nested statement or a quantified set. This is closed. `choice` is a type that is one of several types; it is untagged, so the options have to be distinguishable from one another, and the checker refuses a choice where two options both take a value and neither derives from the other. Where one derives from the other the narrower one wins silently, so the thing to avoid when writing the four fillers is a pair where one derives from the other.

**A reference that resolves against the vocabulary the message itself declared**, which is the preamble. This is half closed. `link` gives the file-wide scope, and a document can declare instances and enum members and then name them, which is how a closed vocabulary is written. What a link may name is the other half: the declaration it names has to carry a value of the type written under `to`, so a link names an instance and cannot name a word. A preamble that declares words and then uses them still has nothing for its second half.

## Datalog is the intended evaluator, and none of it is built

A Datalog rule derives a relation, which is a set, so all six collection operations fall out of one mechanism: facts for listing, a body condition for restriction, two rules with one head for union, two conditions for intersection, negation for difference, and two independent variables for product. Recursion comes with it, which none of the six give on their own. The recorded conclusion is that building the six operations and then Datalog would be building the same thing twice, so Datalog is the thing to build, because the set operations are what it is made of. The order written down was to define Datalog in MetaLang first, as the real test of whether MetaLang can define EvoLang, and to write the interpreter afterwards, in TypeScript first and C# later. Whether to do this was put to Tim and is not recorded as answered.

## What this is waiting on

**Tim's word to start.** He decided on 2026-11-08 to decide later whether to build EvoLang, so nothing of it is written until he says so. That is the only thing this is waiting on.

The four design questions that were open are answered, and the answers authorise no work on their own. They are recorded in `decisions.md` beside this file: a sentence is written as structure, a verb carries named roles, a quantifier sits inside a statement, and a method's parameters and its return type are EvoLang's half.

Two things in the general form are not closed, and neither has been put to him. A computed truth has nothing behind it: `rule` says what one would look like and nothing runs one. And provenance is only part written — the general form defines it as the source it was read from, who wrote it and the date, while `origin` is one optional free-text string.

Whether to define Datalog in MetaLang and then write the interpreter has been put to him and is not recorded as answered.

## Nothing starts here without his word

Two standing rules govern every step of this work, and both are in force. Every word of the language is approved by Tim, one word at a time. And no language feature is raised until a problem cannot be solved without it, so a gap in the language is reported as a gap and not filled with a proposal.

## Where the material is

- `.dev/inprocess/82-guard-process-workflow/rebuild/STATE.md` — the running log, newest last. Every dated entry above comes from it.
- `.dev/inprocess/82-guard-process-workflow/rebuild/GENERAL-FORM.md` — the general form of a statement and the corpus frequencies. Not approved.
- `.dev/reference/denice-handoff/05-evolang-destination.md` — the destination assembled from the record, with the open questions marked open.
- `.dev/reference/denice-handoff/02-controls-against-the-language.md` — what the live vocabulary can and cannot say.
- `.dev/backlog/104-evolang/decisions.md` — the eight decisions Tim signed off on 2026-11-08, beside this file.
