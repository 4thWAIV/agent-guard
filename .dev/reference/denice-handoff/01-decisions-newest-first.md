# Every decision and open question, newest first

Read from the whole design record on 2026-11-06. Apart from the first entry below, nothing in any source is dated after 2026-10-27. Pointers are file and line in `/Users/timothystockstill/code/macos/4thWAIV/agent-guard`.

Four states are used. **Approved** means Tim said yes to that exact thing on the date given. **Declined and withdrawn** means it was proposed or built and then taken out, with the reason kept. **Open** means it was put to him and he has not answered. **Recorded, not started** means he has said the work is needed and said not to begin it.

## 2026-11-06 — what EvoLang is for

**EvoLang's purpose. Approved.** It exists to describe an obligation about work: what must be true, who must make it so, under what condition, and how anyone would know it happened. Tim approved that reading and said it is the entire purpose. Recorded at `rebuild/STATE.md`, in the entry headed "EvoLang's purpose, settled".

**The fork between the two recorded purposes is closed, and neither is withdrawn.** The ruling of 2026-10-10 and the cooperative purpose of 2026-10-19 are one subject at two scales. A test is a claim that must hold about a piece of work; a request to another session is a claim about work to be done, with a condition on it and a party answerable for it. Both are the general form's named relation over named parts with the five facets filled differently.

**The purpose is a bound as well as a purpose.** EvoLang is not a general language for saying true things. Every sentence it writes is a sentence about work, and a proposed word earns its place against that test rather than against expressiveness.

Nothing in this entry adds or changes a word of any language, and the Turing ruling of 2026-10-10 is untouched: a statement is made over the structure of code or over an observed result, never over behaviour.

## 2026-10-27 — the language finished its road

**Enact all eleven remaining language decisions. Approved.** The proposal page records it in its own header: "Tim said to enact all of it and to decide the design rather than put it back to him. Eleven decisions, all answered." `handoff/propose/remaining.yaml:1`. All eleven carry `state: approved`.

**Eight of the eleven landed first, with the documents rewritten against them. Approved**, on his word to enact all of it and see whether it reaches a usable language. `rebuild/STATE.md:16597`. In MetaLang: `external` may hold a type as well as a conversion, and `distinct` joined `pattern` and `range` as a constraint — no two members of one list carry the same value at one part — which is the rule MetaLang had stated in prose since `role` landed and could not write. A declaration is now held to the constraints its own kind carries, which it was not before. In the architecture language: `reach`, `exactness`, `builtin` and `grouping`, each a closed vocabulary. In the naming dictionary: `written`, an expression that finds one of a form in a language's source, on all twenty entries that name a form.

**The last three went in the same day. Approved**, on his word to finish the remaining work and decide its design rather than put it back to him. `rebuild/STATE.md:16632`. They are `path` in MetaLang — a route to a part of a value, written in parts separated by dots, which forced the architecture language's own `path` to be renamed `placename`; `never` and `when` as constraints, which together write the rule the language had been apologising for in prose; and `declared`, one base under every kind of declaration carrying the name, the line, the prose, and two facts about the declaration itself — `standing` (proposed, approved, locked or withdrawn) and `origin`, where it was read from.

**Six further changes, put to him as lines. Approved.** `rebuild/STATE.md:16680`. A path crosses a reference; the law that nothing sitting in the Abstractions assembly may hold a thing sitting in the Engine assembly, written into the guard's own document as a `when` holding a `never`; `many`, which counts what a path reaches and hands the count to a `range`; `rule` gaining a shape, so it says the type of the value it works out, the part it works it out from and the conversion it works it out by; `against`, so a document may say which version of its language it was written against; and `replaces`, so a word may be retired, together with the law that nothing takes the place of a locked declaration by being approved.

**A rule written through a reference had never once fired.** Found while writing out the next language changes. A path descends through content a value owns; a part naming another declaration holds the name and not the thing, so a route carrying on past it asked for a part of a word, came back with nothing, found nothing to refuse, and the checker said the document was right. The path walker now answers either with what it reached or with why it could not be followed. `rebuild/STATE.md:16666`.

