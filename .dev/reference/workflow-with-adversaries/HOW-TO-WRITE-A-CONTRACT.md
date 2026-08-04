# How to write a contract

A contract is two things and nothing else: the instructions for the agent that does the work, and the checks that prove it is done. If a line is neither, it does not belong.

## The one test for every line

Does this line tell the agent WHAT TO DO, what it MUST NOT do, HOW IT IS CHECKED, or WHICH DECISION THE HUMAN APPROVED? If it does none of those, delete it. If the human would read it and ask "what value does this add?", it is already noise — cut it before they have to.

## The sections, in order

1. **Title** — one line naming the job. No "1 of 2", no status, no framing.
2. **Decisions** — the first section after the title, so the human reads the approved choices before any prose. Every design decision the contract rests on, each carrying the human's verbatim words approving that exact item. A contract that presents a decision the human did not approve, in their own words, is invalid. See "Decisions must carry the human's sign-off" below.
3. **The standard / what we're building** — the target end state, stated plainly. If there is a non-negotiable rule, lead with it. Say WHAT the end state is, never WHY it is currently broken or how we got here.
4. **Success definition** — the human's standing definition verbatim (ALL criteria met AND no errors in the system as a result of the change) plus this run's specific expected end state. A contract without this is invalid — the gate has nothing to rule against. Any restatement or weakening of it to fit the result is a top-line Lie-catcher finding.
5. **Surfaces** — every store or file the change touches (each place the same value lives), enumerated, so adversaries refute against a written list at contract time, not a post-mortem.
6. **Reuse ledger** — the output of the GROUND prior-art search: every capability this change needs, each marked `reuse <file:line>` (an existing owner already does it), `extract <copies>` (it exists in more than one place with no single owner — collapse them into one), or `new` (no lens found it). A `new` mark is valid only when every lens — CodeGraph, lore semantic search, grep — came back empty, and it must say so. The contract forbids building a `reuse` or `extract` capability fresh. No ledger, no contract.
7. **What to do** — the concrete work: the real files, functions, data, claim ids, byte offsets. Name them; they are the checkable things.
8. **What the agent MAY do** — the permitted actions, when scoping helps.
9. **What the agent MUST NOT do** — the hard boundaries. Anti-cheat lives here: no weakening, skipping, or deleting tests; no commit; no scope expansion; stop and report on any wall.
10. **Acceptance** — numbered checks, each with the command or proof. Where cheating is a risk, the check must be re-derivable by someone other than the worker — an adversary reading the bytes, or a script the human runs. The worker's own word is not proof.
11. **Tier** — one line: FULL / LITE / CONTRACT-ONLY, plus one clause of why. One line, never a paragraph.
12. **Scope line** — "Change scope only by editing this file before the run starts."

## Decisions must carry the human's sign-off

A *decision* is anything that adds or reverses a design element: a mechanism, a data or wire format, an invariant, a file, a dependency, an interface, or a change to a previously-approved design. How a named thing is coded — a loop, a helper, a variable name — is implementation, not a decision and needs no sign-off.

- Every decision in the contract carries, inline, the human's verbatim words approving that exact item. No quote next to it → it is not a decision → it does not appear as decided; it stays an open item until the human approves it.
- Silence, moving to another topic, or a request to reword, clarify, or define an item is never approval.
- A blanket "put them all back" or "do it" approves only the items the human had already individually approved — never one that was never approved.
- Reversing a design the human previously approved needs an explicit yes for the reversal — that bar is higher, not lower.
- The anchor must point at the human's actual words, never at "it is already in the code." Existing-in-the-tree is how an unapproved thing launders itself into looking approved.
- Before the Decisions section is final, run the `hidden-decision-scan` (a GROUND adversary, `.claude/workflows/hidden-decision-scan.js`): every forced-but-undecided choice it surfaces that the human would care about must be resolved with the human's own words, or the contract is not ready. An undecided forced choice left off the Decisions list is the same failure class as an unapproved decision.

## Cut every one of these — they are noise

- "This is contract 1 of 2 / the other is at <path>." Sequencing is the operator's job, not the agent's. No cross-references between contracts.
- Background: why it is broken, the history, how we got here. The agent needs the target, not the story.
- A tier justification paragraph — one line only.
- An opening narrative that then repeats the sections below. Say each thing once.
- The same constraint stated in two places (a description AND a "design decisions" list) — merge into one.
- Editorializing ("the only way to pass is to cheat", "the highest-blast-radius thing in the system").
- Meta framing ("agree or edit these", "in one breath", "the ends-with-one bar").
- Reassurance, motivation, pitch.

## Voice

- Plain English. No coined or hyphenated terms — write "both verbatim and contiguous", not "verbatim-contiguous".
- One idea per line.
- Concise is not the goal; CLEAR is. Do not pad into noise; do not compress until it cannot be read.
- Every requirement must be verifiable. If you cannot write a check for it, it is not a contract term.
- Every artifact the contract points at — a script, a check, a file — must already exist or be a listed deliverable in the same contract. No dangling references to something that only ran ad-hoc in conversation.

## Last step before it goes to the human

Re-read the whole thing and delete anything that is not an instruction, a boundary, a check, or an approved decision. Confirm every decision line carries the human's own words. Do this yourself so the human never has to read a line of noise, and never has to catch a decision they did not make.
