# The general form of a statement in our plans

Read off every plan document in this issue folder on 2026-10-18. Nothing here is approved.

## Why it exists

The language declares types and cannot write anything down. Before designing the half that writes
things down, the shapes of statement our own documents actually contain were enumerated, so that the
design answers to the full set rather than to one example.

## How to run it again on a new document

Ask one question of the document: what distinct shapes of statement does it contain, the shape and
never the content. Report each shape against the same ten questions, which are what make two
readers' results comparable:

  how many things the statement relates; whether its parts are named or positional; whether its
  truth is written or computed; whether it holds only when something else holds; whether it
  quantifies over a set; whether it speaks about a type or about one thing; whether a part of it is
  itself a statement; whether order carries meaning; whether it carries a count; whether it asserts
  that something does not hold.

Require a verbatim example with a line number for every shape, and a closing list of whatever fitted
no shape. The misfits are where the next finding is.

## The corpus

| Document | Lines | Shapes found |
|---|---|---|
| `workflow-model.yaml` | 1091 | 31 |
| `archive/language-2026-10-06/language.yaml` | 1462 | 27 |
| `archive/language-2026-10-06/architecture.yaml`, `contract.yaml`, `setup.yaml` | 911 | 28 |
| `workflow-inputs-outputs.md`, `workflow-subprocess-inventory.md`, `execution-reality.md` | 577 | 37 |
| `human-checkins-proposal.md`, `archive/language-2026-10-06/meta-language.yaml` | 668 | 40 |

Around a hundred and sixty shapes in all, before duplicates between readers are merged.

## The general form

Every one of them is one of two things.

**A statement is a named relation over named parts, carrying five facets.**

    relation        what is being said: parent, runs-at, is-called-in, lives-in, produces
    parts           the things it relates, each in a named role
    truth           written down, computed from named operands by a named party, or
                    neither — a slot declared and left to be filled
    condition       always, or only when something else holds
    quantity        one, none, every, exactly n, at least one, one per member of a set
    polarity        affirms or denies
    modality        what is, what must be, what may be, or what must not be

**A statement may be the subject of another statement.** Three uses of this recur in every document
read, and four more appear in some:

    standing        proposed, approved, locked, withdrawn, not yet decided
    provenance      the source it was read from, who wrote it, the date
    ground          the reason for it, or the statement it follows from
    absence         what it means that this statement is not written
    order           whether the order of a set of statements carries meaning
    contribution    whether this statement counts toward a stated total
    entailment      that one statement does not license a conclusion from it

## What the facets cost us today

The relation and its parts are the half we already have. An entity with named properties is a
relation, and writing one down is the next concept.

The facets are the half that is missing, and they are the reason the documents read as prose. Every
reader was asked to list what appeared only in prose or in comments, with no structure behind it.
Those lists are almost entirely facets: a count, a condition, a prohibition, a closed set, what an
absence means, where a statement came from, and whether it is agreed.

The frequency across the whole corpus, which is the order the work falls into:

| | Roughly |
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

## Three findings that bear on decisions already taken

**A value indexed by a dimension is one shape, not two.** The per-language table in the archived
language (`equivalents: {csharp: interface, python: Protocol or ABC, …}`) and the per-level table in
the workflow model (`l1: required, l2: required, l3: absent`) are the same statement shape: a value
that varies along a named dimension. One is seventeen tables of four cells, the other is around two
hundred and sixty cells. Together they are the second largest body of content we have.

**There are three absences and they are confused everywhere.** Not written and correct, so a default
applies. Not written and wrong. Not written because nobody has decided. The archived language
carried a three-member vocabulary for exactly this. The `optional` keyword added on 2026-10-17
covers the first of the three; the other two have no word, and the workflow model currently says the
third one eighty-three times by writing `undefined` into a value slot.

**An order that carries meaning has to say so.** The archived language states the rule plainly: one
device in it has members whose order is the content, and everywhere else a list is a set that
happens to be written in some order. Both the workflow model and the architecture file have lists of
both kinds, and in both the distinction survives only in a comment.

## What did not fit any shape

Each reader closed with its own misfits. The ones that recur across readers:

- a claim that a set is complete, so that nothing outside it exists
- a claim that one place is the only place something is held
- a statement withdrawn by a later statement in the same file
- a comparison of two things on a scale that is never named
- a statement whose subject is a name rather than a thing, saying that the name refers to nothing