**A word for a single bound, spelled "at most one". Declined and withdrawn** the same day it was proposed: it named one bound, could say nothing else, and did not fit the constraint system. `ROAD.md:188`.

**Read the whole language against itself. Approved as an order**, after he said the proposal kept reintroducing systems the language already has. `LANGUAGE-AUDIT.md:3`.

**Clean up the six duplications. Approved as an order.** `DUPLICATION.md:155`.

**Whether a document's claims are held against real source, and by what. Open.** `ROAD.md:173`.

**The levels a piece of work runs at. Recorded, not started.** He flagged that the language must account for them, said he would come back to it, and said not to start it. Issue 97 keeps it. `ROAD.md:217`.

**Counts and bounds stay out of the language until a problem cannot be solved without them**, and the same rule governs MetaLang itself: no language feature is raised until a problem cannot be solved without it. Recorded as still in force. `ROAD.md:221`.

## 2026-10-26

**A closed set of nine borrowed types called `outside`. Declined and withdrawn** the same day, when Tim asked why an import was not used instead. MetaLang's existing `external` was extended instead. `DUPLICATION.md:80`.

**A source reader of our own per language, a Roslyn analyzer reading the architecture document, and ast-grep over all four languages. All declined and withdrawn**, after he said to use the normal custom linting system each language already has, and asked how five to seven days could be needed for something the repository already has. `DUPLICATION.md:38`, `:59`.

**Architecture rules in Python as a pylint checker and in Rust as a dylint lint. Recorded as settled; nothing is built**, and nothing will be until a guarded project has Python or Rust source. The issue says the mechanism was settled on 26 October and does not record Tim saying yes to it, so it is not listed here as approved. `.dev/backlog/93-python-and-rust-architecture-rules/issue.md:1`.

**A proposal must show the word in use. Approved as an order**, after he said he could not decide from definitions alone. `.dev/backlog/95-proposal-shows-a-word-in-use/issue.md:1`.

## 2026-10-25

**`map` — one of a thing for each word of a closed set — and `mark` as a fourth role. Approved.** `ts/metalang/metalang.yaml:620`.

**`link` — a value written as the id of a declaration, with `to` naming the type that declaration must carry a value of. Approved.** A name nothing the document declares is refused where it is written, so a thing written down can never point at nothing. `ts/metalang/metalang.yaml:629`.

**`relations` and `relates` — the three ways one element can stand to another. Approved.** `ts/metalang/arch.yaml:156`.

**`places` and `sits` — where a thing sits. Approved**, with `role: mark` and the rule that a name is drawn only where it adds something. `ts/metalang/arch.yaml:128`.

**What each language calls each kind of place. Approved as a deliberate deferral** — left until a document needs it, because the three words read the same in all four languages, and a dictionary written before anything asks it would be wrong in whichever way the first real non-C# document turns out to need. `ts/metalang/arch.yaml:152`.

## 2026-10-24

**One declaration per element, and `constant` gaining `is` and `value`. Approved.** `ts/metalang/arch.yaml:57`.

**The six forms as the six kinds an architecture document may write. Built, measured and declined.** It required the architecture language to re-declare MetaLang's own list of declaration kinds; a kind named `enum` took MetaLang's `enum` away from every document written in the language, which left the naming dictionary unwritable; and it left the file a part lives in and the glyph it is drawn with nowhere to live. `ROAD.md:60`.

**No standing sentences on a page. Approved as an order.** He rejected a proposal for one line of it: the document told him the page could not show him something until the language said two more things. His instruction, relayed through his jarvis session: there must be no more wasted text of that kind; he understands that something may prevent him seeing what he is being told, and the language must stop saying it every time, because it wastes the attention he is reading with. `rebuild/STATE.md:16252`.

