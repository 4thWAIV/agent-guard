> **Superseded 26 October 2026, and kept as a working note only.** Tim asked for the proposal to be
> written in the proposal language and read in the browser, with no estimate and nothing assumed.
> That proposal is `handoff/propose/remaining.yaml`, drawn as `handoff/propose/remaining.html`.
> The day figures below are my own planning numbers and were not wanted; they are not the proposal.

# The rest of the architecture language: one proposal

Written 26 October 2026, at Tim's request for every remaining language task in a single proposal.
This covers the language only. Enforcing architecture rules in Python and Rust is issue 93 and is
not on this road.

## What I am asking for

Approval of the whole remaining shape below, which is roughly **eleven to fifteen working days**.
Two items inside it need a specific yes and are marked. Every new word still comes to Tim as its own
proposal page before it is written, the way `map` and `link` did on 2026-10-25; this proposal is the
shape and the order, not a substitute for those.

## Where the language stands

Five of the seven steps are built, and seven of the eight sentences the road is measured against are
sayable. Every document checks clean against the language it names and the suite passes.

The language can say there are six kinds of thing an architecture holds; what each of C#, Python,
Rust and TypeScript calls each of them; that `IGuard` is an interface; that it has a member called
`PrecheckAsync` and that `PrecheckAsync` is a method; that `FileGuard` implements `IGuard`, answers
with a `Verdict`, and that `Verdict` holds a `VerdictKind`; and that `IGuard` sits in the
`AgentGuard.Abstractions` assembly and in no package of its own.

## 1. which-lists-matter — NEEDS YOUR ANSWER

The design document that came before this one, `rebuild/archive/language-2026-10-06/language.yaml`,
holds 129 named things across 24 lists, where a list is a closed set of words: every word written
down and nothing outside it allowed. Four are built — the six forms, the five kinds of member, the
three kinds of place, and the three relations. Tim said the language should cover the important
components, and which of the 24 those are has never been answered.

Group A is the seven about code: `core`, `levels`, `visibility`, `primitives`, `external`,
`dimensions`, `precision`. Group B is the nine about rules and the workflow, which belong with step
7. Group C is the five about how a page is drawn, and `tones` among them is twelve colour names the
page now works out for itself.

**My pick: group A now, group B when step 7 starts, group C not at all.** Writing the seven, each
with its mark, example, name and prose, is about **1 day**.

## 2. Step 6 — how a form is found in a language's source

`ts/metalang/names.yaml` already says what each of the four languages calls each of the six forms.
Those are names written for a reader: Python's interface is "a Protocol or an ABC", TypeScript's
record is "interface, or a type alias". They name a form; they give no way to find one.

The word this step adds is the other half — to find `IGuard` in the C# source, look for
`interface IGuard` — written on the twenty entries that name a form, the four `process` entries
carrying a decided absence. One proposal page, the word, the checker rule, the page drawing, tests.
**1 to 2 days.** Issue 91.

## 3. Step 7 — what must and must not be true

The last step, and the largest. The shapes of statement our own plan documents contain were read off
every one of them on 2026-10-18 and recorded in `rebuild/GENERAL-FORM.md`, with a frequency count
that gives the order below. Nothing in that reading is approved.

**Four of the shapes it found are already closed**, which is why this is smaller than that document
reads. A part whose value names another declaration is `link`. A value indexed by a dimension is
`map`. The three absences are `optional`, `none` and `pending`. An order that carries meaning is said
by choosing `list` or `array` over `set` or `bag`, which MetaLang already distinguishes.

What is left is six words and one claim, each its own proposal page:

| | what it says | seen in the corpus | estimate |
|---|---|---|---|
| polarity | never this — a prohibition | ~80 | 1 day |
| condition | only when something else holds | ~80 | 1.5 to 2 days |
| standing | proposed, approved, locked, withdrawn, not yet decided | ~40 | 1 day |
| computed truth | a truth worked out rather than written down | ~40 | 1.5 to 2 days |
| provenance | the source it was read from, who wrote it, the date | ~15 | 1 day |
| completeness | a closed set claiming nothing outside it exists | a misfit in every reader | 1 day |

**Step 7 as listed: 7 to 8 days.** Issue 92.

Two of the misfits that recurred need no word of their own. A statement withdrawn by a later one is a
value of `standing`. A name that refers to nothing is already refused, because `link` will not accept
a name nothing the document reaches declares.

## 4. quantity — NEEDS YOUR ANSWER

Your standing rule is that counts, bounds and how many of a thing may exist stay out of the language
until a problem cannot be solved without them, and that I answer yes or no when asked whether such a
problem exists.

**Yes, one exists.** `ts/metalang/metalang.yaml` declares `role` and says in its own description that
"a role is given to at most one property of a type, because a reader asking which property holds the
kind has to get one answer". The language states that rule and cannot express it. No checker rule
holds it; the page draws an error naming both properties where it happens, which means the document
is wrong and the checker passes it.

Saying it needs the language to write "at most one". That is quantity, which the corpus reading found
about a hundred times and which is the largest shape still missing. It also absorbs a second misfit —
a claim that one place is the only place something is held.

**My pick: raise it, as part of step 7.** The word, plus the checker rule and test it unlocks, is
**1.5 days**, and it closes the cheapest open item on the road.

If you would rather hold it, the role rule stays stated and unenforced, and step 7 ships without the
most frequent shape in our own documents.

## 5. Issue 87 — paths that cross a link

Deferred on 2026-10-12 with your approval. A path navigates into nested data, and today it may
descend only through owned content, which forms a tree and is guaranteed to terminate. A path that
crosses a `link` can walk a cycle without end, so admitting it needs either a stated maximum depth or
cycle detection, and the termination guarantee has to be proven because the language is required not
to be Turing complete.

It depends on step 7's logic layer existing with paths working, so it follows rather than competes.
**1 to 2 days**, estimable properly once paths exist.

## 6. The levels of a workflow — parked by you

You flagged that the language must account for the levels of a workflow, said you would come back to
it, and said not to start it. It is untouched and not estimated, and nothing above assumes it.

## The total

| | |
|---|---|
| group A lists | 1 day |
| step 6 | 1 to 2 days |
| step 7, six words and the completeness claim | 7 to 8 days |
| quantity, and the rule and test it unlocks | 1.5 days |
| issue 87, after step 7 | 1 to 2 days |
| **total** | **11.5 to 14.5 days** |

Held back on purpose, by your standing rule, and not in the total: any further language feature until
a problem cannot be solved without it.

## The shape of an answer

Group A, or groups A and B, or name the lists you want. And raise quantity now, or hold it and leave
the role rule stated but unenforced.
