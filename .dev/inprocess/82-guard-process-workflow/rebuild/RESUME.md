# Resume here

Rewritten 2026-10-24, against the live files. Every path below was checked to exist.

---

## 1. What this work is, and why

Issue 82 needs a workflow — the process a run follows, the decisions it must carry, and the types
Guard needs to hold and sign them. Tim is designing that workflow **by writing it in a language we
are building at the same time**, and reading the result as a rendered page.

So two things are in flight at once, and they feed each other:

- **The workflow** is the subject. Eight phases: Design, Spec, Rule, Test, Implement, Refute, Gate,
  Report. One is designed; seven are names.
- **The language** is the tool. Every time a document cannot say something, the LANGUAGE gets the new
  word, and then the document uses it.

The measure of any view is **understanding per minute**. The page exists so a human can read a plan
and sign it; rendering is a consequence of that, not the purpose.

---

## 2. READ THESE FIRST, in this order

1. **`ts/metalang/metalang.yaml`** — THE LANGUAGE. MetaLang's whole vocabulary, written in the
   language it defines, so it checks against itself. Read the comment blocks at the top end to end:
   they are the record of what Tim approved, when, and why. Skim the declarations at the foot — they
   ARE the vocabulary, and every script reads it from there, so there is no second copy anywhere.
2. **`../ROAD.md`** — the architecture language's seven steps, which two are done, and what each
   remaining one needs. Every remaining step has a GitHub issue.
3. **`ts/metalang/arch.yaml`** — the architecture language, a document written in MetaLang. This is
   the language the plans are written in.
4. **`STATE.md`** — the running log, appended after every change, newest last. **Read the last ~700
   lines**, not the whole file. The early entries describe a renderer that no longer exists.
5. **`ts/metalang/check.ts`** — the checker. Two lists come out of it and they mean different things:
   a PROBLEM is something wrong with the document, a BLOCKER is something the checker could not
   verify. It never passes or skips what it cannot verify.

Then as needed:

6. `ts/metalang/words.ts` — the vocabulary as one value, and every question asked of it. If you
   understand `propertiesOf` and `primitiveOf`, you understand how a document is resolved.
7. `ts/metalang/app.tsx` — the page. `Draw` is the only path from data to page; read its header
   comment, which states the two rules the whole file is built on.
8. `ts/metalang/rulebook.ts` — how a document finds the language it is read against.
9. `ts/metalang/syntax.ts` — the one control that colours MetaLang written as YAML.
10. `ts/metalang/names.yaml` and `ts/metalang/parts.yaml` — the two real documents written in the
    architecture language. `handoff/guard.yaml` is a third.
11. `GENERAL-FORM.md` — the shapes of statement our own plan documents contain, read off every plan
    document on 2026-10-18. Nothing in it is approved.
12. `LEDGER.md` — every word the language is allowed to use, with Tim's yes and a date. Its first six
    rows are superseded and say so.

**Archived, and not evidence for anything.** `archive/language-2026-10-06/` holds the language,
contract, setup and architecture documents and the `view/` renderer this work replaced;
`archive/metalang-2026-10-11/` holds the first MetaLang. Nothing in either carries forward. In the
**parent folder** (`../`), `conversation.md`, `execution-reality.md`, `workflow-inputs-outputs.md`,
`workflow-subprocess-inventory.md`, `issue.md` and the `.cs` sketches describe an ABANDONED earlier
model. `execution-reality.md` is the one still worth reading: it holds the runtime constraints in §4.

---

## 3. The rules. Breaking any of these is how this work went wrong before.

**The language drives the change. Never hack the render.**
When something cannot be expressed, STOP and extend the LANGUAGE with Tim, then use it. Do not
reshape a component to make the page look right. Every single defect Tim has caught was a renderer
deciding something the language should declare.

**No language feature is raised until a problem cannot be solved without it.** Tim raises anything he
wants. Answer yes or no when asked whether such a problem exists. Counts, bounds and how many of a
thing may exist stay out under the same rule.

**NO ASSUMPTIONS, EVER. Nothing from the old design carries forward.**
If a concept has not been proposed and agreed in this rebuild, it does not exist and may not be
referenced, reasoned from, or implied. When something is needed and not yet agreed, say so and stop.

**EVERY OPTIONAL NEEDS TIM'S YES.** Marking a part optional is how a hole gets hidden, and it is what
an agent reaches for when a required field is inconvenient. Tim decides what may be left out.

**Nothing enters a document without Tim's explicit yes to that exact item.** Show the real field, with
its name and its value, and wait. Silence, a topic change, or a request to reword is not a yes.

**Nothing may ever fail silently.** A thing that could not be verified is reported as a blocker, never
as checked. No empty catch, no `|| true`, no early return that makes a check pass, and no
"not applicable" exit that really means "could not tell".

**One drawing, in one place.** `Draw` in `ts/metalang/app.tsx` is the only path from data to page. A
component is chosen by what the node IS, and it must account for every key on the node it draws;
whatever it does not claim draws as an error beside it. So a word the language gains and the page does
not handle appears ON THE PAGE, where it can be seen.

**No field list in code.** The page and the checker read the declared parts out of the vocabulary. If
you find yourself writing `['id', 'is', 'title']`, stop — read it from `words.ts`.

**Nothing is keyed on a particular kind.** A kind's colour comes from its position in the vocabulary,
so a kind added to the language draws in a colour of its own without `app.tsx` being touched.