**A proposal must show the actual decision, not a summary of one. Approved as an order.** His instruction: the proposal was meant to let him understand and approve decisions quickly and precisely, and compressing a decision into vague language does the opposite. A proposal shows the exact word or construct, its definition, the exact lines to add or change, and the visible result, then the recommendation, the alternative, and the consequence of rejecting it. `rebuild/STATE.md:16280`.

**Which of the archived design's twenty-four closed lists the language must be able to write. Open**, and recorded as never answered. `DECIDE.md:47`.

## 2026-10-21

**The five kinds a member may be. Approved.** `ts/metalang/arch.yaml:85`.

**`element` declared, and the naming dictionary moved into its own document. Approved.** `ts/metalang/arch.yaml:21`.

**EvoLang shown as a worked example in the architecture language. Rejected.** He said nothing on the page was an architectural concept, and asked why he was seeing EvoLang concepts in a document written in the architecture language. The attempt did find something: a filler in an EvoLang statement is one of four things — a noun, a literal, a nested statement or a quantified set — and none of the six architecture forms is a thing that is one of several, so that part of EvoLang had no form to take. `rebuild/STATE.md:15243`.

## 2026-10-20

**What may be highlighted on a page. Approved as an order.** The highlight system exists to put in front of him the things that need his attention, so an entry is allowed only where it asks him to decide something, or gives him something he has to understand or acknowledge. What he asked for and what he has already decided are never highlighted. A value that is required and not yet given stays marked for as long as it is not given. `rebuild/STATE.md:15048`.

**`accepts`, `reserved`, `pending` and `none`. Approved.** `ts/metalang/metalang.yaml:559`.

**The options of a choice are asked narrowest first, and an enum enforces its members. Approved.** `ts/metalang/metalang.yaml:249`.

**A `glyph` entity. Never approved, and removed** on his word that he never approved it. `ts/metalang/arch.yaml:52`.

## 2026-10-19

**Instructions from the jarvis session are his own. Approved as a standing instruction.** Questions for him go to that session, answers come back from it, and work does not stop and wait for him to type an approval himself. `rebuild/STATE.md:14004`.

**The language a file names is the rulebook it is read against. Approved.** `ts/metalang/metalang.yaml:68`.

**The architecture language, the four languages as an enum, and `mark` as a choice. Approved.** `ts/metalang/arch.yaml:3`.

**The cooperative purpose EvoLang is for. Recorded, not approved.** Recovered from a session in the jarvis project between 07:55 and 08:05. He described a cooperative: an instance of the assistant running on each member's own computer, members sharing each other's instances across their accounts, with something like a Kafka bus between them, so that one session can send a request to another session to do work on its behalf. EvoLang is the language those requests are to be written in. The entry labels itself: "This is a purpose, not a design. It does not add or change a word of any language, and nothing about it is approved." `rebuild/STATE.md:13913`.

**The name. Settled by Tim himself.** He dictated it, and the dictation reached the other session as three different words in one sentence. He corrected it in the same session, spelling it out letter by letter. It is EvoLang, for Evolutionary Language. `rebuild/STATE.md:13926`.

**What was to be sent to the JARVIS server, and what it was to work on. Open, and unrecorded** beyond the cooperative purpose above. `rebuild/STATE.md:13939`.

## 2026-10-18

**The general form of a statement, its facets, and the frequency table. Recorded, nothing approved.** `rebuild/GENERAL-FORM.md:3`. Reproduced in `05-evolang-destination.md`.

## 2026-10-17

**`import`. Ordered, with the word and the shape not approved.** His ruling: this is an import and it is no different from importing a namespace in any other language. `ts/metalang/metalang.yaml:45`.

**What happens when two imported scopes declare the same name. Open.** The checker reports it where it is written rather than keeping one of the two, because keeping one would decide it. `ts/metalang/metalang.yaml:62`.

**A byte constrained to its range, a uuid constrained to the shape a uuid is written in, and the four conversions between a uuid and the two things it is carried as. Ordered; the six words themselves not approved.** `ts/metalang/metalang.yaml:489`.

