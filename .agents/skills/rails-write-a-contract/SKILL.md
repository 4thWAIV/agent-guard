---
name: rails-write-a-contract
description: The sole definition of the contract contents and section order. Load it when writing or reviewing a contract.
---

# Rails: write a contract

A contract is two things and nothing else: the instructions for the agent that does the work, and the checks that prove it is done. If a line is neither, it does not belong.

## The one test for every line

Does this line tell the agent WHAT TO DO, what it MUST NOT do, HOW IT IS CHECKED, or WHICH DECISION THE HUMAN APPROVED? If it does none of those, delete it. If the human would read it and ask "what value does this add?", it is already noise — cut it before they have to.

## The sections, in order

1. **Title** — one line naming the job. No "1 of 2", no status, no framing.
2. **Decisions** — the first section after the title, so the human reads the approved choices before any prose. Every design decision the contract rests on carries the exact decision text and the human's verbatim words approving that exact item. A contract that presents a decision the human did not approve, in their own words, is invalid. `rails-decisions` owns the decision boundary and approval procedure.
3. **Rules to add** — every analyzer rule DESIGN determined, each recorded as an approved decision before RULE-PHASE writes it. If DESIGN determined no rule, say so explicitly.
4. **The standard / what we're building** — the target end state, stated plainly. If there is a non-negotiable rule, lead with it. Say WHAT the end state is, never WHY it is currently broken or how we got here.
5. **Success definition** — the human's standing definition verbatim (ALL criteria met AND no errors in the system as a result of the change) plus this run's specific expected end state. A contract without this is invalid — the gate has nothing to rule against. Any restatement or weakening of it to fit the result is a top-line Lie-catcher finding.
6. **Surfaces** — every store or file the change touches (each place the same value lives), enumerated, so adversaries refute against a written list at contract time, not a post-mortem.
7. **Reuse ledger** — when the work introduces a new capability, record the prior-art ruling for each capability as `reuse <file:line>`, `extract <copies>`, or `new`. A `new` ruling is valid only when every lens required by `rails-dry-code` came back empty, and the contract forbids building a `reuse` or `extract` capability fresh. When the work introduces no new capability, record exactly: `None — this change introduces no new capability`. Do not invent a capability to populate this section. `rails-dry-code` owns the prior-art criteria and evidence requirements.
8. **What to do** — the concrete work: the real files, functions, data, claim ids, byte offsets. Name them; they are the checkable things.
9. **What the agent MAY do** — the permitted actions, when scoping helps.
10. **What the agent MUST NOT do** — the hard boundaries. Anti-cheat lives here: no weakening, skipping, or deleting tests; no commit; no scope expansion; stop and report on any wall.
11. **Acceptance** — numbered checks, each with the command or proof. Where cheating is a risk, the check must be re-derivable by someone other than the worker — an adversary reading the bytes, or a script the human runs. The worker's own word is not proof.
12. **Level** — one line naming L1 or L2, plus one clause explaining why. L3 needs no contract.
13. **Scope** — use this exact rule: “Scope changes ONLY by the human editing the contract — or, when the human is unavailable and has given explicit prior authorization for exactly this extension, by recording that authorization verbatim as the change's ruling provenance and top-lining it.”

## Owner references

`rails-decisions` owns what requires approval, how approval is obtained, and what counts as an approval violation. This guide defines only how approved decisions appear in a contract.

The CONTRACT stage in `rails-run-a-workflow` owns when and how `.agents/workflows/hidden-decision-scan.js` runs and when a contract may advance. This guide does not duplicate that execution procedure.

`rails-dry-code` owns when the prior-art ledger runs, which discovery lenses it uses, and how each ruling is proven. This guide defines only the required Reuse ledger section.

## Cut every one of these — they are noise

- "This is contract 1 of 2 / the other is at <path>." Sequencing is the operator's job, not the agent's. No cross-references between contracts.
- Background: why it is broken, the history, how we got here. The agent needs the target, not the story.
- A level justification paragraph — one line only.
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
- Every item the contract points at — a script, a check, a file — must already exist or be a listed deliverable in the same contract. No dangling references to something that only ran ad-hoc in conversation.

## Last step before it goes to the human

Re-read the whole thing and delete anything that is not an instruction, a boundary, a check, or an approved decision. Confirm every decision line carries the human's own words.
