# The eight controls against the language as it stands

All line numbers are in the working tree of `/Users/timothystockstill/code/macos/4thWAIV/agent-guard` on branch `metalang`, read on 2026-11-06. That branch was committed to and pushed on 2026-11-08, so all of this is now on `origin/metalang`, which is not the branch a clone lands on; see `03-repository-map.md`. The comment blocks added to `ts/metalang/metalang.yaml` on 2026-11-08 shift the line numbers in that one file by about seventy lines.

## The answer in one table

| control | word in the language | how far anything reaches |
|---|---|---|
| inputs | none | a method's own definition says it "Carries a signature" and the language gives no property to write one |
| outputs | none | `answers` is a relation between two whole elements, so it cannot say which member returns what |
| dependencies | none by that name | exactly three fixed relations exist: implements, answers, holds |
| events | none | nothing adjacent at all |
| attention | none | the proposal language's `decision` and `states` are the built mechanism for a named party having to answer before a thing stands |
| workflows | none | `process` is one of six forms an element may take, and an element has no property holding steps, order or a gate |
| steps | none | the word appears once, inside a line of prose |
| state machines | none | two enums say how firmly a written thing is agreed, which is a different subject |

Two of the eight — events and steps — have nothing in the vocabulary that is even adjacent. None of the eight has an approved word. What a document may do about that is at the foot of this file.

## Inputs

No declaration anywhere names a parameter, an argument or an input. The closest thing is the definition of a method, which asserts that a signature exists — `ts/metalang/arch.yaml:271`:

```yaml
        - id: method
          title: A callable on a type. Carries a signature.
          value:
            mark: "()"
            example: IGuard.PrecheckAsync
            name: Method
```

The entity that holds a method has five properties and none of them is a signature — `ts/metalang/arch.yaml:514`. They are `name`, `kind`, `reach`, `type` and `does`. The one type slot, `type`, holds a single built-in type for the whole member, and only where its element is written as `exact`:

```yaml
        type:
          is: builtins
          accepts: [none, pending]
          title: The built-in type it carries where its element is exact.
```

`builtins` lists fifteen numeric, byte, text and time names from `int8` to `duration` (`ts/metalang/arch.yaml:439`) and no element names. `exactness` says of itself that "Nearly everything is loose" (`ts/metalang/arch.yaml:405`), and every member of every live document writes `type: none`.

In the language files, one parameter type is written anywhere at all, and it is inside a prose example: `FileGuard(FileGuardConfiguration)` at `ts/metalang/arch.yaml:281`. It is text, and nothing checks it. The source itself has them, of course — `src/AgentGuard.Abstractions/Contracts/IGuard.cs:25` declares four parameters on one method — and the architecture document describing that interface has no approved word to say so.

So no approved word writes a method's parameters, and none writes its return type. That is not the same as the notation refusing them; see the foot of this file.

## Outputs

The adjacent word is a relation between two elements, not a member's return — `ts/metalang/arch.yaml:363`:

```yaml
        - id: answers
          title: It gives the other one back to whoever called it.
          value:
            mark: "→"
            example: IGuard answers with a Verdict
            name: Answers
```

`relates`, which carries it, is a property of `element` and not of `member` (`ts/metalang/arch.yaml:587`), so `handoff/guard.yaml:84` can say `answers: [Verdict]` for the whole of `IGuard` and cannot say which of its three methods gives a `Verdict` back. It can also only name elements the document declares — `ts/metalang/arch.yaml:165`:

```
# Only elements a document declares are named. A member whose type is a string says nothing here,
# because text is not a part of an architecture.
```

MetaLang's `rule` carries `of`, the type of the value it works out (`ts/metalang/metalang.yaml:1185`), but that is the result of a MetaLang derivation and not a member's return, and nothing runs one.

## Dependencies

No declaration is named dependency, depends, uses, calls or requires. One element's dependence on another can be written in exactly three fixed forms, which are the three words of a closed vocabulary — `ts/metalang/arch.yaml:344`:

```yaml
  - enum:
      id: relations
      is: relation
      title: Every way one element can stand to another, in reading order.
      values:
        - id: implements
          title: It satisfies the whole of the surface the other declares.
        - id: answers
          title: It gives the other one back to whoever called it.
        - id: holds
          title: It carries the other one as part of itself.
```

They are written through one property — `ts/metalang/arch.yaml:587`:

```yaml
        relates:
          map: { by: relations, of: { list: { of: { link: { to: element } } } }, accepts: [none] }
          accepts: [none, pending]
          title: What it stands to, one line for each way one element can stand to another.
```

`map` forces all three keys to appear in the vocabulary's own order and refuses an invented key by name; `link` refuses a name nothing the document declares. In practice an element writes this block and nothing else — `handoff/guard.yaml:82`:

```yaml
        relates:
          implements: [IGuard]
          answers: [Verdict]
          holds: none
```

Every relation in the language is element to element. There is no relation from a member to anything.

The only place the word "depended on" appears is the definition of a kind of place — `ts/metalang/arch.yaml:325`, where a package is "What is shipped and depended on as one thing."

## Events

Nothing. The two closed sets that say what a thing can be are complete and hold nothing event-like: the six forms are interface, class, record, enum, function and process (`ts/metalang/arch.yaml:218`), and the five kinds of member are method, constructor, property, field and value (`ts/metalang/arch.yaml:266`). There is no signal, message, notification, publish, subscribe or handler word in any language file.

## Attention

No word in MetaLang or in the architecture language. The proposal language holds the closest thing in the repository, and it is built and checked: `ts/metalang/propose.yaml:106` declares `decision` — "One thing to approve or reject, with everything needed to answer it" — carrying `pick`, `instead`, `rejected`, `costs` and `state`; and `:178` declares `states`, whose `proposed` is "Put to the reader, not yet answered." MetaLang says the same of a declaration at `ts/metalang/metalang.yaml:1015`, where `standings` has `proposed` as "Put to whoever decides, and not yet answered."

That is a named party having to answer before a thing stands, which is the core of attention. What neither of them carries is who the party is, when they are asked, or what they are shown. The proposal language is also not approved — its own third line says so.

One nearer-sounding word answers a different question, which is who can see a thing — `ts/metalang/arch.yaml:376`:

```yaml
  - entity:
      id: reach
      is: word
      standing: approved
      origin: Tim, through his jarvis session, 27 October 2026
      title: One answer to who can see a thing.
```

Its values are the compile-time reaches, public, internal and private. It is not offered as a partial match for attention.

The attention design that does exist is a document rather than a language word, and it is covered in `01-decisions-newest-first.md`.

## Workflows

One of the six forms says an element is a process, and nothing more — `ts/metalang/arch.yaml:253`:

```yaml
        - id: process
          title: An ordered run of steps with a gate at each end.
          value:
            mark: "▷"
            example: the Conversation phase
            name: Process
```

So a document can say "this element is a process". `element` has eight properties — `form`, `does`, `reach`, `exactness`, `groups`, `members`, `sits`, `relates` (`ts/metalang/arch.yaml:558`) — and none of them holds steps, an order, a gate, or an entry or exit condition. The only thing a process element can hold is `members`, and a member's kind has to be one of the five in `kinds`.

All four naming dictionaries say no language has a word for a process: `ts/metalang/names.yaml:142` writes `name: none, written: none` for C#, Python, Rust and TypeScript.

One near-miss, so it is not mistaken for vocabulary: `ts/metalang/stage.yaml` declares an entity called `stage`, titled "One stage of the workflow", with a property saying how many times a stage is attempted before it stops the work. It checks clean. It is a sample, and the road says so directly at `.dev/inprocess/82-guard-process-workflow/ROAD.md:234`: "`ts/metalang/stage.yaml` is a sample, written to prove a file other than the language file can be checked, and says so in its own first line." Nothing imports its scope, so nothing is written against it.

## Steps

No word. The string appears once in the whole live vocabulary, inside the line of prose quoted above at `ts/metalang/arch.yaml:254`. There is no `step` declaration and no property that holds an ordered run of them. The one ordered collection on an element is its members — `ts/metalang/arch.yaml:579`:

```yaml
        members:
          list: { of: member }
          accepts: [none, pending]
          title: The members it has, in the order the source declares them.
```

Ordered, and of `member`, whose kind must be one of method, constructor, property, field or value.

## State machines, and state changes

No word. Two enums are adjacent and both are about how firmly a written thing is agreed, which is a different subject from the state of a modelled thing.

`ts/metalang/metalang.yaml:1014`:

```yaml
  - enum:
      id: standings
      is: string
      title: How firmly a declaration is agreed.
      values:
        - id: proposed
          title: Put to whoever decides, and not yet answered.
        - id: approved
          title: Agreed, and open to being changed the same way it was agreed.
        - id: locked
          title: Agreed, and not to be changed without undoing the lock first.
        - id: withdrawn
          title: Taken back, and kept so the record of it is not lost.
```

`ts/metalang/propose.yaml:177` declares a parallel set of three — proposed, approved, declined — for where a decision stands, in a file that says of itself that it is not approved.

Neither names a transition, an event that causes one, a rule about which change is legal, or the state of anything modelled. The one thing in the language that constrains a change of standing is a single prohibition, which MetaLang writes about itself — `ts/metalang/metalang.yaml:1038`:

```yaml
      constraints:
        - when:
            at: replaces.standing
            holds: [locked]
            then:
              never: { at: standing, carries: [approved] }
```

