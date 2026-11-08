# What I need decided, and why

> **Superseded on 27 October 2026.** Everything waiting on Tim is now put to him in the proposal
> language and drawn by its own renderer: `handoff/propose/remaining.yaml`, read as
> `handoff/propose/remaining.html`. That page holds five decisions — who can see a thing, how
> exactly types are pinned, the built-in types, letting MetaLang's `external` hold a type, and ways
> of grouping. A sixth waits on him and is not on that page because it is a change to the renderer's
> code rather than to a language: letting `rulebookFor` return the documents it already reads, so
> the second import walk in `ts/metalang/propose.ts` can be deleted.
>
> What follows is the record of what this file asked before that, kept for its history. `quantity`
> below is still open and still unenforced, and it is listed on the road.

Rewritten 26 October 2026. Everything remaining on the language is presented as one proposal in
`PROPOSAL.md` beside this file, drawn in `proposal.html`. That proposal is the fuller picture: the
whole remaining shape, in order, at eleven and a half to fourteen and a half working days.

Two decisions sit inside it: `which-lists-matter` and `quantity`.

---

## quantity — whether the language gains a way to say how many

**What the situation is.** Tim's standing rule is that counts, bounds and how many of a thing may
exist stay out of the language until a problem cannot be solved without them, and that I answer yes
or no when asked whether such a problem exists.

Yes, one exists. `ts/metalang/metalang.yaml` declares `role` and says in its own description that a
role is given to at most one property of a type, because a reader asking which property holds the
kind has to get one answer. The language states that rule and cannot express it. No checker rule
holds it, and the page draws an error naming both properties where it happens, which means the
document is wrong and the checker passes it.

**What the choice is between.**

- **Raise it, as part of step 7.** Saying "at most one" is quantity, the shape the corpus reading
  found about a hundred times and the largest one still missing. It also absorbs a second misfit from
  that reading, a claim that one place is the only place something is held. The word, plus the
  checker rule and test it unlocks, is 1.5 days, and it closes the cheapest open item on the road.
- **Hold it.** The role rule stays stated and unenforced, and step 7 ships without the most frequent
  shape in our own documents.

**My pick: raise it.**

---

## which-lists-matter — which of the archived design's lists the language must be able to write

**What the situation is.** The design document that came before this one is
`rebuild/archive/language-2026-10-06/language.yaml`. It holds 129 named things across 24 lists,
where a list is a closed set of words: every word written down, and nothing outside it allowed. Tim
said the language should cover the important components, and which of the 24 those are has never
been answered.

Four of the 24 are built. `forms` is the six kinds of thing. `members` is the five kinds of member.
`containment` is the three kinds of place, built as `places`. `relations` is the ways two elements
stand to each other, built as three where that document wrote two.

The other twenty fall into three groups.

**Group A, about code.** My pick is all of these.

| list | what it holds |
|---|---|
| `core` | element, kind, containment, relation, constraint — the five things that document said an architecture is made of. Four are built; `constraint` is step 7. |
| `levels` | unit, scope, element, member — four sizes of thing an architecture holds. |
| `visibility` | public, internal, private — who can see a thing. |
| `primitives` | seventeen built-in types, `int8` through `duration`, that a member's type may be. |
| `external` | nine types from outside the architecture a member may use: `string`, `Task`, `IReadOnlyList`, `GitHubIssue` and the rest. |
| `dimensions` | layer, concept, role — three ways of grouping elements. |
| `precision` | loose, exact — how exactly a written thing has to match the source. |

**Group B, about rules and the workflow.** These belong with step 7 and the workflow it serves, not
with the code the language describes: `constraint_kinds`, `permission_verbs`, `actors`, `gate_kinds`,
`gate_condition_categories`, `gate_condition_kinds`, `lenses`, `storage`, `part_absence`. My pick is
to take them when step 7 starts rather than now.

`lenses` holds codegraph and grep, which is how a thing is found in source, so it sits next to step 6.
It names the tool the check uses inside itself, which is not something an architecture document has
to say, so my pick leaves it in group B.

**Group C, about how a page is drawn.** `tones`, `views`, `prose`, `alteration`,
`gate_condition_icons`. My pick is to leave all five out. `tones` is twelve colour names, and the
page now works out a colour from the vocabulary itself; writing twelve colour names down is the
hand-written list that was deleted on purpose.

**My pick: group A now, group B when step 7 starts, group C not at all.**

---

## The shape of an answer

Group A, or group A and B, or name the lists you want.