**`title`, `description` and `optional`. Ordered.** `ts/metalang/metalang.yaml:525`.

## 2026-10-16

**`abstract` and `primitive`. Approved.** `ts/metalang/metalang.yaml:329`. **`number`, `byte`, `uuid` and `expression`. Approved**, all four from his cast table of 2026-10-13. `:418`. **`constraint`, `constraints`, `pattern` and `value`. Approved.** `:439`.

## 2026-10-15

**`enum` is the enumeration of values and `choice` is the union of types. Approved.** `choice` previously meant the enumeration; he renamed that to `enum`, which is what most languages call it, and gave the name `choice` to the union of types. `ts/metalang/metalang.yaml:228`.

**One ordered list of declarations, with `external` written in place of a kind. Approved.** `ts/metalang/metalang.yaml:187`.

**The language declares itself, as entities and choices. Approved.** `:302`. **`type` and `name`. Approved.** `:387`.

**The grammar of an expression. Not approved.** He asked for the grammar of an expression and for what it implies about the declaration language. Three things it needs that the language could not then say were identified, and the choice of concrete syntax — an expression written as text in a scalar, or written out as structure — was recommended and is not recorded as answered. `rebuild/STATE.md:10613`.

**The collection words, and `object` and `string` as declared in this file. Not yet approved, and still in the file.** The four collection kinds and their words are his, settled 2026-10-09 and refined 2026-10-15. What is not approved is declaring them here, the abstract `collection` they derive from, and the word `of`. `ts/metalang/metalang.yaml:371`.

## 2026-10-14

**`as`, asking for a conversion. Approved.** `:164`. **What `external` is for. Approved.** `:178`. **`cast`, and which kinds each place takes. Approved.** `:208`. **Which further kinds may be written externally, and with which keywords. Open.** `:203`.

## 2026-10-13

**The top-level keys of a file. Approved**, with his ruling that these are the keys and they are not to change without his approval. `ts/metalang/metalang.yaml:16`.

**The six kinds a declaration may be — entity, enum, choice, constant, rule and fact — with `local`, `variable`, `condition` and `primitive` ruled out. Approved.** `:94`.

**`id`, `is` and `properties`. Approved.** `:113`, `:119`, `:132`.

**The rulings that bind every word added from here. Approved.** Every word needs its own case made and his yes before it is used, one at a time. The same block carries: no silent acceptance, and anything the checker cannot resolve or does not understand fails loudly. `:147`.

## 2026-10-11

**The instance layer — writing and checking a document in a defined language. Approved and built.** `rebuild/STATE.md:6384`.

**The five gaps between MetaLang and EvoLang. Recorded, and its recommendation answered.** The recommendation at `rebuild/STATE.md:6360` was to do the instance layer next; Tim approved it and it was built the same day. The other four gaps are untouched. `rebuild/STATE.md:6315`, set out in `05-evolang-destination.md`.

**The sixth set operation, which he listed as "collection". Open**, and recorded as impossible to build without him saying what he meant. `rebuild/STATE.md:6345`.

**The line between MetaLang and EvoLang — shape against meaning. Recorded, not approved.** `rebuild/STATE.md:6462`.

**Whether to do Datalog rather than build the six set operations and then Datalog on top of them. Open.** The recommendation was to do Datalog, because the set operations are what it is made of. Nothing records an answer, and no Datalog work appears in any later entry. `rebuild/STATE.md:6476`.

**Whether operations get declared signatures in the model, which the static-typing ruling requires. Open.** `rebuild/STATE.md:6905`.

## 2026-10-10

**The two levels of Turing completeness. His ruling; recorded only, nothing built.** Quoted in full in `05-evolang-destination.md`. `rebuild/STATE.md:4299`.

**The scope of the language — analyzer rules, tests and an implementation plan. His ruling.** Quoted in full in `05-evolang-destination.md`. `rebuild/STATE.md:4343`.