Read: nothing takes the place of a locked declaration by being approved.

## What the checker enforces, which is what a rule can actually say

Six constraint kinds are declared and all six are implemented. The handlers are entries in the `LOGIC` record at `ts/metalang/constraints.ts:221`.

| kind | what it does | declared | handler |
|---|---|---|---|
| `pattern` | the whole value matches a regular expression | `metalang.yaml:665` | `constraints.ts:222` |
| `never` | nothing the path reaches may carry one of these values | `metalang.yaml:692` | `constraints.ts:283` |
| `when` | applies the constraint under `then` only where the part carries one of `holds` | `metalang.yaml:708` | `constraints.ts:302` |
| `many` | counts what the path reaches and holds the count to a `range` | `metalang.yaml:726` | `constraints.ts:331` |
| `distinct` | no two members of one list carry the same value at one part | `metalang.yaml:742` | `constraints.ts:241` |
| `range` | the value sits between a lowest and a highest | `metalang.yaml:759` | `constraints.ts:351` |

A constraint kind with no handler is refused rather than passed. `check.ts:890` reports "nothing says how a <kind> is applied, so no value can be checked against it", and the function that would answer whether a value met it throws rather than answering that it did.

A path is how a constraint names the part it is about. It descends through content a value owns, follows a name into the declaration it names, and ends when it returns to a name it has already crossed. A part holding many of something fans out, so `members.type` names the type of every member.

Five of the six are written in live documents. `many` is declared, implemented and written nowhere yet.

## The two reserved states, which are how an absence is written

`ts/metalang/metalang.yaml:1115`:

```yaml
  - enum:
      id: reserved
      is: string
      title: What a value says when it is not the value itself.
      values:
        - id: pending
          title: Required, and not yet given.
          description: >
            The work is not done. It is reported, and the run it is reported in exits non-zero,
            so a document holding one can never be called finished.
        - id: none
          title: Deliberately left empty.
          description: >
            Nothing is missing, so nothing is reported. It says that the absence was decided
            rather than overlooked, which is what writing nothing at all cannot say.
```

A property takes one of these only where its own `accepts` names it. Without that, the word is simply the text it is written as, so no document gains a meaning it did not ask for.

This matters for any model of the eight controls: the language already distinguishes "not yet given" from "deliberately empty", and it fails a run that holds the first.

## Where words for the eight controls do exist, and why they do not count

`parameters`, `returns`, `signature`, `state`, `depends-on` and `workflow` are all declared in `.dev/inprocess/82-guard-process-workflow/rebuild/archive/language-2026-10-06/language.yaml`, an archived architecture language that predates MetaLang. `workflow` and `signature` also appear in `.dev/inprocess/82-guard-process-workflow/workflow-model.yaml`, which states at its own line 4: "Status: proposal. Nothing here is approved."

Neither file is read by the checker and neither is written in MetaLang. They are prior attempts, and they are useful as evidence that these words were tried, not as vocabulary.

## What a document may declare for itself, which changes how to read all of the above

Everything above reports approved vocabulary. It is not the same as what the notation will accept, and the difference decides what you do next.

**A document written in a language may declare its own words and extend what the language gave it.** `ts/metalang/metalang.yaml:958` states the rule: a property "Written again on a type deriving from the one that declared it, it takes everything it does not write from above." The architecture language's own documents already use it — `handoff/guard.yaml:14` declares `guardpart`, deriving from `element`, and hangs a constraint on it that the language itself does not carry.

**So a method's signature can be written today, in a document, without changing any language file.** A probe run against the real checker declares an entity `signature` with `takes`, a list of strings, and `gives`, a string; derives `signedmember` from the language's `member` and gives it that property; derives `signedelement` from `element` so its members are `signedmember`; and writes the four parameter types and the return type of a real method. The checker reads it:

```
sig.yaml — 5 declarations, checked against "arch", the language it names

Checked, and nothing is wrong.
```

It is genuinely checked, not passed over. Writing `takes: 7` is refused — "is not written as a list, which is how a value of a list is written" — and misspelling the property is refused twice, once for the name the type does not have and once for the signature now missing.

**What that does and does not mean.** It means the shortage is approved vocabulary, not notation, for every one of the eight controls. It does not mean the gap is yours to fill. A document that declares `signature` has added a word to how this project describes architecture, and by Tim's standing rule every word of the language is his to approve, one at a time. The right move on finding a gap is to say which document needs which sentence, and let him decide the word.

The four things the notation itself cannot do, which no document can work around, are the gaps recorded on 2026-10-11 and still open: set construction, an expression over more than one value, a function with parameters as a declared thing rather than a written-down list, and parts of speech. They are in `05-evolang-destination.md`.
