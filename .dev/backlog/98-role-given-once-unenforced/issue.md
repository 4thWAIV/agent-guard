`ts/metalang/metalang.yaml` declares `role` and says in its own description that a role is given to at most one property of a type, "because a reader asking which property holds the kind has to get one answer".

**The language states that rule and cannot express it.** No checker rule holds it. `propertiesCarrying` in `ts/metalang/words.ts` returns every property carrying the role, and each renderer draws an error naming both properties where two come back — so the page says something is wrong while `bun run check` passes the document. A language that gives one role to two properties is accepted.

**What it needs.** A checker rule and a test. The rule refuses a type whose properties carry one role twice, naming both properties and the role.

**Why it is not simply a rule.** Saying "at most one" is quantity, which is held out of the language by a standing rule: counts and bounds stay out until a problem cannot be solved without them. This is that problem. The corpus reading in `.dev/inprocess/82-guard-process-workflow/rebuild/GENERAL-FORM.md` counted quantity at roughly 100 occurrences, the largest shape still missing, and a second misfit from that reading — a claim that one place is the only place something is held — is the same shape.

So the choice is between writing one rule in the checker that states the bound in code, and giving the language a way to say "at most one" so the rule is written in the language like every other rule. That choice is Tim's and has not been made.
**Corrected 27 October 2026.** The body above framed this as quantity — a new word for "at most one" — and that was wrong twice over.

It is not a count. It is a uniqueness constraint: no two properties of a type carry the same role. MetaLang already has that concept one level down. A `collection` carries `unique`, "Whether two members may be the same", and `list` and `set` set it to true. What is missing is a way to say the same thing of a type's properties rather than of a collection's members.

A word spelled "at most one" would also have been a one-off. It names a single bound and cannot say "at least one", "exactly two", or anything else, so it is not a vocabulary and does not fit the constraint system.

This is now carried inside step 7 (#92) rather than as a decision of its own, which is where a rule about a document belongs: `rule` in `ts/metalang/metalang.yaml` is an empty shell, and this is the concrete cost of that. It is drawn in the proposal at `handoff/propose/remaining.html` under `say-what-must-and-must-not-be-true`, as the part "no two the same", with the recommendation that it is taken first because it is the smallest and reuses a word the language already has.

How many of a thing may exist is a separate shape, counted about a hundred times in the corpus reading. It is listed in step 7 with no word proposed for it.