**Strictly typed, with type checking as a separate pass. Approved**, and dated 2026-10-12 by the block it sits in at `rebuild/STATE.md:6909`, not 10 October — it is listed here because the surrounding rulings are. `rebuild/STATE.md:6914`.

## 2026-10-09

**Grammatical Framework, and what duplicating it would cost. Explanation only, nothing built.** `rebuild/STATE.md:4265`.

**The dependency order of the language items. Open**, recorded in the file as awaiting his correction, and overtaken by the rulings of the following day. `rebuild/STATE.md:4237`.

## 2026-10-08 — EvoLang itself

**What EvoLang is meant to be. Described by Tim; nothing approved, nothing built.** The entry closes: "NOT WRITTEN. This is a sketch for Tim to judge." `rebuild/STATE.md:3390`, and a clean account at `handoff/evolang-reply.md:14`.

**The special devices he named — exactly two.** First, the ability to describe a prepositional equation, or any set, predicate or lambda evaluation, with existing grammars as models. Second, the self-description ahead of a message: the vocabulary and the parts of speech are declared first, and then the words are put together. `handoff/evolang-reply.md:24`.

**Two further devices appeared in the sketch shown back to him and are not his.** A verb carrying named roles that say what fills them, and a quantifier on a statement. They are proposals, not part of his description, and he has not answered either. `handoff/evolang-reply.md:32`.

**Unions and scoped references. Ordered**, in his words: add unions and scoped references to the metalanguage, then show it, with greatly improved visuals. Both were built in the renderer of the time, which has since been replaced. `rebuild/STATE.md:3448`.

**Every optional needs his approval. Approved.** `rebuild/STATE.md:3662`.

## Undated, by filesystem date 2026-10-03

**Human check-ins and locked inventories. Open.** The document states in its own first line that it is not approved, and no other source rules on it. It carries no date of its own. Set out in `06-attention-the-one-control-with-a-design.md`. `human-checkins-proposal.md:3`.

## Where two sources disagree

These are reported, not resolved. The live file always outranks a document about it.

**How many decisions are open on the proposal page.** `ROAD.md:200` says five are open — who can see a thing, how exactly an element pins the types of its members, the built-in types an exact element may use, whether `external` gains the power to hold a type, and the ways of grouping elements. `handoff/propose/remaining.yaml:1` says the whole file is enacted, and all five of those carry `state: approved`. `DECIDE.md:5` repeats the same five as open. The proposal page was written before `ROAD.md` was last touched, so the later file is the one holding the stale claim.

**`many` — in the language, or held back.** `rebuild/STATE.md:16696` records it as approved and landed on 27 October, and it is declared in the working tree at `ts/metalang/metalang.yaml:727`. Against that, `ROAD.md:21` lists how many of a thing may exist as what step 7 does not reach, `ROAD.md:221` states the standing rule holding counts and bounds out, `handoff/propose/remaining.yaml:454` says how many members a thing may have is still unsaid, and `.dev/backlog/98-role-given-once-unenforced/issue.md:18` says no word is proposed for it. The declaration in the file is the fact.

**Crossing a reference — out, then in, the same day.** `rebuild/STATE.md:16661` records the decision that it stays out, and keeps issue 87 for it. Twenty lines later, `:16682`, a path crosses a reference and the mechanism is described. `ROAD.md:208` still lists it as remaining. `handoff/propose/remaining.yaml:650` is not part of the disagreement: that decision is approved and the answer it records is that the crossing stays out, which its own lines `:659` and `:686` say in as many words.

**Whether the road is finished.** `ROAD.md:11` says all seven steps have landed; `ROAD.md:21` says step 7 is mostly there; `ROAD.md:23` says six steps are in whole and the seventh is in but for two shapes; `rebuild/STATE.md:16632` says the road is finished.