**The product pages are browser-rendered, never server-rendered.** `ts/metalang/render.ts` writes one
self-contained page per document; React mounts and draws it in the browser, and nothing in the bundle
reaches a filesystem. Tim was explicit about this on 2026-10-06. It does not forbid
`renderToStaticMarkup` in a one-off explanation page written for him to read, where the real markup is
embedded as evidence — `handoff/propose/render.ts` does that — but a document's own page is never
built that way.

**Verify against the live file and prove it.** Read the file; never answer from memory. After any
change: `bun run typecheck`, `bun run lint`, `bun test`, the checker on every document, and look at a
page. Prove a new check bites by breaking something and restoring it byte-identical.

**How to talk to Tim.** Answer the literal question first, in one line if it is a yes or no. Problems
before successes. No preamble, no self-narration, no ranking your own output. If something needs his
decision, state it in full every time until he answers. Record before you report. His decisions arrive
through his jarvis session and are his.

---

## 4. Execution constraints that bound every design

Facts about the runtime, not preferences. Every unsourceable field in the abandoned model came from
ignoring them.

- The workflow script is **sandboxed** — no filesystem, no process, no module import — so it **cannot
  call Guard**. It routes; it never writes.
- `agent()` fires **no hook**, so Guard never sees a launch, a death, or a duration.
- Only the **main agent** and **subagents** can call Guard.
- **Presence is main-thread only.** No step needing the human can run inside a subagent.
- Claude Workflow compatible only. Do not design for Codex beyond that.

---

## 5. How to build and check

```
bun run check ts/metalang/<file>.yaml                      # one document against its language
bun test                                                   # the suite
bun run typecheck && bun run lint                          # types and style
bun run render ts/metalang/<f>.yaml ts/metalang/<f>.html    # the page
bun run examples                                           # the coloured examples in a handoff page
```

The five documents are `metalang.yaml`, `arch.yaml`, `names.yaml`, `parts.yaml` and `stage.yaml`. The
rendered `.html` beside each is build output and is gitignored. To see a page, open the `.html`.

---

## 6. Where the work actually stands

**The language.** MetaLang checks against itself. The architecture language and the three documents
written in it check clean, and every page draws with nothing it cannot draw. 679 tests pass; the types
and the linter are clean.

**The road.** Two of seven steps done. Step 3 — say what a thing is made of — is next, and is issue
88. `../ROAD.md` is the detail.

**The workflow contract is not signable, and has not been touched in this phase.** Everything in it is
`Proposed`. Seven of the eight phases are names. It was written in the archived language and has to be
rewritten in MetaLang once the language can say what a rule and a test are about, which is steps 3
and 5.

**Design is the one designed phase.** It takes the issue or the request and produces the Contract:
Guard opens the contract on the request; the discussion runs, agent proposing and human deciding,
until the human is finished; the architecture runs five research kinds, four of them required, with
six questions and an exit gate wanting research complete and the Architecture decision locked; Guard
then shows the exact text, locks what was accepted, and signs it.

**A decision can carry eight payload kinds:** Sentence, Architecture, Process, Constraint, CommandSet,
Rule, Spec, UnitTest. Rules and constraints are different things: a constraint is what a judge must
watch for BECAUSE THERE IS NO RULE; a rule is built code, and the judge verifies the rule rather than
proving what it proves.

---

## 7. What is open, and whose it is

**Tim's, unanswered:** which of the archived design's twenty-four lists are the important components;
which target languages the language has to reach. He flagged that the language must account for the
levels of a workflow, said he would come back to it, and said not to start it.

**Approved and not started:** moving the proposal language and its renderer from `handoff/propose/`
into `ts/metalang`, so every proposal is written as YAML the checker reads and drawn as a document;
and drawing a value that names one member of an enum with the name the enum gives it beside the id.

**Not started, not attached to a step:** a way for a closed set to claim it is complete; a way to say
which part of a thing is its name rather than the code assuming it.

**Carried from the abandoned contract, and still true of it:** the contract's `scope` was never
written; `Acceptance` is wanted and is not a declared payload kind, and its shape has never been
designed; the language cannot say that a spec needs the architecture first or that a unit test serves
a spec; and nothing in the run produces a Rule, a Spec or a UnitTest.

---

## 8. Things already tried that FAILED. Do not repeat them.

- **Declaring the six forms as the six kinds a document writes.** Built, measured and declined on
  2026-10-24. It required the architecture language to re-declare MetaLang's own `declaration` choice
  and replace its list, a kind named `enum` took MetaLang's `enum` away from every document written in
  the language, and it left 115 tests failing against 4 for the shape that was taken.
- **Writing the elements of an architecture as the values of one enum.** Replaced on 2026-10-24. An
  enum says the set is closed, which the parts of a guard are not, and it gave every element two
  names.
- **Making a part "optional" to clear errors.** `takes` was marked optional to silence thirty
  complaints, which made "not stated yet" indistinguishable from "legitimately none" and hid a broken
  step for hours.
- **A highlight system that named node kinds in code.** Two components each wrapped themselves and a
  hardcoded list said what could be marked, so it could only mark whole sections. Replaced by a
  highlight file holding paths that work at any depth.
- **Server rendering a document's own page.** Removed on his instruction 2026-10-06. Never
  reintroduce it.
- **Reordering the research pill and adding counts to the band.** Rejected: the earlier way was
  better. The pill is name, qualifier, then the error mark.
- **Claiming something is enforced when nothing enforces it.** Two constraints claimed `mechanical`
  falsely. If nothing is built, it is a constraint.