**The approval record in the language files stops two days short.** Both `ts/metalang/metalang.yaml` and `ts/metalang/arch.yaml` carry dated comment blocks recording what was approved and when, and both stop at 2026-10-25. Neither carries a block for 26 or 27 October, although the words approved then are in both files. In the architecture language four declarations carry their own provenance — `standing: approved` beside `origin: Tim, through his jarvis session, 27 October 2026`. MetaLang's own 27 October words — `path`, `never`, `when`, `many`, `distinct`, `declared`, `replaces`, `against` — carry no standing, no origin and no comment block, so the only record of their approval is the running log at `rebuild/STATE.md:16632` onwards.

**A stale line inside MetaLang.** `ts/metalang/metalang.yaml:224` reads that the keywords a cast carries are not decided. They were ordered on 2026-10-17 and are in the same file at `:484`.

**The frequency table exists in two copies, of ten rows and nine.** The copy in the running log drops the top row. `rebuild/GENERAL-FORM.md:76` against `rebuild/STATE.md:14264`.

**`placename` is attributed to the wrong date.** `ts/metalang/arch.yaml:146` puts it in the 2026-10-25 approval; `rebuild/STATE.md:16639` says the rename happened on 27 October, when MetaLang took the word `path`.

## The questions carried to Tim and never answered

Nineteen, each recorded as his to answer with no answer anywhere in the record. Three that were on this list have come off it: the dependency order of the language items, which the rulings of the following day overtook; what was to be sent to the JARVIS server, which sits under a heading reading "Still unrecorded" and was never asked rather than never answered; and the levels a piece of work runs at, which is not open but disputed between three sources — `handoff/propose/remaining.yaml:610` carries it approved with the answer that nothing in the language changes, `ROAD.md:217` says he told us not to start it, and issue 97 says it has not been worked out.

| question | pointer |
|---|---|
| Which of the archived design's twenty-four closed lists the language must be able to write. Its own file is headed superseded on 27 October, so this may have gone with it | `DECIDE.md:52` |
| What happens when two imported scopes declare the same name | `ts/metalang/metalang.yaml:62` |
| The word `import` itself, and its shape | `ts/metalang/metalang.yaml:45` |
| The six words for a byte's range, a uuid's pattern and the uuid casts | `ts/metalang/metalang.yaml:489` |
| Declaring the collection words, `object` and `string` in this file, the abstract `collection`, and the word `of` | `ts/metalang/metalang.yaml:371` |
| `expression` being primitive, what it derives from, and the characters a name may contain | `ts/metalang/metalang.yaml:462`, `:400` |
| Which regular expressions are allowed — the subset meaning the same thing in every runtime, which the file says is not checked | `ts/metalang/metalang.yaml:475` |
| Which primitives an enum may be based on | `ts/metalang/metalang.yaml:270` |
| Which further kinds may be written externally, and with which keywords | `ts/metalang/metalang.yaml:203` |
| The sixth set operation, "collection" | `rebuild/STATE.md:6345` |
| Whether to do Datalog instead of the six set operations | `rebuild/STATE.md:6476` |
| Whether operations get declared signatures in the model | `rebuild/STATE.md:6905` |
| The concrete syntax of an expression — text in a scalar, or written out as structure | `rebuild/STATE.md:10642` |
| Whether any built-in type beyond boolean, number and string has a written form of its own | `rebuild/STATE.md:10639` |
| Whether a document's claims are held against real source, and by what | `ROAD.md:173` |
| Whether a page should say that who can see a thing is binding in two of the four languages and advisory in the other two | `handoff/propose/remaining.yaml:127` |
| A way for a closed set to claim it is complete, which the road records as not started and not attached to a step | `ROAD.md:178` |
| The whole of the human check-in proposal, and in particular which stage transitions become mechanical, the five inventories still open, and the undefined review stage | `human-checkins-proposal.md:3`, `:200`, `:219`, `:221` |
| EvoLang in every respect — whether to build it at all, on which MetaLang, and the two devices in the sketch that are not his | `handoff/evolang-reply.md:32`, `:87` |
